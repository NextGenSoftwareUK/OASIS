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

public partial class Neo4jOASIS : ICosmicQueryProjectedAllStorageProvider, ICosmicQueryEntityProjectionCardinalityStorageProvider, ICosmicQueryEntityProjectionNumericStorageProvider, ICosmicQueryEntityProjectedAllStorageProvider
{
    public bool SupportsEntityProjectedAll=>true;
    public Task<OASISResult<CosmicProjectedAllPacket>> ExecuteCosmicEntityProjectedAllAsync<T,TResult>(
        CosmicProjectedAllPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,CancellationToken cancellationToken=default) where T:IHolon
        => ExecuteProjectedAllReadAsync(plan,limits,cancellationToken,true);

    public bool SupportsEntityProjectionNumeric => true;
    public bool SupportsEntityProjectionCardinality=>true;
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
            CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging | CosmicSelectedStageFeatures.Reverse | CosmicSelectedStageFeatures.Distinct,
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

    public Task<OASISResult<CosmicProjectedAllPacket>> ExecuteCosmicProjectedAllAsync<T,TResult>(
        CosmicProjectedAllPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,CancellationToken cancellationToken=default) where T:IHolon
        => ExecuteProjectedAllReadAsync(plan,limits,cancellationToken,false);
    private async Task<OASISResult<CosmicProjectedAllPacket>> ExecuteProjectedAllReadAsync<T,TResult>(
        CosmicProjectedAllPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,CancellationToken cancellationToken,bool entity) where T:IHolon
    {
        var result = await ExecuteSelectedSnapshotAsync<T,TResult>(null, plan, limits, cancellationToken,entity).ConfigureAwait(false);
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
        internal CosmicProjectedSetPacket SetProof;
        internal long? Counterexamples;
    }

    private async Task<OASISResult<NeoSelectedResult>> ExecuteSelectedSnapshotAsync<T,TResult>(
        CosmicProjectionCompositionPlan<T,TResult> plan, CosmicProjectedAllPlan<T,TResult> all,
        CosmicProjectionCompositionExecutionLimits limits, CancellationToken cancellationToken, bool entity=false,
        CosmicProjectedSetPlan<T,TResult> selectedSet=null,CosmicProjectedSetExecutionLimits setLimits=null) where T:IHolon
    {
        NeoCqlReadBudget readBudget=null;
        try
        {
            if(selectedSet!=null)
            {
                var actual=CosmicProjectedSetPlanner.Plan(selectedSet.Query,Core.Enums.ProviderType.Neo4jOASIS,
                    new CosmicProjectionCompositionTarget(Core.Enums.ProviderType.Neo4jOASIS,CqlCapabilities,
                        CqlProjectionCapabilities,CqlCompositionCapabilities),ProjectedSetCapabilities,selectedSet.Options);
                if(actual.IsError)return CompositionCqlFailure(actual.ErrorCode,actual.Message,actual.Exception);
                selectedSet=actual.Result;
            }
            else
            {
            var admitted=entity ? (all==null ? CosmicProjectionCompositionPlanner.AdmitEntityExecution(plan,this,limits)
                : CosmicProjectedAllPlanner.AdmitEntityExecution(all,this,limits)) : all == null ? CosmicProjectionCompositionPlanner.AdmitExecution(plan,Core.Enums.ProviderType.Neo4jOASIS,
                CqlCapabilities,CqlProjectionCapabilities,CqlCompositionCapabilities,limits)
                : CosmicProjectedAllPlanner.AdmitExecution(all,Core.Enums.ProviderType.Neo4jOASIS,
                    CqlCapabilities,CqlProjectionCapabilities,CqlCompositionCapabilities,limits);
            if(admitted.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,NeoSelectedResult>(admitted);
            }
            var query=all == null ? plan.Query : all.Query.Projection;
            var command=selectedSet!=null ? CompileCqlProjectedSet(selectedSet,setLimits)
                : all == null ? CompileCqlComposition(plan,limits) : CompileCqlProjectedAll(all,limits);
            using var budget=readBudget=new NeoCqlReadBudget(setLimits?.MaximumPayloadBytes??limits.MaximumPayloadBytes,cancellationToken);
            budget.Check();
            await using var readOwner=await LeaseCqlReadAsync(budget.Token).ConfigureAwait(false);
            using var session=readOwner.Driver.AsyncSession(config=>config.WithDefaultAccessMode(AccessMode.Read).WithFetchSize(1));
            using var transaction=await session.BeginTransactionAsync(config=>config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            budget.Check();
            var cursor=await transaction.RunAsync(command.Text,command.Parameters).ConfigureAwait(false);
            if(selectedSet!=null)
            {
                var setDecoder=new NeoCqlProjectedSetDecoder<T,TResult>(selectedSet,setLimits,budget.Check);
                while(true)
                {
                    budget.Check();
                    if(!await cursor.FetchAsync().ConfigureAwait(false))break;
                    budget.Check();setDecoder.Add(cursor.Current.Values);
                }
                var setPacket=setDecoder.Complete();budget.Check();
                await transaction.CommitAsync().ConfigureAwait(false);budget.Check();
                return new OASISResult<NeoSelectedResult>(new NeoSelectedResult{SetProof=setPacket}){IsLoaded=true};
            }
            var decoder=new NeoCqlCompositionDecoder<T,TResult>(query,limits,budget.Check,terminal:all != null, cardinality:plan?.RequestsCardinality == true, numeric:plan?.NumericReduction);
            var entityProof=entity ? new NeoCqlEntitySelectedProof((plan?.EntitySource??all?.EntitySource).Options) : null;
            while(true)
            {
                budget.Check();
                if(!await cursor.FetchAsync().ConfigureAwait(false))break;
                budget.Check();
                var record=cursor.Current;
                if(entityProof?.Consume(record.Values)!=true)decoder.Add(record.Values);
            }
            entityProof?.Complete();
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
