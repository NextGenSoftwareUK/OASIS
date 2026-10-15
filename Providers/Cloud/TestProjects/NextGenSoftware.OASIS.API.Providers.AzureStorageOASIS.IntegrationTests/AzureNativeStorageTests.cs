using Azure.Storage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

[TestClass]
public class AzureNativeStorageTests
{
    [TestMethod]
    public async Task OfficialBlobSdkPreservesProfileDetailAndHolon()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_AZURE_NATIVE_TEST"),"Use the owned reusable runner.");
        const string connection="UseDevelopmentStorage=true";
        var names=new[] {"oasis-native-holons","oasis-native-avatars","oasis-native-details"};
        var service=new BlobServiceClient(connection);
        foreach(var name in names) Assert.IsFalse((await service.GetBlobContainerClient(name).ExistsAsync()).Value,"Refusing existing fixture container.");
        var provider=new AzureStorageOASIS(connection,names[0],names[1],names[2]);
        try {
            var active=await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError,active.Message);Assert.IsTrue(provider.IsProviderActivated);
            var id=Guid.NewGuid();
            var avatar=new Avatar {Id=id,Username="owned-native",Email="owned@example.invalid",FirstName="Actual",LastName="Profile",Description="full profile"};
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            var loaded=await provider.LoadAvatarAsync(id);
            Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual("Actual",loaded.Result.FirstName);Assert.AreEqual("full profile",loaded.Result.Description);
            Assert.AreEqual(id,(await provider.LoadAvatarByUsernameAsync(avatar.Username)).Result.Id);
            var detail=new AvatarDetail {Id=id,Username=avatar.Username,Email=avatar.Email,XP=321,Karma=29};
            detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="owned seed"});
            var saved=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Message);
            var read=await provider.LoadAvatarDetailAsync(id);Assert.IsFalse(read.IsError,read.Message);
            Assert.AreEqual(321,read.Result.XP);Assert.AreEqual(29L,read.Result.Karma);
            Assert.AreEqual(detail.Inventory[0].Id,read.Result.Inventory.Single().Id);
            Assert.AreEqual(id,(await provider.LoadAvatarDetailByEmailAsync("OWNED@example.invalid")).Result.Id);
            Assert.AreEqual(1,(await provider.LoadAllAvatarDetailsAsync()).Result.Count());
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=Guid.NewGuid(),Username=detail.Username,Email=detail.Email})).IsError);
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.LoadAvatarDetailByUsernameAsync(detail.Username)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarDetailAsync(Guid.NewGuid())).ErrorCode);
            var holon=new Holon {Id=Guid.NewGuid(),Name="owned root",Description="stored description",ParentHolonId=Guid.NewGuid(),CreatedByAvatarId=id};
            var written=await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(written.IsError,written.Message);
            var stored=await provider.LoadHolonAsync(holon.Id,loadChildren:false);Assert.IsFalse(stored.IsError,stored.Message);
            Assert.AreEqual(holon.Id,stored.Result.Id);Assert.AreEqual(holon.ParentHolonId,stored.Result.ParentHolonId);Assert.AreEqual(holon.Name,stored.Result.Name);
        }
        finally {
            await provider.DeActivateProviderAsync();
            foreach(var name in names) await service.GetBlobContainerClient(name).DeleteIfExistsAsync();
        }
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
