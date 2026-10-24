using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedFilterCaptureTests
{
    [Fact]
    public void TypedFilterSharesSourceGrammarAndSnapshotsClosedScalars()
    {
        var minimum = 3;
        var original = CosmicQuery<Holon>.Create().Where(h => h.Name != "excluded")
            .GroupBy(h => h.Name).Select(g => new { Name = g.Key, Rows = g.Count() }).Result;
        var captured = original.Where(row => row.Rows >= minimum && row.Name.StartsWith("c", StringComparison.Ordinal));
        Assert.False(captured.IsError, captured.Message); minimum = 99;
        Assert.Same(original.Source, captured.Result.Source); Assert.Empty(original.ResultPredicates);
        var logical = Assert.IsType<CosmicQueryLogical>(Assert.Single(captured.Result.ResultPredicates));
        Assert.Equal("Rows", Assert.IsType<CosmicQueryComparison>(logical.Left).Path.Segments.Single());
        Assert.Equal(3L, Assert.IsType<CosmicQueryComparison>(logical.Left).Literal.Value);
        Assert.IsType<CosmicQueryStringMatch>(logical.Right);
        var paged = captured.OrderBy(row => row.Rows).Skip(1).Take(2).Result;
        Assert.Same(logical, Assert.Single(paged.ResultPredicates)); Assert.Equal(1, paged.ResultOffset);
    }

    [Fact]
    public void ScalarAndExplicitRecordColumnsAreCapturedWithoutExecutableAccess()
    {
        var scalar = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).Where(rows => rows > 2L);
        Assert.False(scalar.IsError, scalar.Message);
        var grouping = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var record = grouping.SelectColumns(new[] {
            CosmicGroupedBinding<Holon, string>.Aggregate(grouping, "Rows", grouping.Source.LongCount().Result).Result }).Result;
        Assert.False(record.Where(row => (long)row["Rows"] > 2L).IsError);
        Assert.True(record.Where(row => (int)row["Rows"] > 2).IsError);
        Assert.True(record.Where(row => (long)row["missing"] > 2L).IsError);
    }

    [Fact]
    public void SelectedFieldComparisonMembershipAndNullableGrammarRemainShared()
    {
        var members = new[] { "coffee", "tea" };
        var selected = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Name = g.Key, Rows = g.LongCount(), Total = g.Sum(h => h.Version), Mean = g.Average(h => h.Version) }).Result;
        var query = selected.Where(row => members.Contains(row.Name) && row.Rows > row.Total && row.Mean >= 1.5m);
        Assert.False(query.IsError, query.Message); members[0] = "changed";
        var top = Assert.IsType<CosmicQueryLogical>(Assert.Single(query.Result.ResultPredicates));
        var left = Assert.IsType<CosmicQueryLogical>(top.Left);
        Assert.Equal("coffee", Assert.IsType<CosmicQueryMembership>(left.Left).Values[0].Value);
        Assert.IsType<CosmicQueryFieldComparison>(left.Right);
    }

    [Fact]
    public void ArbitraryMethodsGettersAndUnselectedFieldsCannotExecute()
    {
        var calls = new Trap();
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => new { Name = g.Key, Rows = g.Count() }).Result;
        Assert.True(query.Where(row => row.Rows > calls.Value).IsError);
        Assert.True(query.Where(row => calls.Match(row.Name)).IsError);
        Assert.Equal(0, calls.Calls);
        Assert.True(query.Where(row => row.Name.ToLower() == "x").IsError);
    }

    [Fact]
    public void PostWindowFilteringAndReorderingCannotSilentlyChangeOperatorOrder()
    {
        var selected = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).Result;
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED", selected.Take(1).Where(rows => rows > 1).ErrorCode);
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED", selected.Skip(1).OrderBy(rows => rows).ErrorCode);
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED", selected.Take(0).OrderByDescending(rows => rows).ErrorCode);
        Assert.False(selected.Where(rows => rows > 1).OrderBy(rows => rows).Take(1).IsError);
    }

    [Fact]
    public void ExistingGroupedCapabilitiesCannotSilentlyIgnoreResultFilters()
    {
        var selected = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).Where(rows => rows > 1).Result;
        var fields = new CosmicProjectionCapabilities(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
        var profile = new CosmicGroupedCapabilities(fields, new CosmicScalarCapabilities(new[] { CosmicScalarOperation.LongCount }));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", profile.Admit(selected).ErrorCode);
    }

    [Fact]
    public void RepeatedFiltersRemainBoundedAndPreserveEarlierErrors()
    {
        var selected = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).Result;
        for (var index = 0; index < 32; index++) selected = selected.Where(rows => rows > 0).Result;
        Assert.Equal(32, selected.ResultPredicates.Count);
        Assert.Equal("CQL_QUERY_LIMIT", selected.Where(rows => rows > 0).ErrorCode);
        Assert.Equal("CQL_QUERY_LIMIT", selected.Where(rows => rows > 0).Take(1).ErrorCode);
    }


    [Fact]
    public void SingleColumnDtoCannotBeMistakenForItsScalarCell()
    {
        var dto = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => new { Rows = g.LongCount() }).Result;
        Assert.True(dto.Where(row => row != null).IsError);
        Assert.True(dto.OrderBy(row => row).IsError);
    }

    [Fact]
    public void ManualMaterializationCannotIgnoreCapturedFilters()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).Where(rows => rows > 3).Result;
        var result = CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(2, new object[] { "A" }, 2L) });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode);
    }

    private sealed class Trap
    {
        internal int Calls;
        internal int Value { get { Calls++; return 1; } }
        internal bool Match(string name) { Calls++; return true; }
    }
}
