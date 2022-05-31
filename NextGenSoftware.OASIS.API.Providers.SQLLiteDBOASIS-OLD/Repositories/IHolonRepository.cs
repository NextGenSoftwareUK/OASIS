using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Repositories{

    public interface IHolonRepository
    {
        OASISResult<IHolon> Add(IHolon holon);
        Task<OASISResult<IHolon>> AddAsync(IHolon holon);
        OASISResult<IHolon> Update(IHolon holon);
        Task<OASISResult<IHolon>> UpdateAsync(IHolon holon);
        OASISResult<bool> Delete(Guid id, bool softDelete = true);
        OASISResult<bool> Delete(string providerKey, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAsync(Guid id, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAsync(string providerKey, bool softDelete = true);
        OASISResult<IEnumerable<Holon>> GetAllHolons(HolonType holonType = HolonType.All);
        Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsAsync(HolonType holonType = HolonType.All);
        OASISResult<IEnumerable<Holon>> GetAllHolonsForParent(Guid id, HolonType holonType);
        OASISResult<IEnumerable<Holon>> GetAllHolonsForParent(string providerKey, HolonType holonType);
        Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentAsync(Guid id, HolonType holonType);
        Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentAsync(string providerKey, HolonType holonType);
        OASISResult<Holon> GetHolon(Guid id);
        OASISResult<Holon> GetHolon(string providerKey);
        Task<OASISResult<Holon>> GetHolonAsync(Guid id);
        Task<OASISResult<Holon>> GetHolonAsync(string providerKey);
    }
}