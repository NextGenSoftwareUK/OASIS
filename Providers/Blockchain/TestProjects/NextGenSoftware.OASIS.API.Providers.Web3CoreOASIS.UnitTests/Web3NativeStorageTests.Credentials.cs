using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyNativeCredentialSelectorsAsync(Web3CoreOASISBaseProvider provider)
    {
        foreach(var synchronous in new[] {false,true})
        {
            var storage=(IOASISStorageProvider)provider;var key="owned-credential-"+Guid.NewGuid().ToString("N");
            Avatar Create(string suffix) => new() {Id=Guid.NewGuid(),Username=key+suffix,IsActive=true,VerificationToken=key,ResetToken=key,
                RefreshTokens=new() {new RefreshToken {Token=key}},ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {
                    new ProviderWallet {Id=Guid.NewGuid(),PublicKey=key,PrivateKey=key}}}}};
            var first=Create("first");var second=Create("second");
            async Task<OASISResult<IAvatar>> Read(int kind,string value,int version=0) => kind switch {
                0=>synchronous ? storage.LoadAvatarByVerificationToken(value,version):await storage.LoadAvatarByVerificationTokenAsync(value,version),
                1=>synchronous ? storage.LoadAvatarByResetToken(value,version):await storage.LoadAvatarByResetTokenAsync(value,version),
                2=>synchronous ? storage.LoadAvatarByRefreshToken(value,version):await storage.LoadAvatarByRefreshTokenAsync(value,version),
                3=>synchronous ? storage.LoadAvatarByPublicKey(value,version):await storage.LoadAvatarByPublicKeyAsync(value,version),
                _=>synchronous ? storage.LoadAvatarByPrivateKey(value,version):await storage.LoadAvatarByPrivateKeyAsync(value,version)};
            Assert.IsFalse((await provider.SaveAvatarAsync(first)).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=first.Id,IsActive=true})).IsError);
            for(var kind=0;kind<5;kind++)
            {
                var found=await Read(kind,key);Assert.IsFalse(found.IsError,found.Message);Assert.IsTrue(found.IsLoaded);Assert.AreEqual(first.Id,found.Result.Id);
                Assert.AreEqual(first.ProviderWallets[ProviderType.SolanaOASIS][0].Id,found.Result.ProviderWallets[ProviderType.SolanaOASIS][0].Id);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await Read(kind,key+"missing")).ErrorCode);
                Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",(await Read(kind," ")).ErrorCode);
                var history=await Read(kind,key,1);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",history.ErrorCode);Assert.IsInstanceOfType(history.Exception,typeof(NotSupportedException));
            }
            Assert.IsFalse((await provider.SaveAvatarAsync(second)).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=second.Id,IsActive=true})).IsError);
            for(var kind=0;kind<5;kind++) {var duplicate=await Read(kind,key);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,duplicate.ErrorCode);Assert.IsNull(duplicate.Result);}
            var deleted=await provider.DeleteAvatarAsync(second.Id,true);Assert.IsFalse(deleted.IsError,deleted.Message);
            for(var kind=0;kind<5;kind++) Assert.AreEqual(first.Id,(await Read(kind,key)).Result.Id);
            Assert.IsFalse((await provider.DeleteAvatarAsync(first.Id,false)).IsError);
            Assert.IsFalse((await provider.DeleteAvatarAsync(second.Id,false)).IsError);
        }
    }
}
