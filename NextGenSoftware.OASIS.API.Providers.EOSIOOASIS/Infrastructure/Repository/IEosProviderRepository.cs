using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository
{
    /// <summary>
    /// Contains CRUD methods for OASIS Avatar, AvatarDetail, Holon entities to store them into EOS-Blockchain.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IEosProviderRepository<TEntity> : IDisposable
        where TEntity : new()
    {
        /// <summary>
        /// Creates entity in EOS-blockchain data storage
        /// </summary>
        /// <param name="entity">Entity that need to be created</param>
        /// <returns>Task</returns>
        public Task Create(TEntity entity);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task Update(TEntity entity, Guid id);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<TEntity> Read(Guid id);
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<ImmutableArray<TEntity>> ReadAll();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task DeleteSoft(Guid id);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task DeleteHard(Guid id);
    }
}