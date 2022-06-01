using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    // This interface is responsbile for persisting provider wallets (including private keys) to local storage ONLY.
    public interface IOASISLocalStorageProvider //: IOASISStorageProvider
    {
        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWallets();
        public Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsAsync();
        public OASISResult<bool> SaveProviderWallets(Dictionary<ProviderType, List<IProviderWallet>> providerWallets);
        public Task<OASISResult<bool>> SaveProviderWalletsAsync(Dictionary<ProviderType, List<IProviderWallet>> providerWallets);
    }
}