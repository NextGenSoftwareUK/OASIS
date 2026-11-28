using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
public partial class SQLLiteDBOASIS : ICosmicQueryProjectedSetStorageProvider
{
    public CosmicProjectedSetCapabilities ProjectedSetCapabilities=>CqlProjectedSetCapabilities;
    internal static CosmicProjectedSetCapabilities CqlProjectedSetCapabilities=>new(
        CosmicSetQueryFeatures.Union|CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Intersect|CosmicSetQueryFeatures.Except,
        CqlCompositionCapabilities.OrderingDomains.ToArray(),CqlCompositionCapabilities.OrderingDomains.ToArray(),
        CqlCompositionCapabilities,supportsNestedSets:true);
    public Task<OASISResult<CosmicProjectedSetPacket>> ExecuteCosmicProjectedSetAsync<T,TResult>(
        CosmicProjectedSetPlan<T,TResult> plan,CosmicProjectedSetExecutionLimits limits,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        if(!IsProviderActivated || _holonRepository is not HolonRepository repository)
            return Task.FromResult(new OASISResult<CosmicProjectedSetPacket>{IsError=true,
                ErrorCode="CQL_PROVIDER_NOT_READY",Message="Activate SQLite before native selected-value set execution."});
        return repository.ExecuteCosmicProjectedSetAsync(plan,limits,cancellationToken);
    }
}
