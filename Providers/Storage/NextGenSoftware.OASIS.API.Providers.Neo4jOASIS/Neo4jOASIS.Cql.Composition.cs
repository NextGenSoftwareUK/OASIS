using System;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicQueryProjectionCompositionStorageProvider
{
    private static class CompositionContract
    {
        internal static readonly CosmicProjectionCompositionCapabilities Value = new(
            CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging,
            CqlGroupedCapabilities.FilteringCapabilities.Features, CqlGroupedCapabilities.FilteringCapabilities.Keys,
            CqlGroupedCapabilities.OrderingCapabilities.KeyTypes);
    }
    public static CosmicProjectionCompositionCapabilities CqlCompositionCapabilities => CompositionContract.Value;
    public CosmicProjectionCompositionCapabilities CompositionCapabilities => CqlCompositionCapabilities;

    public async Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T,TResult>(
        CosmicProjectionCompositionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        NeoCqlReadBudget readBudget=null;
        try
        {
            var admitted=CosmicProjectionCompositionPlanner.AdmitExecution(plan,Core.Enums.ProviderType.Neo4jOASIS,
                CqlCapabilities,CqlProjectionCapabilities,CqlCompositionCapabilities,limits);
            if(admitted.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,CosmicProjectionCompositionPacket>(admitted);
            var command=CompileCqlComposition(plan,limits);
            using var budget=readBudget=new NeoCqlReadBudget(limits.MaximumPayloadBytes,cancellationToken);
            budget.Check();
            await using var readOwner=await LeaseCqlReadAsync(budget.Token).ConfigureAwait(false);
            using var session=readOwner.Driver.AsyncSession(config=>config.WithDefaultAccessMode(AccessMode.Read).WithFetchSize(1));
            using var transaction=await session.BeginTransactionAsync(config=>config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            budget.Check();
            var cursor=await transaction.RunAsync(command.Text,command.Parameters).ConfigureAwait(false);
            var decoder=new NeoCqlCompositionDecoder<T,TResult>(plan.Query,limits,budget.Check);
            while(true)
            {
                budget.Check();
                if(!await cursor.FetchAsync().ConfigureAwait(false))break;
                budget.Check();decoder.Add(cursor.Current.Values);
            }
            budget.Check();var packet=decoder.Complete();
            await transaction.CommitAsync().ConfigureAwait(false);budget.Check();
            return new OASISResult<CosmicProjectionCompositionPacket>(packet){IsLoaded=true};
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

    private static OASISResult<CosmicProjectionCompositionPacket> CompositionCqlFailure(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCode=code,Message=message,Exception=cause};
}
