using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public interface IHolonRepository
    {
        Task<Holon> Add(Holon holon);
        Task<Holon> Update(Holon holon);
        Task<bool> Delete(Guid id, bool softDelete = true);
        Task<Holon> GetHolon(Guid id);
        Task<IEnumerable<Holon>> GetHolons();
    }

}
