using System;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicQueryJoinStorageProvider
{
    private static class JoinContract
    {
        internal static readonly CosmicJoinCapabilities Value = new(
            new[] { CosmicJoinKind.Inner, CosmicJoinKind.Left }, CqlProjectionCapabilities, CqlProjectionCapabilities);
    }
    public static CosmicJoinCapabilities CqlJoinCapabilities => JoinContract.Value;
    public CosmicJoinCapabilities JoinCapabilities => CqlJoinCapabilities;

    public async Task<OASISResult<CosmicJoinPacket>> ExecuteCosmicJoinAsync<TLeft,TRight,TKey,TResult>(
        CosmicJoinPlan<TLeft,TRight,TKey,TResult> plan,CosmicJoinExecutionLimits limits,
        CancellationToken cancellationToken=default) where TLeft:IHolon where TRight:IHolon
    {
        NeoCqlReadBudget readBudget=null;
        try
        {
            var admitted=CosmicJoinPlanner.AdmitExecution(plan,Core.Enums.ProviderType.Neo4jOASIS,
                CqlCapabilities,CqlJoinCapabilities,limits);
            if(admitted.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,CosmicJoinPacket>(admitted);
            var command=CompileCqlJoin(plan.Query,limits);
            using var budget=readBudget=new NeoCqlReadBudget(limits.MaximumPayloadBytes,cancellationToken);
            budget.Check();
            await using var readOwner=await LeaseCqlReadAsync(budget.Token).ConfigureAwait(false);
            using var session=readOwner.Driver.AsyncSession(config=>config.WithDefaultAccessMode(AccessMode.Read).WithFetchSize(1));
            using var transaction=await session.BeginTransactionAsync(config=>config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            budget.Check();
            var cursor=await transaction.RunAsync(command.Text,command.Parameters).ConfigureAwait(false);
            var decoder=new NeoCqlJoinDecoder<TLeft,TRight,TKey,TResult>(plan.Query,limits,budget.Check);
            while(true)
            {
                budget.Check();
                if(!await cursor.FetchAsync().ConfigureAwait(false))break;
                budget.Check();decoder.Add(cursor.Current.Values);
            }
            budget.Check();var packet=decoder.Complete();
            await transaction.CommitAsync().ConfigureAwait(false);budget.Check();
            return new OASISResult<CosmicJoinPacket>(packet){IsLoaded=true};
        }
        catch(Exception cause) when(cancellationToken.IsCancellationRequested)
        { return JoinCqlFailure("CQL_QUERY_CANCELLED","Neo4j join was cancelled.",cause); }
        catch(Exception cause) when(readBudget?.IsTimedOut==true)
        { return JoinCqlFailure("CQL_QUERY_TIMEOUT","Neo4j join exceeded its cumulative deadline.",cause); }
        catch(NeoCqlNotReadyException cause) { return JoinCqlFailure("CQL_PROVIDER_NOT_READY",cause.Message,cause); }
        catch(NeoCqlLimitException cause) { return JoinCqlFailure("CQL_QUERY_LIMIT",cause.Message,cause); }
        catch(NeoCqlTimeoutException cause) { return JoinCqlFailure("CQL_QUERY_TIMEOUT",cause.Message,cause); }
        catch(Neo4jException cause) when(cause.Code=="Neo.ClientError.Transaction.TransactionTimedOut"
            || cause.Code=="Neo.TransientError.Transaction.TransactionTimedOut" || cause.Code=="Neo.ClientError.Transaction.TransactionTimedOutClientConfiguration")
        { return JoinCqlFailure("CQL_QUERY_TIMEOUT",cause.Message,cause); }
        catch(OperationCanceledException cause) { return JoinCqlFailure("CQL_QUERY_CANCELLED",cause.Message,cause); }
        catch(NotSupportedException cause) { return JoinCqlFailure("CQL_CAPABILITY_UNSUPPORTED",cause.Message,cause); }
        catch(OverflowException cause) { return JoinCqlFailure("CQL_DATA_INVALID",cause.Message,cause); }
        catch(System.IO.InvalidDataException cause) { return JoinCqlFailure("CQL_DATA_INVALID",cause.Message,cause); }
        catch(Exception cause) { return JoinCqlFailure("CQL_PROVIDER_ERROR",cause.Message,cause); }
    }

    private static OASISResult<CosmicJoinPacket> JoinCqlFailure(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCode=code,Message=message,Exception=cause};
}
