using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicQueryEntityCountStorageProvider
{
    public bool SupportsEntityCount => true;

    public Task<OASISResult<long>> ExecuteCosmicEntityCountAsync<T>(CosmicEntityCompositionPlan<T> plan,
        CancellationToken cancellationToken=default) where T:IHolon
        => ((HolonRepository)_holonRepository).ExecuteCosmicEntityCountAsync(plan,cancellationToken);

    public CosmicEntityCompositionCapabilities EntityCompositionCapabilities => CqlEntityCompositionCapabilities;
    internal static CosmicEntityCompositionCapabilities CqlEntityCompositionCapabilities => new(
        CosmicEntityStageFeatures.Filter | CosmicEntityStageFeatures.Ordering | CosmicEntityStageFeatures.Paging | CosmicEntityStageFeatures.Reverse,
        SqliteCqlContract.Capabilities);

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicEntityCompositionAsync<T>(CosmicEntityCompositionPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        => ((HolonRepository)_holonRepository).ExecuteCosmicEntityCompositionAsync(plan, maximumResults, cancellationToken);
}
