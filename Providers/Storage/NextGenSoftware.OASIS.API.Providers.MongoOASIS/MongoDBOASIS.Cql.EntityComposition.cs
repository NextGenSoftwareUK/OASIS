using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICosmicQueryEntityCompositionStorageProvider
{
    public CosmicEntityCompositionCapabilities EntityCompositionCapabilities => new(
        CosmicEntityStageFeatures.Filter | CosmicEntityStageFeatures.Ordering | CosmicEntityStageFeatures.Paging,
        CqlCapabilities);

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicEntityCompositionAsync<T>(CosmicEntityCompositionPlan<T> plan,
        int maximumResults=100, CancellationToken cancellationToken=default) where T:IHolon
        => ExecuteCqlRowsAsync(plan?.Source, maximumResults, cancellationToken, plan);
}
