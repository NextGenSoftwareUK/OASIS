using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicQueryEntityCompositionStorageProvider
{
    public CosmicEntityCompositionCapabilities EntityCompositionCapabilities => new(
        CosmicEntityStageFeatures.Filter | CosmicEntityStageFeatures.Ordering | CosmicEntityStageFeatures.Paging,
        SqliteCqlContract.Capabilities);

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicEntityCompositionAsync<T>(CosmicEntityCompositionPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        => ((HolonRepository)_holonRepository).ExecuteCosmicEntityCompositionAsync(plan, maximumResults, cancellationToken);
}
