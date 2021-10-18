using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.MongoDb.Interface
{
    public interface IEntityProvider<TEntity>
    {
        Task<IList<TEntity>> GetAllAsync();

        IList<TEntity> GetAll();

        Task<IList<TEntity>> GetAllAsync(FilterDefinition<TEntity> filters);

        IList<TEntity> GetAll(FilterDefinition<TEntity> filters);

        Task<TEntity> GetAsync(Guid objectId);

        TEntity Get(Guid objectId);

        Task<TEntity> GetAsync(FilterDefinition<TEntity> filters);

        TEntity Get(FilterDefinition<TEntity> filters);

        Task<IEnumerable<TEntity>> GetAsync(FilterDefinition<TEntity> filters, FindOptions<TEntity> options);

        IEnumerable<TEntity> Get(FilterDefinition<TEntity> filters, FindOptions<TEntity> options);

        Task<TEntity> InsertAsync(TEntity entityObject);

        TEntity Insert(TEntity entityObject);

        Task<TEntity> UpdateAsync(TEntity entityObject);

        TEntity Update(TEntity entityObject);

        Task<TEntity> UpsertAsync(TEntity entityObject);

        TEntity Upsert(TEntity entityObject);

        Task<DeleteResult> RemoveAsync(Guid objectId);

        DeleteResult Remove(Guid objectId);

        Task<DeleteResult> RemoveAsync(FilterDefinition<TEntity> filters);

        DeleteResult Remove(FilterDefinition<TEntity> filters);

        Task<DeleteResult> RemoveAsync(TEntity objectId);

        DeleteResult Remove(TEntity objectId);

        Task<long> CountAsync();

        long Count();

        void RemoveCollection();
    }
}