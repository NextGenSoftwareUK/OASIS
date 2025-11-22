using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Repositories
{
    public interface IAztecRepository
    {
        Task<IHolon> LoadHolonAsync(Guid id);
        Task<IHolon> LoadHolonByProviderKeyAsync(string providerKey);
        Task<IHolon> SaveHolonAsync(IHolon holon);
    }
}

