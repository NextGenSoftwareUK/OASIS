using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityCompositionTests
{
    private static Native Data(params string[] names) => new()
    {
        Data = names.Select((name, index) => (IHolon)new Holon
        { Id = Guid.Parse($"10000000-0000-0000-0000-{index + 1:D12}"), Name = name }).ToArray()
    };
    private static IQueryable<Holon> Root(Native native, string mode = HyperDriveModes.V2)
        => CosmicLinq.Create(Manager(mode), native, Source()).Result;

    [Fact]
    public void StandardEntityStagesDoNotMoveFiltersOrOrderingsAheadOfSourcePage()
    {
        var native = Data("c", "b", "a", "d");
        var root = Root(native);
        Assert.Empty(root.Take(2).Where(row => row.Name == "a").ToArray());
        Assert.Equal(2, native.Seen!.Query.Source.Limit);
        Assert.Empty(native.Seen.Query.Source.Predicates);
        Assert.Equal("a", Assert.Single(root.Take(3).OrderBy(row => row.Name).Take(1).ToArray()).Name);
        Assert.Equal("c", Assert.Single(root.Take(1).OrderBy(row => row.Name).Take(1).ToArray()).Name);
        Assert.Empty(root.Take(3).OrderBy(row => row.Name).Take(1).Where(row => row.Name == "c").ToArray());
        Assert.Equal(0, native.RowCalls);
    }

    [Fact]
    public async Task DeferredEntityStageClosuresRebindAndThenByExtendsOneStage()
    {
        var native = Data("a", "b", "c", "d");
        var desired = "b";
        var query = Root(native).Take(3).Where(row => row.Name == desired)
            .OrderByDescending(row => row.Name).ThenBy(row => row.Name).Skip(-1).Take(2);
        Assert.Equal(0, native.StageCalls);
        desired = "c";
        var first = await query.ToCosmicListAsync();
        Assert.False(first.IsError, first.Message + first.Exception);
        Assert.Equal("c", Assert.Single(first.Result).Name);
        desired = "a";
        Assert.Equal("a", Assert.Single(query.ToArray()).Name);
        Assert.Equal(4, native.Seen!.Query.Stages.Count);
        Assert.Equal(2, Assert.IsType<CosmicEntityOrderStage>(native.Seen.Query.Stages[1]).Orderings.Count);
        Assert.Empty(query.Take(-1).ToArray());
        Assert.Equal(0, native.RowCalls);
    }

    [Fact]
    public async Task EntityStageBridgePreservesCancellationIdentityAndV2Gates()
    {
        var native = Data("a", "b");
        var query = Root(native).Take(2).Where(row => row.Name == "a");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var interrupted = await query.ToCosmicListAsync(cancelled.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", interrupted.ErrorCode);
        native.Identity = ProviderType.MongoDBOASIS;
        Assert.Equal("CQL_EXECUTION_INVALID", Assert.Throws<CosmicLinqException>(() => query.ToArray()).Code);
        native.Identity = ProviderType.SQLLiteDBOASIS;
        Assert.Equal("CQL_V2_REQUIRED", Assert.Throws<CosmicLinqException>(() => Root(native, "legacy").Take(2).Where(row => row.Name == "a").ToArray()).Code);
        native.Enabled = false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Assert.Throws<CosmicLinqException>(() => query.ToArray()).Code);
        Assert.Equal(0, native.StageCalls); Assert.Equal(0, native.RowCalls);
    }

    [Fact]
    public void UnintegratedEntityStageTerminalsAndProjectionsCannotUseFlatSource()
    {
        var native = Data("a", "b");
        var query = Root(native).Take(2).Where(row => row.Name == "a");
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Assert.Throws<CosmicLinqException>(() => query.Count()).Code);
        Assert.Equal("CQL_ORDER_REQUIRED", Assert.Throws<CosmicLinqException>(() => query.Last()).Code);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Assert.Throws<CosmicLinqException>(() => query.Select(row => row.Name).ToArray()).Code);
        Assert.Throws<CosmicLinqException>(() => query.Where((row, index) => true));
        Assert.Throws<CosmicLinqException>(() => query.OrderBy(row => row.Name, StringComparer.Ordinal));
        Assert.Equal(0, native.StageCalls); Assert.Equal(0, native.RowCalls);
    }

    public sealed class StagedTypedRow : Holon { [CustomOASISProperty] public int Rating { get; set; } }

    [Fact]
    public void StagedTypedRowsUseExistingHydrationAndTrackingOwner()
    {
        var native = new Native();
        native.Value.MetaData = new() { ["Rating"] = 7 };
        var source = CosmicQuery<StagedTypedRow>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result);
        var root = CosmicLinq.Create(Manager(), native, source).Result;
        var row = Assert.Single(root.Take(2).Where(value => value.Name == "native").ToArray());
        Assert.Equal(7, row.Rating);
        Assert.False(HolonChangeTracking.HasChanges(row));
        row.Rating = 8;
        Assert.True(HolonChangeTracking.HasChanges(row));
        Assert.Equal(7, native.Value.MetaData["Rating"]);
        Assert.Equal(0, native.RowCalls);
    }
}
