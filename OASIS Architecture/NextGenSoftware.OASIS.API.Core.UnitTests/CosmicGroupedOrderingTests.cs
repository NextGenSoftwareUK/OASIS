using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedOrderingTests
{
    [Fact]
    public void TypedOrderingsCaptureOrdinalsAndDoNotMutateOriginalIntent()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Name = g.Key, Rows = g.LongCount() }).Result;
        var sorted = query.OrderByDescending(g => g.Rows).ThenBy(g => g.Name);
        Assert.False(sorted.IsError, sorted.Message); Assert.Empty(query.Orderings);
        Assert.Equal(2, sorted.Result.Orderings.Count);
        Assert.Equal(1, sorted.Result.Orderings[0].ColumnIndex); Assert.True(sorted.Result.Orderings[0].Descending);
        Assert.Equal(0, sorted.Result.Orderings[1].ColumnIndex); Assert.False(sorted.Result.Orderings[1].Descending);
        Assert.Same(query.Grouping, sorted.Result.Grouping);
        Assert.Single(sorted.Result.OrderBy(g => g.Name).Result.Orderings);
    }

    [Fact]
    public void ComputedAndWidenedKeysCannotBecomeSortColumns()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        Assert.True(query.OrderBy(g => g + 1).IsError);
        Assert.True(query.OrderBy(g => (long)g).IsError);
        Assert.True(query.ThenBy(g => g).IsError);
        Assert.False(query.OrderBy(g => g).IsError);
    }

    [Fact]
    public void NamedRecordOrderingRequiresALiteralAliasAndExactDomain()
    {
        var group = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var query = group.SelectColumns(new[] { CosmicGroupedBinding<Holon, string>.Aggregate(group, "Rows", group.Source.LongCount().Result).Result }).Result;
        Assert.False(query.OrderByDescending(row => (long)row["Rows"]).IsError);
        Assert.True(query.OrderBy(row => (int)row["Rows"]).IsError);
        Assert.True(query.OrderBy(row => (long)row["missing"]).IsError);
    }

    [Fact]
    public void UnenrolledOrderingCannotPassProviderAdmission()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount())
            .OrderBy(g => g).Result;
        var projection = new CosmicProjectionCapabilities(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, System.Array.Empty<System.Type>());
        var capabilities = new CosmicGroupedCapabilities(projection, new CosmicScalarCapabilities(new[] { CosmicScalarOperation.LongCount }));
        var result = capabilities.Admit(query);
        Assert.True(result.IsError); Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
    }
}
