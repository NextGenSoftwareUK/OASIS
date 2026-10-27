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

public partial class MongoDBOASIS : ICosmicQueryProjectedAllStorageProvider
{
    private static class CompositionContract
    {
        internal static readonly CosmicProjectionCompositionCapabilities Value = new(
            CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging | CosmicSelectedStageFeatures.Reverse,
            CqlGroupedCapabilities.FilteringCapabilities.Features, CqlGroupedCapabilities.FilteringCapabilities.Keys,
            CqlGroupedCapabilities.OrderingCapabilities.KeyTypes, 32, supportsAll: true, supportsCardinality: true);
    }
    public static CosmicProjectionCompositionCapabilities CqlCompositionCapabilities => CompositionContract.Value;
    public CosmicProjectionCompositionCapabilities CompositionCapabilities => CqlCompositionCapabilities;

    public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T, TResult>(
        CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken = default) where T : IHolon
        => ExecuteSelectedCqlAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var admission = CosmicProjectionCompositionPlanner.AdmitExecution(plan, Core.Enums.ProviderType.MongoDBOASIS,
                CqlCapabilities, CqlProjectionCapabilities, CqlCompositionCapabilities, limits);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, CosmicProjectionCompositionPacket>(admission);
            var command = new MongoCqlTranslator().CompileComposition(plan, limits);
            if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return SelectedCqlFailure<CosmicProjectionCompositionPacket>("CQL_PROVIDER_NOT_READY", "Activate MongoDB before native composition execution.");
            var packet = await repository.ExecuteNativeCqlCompositionAsync(command, plan.Query, limits, cancellationToken).ConfigureAwait(false);
            return new OASISResult<CosmicProjectionCompositionPacket>(packet) { IsLoaded = true };
        });

    public Task<OASISResult<CosmicProjectedAllPacket>> ExecuteCosmicProjectedAllAsync<T, TResult>(
        CosmicProjectedAllPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken = default) where T : IHolon
        => ExecuteSelectedCqlAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var admission = CosmicProjectedAllPlanner.AdmitExecution(plan, Core.Enums.ProviderType.MongoDBOASIS,
                CqlCapabilities, CqlProjectionCapabilities, CqlCompositionCapabilities, limits);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, CosmicProjectedAllPacket>(admission);
            var command = new MongoCqlTranslator().CompileProjectedAll(plan, limits);
            if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return SelectedCqlFailure<CosmicProjectedAllPacket>("CQL_PROVIDER_NOT_READY", "Activate MongoDB before native projected All execution.");
            var packet = await repository.ExecuteNativeCqlProjectedAllAsync(command, plan.Query.Projection, limits, cancellationToken).ConfigureAwait(false);
            return new OASISResult<CosmicProjectedAllPacket>(packet) { IsLoaded = true };
        });

    // Selected composition and its terminals share one provider error boundary
    // and the repository's one snapshot/deadline owner.
    private static async Task<OASISResult<TOutput>> ExecuteSelectedCqlAsync<TOutput>(Func<Task<OASISResult<TOutput>>> invoke)
    {
        try { return await invoke().ConfigureAwait(false); }
        catch (System.IO.InvalidDataException cause) { return SelectedCqlFailure<TOutput>("CQL_DATA_INVALID", cause.Message, cause); }
        catch (OverflowException cause) { return SelectedCqlFailure<TOutput>("CQL_DATA_INVALID", cause.Message, cause); }
        catch (MongoCqlLimitException cause) { return SelectedCqlFailure<TOutput>("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (MongoCqlGraphTimeoutException cause) { return SelectedCqlFailure<TOutput>("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (NotSupportedException cause) { return SelectedCqlFailure<TOutput>("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return SelectedCqlFailure<TOutput>("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (MongoCommandException cause) when (cause.Code == 146 || cause.Code == 292)
        { return SelectedCqlFailure<TOutput>("CQL_QUERY_LIMIT", "Selected execution exceeded its native in-memory budget.", cause); }
        catch (MongoExecutionTimeoutException cause) { return SelectedCqlFailure<TOutput>("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (Exception cause) { return SelectedCqlFailure<TOutput>("CQL_QUERY_FAILED", "MongoDB native selected execution did not complete.", cause); }
    }

    private static OASISResult<TOutput> SelectedCqlFailure<TOutput>(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCode = code, Message = message, Exception = cause };
}
