using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace CosmosConsumerModels
{
    public sealed class AvatarInput:Avatar { }
    public sealed class DetailInput:AvatarDetail { }
    public sealed class HolonInput:Holon { }
}

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests
{
    public partial class AzureCosmosDBOASISIntegrationTests
    {
        [DataTestMethod]
        [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)]
        [DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
        public async Task NativeTrustedConsumerInputUsesCanonicalCoreRecord(int kind,bool synchronous)
        {
            var id=Guid.NewGuid();var marker="owned-consumer-"+id.ToString("N");
            var avatar=new CosmosConsumerModels.AvatarInput{Id=id,IsActive=true,Username=marker};
            var detail=new CosmosConsumerModels.DetailInput{Id=id,IsActive=true,Username=marker,XP=543};
            detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name=marker});
            var holon=new CosmosConsumerModels.HolonInput{Id=id,IsActive=true,Name=marker};
            var child=new CosmosConsumerModels.HolonInput{Id=Guid.NewGuid(),IsActive=true,Name=marker+"-child"};
            if(kind==3)holon.Children.Add(child);
            if(kind==0)
            {
                var saved=synchronous?_provider.SaveAvatar(avatar):await _provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                Assert.IsTrue(saved.IsSaved);Assert.AreEqual(1,saved.SavedCount);
                var read=await _provider.LoadAvatarAsync(id);Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(marker,read.Result.Username);Assert.AreEqual(typeof(Avatar),read.Result.GetType());
            }
            else if(kind==1)
            {
                var saved=synchronous?_provider.SaveAvatarDetail(detail):await _provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                Assert.IsTrue(saved.IsSaved);Assert.AreEqual(1,saved.SavedCount);
                var read=await _provider.LoadAvatarDetailAsync(id);Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(543L,read.Result.XP);Assert.AreEqual(marker,read.Result.Inventory.Single().Name);Assert.AreEqual(typeof(AvatarDetail),read.Result.GetType());
            }
            else
            {
                var saved=synchronous?_provider.SaveHolon(holon,saveChildren:kind==3):await _provider.SaveHolonAsync(holon,saveChildren:kind==3);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                Assert.IsTrue(saved.IsSaved);Assert.AreEqual(kind==3?2:1,saved.SavedCount);
                var read=await _provider.LoadHolonAsync(id,loadChildren:kind==3,loadChildrenFromProvider:kind==3);Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(marker,read.Result.Name);Assert.AreEqual(typeof(Holon),read.Result.GetType());
                if(kind==3){Assert.AreEqual(child.Id,read.Result.Children.Single().Id);Assert.AreEqual(id,read.Result.Children.Single().ParentHolonId);}
            }
            var native=new CosmosDbClient(_client.GetContainer(DatabaseName,kind==0?"avatarItems":kind==1?"avatarDetailItems":"holonItems"));
            foreach(var originalId in kind==3?new[]{id,child.Id}:new[]{id})
            {
                var json=await native.ReadDocumentAsync(originalId.ToString(),new PartitionKey(originalId.ToString()));
                Assert.IsNotNull(json);Assert.IsFalse(json.Contains("CosmosConsumerModels",StringComparison.Ordinal));
                Assert.AreEqual(originalId.ToString(),Newtonsoft.Json.Linq.JObject.Parse(json)["id"]!.ToString());
                await native.DeleteDocumentAsync(originalId.ToString(),new PartitionKey(originalId.ToString()));
            }
        }
    }
}
