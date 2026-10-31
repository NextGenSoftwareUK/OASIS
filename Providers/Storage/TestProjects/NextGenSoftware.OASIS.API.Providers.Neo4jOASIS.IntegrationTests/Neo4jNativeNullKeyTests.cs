using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)]
    [DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
    public async Task NativeAcknowledgedSavePublishesPreviouslyNullCallerStorageKeys(int kind,bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var id=Guid.NewGuid();
        var avatar=new Avatar{Id=id,Username="owned-null-key-"+id,ProviderUniqueStorageKey=null!};
        var detail=new AvatarDetail{Id=id,Username="owned-null-key-"+id,XP=678,ProviderUniqueStorageKey=null!};
        var root=new Holon{Id=id,Name="owned-null-key-"+id,ProviderUniqueStorageKey=null!};
        var child=new Holon{Id=Guid.NewGuid(),Name="owned-null-key-child",ProviderUniqueStorageKey=null!};
        if(kind==3)root.Children.Add(child);
        try
        {
            bool failed,saved;string? message;
            if(kind==0){var r=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);failed=r.IsError;saved=r.IsSaved;message=r.Exception?.ToString();}
            else if(kind==1){var r=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);failed=r.IsError;saved=r.IsSaved;message=r.Exception?.ToString();}
            else{var r=synchronous?provider.SaveHolon(root,saveChildren:kind==3):await provider.SaveHolonAsync(root,saveChildren:kind==3);failed=r.IsError;saved=r.IsSaved;message=r.Exception?.ToString();}
            using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var check=raw.AsyncSession();
            foreach(var originalId in kind==3?new[]{id,child.Id}:new[]{id})
            {
                var records=await(await check.RunAsync("MATCH (n {Id:$id}) WHERE n:Avatar OR n:AvatarDetail OR n:Holon RETURN n.ProviderKey AS key,n.SnapshotJson AS json",new{id=originalId.ToString()})).ToListAsync();
                Assert.AreEqual(1,records.Count);Assert.AreEqual(originalId.ToString(),records.Single()["key"].As<string>());
                Assert.IsTrue(records.Single()["json"].As<string>().Contains(originalId.ToString(),StringComparison.Ordinal));
                // Only these independently inspected, already committed originals; never repeat the save.
                await(await check.RunAsync("MATCH (n {Id:$id}) WHERE n:Avatar OR n:AvatarDetail OR n:Holon DETACH DELETE n",new{id=originalId.ToString()})).ConsumeAsync();
            }
            Assert.IsFalse(failed,message);Assert.IsTrue(saved);
            IHolonBase source=kind==0?avatar:kind==1?detail:root;
            Assert.AreEqual(id.ToString(),source.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS]);
            if(kind==3)Assert.AreEqual(child.Id.ToString(),child.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS]);
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
