using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

public partial class TRONOASIS
{
    private Task<OASISResult<string>> GetWalletAddressForAvatar(Guid avatarId)
        => WalletHelper.GetWalletAddressForAvatarAsync(WalletManager,
            NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, avatarId, _httpClient);
}
