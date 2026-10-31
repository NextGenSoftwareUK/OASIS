using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NeoConsumerModels
{
    public sealed class AvatarInput:Avatar { }
    public sealed class DetailInput:AvatarDetail { }
    public sealed class HolonInput:Holon { }
}

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests
{
    public partial class Neo4jNativeAvatarTests
    {
        [DataTestMethod]
        [DataRow(0,false,false)][DataRow(0,true,false)][DataRow(0,false,true)][DataRow(0,true,true)]
        [DataRow(1,false,false)][DataRow(1,true,false)][DataRow(1,false,true)][DataRow(1,true,true)]
        [DataRow(2,false,false)][DataRow(2,true,false)][DataRow(2,false,true)][DataRow(2,true,true)]
        [DataRow(3,false,false)][DataRow(3,true,false)][DataRow(3,false,true)][DataRow(3,true,true)]
        public async Task NativeConsumerInputPersistsCanonicalRecord(int kind,bool synchronous,bool history)
        {
            const string uri="bolt://127.0.0.1:17687";
            Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
            var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=history};
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var id=Guid.NewGuid();var marker="owned-consumer-"+id.ToString("N");
            var avatar=new NeoConsumerModels.AvatarInput{Id=id,Username=marker,IsActive=true};
            var detail=new NeoConsumerModels.DetailInput{Id=id,Username=marker,IsActive=true,XP=543};
            detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name=marker});
            var root=new NeoConsumerModels.HolonInput{Id=id,Name=marker,IsActive=true};
            var child=new NeoConsumerModels.HolonInput{Id=Guid.NewGuid(),Name=marker+"-child",IsActive=true};
            if(kind==3)root.Children.Add(child);
            try
            {
                if(kind==0)
                {
                    var saved=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                    var fresh=await provider.LoadAvatarAsync(id);Assert.IsFalse(fresh.IsError,fresh.Message);Assert.AreEqual(marker,fresh.Result.Username);Assert.AreEqual(typeof(Avatar),fresh.Result.GetType());
                }
                else if(kind==1)
                {
                    var saved=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                    var fresh=await provider.LoadAvatarDetailAsync(id);Assert.IsFalse(fresh.IsError,fresh.Message);Assert.AreEqual(543L,fresh.Result.XP);Assert.AreEqual(marker,fresh.Result.Inventory.Single().Name);Assert.AreEqual(typeof(AvatarDetail),fresh.Result.GetType());
                }
                else
                {
                    var saved=synchronous?provider.SaveHolon(root,saveChildren:kind==3):await provider.SaveHolonAsync(root,saveChildren:kind==3);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                    var fresh=await provider.LoadHolonAsync(id,loadChildren:kind==3,loadChildrenFromProvider:kind==3);Assert.IsFalse(fresh.IsError,fresh.Message);Assert.AreEqual(marker,fresh.Result.Name);Assert.AreEqual(typeof(Holon),fresh.Result.GetType());
                    if(kind==3){Assert.AreEqual(child.Id,fresh.Result.Children.Single().Id);Assert.AreEqual(id,fresh.Result.Children.Single().ParentHolonId);}
                }
                using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var check=raw.AsyncSession();
                foreach(var originalId in kind==3?new[]{id,child.Id}:new[]{id})
                {
                    var rows=await(await check.RunAsync("MATCH (n {Id:$id}) WHERE n:Avatar OR n:AvatarDetail OR n:Holon RETURN n.SnapshotJson AS json",new{id=originalId.ToString()})).ToListAsync();
                    Assert.AreEqual(1,rows.Count);Assert.IsFalse(rows.Single()["json"].As<string>().Contains("NeoConsumerModels",StringComparison.Ordinal));
                    await(await check.RunAsync("MATCH (n {Id:$id}) WHERE n:Avatar OR n:AvatarDetail OR n:Holon DETACH DELETE n",new{id=originalId.ToString()})).ConsumeAsync();
                    Assert.AreEqual(0L,(await(await check.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) RETURN count(v) AS count",new{id=originalId.ToString()})).SingleAsync())["count"].As<long>(),"First version creates no archive fixture.");
                }
            }
            finally{await provider.DeActivateProviderAsync();}
        }
    }
}
