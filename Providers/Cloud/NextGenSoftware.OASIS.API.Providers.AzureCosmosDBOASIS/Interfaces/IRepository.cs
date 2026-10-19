using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces
{
    public interface IRepository<T> //where T : Entity
    {
        Task<T> GetByIdAsync(string id);
        Task<T> GetByIdAsync(string id, int version);
        T GetByField(string fieldName, string fieldValue, int version = 0);
        Task<T> AddAsync(T entity);
        Task<T> UpsertAsync(T entity);
        Task<T> ReplaceIfUnchangedAsync(T expected,T replacement);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task DeleteAsync(Guid id);
        Task DeleteAsync(string providerKey);
        //Task<List<T>> GetListAsync();
        List<T> GetList();
        List<T> GetList(int version);
    }
}
