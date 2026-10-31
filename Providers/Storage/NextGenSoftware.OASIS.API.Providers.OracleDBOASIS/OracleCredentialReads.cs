using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    public Task<OASISResult<IAvatar>> LoadAvatarByPublicKeyAsync(string key,int version=0)
        =>version==0?StoredAvatarWalletSelector.SelectAsync(key,false,0,LoadAllAvatarsAsync):Task.FromResult(InvalidCredentialVersion(version));
    public OASISResult<IAvatar> LoadAvatarByPublicKey(string key,int version=0)
        =>LoadAvatarByPublicKeyAsync(key,version).GetAwaiter().GetResult();
    public Task<OASISResult<IAvatar>> LoadAvatarByPrivateKeyAsync(string key,int version=0)
        =>version==0?StoredAvatarWalletSelector.SelectAsync(key,true,0,LoadAllAvatarsAsync):Task.FromResult(InvalidCredentialVersion(version));
    public OASISResult<IAvatar> LoadAvatarByPrivateKey(string key,int version=0)
        =>LoadAvatarByPrivateKeyAsync(key,version).GetAwaiter().GetResult();

    // Historical profiles are data, never a source of current credential authority.
    private static OASISResult<IAvatar> InvalidCredentialVersion(int version)
        =>new(){IsError=true,ErrorCode="HYPERDRIVE_PAYLOAD_INVALID",Exception=new ArgumentOutOfRangeException(nameof(version),"Credential selectors require latest version zero."),Message="Oracle credential selectors require latest version zero."};
    public override Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string token,int version=0)
        =>version==0?base.LoadAvatarByVerificationTokenAsync(token,0):Task.FromResult(InvalidCredentialVersion(version));
    public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string token,int version=0)
        =>LoadAvatarByVerificationTokenAsync(token,version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string token,int version=0)
        =>version==0?base.LoadAvatarByResetTokenAsync(token,0):Task.FromResult(InvalidCredentialVersion(version));
    public override OASISResult<IAvatar> LoadAvatarByResetToken(string token,int version=0)
        =>LoadAvatarByResetTokenAsync(token,version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string token,int version=0)
        =>version==0?base.LoadAvatarByRefreshTokenAsync(token,0):Task.FromResult(InvalidCredentialVersion(version));
    public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string token,int version=0)
        =>LoadAvatarByRefreshTokenAsync(token,version).GetAwaiter().GetResult();
}
