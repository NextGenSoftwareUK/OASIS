using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static TValue FilterSingle<TValue>(IReadOnlyList<TValue> rows)
    {
        Assert.AreEqual(1, rows.Count); return rows[0];
    }
    private static void SetGroupedFilterRows(Holon root, params (object? Key, object? Stock)[] rows)
    {
        var nodes = new List<IHolon> { root };
        for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
        Assert.AreEqual(rows.Length, nodes.Count);
        for (var index = 0; index < nodes.Count; index++)
        {
            nodes[index].MetaData["category"] = rows[index].Key!;
            nodes[index].MetaData["stock"] = rows[index].Stock!;
        }
    }
    private static CosmicQuery<Holon> GroupFilterSource() => CosmicQuery<Holon>.Create()
        .Where(h => h.Name.StartsWith("owned CQL", StringComparison.Ordinal)).Result;
    private static async Task<IReadOnlyList<TResult>> RunGroupedFilter<T, TKey, TResult>(GraphFixture fixture,
        CosmicGroupedSelection<T, TKey, TResult> query) where T : IHolon
    {
        var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new ICosmicQueryStorageProvider[] { fixture.Provider });
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        return FilterSingle(result.Result.Providers).Rows;
    }

    [TestMethod]
    public Task NativeGroupedFilterCombinesAggregatesAndRetainsCompleteProof()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Rows = g.LongCount(), Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                    Min = g.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")), Max = g.Max(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
                .Where(row => row.Rows >= 2 && row.Total == 3L && row.Min == 1L && row.Max == 2L).OrderBy(row => row.Key).Take(1).Result;
            var proof = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.IsFalse(proof.IsError, proof.Message + proof.Exception);
            CollectionAssert.AreEqual(new[] { true, false, false }, (proof.Result.Select(row => row.IsResult)).ToArray());
            var row = FilterSingle(await RunGroupedFilter(fixture, query));
            Assert.AreEqual("coffee", row.Key); Assert.AreEqual(3L, row.Total); Assert.AreEqual(1.5m, row.Mean);
        }, configure: root => SetGroupedFilterRows(root, ("coffee", 1L), ("coffee", 2L), ("tea", 3L), ("water", 4L)));

    [TestMethod]
    public Task NativeGroupedFilterPagesMatchingRanksAndComparesSumToCount()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Rows = g.LongCount(), Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
                .Where(row => row.Total > row.Rows).OrderBy(row => row.Key).Skip(1).Take(1).Result;
            Assert.AreEqual("c", FilterSingle(await RunGroupedFilter(fixture, query)).Key);
        }, configure: root => SetGroupedFilterRows(root, ("a", 2L), ("a", 2L), ("b", -1L), ("c", 4L)));

    [TestMethod]
    public Task NativeGroupedNullableFilterKeepsNegationAndMixedInequality()
        => WithNativeGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Min = g.Min(h => CosmicQueryMetadata.Value<long?>(h, "stock")) }).Result;
            CollectionAssert.AreEqual(new[] { "a", "c" }, ((await RunGroupedFilter(fixture, selected.Where(row => !(row.Min == 2L)).Result)).Select(row => row.Key)).ToArray());
            Assert.AreEqual(4, (await RunGroupedFilter(fixture, selected.Where(row => row.Min != 0.5m).Result)).Count);
        }, configure: root => SetGroupedFilterRows(root, ("a", null), ("b", 2L), ("c", 3L), ("d", 2L)));

    [TestMethod]
    public Task NativeGroupedSumFilterKeepsAdjacentInt64AndNegativeBorrow()
        => WithNativeGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
            Assert.AreEqual("b", FilterSingle(await RunGroupedFilter(fixture, selected.Where(row => row.Total > 9007199254740992m).Result)).Key);
            Assert.AreEqual("negative", FilterSingle(await RunGroupedFilter(fixture, selected.Where(row => row.Total == -4294967297L).Result)).Key);
        }, configure: root => SetGroupedFilterRows(root, ("a", 9007199254740992L), ("b", 9007199254740993L), ("negative", -4294967296L), ("negative", -1L)));

    [TestMethod]
    public Task NativeGroupedMixedFilterRetainsBinary64SubnormalAndFractionalBoundary()
        => WithNativeGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<int>(h, "stock")) }).Result;
            CollectionAssert.AreEqual(new[] { "a", "b", "d" }, ((await RunGroupedFilter(fixture, selected.Where(row => row.Total < double.Epsilon).Result)).Select(row => row.Key)).ToArray());
            CollectionAssert.AreEqual(new[] { "b", "c", "d" }, ((await RunGroupedFilter(fixture, selected.Where(row => row.Total >= -0.5m).Result)).Select(row => row.Key)).ToArray());
        }, configure: root => SetGroupedFilterRows(root, ("a", -1), ("b", 0), ("c", 1), ("d", 0)));

    [TestMethod]
    public Task NativeGroupedStringFiltersKeepLiteralTextAndNullEmptySuffixSemantics()
        => WithNativeGraph(async fixture =>
        {
            var names = new[] { "c_%'", "tea" };
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category")).Select(g => g.Key).Result;
            var literal = selected.Where(name => names.Contains(name) && name.StartsWith("c_", StringComparison.Ordinal)
                && name.Contains("%'", StringComparison.Ordinal) && name.EndsWith("_%'", StringComparison.Ordinal)).Result;
            Assert.AreEqual("c_%'", FilterSingle(await RunGroupedFilter(fixture, literal)));
            Assert.IsNull(FilterSingle(await RunGroupedFilter(fixture, selected.Where(name => !name.EndsWith("", StringComparison.Ordinal)).Result)));
        }, configure: root => SetGroupedFilterRows(root, ("c_%'", 1L), ("coffee", 1L), ("tea", 1L), (null, 1L)));

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeGroupedFilterCannotHideExcludedMalformedValues(bool emptyPage)
        => WithNativeGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Result;
            var query = emptyPage ? selected.Where(total => false).Take(0).Result : selected.Where(total => total < 0).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.IsTrue(result.IsError); Assert.AreEqual("CQL_DATA_INVALID", result.ErrorCode); Assert.IsNull(result.Result);
        }, configure: root => SetGroupedFilterRows(root, ("a", 1L), ("z", "invalid"), ("b", 2L), ("c", 3L)));

    [TestMethod]
    public Task NativeGroupedFilterCannotHideFinalOverflowInExcludedSum()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Where(total => total < 0).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.IsTrue(result.IsError); Assert.AreEqual("CQL_SCALAR_OVERFLOW", result.ErrorCode); Assert.IsNull(result.Result);
        }, configure: root => SetGroupedFilterRows(root, ("a", 1L), ("z", long.MaxValue), ("z", 1L), ("b", 2L)));

    [TestMethod]
    public Task NativeGroupedFalseFilterStillEnforcesCompleteGroupBudget()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => h.Name, CosmicGroupingOptions.Create(2).Result)
                .Select(g => g.LongCount()).Where(rows => false).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.IsTrue(result.IsError); Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode); Assert.IsNull(result.Result);
        });

    [TestMethod]
    public Task NativeGroupedFilterRetainsBothInt64LimitsAndNumericMembership()
        => WithNativeGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
            Assert.AreEqual(long.MaxValue, FilterSingle(await RunGroupedFilter(fixture, selected.Where(row => row.Total > 9223372036854775806.5m).Result)).Total);
            Assert.AreEqual(long.MinValue, FilterSingle(await RunGroupedFilter(fixture, selected.Where(row => row.Total <= -9223372036854775807.5m).Result)).Total);
            var extremes = new[] { long.MinValue, long.MaxValue };
            Assert.AreEqual(2, (await RunGroupedFilter(fixture, selected.Where(row => extremes.Contains(row.Total)).Result)).Count);
            Assert.AreEqual(3, (await RunGroupedFilter(fixture, selected.Where(row => row.Total < decimal.MaxValue).Result)).Count);
        }, configure: root => SetGroupedFilterRows(root, ("minimum", long.MinValue), ("middle", 0L), ("maximum", long.MaxValue), ("middle", 0L)));

    [DataTestMethod]
    [DataRow("Guid")] [DataRow("Enum")] [DataRow("Boolean")]
    public Task NativeGroupedKeyFilterKeepsExactScalarDomains(string type)
    {
        if (type == "Guid") return Check(Guid.Parse("00000000-0000-0000-0000-000000000001"), Guid.Parse("00000000-0000-0000-0000-000000000002"));
        if (type == "Enum") return Check(Core.Enums.HolonType.Quest, Core.Enums.HolonType.Cafe);
        return Check(true, false);
        Task Check<TValue>(TValue expected, TValue other) => WithNativeGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<TValue>(h, "category")).Select(g => g.Key).Result;
            var key = Expression.Parameter(typeof(TValue), "key");
            var predicate = Expression.Lambda<Func<TValue, bool>>(Expression.Equal(key, Expression.Constant(expected, typeof(TValue))), key);
            Assert.AreEqual(expected, FilterSingle(await RunGroupedFilter(fixture, selected.Where(predicate).Result)));
            var members = new[] { expected };
            Assert.AreEqual(expected, FilterSingle(await RunGroupedFilter(fixture, selected.Where(value => members.Contains(value)).Result)));
        }, configure: root => SetGroupedFilterRows(root, (expected, 1L), (other, 1L), (expected, 2L), (other, 2L)));
    }
}
