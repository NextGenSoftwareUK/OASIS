using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Service.Entity
{
    public class EntityService<TE> : IEntityService<TE>
    {
        private IEntityRepository<TE> _entityRepository;

        public EntityService(IEntityRepository<TE> entityRepository)
        {
            this._entityRepository = entityRepository;
        }

        public async Task<long> Count(TE instance)
        {
            return await this._entityRepository.Count(instance);
        }

        public async Task<TE> Create(TE instance)
        {
            return await this._entityRepository.Create(instance);
        }

        public void Delete(TE instance)
        {
            this._entityRepository.Delete(instance);
        }

        public async Task<TE> Get(Guid id)
        {
            return await this._entityRepository.Get(id);
        }

        public async Task<IEnumerable<TE>> GetAll()
        {
            return await this._entityRepository.GetAll();
        }

        public async Task<TE> Update(TE instance)
        {
            return await this._entityRepository.Update(instance);
        }
    }
}