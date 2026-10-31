using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicQueryJoinStorageProvider
{
    private static class JoinContract
    {
        // Partial-class field declaration order cannot own dependencies between
        // capabilities. Initialize only after the provider's static state exists.
        internal static readonly CosmicJoinCapabilities Value = new(
            new[] { CosmicJoinKind.Inner, CosmicJoinKind.Left }, CqlProjectionCapabilities, CqlProjectionCapabilities);
    }
    public static CosmicJoinCapabilities CqlJoinCapabilities => JoinContract.Value;
    public CosmicJoinCapabilities JoinCapabilities => CqlJoinCapabilities;

    public Task<OASISResult<CosmicJoinPacket>> ExecuteCosmicJoinAsync<TLeft, TRight, TKey, TResult>(
        CosmicJoinPlan<TLeft, TRight, TKey, TResult> plan, CosmicJoinExecutionLimits limits,
        CancellationToken cancellationToken = default) where TLeft : IHolon where TRight : IHolon
    {
        if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
            return Task.FromResult(new OASISResult<CosmicJoinPacket>
            { IsError = true, ErrorCode = "CQL_PROVIDER_NOT_READY", Message = "Activate SQLite before native join execution." });
        return repository.ExecuteCosmicJoinAsync(plan, limits, cancellationToken);
    }
}
