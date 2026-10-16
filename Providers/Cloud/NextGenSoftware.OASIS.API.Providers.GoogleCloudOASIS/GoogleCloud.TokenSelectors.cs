using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS
{
    public override Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string token,int version=0)
        => ReadCanonicalAvatarSelectorAsync(token,avatar=>string.Equals(avatar.VerificationToken,token,StringComparison.Ordinal),version);
    public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string token,int version=0)
        => LoadAvatarByVerificationTokenAsync(token,version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string token,int version=0)
        => ReadCanonicalAvatarSelectorAsync(token,avatar=>string.Equals(avatar.ResetToken,token,StringComparison.Ordinal),version);
    public override OASISResult<IAvatar> LoadAvatarByResetToken(string token,int version=0)
        => LoadAvatarByResetTokenAsync(token,version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string token,int version=0)
        => ReadCanonicalAvatarSelectorAsync(token,avatar=>avatar.RefreshTokens?.Any(value=>value!=null && string.Equals(value.Token,token,StringComparison.Ordinal))==true,version);
    public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string token,int version=0)
        => LoadAvatarByRefreshTokenAsync(token,version).GetAwaiter().GetResult();
}
