using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class GoogleLifecycleTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task PairedDeletionPreservesActorAndCanonicalFields(bool synchronous)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        const string project="oasis-google-native";
        var db=new FirestoreDbBuilder {ProjectId=project,EmulatorDetection=Google.Api.Gax.EmulatorDetection.EmulatorOnly}.Build();
        var provider=new GoogleCloudOASIS(projectId:project,enableStorage:false,enableFirestore:true,enableBigQuery:false);
        var id=Guid.NewGuid();var actor=Guid.NewGuid();
        var profile=db.Collection("avatars").Document(id.ToString());
        var detail=db.Collection("avatarDetails").Document(id.ToString());
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=id,Username="owned-lifecycle-"+id,IsActive=true,
                FirstName="preserved-profile",DeletedByAvatarId=actor})).IsError);
            var value=new AvatarDetail {Id=id,Username="owned-lifecycle-"+id,IsActive=true,XP=321,Karma=29};
            value.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="preserved-seed"});
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(value)).IsError);
            DateTime? committedDate=null;
            for(var repeat=0;repeat<2;repeat++)
            {
                var removed=synchronous ? provider.DeleteAvatar(id):await provider.DeleteAvatarAsync(id);
                Assert.IsFalse(removed.IsError,removed.Message);
                Assert.IsTrue(AvatarTombstoneEvidence.TryGet(removed,out var committed));
                Assert.AreEqual(actor,committed.DeletedByAvatarId,"Deletion must not discard the stored lifecycle actor.");
                if(committedDate.HasValue) Assert.AreEqual(committedDate.Value,committed.DeletedAtUtc);
                committedDate=committed.DeletedAtUtc;
                var p=OasisJson.Deserialize<Avatar>((await profile.GetSnapshotAsync()).GetValue<string>("data_json"));
                var d=OasisJson.Deserialize<AvatarDetail>((await detail.GetSnapshotAsync()).GetValue<string>("data_json"));
                Assert.IsFalse(p.IsActive);Assert.IsFalse(d.IsActive);
                Assert.AreEqual(committed.DeletedAtUtc,p.DeletedDate);Assert.AreEqual(committed.DeletedAtUtc,d.DeletedDate);
                Assert.AreEqual(actor,p.DeletedByAvatarId);Assert.AreEqual(actor,d.DeletedByAvatarId);
                Assert.AreEqual("preserved-profile",p.FirstName);Assert.AreEqual(321L,d.XP);Assert.AreEqual(29L,d.Karma);
                Assert.AreEqual("preserved-seed",d.Inventory.Single().Name);
            }
            var hard=synchronous ? provider.DeleteAvatar(id,false):await provider.DeleteAvatarAsync(id,false);
            Assert.IsFalse(hard.IsError,hard.Message);
            Assert.IsFalse((await profile.GetSnapshotAsync()).Exists);Assert.IsFalse((await detail.GetSnapshotAsync()).Exists);
        }
        finally
        {
            await profile.DeleteAsync();await detail.DeleteAsync();await provider.DeActivateProviderAsync();
        }
    }
}
