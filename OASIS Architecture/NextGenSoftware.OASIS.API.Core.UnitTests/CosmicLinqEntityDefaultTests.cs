using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityCompositionTests
{
    [Fact]
    public void CapturedElementDefaultsPreserveCallerIdentityAndAreNeverHydrated()
    {
        var native=Data();native.ReverseEnabled=true;
        var query=Root(native).Take(3).OrderBy(row=>row.Name);
        var fallback=new Holon { Name="caller default" };var wasChanged=HolonChangeTracking.HasChanges(fallback);
        Assert.Same(fallback,query.FirstOrDefault(fallback));
        Assert.Same(fallback,query.SingleOrDefault(fallback));
        Assert.Same(fallback,query.LastOrDefault(fallback));
        Assert.Equal("caller default",fallback.Name);Assert.Equal(wasChanged,HolonChangeTracking.HasChanges(fallback));
        Assert.Null(query.FirstOrDefault(defaultValue:(Holon)null!));Assert.Equal(0,native.RowCalls);
    }
    [Fact]
    public void PredicateDefaultsRemainAfterWindowsAndRebindAtExecution()
    {
        var native=Data("a","b");native.ReverseEnabled=true;
        var query=Root(native).Take(2).OrderBy(row=>row.Name).Take(1);
        var fallback=new Holon { Name="fallback" };var name="b";
        Assert.Same(fallback,query.FirstOrDefault(row=>row.Name==name,fallback));
        Assert.Same(fallback,query.SingleOrDefault(row=>row.Name==name,fallback));
        Assert.Same(fallback,query.LastOrDefault(row=>row.Name==name,fallback));
        name="a";
        Assert.Equal("a",query.FirstOrDefault(row=>row.Name==name,fallback).Name);
        Assert.Equal("a",query.SingleOrDefault(row=>row.Name==name,fallback).Name);
        Assert.Equal("a",query.LastOrDefault(row=>row.Name==name,fallback).Name);
        Assert.Equal(0,native.RowCalls);
    }
    [Fact]
    public void DefaultsCannotHideAmbiguityProtectedErrorsOrIncompleteAcknowledgements()
    {
        var native=Data("a","b");var query=Root(native).Take(2).OrderBy(row=>row.Name);
        var fallback=new Holon { Name="fallback" };
        Assert.Equal("CQL_SEQUENCE_MULTIPLE",Assert.Throws<CosmicLinqException>(()=>query.SingleOrDefault(fallback)).Code);
        native.Failure="CQL_QUERY_LIMIT";
        Assert.Equal("CQL_QUERY_LIMIT",Assert.Throws<CosmicLinqException>(()=>query.FirstOrDefault(fallback)).Code);
        native.Failure=null;native.Incomplete=true;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",Assert.Throws<CosmicLinqException>(()=>query.FirstOrDefault(fallback)).Code);
        Assert.Equal("fallback",fallback.Name);Assert.Equal(0,native.RowCalls);
    }
}
