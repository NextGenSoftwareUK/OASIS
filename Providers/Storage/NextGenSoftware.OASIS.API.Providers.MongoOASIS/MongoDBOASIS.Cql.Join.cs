using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICosmicQueryJoinStorageProvider
{
    private static class JoinContract
    {
        internal static readonly CosmicJoinCapabilities Value = new(
            new[] { CosmicJoinKind.Inner, CosmicJoinKind.Left }, CqlProjectionCapabilities, CqlProjectionCapabilities);
    }
    public static CosmicJoinCapabilities CqlJoinCapabilities => JoinContract.Value;
    public CosmicJoinCapabilities JoinCapabilities => CqlJoinCapabilities;

    public async Task<OASISResult<CosmicJoinPacket>> ExecuteCosmicJoinAsync<TLeft, TRight, TKey, TResult>(
        CosmicJoinPlan<TLeft, TRight, TKey, TResult> plan, CosmicJoinExecutionLimits limits,
        CancellationToken cancellationToken = default) where TLeft : IHolon where TRight : IHolon
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var admission = CosmicJoinPlanner.AdmitExecution(plan, Core.Enums.ProviderType.MongoDBOASIS,
                CqlCapabilities, CqlJoinCapabilities, limits);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, CosmicJoinPacket>(admission);
            var command = new MongoCqlTranslator().CompileJoin(plan.Query, limits);
            if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return JoinCqlFailure("CQL_PROVIDER_NOT_READY", "Activate MongoDB before native join execution.");
            var packet = await repository.ExecuteNativeCqlJoinAsync(command, plan.Query, limits, cancellationToken).ConfigureAwait(false);
            return new OASISResult<CosmicJoinPacket>(packet) { IsLoaded = true };
        }
        catch (System.IO.InvalidDataException cause) { return JoinCqlFailure("CQL_DATA_INVALID", cause.Message, cause); }
        catch (OverflowException cause) { return JoinCqlFailure("CQL_DATA_INVALID", cause.Message, cause); }
        catch (MongoCqlLimitException cause) { return JoinCqlFailure("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (MongoCqlGraphTimeoutException cause) { return JoinCqlFailure("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (NotSupportedException cause) { return JoinCqlFailure("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return JoinCqlFailure("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (MongoCommandException cause) when (cause.Code == 146 || cause.Code == 292)
        { return JoinCqlFailure("CQL_QUERY_LIMIT", "Join exceeded its native in-memory budget.", cause); }
        catch (MongoExecutionTimeoutException cause) { return JoinCqlFailure("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (Exception cause) { return JoinCqlFailure("CQL_QUERY_FAILED", "MongoDB native join did not complete.", cause); }
    }

    private static OASISResult<CosmicJoinPacket> JoinCqlFailure(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCode = code, Message = message, Exception = cause };
}
