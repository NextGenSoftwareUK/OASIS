using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicQueryProjectedAllStorageProvider, ICosmicQueryEntityProjectionCompositionStorageProvider
{
    public bool SupportsEntityProjectionComposition=>true;
    public async Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicEntityProjectionCompositionAsync<T,TResult>(
        CosmicProjectionCompositionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        var result=await ExecuteSelectedSnapshotAsync(plan,null,limits,cancellationToken,entity:true).ConfigureAwait(false);
        return result.IsError
            ? OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<NeoSelectedResult,CosmicProjectionCompositionPacket>(result)
            : new OASISResult<CosmicProjectionCompositionPacket>(result.Result.Proof){IsLoaded=true};
    }

    private static class SelectedNumericContract
    {
        private static readonly Type[] Domains = { typeof(int), typeof(int?), typeof(long), typeof(long?), typeof(ulong), typeof(ulong?), typeof(decimal), typeof(decimal?), typeof(float), typeof(float?), typeof(double), typeof(double?) };
        internal static readonly CosmicScalarCapabilities Value = new(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, CqlProjectionCapabilities, Domains),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, CqlProjectionCapabilities, Domains,
                supportsInt32DoubleAverage: true, supportsInt64DoubleAverage: true)
        });
    }
    private static class CompositionContract
    {
        internal static readonly CosmicProjectionCompositionCapabilities Value = new(
            CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging | CosmicSelectedStageFeatures.Reverse,
            CqlGroupedCapabilities.FilteringCapabilities.Features, CqlGroupedCapabilities.FilteringCapabilities.Keys,
            CqlGroupedCapabilities.OrderingCapabilities.KeyTypes, 32, supportsAll: true, supportsCardinality: true, numericReductions: SelectedNumericContract.Value, extremaDomains: CqlGroupedCapabilities.OrderingCapabilities.KeyTypes);
    }
    public static CosmicProjectionCompositionCapabilities CqlCompositionCapabilities => CompositionContract.Value;
    public CosmicProjectionCompositionCapabilities CompositionCapabilities => CqlCompositionCapabilities;

    public async Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T,TResult>(
        CosmicProjectionCompositionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        var result = await ExecuteSelectedSnapshotAsync(plan, null, limits, cancellationToken).ConfigureAwait(false);
        return result.IsError
            ? OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<NeoSelectedResult, CosmicProjectionCompositionPacket>(result)
            : new OASISResult<CosmicProjectionCompositionPacket>(result.Result.Proof) { IsLoaded = true };
    }

    public async Task<OASISResult<CosmicProjectedAllPacket>> ExecuteCosmicProjectedAllAsync<T,TResult>(
        CosmicProjectedAllPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        var result = await ExecuteSelectedSnapshotAsync<T,TResult>(null, plan, limits, cancellationToken).ConfigureAwait(false);
        if (result.IsError)
            return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<NeoSelectedResult, CosmicProjectedAllPacket>(result);
        if (!result.Result.Counterexamples.HasValue)
            return new OASISResult<CosmicProjectedAllPacket> { IsError = true, ErrorCode = "CQL_PROVIDER_RESULT_INVALID",
                Message = "Neo4j did not acknowledge a native projected All reduction." };
        return new OASISResult<CosmicProjectedAllPacket>(new CosmicProjectedAllPacket(result.Result.Proof,
            result.Result.Counterexamples.Value)) { IsLoaded = true };
    }

    private sealed class NeoSelectedResult
    {
        internal CosmicProjectionCompositionPacket Proof;
        internal long? Counterexamples;
    }

    private async Task<OASISResult<NeoSelectedResult>> ExecuteSelectedSnapshotAsync<T,TResult>(
        CosmicProjectionCompositionPlan<T,TResult> plan, CosmicProjectedAllPlan<T,TResult> all,
        CosmicProjectionCompositionExecutionLimits limits, CancellationToken cancellationToken, bool entity=false) where T:IHolon
    {
        NeoCqlReadBudget readBudget=null;
        try
        {
            var admitted=entity ? CosmicProjectionCompositionPlanner.AdmitEntityExecution(plan,this,limits) : all == null ? CosmicProjectionCompositionPlanner.AdmitExecution(plan,Core.Enums.ProviderType.Neo4jOASIS,
                CqlCapabilities,CqlProjectionCapabilities,CqlCompositionCapabilities,limits)
                : CosmicProjectedAllPlanner.AdmitExecution(all,Core.Enums.ProviderType.Neo4jOASIS,
                    CqlCapabilities,CqlProjectionCapabilities,CqlCompositionCapabilities,limits);
            if(admitted.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,NeoSelectedResult>(admitted);
            var query=all == null ? plan.Query : all.Query.Projection;
            var command=all == null ? CompileCqlComposition(plan,limits) : CompileCqlProjectedAll(all,limits);
            using var budget=readBudget=new NeoCqlReadBudget(limits.MaximumPayloadBytes,cancellationToken);
            budget.Check();
            await using var readOwner=await LeaseCqlReadAsync(budget.Token).ConfigureAwait(false);
            using var session=readOwner.Driver.AsyncSession(config=>config.WithDefaultAccessMode(AccessMode.Read).WithFetchSize(1));
            using var transaction=await session.BeginTransactionAsync(config=>config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            budget.Check();
            var cursor=await transaction.RunAsync(command.Text,command.Parameters).ConfigureAwait(false);
            var decoder=new NeoCqlCompositionDecoder<T,TResult>(query,limits,budget.Check,terminal:all != null, cardinality:plan?.RequestsCardinality == true, numeric:plan?.NumericReduction);
            var entityProof=entity ? new NeoCqlEntityInputProof(plan.EntitySource.Options) : null;
            long observedEntity=0;long? expectedEntity=null;var selectedStarted=false;
            while(true)
            {
                budget.Check();
                if(!await cursor.FetchAsync().ConfigureAwait(false))break;
                budget.Check();
                var record=cursor.Current;
                if(entityProof!=null && record["phase"].As<long>()==-3)
                {
                    if(selectedStarted || record.Values.Count!=6 || record["position"].As<long>()!=observedEntity
                        || record["sourcePosition"].As<long>()!=-1 || record["stageWork"].As<long>()!=0)
                        throw new System.IO.InvalidDataException("Entity source proof phase/order is invalid.");
                    var count=record["nativeCount"].As<long>();
                    if(count<=observedEntity || expectedEntity.HasValue && expectedEntity.Value!=count)
                        throw new System.IO.InvalidDataException("Entity source proof count changed.");
                    expectedEntity=count;
                    entityProof.Observe(record["proof"].As<IReadOnlyDictionary<string,object>>());observedEntity++;
                    continue;
                }
                selectedStarted=true;decoder.Add(record.Values);
            }
            if(expectedEntity.HasValue && observedEntity!=expectedEntity.Value)
                throw new System.IO.InvalidDataException("Entity source proof is incomplete.");
            budget.Check();var packet=decoder.Complete();
            await transaction.CommitAsync().ConfigureAwait(false);budget.Check();
            return new OASISResult<NeoSelectedResult>(new NeoSelectedResult { Proof=packet,Counterexamples=decoder.Counterexamples }){IsLoaded=true};
        }
        catch(Exception cause) when(cancellationToken.IsCancellationRequested)
        { return CompositionCqlFailure("CQL_QUERY_CANCELLED","Neo4j composition was cancelled.",cause); }
        catch(Exception cause) when(readBudget?.IsTimedOut==true)
        { return CompositionCqlFailure("CQL_QUERY_TIMEOUT","Neo4j composition exceeded its cumulative deadline.",cause); }
        catch(NeoCqlNotReadyException cause) { return CompositionCqlFailure("CQL_PROVIDER_NOT_READY",cause.Message,cause); }
        catch(NeoCqlLimitException cause) { return CompositionCqlFailure("CQL_QUERY_LIMIT",cause.Message,cause); }
        catch(NeoCqlTimeoutException cause) { return CompositionCqlFailure("CQL_QUERY_TIMEOUT",cause.Message,cause); }
        catch(Neo4jException cause) when(cause.Code=="Neo.ClientError.Transaction.TransactionTimedOut"
            || cause.Code=="Neo.TransientError.Transaction.TransactionTimedOut" || cause.Code=="Neo.ClientError.Transaction.TransactionTimedOutClientConfiguration")
        { return CompositionCqlFailure("CQL_QUERY_TIMEOUT",cause.Message,cause); }
        catch(OperationCanceledException cause) { return CompositionCqlFailure("CQL_QUERY_CANCELLED",cause.Message,cause); }
        catch(NotSupportedException cause) { return CompositionCqlFailure("CQL_CAPABILITY_UNSUPPORTED",cause.Message,cause); }
        catch(OverflowException cause) { return CompositionCqlFailure("CQL_DATA_INVALID",cause.Message,cause); }
        catch(System.IO.InvalidDataException cause) { return CompositionCqlFailure("CQL_DATA_INVALID",cause.Message,cause); }
        catch(Exception cause) { return CompositionCqlFailure("CQL_PROVIDER_ERROR",cause.Message,cause); }
    }

    private static OASISResult<NeoSelectedResult> CompositionCqlFailure(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCode=code,Message=message,Exception=cause};
}
