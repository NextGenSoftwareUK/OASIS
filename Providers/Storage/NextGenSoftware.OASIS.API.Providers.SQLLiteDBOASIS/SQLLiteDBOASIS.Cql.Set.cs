using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
public partial class SQLLiteDBOASIS:ICosmicSetQueryStorageProvider
{
    public CosmicSetQueryCapabilities SetQueryCapabilities=>CqlSetCapabilities;
    internal static CosmicSetQueryCapabilities CqlSetCapabilities=>new(
        CosmicSetQueryFeatures.Union|CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Intersect|CosmicSetQueryFeatures.Except,
        CqlEntityCompositionCapabilities);
    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicSetQueryAsync<T>(
        CosmicSetQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        =>((HolonRepository)_holonRepository).ExecuteCosmicSetQueryAsync(plan,maximumResults,cancellationToken);
}
