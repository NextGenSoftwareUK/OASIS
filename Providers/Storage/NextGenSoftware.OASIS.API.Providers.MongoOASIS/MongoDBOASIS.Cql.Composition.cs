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

public partial class MongoDBOASIS : ICosmicQueryProjectionCompositionStorageProvider
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

    public async Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T, TResult>(
        CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var admission = CosmicProjectionCompositionPlanner.AdmitExecution(plan, Core.Enums.ProviderType.MongoDBOASIS,
                CqlCapabilities, CqlProjectionCapabilities, CqlCompositionCapabilities, limits);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, CosmicProjectionCompositionPacket>(admission);
            var command = new MongoCqlTranslator().CompileComposition(plan, limits);
            if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return CompositionCqlFailure("CQL_PROVIDER_NOT_READY", "Activate MongoDB before native composition execution.");
            var packet = await repository.ExecuteNativeCqlCompositionAsync(command, plan.Query, limits, cancellationToken).ConfigureAwait(false);
            return new OASISResult<CosmicProjectionCompositionPacket>(packet) { IsLoaded = true };
        }
        catch (System.IO.InvalidDataException cause) { return CompositionCqlFailure("CQL_DATA_INVALID", cause.Message, cause); }
        catch (OverflowException cause) { return CompositionCqlFailure("CQL_DATA_INVALID", cause.Message, cause); }
        catch (MongoCqlLimitException cause) { return CompositionCqlFailure("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (MongoCqlGraphTimeoutException cause) { return CompositionCqlFailure("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (NotSupportedException cause) { return CompositionCqlFailure("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return CompositionCqlFailure("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (MongoCommandException cause) when (cause.Code == 146 || cause.Code == 292)
        { return CompositionCqlFailure("CQL_QUERY_LIMIT", "Composition exceeded its native in-memory budget.", cause); }
        catch (MongoExecutionTimeoutException cause) { return CompositionCqlFailure("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (Exception cause) { return CompositionCqlFailure("CQL_QUERY_FAILED", "MongoDB native composition did not complete.", cause); }
    }

    private static OASISResult<CosmicProjectionCompositionPacket> CompositionCqlFailure(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCode = code, Message = message, Exception = cause };
}
