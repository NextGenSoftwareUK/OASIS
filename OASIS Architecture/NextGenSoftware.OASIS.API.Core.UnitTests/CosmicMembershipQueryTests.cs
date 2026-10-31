using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicMembershipQueryTests
{
    [Fact]
    public void ScalarArraysFreezeValuesAndInitializedArraysHaveImmutableLiteralNodes()
    {
        var values = new string?[] { "Coffee", null, "%_'; --" };
        System.Linq.Expressions.Expression<Func<Holon, bool>> expression = h => values.Contains(h.Name);
        var result = CosmicQueryExpression.Compile(expression);
        var call = (System.Linq.Expressions.MethodCallExpression)expression.Body;
        Assert.False(result.IsError, result.Message + " | " + call.Method.DeclaringType + "." + call.Method + " | source=" + call.Arguments[0].NodeType + " " + call.Arguments[0]); values[0] = "changed";
        var membership = Assert.IsType<CosmicQueryMembership>(result.Result);
        Assert.Equal("Coffee", membership.Values[0].Value); Assert.Equal(CosmicQueryScalarKind.Null, membership.Values[1].Kind);
        Assert.Throws<NotSupportedException>(() => ((IList<CosmicQueryLiteral>)membership.Values).Clear());
        var initialized = CosmicQueryExpression.Compile<Holon>(h => new[] { 1, 2, 3 }.Contains(h.Version));
        Assert.False(initialized.IsError, initialized.Message);
        Assert.Equal(new object[] { 1L, 2L, 3L }, Assert.IsType<CosmicQueryMembership>(initialized.Result).Values.Select(v => v.Value));
    }

    private sealed class Probe : IEnumerable<string>
    {
        public int Calls;
        public IEnumerator<string> GetEnumerator() { Calls++; throw new InvalidOperationException("must not enumerate"); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    [Fact]
    public void ArbitraryEnumerablesComparersCoercionsAndOversizedListsRejectWithoutExecution()
    {
        var probe = new Probe(); Assert.True(CosmicQueryExpression.Compile<Holon>(h => probe.Contains(h.Name)).IsError); Assert.Equal(0, probe.Calls);
        Assert.True(CosmicQueryExpression.Compile<Holon>(h => new[] { "x" }.Contains(h.Name, StringComparer.OrdinalIgnoreCase)).IsError);
        var decimals = new[] { 1.000000000000000000000000001m };
        Assert.True(CosmicQueryExpression.Compile<Holon>(h => decimals.Contains((decimal)h.Version)).IsError);
        var oversized = new string[CosmicQueryExpression.MaximumMembershipValues + 1];
        Assert.Equal("CQL_EXPRESSION_INVALID", CosmicQueryExpression.Compile<Holon>(h => oversized.Contains(h.Name)).ErrorCode);
        var empty = Array.Empty<string>();
        Assert.Empty(Assert.IsType<CosmicQueryMembership>(CosmicQueryExpression.Compile<Holon>(h => empty.Contains(h.Name)).Result).Values);
    }

    [Fact]
    public void EnumAndUnicodeMembershipLiteralsUseTheCanonicalValidationBoundary()
    {
        var values = new[] { HolonType.Quest }; var result = CosmicQueryExpression.Compile<Holon>(h => values.Contains(h.HolonType));
        Assert.False(result.IsError, result.Message); values[0] = (HolonType)int.MaxValue;
        Assert.Equal(Convert.ToInt64(HolonType.Quest), Assert.IsType<CosmicQueryMembership>(result.Result).Values[0].Value);
        Assert.Equal("CQL_EXPRESSION_INVALID", CosmicQueryExpression.Compile<Holon>(h => values.Contains(h.HolonType)).ErrorCode);
        var invalid = new string((char)0xD800, 1); var strings = new[] { invalid };
        Assert.Equal("CQL_EXPRESSION_INVALID", CosmicQueryExpression.Compile<Holon>(h => strings.Contains(h.Name)).ErrorCode);
        Assert.Equal("CQL_EXPRESSION_INVALID", CosmicQueryExpression.Compile<Holon>(h => h.Name == invalid).ErrorCode);
        Assert.Equal("CQL_EXPRESSION_INVALID", CosmicQueryExpression.Compile<Holon>(h => CosmicQueryMetadata.Exists(h, invalid)).ErrorCode);
    }

    [Fact]
    public void MembershipRequiresExplicitCapabilityEqualityFieldAndNullAdmission()
    {
        var values = new string?[] { null, "Coffee" }; var query = CosmicQuery<Holon>.Create().Where(h => values.Contains(h.Name)).Result;
        var field = new CosmicQueryFieldCapability("Name", typeof(string), CosmicQueryComparisonOperator.Equal);
        Assert.True(new CosmicQueryCapabilities(CosmicQueryFeatures.NullLiteral, field).Admit(query).IsError);
        Assert.True(new CosmicQueryCapabilities(CosmicQueryFeatures.Membership, field).Admit(query).IsError);
        Assert.False(new CosmicQueryCapabilities(CosmicQueryFeatures.Membership | CosmicQueryFeatures.NullLiteral, field).Admit(query).IsError);
        var other = new CosmicQueryFieldCapability("Description", typeof(string), CosmicQueryComparisonOperator.Equal);
        Assert.True(new CosmicQueryCapabilities(CosmicQueryFeatures.Membership | CosmicQueryFeatures.NullLiteral, other).Admit(query).IsError);
    }

    [Fact]
    public void NormalizedArrayConversionsStillConsumeTheCompilerDepthBudget()
    {
        var root = System.Linq.Expressions.Expression.Parameter(typeof(Holon), "h");
        System.Linq.Expressions.Expression array = System.Linq.Expressions.Expression.Constant(new[] { "x" });
        for (var index = 0; index < CosmicQueryExpression.MaximumDepth + 8; index++)
            array = System.Linq.Expressions.Expression.Convert(array, typeof(string[]));
        var contains = typeof(Enumerable).GetMethods().Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2).MakeGenericMethod(typeof(string));
        var call = System.Linq.Expressions.Expression.Call(contains, array, System.Linq.Expressions.Expression.Property(root, nameof(Holon.Name)));
        var predicate = System.Linq.Expressions.Expression.Lambda<Func<Holon, bool>>(call, root);
        var result = CosmicQueryExpression.Compile(predicate);
        Assert.True(result.IsError); Assert.Equal("CQL_EXPRESSION_INVALID", result.ErrorCode);
    }

    [Fact]
    public void MixedNumericComparisonsRequireExplicitAdapterSemantics()
    {
        var value = 1.000000000000000000000000001m;
        var query = CosmicQuery<Holon>.Create().Where(h => (decimal)h.Version == value);
        Assert.False(query.IsError, query.Message);
        var field = new CosmicQueryFieldCapability("Version", typeof(int), CosmicQueryComparisonOperator.Equal);
        Assert.True(new CosmicQueryCapabilities(CosmicQueryFeatures.None, field).Admit(query.Result).IsError);
        Assert.False(new CosmicQueryCapabilities(CosmicQueryFeatures.MixedNumericComparison, field).Admit(query.Result).IsError);
    }
}
