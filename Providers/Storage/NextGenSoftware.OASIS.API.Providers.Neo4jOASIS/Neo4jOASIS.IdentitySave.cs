using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using INode=Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static async Task<(T Previous,string Expected)> ReadLockedIdentityAsync<T>(IAsyncTransaction transaction,string kind,Guid id,Func<INode,T> readCurrent) where T:class,IHolonBase
    {
        HistoryRecordKind(kind);
        var rows=await(await transaction.RunAsync("MATCH (a:"+kind+" {Id:$id}) SET a.__oasisAvatarLock=true REMOVE a.__oasisAvatarLock RETURN a",new{id=id.ToString()}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
        if(rows.Count>1)throw new StorageIdentityAmbiguousException();
        return rows.Count==0?(null,null):(readCurrent(rows[0]["a"].As<INode>()),(string)rows[0]["a"].As<INode>()["SnapshotJson"]);
    }

    private async Task<OASISResult<T>> WriteIdentitySnapshotAsync<T>(string kind,T staged,Func<INode,T> readCurrent,
        string assignments,Func<T,Dictionary<string,object>> nativeFields,bool archiveVersions) where T:class,IHolonBase
    {
        var result=new OASISResult<T>();var state=new NeoHolonTransactionState();
        IAsyncSession session=null;IAsyncTransaction transaction=null;Exception failure=null,operationFailure=null;
        try
        {
            if(!IsProviderActivated||_driver==null)throw new InvalidOperationException("Neo4j provider is not activated.");
            session=_driver.AsyncSession();transaction=await session.BeginTransactionAsync().ConfigureAwait(false);
            var observed=await ReadLockedIdentityAsync(transaction,kind,staged.Id,readCurrent).ConfigureAwait(false);
            RequireSaveLifecycle(observed.Previous,staged);
            if(archiveVersions)await StoredRecordVersions.StampAsync(staged.Id,staged,observed.Previous,stored=>ArchiveRecordVersionAsync(transaction,kind,stored)).ConfigureAwait(false);
            var parameters=nativeFields(staged);
            parameters["id"]=staged.Id.ToString();parameters["snapshot"]=OasisJson.Serialize(staged);
            parameters["expected"]=observed.Expected;parameters["writeId"]=Guid.NewGuid().ToString("N");
            var rows=await(await transaction.RunAsync(SnapshotWritePrefix(kind)+assignments,parameters).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            if(rows.Count==0)throw new StorageLifecycleConflictException("Neo4j identity changed before snapshot replacement.");
            if(rows.Count!=1)throw new InvalidDataException("Neo4j identity save did not acknowledge exactly one record.");
            var acknowledged=readCurrent(rows[0]["a"].As<INode>());
            RequireAcknowledgedRecord(staged,acknowledged);
            state.CommitDispatched=true;await transaction.CommitAsync().ConfigureAwait(false);state.KnownCommitted=true;
            result.Result=acknowledged;result.IsSaved=true;result.SavedCount=1;result.HasAnyHolonsChanged=true;
        }
        catch(Exception cause)
        {
            failure=cause;operationFailure=cause;
            if(transaction!=null&&!state.CommitDispatched&&!state.KnownCommitted)
            {
                try{await transaction.RollbackAsync().ConfigureAwait(false);}
                catch(Exception rollback){state.RollbackFailed=true;failure=new AggregateException(cause,rollback);}
            }
        }
        finally
        {
            foreach(var resource in new IDisposable[]{transaction,session})
            {
                try{resource?.Dispose();}
                catch(Exception cleanup){failure=failure==null?cleanup:new AggregateException(failure,cleanup);}
            }
        }
        if(failure==null)return result;
        result.Exception=failure;result.Message=failure.Message;
        if(state.KnownCommitted){result.IsWarning=true;result.WarningCount++;return result;}
        var uncertain=state.CommitDispatched||state.RollbackFailed;
        result.IsError=true;result.ErrorCount++;result.Result=null;result.IsSaved=false;result.SavedCount=0;result.HasAnyHolonsChanged=false;
        if(uncertain)result.Exception=new Neo4jMutationReconciliationException(failure);
        var reason=operationFailure??failure;
        result.ErrorCode=uncertain?"COSMIC_WRITE_RECONCILIATION_REQUIRED":reason is StorageLifecycleConflictException?StorageLifecycleConflictException.ErrorCode
            :reason is ArgumentException or InvalidDataException?"HYPERDRIVE_PAYLOAD_INVALID":"COSMIC_WRITE_FAILED";
        result.Message=result.Exception.Message;return result;
    }
}
