using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedColumnsTests
{
    private static CosmicProjectionSelection<Holon> Key(string alias) => CosmicProjectionSelection<Holon>.Create(alias, h => h.Name).Result;

    [Fact]
    public void NamedKeysRetainSourceAndSnapshotTheCallerArray()
    {
        var source = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Skip(3).Take(7).Result;
        var keys = new[] { Key("category"), CosmicProjectionSelection<Holon>.Create("revision", h => h.Version).Result };
        var grouped = source.GroupByColumns(keys);
        Assert.False(grouped.IsError, grouped.Message);
        keys[0] = Key("changed");
        Assert.Same(source, grouped.Result.Source);
        Assert.Equal(new[] { "category", "revision" }, grouped.Result.Key.Columns.Select(c => c.Alias));
        Assert.Equal(3, grouped.Result.Source.Offset); Assert.Equal(7, grouped.Result.Source.Limit);
    }

    [Fact]
    public void BothTypedAndNamedKeysEnforceEightColumnMaximum()
    {
        var source = CosmicQuery<Holon>.Create();
        var tooMany = source.GroupByColumns(Enumerable.Range(0, 9).Select(i => Key("key" + i)).ToArray());
        Assert.True(tooMany.IsError); Assert.Equal("CQL_PROJECTION_UNSUPPORTED", tooMany.ErrorCode);
        Assert.False(source.GroupByColumns(Enumerable.Range(0, 8).Select(i => Key("key" + i)).ToArray()).IsError);
    }

    [Fact]
    public void InvalidSourceAndDuplicateAliasesCannotBecomeGroupingIntents()
    {
        var duplicate = CosmicQuery<Holon>.Create().GroupByColumns(new[] { Key("same"), Key("same") });
        Assert.True(duplicate.IsError);
        var source = CosmicQuery<Holon>.Create().Take(-1);
        var grouped = source.GroupByColumns(new[] { Key("name") });
        Assert.True(grouped.IsError); Assert.Equal(source.ErrorCode, grouped.ErrorCode);
    }

    [Fact]
    public void ExplicitOutputOptionsRemainOwnedByTheGrouping()
    {
        var options = CosmicGroupingOptions.Create(17, 4096).Result;
        var grouped = CosmicQuery<Holon>.Create().GroupByColumns(new[] { Key("name") }, options);
        Assert.False(grouped.IsError, grouped.Message); Assert.Same(options, grouped.Result.Options);
    }
}
