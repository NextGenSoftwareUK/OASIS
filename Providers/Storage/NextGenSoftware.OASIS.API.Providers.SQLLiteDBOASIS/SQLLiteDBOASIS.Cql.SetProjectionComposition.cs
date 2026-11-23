using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
public partial class SQLLiteDBOASIS:ICosmicQuerySetProjectionCompositionStorageProvider
{
    public bool SupportsSetProjectionComposition=>true;
    public Task<OASISResult<CosmicSetProjectionCompositionPacket>> ExecuteCosmicSetProjectionCompositionAsync<T,TResult>(
        CosmicSetProjectionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken=default) where T:IHolon
        =>!IsProviderActivated || _holonRepository is not HolonRepository repository
            ?Task.FromResult(new OASISResult<CosmicSetProjectionCompositionPacket>{IsError=true,ErrorCount=1,
                ErrorCode="CQL_PROVIDER_NOT_READY",Message="Activate SQLite before selected stages after entity sets."})
            :repository.ExecuteCosmicSetProjectionCompositionAsync(plan,limits,cancellationToken);
}
