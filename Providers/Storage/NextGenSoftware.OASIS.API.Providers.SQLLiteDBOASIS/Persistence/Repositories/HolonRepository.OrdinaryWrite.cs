using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private OASISResult<IEnumerable<IHolon>> SaveOrdinaryGraph(IEnumerable<IHolon> roots,bool children,bool recursive,int depth,int initialDepth=0)
    {
        PreparedGraph graph=null;SqliteTransaction native=null;IDbContextTransaction enlisted=null;
        var opened=false;var commitEntered=false;var known=false;var rollbackFailed=false;var admitted=false;
        Exception failure=null;OASISResult<IEnumerable<IHolon>> result=null;
        void Record(Exception cause)=>failure=failure==null ? cause:new AggregateException(failure,cause);
        try
        {
            graph=PrepareGraph(roots,children,recursive,depth,initialDepth,callerSave:true);
            if(_dbContext.Database.CurrentTransaction!=null || _dbContext.ChangeTracker.HasChanges())
                throw new InvalidOperationException("An ordinary graph save must own its transaction and cannot commit unrelated pending changes.");
            admitted=true;
            _dbContext.Database.OpenConnection();opened=true;
            var connection=(SqliteConnection)_dbContext.Database.GetDbConnection();
            native=connection.BeginTransaction(deferred:false);
            enlisted=_dbContext.Database.UseTransaction(native);
            var ids=graph.Models.Select(model=>model.Id).ToArray();
            var observed=_dbContext.Holons.AsNoTracking().Where(row=>ids.Contains(row.Id)).ToDictionary(row=>row.Id);
            ValidateSaveLifecycle(graph.Models,observed);
            var history=_versionControlEnabled();
            for(var index=0;index<graph.Models.Count;index++)
            {
                observed.TryGetValue(graph.Models[index].Id,out var previous);
                ResolveOrdinaryRoleManifest(graph.Nodes[index],graph.Models[index],previous);
                graph.Models[index]=AddOrUpdateHolon(graph.Models[index],false,
                    history ? prior=>ArchiveHolonRevision(connection,native,prior):null);
            }
            _dbContext.SaveChanges();
            var acknowledged=CommittedGraph(graph);
            commitEntered=true;enlisted.Commit();
            result=new OASISResult<IEnumerable<IHolon>>(acknowledged){IsSaved=true,SavedCount=graph.Models.Count};known=true;
            graph.Publish();
        }
        catch(Exception cause){Record(cause);}
        if(native!=null && !commitEntered)
        {try{native.Rollback();}catch(Exception cause){rollbackFailed=true;Record(cause);}}
        foreach(var resource in new IDisposable[]{enlisted,native})
        {try{resource?.Dispose();}catch(Exception cause){Record(cause);}}
        if(opened){try{_dbContext.Database.CloseConnection();}catch(Exception cause){Record(cause);}}
        if(failure!=null)
        {
            if(known)
            {result.IsWarning=true;result.WarningCount++;result.Exception=failure;result.Message="The graph committed, but caller publication or SQLite cleanup failed.";}
            else
            {
                if(admitted)failure=HolonSaveFailure(graph?.Models ?? new(),commitEntered || rollbackFailed,failure,true);
                result=new OASISResult<IEnumerable<IHolon>> {IsError=true,ErrorCount=1,IsSaved=false,Exception=failure,
                    ErrorCode=commitEntered || rollbackFailed ? "SQLITE_WRITE_RECONCILIATION_REQUIRED":failure is NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException?NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode:"SQLITE_GRAPH_WRITE_FAILED",
                    Message=commitEntered || rollbackFailed ? "Reconcile the original graph write before another mutation.":"The graph write did not commit."};
            }
        }
        return result;
    }

    private static void ResolveOrdinaryRoleManifest(PlannedHolonWrite write,HolonModel model,HolonModel previous)
    {
        var field=nameof(Holon.ChildRoleManifestJson);
        string storedManifest=null;
        if(previous!=null)
        {
            var stored=OasisJson.ParseStoredObject(previous.PersistentValueJson).Property(field);
            if(stored!=null)
            {
                if(stored.Value.Type!=JTokenType.String)
                    throw new InvalidDataException("Stored child-role observation must be canonical manifest JSON.");
                storedManifest=(string)stored.Value;
            }
        }
        // The immediate transaction protects this observation. Selected writes
        // capture logical roles; root-only writes preserve the existing owner.
        var resolved=HolonChildRolePersistence.Resolve(write,HolonChildRoleWriteKind.SourceSave,
            previous!=null,storedManifest);
        var values=OasisJson.ParseStoredObject(model.PersistentValueJson);
        if(resolved==null)values.Remove(field);else values[field]=resolved;
        model.PersistentValueJson=values.ToString(Formatting.None);
        write.Snapshot.ChildRoleManifestJson=resolved;
    }
}
