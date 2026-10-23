using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicQueryProjectionStorageProvider
{
    public static CosmicProjectionCapabilities CqlProjectionCapabilities { get; } = new(
        SqliteCqlContract.Fields.Values.Select(field => new CosmicProjectionFieldCapability(field.Name, field.ValueType)).ToArray(),
        SqliteCqlContract.MetadataTypes.Keys.ToArray());
    public CosmicProjectionCapabilities ProjectionCapabilities => CqlProjectionCapabilities;

    public Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicProjectionAsync<T, TResult>(
        CosmicProjectionPlan<T, TResult> plan, int maximumResults = 100,
        int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken = default) where T : IHolon
        => ((HolonRepository)_holonRepository).ExecuteCosmicProjectionAsync(plan, maximumResults, maximumPayloadBytes, cancellationToken);
}
