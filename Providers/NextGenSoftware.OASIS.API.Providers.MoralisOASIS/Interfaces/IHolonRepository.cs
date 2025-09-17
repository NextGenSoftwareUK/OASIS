using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.MoralisOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS.Interfaces
{
    public interface IHolonRepository
    {
        Holon Add(Holon holon);
        Task<Holon> AddAsync(Holon holon);
        bool Delete(Guid id, bool softDelete = true);
        bool Delete(string providerKey, bool softDelete = true);
        Task<bool> DeleteAsync(Guid id, bool softDelete = true);
        Task<bool> DeleteAsync(string providerKey, bool softDelete = true);
        IEnumerable<Holon> GetAllHolons(HolonType holonType = HolonType.All);
        Task<IEnumerable<Holon>> GetAllHolonsAsync(HolonType holonType = HolonType.All);
        IEnumerable<Holon> GetAllHolonsForParent(Guid id, HolonType holonType);
        IEnumerable<Holon> GetAllHolonsForParent(string providerKey, HolonType holonType);
        Task<IEnumerable<Holon>> GetAllHolonsForParentAsync(Guid id, HolonType holonType);
        Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentAsync(string providerKey, HolonType holonType);
        Holon GetHolon(Guid id);
        Holon GetHolon(string providerKey);
        Task<Holon> GetHolonAsync(Guid id);
        Task<Holon> GetHolonAsync(string providerKey);
        Holon Update(Holon holon);
        Task<Holon> UpdateAsync(Holon holon);
    }
}