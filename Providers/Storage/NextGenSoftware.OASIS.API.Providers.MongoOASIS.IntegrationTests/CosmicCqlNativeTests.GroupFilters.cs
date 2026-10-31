using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    private static void SetGroupedFilterRows(Holon root, params (object? Key, object? Stock)[] rows)
    {
        var nodes = new List<IHolon> { root };
        for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
        Assert.Equal(rows.Length, nodes.Count);
        for (var index = 0; index < nodes.Count; index++)
        {
            nodes[index].MetaData["category"] = rows[index].Key!;
            nodes[index].MetaData["stock"] = rows[index].Stock!;
        }
    }
    private static CosmicQuery<Holon> GroupFilterSource() => CosmicQuery<Holon>.Create()
        .Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal)).Result;
    private static async Task<IReadOnlyList<TResult>> RunGroupedFilter<T, TKey, TResult>(MongoGraphFixture fixture,
        CosmicGroupedSelection<T, TKey, TResult> query) where T : IHolon
    {
        var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new ICosmicQueryStorageProvider[] { fixture.Provider });
        Assert.False(result.IsError, result.Message + result.Exception);
        return Assert.Single(result.Result.Providers).Rows;
    }

    [Fact]
    public Task NativeGroupedFilterCombinesAggregatesAndRetainsCompleteProof()
        => WithMongoGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Rows = g.LongCount(), Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                    Min = g.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")), Max = g.Max(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
                .Where(row => row.Rows >= 2 && row.Total == 3L && row.Min == 1L && row.Max == 2L).OrderBy(row => row.Key).Take(1).Result;
            var proof = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.False(proof.IsError, proof.Message + proof.Exception);
            Assert.Equal(new[] { true, false, false }, proof.Result.Select(row => row.IsResult));
            var row = Assert.Single(await RunGroupedFilter(fixture, query));
            Assert.Equal("coffee", row.Key); Assert.Equal(3L, row.Total); Assert.Equal(1.5m, row.Mean);
        }, configure: root => SetGroupedFilterRows(root, ("coffee", 1L), ("coffee", 2L), ("tea", 3L), ("water", 4L)));

    [Fact]
    public Task NativeGroupedFilterPagesMatchingRanksAndComparesSumToCount()
        => WithMongoGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Rows = g.LongCount(), Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
                .Where(row => row.Total > row.Rows).OrderBy(row => row.Key).Skip(1).Take(1).Result;
            Assert.Equal("c", Assert.Single(await RunGroupedFilter(fixture, query)).Key);
        }, configure: root => SetGroupedFilterRows(root, ("a", 2L), ("a", 2L), ("b", -1L), ("c", 4L)));

    [Fact]
    public Task NativeGroupedNullableFilterKeepsNegationAndMixedInequality()
        => WithMongoGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Min = g.Min(h => CosmicQueryMetadata.Value<long?>(h, "stock")) }).Result;
            Assert.Equal(new[] { "a", "c" }, (await RunGroupedFilter(fixture, selected.Where(row => !(row.Min == 2L)).Result)).Select(row => row.Key));
            Assert.Equal(4, (await RunGroupedFilter(fixture, selected.Where(row => row.Min != 0.5m).Result)).Count);
        }, configure: root => SetGroupedFilterRows(root, ("a", null), ("b", 2L), ("c", 3L), ("d", 2L)));

    [Fact]
    public Task NativeGroupedSumFilterKeepsAdjacentInt64AndNegativeBorrow()
        => WithMongoGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
            Assert.Equal("b", Assert.Single(await RunGroupedFilter(fixture, selected.Where(row => row.Total > 9007199254740992m).Result)).Key);
            Assert.Equal("negative", Assert.Single(await RunGroupedFilter(fixture, selected.Where(row => row.Total == -4294967297L).Result)).Key);
        }, configure: root => SetGroupedFilterRows(root, ("a", 9007199254740992L), ("b", 9007199254740993L), ("negative", -4294967296L), ("negative", -1L)));

    [Fact]
    public Task NativeGroupedMixedFilterRetainsBinary64SubnormalAndFractionalBoundary()
        => WithMongoGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<int>(h, "stock")) }).Result;
            Assert.Equal(new[] { "a", "b", "d" }, (await RunGroupedFilter(fixture, selected.Where(row => row.Total < double.Epsilon).Result)).Select(row => row.Key));
            Assert.Equal(new[] { "b", "c", "d" }, (await RunGroupedFilter(fixture, selected.Where(row => row.Total >= -0.5m).Result)).Select(row => row.Key));
        }, configure: root => SetGroupedFilterRows(root, ("a", -1), ("b", 0), ("c", 1), ("d", 0)));

    [Fact]
    public Task NativeGroupedStringFiltersKeepLiteralTextAndNullEmptySuffixSemantics()
        => WithMongoGraph(async fixture =>
        {
            var names = new[] { "c_%'", "tea" };
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category")).Select(g => g.Key).Result;
            var literal = selected.Where(name => names.Contains(name) && name.StartsWith("c_", StringComparison.Ordinal)
                && name.Contains("%'", StringComparison.Ordinal) && name.EndsWith("_%'", StringComparison.Ordinal)).Result;
            Assert.Equal("c_%'", Assert.Single(await RunGroupedFilter(fixture, literal)));
            Assert.Null(Assert.Single(await RunGroupedFilter(fixture, selected.Where(name => !name.EndsWith("", StringComparison.Ordinal)).Result)));
        }, configure: root => SetGroupedFilterRows(root, ("c_%'", 1L), ("coffee", 1L), ("tea", 1L), (null, 1L)));

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeGroupedFilterCannotHideExcludedMalformedValues(bool emptyPage)
        => WithMongoGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Result;
            var query = emptyPage ? selected.Where(total => false).Take(0).Result : selected.Where(total => total < 0).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.True(result.IsError); Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.Null(result.Result);
        }, configure: root => SetGroupedFilterRows(root, ("a", 1L), ("z", "invalid"), ("b", 2L), ("c", 3L)));

    [Fact]
    public Task NativeGroupedFilterCannotHideFinalOverflowInExcludedSum()
        => WithMongoGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Where(total => total < 0).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.True(result.IsError); Assert.Equal("CQL_SCALAR_OVERFLOW", result.ErrorCode); Assert.Null(result.Result);
        }, configure: root => SetGroupedFilterRows(root, ("a", 1L), ("z", long.MaxValue), ("z", 1L), ("b", 2L)));

    [Fact]
    public Task NativeGroupedFalseFilterStillEnforcesCompleteGroupBudget()
        => WithMongoGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => h.Name, CosmicGroupingOptions.Create(2).Result)
                .Select(g => g.LongCount()).Where(rows => false).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.True(result.IsError); Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode); Assert.Null(result.Result);
        });

    [Fact]
    public Task NativeGroupedFilterRetainsBothInt64LimitsAndNumericMembership()
        => WithMongoGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
            Assert.Equal(long.MaxValue, Assert.Single(await RunGroupedFilter(fixture, selected.Where(row => row.Total > 9223372036854775806.5m).Result)).Total);
            Assert.Equal(long.MinValue, Assert.Single(await RunGroupedFilter(fixture, selected.Where(row => row.Total <= -9223372036854775807.5m).Result)).Total);
            var extremes = new[] { long.MinValue, long.MaxValue };
            Assert.Equal(2, (await RunGroupedFilter(fixture, selected.Where(row => extremes.Contains(row.Total)).Result)).Count);
            Assert.Equal(3, (await RunGroupedFilter(fixture, selected.Where(row => row.Total < decimal.MaxValue).Result)).Count);
        }, configure: root => SetGroupedFilterRows(root, ("minimum", long.MinValue), ("middle", 0L), ("maximum", long.MaxValue), ("middle", 0L)));

    [Theory]
    [InlineData("Guid")] [InlineData("DateTime")] [InlineData("Enum")] [InlineData("Boolean")]
    public Task NativeGroupedKeyFilterKeepsExactScalarDomains(string type)
    {
        var date = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        if (type == "Guid") return Check(Guid.Parse("00000000-0000-0000-0000-000000000001"), Guid.Parse("00000000-0000-0000-0000-000000000002"));
        if (type == "DateTime") return Check(date, date.AddTicks(1));
        if (type == "Enum") return Check(Core.Enums.HolonType.Quest, Core.Enums.HolonType.Cafe);
        return Check(true, false);
        Task Check<TValue>(TValue expected, TValue other) => WithMongoGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<TValue>(h, "category")).Select(g => g.Key).Result;
            var key = Expression.Parameter(typeof(TValue), "key");
            var predicate = Expression.Lambda<Func<TValue, bool>>(Expression.Equal(key, Expression.Constant(expected, typeof(TValue))), key);
            Assert.Equal(expected, Assert.Single(await RunGroupedFilter(fixture, selected.Where(predicate).Result)));
            var members = new[] { expected };
            Assert.Equal(expected, Assert.Single(await RunGroupedFilter(fixture, selected.Where(value => members.Contains(value)).Result)));
        }, configure: root => SetGroupedFilterRows(root, (expected, 1L), (other, 1L), (expected, 2L), (other, 2L)));
    }
}
