using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class KeyValueCredentialSelectorTests
{
    [DataTestMethod]
    [DataRow(false,0)] [DataRow(true,0)] [DataRow(false,1)] [DataRow(true,1)]
    [DataRow(false,2)] [DataRow(true,2)] [DataRow(false,3)] [DataRow(true,3)] [DataRow(false,4)] [DataRow(true,4)]
    public async Task CredentialSelectorsValidateUniquenessDeletionAndNativeCauses(bool sync,int kind)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);var storage=(IOASISStorageProvider)provider;
        const string key="owned-synthetic-credential";
        Avatar Create(string name) => new() {Id=Guid.NewGuid(),Username=name,Email=name+"@example.invalid",IsActive=true,
            VerificationToken=key,ResetToken=key,RefreshTokens=new() {new RefreshToken {Token=key}},
            ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=Guid.NewGuid(),PublicKey=key,PrivateKey=key}}}}};
        async Task<OASISResult<IAvatar>> Read(string value,int version=0) => kind switch {
            0=>sync ? storage.LoadAvatarByVerificationToken(value,version):await storage.LoadAvatarByVerificationTokenAsync(value,version),
            1=>sync ? storage.LoadAvatarByResetToken(value,version):await storage.LoadAvatarByResetTokenAsync(value,version),
            2=>sync ? storage.LoadAvatarByRefreshToken(value,version):await storage.LoadAvatarByRefreshTokenAsync(value,version),
            3=>sync ? storage.LoadAvatarByPublicKey(value,version):await storage.LoadAvatarByPublicKeyAsync(value,version),
            _=>sync ? storage.LoadAvatarByPrivateKey(value,version):await storage.LoadAvatarByPrivateKeyAsync(value,version)};
        var before=backend.ListCalls;
        foreach(var input in new[] {(" ",0),(key,-1)})
        {
            var invalid=await Read(input.Item1,input.Item2);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",invalid.ErrorCode);
            Assert.IsInstanceOfType(invalid.Exception,typeof(ArgumentException));Assert.IsNull(invalid.Result);
        }
        Assert.AreEqual(before,backend.ListCalls);
        var first=Create("first");var duplicate=Create("duplicate");
        Assert.IsFalse((await provider.SaveAvatarAsync(first)).IsError);
        var loaded=await Read(key);Assert.IsFalse(loaded.IsError,loaded.Message);Assert.IsTrue(loaded.IsLoaded);Assert.AreEqual(first.Id,loaded.Result.Id);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await Read(key+"missing")).ErrorCode);
        Assert.IsFalse((await provider.SaveAvatarAsync(duplicate)).IsError);
        var ambiguous=await Read(key);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,ambiguous.ErrorCode);Assert.IsNull(ambiguous.Result);
        Assert.IsFalse((await provider.DeleteAvatarAsync(duplicate.Id,true)).IsError);
        Assert.AreEqual(first.Id,(await Read(key)).Result.Id);
        var cause=new System.IO.IOException("owned backend read failure");backend.FailGetKey="oasis/avatar/"+first.Id.ToString("N");backend.FailGetException=cause;
        var failed=await Read(key);Assert.IsTrue(failed.IsError);Assert.AreSame(cause,failed.Exception);Assert.IsNull(failed.Result);
    }
}
