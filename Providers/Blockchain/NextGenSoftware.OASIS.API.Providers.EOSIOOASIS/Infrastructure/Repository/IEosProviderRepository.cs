using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository
{
    /// <summary>
    ///     Contains CRUD methods for OASIS Avatar, AvatarDetail, Holon entities to store them into EOS-Blockchain.
    /// </summary>
    /// <typeparam name="TEntity">Entity</typeparam>
    public interface IEosProviderRepository<TEntity> : IDisposable
        where TEntity : new()
    {
        /// <summary>
        ///     Creates entity in EOS-blockchain data storage
        /// </summary>
        /// <param name="entity">Entity that need to be created</param>
        /// <returns>Task</returns>
        public Task Create(TEntity entity);

        /// <summary>
        ///     Updates properties of entity that stores in EOS-blockchain
        /// </summary>
        /// <param name="entity">Updated entity</param>
        /// <param name="id">Id of entity that need to be updated</param>
        /// <returns>Task</returns>
        public Task Update(TEntity entity, Guid id);

        /// <summary>
        ///     Gets exist entity from EOS-blockchain storage
        /// </summary>
        /// <param name="id">Entity that need to be returned</param>
        /// <returns>Exist entity</returns>
        public Task<TEntity> Read(Guid id);

        /// <summary>
        ///     Returns all entities from EOS-blockchain storage
        /// </summary>
        /// <returns>Readonly list of entity</returns>
        public Task<ImmutableArray<TEntity>> ReadAll();

        public Task<ImmutableArray<HolonDto>> ReadAllByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All);

        public Task<ImmutableArray<HolonDto>> ReadAllByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All);

        /// <summary>
        ///     Sets IsDeleted property to true of specified entity, by Id
        /// </summary>
        /// <param name="id">Id of entity that need to be soft-deleted</param>
        /// <returns>Task</returns>
        public Task DeleteSoft(Guid id);

        /// <summary>
        ///     Deletes entity specified by Id from EOS-blockchain
        /// </summary>
        /// <param name="id">Id of entity that need to be deleted</param>
        /// <returns>Task</returns>
        public Task DeleteHard(Guid id);
    }
}