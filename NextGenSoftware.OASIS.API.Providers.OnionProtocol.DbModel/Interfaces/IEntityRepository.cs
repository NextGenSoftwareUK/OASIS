using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Interfaces
{
    public interface IEntityRepository<TE>
    {
        Task<TE> Create(TE instance);

        Task<TE> Get(Guid id);

        Task<TE> Update(TE instance);

        void Delete(TE instance);

        Task<IEnumerable<TE>> GetAll();

        Task<long> Count(TE instance);
    }
}