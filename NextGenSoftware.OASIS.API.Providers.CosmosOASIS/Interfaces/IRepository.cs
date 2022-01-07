using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Entites;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces
{
    public interface IRepository<T> where T : Entity
    {
        Task<T> GetByIdAsync(string id);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
    }
}
