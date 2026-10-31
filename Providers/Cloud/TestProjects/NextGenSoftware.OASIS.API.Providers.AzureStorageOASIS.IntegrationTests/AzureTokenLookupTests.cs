using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeTokenSelectorsResolveAndRejectDuplicates(bool sync)
    {
        var token=Guid.NewGuid().ToString("N");
        Avatar Create(string suffix) => new() {Id=Guid.NewGuid(),Username="owned-token-"+token+suffix,Email=token+suffix+"@example.invalid",
            IsActive=true,VerificationToken=token,ResetToken=token,RefreshTokens=new() {new RefreshToken {Token=token}}};
        var first=Create("first");var duplicate=Create("duplicate");
        foreach(var avatar in new[] {first,duplicate})
            avatar.ProviderWallets[Core.Enums.ProviderType.SolanaOASIS]=new() {new ProviderWallet {Id=Guid.NewGuid(),PublicKey=token,PrivateKey=token}};
        var storage=(IOASISStorageProvider)_provider;
        async Task<OASISResult<IAvatar>> Read(int kind,string value,int version=0) => kind switch {
            0=>sync ? _provider.LoadAvatarByVerificationToken(value,version):await _provider.LoadAvatarByVerificationTokenAsync(value,version),
            1=>sync ? _provider.LoadAvatarByResetToken(value,version):await _provider.LoadAvatarByResetTokenAsync(value,version),
            2=>sync ? _provider.LoadAvatarByRefreshToken(value,version):await _provider.LoadAvatarByRefreshTokenAsync(value,version),
            3=>sync ? storage.LoadAvatarByPublicKey(value,version):await storage.LoadAvatarByPublicKeyAsync(value,version),
            _=>sync ? storage.LoadAvatarByPrivateKey(value,version):await storage.LoadAvatarByPrivateKeyAsync(value,version)};
        Assert.IsFalse((await _provider.SaveAvatarAsync(first)).IsError);
        for(var kind=0;kind<5;kind++)
        {
            var found=await Read(kind,token);Assert.IsFalse(found.IsError,found.Message);Assert.IsTrue(found.IsLoaded);Assert.AreEqual(first.Id,found.Result?.Id);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await Read(kind,token+"missing")).ErrorCode);
            var invalid=await Read(kind," ");Assert.IsTrue(invalid.IsError);Assert.IsInstanceOfType(invalid.Exception,typeof(ArgumentException));
            Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",invalid.ErrorCode);
            var version=await Read(kind,token,-1);Assert.IsTrue(version.IsError);Assert.IsInstanceOfType(version.Exception,typeof(ArgumentOutOfRangeException));
            Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",version.ErrorCode);
        }
        Assert.IsFalse((await _provider.SaveAvatarAsync(duplicate)).IsError);
        for(var kind=0;kind<5;kind++) {var result=await Read(kind,token);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);Assert.IsNull(result.Result);}
        Assert.IsFalse((await _provider.DeleteAvatarAsync(duplicate.Id,true)).IsError);
        for(var kind=0;kind<5;kind++) Assert.AreEqual(first.Id,(await Read(kind,token)).Result?.Id);
    }
}
