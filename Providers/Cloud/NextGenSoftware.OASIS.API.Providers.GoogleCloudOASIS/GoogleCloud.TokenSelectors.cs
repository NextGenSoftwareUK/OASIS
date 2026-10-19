using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    public override Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string token,int version=0)
        => ReadCanonicalAvatarSelectorAsync(token,avatar=>string.Equals(avatar.VerificationToken,token,StringComparison.Ordinal),version,credentialsLatestOnly:true);
    public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string token,int version=0)
        => LoadAvatarByVerificationTokenAsync(token,version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string token,int version=0)
        => ReadCanonicalAvatarSelectorAsync(token,avatar=>string.Equals(avatar.ResetToken,token,StringComparison.Ordinal),version,credentialsLatestOnly:true);
    public override OASISResult<IAvatar> LoadAvatarByResetToken(string token,int version=0)
        => LoadAvatarByResetTokenAsync(token,version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string token,int version=0)
        => ReadCanonicalAvatarSelectorAsync(token,avatar=>avatar.RefreshTokens?.Any(value=>value!=null && string.Equals(value.Token,token,StringComparison.Ordinal))==true,version,credentialsLatestOnly:true);
    public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string token,int version=0)
        => LoadAvatarByRefreshTokenAsync(token,version).GetAwaiter().GetResult();

    public Task<OASISResult<IAvatar>> LoadAvatarByPublicKeyAsync(string publicKey,int version=0)
        => ReadCanonicalAvatarSelectorAsync(publicKey,avatar=>avatar.ProviderWallets?.Values.Any(wallets=>
            wallets?.Any(wallet=>wallet!=null && string.Equals(wallet.PublicKey,publicKey,StringComparison.Ordinal))==true)==true,version,credentialsLatestOnly:true);
    public OASISResult<IAvatar> LoadAvatarByPublicKey(string publicKey,int version=0)
        => LoadAvatarByPublicKeyAsync(publicKey,version).GetAwaiter().GetResult();
    public Task<OASISResult<IAvatar>> LoadAvatarByPrivateKeyAsync(string privateKey,int version=0)
        => ReadCanonicalAvatarSelectorAsync(privateKey,avatar=>avatar.ProviderWallets?.Values.Any(wallets=>
            wallets?.Any(wallet=>wallet!=null && string.Equals(wallet.PrivateKey,privateKey,StringComparison.Ordinal))==true)==true,version,credentialsLatestOnly:true);
    public OASISResult<IAvatar> LoadAvatarByPrivateKey(string privateKey,int version=0)
        => LoadAvatarByPrivateKeyAsync(privateKey,version).GetAwaiter().GetResult();
}
