using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeGroupedCompositeKeysSortNullBeforeUnicodeScalars()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id, fixture.Root.Children.Last().Id };
            var selection = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => new { Category = CosmicQueryMetadata.Value<string>(h, "category"), h.Version })
                .Select(g => new { g.Key.Category, Rows = g.LongCount() }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(3, result.Result.Count);
            Assert.IsNull(result.Result[0].Keys[0]);
            Assert.AreEqual("\uE000", result.Result[1].Keys[0]);
            Assert.AreEqual("\U00010000", result.Result[2].Keys[0]);
        }, configure: root =>
        {
            SetGroupedVersions(root);
            root.Children.First().MetaData["category"] = "\U00010000";
            root.Children.Last().MetaData["category"] = "\uE000";
        });

    [TestMethod]
    public Task NativeGroupedSignedLimbsAvoidIntermediateOverflow()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id, fixture.Root.Children.Last().Id };
            var selection = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => h.Version)
                .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Single();
            Assert.AreEqual(long.MaxValue, row.Values[0]);
            Assert.AreEqual(decimal.Divide(long.MaxValue, 3m), row.Values[1]);
        }, configure: root =>
        {
            root.MetaData["stock"] = long.MaxValue;
            root.Children.First().MetaData["stock"] = 1L;
            root.Children.Last().MetaData["stock"] = -1L;
        });

    [TestMethod]
    public Task NativeGroupedFinalOverflowDoesNotPublishPartialValues()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var selection = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => h.Version)
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.AreEqual("CQL_SCALAR_OVERFLOW", result.ErrorCode); Assert.IsNull(result.Result);
        }, configure: root =>
        {
            root.MetaData["stock"] = long.MaxValue;
            root.Children.First().MetaData["stock"] = long.MaxValue;
        });

    [TestMethod]
    public Task NativeGroupedPublicAccessExcludesMalformedHiddenInputs()
        => WithNativeGraph(async fixture =>
        {
            var selection = CosmicQuery<Holon>.Create()
                .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
                .Where(h => h.Name.StartsWith("owned CQL", StringComparison.Ordinal))
                .GroupBy(h => h.Version).Select(g => new { Rows = g.LongCount(),
                    Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Single(); Assert.AreEqual(1L, row.Values[0]); Assert.AreEqual(7L, row.Values[1]);
        }, configure: root =>
        {
            root.IsPublic = true; root.MetaData["stock"] = 7L;
            foreach (var child in root.Children) child.MetaData["stock"] = "hidden malformed integer";
        });
}
