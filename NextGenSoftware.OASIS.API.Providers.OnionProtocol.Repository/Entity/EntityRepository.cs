using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Interfaces;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.MongoDb.Interface;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Entity
{
    public abstract class EntityRepository<TE, MDE> : IEntityRepository<TE>
        where TE : IEntity
        where MDE : IEntityProvider<TE>
    {
        protected MDE _provider;

        protected EntityRepository(MDE provider)
        {
            this._provider = provider;
        }

        public async Task<long> Count(TE instance)
        {
            return await this._provider.CountAsync();
        }

        public async Task<TE> Create(TE instance)
        {
            return await this._provider.InsertAsync(instance);
        }

        public void Delete(TE instance, bool softDelete = true)
        {
            this._provider.Remove(instance.Id);
        }

        public void Delete(TE instance)
        {
            throw new NotImplementedException();
        }

        public async Task<TE> Get(Guid id)
        {
            return await this._provider.GetAsync(id);
        }

        public async Task<IEnumerable<TE>> Get(Expression<Func<TE, bool>> expression)
        {
            return await this._provider.GetAllAsync(expression);
        }

        public async Task<IEnumerable<TE>> GetAll()
        {
            return await this._provider.GetAllAsync();
        }

        public async Task<TE> Update(TE instance)
        {
            return await this._provider.UpdateAsync(instance);
        }
    }
}