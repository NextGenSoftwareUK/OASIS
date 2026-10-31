using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow("Name",false)][DataRow("Name",true)]
    [DataRow("Description",false)][DataRow("Description",true)]
    [DataRow("HolonType",false)][DataRow("HolonType",true)]
    public async Task NativeHolonProjectionMustMatchCanonicalSnapshot(string field,bool missing)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        CollectionAssert.Contains(new[]{"Name","Description","HolonType"},field);
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var id=Guid.NewGuid();var original=new Holon{Id=id,Name="owned-index-"+id,Description="original canonical description"};
        try
        {
            Assert.IsFalse((await provider.SaveHolonAsync(original,saveChildren:false)).IsError);
            using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var check=raw.AsyncSession();
            var before=(await(await check.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS json",new{id=id.ToString()})).SingleAsync())["json"].As<string>();
            await(await check.RunAsync("MATCH (h:Holon {Id:$id}) SET h."+field+"=$value",new{id=id.ToString(),value=missing?null:"owned incorrect projection"})).ConsumeAsync();
            var byId=await provider.LoadHolonAsync(id,loadChildren:false);
            var synchronous=provider.LoadHolon(id,loadChildren:false);
            var byKey=await provider.LoadHolonAsync(id.ToString(),loadChildren:false);
            var listed=await provider.LoadAllHolonsAsync(loadChildren:false);
            var inspected=(await(await check.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS json",new{id=id.ToString()})).SingleAsync())["json"].As<string>();
            Assert.AreEqual(before,inspected,"Controlled projection change must not alter canonical original.");
            // Only this known acknowledged and independently inspected original fixture.
            await(await check.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h",new{id=id.ToString()})).ConsumeAsync();
            AssertProjectionFailure(byId);AssertProjectionFailure(synchronous);AssertProjectionFailure(byKey);AssertProjectionFailure(listed);
        }
        finally{await provider.DeActivateProviderAsync();}
    }

    private static void AssertProjectionFailure<T>(OASISResult<T> result)
    {
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
        Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
    }
}
