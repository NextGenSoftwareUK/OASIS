using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicQueryScalarStorageProvider
{
    public static CosmicScalarCapabilities CqlScalarCapabilities => ScalarContract.Value;
    private static class ScalarContract
    {
        internal static readonly CosmicScalarCapabilities Value = new(
            new[] { CosmicScalarOperation.Count, CosmicScalarOperation.LongCount, CosmicScalarOperation.Any },
            new[] { new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, CqlProjectionCapabilities),
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Max, CqlProjectionCapabilities),
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, CqlProjectionCapabilities),
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, CqlProjectionCapabilities) });
    }

    public CosmicScalarCapabilities ScalarCapabilities => CqlScalarCapabilities;

    /// <summary>Native scalar execution reuses the row predicate compiler and bounded SQLite read owner.</summary>
    public Task<OASISResult<TResult>> ExecuteCosmicScalarAsync<T, TResult>(CosmicScalarPlan<T, TResult> plan,
        CancellationToken cancellationToken = default) where T : IHolon
        => ((HolonRepository)_holonRepository).ExecuteCosmicScalarAsync(plan, cancellationToken);
}
