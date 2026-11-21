using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlStringLengthWireTests_NullablePresence
{
    private static int GetterCalls;
    private static bool HasValue { get { GetterCalls++; return true; } }

    [Fact]
    public void ArbitraryHasValueGetterIsRejectedWithoutExecution()
    {
        GetterCalls = 0;
        var result = CosmicQueryExpression.Compile<Holon>(row => HasValue);
        Assert.True(result.IsError);
        Assert.Equal(0, GetterCalls);
    }

    [Fact]
    public void SourceNullableMetadataPresenceUsesTheSameNullComparison()
    {
        var result = CosmicQueryExpression.Compile<Holon>(
            row => CosmicQueryMetadata.Value<int?>(row, "optional").HasValue);
        Assert.False(result.IsError, result.Message);
        var comparison = Assert.IsType<CosmicQueryComparison>(result.Result);
        Assert.True(comparison.Path.IsMetadata);
        Assert.Equal(typeof(int?), comparison.Path.ValueType);
        Assert.Equal(CosmicQueryComparisonOperator.NotEqual, comparison.Operator);
        Assert.Equal(CosmicQueryScalarKind.Null, comparison.Literal.Kind);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SelectedNullablePresenceBindsARealNullComparison(bool negate)
    {
        var selected = CosmicQuery<Holon>.Create()
            .Select(row => CosmicQueryMetadata.Value<int?>(row, "optional")).Result;
        Expression<Func<int?, bool>> predicate = negate ? value => !value.HasValue : value => value.HasValue;
        var query = selected.Where(predicate);
        Assert.False(query.IsError, query.Message);
        var stage = Assert.IsType<CosmicSelectedFilterStage>(Assert.Single(query.Result.ResultStages));
        var comparison = Assert.IsType<CosmicQueryComparison>(
            negate ? Assert.IsType<CosmicQueryNot>(stage.Predicate).Operand : stage.Predicate);
        Assert.Equal(CosmicQueryComparisonOperator.NotEqual, comparison.Operator);
        Assert.Equal(CosmicQueryScalarKind.Null, comparison.Literal.Kind);
        Assert.Equal(typeof(int?), comparison.Path.ValueType);
    }

    [Fact]
    public void SourceNullablePresenceUsesTheSameBoundedComparisonGrammar()
    {
        var captured = CosmicQueryExpression.Compile<Holon>(
            row => CosmicQueryMetadata.Value<Guid?>(row, "optional").HasValue);
        Assert.False(captured.IsError, captured.Message);
        var comparison = Assert.IsType<CosmicQueryComparison>(captured.Result);
        Assert.True(comparison.Path.IsMetadata);
        Assert.Equal(CosmicQueryComparisonOperator.NotEqual, comparison.Operator);
        Assert.Equal(typeof(Guid?), comparison.Path.ValueType);
        Assert.Null(comparison.Literal.Value);
    }

    private static class CustomPresence
    {
        public static int Calls;
        public static bool HasValue { get { Calls++; throw new InvalidOperationException(); } }
    }

    [Fact]
    public void PresenceNameDoesNotAuthorizeAnApplicationGetter()
    {
        CustomPresence.Calls = 0;
        var captured = CosmicQueryExpression.Compile<Holon>(row => CustomPresence.HasValue);
        Assert.True(captured.IsError);
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED", captured.ErrorCode);
        Assert.Equal(0, CustomPresence.Calls);
    }
}
