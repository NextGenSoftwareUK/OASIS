using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public async Task<OASISResult<CosmicKeyedEntityBatch>> ExecuteCosmicKeyedEntitiesAsync<T>(
        CosmicQueryPlan<T> plan,IReadOnlyList<CosmicQueryPath> keys,int maximumRows,
        int maximumPayloadBytes,CancellationToken cancellationToken) where T:IHolon
    {
        try
        {
            var admitted=CosmicKeyedEntitySource.Admit(plan,keys,ProviderType.SQLLiteDBOASIS,
                SqliteCqlContract.Capabilities,SQLLiteDBOASIS.CqlProjectionCapabilities,maximumRows,maximumPayloadBytes);
            if(admitted.IsError)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,CosmicKeyedEntityBatch>(admitted);
            var translator=new CqlSqlTranslator(plan.Query.ReadAccess);
            var predicate=translator.TranslateSource(plan.Query.Predicates);
            var source=CompileKeyedSourceSql(translator,plan.Query,keys,Array.Empty<CosmicQueryPath>(),
                maximumRows,predicate,"keyedEntity");
            cancellationToken.ThrowIfCancellationRequested();
            using var exclusive=_dbContext.GetService<IConcurrencyDetector>().EnterCriticalSection();
            var connection=(SqliteConnection)_dbContext.Database.GetDbConnection();
            var opened=connection.State!=ConnectionState.Open;
            if(opened)await _dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var budget=new SqliteCqlReadBudget(connection,cancellationToken);
                using var traversal=translator.HasRelationship?new SqliteCqlTraversalGuard(connection,budget.Check):null;
                if(_dbContext.Database.CurrentTransaction!=null
                    && _dbContext.Database.CurrentTransaction.GetDbTransaction().IsolationLevel!=IsolationLevel.Serializable)
                    return KeyedCqlError("CQL_CONSISTENCY_UNSUPPORTED","SQLite keyed snapshots require a serializable read owner.");
                using var transaction=_dbContext.Database.CurrentTransaction==null
                    ?connection.BeginTransaction(IsolationLevel.Serializable,deferred:true):null;
                using var owner=transaction==null?null:_dbContext.Database.UseTransaction(transaction);
                try
                {
                    if(translator.HasRelationship)
                    {
                        using var validation=Command("SELECT count(*) FROM Holon WHERE "+predicate);
                        await validation.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);Check();
                    }
                    var storageKeys=new Dictionary<Guid,string>();
                    var input=await ReadNativeCqlKeyedSourceAsync(plan.Query,source,keys,Array.Empty<CosmicQueryPath>(),
                        maximumRows,maximumPayloadBytes,storageKeys,Command,Check,cancellationToken).ConfigureAwait(false);
                    long bytes=input.Bytes;
                    var entities=await ReadNativeCqlIdentitySnapshotsAsync(storageKeys.Keys.ToArray(),storageKeys,
                        maximumPayloadBytes-bytes,Check,cancellationToken,
                        payloadBytes=>bytes=checked(bytes+payloadBytes)).ConfigureAwait(false);
                    Check();
                    var rows=input.Rows.Values.Select(proof=>new CosmicKeyedEntityRow(proof,entities[proof.Id])).ToArray();
                    var batch=new CosmicKeyedEntityBatch(input.Count,rows);
                    transaction?.Commit();Check();
                    return new OASISResult<CosmicKeyedEntityBatch>(batch){IsLoaded=true};

                    void Check(){budget.Check();traversal?.Check();}
                    SqliteCommand Command(string sql)
                    {
                        var command=connection.CreateCommand();
                        command.Transaction=_dbContext.Database.CurrentTransaction?.GetDbTransaction() as SqliteTransaction;
                        command.CommandText=sql;
                        foreach(var parameter in translator.Parameters)
                            command.Parameters.Add(new SqliteParameter(parameter.ParameterName,parameter.SqliteType){Value=parameter.Value});
                        return command;
                    }
                }
                catch(SqliteException cause) when(cause.SqliteErrorCode==9){budget.Check();throw;}
            }
            finally{if(opened)await _dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);}
        }
        catch(OverflowException cause){return KeyedCqlError("CQL_DATA_INVALID",cause.Message,cause);}
        catch(InvalidDataException cause){return KeyedCqlError("CQL_DATA_INVALID",cause.Message,cause);}
        catch(SqliteCqlDeadlineException cause){return KeyedCqlError("CQL_QUERY_TIMEOUT",cause.Message,cause);}
        catch(SqliteCqlResourceException cause){return KeyedCqlError("CQL_QUERY_LIMIT",cause.Message,cause);}
        catch(NotSupportedException cause){return KeyedCqlError("CQL_CAPABILITY_UNSUPPORTED",cause.Message,cause);}
        catch(OperationCanceledException cause){return KeyedCqlError("CQL_QUERY_CANCELLED",cause.Message,cause);}
        catch(Exception cause){return KeyedCqlError("CQL_QUERY_FAILED","SQLite native keyed entity read did not complete.",cause);}
    }

    private async Task<Dictionary<Guid,IHolon>> ReadNativeCqlIdentitySnapshotsAsync(
        IReadOnlyList<Guid> identities,IReadOnlyDictionary<Guid,string> sourceKeys,long maximumBytes,
        Action check,CancellationToken cancellationToken,Action<long> reserveBytes)
    {
        var byId=new Dictionary<Guid,IHolon>(identities.Count);
        if(identities.Count==0){check();return byId;}
        long measured=0;
        var storageKeys=identities.Select(id=>sourceKeys[id]).ToArray();
        var records=_dbContext.Holons.AsNoTracking().Where(row=>storageKeys.Contains(row.Id));
        await foreach(var record in records.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            check();
            var bytes=CqlRecordSize(record);
            measured=checked(measured+bytes);
            if(measured>maximumBytes)throw new SqliteCqlResourceException("Native entity snapshots exceed their complete payload bound.");
            reserveBytes(bytes);
            var entity=GetHolonFromEntity(record);
            byId.Add(entity.Id,entity);
        }
        check();
        if(byId.Count!=identities.Count || identities.Any(id=>!byId.ContainsKey(id)))
            throw new InvalidDataException("Native entity snapshots do not cover the complete selected identity set.");
        return byId;
    }

    private static OASISResult<CosmicKeyedEntityBatch> KeyedCqlError(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCode=code,Message=message,Exception=cause};
}
