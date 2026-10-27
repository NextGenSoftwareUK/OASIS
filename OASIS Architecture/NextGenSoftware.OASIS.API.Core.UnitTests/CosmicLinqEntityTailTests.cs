using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityCompositionTests
{
    [Fact]
    public void StandardReversalAndTailIndexingPreserveTheEntirePriorPipeline()
    {
        var native=Data("d","c","b","a");native.ReverseEnabled=true;
        var query=Root(native).OrderBy(row=>row.Name).Take(3).Reverse();
        Assert.Equal(new[] {"c","b","a"},query.ToArray().Select(row=>row.Name));
        Assert.Equal(new[] {"b","c"},query.Take(2).Reverse().ToArray().Select(row=>row.Name));
        Assert.Equal("a",query.Last().Name);Assert.Equal("a",query.ElementAt(^1).Name);
        Assert.Equal("b",query.ElementAt(^2).Name);Assert.Equal("c",query.ElementAt(new Index(0)).Name);
        Assert.Null(query.ElementAtOrDefault(^0));Assert.Null(query.ElementAtOrDefault(^9));
        Assert.Throws<ArgumentOutOfRangeException>(()=>query.ElementAt(^0));
        Assert.Equal("CQL_INDEX_OUT_OF_RANGE",Assert.Throws<CosmicLinqException>(()=>query.ElementAt(^9)).Code);
        Assert.Equal(0,native.RowCalls);
    }
    [Fact]
    public void TailPredicatesAndEmptyDefaultsRemainAfterEarlierWindows()
    {
        var native=Data("a","b","c");native.ReverseEnabled=true;
        var query=Root(native).Take(3).OrderBy(row=>row.Name).Take(2);
        var name="b";Assert.Equal("b",query.Last(row=>row.Name==name).Name);
        name="c";Assert.Null(query.LastOrDefault(row=>row.Name==name));
        Assert.Null(query.Take(0).LastOrDefault());
        Assert.Equal("CQL_SEQUENCE_EMPTY",Assert.Throws<CosmicLinqException>(()=>query.Take(0).Last()).Code);
        Assert.Equal(0,native.RowCalls);
    }
    [Fact]
    public void OrdinaryStagePermissionDoesNotEnrollReverseOrTailOperations()
    {
        var native=Data("a","b");var query=Root(native).Take(2).OrderBy(row=>row.Name);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>query.Last()).Code);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>query.ElementAt(^1)).Code);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>query.Reverse().ToArray()).Code);
        Assert.Equal(0,native.StageCalls);Assert.Equal(0,native.RowCalls);
    }
}
