using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicGuidEmptyLiteralTests
{
    private static int _executions;
    private static class UnsafeStatics
    {
        public static readonly Guid Value=Create();
        public static Guid Property=>Create();
        private static Guid Create(){_executions++;return Guid.NewGuid();}
    }

    [Fact]
    public void KnownGuidEmptySentinelCompilesAsAnImmutableGuidLiteral()
    {
        var result=CosmicQueryExpression.Compile<Holon>(h=>h.ParentHolonId==Guid.Empty);
        Assert.False(result.IsError,result.Message);
        var comparison=Assert.IsType<CosmicQueryComparison>(result.Result);
        Assert.Equal(CosmicQueryScalarKind.Guid,comparison.Literal.Kind);
        Assert.Equal(Guid.Empty,comparison.Literal.Value);
    }

    [Fact]
    public void KnownGuidEmptyAlsoWorksInsideLiteralMembership()
    {
        var result=CosmicQueryExpression.Compile<Holon>(h=>new[]{Guid.Empty}.Contains(h.ParentHolonId));
        Assert.False(result.IsError,result.Message);
        Assert.Equal(Guid.Empty,Assert.IsType<CosmicQueryMembership>(result.Result).Values.Single().Value);
    }

    [Fact]
    public void ArbitraryStaticInitializationAndPropertyExecutionRemainRejected()
    {
        Assert.True(CosmicQueryExpression.Compile<Holon>(h=>h.Id==UnsafeStatics.Value).IsError);
        Assert.True(CosmicQueryExpression.Compile<Holon>(h=>h.Id==UnsafeStatics.Property).IsError);
        Assert.True(CosmicQueryExpression.Compile<Holon>(h=>h.Id==Guid.NewGuid()).IsError);
        Assert.Equal(0,_executions);
    }
}
