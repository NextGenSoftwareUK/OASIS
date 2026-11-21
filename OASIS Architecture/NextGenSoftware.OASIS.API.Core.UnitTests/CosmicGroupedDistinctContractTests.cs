using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedDistinctContractTests
{
    [Fact]
    public void ExplicitDistinctIntentKeepsTheExistingCountContractIndependent()
    {
        var g = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var all = CosmicGroupedBinding<Holon, string>.CountValues(g, "All", h => h.Version);
        var unique = CosmicGroupedBinding<Holon, string>.CountValues(g, "Unique", h => h.Version, true);
        Assert.False(all.IsError, all.Message); Assert.False(unique.IsError, unique.Message);
        var selected = g.SelectColumns(new[] { all.Result, unique.Result });
        Assert.False(selected.IsError, selected.Message);
        Assert.False(selected.Result.Columns[0].Distinct); Assert.True(selected.Result.Columns[1].Distinct);
        Assert.All(selected.Result.Columns, column => Assert.Equal(typeof(long), column.ValueType));
        Assert.Same(g.Source, selected.Result.Source);
    }

    [Fact]
    public void LegacyCapabilitiesRefuseDistinctUntilTheExactPathDomainIsEnrolled()
    {
        var g = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var ordinary = g.SelectColumns(new[] { CosmicGroupedBinding<Holon, string>.CountValues(g, "All", h => h.Version).Result }).Result;
        var distinct = g.SelectColumns(new[] { CosmicGroupedBinding<Holon, string>.CountValues(g, "Unique", h => h.Version, true).Result }).Result;
        var native = MongoProvider.CqlGroupedCapabilities;
        var legacy = new CosmicGroupedCapabilities(native.KeyCapabilities, native.AggregateCapabilities,
            native.OrderingCapabilities, native.SupportsResultPaging, native.FilteringCapabilities, native.SelectedCountCapabilities);
        Assert.False(legacy.Admit(ordinary).IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", legacy.Admit(distinct).ErrorCode);
        var admitted = new CosmicGroupedCapabilities(native.KeyCapabilities, native.AggregateCapabilities,
            native.OrderingCapabilities, native.SupportsResultPaging, native.FilteringCapabilities,
            native.SelectedCountCapabilities, native.SelectedCountCapabilities);
        Assert.False(admitted.Admit(distinct).IsError);
        Assert.Null(legacy.DistinctSelectedCountCapabilities);
    }

    [Fact]
    public void ComputedDistinctSelectorsAndInvalidAliasesAreRefusedBeforeExecution()
    {
        var g = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        Assert.True(CosmicGroupedBinding<Holon, string>.CountValues(g, "Lengths", h => h.Name.Length, true).IsError);
        Assert.True(CosmicGroupedBinding<Holon, string>.CountValues(g, "", h => h.Version, true).IsError);
        Assert.True(CosmicGroupedBinding<Holon, string>.CountValues<int>(null!, "Values", h => h.Version, true).IsError);
    }

    [Fact]
    public void DistinctColumnsRequireSelectedValueCountAndInt64Output()
    {
        var g = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var selector = CosmicGroupedBinding<Holon, string>.CountValues(g, "Values", h => h.Version).Result.Column.Selector;
        Assert.Throws<ArgumentException>(() => new CosmicGroupedColumn("Rows", -1, CosmicScalarOperation.Count, null, typeof(int), true));
        Assert.Throws<ArgumentException>(() => new CosmicGroupedColumn("Key", 0, null, null, typeof(string), true));
        Assert.Throws<ArgumentException>(() => new CosmicGroupedColumn("Bad", -1, CosmicScalarOperation.LongCount, selector, typeof(int), true));
    }
}
