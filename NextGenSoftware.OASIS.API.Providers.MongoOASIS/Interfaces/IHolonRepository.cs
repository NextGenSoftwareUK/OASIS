using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces
{
    public interface IHolonRepository
    {
        //TODO: Apply OASISResult pattern to rest of OASIS ASAP! Thanks! :)
        OASISResult<Holon> Add(Holon holon);
        Task<OASISResult<Holon>> AddAsync(Holon holon);
        OASISResult<Holon> Update(Holon holon);
        Task<OASISResult<Holon>> UpdateAsync(Holon holon);
        OASISResult<IHolon> Delete(Guid id, bool softDelete = true);
        OASISResult<IHolon> Delete(string providerKey, bool softDelete = true);
        Task<OASISResult<IHolon>> DeleteAsync(Guid id, bool softDelete = true);
        Task<OASISResult<IHolon>> DeleteAsync(string providerKey, bool softDelete = true);
        IEnumerable<Holon> GetAllHolons(HolonType holonType = HolonType.All);
        Task<IEnumerable<Holon>> GetAllHolonsAsync(HolonType holonType = HolonType.All);
        IEnumerable<Holon> GetAllHolonsForParent(Guid id, HolonType holonType);
        IEnumerable<Holon> GetAllHolonsForParent(string providerKey, HolonType holonType);
        IEnumerable<Holon> GetAllHolonsForParentByCustomKey(string customKey, HolonType holonType);
        IEnumerable<Holon> GetAllHolonsForParentByMetaData(string metaKey, string metaValue, HolonType holonType);
        Task<IEnumerable<Holon>> GetAllHolonsForParentAsync(Guid id, HolonType holonType);
        Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentAsync(string providerKey, HolonType holonType);
        Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentByCustomKeyAsync(string customKey, HolonType holonType);
        Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentByMetaDataAsync(string metaKey, string metaValue, HolonType holonType);
        Holon GetHolon(Guid id);
        Holon GetHolon(string providerKey);
        Holon GetHolonByCustomKey(string customKey);
        Holon GetHolonByMetaData(string metaKey, string metaValue);
        Task<Holon> GetHolonAsync(Guid id);
        Task<Holon> GetHolonAsync(string providerKey);
        Task<Holon> GetHolonByCustomKeyAsync(string customKey);
        Task<Holon> GetHolonByMetaDataAsync(string metaKey, string metaValue);
    }
}