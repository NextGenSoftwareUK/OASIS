using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace GoogleConsumerModels
{
    public sealed class AvatarInput:Avatar { }
    public sealed class DetailInput:AvatarDetail { }
    public sealed class HolonInput:Holon { }
}

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests
{
    public sealed partial class GoogleCloudOASISIntegrationTests
    {
        [DataTestMethod][DataRow(false)][DataRow(true)]
        public async Task NativeDetailStorageKeyIsPersistedAndPublished(bool synchronous)
        {
            var id=Guid.NewGuid();var detail=new AvatarDetail{Id=id,IsActive=true,Username="owned-key-"+id.ToString("N")};
            var saved=synchronous?_provider.SaveAvatarDetail(detail):await _provider.SaveAvatarDetailAsync(detail);
            Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            var read=await _provider.LoadAvatarDetailAsync(id);Assert.IsFalse(read.IsError,read.Message);
            var sourceKey=detail.ProviderUniqueStorageKey.GetValueOrDefault(Core.Enums.ProviderType.GoogleCloudOASIS);
            var storedKey=read.Result.ProviderUniqueStorageKey.GetValueOrDefault(Core.Enums.ProviderType.GoogleCloudOASIS);
            var database=(FirestoreDb)typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(_provider)!;
            var reference=database.Collection("avatarDetails").Document(id.ToString());
            Assert.IsTrue((await reference.GetSnapshotAsync()).Exists);
            await reference.DeleteAsync(); // Only this acknowledged and read original fixture.
            Assert.AreEqual(id.ToString(),sourceKey);Assert.AreEqual(id.ToString(),storedKey);
        }

        [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
        public async Task TrustedConsumerInputPersistsCanonicalFirestoreRecord(int kind,bool synchronous)
        {
            var id=Guid.NewGuid();var marker="owned-consumer-"+id.ToString("N");
            var avatar=new GoogleConsumerModels.AvatarInput{Id=id,IsActive=true,Username=marker};
            var detail=new GoogleConsumerModels.DetailInput{Id=id,IsActive=true,Username=marker,XP=543};
            detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name=marker});
            var holon=new GoogleConsumerModels.HolonInput{Id=id,IsActive=true,Name=marker};
            var child=new GoogleConsumerModels.HolonInput{Id=Guid.NewGuid(),IsActive=true,Name=marker+"-child"};
            if(kind==3)holon.Children.Add(child);
            if(kind==0)
            {
                var saved=synchronous?_provider.SaveAvatar(avatar):await _provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                var read=await _provider.LoadAvatarAsync(id);Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(marker,read.Result.Username);Assert.AreEqual(typeof(Avatar),read.Result.GetType());
            }
            else if(kind==1)
            {
                var saved=synchronous?_provider.SaveAvatarDetail(detail):await _provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                var read=await _provider.LoadAvatarDetailAsync(id);Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(543L,read.Result.XP);Assert.AreEqual(marker,read.Result.Inventory.Single().Name);Assert.AreEqual(typeof(AvatarDetail),read.Result.GetType());
            }
            else
            {
                var saved=synchronous?_provider.SaveHolon(holon,saveChildren:kind==3):await _provider.SaveHolonAsync(holon,saveChildren:kind==3);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                var read=await _provider.LoadHolonAsync(id,loadChildren:kind==3,loadChildrenFromProvider:kind==3);Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(marker,read.Result.Name);Assert.AreEqual(typeof(Holon),read.Result.GetType());
                if(kind==3){Assert.AreEqual(child.Id,read.Result.Children.Single().Id);Assert.AreEqual(id,read.Result.Children.Single().ParentHolonId);}
            }
            var database=(FirestoreDb)typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(_provider)!;
            var collection=kind==0?"avatars":kind==1?"avatarDetails":"holons";
            var reference=database.Collection(collection).Document(id.ToString());var native=await reference.GetSnapshotAsync();
            Assert.IsTrue(native.Exists);Assert.IsFalse(native.GetValue<string>("data_json").Contains("GoogleConsumerModels",StringComparison.Ordinal));
            if(kind==3)
            {
                var childReference=database.Collection(collection).Document(child.Id.ToString());var storedChild=await childReference.GetSnapshotAsync();
                Assert.IsFalse(storedChild.GetValue<string>("data_json").Contains("GoogleConsumerModels",StringComparison.Ordinal));
                await childReference.DeleteAsync();
            }
            // Only remove the acknowledged and independently read original test documents.
            await reference.DeleteAsync();
        }
    }
}
