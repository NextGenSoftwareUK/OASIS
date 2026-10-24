using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeGroupedPageRetainsCompleteProofAndNativeSelectedRanks()
        => WithNativeGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL", StringComparison.Ordinal))
                .GroupBy(h => h.HolonType).Select(g => new { g.Key, Rows = g.LongCount() })
                .OrderByDescending(g => g.Rows).ThenBy(g => g.Key).Skip(1).Take(1).Result;
            var plan = GroupedPlan(query);
            var proof = await fixture.Provider.ExecuteCosmicGroupedAsync(plan);
            Assert.IsFalse(proof.IsError, proof.Message + proof.Exception);
            CollectionAssert.AreEqual(new[] { false, true, false }, proof.Result.Select(row => row.IsResult).ToArray());
            CollectionAssert.AreEqual(new long[] { 2, 1, 1 }, proof.Result.Select(row => row.InputCount).ToArray());
            var published = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(plan,
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.IsFalse(published.IsError, published.Message + published.Exception);
            Assert.AreEqual(proof.Result[1].Keys[0], published.Result.Providers.Single().Rows.Single().Key);
        });

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)]
    public Task NativeGroupedPageCannotHideOffPageMalformedAggregate(int take)
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => h.Name)
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Take(take).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.IsTrue(result.IsError); Assert.AreEqual("CQL_DATA_INVALID", result.ErrorCode); Assert.IsNull(result.Result);
        }, configure: root =>
        {
            root.MetaData["stock"] = "invalid off-page integer";
            root.Children.First().MetaData["stock"] = 1L;
        });

    [TestMethod]
    public Task NativeGroupedPageCannotHideOverflowInSkippedGroup()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id, fixture.Root.Children.Last().Id };
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Skip(1).Take(1).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.IsTrue(result.IsError); Assert.AreEqual("CQL_SCALAR_OVERFLOW", result.ErrorCode); Assert.IsNull(result.Result);
        }, configure: root =>
        {
            root.MetaData["category"] = "a"; root.MetaData["stock"] = long.MaxValue;
            root.Children.First().MetaData["category"] = "a"; root.Children.First().MetaData["stock"] = 1L;
            root.Children.Last().MetaData["category"] = "z"; root.Children.Last().MetaData["stock"] = 2L;
        });

    [TestMethod]
    public Task NativeEmptyGroupedPageStillEnforcesCompleteGroupBudget()
        => WithNativeGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL", StringComparison.Ordinal))
                .GroupBy(h => h.Name, CosmicGroupingOptions.Create(2).Result).Select(g => g.LongCount()).Take(0).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.IsTrue(result.IsError); Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode); Assert.IsNull(result.Result);
        });

    [TestMethod]
    public Task NativeGroupedPageKeepsSourceAndResultWindowsIndependent()
        => WithNativeGraph(async fixture =>
        {
            var selected = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .OrderBy(h => h.Name).Take(2).GroupBy(h => h.Name).Select(g => new { g.Key, Rows = g.LongCount() }).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(selected.Skip(1).Take(1).Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual("owned CQL graph child 1", result.Result.Providers.Single().Rows.Single().Key);
            var empty = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(selected.Take(0).Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.IsFalse(empty.IsError, empty.Message); Assert.AreEqual(0, empty.Result.Providers.Single().Rows.Count);
        });
}
