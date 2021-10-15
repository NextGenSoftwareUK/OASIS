using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Interfaces;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface
{
    public interface IHolonRepository : IEntityRepository<Holon>
    {
        OASISResult<Holon> Add(Holon holon);

        Task<OASISResult<Holon>> AddAsync(Holon holon);

        OASISResult<Holon> Update(Holon holon);

        Task<OASISResult<Holon>> UpdateAsync(Holon holon);

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
    }
}