using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Repositories
{
    public interface IZcashRepository
    {
        Task<IHolon> LoadHolonAsync(Guid id);
        Task<IHolon> LoadHolonByProviderKeyAsync(string providerKey);
        Task<IHolon> SaveHolonAsync(IHolon holon);
        Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true);
        Task<Guid?> GetAvatarIdByUsernameAsync(string username);
        Task<Guid?> GetAvatarIdByEmailAsync(string email);
        Task<bool> SaveAvatarIndexAsync(string indexType, string indexValue, Guid avatarId);
        Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid parentId);
        Task<IEnumerable<IHolon>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue);
    }
}

