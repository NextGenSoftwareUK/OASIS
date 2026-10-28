using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed partial class SqliteCosmicChainIndexStore : ICosmicChainQueryIndexScalarReader
{
    public static CosmicScalarCapabilities CanonicalScalarCapabilities { get; } = new(
        new[] { CosmicScalarOperation.Count, CosmicScalarOperation.LongCount, CosmicScalarOperation.Any },
        Array.Empty<CosmicScalarSelectionCapability>(), false, false, false, false, false);
    public CosmicScalarCapabilities IndexedScalarCapabilities => canonicalRowProjection
        ? CanonicalScalarCapabilities : new(Array.Empty<CosmicScalarOperation>());

    public async Task<OASISResult<CosmicChainIndexCardinality>> QueryCardinalityAsync<T,TResult>(CosmicScalarPlan<T,TResult> plan,
        ICosmicChainQuerySourceObserver observer,CancellationToken cancellationToken=default) where T:IHolon
    {
        try{
            ObjectDisposedException.ThrowIf(disposed,this);
            if(!canonicalRowProjection||plan==null||observer==null)
                return Error<CosmicChainIndexCardinality>("CQL_CAPABILITY_UNSUPPORTED","An opted-in canonical index and explicit observer/plan are required.");
            var admitted=IndexedRowCapabilities.AdmitIndexedRows(plan.SourcePlan,source);
            if(admitted.IsError)return Error<CosmicChainIndexCardinality>(admitted.ErrorCode,admitted.Message);
            admitted=CanonicalScalarCapabilities.Admit(plan.Query);
            if(admitted.IsError)return Error<CosmicChainIndexCardinality>(admitted.ErrorCode,admitted.Message);
            if(plan.Query.TerminalPredicate!=null||plan.Query.Selector!=null)
                return Error<CosmicChainIndexCardinality>("CQL_CAPABILITY_UNSUPPORTED","Indexed cardinality does not enroll post-window predicates or selectors.");
            var access=plan.Query.Source.ReadAccess;
            if(access==null||access.Authority.HasValue&&access.Authority!=source.SourceProvider)
                return Error<CosmicChainIndexCardinality>("CQL_ACCESS_INVALID","A matching server actor read policy is required.");
            var sql=HolonRepository.CompileChainIndexRows(plan.Query.Source);
            CosmicChainQueryCheckpoint checkpoint=null;long count=0;int historyCount=0;string historyHash=null;
            var snapshot=await CosmicIndexedReadAttempts.ExecuteIndexSnapshotAsync(source,()=>{
                cancellationToken.ThrowIfCancellationRequested();
                using var connection=Open();
                using var budget=metadataPolicy==null?null:new SqliteCqlReadBudget(connection,cancellationToken);
                using var transaction=connection.BeginTransaction(deferred:true);
                var state=ReadState(connection,transaction);
                if(state==null)return Task.FromResult(Error<bool>("CQL_SOURCE_EVIDENCE_MISSING","No complete authenticated index checkpoint exists."));
                ValidateQueryProjection(connection,transaction,state);
                checkpoint=state.Checkpoint;historyCount=state.HistoryCount;historyHash=state.HistoryHash;
                using var command=connection.CreateCommand();command.Transaction=transaction;
                // Visibility/filtering precedes the native source window. The outer
                // scalar never counts a fetched/limited application row buffer.
                var window="SELECT 1 FROM CosmicChainIndexRows AS Holon WHERE Holon.IsDeleted=0 AND ("+sql.Predicate+") ORDER BY "+sql.Ordering+" LIMIT $limit OFFSET $offset";
                command.CommandText=plan.Query.Operation==CosmicScalarOperation.Any?"SELECT EXISTS("+window+")":"SELECT COUNT(*) FROM ("+window+") AS Selected";
                command.Parameters.AddRange(sql.Parameters.ToArray());
                command.Parameters.AddWithValue("$limit",plan.Query.Source.Limit??-1);
                command.Parameters.AddWithValue("$offset",plan.Query.Source.Offset);
                using var registration=cancellationToken.Register(command.Cancel);
                var native=command.ExecuteScalar();budget?.Check();
                if(native is not long integer)throw new InvalidOperationException("Native cardinality must retain the exact SQLite INTEGER domain.");
                count=integer;return Task.FromResult(new OASISResult<bool>(true));
            },cancellationToken);
            if(snapshot.IsError)return Error<CosmicChainIndexCardinality>(snapshot.ErrorCode,snapshot.Message,snapshot.Exception);
            var requirement=plan.Query.Source.IndexedSource;
            var head=await observer.ObserveHeadAsync(source,CosmicChainQueryCheckpointAdmission.RequiredSlots(requirement,checkpoint),cancellationToken);
            if(head?.IsError!=false||head.Result==null)return Error<CosmicChainIndexCardinality>(head?.ErrorCode??"CQL_SOURCE_EVIDENCE_MISSING","Fresh source head evidence is unavailable.",head?.Exception);
            var freshness=CosmicChainQueryCheckpointAdmission.Admit(requirement,checkpoint,head.Result,DateTimeOffset.UtcNow);
            if(freshness.IsError)return Error<CosmicChainIndexCardinality>(freshness.ErrorCode,freshness.Message);
            cancellationToken.ThrowIfCancellationRequested();
            return new(new(checkpoint,head.Result,plan.Query.Operation,count,historyCount,historyHash)){IsLoaded=true};
        }
        catch(OperationCanceledException cause){return Error<CosmicChainIndexCardinality>("CQL_QUERY_CANCELLED","Indexed cardinality cancelled.",cause);}
        catch(CosmicIndexedReadException cause){return Error<CosmicChainIndexCardinality>(cause.Code,cause.Message,cause);}
        catch(NextGenSoftware.OASIS.API.Core.Services.Subscriptions.UsageProtocolException cause){return Error<CosmicChainIndexCardinality>(cause.Code,cause.Message,cause);}
        catch(Exception cause){return Error<CosmicChainIndexCardinality>("CQL_INDEX_READ_FAILED","Native indexed cardinality failed; no scalar published.",cause);}
    }
}
