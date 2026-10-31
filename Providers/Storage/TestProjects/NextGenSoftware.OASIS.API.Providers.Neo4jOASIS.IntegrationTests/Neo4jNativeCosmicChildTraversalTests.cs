using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false, false)] [DataRow(false, true)] [DataRow(true, false)] [DataRow(true, true)]
    public async Task NativeCosmicParentTraversalHasConsistentDepthBoundary(bool synchronous, bool byKey)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = raw.AsyncSession();
        var grand = new Holon { Id = Guid.NewGuid(), Name = "owned cosmic grandchild" };
        var child = new Holon { Id = Guid.NewGuid(), Name = "owned cosmic child", Children = new List<IHolon> { grand } };
        var logicalVersion=Guid.NewGuid();
        var parent = new Holon { Id = Guid.NewGuid(), Name = "owned cosmic parent", Version=4, VersionId=logicalVersion,
            Children = new List<IHolon> { child } };
        var ids = new[] { parent.Id.ToString(), child.Id.ToString(), grand.Id.ToString() };
        var complete = false;
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            Assert.AreEqual(0L, (await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS count", new { ids })).SingleAsync())["count"].As<long>());
            var saved = await provider.SaveHolonAsync(parent);
            Assert.IsFalse(saved.IsError, saved.Message);
            Guid Token(IHolon value)=>CosmicConcurrency.GetProviderRevisionId(value,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Neo4jOASIS)!.Value;
            var firstToken=Token(saved.Result);
            Assert.AreNotEqual(Guid.Empty,firstToken);Assert.AreNotEqual(Guid.Empty,Token(saved.Result.Children.Single()));
            Assert.AreEqual(logicalVersion,saved.Result.VersionId);

            foreach (var fromProvider in new[] { false, true })
            foreach (var recursive in new[] { false, true })
            foreach (var depth in new[] { 0, 1, 2 })
            {
                var found = byKey
                    ? synchronous ? provider.LoadHolonsForParent(parent.Id.ToString(), recursive: recursive, maxChildDepth: depth, loadChildrenFromProvider: fromProvider)
                        : await provider.LoadHolonsForParentAsync(parent.Id.ToString(), recursive: recursive, maxChildDepth: depth, loadChildrenFromProvider: fromProvider)
                    : synchronous ? provider.LoadHolonsForParent(parent.Id, recursive: recursive, maxChildDepth: depth, loadChildrenFromProvider: fromProvider)
                        : await provider.LoadHolonsForParentAsync(parent.Id, recursive: recursive, maxChildDepth: depth, loadChildrenFromProvider: fromProvider);
                Assert.IsFalse(found.IsError, found.Message + found.Exception);
                Assert.AreEqual(child.Id, found.Result.Single().Id);
                Assert.AreEqual(recursive && (depth == 0 || depth > 1) ? 1 : 0, found.Result.Single().Children.Count());
                var offset = await provider.LoadHolonsForParentAsync(parent.Id, maxChildDepth: 2, curentChildDepth: 1, loadChildrenFromProvider: fromProvider);
                Assert.IsFalse(offset.IsError, offset.Message + offset.Exception); Assert.AreEqual(0, offset.Result.Single().Children.Count());
            }
            var descendantBefore=await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN h.Id AS id,h.SnapshotJson AS snapshot,h.ProviderRevisionId AS revision",
                new {ids=new[]{child.Id.ToString(),grand.Id.ToString()}})).ToListAsync();
            var rootOnly=await provider.LoadHolonAsync(parent.Id,loadChildren:false);
            Assert.IsFalse(rootOnly.IsError,rootOnly.Message);Assert.AreEqual(firstToken,Token(rootOnly.Result));
            rootOnly.Result.Description="owned root-only update";
            rootOnly.Result.Children=new List<IHolon>{new Holon{Name="must never persist"}};
            var written=synchronous ? provider.SaveHolon(rootOnly.Result,saveChildren:false) : await provider.SaveHolonAsync(rootOnly.Result,saveChildren:false);
            Assert.IsFalse(written.IsError,written.Message+written.Exception);Assert.AreEqual(0,written.Result.Children.Count);
            Assert.AreNotEqual(firstToken,Token(written.Result));Assert.AreEqual(firstToken,Token(rootOnly.Result));
            Assert.AreEqual(logicalVersion,written.Result.VersionId);Assert.AreEqual(4,written.Result.Version);
            var physical=await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.ProviderRevisionId AS revision",new{id=parent.Id.ToString()})).SingleAsync();
            Assert.AreEqual(Token(written.Result).ToString("D"),physical["revision"].As<string>());
            var descendantAfter=await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN h.Id AS id,h.SnapshotJson AS snapshot,h.ProviderRevisionId AS revision",
                new {ids=new[]{child.Id.ToString(),grand.Id.ToString()}})).ToListAsync();
            foreach(var before in descendantBefore)
            {
                var after=descendantAfter.Single(row=>row["id"].As<string>()==before["id"].As<string>());
                Assert.AreEqual(before["snapshot"].As<string>(),after["snapshot"].As<string>());
                Assert.AreEqual(before["revision"].As<string>(),after["revision"].As<string>());
            }
            await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) REMOVE h.ProviderRevisionId",new{id=parent.Id.ToString()})).ConsumeAsync();
            var legacy=await provider.LoadHolonAsync(parent.Id,loadChildren:false);
            Assert.IsFalse(legacy.IsError,legacy.Message);Assert.AreEqual(Guid.Empty,Token(legacy.Result));
            var spoofed=Guid.NewGuid();CosmicConcurrency.SetProviderRevisionId(legacy.Result,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Neo4jOASIS,spoofed);
            var upgraded=await provider.SaveHolonAsync(legacy.Result,saveChildren:false);
            Assert.IsFalse(upgraded.IsError,upgraded.Message+upgraded.Exception);
            Assert.AreNotEqual(Guid.Empty,Token(upgraded.Result));Assert.AreNotEqual(spoofed,Token(upgraded.Result));
            Assert.AreEqual(spoofed,Token(legacy.Result));Assert.AreEqual(logicalVersion,upgraded.Result.VersionId);
            var reloaded=await provider.LoadHolonAsync(parent.Id,loadChildrenFromProvider:true);
            Assert.IsFalse(reloaded.IsError,reloaded.Message);Assert.AreEqual(grand.Id,reloaded.Result.Children.Single().Children.Single().Id);
            complete = true;
        }
        finally
        {
            if (complete) await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids })).ConsumeAsync();
            await provider.DeActivateProviderAsync();
        }
    }
}
