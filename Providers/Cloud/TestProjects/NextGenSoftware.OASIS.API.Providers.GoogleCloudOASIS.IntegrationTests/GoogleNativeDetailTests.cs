using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
public class GoogleNativeDetailTests
{
    [TestMethod]
    public async Task OfficialFirestoreSdkRetainsCanonicalDetailsAndRejectsDuplicateSelectors()
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        const string project="oasis-google-native";
        var db=new FirestoreDbBuilder {ProjectId=project,EmulatorDetection=Google.Api.Gax.EmulatorDetection.EmulatorOnly}.Build();
        Assert.AreEqual(0,(await db.Collection("avatarDetails").Limit(1).GetSnapshotAsync()).Count,"Refusing existing test data.");
        var provider=new GoogleCloudOASIS(projectId:project,enableStorage:false,enableFirestore:true,enableBigQuery:false);
        var ids=new List<Guid>();
        try {
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
            var empty=await provider.LoadAllAvatarDetailsAsync();Assert.IsFalse(empty.IsError,empty.Message);Assert.AreEqual(0,empty.Result.Count());
            var detail=new AvatarDetail {Id=Guid.NewGuid(),IsActive=true,Username="owned-user",Email="owned@example.invalid",XP=321,Karma=29,
                MetaData=new Dictionary<string,object> {{"owned-field","owned-value"}}};
            detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="owned seed"});ids.Add(detail.Id);
            var saved=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Message);Assert.IsTrue(saved.IsSaved);
            var loaded=await provider.LoadAvatarDetailAsync(detail.Id);Assert.IsFalse(loaded.IsError,loaded.Message);
            Assert.AreEqual(321,loaded.Result.XP);Assert.AreEqual(29L,loaded.Result.Karma);
            Assert.AreEqual(detail.Inventory[0].Id,loaded.Result.Inventory.Single().Id);Assert.AreEqual("owned-value",loaded.Result.MetaData["owned-field"]);
            Assert.AreEqual(detail.Id,(await provider.LoadAvatarDetailByUsernameAsync(detail.Username)).Result.Id);
            Assert.AreEqual(detail.Id,provider.LoadAvatarDetailByEmail(detail.Email).Result.Id);
            var duplicate=new AvatarDetail {Id=Guid.NewGuid(),IsActive=true,Username=detail.Username,Email=detail.Email};ids.Add(duplicate.Id);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(duplicate)).IsError);
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.LoadAvatarDetailByEmailAsync(detail.Email)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.LoadAvatarDetailByUsernameAsync(detail.Username)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarDetailAsync(Guid.NewGuid())).ErrorCode);
            Assert.AreEqual(2,(await provider.LoadAllAvatarDetailsAsync()).Result.Count());
        }
        finally {foreach(var id in ids) await db.Collection("avatarDetails").Document(id.ToString()).DeleteAsync();await provider.DeActivateProviderAsync();}
    }
}
