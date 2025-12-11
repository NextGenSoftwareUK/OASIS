using System;
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
    }
}

