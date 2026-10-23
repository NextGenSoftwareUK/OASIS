using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Enums;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeCqlDescendantCertifiesTheActualDepth32AndNodeBoundary()
        =>WithNativeGraph(fixture=>global::OASIS.ProviderTests.Fixtures.CqlDescendantAcceptance.VerifyAsync(fixture.Provider,fixture.Root),
            width:0,configure:global::OASIS.ProviderTests.Fixtures.CqlDescendantAcceptance.Configure);

    private static CosmicQueryPlan<Holon> DescendantPlan(GraphFixture fixture, int depth = 4, int nodes = 10,
        CosmicQueryReadAccess? access = null, string name = "owned CQL grandchild")
    {
        var id = fixture.Root.Id;
        var query = CosmicQuery<Holon>.Create();
        if (access != null) query = query.WithReadAccess(access).Result;
        var selected = query.Where(root => root.Id == id)
            .WhereAnyDescendant(child => child.Name == name, CosmicQueryTraversalOptions.Create(depth, nodes).Result)
            .OrderBy(root => root.Name).Take(1);
        Assert.IsFalse(selected.IsError, selected.Message);
        var plan = CosmicQueryPlanner.Plan(selected.Result, ProviderType.Neo4jOASIS,
            new[] { new CosmicQueryTarget(ProviderType.Neo4jOASIS, Provider.CqlCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    [TestMethod]
    public Task NativeDescendantDuplicatePhysicalEdgesDoNotInflateTheNodeBudget()
        =>WithNativeGraph(async fixture=>
        {
            await fixture.Edit("MATCH (parent:Holon {Id:$parent}),(child:Holon {Id:$child}) CREATE (parent)-[:HAS_CHILD]->(child)",
                new{parent=fixture.Root.Id.ToString("D"),child=fixture.Root.Children.First().Id.ToString("D")});
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture,nodes:3));
            Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
        });

    [TestMethod]
    public Task NativeDescendantDoesNotTraverseAnUnlistedDeclaredBranch()
        =>WithNativeGraph(async fixture=>
        {
            var orphan=new Holon{Id=Guid.NewGuid(),ParentHolonId=fixture.Root.Id,Name="unlisted branch"};
            var target=new Holon{Id=Guid.NewGuid(),ParentHolonId=orphan.Id,Name="target"};orphan.Children.Add(target);
            fixture.Ids.Add(orphan.Id);fixture.Ids.Add(target.Id);await fixture.Record("before-owned-unlisted-branch");fixture.Known=false;
            var saved=await fixture.Provider.SaveHolonAsync(orphan,saveChildren:true);
            fixture.Known=!saved.IsError;Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture,nodes:3,name:"target"));
            Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(0,read.Result.Count);
        },configure:root=>root.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(
            new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole("Children",root.Children.Select(child=>child.Id))})));

    [TestMethod]
    public Task NativeDescendantIndependentClausesPreserveExactInt64Metadata()
        =>WithNativeGraph(async fixture=>
        {
            var id=fixture.Root.Id;var bounds=CosmicQueryTraversalOptions.Create(2,3).Result;
            var query=CosmicQuery<Holon>.Create().Where(root=>root.Id==id)
                .WhereAnyDescendant(child=>child.Name=="owned CQL graph child 0",bounds)
                .WhereAnyDescendant(child=>CosmicQueryMetadata.Value<long>(child,"precision")==9007199254740993L,bounds);
            Assert.IsFalse(query.IsError,query.Message);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.Neo4jOASIS,
                new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities)});
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
        },configure:root=>root.Children.First().Children.Single().MetaData["precision"]=9007199254740993L);

    [TestMethod]
    public Task NativeDescendantFilteringPrecedesTheRootWindow()
        =>WithNativeGraph(async fixture=>
        {
            var other=new Holon{Id=Guid.NewGuid(),Name="B"};
            var target=new Holon{Id=Guid.NewGuid(),ParentHolonId=other.Id,Name="target"};other.Children.Add(target);
            fixture.Ids.Add(other.Id);fixture.Ids.Add(target.Id);await fixture.Record("before-owned-paging-root");fixture.Known=false;
            var saved=await fixture.Provider.SaveHolonAsync(other,saveChildren:true);
            fixture.Known=!saved.IsError;Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            var ids=new[]{fixture.Root.Id,other.Id};
            var query=CosmicQuery<Holon>.Create().Where(root=>ids.Contains(root.Id))
                .WhereAnyDescendant(child=>child.Name=="target",CosmicQueryTraversalOptions.Create(2,3).Result)
                .OrderBy(root=>root.Name).Take(1);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.Neo4jOASIS,
                new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities)});
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual("B",read.Result.Single().Name);
        },configure:root=>root.Name="A");

    [DataTestMethod]
    [DataRow(1, false)] [DataRow(2, true)]
    public Task NativeDescendantDepthBoundaryReturnsLeafRoots(int depth, bool match)
        => WithNativeGraph(async fixture =>
        {
            var read = await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture, depth));
            Assert.IsFalse(read.IsError, read.Message + read.Exception);
            Assert.AreEqual(match ? 1 : 0, read.Result.Count);
            if (match) Assert.AreEqual(0, read.Result.Single().Children.Count());
        });

    [TestMethod]
    public Task NativeDescendantBudgetErrorCannotBeHiddenByRootPaging()
        => WithNativeGraph(async fixture =>
        {
            var rejected = await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture, nodes: 2));
            Assert.AreEqual("CQL_QUERY_LIMIT", rejected.ErrorCode, rejected.Message + rejected.Exception);
            Assert.IsNull(rejected.Result); Assert.IsFalse(rejected.IsLoaded);
            var accepted = await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture, nodes: 3));
            Assert.IsFalse(accepted.IsError, accepted.Message + accepted.Exception);
            Assert.AreEqual(1, accepted.Result.Count);
        });

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public Task NativeDescendantRequiresVisibleIntermediateNodes(int visibility)
    {
        var actor = Guid.NewGuid();
        return WithNativeGraph(async fixture =>
        {
            var read = await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture,
                access: CosmicQueryReadAccess.OwnerOrPublic(actor).Result));
            Assert.IsFalse(read.IsError, read.Message + read.Exception);
            Assert.AreEqual(visibility == 0 ? 0 : 1, read.Result.Count);
        }, configure: root =>
        {
            root.IsPublic = true;
            root.Children.First().IsPublic = visibility == 1;
            root.Children.First().CreatedByAvatarId = visibility == 2 ? actor : Guid.NewGuid();
            root.Children.First().Children.Single().IsPublic = true;
        });
    }

    [TestMethod]
    public Task NativeDescendantCycleReturnsTypedDataError()
        => WithNativeGraph(async fixture =>
        {
            using var inspection = fixture.Driver.AsyncSession();
            var id = fixture.Root.Id.ToString("D");
            var row = await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot", new { id })).SingleAsync();
            var source = Core.Serialization.StoredRecordJson.DeserializeStoredHolon(row["snapshot"].As<string>());
            source.ParentHolonId = source.Id; ((Holon)source).ChildRoleManifestJson = null;
            var snapshot = Core.Serialization.StoredRecordJson.Serialize(source);
            await fixture.Edit("MATCH (h:Holon {Id:$id})-[p:HAS_CQL_PROJECTION]->(h) SET h.ParentHolonId=$id,h.SnapshotJson=$snapshot,p.f_ParentHolonId=$id CREATE (h)-[:HAS_CHILD]->(h)", new { id, snapshot });
            var rejected = await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture, name: fixture.Root.Name));
            Assert.AreEqual("CQL_DATA_INVALID", rejected.ErrorCode, rejected.Message + rejected.Exception);
            Assert.IsNull(rejected.Result); Assert.IsFalse(rejected.IsLoaded);
        }, manifested: false);
}
