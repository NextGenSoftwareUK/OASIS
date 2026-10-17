using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Repositories
{
    public class AztecRepository : IAztecRepository
    {
        private readonly AztecAPIClient _apiClient;

        public AztecRepository(AztecAPIClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<IHolon> LoadHolonAsync(Guid id)
        {
            var result = await _apiClient.GetAsync<Holon>($"/api/holons/{id}");
            return result.IsError ? null : result.Result;
        }

        public async Task<IHolon> LoadHolonByProviderKeyAsync(string providerKey)
        {
            var result = await _apiClient.GetAsync<Holon>($"/api/holons/by-key/{Uri.EscapeDataString(providerKey)}");
            return result.IsError ? null : result.Result;
        }

        public async Task<IHolon> SaveHolonAsync(IHolon holon)
        {
            if (holon.ProviderUniqueStorageKey == null)
                holon.ProviderUniqueStorageKey = new System.Collections.Generic.Dictionary<Core.Enums.ProviderType, string>();

            var result = await _apiClient.PostAsync<Holon>("/api/holons", holon);
            if (!result.IsError && result.Result != null)
            {
                if (!string.IsNullOrEmpty(result.Result.ProviderUniqueStorageKey?[Core.Enums.ProviderType.AztecOASIS]))
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AztecOASIS] =
                        result.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.AztecOASIS];
                return result.Result;
            }

            return holon;
        }
    }
}
