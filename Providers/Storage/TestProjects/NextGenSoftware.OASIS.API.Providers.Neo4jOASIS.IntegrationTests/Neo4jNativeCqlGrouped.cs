using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static CosmicGroupedPlan<T, TKey, TResult> GroupedPlan<T, TKey, TResult>(CosmicGroupedSelection<T, TKey, TResult> query) where T : IHolon
    {
        var plan = CosmicGroupedPlanner.Plan(query, Core.Enums.ProviderType.Neo4jOASIS,
            new[] { new CosmicGroupedTarget(Core.Enums.ProviderType.Neo4jOASIS, Provider.CqlCapabilities, Provider.CqlGroupedCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message); return plan.Result;
    }

    private static void SetGroupedVersions(Holon root)
    {
        // This fixture disables version control; seed explicit nonzero values
        // so aggregates verify arithmetic rather than merely returning zeros.
        var pending = new List<IHolon> { root };
        for (var index = 0; index < pending.Count; index++)
        {
            pending[index].Version = 2;
            pending.AddRange(pending[index].Children);
        }
    }

    [TestMethod]
    public Task NativeGroupingReducesAllFamiliesWithoutHydratingEntities()
        => WithNativeGraph(async fixture =>
        {
            var selected = CosmicQuery<ScalarMustNotConstruct>.Create()
                .Where(h => h.Name.StartsWith("owned CQL", StringComparison.Ordinal))
                .GroupBy(h => h.Version).Select(g => new { g.Key, Count = g.Count(), Rows = g.LongCount(),
                    Min = g.Min(h => h.Version), Max = g.Max(h => h.Version),
                    Total = g.Sum(h => h.Version), Mean = g.Average(h => h.Version) }).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(selected),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var rows = result.Result.Providers.Single().Rows;
            Assert.AreEqual(1, rows.Count); var row = rows.Single();
            Assert.AreEqual(2, row.Key); Assert.AreEqual(4, row.Count); Assert.AreEqual(4L, row.Rows);
            Assert.AreEqual(2, row.Min); Assert.AreEqual(2, row.Max); Assert.AreEqual(8, row.Total); Assert.AreEqual(2m, row.Mean);
        }, configure: SetGroupedVersions);

    [TestMethod]
    public Task NativeGroupingPreservesOrderedSourceWindows()
        => WithNativeGraph(async fixture =>
        {
            var selected = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL", StringComparison.Ordinal))
                .OrderBy(h => h.Name).Skip(1).Take(2).GroupBy(h => h.Version)
                .Select(g => new { Rows = g.LongCount(), Total = g.Sum(h => h.Version) }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selected));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(1, result.Result.Count); var row = result.Result.Single();
            Assert.AreEqual(2L, row.Values[0]); Assert.AreEqual(4, row.Values[1]);
        }, configure: SetGroupedVersions);

    [TestMethod]
    public Task NativeGroupingRejectsExcessOutputWithoutPartialResults()
        => WithNativeGraph(async fixture =>
        {
            var selected = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL", StringComparison.Ordinal))
                .GroupBy(h => h.Name).Select(g => new { g.Key, Rows = g.LongCount() }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selected), maximumGroups: 1);
            Assert.IsTrue(result.IsError); Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode); Assert.IsNull(result.Result);
        });

    [TestMethod]
    public Task NativeGroupingRejectsNativePayloadAndZeroRemainingGroupBudgets()
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var selected = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .GroupBy(h => h.Name).Select(g => new { g.Key, Rows = g.LongCount() }).Result;
            var plan = GroupedPlan(selected);
            var payload = await fixture.Provider.ExecuteCosmicGroupedAsync(plan, maximumPayloadBytes: 1);
            Assert.AreEqual("CQL_QUERY_LIMIT", payload.ErrorCode); Assert.IsNull(payload.Result);
            var groups = await fixture.Provider.ExecuteCosmicGroupedAsync(plan, maximumGroups: 0);
            Assert.AreEqual("CQL_QUERY_LIMIT", groups.ErrorCode); Assert.IsNull(groups.Result);
        });

    [TestMethod]
    public Task NativeGroupingPreservesNullableEmptyAggregatesAndNullKeys()
        => WithNativeGraph(async fixture =>
        {
            var selected = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL", StringComparison.Ordinal))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "absentCategory"))
                .Select(g => new { g.Key, Rows = g.LongCount(),
                    Total = g.Sum(h => CosmicQueryMetadata.Value<long?>(h, "absentStock")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<long?>(h, "absentStock")),
                    Min = g.Min(h => CosmicQueryMetadata.Value<long?>(h, "absentStock")) }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selected));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Single(); Assert.IsNull(row.Keys[0]);
            Assert.AreEqual(4L, row.Values[1]); Assert.AreEqual(0L, row.Values[2]);
            Assert.IsNull(row.Values[3]); Assert.IsNull(row.Values[4]);
        });

    [TestMethod]
    public Task NativeGroupingEmptyWindowStillChecksTraversalLimits()
        => WithNativeGraph(async fixture =>
        {
            var bad = DescendantPlan(fixture, depth: 2, nodes: 2).Query.Take(0)
                .GroupBy(h => h.Version).Select(g => g.LongCount()).Result;
            var rejected = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(bad));
            Assert.AreEqual("CQL_QUERY_LIMIT", rejected.ErrorCode); Assert.IsNull(rejected.Result);
            var good = DescendantPlan(fixture, depth: 2, nodes: 3).Query.Take(0)
                .GroupBy(h => h.Version).Select(g => g.LongCount()).Result;
            var empty = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(good));
            Assert.IsFalse(empty.IsError, empty.Message + empty.Exception); Assert.AreEqual(0, empty.Result.Count);
        });
}
