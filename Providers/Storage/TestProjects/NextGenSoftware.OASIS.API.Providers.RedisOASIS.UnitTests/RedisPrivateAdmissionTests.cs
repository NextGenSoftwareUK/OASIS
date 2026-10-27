using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.UnitTests;

[TestClass]
public class RedisPrivateAdmissionTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task InvalidHolonAliasRejectsBeforeBackendDispatch(bool sync)
    {
        var backend=new RedisControlledStore();
        using var provider=new RedisOASIS(backend,()=>throw new InvalidOperationException("Key must not be requested for invalid input."));
        var result=sync?provider.DeleteHolonSoft("not-a-provider-guid",false):await provider.DeleteHolonSoftAsync("not-a-provider-guid",false);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentException>(result.Exception);Assert.IsFalse(result.Result);
        Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);Assert.AreEqual(0,backend.Reads);Assert.AreEqual(0,backend.Items.Count);
    }

    [TestMethod]
    public async Task MissingKeyFailsBeforeTransportAndCannotActivate()
    {
        var backend=new RedisControlledStore();
        using var provider=new RedisOASIS(backend,()=>throw new InvalidOperationException("Required key missing."));
        var result=await provider.ActivateProviderAsync();Assert.IsTrue(result.IsError);Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(0,backend.Reads);Assert.AreEqual(0,backend.Items.Count);
    }

    [TestMethod]
    public async Task WrongKeyCannotActivateOrChangeExistingEncryptedState()
    {
        var key=RandomNumberGenerator.GetBytes(32);var wrong=RandomNumberGenerator.GetBytes(32);
        try
        {
            var backend=new RedisControlledStore();
            using(var original=new RedisOASIS(backend,()=>(byte[])key.Clone()))
            {
                var saved=await original.SaveAvatarAsync(new Avatar{Id=Guid.NewGuid(),Username="private-owner",Email="private@example.invalid",IsActive=true});
                Assert.IsFalse(saved.IsError,saved.Message);
            }
            var before=backend.Items.OrderBy(pair=>pair.Key,StringComparer.Ordinal).ToArray();
            using var rejected=new RedisOASIS(backend,()=>(byte[])wrong.Clone());
            var result=await rejected.ActivateProviderAsync();Assert.IsTrue(result.IsError);Assert.IsFalse(rejected.IsProviderActivated);
            CollectionAssert.AreEqual(before,backend.Items.OrderBy(pair=>pair.Key,StringComparer.Ordinal).ToArray());
        }
        finally{CryptographicOperations.ZeroMemory(key);CryptographicOperations.ZeroMemory(wrong);}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task DefaultDeletePreservesHistoryAndExplicitHardDeleteRemovesIt(bool sync)
    {
        var key=RandomNumberGenerator.GetBytes(32);
        try
        {
            using var provider=new RedisOASIS(new RedisControlledStore(),()=>(byte[])key.Clone()){IsVersionControlEnabled=true};
            var id=Guid.NewGuid();var saved=sync?provider.SaveHolon(new Holon{Id=id,Name="owned-soft-delete-contract"}):await provider.SaveHolonAsync(new Holon{Id=id,Name="owned-soft-delete-contract"});
            Assert.IsFalse(saved.IsError);
            var deleted=sync?provider.DeleteHolon(id):await provider.DeleteHolonAsync(id);
            Assert.IsFalse(deleted.IsError);Assert.IsTrue(deleted.IsDeleted);Assert.IsTrue(deleted.Result.IsDeleted);Assert.AreEqual(2,deleted.Result.Version);
            var historical=sync?provider.LoadHolon(id,loadChildren:false,version:1):await provider.LoadHolonAsync(id,loadChildren:false,version:1);
            Assert.IsFalse(historical.IsError);Assert.IsNotNull(historical.Result);Assert.IsFalse(historical.Result.IsDeleted);
            var hard=sync?provider.DeleteHolonSoft(id,false):await provider.DeleteHolonSoftAsync(id,false);
            Assert.IsFalse(hard.IsError);Assert.IsTrue(hard.Result);Assert.IsTrue(hard.IsDeleted);
            var absent=await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsNull(absent.Result);Assert.IsFalse(absent.IsLoaded);
        }
        finally{CryptographicOperations.ZeroMemory(key);}
    }

}
