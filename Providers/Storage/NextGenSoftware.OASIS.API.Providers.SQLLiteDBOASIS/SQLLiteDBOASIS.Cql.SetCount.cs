using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
public partial class SQLLiteDBOASIS:ICosmicSetCountQueryStorageProvider
{
    public bool SupportsSetCount=>true;
    public Task<OASISResult<long>> ExecuteCosmicSetCountAsync<T>(CosmicSetQueryPlan<T> plan,
        CancellationToken cancellationToken=default) where T:IHolon
        =>((HolonRepository)_holonRepository).ExecuteCosmicSetCountAsync(plan,cancellationToken);
}
