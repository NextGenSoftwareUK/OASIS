using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Repositories
{
    public class AztecRepository : IAztecRepository
    {
        public async Task<IHolon> LoadHolonAsync(Guid id)
        {
            // Placeholder implementation - in production would fetch from Aztec state or metadata store
            return await Task.FromResult(new Holon
            {
                Id = id,
                Name = "Aztec Holon",
                Description = "Holon synchronized with Aztec private state",
                HolonType = Core.Enums.HolonType.All
            });
        }

        public async Task<IHolon> LoadHolonByProviderKeyAsync(string providerKey)
        {
            return await Task.FromResult(new Holon
            {
                Id = Guid.NewGuid(),
                Name = "Aztec Holon",
                Description = $"Holon for Aztec key {providerKey}",
                HolonType = Core.Enums.HolonType.All,
                ProviderUniqueStorageKey = new System.Collections.Generic.Dictionary<Core.Enums.ProviderType, string>
                {
                    { Core.Enums.ProviderType.AztecOASIS, providerKey }
                }
            });
        }

        public async Task<IHolon> SaveHolonAsync(IHolon holon)
        {
            if (holon.ProviderUniqueStorageKey == null)
            {
                holon.ProviderUniqueStorageKey = new System.Collections.Generic.Dictionary<Core.Enums.ProviderType, string>();
            }

            // Preserve any existing real provider key (Aztec tx hash / note ID set by MintTokenAsync
            // or GenerateKeyPairAsync).  Only fall back to a UUID placeholder if no key exists yet,
            // which means SaveHolonAsync is being called before an on-chain operation — in that case
            // the caller should update the key after the tx completes.
            if (!holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.AztecOASIS)
                || string.IsNullOrWhiteSpace(holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AztecOASIS]))
            {
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AztecOASIS] =
                    $"pending:{Guid.NewGuid():N}";
            }

            return await Task.FromResult(holon);
        }
    }
}

