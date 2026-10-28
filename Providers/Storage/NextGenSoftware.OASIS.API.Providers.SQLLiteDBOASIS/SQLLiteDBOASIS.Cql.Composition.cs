using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicQueryProjectedAllStorageProvider, ICosmicQueryEntityProjectionCompositionStorageProvider
{
    public bool SupportsEntityProjectionComposition => true;
    public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicEntityProjectionCompositionAsync<T,TResult>(
        CosmicProjectionCompositionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        if(!IsProviderActivated || _holonRepository is not HolonRepository repository)
            return Task.FromResult(new OASISResult<CosmicProjectionCompositionPacket>{IsError=true,
                ErrorCode="CQL_PROVIDER_NOT_READY",Message="Activate SQLite before entity-selected composition execution."});
        var admitted=CosmicProjectionCompositionPlanner.AdmitEntityExecution(plan,this,limits);
        return admitted.IsError
            ? Task.FromResult(new OASISResult<CosmicProjectionCompositionPacket>{IsError=true,ErrorCode=admitted.ErrorCode,Message=admitted.Message,Exception=admitted.Exception})
            : repository.ExecuteCosmicEntityProjectionCompositionAsync(plan,limits,cancellationToken);
    }

    private static class SelectedNumericContract
    {
        private static readonly System.Type[] Domains = { typeof(int), typeof(int?), typeof(long), typeof(long?),
            typeof(ulong), typeof(ulong?), typeof(decimal), typeof(decimal?), typeof(float), typeof(float?), typeof(double), typeof(double?) };
        internal static readonly CosmicScalarCapabilities Value = new(System.Array.Empty<CosmicScalarOperation>(), new[]
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
