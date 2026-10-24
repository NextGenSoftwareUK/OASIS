using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedOrderedResultTests
{
    private static CosmicGroupedSelection<Holon, string, CosmicProjectionRecord> Query()
    {
        var group = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        return group.SelectColumns(new[]
        {
            CosmicGroupedBinding<Holon, string>.Key(group, "Name", 0).Result,
            CosmicGroupedBinding<Holon, string>.Aggregate(group, "Rows", group.Source.LongCount().Result).Result
        }).Result.OrderByDescending(row => (long)row["Rows"]).Result;
    }

    [Fact]
    public void SelectedDescendingOrderCanDifferFromNaturalIdentityOrder()
    {
        var result = CosmicGroupedResults.Materialize(Query(), new[]
        {
            new CosmicGroupedRow(3, new object[] { "B" }, "B", 3L),
            new CosmicGroupedRow(2, new object[] { "A" }, "A", 2L)
        });
        Assert.False(result.IsError, result.Message);
        Assert.Equal("B", result.Result[0]["Name"]);
    }

    [Fact]
    public void TiesRequireCompleteIdentityAscending()
    {
        var correct = new[] { new CosmicGroupedRow(2, new object[] { "A" }, "A", 2L),
            new CosmicGroupedRow(2, new object[] { "B" }, "B", 2L) };
        Assert.False(CosmicGroupedResults.Materialize(Query(), correct).IsError);
        Assert.True(CosmicGroupedResults.Materialize(Query(), correct.Reverse().ToArray()).IsError);
    }

    [Fact]
    public void NonadjacentDuplicateIdentitiesCannotHideBehindAggregateOrdering()
    {
        var result = CosmicGroupedResults.Materialize(Query(), new[]
        {
            new CosmicGroupedRow(3, new object[] { "A" }, "A", 3L),
            new CosmicGroupedRow(2, new object[] { "B" }, "B", 2L),
            new CosmicGroupedRow(1, new object[] { "A" }, "A", 1L)
        });
        Assert.True(result.IsError); Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode); Assert.Null(result.Result);
    }

    [Fact]
    public void WrongExactCellTypesRejectBeforeComparisonOrPublication()
    {
        var result = CosmicGroupedResults.Materialize(Query(), new[]
        {
            new CosmicGroupedRow(3, new object[] { "A" }, "A", 3L),
            new CosmicGroupedRow(2, new object[] { "B" }, "B", 2)
        });
        Assert.True(result.IsError); Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode); Assert.Null(result.Result);
    }

    [Fact]
    public void NullableInt64SortingPreservesNullAndBeyondBinary64Distinctions()
    {
        var group = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var selected = group.SelectColumns(new[]
        {
            CosmicGroupedBinding<Holon, string>.Aggregate(group, "Score",
                group.Source.Min(h => CosmicQueryMetadata.Value<long?>(h, "score")).Result).Result
        }).Result;
        var rows = new[]
        {
            new CosmicGroupedRow(1, new object[] { "C" }, new object[] { null! }),
            new CosmicGroupedRow(1, new object[] { "B" }, 9007199254740992L),
            new CosmicGroupedRow(1, new object[] { "A" }, 9007199254740993L)
        };
        var ascending = selected.OrderBy(row => (long?)row["Score"]).Result;
        var descending = selected.OrderByDescending(row => (long?)row["Score"]).Result;
        Assert.False(CosmicGroupedResults.Materialize(ascending, rows).IsError);
        Assert.False(CosmicGroupedResults.Materialize(descending, rows.Reverse().ToArray()).IsError);
        Assert.True(CosmicGroupedResults.Materialize(ascending, rows.Reverse().ToArray()).IsError);
    }
}
