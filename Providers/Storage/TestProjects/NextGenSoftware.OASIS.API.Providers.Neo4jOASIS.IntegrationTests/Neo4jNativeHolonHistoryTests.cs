using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OwnedNativeHolonHistorySelectsRootAndKeepsIndependentCurrentDescendants(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=raw.AsyncSession();
        Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (a) WHERE a:Holon OR a:OasisRecordVersion RETURN count(a) AS count")).SingleAsync())["count"].As<long>());
        var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=true};
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var marker=Guid.NewGuid().ToString("N");
            var root=new Holon{Id=Guid.NewGuid(),Name=$"root-v1-{marker}"};
            var child=new Holon{Id=Guid.NewGuid(),Name=$"child-v1-{marker}"};
            var grandchild=new Holon{Id=Guid.NewGuid(),Name=$"grandchild-v1-{marker}"};
            root.Children.Add(child);child.Children.Add(grandchild);
            var created=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
            Assert.IsFalse(created.IsError,created.Message);
            Assert.AreEqual(1,root.Version);Assert.AreEqual(1,child.Version);Assert.AreEqual(1,grandchild.Version);
            var rootVersionId=root.VersionId;
            child.Name=$"child-v2-{marker}";
            Assert.IsFalse((await provider.SaveHolonAsync(child,saveChildren:false)).IsError);
            grandchild.Name=$"grandchild-v2-{marker}";
            Assert.IsFalse((await provider.SaveHolonAsync(grandchild,saveChildren:false)).IsError);
            root.Name=$"root-v2-{marker}";
            Assert.IsFalse((await provider.SaveHolonAsync(root,saveChildren:false)).IsError);
            root.Name=$"root-v3-{marker}";
            Assert.IsFalse((await provider.SaveHolonAsync(root,saveChildren:false)).IsError);
            Assert.AreEqual(3,root.Version);Assert.AreEqual(2,child.Version);Assert.AreEqual(2,grandchild.Version);
            var old=synchronous?provider.LoadHolon(root.Id,version:1):await provider.LoadHolonAsync(root.Id,version:1);
            Assert.IsFalse(old.IsError,old.Message);
            Assert.AreEqual($"root-v1-{marker}",old.Result.Name);Assert.AreEqual(rootVersionId,old.Result.VersionId);
            Assert.AreEqual(1,old.Result.Version);
            Assert.AreEqual(child.Name,old.Result.Children.Single().Name);Assert.AreEqual(2,old.Result.Children.Single().Version);
            Assert.AreEqual(grandchild.Name,old.Result.Children.Single().Children.Single().Name);
            Assert.AreEqual(2,old.Result.Children.Single().Children.Single().Version);
            var byKey=provider.LoadHolon(root.Id.ToString(),loadChildren:false,version:1);
            Assert.IsFalse(byKey.IsError,byKey.Message);Assert.AreEqual($"root-v1-{marker}",byKey.Result.Name);
            Assert.AreEqual(0,byKey.Result.Children.Count());
            Assert.AreEqual(3,(await provider.LoadHolonAsync(root.Id)).Result.Version);
            Assert.IsTrue((await provider.LoadHolonAsync(root.Id,version:99)).IsError);
            Assert.IsTrue((await provider.LoadHolonAsync(root.Id,version:-1)).IsError);
            Assert.AreEqual(2L,(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id}) RETURN count(v) AS count",new{id=root.Id.ToString()})).SingleAsync())["count"].As<long>());
            var original=(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id,Version:1}) RETURN v.SnapshotJson AS snapshot",new{id=root.Id.ToString()})).SingleAsync())["snapshot"].As<string>();
            await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id,Version:1}) SET v.SnapshotJson='null'",new{id=root.Id.ToString()})).ConsumeAsync();
            try
            {
                var invalid=await provider.LoadHolonAsync(root.Id,version:1);
                Assert.IsTrue(invalid.IsError);Assert.IsNull(invalid.Result);
                Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,invalid.ErrorCode);
            }
            finally
            {
                await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id,Version:1}) SET v.SnapshotJson=$snapshot",new{id=root.Id.ToString(),snapshot=original})).ConsumeAsync();
            }
            Assert.IsFalse((await provider.DeleteHolonAsync(root.Id)).IsError);
            Assert.IsTrue((await provider.LoadHolonAsync(root.Id,version:1)).IsError);
            Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id}) RETURN count(v) AS count",new{id=root.Id.ToString()})).SingleAsync())["count"].As<long>());
            Assert.IsFalse((await provider.DeleteHolonAsync(child.Id)).IsError);
            Assert.IsFalse((await provider.DeleteHolonAsync(grandchild.Id)).IsError);
            Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (a) WHERE a:Holon OR a:OasisRecordVersion RETURN count(a) AS count")).SingleAsync())["count"].As<long>());
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
