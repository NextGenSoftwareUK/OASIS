using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace ConsumerModels
{
    public sealed class NativeAvatarInput:Avatar { }
    public sealed class NativeDetailInput:AvatarDetail { }
    public sealed class NativeHolonInput:Holon { }
}

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests
{
    public partial class AWSOASISIntegrationTests
    {
        [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
        public async Task NativeConsumerInputPersistsCanonicalDtoWithoutConsumerTypeNames(int kind,bool synchronous)
        {
            var id=Guid.NewGuid();var marker="consumer-"+id.ToString("N");
            var avatar=new ConsumerModels.NativeAvatarInput{Id=id,IsActive=true,Username=marker};
            var detail=new ConsumerModels.NativeDetailInput{Id=id,IsActive=true,Username=marker,XP=654};
            detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name=marker});
            var holon=new ConsumerModels.NativeHolonInput{Id=id,IsActive=true,Name=marker};
            var child=new ConsumerModels.NativeHolonInput{Id=Guid.NewGuid(),IsActive=true,Name=marker+"-child"};
            if(kind==3)holon.Children.Add(child);
            if(kind==0)
            {
                var saved=synchronous?_provider.SaveAvatar(avatar):await _provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                var loaded=await _provider.LoadAvatarAsync(id);Assert.IsFalse(loaded.IsError,loaded.Message);
                Assert.AreEqual(typeof(Avatar),loaded.Result.GetType());Assert.AreEqual(marker,loaded.Result.Username);
            }
            else if(kind==1)
            {
                var saved=synchronous?_provider.SaveAvatarDetail(detail):await _provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                var loaded=await _provider.LoadAvatarDetailAsync(id);Assert.IsFalse(loaded.IsError,loaded.Message);
                Assert.AreEqual(typeof(AvatarDetail),loaded.Result.GetType());Assert.AreEqual(654L,loaded.Result.XP);Assert.AreEqual(marker,loaded.Result.Inventory.Single().Name);
            }
            else
            {
                var saved=synchronous?_provider.SaveHolon(holon,saveChildren:kind==3):await _provider.SaveHolonAsync(holon,saveChildren:kind==3);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                var loaded=await _provider.LoadHolonAsync(id,loadChildren:kind==3,loadChildrenFromProvider:kind==3);Assert.IsFalse(loaded.IsError,loaded.Message);
                Assert.AreEqual(typeof(Holon),loaded.Result.GetType());Assert.AreEqual(marker,loaded.Result.Name);
                if(kind==3){Assert.AreEqual(child.Id,loaded.Result.Children.Single().Id);Assert.AreEqual(typeof(Holon),loaded.Result.Children.Single().GetType());Assert.AreEqual(id,loaded.Result.Children.Single().ParentHolonId);}
            }
            var table=kind==0?"OasisAvatars":kind==1?"OasisAvatarDetails":"OasisHolons";
            var native=await Client.GetItemAsync(new GetItemRequest{TableName=table,Key=Key(id),ConsistentRead=true});
            Assert.IsFalse(native.Item["data_json"].S.Contains("ConsumerModels",StringComparison.Ordinal));
            Assert.AreEqual(id.ToString(),native.Item["id"].S);
            if(kind==3)
            {
                var nativeChild=await Client.GetItemAsync(new GetItemRequest{TableName=table,Key=Key(child.Id),ConsistentRead=true});
                Assert.IsFalse(nativeChild.Item["data_json"].S.Contains("ConsumerModels",StringComparison.Ordinal));
                Assert.AreEqual(id.ToString(),nativeChild.Item["parentHolonId"].S);
                Assert.AreEqual(System.Net.HttpStatusCode.OK,(await Client.DeleteItemAsync(new DeleteItemRequest{TableName=table,Key=Key(child.Id)})).HttpStatusCode);
            }
            // Delete only the acknowledged, independently inspected original fixture.
            var deleted=await Client.DeleteItemAsync(new DeleteItemRequest{TableName=table,Key=Key(id)});
            Assert.AreEqual(System.Net.HttpStatusCode.OK,deleted.HttpStatusCode);
        }
    }
}
