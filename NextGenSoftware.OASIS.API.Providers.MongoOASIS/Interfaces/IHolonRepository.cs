using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces
{
    public interface IHolonRepository
    {
        Holon Add(Holon holon);
        Task<Holon> AddAsync(Holon holon);
        bool Delete(Guid id, bool softDelete = true);
        bool Delete(string providerKey, bool softDelete = true);
        Task<bool> DeleteAsync(Guid id, bool softDelete = true);
        Task<bool> DeleteAsync(string providerKey, bool softDelete = true);
        IEnumerable<Holon> GetAllHolons();
        Task<IEnumerable<Holon>> GetAllHolonsAsync();
        IEnumerable<Holon> GetAllHolonsForParent(Guid id, HolonType holonType);
        IEnumerable<Holon> GetAllHolonsForParent(string providerKey, HolonType holonType);
        Task<IEnumerable<Holon>> GetAllHolonsForParentAsync(Guid id, HolonType holonType);
        Task<IEnumerable<Holon>> GetAllHolonsForParentAsync(string providerKey, HolonType holonType);
        Holon GetHolon(Guid id);
        Holon GetHolon(string providerKey);
        Task<Holon> GetHolonAsync(Guid id);
        Task<Holon> GetHolonAsync(string providerKey);
        Holon Update(Holon holon);
        Task<Holon> UpdateAsync(Holon holon);
    }
}