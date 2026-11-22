using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
public partial class MongoDBOASIS:ICosmicSetQueryStorageProvider
{
    public CosmicSetQueryCapabilities SetQueryCapabilities=>new(
        CosmicSetQueryFeatures.Union|CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Intersect|CosmicSetQueryFeatures.Except,
        EntityCompositionCapabilities);
    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicSetQueryAsync<T>(
        CosmicSetQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        =>ExecuteCqlRowsAsync(plan?.Left,maximumResults,cancellationToken,set:plan);
}
