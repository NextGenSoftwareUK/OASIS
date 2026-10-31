using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static CosmicScalarPlan<T, TResult> ScalarPlan<T, TResult>(CosmicScalarQuery<T, TResult> query,
        CosmicQueryCapabilities? claimed = null) where T : IHolon
    {
        var plan = CosmicScalarPlanner.Plan(query, Core.Enums.ProviderType.Neo4jOASIS, new[]
        {
            new CosmicScalarTarget(Core.Enums.ProviderType.Neo4jOASIS, claimed ?? Provider.CqlCapabilities, Provider.CqlScalarCapabilities)
        });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    public sealed class ScalarMustNotConstruct : Holon
    {
        public ScalarMustNotConstruct() => throw new InvalidOperationException("A scalar must not construct an entity.");
        [Core.CustomAttrbiutes.CustomOASISProperty] public int Rating { get; set; }
    }

    [DataTestMethod]
    [DataRow(0, -1, 4)] [DataRow(1, 2, 2)] [DataRow(3, 100, 1)]
    [DataRow(9, 2, 0)] [DataRow(0, 0, 0)]
    public Task NativeScalarWindowsAndTypedSourcesDoNotHydrateEntities(int skip, int take, int expected)
        => WithNativeGraph(async fixture =>
        {
            var source = CosmicQuery<ScalarMustNotConstruct>.Create()
                .Where(row => row.Name.StartsWith("owned CQL", StringComparison.Ordinal))
                .OrderByDescending(row => row.Name).Skip(skip).Result;
            if (take >= 0) source = source.Take(take).Result;
            var count = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Count().Result));
            Assert.IsFalse(count.IsError, count.Message + count.Exception); Assert.IsTrue(count.IsLoaded); Assert.AreEqual(expected, count.Result);
            var wide = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.LongCount().Result));
            Assert.IsFalse(wide.IsError, wide.Message + wide.Exception); Assert.AreEqual((long)expected, wide.Result);
            var any = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Any().Result));
            Assert.IsFalse(any.IsError, any.Message + any.Exception); Assert.AreEqual(expected != 0, any.Result);
            var managed = await GraphManager(fixture.Provider).ExecuteScalarQueryAsync(ScalarPlan(source.LongCount().Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.IsFalse(managed.IsError, managed.Message + managed.Exception);
            Assert.AreEqual((long)expected, managed.Result.Providers.Single().Value);
        }, configure: root => root.MetaData["Rating"] = "not an integer");

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeScalarAccessPrecedesCountAndExistence(bool isPublic)
    {
        var actor = Guid.NewGuid();
        return WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var owner = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                .Where(row => row.Id == id).Count().Result;
            var own = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(owner));
            Assert.IsFalse(own.IsError, own.Message + own.Exception); Assert.AreEqual(1, own.Result);
            var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
                .Where(row => row.Id == id).Result;
            var count = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.LongCount().Result));
            Assert.IsFalse(count.IsError, count.Message + count.Exception); Assert.AreEqual(isPublic ? 1L : 0L, count.Result);
            var any = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Any().Result));
            Assert.IsFalse(any.IsError, any.Message + any.Exception); Assert.AreEqual(isPublic, any.Result);
        }, configure: root => { root.CreatedByAvatarId = actor; root.IsPublic = isPublic; });
    }

    [TestMethod]
    public Task NativeScalarInputExceedsRowAndGraphPublicationLimits()
        => WithNativeGraph(async fixture =>
        {
            var graph = CosmicQueryGraphOptions.Create(maxChildDepth: 1, maximumNodes: 1).Result;
            var source = CosmicQuery<Holon>.Create().WithChildren(graph)
                .Where(row => row.Name.StartsWith("owned CQL graph child", StringComparison.Ordinal)).LongCount().Result;
            var count = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source));
            Assert.IsFalse(count.IsError, count.Message + count.Exception); Assert.AreEqual(137L, count.Result);
        }, width: 137);

    [TestMethod]
    public Task NativeScalarAnyDoesNotHideDescendantOverflowAndCanReadAgain()
        => WithNativeGraph(async fixture =>
        {
            var tooSmall = DescendantPlan(fixture, depth: 2, nodes: 2).Query;
            var limited = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(tooSmall.Any().Result));
            Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode); Assert.IsFalse(limited.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(DescendantPlan(fixture, depth: 2, nodes: 3).Query.LongCount().Result));
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(1L, fresh.Result);
        });

    [TestMethod]
    public Task NativeScalarEmptySourceReturnsAnAcknowledgedZeroAndFalse()
        => WithNativeGraph(async fixture =>
        {
            var source = CosmicQuery<Holon>.Create().Where(row => row.Name == "absent scalar source").Result;
            var count = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Count().Result));
            Assert.IsFalse(count.IsError, count.Message + count.Exception); Assert.IsTrue(count.IsLoaded); Assert.AreEqual(0, count.Result);
            var any = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Any().Result));
            Assert.IsFalse(any.IsError, any.Message + any.Exception); Assert.IsFalse(any.Result);
        }, width: 0);

    [TestMethod]
    public Task NativeScalarPreCancellationCannotPublishAnEmptyWindow()
        => WithNativeGraph(async fixture =>
        {
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var plan = ScalarPlan(CosmicQuery<Holon>.Create().Take(0).LongCount().Result);
            var cancelled = await fixture.Provider.ExecuteCosmicScalarAsync(plan, cancellation.Token);
            Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode); Assert.IsFalse(cancelled.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(0L, fresh.Result);
        }, width: 0);

    [TestMethod]
    public Task NativeScalarCancellationAfterActualFetchPublishesNothingAndAllowsReuse()
        => WithNativeGraph(async fixture =>
        {
            using var cancellation = new CancellationTokenSource(); var observed = false;
            fixture.AfterRootRow = () => { observed = true; cancellation.Cancel(); return Task.CompletedTask; };
            var plan = ScalarPlan(DescendantPlan(fixture).Query.LongCount().Result);
            var cancelled = await fixture.Provider.ExecuteCosmicScalarAsync(plan, cancellation.Token);
            Assert.IsTrue(observed); Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode); Assert.IsFalse(cancelled.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(1L, fresh.Result);
        });

    [TestMethod]
    public Task NativeScalarDeadlineIncludesActualCursorDeliveryAndAllowsReuse()
        => WithNativeGraph(async fixture =>
        {
            fixture.AfterRootRow = () => Task.Delay(TimeSpan.FromSeconds(31));
            var plan = ScalarPlan(DescendantPlan(fixture).Query.LongCount().Result);
            var timedOut = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
            Assert.AreEqual("CQL_QUERY_TIMEOUT", timedOut.ErrorCode); Assert.IsFalse(timedOut.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(1L, fresh.Result);
        });

    [TestMethod]
    public Task NativeScalarProjectionCorruptionCannotBeHiddenByAnEmptySourceOrWindow()
        => WithNativeGraph(async fixture =>
        {
            await fixture.Edit("MATCH (h:Holon {Id:$id})-[p:HAS_CQL_PROJECTION]->(h) DELETE p", new { id = fixture.Root.Id.ToString() });
            var source = CosmicQuery<Holon>.Create().Where(row => row.Name == "absent scalar source").Skip(1000).Take(1).LongCount().Result;
            var invalid = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source));
            Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode); Assert.IsFalse(invalid.IsLoaded);
        });

    [TestMethod]
    public Task NativeScalarCannotBorrowASnapshotGuaranteeFromAClaimedProfile()
        => WithNativeGraph(async fixture =>
        {
            var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maxChildDepth: 1).Result;
            var source = CosmicQuery<Holon>.Create().WithChildren(graph).Count().Result;
            var actual = Provider.CqlCapabilities;
            var claimed = new CosmicQueryCapabilities(actual.Features | CosmicQueryFeatures.GraphSnapshot,
                actual.CanonicalFields.ToArray(), actual.MetadataTypes.ToArray(), actual.Traversal);
            var observed = false;
            fixture.AfterRootRow = () => { observed = true; return Task.CompletedTask; };
            var rejected = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source, claimed));
            Assert.AreEqual("CQL_CONSISTENCY_UNSUPPORTED", rejected.ErrorCode);
            Assert.IsFalse(rejected.IsLoaded); Assert.IsFalse(observed);
        });
}
