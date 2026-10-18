using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS : IOASISStorageProvider
{
    private Task<OASISResult<IEnumerable<IAvatar>>> LoadCredentialAvatarsAsync(int version)
    {
        if(version!=0) throw new NotSupportedException("Ethereum credential collection history is not implemented; only version 0 is supported.");
        return LoadAllAvatarsAsync(version);
    }
    public Task<OASISResult<IAvatar>> LoadAvatarByPublicKeyAsync(string key,int version=0)
        => StoredAvatarWalletSelector.SelectAsync(key,false,version,LoadCredentialAvatarsAsync);
    public OASISResult<IAvatar> LoadAvatarByPublicKey(string key,int version=0)
        => LoadAvatarByPublicKeyAsync(key,version).GetAwaiter().GetResult();
    public Task<OASISResult<IAvatar>> LoadAvatarByPrivateKeyAsync(string key,int version=0)
        => StoredAvatarWalletSelector.SelectAsync(key,true,version,LoadCredentialAvatarsAsync);
    public OASISResult<IAvatar> LoadAvatarByPrivateKey(string key,int version=0)
        => LoadAvatarByPrivateKeyAsync(key,version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string token,int version=0)
        => StoredAvatarWalletSelector.SelectMatchingAsync(token,version,LoadCredentialAvatarsAsync,avatar=>string.Equals(avatar.VerificationToken,token,StringComparison.Ordinal));
    public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string token,int version=0)
        => LoadAvatarByVerificationTokenAsync(token,version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string token,int version=0)
        => StoredAvatarWalletSelector.SelectMatchingAsync(token,version,LoadCredentialAvatarsAsync,avatar=>string.Equals(avatar.ResetToken,token,StringComparison.Ordinal));
    public override OASISResult<IAvatar> LoadAvatarByResetToken(string token,int version=0)
        => LoadAvatarByResetTokenAsync(token,version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string token,int version=0)
        => StoredAvatarWalletSelector.SelectMatchingAsync(token,version,LoadCredentialAvatarsAsync,avatar=>avatar.RefreshTokens?.Any(value=>value!=null && string.Equals(value.Token,token,StringComparison.Ordinal))==true);
    public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string token,int version=0)
        => LoadAvatarByRefreshTokenAsync(token,version).GetAwaiter().GetResult();
}
