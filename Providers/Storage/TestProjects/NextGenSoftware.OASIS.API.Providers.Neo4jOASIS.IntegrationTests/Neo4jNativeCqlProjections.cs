using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static CosmicProjectionPlan<T, TResult> ProjectionPlan<T, TResult>(Provider provider,
        CosmicProjectionQuery<T, TResult> query, CosmicQueryCapabilities? claimed = null) where T : IHolon
    {
        var adapter = (ICosmicQueryProjectionStorageProvider)provider;
        var plan = CosmicProjectionPlanner.Plan(query, Core.Enums.ProviderType.Neo4jOASIS,
            new[] { new CosmicProjectionTarget(Core.Enums.ProviderType.Neo4jOASIS,
                claimed ?? adapter.QueryCapabilities, adapter.ProjectionCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    [TestMethod]
    public Task NativeProjectionPagesExactColumnsWithoutEntityHydration()
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => h.Id == id)
                .OrderBy(h => h.Name).Take(1).Select(h => new { h.Name, Stock = CosmicQueryMetadata.Value<long>(h, "stock") }).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(fixture.Root.Name, result.Result.Providers.Single().Rows.Single().Name);
            Assert.AreEqual(9007199254740993L, result.Result.Providers.Single().Rows.Single().Stock);
        }, configure: root => { root.MetaData["stock"] = 9007199254740993L; root.MetaData["Rating"] = "invalid unselected integer"; });

    [TestMethod]
    public Task NativeProjectionRetainsGuidBooleanAndNullableMetadataDomains()
    {
        var code = Guid.NewGuid();
        return WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => new
            {
                Code = CosmicQueryMetadata.Value<Guid>(h, "code"),
                Enabled = CosmicQueryMetadata.Value<bool>(h, "enabled"),
                Nothing = CosmicQueryMetadata.Value<long?>(h, "nothing"),
                Missing = CosmicQueryMetadata.Value<long?>(h, "missing")
            }).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            Assert.AreEqual(code, row.Code); Assert.IsTrue(row.Enabled); Assert.IsNull(row.Nothing); Assert.IsNull(row.Missing);
        }, configure: root => { root.MetaData["code"] = code; root.MetaData["enabled"] = true; root.MetaData["nothing"] = null; });
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeProjectionRejectsWrongAndMissingRequiredMetadata(bool missing)
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsLoaded); Assert.IsNull(result.Result);
        }, configure: root => { if (!missing) root.MetaData["stock"] = "7"; });

    [TestMethod]
    public Task NativeProjectionVisibilityAndOutputBudgetPrecedePublication()
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
                .Where(h => h.Id == id).Take(1).Select(h => h.Name).Result;
            var hidden = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(hidden.IsError, hidden.Message + hidden.Exception); Assert.AreEqual(0, hidden.Result.Providers.Single().Rows.Count);
            var visible = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => h.Name).Result;
            var limited = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, visible), new[] { fixture.Provider }, maximumPayloadBytes: 2);
            Assert.IsTrue(limited.IsError); Assert.IsNull(limited.Result);
        });

    [TestMethod]
    public Task NativeProjectionCanonicalColumnsPreserveDeclaredDomains()
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => new
            {
                h.Id, h.Name, h.Description, h.Version, h.HolonType, h.IsActive, h.IsPublic,
                h.ParentHolonId, h.VersionId, h.PreviousVersionId, h.CreatedByAvatarId, h.ModifiedByAvatarId
            }).Result;
            var rows = await fixture.Provider.ExecuteCosmicProjectionAsync(ProjectionPlan(fixture.Provider, query));
            Assert.IsFalse(rows.IsError, rows.Message + rows.Exception);
            var result = CosmicProjectionResults.Materialize(query, rows.Result);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Single();
            Assert.AreEqual(id, row.Id); Assert.AreEqual(fixture.Root.Name, row.Name);
            Assert.AreEqual(fixture.Root.Description, row.Description); Assert.AreEqual(fixture.Root.Version, row.Version);
            Assert.AreEqual(fixture.Root.HolonType, row.HolonType); Assert.AreEqual(fixture.Root.IsActive, row.IsActive);
            Assert.AreEqual(fixture.Root.IsPublic, row.IsPublic); Assert.AreEqual(fixture.Root.ParentHolonId, row.ParentHolonId);
            Assert.AreEqual(fixture.Root.VersionId, row.VersionId); Assert.AreEqual(fixture.Root.PreviousVersionId, row.PreviousVersionId);
            Assert.AreEqual(fixture.Root.CreatedByAvatarId, row.CreatedByAvatarId); Assert.AreEqual(fixture.Root.ModifiedByAvatarId, row.ModifiedByAvatarId);
        });

    [TestMethod]
    public Task NativeProjectionOrderingAndPagingRunBeforeSelectedColumns()
        => WithNativeGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().OrderByDescending(h => h.Name).Skip(1).Take(2).Select(h => h.Id).Result;
            var rows = await fixture.Provider.ExecuteCosmicProjectionAsync(ProjectionPlan(fixture.Provider, query));
            Assert.IsFalse(rows.IsError, rows.Message + rows.Exception);
            var expected = new[] { fixture.Root }.Concat(fixture.Root.Children).Concat(fixture.Root.Children.SelectMany(child => child.Children))
                .OrderByDescending(h => h.Name, StringComparer.Ordinal).ThenBy(h => h.Id).Skip(1).Take(2).Select(h => h.Id).ToArray();
            CollectionAssert.AreEqual(expected, CosmicProjectionResults.Materialize(query, rows.Result).Result.ToArray());
        });

    [TestMethod]
    public Task NativeProjectionInputIsIndependentOfGraphOutputCapAndRejectsTruncation()
        => WithNativeGraph(async fixture =>
        {
            var graph = CosmicQueryGraphOptions.Create(maxChildDepth: 1, maximumNodes: 1).Result;
            var source = CosmicQuery<Holon>.Create().WithChildren(graph)
                .Where(h => h.Name.StartsWith("owned CQL graph child", StringComparison.Ordinal));
            var plan = ProjectionPlan(fixture.Provider, source.Select(h => h.Id).Result);
            var rows = await fixture.Provider.ExecuteCosmicProjectionAsync(plan, maximumResults: 200);
            Assert.IsFalse(rows.IsError, rows.Message + rows.Exception); Assert.AreEqual(137, rows.Result.Count);
            var rejected = await fixture.Provider.ExecuteCosmicProjectionAsync(plan, maximumResults: 136);
            Assert.AreEqual("CQL_QUERY_LIMIT", rejected.ErrorCode); Assert.IsNull(rejected.Result);
            var explicitPage = await fixture.Provider.ExecuteCosmicProjectionAsync(
                ProjectionPlan(fixture.Provider, source.Take(136).Select(h => h.Id).Result), maximumResults: 136);
            Assert.IsFalse(explicitPage.IsError, explicitPage.Message + explicitPage.Exception); Assert.AreEqual(136, explicitPage.Result.Count);
        }, width: 137);

    [TestMethod]
    public Task NativeProjectionPreCancellationCannotPublishAnEmptyWindow()
        => WithNativeGraph(async fixture =>
        {
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var plan = ProjectionPlan(fixture.Provider, CosmicQuery<Holon>.Create().Take(0).Select(h => h.Name).Result);
            var cancelled = await fixture.Provider.ExecuteCosmicProjectionAsync(plan, cancellationToken: cancellation.Token);
            Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode); Assert.IsFalse(cancelled.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicProjectionAsync(plan);
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(0, fresh.Result.Count);
        }, width: 0);

    [TestMethod]
    public Task NativeProjectionCancellationAfterActualFetchPublishesNothingAndAllowsReuse()
        => WithNativeGraph(async fixture =>
        {
            using var cancellation = new CancellationTokenSource(); var observed = false;
            fixture.AfterRootRow = () => { observed = true; cancellation.Cancel(); return Task.CompletedTask; };
            var plan = ProjectionPlan(fixture.Provider, DescendantPlan(fixture).Query.Select(h => h.Name).Result);
            var cancelled = await fixture.Provider.ExecuteCosmicProjectionAsync(plan, cancellationToken: cancellation.Token);
            Assert.IsTrue(observed); Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode); Assert.IsNull(cancelled.Result);
            var fresh = await fixture.Provider.ExecuteCosmicProjectionAsync(plan);
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(1, fresh.Result.Count);
        });

    [TestMethod]
    public Task NativeProjectionDeadlineIncludesActualCursorDeliveryAndAllowsReuse()
        => WithNativeGraph(async fixture =>
        {
            fixture.AfterRootRow = () => Task.Delay(TimeSpan.FromSeconds(31));
            var plan = ProjectionPlan(fixture.Provider, DescendantPlan(fixture).Query.Select(h => h.Name).Result);
            var timedOut = await fixture.Provider.ExecuteCosmicProjectionAsync(plan);
            Assert.AreEqual("CQL_QUERY_TIMEOUT", timedOut.ErrorCode); Assert.IsNull(timedOut.Result);
            var fresh = await fixture.Provider.ExecuteCosmicProjectionAsync(plan);
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(1, fresh.Result.Count);
        });

    [TestMethod]
    public Task NativeProjectionCorruptionCannotBeHiddenByAnEmptySourceOrWindow()
        => WithNativeGraph(async fixture =>
        {
            await fixture.Edit("MATCH (h:Holon {Id:$id})-[p:HAS_CQL_PROJECTION]->(h) DELETE p", new { id = fixture.Root.Id.ToString() });
            var source = CosmicQuery<Holon>.Create().Where(h => h.Name == "absent projection source").Skip(1000).Take(1).Select(h => h.Name).Result;
            var invalid = await fixture.Provider.ExecuteCosmicProjectionAsync(ProjectionPlan(fixture.Provider, source));
            Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode); Assert.IsNull(invalid.Result);
        });

    [TestMethod]
    public Task NativeProjectionCannotBorrowASnapshotGuaranteeFromAClaimedProfile()
        => WithNativeGraph(async fixture =>
        {
            var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maxChildDepth: 1).Result;
            var source = CosmicQuery<Holon>.Create().WithChildren(graph).Select(h => h.Name).Result;
            var actual = Provider.CqlCapabilities;
            var claimed = new CosmicQueryCapabilities(actual.Features | CosmicQueryFeatures.GraphSnapshot,
                actual.CanonicalFields.ToArray(), actual.MetadataTypes.ToArray(), actual.Traversal);
            var observed = false;
            fixture.AfterRootRow = () => { observed = true; return Task.CompletedTask; };
            var rejected = await fixture.Provider.ExecuteCosmicProjectionAsync(ProjectionPlan(fixture.Provider, source, claimed));
            Assert.AreEqual("CQL_CONSISTENCY_UNSUPPORTED", rejected.ErrorCode); Assert.IsNull(rejected.Result); Assert.IsFalse(observed);
        });

    [TestMethod]
    public Task NativeProjectionCannotHideDescendantOverflowAndCanReadAgain()
        => WithNativeGraph(async fixture =>
        {
            var limited = await fixture.Provider.ExecuteCosmicProjectionAsync(
                ProjectionPlan(fixture.Provider, DescendantPlan(fixture, depth: 2, nodes: 2).Query.Select(h => h.Name).Result));
            Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode); Assert.IsNull(limited.Result);
            var fresh = await fixture.Provider.ExecuteCosmicProjectionAsync(
                ProjectionPlan(fixture.Provider, DescendantPlan(fixture, depth: 2, nodes: 3).Query.Select(h => h.Name).Result));
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(1, fresh.Result.Count);
        });

    [TestMethod]
    public Task NativeProjectionNestedMetadataAndNarrowIntegerRangesStayTyped()
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .Select(h => CosmicQueryMetadata.Value<long>(h, "nested", "count")).Result;
            var rows = await fixture.Provider.ExecuteCosmicProjectionAsync(ProjectionPlan(fixture.Provider, query));
            Assert.IsFalse(rows.IsError, rows.Message + rows.Exception); Assert.AreEqual(long.MaxValue, rows.Result.Single().Values.Single());
            var narrow = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => CosmicQueryMetadata.Value<byte>(h, "wide")).Result;
            var invalid = await fixture.Provider.ExecuteCosmicProjectionAsync(ProjectionPlan(fixture.Provider, narrow));
            Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode); Assert.IsNull(invalid.Result);
        }, configure: root => { root.MetaData["nested"] = new Dictionary<string, object> { ["count"] = long.MaxValue }; root.MetaData["wide"] = 256; });

    [TestMethod]
    public Task NativeProjectionUnsupportedDateTimeDoesNotReachTheDriver()
        => WithNativeGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<DateTime>(h, "when")).Result;
            var planned = CosmicProjectionPlanner.Plan(query, Core.Enums.ProviderType.Neo4jOASIS,
                new[] { new CosmicProjectionTarget(Core.Enums.ProviderType.Neo4jOASIS, Provider.CqlCapabilities, Provider.CqlProjectionCapabilities) });
            Assert.IsTrue(planned.IsError); Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", planned.ErrorCode);
        }, width: 0);

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeProjectionBoundsWireRowsAndChecksExactUtf8AfterFetch(bool multiByte)
        => WithNativeGraph(async fixture =>
        {
            var observed = false;
            fixture.InspectRootRow = record =>
            {
                observed = true;
                Assert.AreEqual(!multiByte, record["payloadLimit"].As<bool>());
                if (!multiByte) Assert.AreEqual(0, ((IReadOnlyList<object>)record["cells"]).Count,
                    "An impossible native row must not transfer the selected string.");
            };
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => CosmicQueryMetadata.Value<string>(h, "text")).Result;
            var plan = ProjectionPlan(fixture.Provider, query);
            var limited = await fixture.Provider.ExecuteCosmicProjectionAsync(plan, maximumPayloadBytes: 128);
            Assert.IsTrue(observed); Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode); Assert.IsNull(limited.Result);
            fixture.InspectRootRow = null;
            var fresh = await fixture.Provider.ExecuteCosmicProjectionAsync(plan, maximumPayloadBytes: 1024);
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception);
            Assert.AreEqual(fixture.Root.MetaData["text"], fresh.Result.Single().Values.Single());
        }, configure: root => root.MetaData["text"] = new string(multiByte ? '\u00e9' : 'x', multiByte ? 80 : 256));
}
