using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityCountTests
{
    private static IQueryable<Holon> Root(Native native,string mode=HyperDriveModes.V2)
        => CosmicLinq.Create(Manager(mode),native,CosmicQuery<Holon>.Create(
            CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result)).Result;

    [Fact]
    public void StandardStagedCardinalityUsesOnlyNativeCountAndExactResultTypes()
    {
        var native=new Native(); var query=Root(native).Take(3).Where(row=>row.Name=="coffee").Take(2);
        Assert.Equal(1,query.Count()); Assert.Equal(1L,query.LongCount()); Assert.True(query.Any());
        native.Count=0;
        Assert.False(query.Any()); Assert.True(query.All(row=>row.Name=="coffee"));
        var predicate=Assert.IsType<CosmicEntityFilterStage<Holon>>(native.Seen!.Query.Stages.Last()).Predicate.Predicates.Single();
        Assert.IsType<CosmicQueryNot>(predicate);
        Assert.False(CosmicSelectedPredicateEvaluation.Evaluate(predicate,_=>"coffee",CancellationToken.None));
        Assert.True(CosmicSelectedPredicateEvaluation.Evaluate(predicate,_=>"tea",CancellationToken.None));
        native.Count=1; Assert.False(query.All(row=>row.Name=="coffee"));
        Assert.Equal(6,native.Calls);
    }

    [Fact]
    public void TerminalPredicatesRemainAfterEveryWindowAndRebindAtExecution()
    {
        var native=new Native(); var target="coffee";
        var query=Root(native).Take(3).Where(row=>row.Name=="coffee").Take(1);
        Assert.Equal(1,query.Count(row=>row.Name==target));
        Assert.Equal(3,native.Seen!.Query.Source.Limit); Assert.Equal(3,native.Seen.Query.Stages.Count);
        Assert.IsType<CosmicEntityWindowStage>(native.Seen.Query.Stages[1]);
        bool Matches(string value) => CosmicSelectedPredicateEvaluation.Evaluate(
            Assert.IsType<CosmicEntityFilterStage<Holon>>(native.Seen!.Query.Stages.Last()).Predicate.Predicates.Single(),_=>value,CancellationToken.None);
        Assert.True(Matches("coffee")); Assert.False(Matches("tea"));
        target="tea"; Assert.True(query.Any(row=>row.Name==target));
        Assert.True(Matches("tea")); Assert.False(Matches("coffee"));
        Assert.Equal(1L,query.LongCount(row=>row.Name==target));
    }

    [Fact]
    public void UnsupportedResultTypesAndUnintegratedTerminalsCannotPerformIo()
    {
        var native=new Native(); var query=Root(native).Take(3).Where(row=>row.Name=="coffee");
        var count=Expression.Call(typeof(Queryable),nameof(Queryable.Count),new[] {typeof(Holon)},query.Expression);
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>query.Provider.Execute<string>(count)).Code);
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>query.First()).Code);
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>query.Select(row=>row.Name)).Code);
        Assert.Equal(0,native.Calls);
    }

    [Fact]
    public void StandardStagedCountPreservesV2CapabilityIdentityAndNativeErrors()
    {
        var native=new Native(); var query=Root(native).Take(3).Where(row=>row.Name=="coffee");
        native.Enabled=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>query.Count()).Code);
        native.Enabled=true;
        Assert.Equal("CQL_V2_REQUIRED",Assert.Throws<CosmicLinqException>(()=>Root(native,HyperDriveModes.Legacy).Take(3).Where(row=>row.Name=="coffee").Any()).Code);
        native.Identity=ProviderType.MongoDBOASIS;
        Assert.Equal("CQL_EXECUTION_INVALID",Assert.Throws<CosmicLinqException>(()=>query.LongCount()).Code);
        native.Identity=ProviderType.SQLLiteDBOASIS; Assert.Equal(0,native.Calls);
        native.Failure="CQL_QUERY_LIMIT";
        Assert.Equal("CQL_QUERY_LIMIT",Assert.Throws<CosmicLinqException>(()=>query.All(row=>row.Name=="coffee")).Code);
    }
}
