using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeGroupedCountOrderUsesCompleteIdentityTies()
        => WithNativeGraph(async fixture =>
        {
            var selected = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL", StringComparison.Ordinal))
                .GroupBy(h => h.HolonType).Select(g => new { g.Key, Rows = g.LongCount() })
                .OrderByDescending(g => g.Rows).ThenBy(g => g.Key).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selected));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(new long[] { 2, 1, 1 }, result.Result.Select(row => (long)row.Values[1]).ToArray());
        });

    [TestMethod]
    public Task NativeGroupedSumOrderingRetainsAdjacentLargeIntegers()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => h.Name)
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
                .OrderByDescending(g => g.Total).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selected));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(new long[] { 9007199254740993, 9007199254740992 }, result.Result.Select(row => (long)row.Values[1]).ToArray());
        }, configure: root =>
        {
            root.MetaData["stock"] = 9007199254740992L;
            root.Children.First().MetaData["stock"] = 9007199254740993L;
        });

    [TestMethod]
    public Task NativeGroupedSumOrderingNormalizesNegativeBorrowWithoutFloatingPoint()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id, fixture.Root.Children.Last().Id };
            var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "bucket"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
                .OrderBy(g => g.Total).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selected));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(new long[] { -1, 0 }, result.Result.Select(row => (long)row.Values[1]).ToArray());
        }, configure: root =>
        {
            root.MetaData["bucket"] = "a"; root.MetaData["stock"] = -4294967297L;
            root.Children.First().MetaData["bucket"] = "a"; root.Children.First().MetaData["stock"] = 4294967296L;
            root.Children.Last().MetaData["bucket"] = "b"; root.Children.Last().MetaData["stock"] = 0L;
        });
}
