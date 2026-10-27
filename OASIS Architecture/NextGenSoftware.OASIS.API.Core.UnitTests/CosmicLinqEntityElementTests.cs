using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityCompositionTests
{
    [Fact]
    public void StandardStagedForwardElementsPreserveOrderingWindowsAndCardinality()
    {
        var native=Data("c","b","a");var root=Root(native).Take(3).OrderBy(row=>row.Name);
        Assert.Equal("a",root.First().Name);
        Assert.Equal("b",root.ElementAt(1).Name);
        Assert.Null(root.ElementAtOrDefault(9));Assert.Null(root.ElementAtOrDefault(-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>root.ElementAt(-1));
        Assert.Equal("CQL_SEQUENCE_MULTIPLE",Assert.Throws<CosmicLinqException>(()=>root.Single()).Code);
        Assert.Equal("a",root.Take(1).Single().Name);
        Assert.Null(root.Take(0).FirstOrDefault());Assert.Null(root.Take(0).SingleOrDefault());
        Assert.Equal("CQL_SEQUENCE_EMPTY",Assert.Throws<CosmicLinqException>(()=>root.Take(0).First()).Code);
        Assert.Equal(0,native.RowCalls);
    }
    [Fact]
    public void StandardElementPredicatesRebindAfterEveryEarlierStage()
    {
        var native=Data("a","b","c");var root=Root(native).Take(3).OrderBy(row=>row.Name).Take(2);
        var name="b";Assert.Equal("b",root.Single(row=>row.Name==name).Name);
        name="c";Assert.Null(root.FirstOrDefault(row=>row.Name==name));
        Assert.Equal(3,native.Seen!.Query.Source.Limit);
        Assert.Equal(2,Assert.IsType<CosmicEntityWindowStage>(native.Seen.Query.Stages[1]).Count);
    }
    [Fact]
    public void ExplicitDefaultsTailIndexesAndForeignResultTypesCannotUseFlatSource()
    {
        var native=Data("a","b");var root=Root(native).Take(2).OrderBy(row=>row.Name);
        Assert.Throws<CosmicLinqException>(()=>root.FirstOrDefault(new Holon()));
        Assert.Throws<CosmicLinqException>(()=>root.ElementAt(^1));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>root.Last()).Code);
        Assert.Equal(0,native.StageCalls);Assert.Equal(0,native.RowCalls);
    }
}
