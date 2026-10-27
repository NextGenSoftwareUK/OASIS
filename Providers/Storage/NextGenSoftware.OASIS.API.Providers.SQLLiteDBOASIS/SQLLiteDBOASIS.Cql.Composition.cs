using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicQueryProjectedAllStorageProvider
{
    private static class CompositionContract
    {
        internal static readonly CosmicProjectionCompositionCapabilities Value = new(
            CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging,
            CqlGroupedCapabilities.FilteringCapabilities.Features, CqlGroupedCapabilities.FilteringCapabilities.Keys,
            CqlGroupedCapabilities.OrderingCapabilities.KeyTypes, 32, supportsAll: true);
    }
    public static CosmicProjectionCompositionCapabilities CqlCompositionCapabilities => CompositionContract.Value;
    public CosmicProjectionCompositionCapabilities CompositionCapabilities => CqlCompositionCapabilities;
    public Task<OASISResult<CosmicProjectedAllPacket>> ExecuteCosmicProjectedAllAsync<T, TResult>(
        CosmicProjectedAllPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
            return Task.FromResult(new OASISResult<CosmicProjectedAllPacket>
            { IsError = true, ErrorCode = "CQL_PROVIDER_NOT_READY", Message = "Activate SQLite before projected All execution." });
        return repository.ExecuteCosmicProjectedAllAsync(plan, limits, cancellationToken);
    }
    public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T, TResult>(
        CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
            return Task.FromResult(new OASISResult<CosmicProjectionCompositionPacket>
            { IsError = true, ErrorCode = "CQL_PROVIDER_NOT_READY", Message = "Activate SQLite before native composition execution." });
        return repository.ExecuteCosmicProjectionCompositionAsync(plan, limits, cancellationToken);
    }
}
