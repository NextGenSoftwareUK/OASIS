using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedRecordTests
{
    [Fact]
    public void NamedKeyAndAggregateMaterializeThroughValidatedDetachedRecords()
    {
        var group = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var selected = group.SelectColumns(new[]
        {
            CosmicGroupedBinding<Holon, string>.Key(group, "category", 0).Result,
            CosmicGroupedBinding<Holon, string>.Aggregate(group, "stock", group.Source.Sum(h => h.Version).Result).Result
        });
        Assert.False(selected.IsError, selected.Message);
        var result = CosmicGroupedResults.Materialize(selected.Result,
            new[] { new CosmicGroupedRow(1, new object[] { "coffee" }, "coffee", 7) });
        Assert.False(result.IsError, result.Message);
        var row = Assert.Single(result.Result);
        Assert.Equal(new[] { "category", "stock" }, row.Aliases);
        Assert.Equal("coffee", row["category"]); Assert.Equal(7, row["stock"]);
    }

    [Fact]
    public void IdenticalLookingForeignSourcesCannotSupplyAggregateBindings()
    {
        var group = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var foreign = CosmicQuery<Holon>.Create().Count().Result;
        Assert.True(CosmicGroupedBinding<Holon, string>.Aggregate(group, "count", foreign).IsError);
        Assert.True(CosmicGroupedBinding<Holon, string>.Aggregate(group, "any", group.Source.Any().Result).IsError);
        Assert.True(CosmicGroupedBinding<Holon, string>.Key(group, "bad", 1).IsError);
    }

    [Fact]
    public void BindingsRemainOwnedByTheirGroupingAndAliasesMustBeUnique()
    {
        var source = CosmicQuery<Holon>.Create();
        var first = source.GroupBy(h => h.Name).Result; var second = source.GroupBy(h => h.Name).Result;
        var key = CosmicGroupedBinding<Holon, string>.Key(first, "key", 0).Result;
        Assert.True(second.SelectColumns(new[] { key }).IsError);
        Assert.True(first.SelectColumns(new[] { key, key }).IsError);
        Assert.True(first.SelectColumns(Enumerable.Range(0, 33)
            .Select(i => CosmicGroupedBinding<Holon, string>.Key(first, "k" + i, 0).Result).ToArray()).IsError);
    }

    [Fact]
    public void CallerMutationCannotChangeSelectedAliasesAndInvalidBuffersCannotPublish()
    {
        var group = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var bindings = new[] { CosmicGroupedBinding<Holon, string>.Key(group, "original", 0).Result };
        var selection = group.SelectColumns(bindings).Result;
        bindings[0] = CosmicGroupedBinding<Holon, string>.Key(group, "mutated", 0).Result;
        Assert.Equal("original", selection.Columns[0].Alias);
        var rejected = CosmicGroupedResults.Materialize(selection,
            new[] { new CosmicGroupedRow(1, new object[] { "coffee" }, 12) });
        Assert.True(rejected.IsError); Assert.Null(rejected.Result);
    }
}
