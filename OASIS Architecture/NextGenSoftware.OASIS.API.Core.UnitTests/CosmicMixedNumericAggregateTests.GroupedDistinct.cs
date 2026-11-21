using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicMixedNumericAggregateTests
{
    private static CosmicGroupedSelection<Holon, CosmicProjectionRecord, CosmicProjectionRecord> DistinctGroups<TValue>(
        Fixture f, Expression<Func<Holon, TValue>> selector, int maximumGroups = 25)
    {
        var key = CosmicProjectionSelection<Holon>.Create("Category", h => CosmicQueryMetadata.Value<string?>(h, new[] { "category" })).Result;
        var g = f.Query.Source.GroupByColumns(new[] { key }, CosmicGroupingOptions.Create(maximumGroups, 1048576).Result).Result;
        var selected = g.SelectColumns(new[] {
            CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Key(g, "Category", 0).Result,
            CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Aggregate(g, "Rows", g.Source.LongCount().Result).Result,
            CosmicGroupedBinding<Holon, CosmicProjectionRecord>.CountValues(g, "Populated", selector).Result,
            CosmicGroupedBinding<Holon, CosmicProjectionRecord>.CountValues(g, "Unique", selector, true).Result });
        Assert.False(selected.IsError, selected.Message);
        return selected.Result;
    }

    private static CosmicGroupedSelection<Holon, CosmicProjectionRecord, CosmicProjectionRecord> DistinctIntGroups(Fixture f, int maximumGroups = 25)
        => DistinctGroups(f, h => CosmicQueryMetadata.Value<int?>(h, new[] { "amount" }), maximumGroups);

    [Fact]
    public async Task GroupedDistinctReconcilesIdentityBeforeCountingValuesAndExcludesNull()
    {
        var f = await Create(typeof(int), true, new object[] { 100, 2, 3, null! }, new object[] { 2, 4, null! }, overlap: 1,
            storedKeys: new object[] { "g", "g", "g", "g" }, nativeKeys: new object[] { "g", "g", "g" });
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, DistinctIntGroups(f));
        Assert.False(plan.IsError, plan.Message);
        var result = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(result.IsError, result.Message); var row = Assert.Single(result.Result);
        Assert.Equal(6L, row["Rows"]); Assert.Equal(4L, row["Populated"]); Assert.Equal(3L, row["Unique"]);
    }

    [Fact]
    public async Task GroupedDistinctHavingOrderingAndPagingUseTheLiveChangedGroupKey()
    {
        var f = await Create(typeof(int), true, new object[] { 99, 2, 3, 4, 4 }, new object[] { 2, 5, 5 }, overlap: 1,
            storedKeys: new object[] { "old", "a", "b", "b", "b" }, nativeKeys: new object[] { "a", "a", "b" });
        var selected = DistinctIntGroups(f).Where(row => (long)row["Unique"] >= 2)
            .OrderByDescending(row => (long)row["Unique"]).Skip(1).Take(1);
        Assert.False(selected.IsError, selected.Message);
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, selected.Result);
        Assert.False(plan.IsError, plan.Message);
        var result = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(result.IsError, result.Message); var row = Assert.Single(result.Result);
        Assert.Equal("a", row["Category"]); Assert.Equal(2L, row["Unique"]); Assert.Equal(3L, row["Rows"]);
    }

    [Fact]
    public async Task GroupedDistinctCountsTheComplete198RowUnionWithoutReapplyingOneBranchLimit()
    {
        var values = Enumerable.Repeat((object)2, 99).ToArray(); var keys = Enumerable.Repeat((object)"g", 99).ToArray();
        var f = await Create(typeof(int), true, values, values, storedKeys: keys, nativeKeys: keys);
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, DistinctIntGroups(f));
        var result = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(result.IsError, result.Message); var row = Assert.Single(result.Result);
        Assert.Equal(198L, row["Rows"]); Assert.Equal(198L, row["Populated"]); Assert.Equal(1L, row["Unique"]);
    }

    [Fact]
    public async Task GroupedDistinctAllNullAndEmptyInputsDoNotInventValuesOrGroups()
    {
        var f = await Create(typeof(int), true, new object[] { null!, null! }, new object[] { null! },
            storedKeys: new object[] { "g", "g" }, nativeKeys: new object[] { "g" });
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, DistinctIntGroups(f));
        var result = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(result.IsError, result.Message); var row = Assert.Single(result.Result);
        Assert.Equal(3L, row["Rows"]); Assert.Equal(0L, row["Populated"]); Assert.Equal(0L, row["Unique"]);
        var empty = await Create(typeof(int), true, Array.Empty<object>(), Array.Empty<object>(),
            storedKeys: Array.Empty<object>(), nativeKeys: Array.Empty<object>());
        var emptyPlan = CosmicMixedProjectionResults.PrepareGrouping(empty.Query, DistinctIntGroups(empty));
        var emptyResult = CosmicMixedProjectionResults.Group(emptyPlan.Result, Actor, empty.Mapping, empty.Stored, empty.Live);
        Assert.False(emptyResult.IsError, emptyResult.Message); Assert.Empty(emptyResult.Result);
    }

    [Fact]
    public async Task GroupedDistinctKeepsAdjacentInt64ValuesBeyondFloatingPointPrecisionSeparate()
    {
        const long a = 9007199254740992L, b = 9007199254740993L;
        var f = await Create(typeof(long), true, new object[] { a, a }, new object[] { b, a },
            storedKeys: new object[] { "g", "g" }, nativeKeys: new object[] { "g", "g" });
        var selected = DistinctGroups(f, h => CosmicQueryMetadata.Value<long?>(h, new[] { "amount" }));
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, selected);
        var result = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(result.IsError, result.Message); var row = Assert.Single(result.Result);
        Assert.Equal(4L, row["Rows"]); Assert.Equal(2L, row["Unique"]);
    }

    [Fact]
    public async Task GroupedDistinctUsesOrdinalCaseUnicodeAndEmbeddedNulValues()
    {
        var f = await Create(typeof(string), true, new object[] { "A", "a", "embedded\0nul", "\u03bb" }, new object[] { "A", "\u03bb", "\u03b2" },
            storedKeys: new object[] { "g", "g", "g", "g" }, nativeKeys: new object[] { "g", "g", "g" });
        var selected = DistinctGroups(f, h => CosmicQueryMetadata.Value<string?>(h, new[] { "amount" }));
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, selected);
        var result = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(result.IsError, result.Message); Assert.Equal(5L, Assert.Single(result.Result)["Unique"]);
    }

    [Fact]
    public async Task GroupedDistinctRetainsActorMappingAndExactProjectionBinding()
    {
        var f = await Create(typeof(int), true, new object[] { 2 }, new object[] { 3 },
            storedKeys: new object[] { "g" }, nativeKeys: new object[] { "g" });
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, DistinctIntGroups(f));
        Assert.Equal("CQL_ACCESS_INVALID", CosmicMixedProjectionResults.Group(plan.Result, Guid.NewGuid(), f.Mapping, f.Stored, f.Live).ErrorCode);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicMixedProjectionResults.Group(plan.Result, Actor, Guid.NewGuid(), f.Stored, f.Live).ErrorCode);
        var unprojected = DistinctGroups(f, h => h.Version);
        Assert.Equal("CQL_MIXED_QUERY_INVALID", CosmicMixedProjectionResults.PrepareGrouping(f.Query, unprojected).ErrorCode);
    }

    [Fact]
    public async Task GroupedDistinctValidatesOffPageGroupsBeforePublishingOnePage()
    {
        var f = await Create(typeof(int), true, new object[] { 2, 3 }, Array.Empty<object>(),
            storedKeys: new object[] { "a", "b" }, nativeKeys: Array.Empty<object>());
        var selected = DistinctIntGroups(f, maximumGroups: 1).Take(1).Result;
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, selected);
        var result = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.Equal("CQL_RESULT_LIMIT", result.ErrorCode); Assert.Null(result.Result);
    }
}
