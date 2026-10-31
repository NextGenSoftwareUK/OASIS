
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces
{
    public interface IHolonRepository : IRepository<IHolon>
    {
        System.Threading.Tasks.Task<IHolon> UpsertPlannedAsync(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite plan);
    }
}
