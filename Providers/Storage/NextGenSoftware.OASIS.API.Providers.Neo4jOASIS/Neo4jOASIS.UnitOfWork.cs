using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using INode = Neo4j.Driver.INode;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : IConditionalHolonUnitOfWorkStorageProvider
{
    /// <summary>Ordinary UOW owns its commit; held owners reuse exactly the native staging work below.</summary>
    public async Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonUnitOfWorkAsync(
        ConditionalHolonUnitOfWorkRequest request,CancellationToken cancellationToken=default)
    {
        if(request==null)return NativeUnitOfWorkError("COSMIC_UNIT_OF_WORK_INVALID","A unit of work is required.");
        if(request.Write.Authority!=Core.Enums.ProviderType.Neo4jOASIS)
            return NativeUnitOfWorkError("COSMIC_AUTHORITY_MISMATCH","The unit of work belongs to another provider authority.");
        if(!IsProviderActivated||_driver==null)return NativeUnitOfWorkError("COSMIC_AUTHORITY_UNAVAILABLE","Neo4j provider is not activated.");
        var binding=request.Write.ReplicationEnrollment?.SourceStorageIdentity;
        var gateHeld=false;var commitDispatched=false;var rollbackAttempted=false;var acknowledged=false;
        OASISResult<IEnumerable<IHolon>> result=null;Exception primary=null;
        try
        {
            if(binding!=null){await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);gateHeld=true;}
            await using var session=_driver.AsyncSession();
            await using var transaction=await session.BeginTransactionAsync(config=>config.WithMetadata(
                new Dictionary<string,object>{["COSMIC.OperationId"]=request.Write.OperationId.ToString("D")})).ConfigureAwait(false);
            try
            {
                var staged=await StageNativeUnitOfWorkAsync(request,transaction,cancellationToken,allowReplay:true).ConfigureAwait(false);
                result=staged.Result;primary=result?.Exception;
                if(result==null||result.IsError)
                {
                    rollbackAttempted=true;await transaction.RollbackAsync().ConfigureAwait(false);
                    return result??NativeUnitOfWorkError("COSMIC_CONDITIONAL_WRITE_FAILED","A native unit-of-work stage returned no outcome.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                if(staged.Replay){rollbackAttempted=true;await transaction.RollbackAsync().ConfigureAwait(false);}
                else{commitDispatched=true;await transaction.CommitAsync().ConfigureAwait(false);}
                acknowledged=true;return result;
            }
            catch(Exception cause) when(!commitDispatched&&!rollbackAttempted)
            {
                try{rollbackAttempted=true;await transaction.RollbackAsync().ConfigureAwait(false);}
                catch(Exception rollback){throw new AggregateException(cause,rollback);}
                throw;
            }
        }
        catch(Exception cause)
        {
            if(primary!=null&&!ReferenceEquals(primary,cause))cause=new AggregateException(primary,cause);
            if(acknowledged&&result!=null)
            {result.IsWarning=true;result.WarningCount++;result.Exception=cause;
                result.Message="The Neo4j unit of work is acknowledged, but native cleanup failed.";return result;}
            return NativeUnitOfWorkError(commitDispatched?"COSMIC_WRITE_RECONCILIATION_REQUIRED":
                cause is OperationCanceledException?"COSMIC_CONDITIONAL_WRITE_CANCELLED":"COSMIC_CONDITIONAL_WRITE_FAILED",
                commitDispatched?"Reconcile the original unit-of-work receipt before another mutation.":"The Neo4j unit of work did not commit.",cause);
        }
        finally{if(gateHeld)_lifecycleGate.Release();}
    }

    private async Task<(OASISResult<IEnumerable<IHolon>> Result,bool Replay)> StageNativeUnitOfWorkAsync(
        ConditionalHolonUnitOfWorkRequest request,IAsyncTransaction transaction,CancellationToken cancellationToken,bool allowReplay)
    {
        if(request==null||transaction==null)return(NativeUnitOfWorkError("COSMIC_UNIT_OF_WORK_INVALID","A request and native transaction are required."),false);
        if(request.Write.Authority!=Core.Enums.ProviderType.Neo4jOASIS)
            return(NativeUnitOfWorkError("COSMIC_AUTHORITY_MISMATCH","The unit of work belongs to another provider authority."),false);
        var(save,delete)=request.CreatePartRequests();
        if(save==null&&delete==null)return(NativeUnitOfWorkError("COSMIC_UNIT_OF_WORK_INVALID","A unit of work needs at least one entry."),false);
        var expected=request.Write.ReplicationEnrollment?.SourceStorageIdentity;
        cancellationToken.ThrowIfCancellationRequested();
        var parentId=request.Write.OperationId.ToString("D");
        var parents=await(await transaction.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN r",
            new{operation=parentId}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
        if(parents.Count>1)return(NativeUnitOfWorkError("COSMIC_RECEIPT_INVALID","The native operation receipt is not unique."),false);
        var replay=parents.Count==1;
        if(replay)
        {
            var stored=parents[0]["r"].As<INode>();
            if(!stored.Properties.TryGetValue("Fingerprint",out var fingerprint)||(fingerprint as string)!=request.ParentFingerprint)
                return(NativeUnitOfWorkError("COSMIC_OPERATION_ID_REUSED","This operation identity is bound to another write intent or actor."),false);
            if(!allowReplay)return(NativeUnitOfWorkError("COSMIC_TRANSACTION_OPERATION_ALREADY_COMMITTED","A held transaction requires a fresh operation identity."),true);
        }
        var results=new List<OASISResult<IEnumerable<IHolon>>>();
        foreach(var part in new[]{save,delete}.Where(part=>part!=null))
        {
            var plan=part.Write.ReplicationEnrollment==null?ConditionalHolonPlan.Create(part,Core.Enums.ProviderType.Neo4jOASIS)
                :ConditionalHolonPlan.CreateReplicating(part,Core.Enums.ProviderType.Neo4jOASIS,part.Write.ReplicationEnrollment);
            var write=new NeoAtomicHolonWrite(plan,part.Write.ActorId,cancellationToken){ExpectedStorage=expected,
                StorageBindingCodePrefix="COSMIC_SOURCE_STORAGE_BINDING",Ambient=transaction};
            var written=await SaveHolonSnapshotsAsync(part.Roots,false,false,0,0,write).ConfigureAwait(false);
            if(written==null||written.IsError)return(written??NativeUnitOfWorkError("COSMIC_CONDITIONAL_WRITE_FAILED","A unit-of-work part returned no outcome."),replay);
            if(replay!=write.AmbientReplayed)return(NativeUnitOfWorkError("COSMIC_RECEIPT_INVALID","The parent and part receipts disagree."),replay);
            results.Add(written);
        }
        if(!replay)
        {
            if(!await ApplyUniqueKeyClaimsAsync(transaction,request).ConfigureAwait(false))
                return(NativeUnitOfWorkError("COSMIC_UNIQUE_KEY_CONFLICT","A unique key is already held by another record."),false);
            await(await transaction.RunAsync("CREATE (r:CosmicWriteReceipt {OperationId:$operation,Fingerprint:$fingerprint,ResultJson:$json,CommittedAtUtc:$now})",
                new{operation=parentId,fingerprint=request.ParentFingerprint,now=DateTime.UtcNow.ToString("O"),
                    json=System.Text.Json.JsonSerializer.Serialize(new{kind=ConditionalHolonUnitOfWorkRequest.ReceiptKind,
                        save=request.SaveOperationId.ToString("D"),delete=request.DeleteOperationId.ToString("D")})}).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
        var saved=save==null?null:results[0];var deleted=delete==null?null:results[results.Count-1];
        var combined=new OASISResult<IEnumerable<IHolon>>(results.SelectMany(item=>item.Result).ToList()){
            IsSaved=true,IsDeleted=deleted!=null,HasAnyHolonsChanged=true,SavedCount=saved?.SavedCount??0,DeletedCount=deleted?.DeletedCount??0,
            IsWarning=results.Any(item=>item.IsWarning),WarningCount=results.Sum(item=>item.WarningCount),
            MetaData=new Dictionary<string,string>{["COSMIC.OperationId"]=parentId,["COSMIC.SaveOperationId"]=request.SaveOperationId.ToString("D"),
                ["COSMIC.DeleteOperationId"]=request.DeleteOperationId.ToString("D")}};
        foreach(var(part,name)in new[]{(saved,"Save"),(deleted,"Delete")})
            if(part?.MetaData!=null)foreach(var pair in part.MetaData.Where(pair=>pair.Key!="COSMIC.OperationId"))combined.MetaData[pair.Key+"."+name]=pair.Value;
        return(combined,replay);
    }
    private static OASISResult<IEnumerable<IHolon>> NativeUnitOfWorkError(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};

    /// <summary>M10 claims in the unit-of-work transaction; the KeyId constraint rejects racing claims.</summary>
    private static async Task<bool> ApplyUniqueKeyClaimsAsync(IAsyncTransaction transaction, ConditionalHolonUnitOfWorkRequest request)
    {
        if (request.Deletes.Count > 0)
            await (await transaction.RunAsync("MATCH (k:CosmicUniqueKey) WHERE k.HolonId IN $ids DELETE k",
                new { ids = request.Deletes.Select(row => row.Id.ToString("D")).ToArray() }).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
        foreach (var claim in request.UniqueKeys)
        {
            var holon = claim.HolonId.ToString("D");
            await (await transaction.RunAsync("MATCH (k:CosmicUniqueKey {HolonId:$holon,Scope:$scope}) WHERE $key IS NULL OR k.KeyId <> $key DELETE k",
                new { holon, scope = claim.Scope, key = claim.Value == null ? null : claim.NativeKey }).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
            if (claim.Value == null) continue;
            var row = await (await transaction.RunAsync("MERGE (k:CosmicUniqueKey {KeyId:$key}) ON CREATE SET k.Scope=$scope, k.HolonId=$holon RETURN k.HolonId AS holder",
                new { key = claim.NativeKey, scope = claim.Scope, holon }).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            if (row["holder"].As<string>() != holon) return false;
        }
        return true;
    }
}
