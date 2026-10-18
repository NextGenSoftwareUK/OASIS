using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private Task<OASISResult<IAvatar>> ReadCredentialAsync(string value,int version,Func<IAvatar,bool> predicate)
        => StoredAvatarWalletSelector.SelectMatchingAsync(value,version,LoadAllAvatarsAsync,predicate);

    public override Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string value,int version=0)
        => ReadCredentialAsync(value,version,avatar=>string.Equals(avatar.VerificationToken,value,StringComparison.Ordinal));
    public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string value,int version=0)
        => LoadAvatarByVerificationTokenAsync(value,version).GetAwaiter().GetResult();

    public override Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string value,int version=0)
        => ReadCredentialAsync(value,version,avatar=>string.Equals(avatar.ResetToken,value,StringComparison.Ordinal));
    public override OASISResult<IAvatar> LoadAvatarByResetToken(string value,int version=0)
        => LoadAvatarByResetTokenAsync(value,version).GetAwaiter().GetResult();

    public override Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string value,int version=0)
        => ReadCredentialAsync(value,version,avatar=>string.Equals(avatar.RefreshToken,value,StringComparison.Ordinal) || avatar.RefreshTokens?.Any(token=>string.Equals(token.Token,value,StringComparison.Ordinal))==true);
    public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string value,int version=0)
        => LoadAvatarByRefreshTokenAsync(value,version).GetAwaiter().GetResult();

    public Task<OASISResult<IAvatar>> LoadAvatarByPublicKeyAsync(string value,int version=0)
        => ReadCredentialAsync(value,version,avatar=>avatar.ProviderWallets?.Values.Any(wallets=>wallets?.Any(wallet=>wallet!=null && string.Equals(wallet.PublicKey,value,StringComparison.Ordinal))==true)==true);
    public OASISResult<IAvatar> LoadAvatarByPublicKey(string value,int version=0)
        => LoadAvatarByPublicKeyAsync(value,version).GetAwaiter().GetResult();

    public Task<OASISResult<IAvatar>> LoadAvatarByPrivateKeyAsync(string value,int version=0)
        => ReadCredentialAsync(value,version,avatar=>avatar.ProviderWallets?.Values.Any(wallets=>wallets?.Any(wallet=>wallet!=null && string.Equals(wallet.PrivateKey,value,StringComparison.Ordinal))==true)==true);
    public OASISResult<IAvatar> LoadAvatarByPrivateKey(string value,int version=0)
        => LoadAvatarByPrivateKeyAsync(value,version).GetAwaiter().GetResult();
}

