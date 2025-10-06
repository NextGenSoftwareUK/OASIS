using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces
{
    public interface IRepository<T> //where T : Entity
    {
        Task<T> GetByIdAsync(string id);
        T GetByField(string fieldName, string fieldValue, int version = 0);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task DeleteAsync(Guid id);
        Task DeleteAsync(string providerKey);
        //Task<List<T>> GetListAsync();
        List<T> GetList();
    }
}
