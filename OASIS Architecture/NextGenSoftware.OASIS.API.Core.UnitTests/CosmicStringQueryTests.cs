using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicStringQueryTests
{
    [Fact]
    public void OrdinalStringPredicatesCapturePatternsAndComposeWithoutClientExecution()
    {
        var pattern = "%_'; --"; var comparison = StringComparison.Ordinal;
        var compiled = CosmicQueryExpression.Compile<Holon>(h => h.Name.Contains(pattern) &&
            (h.Description.StartsWith(pattern, comparison) || !h.Name.EndsWith(pattern, StringComparison.Ordinal)));
        Assert.False(compiled.IsError, compiled.Message); pattern = "changed";
        var root = Assert.IsType<CosmicQueryLogical>(compiled.Result);
        var contains = Assert.IsType<CosmicQueryStringMatch>(root.Left);
        Assert.Equal(CosmicQueryStringOperator.Contains, contains.Operator); Assert.Equal("%_'; --", contains.Value);
        var alternatives = Assert.IsType<CosmicQueryLogical>(root.Right);
        Assert.Equal(CosmicQueryStringOperator.StartsWith, Assert.IsType<CosmicQueryStringMatch>(alternatives.Left).Operator);
        Assert.Equal(CosmicQueryStringOperator.EndsWith, Assert.IsType<CosmicQueryStringMatch>(Assert.IsType<CosmicQueryNot>(alternatives.Right).Operand).Operator);
    }

    [Fact]
    public void StringComparisonAdmissionRejectsCultureNullInvalidUnicodeAndOversizedPatterns()
    {
        Assert.True(CosmicQueryExpression.Compile<Holon>(h => h.Name.StartsWith("x")).IsError);
        Assert.True(CosmicQueryExpression.Compile<Holon>(h => h.Name.Contains("x", StringComparison.OrdinalIgnoreCase)).IsError);
        string? missing = null;
        var nullPattern = CosmicQueryExpression.Compile<Holon>(h => h.Name.Contains(missing!));
        Assert.Equal("CQL_EXPRESSION_INVALID", nullPattern.ErrorCode);
        var invalid = new string((char)0xD800, 1);
        Assert.Equal("CQL_EXPRESSION_INVALID", CosmicQueryExpression.Compile<Holon>(h => h.Name.Contains(invalid)).ErrorCode);
        var oversized = new string('x', CosmicQueryExpression.MaximumStringLength + 1);
        Assert.Equal("CQL_EXPRESSION_INVALID", CosmicQueryExpression.Compile<Holon>(h => h.Name.Contains(oversized)).ErrorCode);
    }

    [Fact]
    public void StringCapabilitiesAreExactPerFieldAndOperatorAndRemainImmutable()
    {
        var operations = new[] { CosmicQueryStringOperator.Contains };
        var field = new CosmicQueryFieldCapability("Name", typeof(string), Array.Empty<CosmicQueryComparisonOperator>(), operations);
        var capability = new CosmicQueryCapabilities(CosmicQueryFeatures.None, field); operations[0] = CosmicQueryStringOperator.EndsWith;
        Assert.False(capability.Admit(CosmicQuery<Holon>.Create().Where(h => h.Name.Contains("x")).Result).IsError);
        Assert.True(capability.Admit(CosmicQuery<Holon>.Create().Where(h => h.Description.Contains("x")).Result).IsError);
        Assert.True(capability.Admit(CosmicQuery<Holon>.Create().Where(h => h.Name.EndsWith("x", StringComparison.Ordinal)).Result).IsError);
        Assert.Equal(CosmicQueryStringOperator.Contains, Assert.Single(field.StringOperations));
        Assert.Throws<ArgumentException>(() => new CosmicQueryFieldCapability("Version", typeof(int), Array.Empty<CosmicQueryComparisonOperator>(), new[] { CosmicQueryStringOperator.Contains }));
    }

    [Fact]
    public void BroadStringAdmissionRequiresExplicitOperatorAndMetadataCapabilities()
    {
        var contains = CosmicQuery<Holon>.Create().Where(h => h.Name.Contains("x")).Result;
        var metadata = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<string>(h, "title").Contains("x")).Result;
        var profile = new CosmicQueryCapabilities(CosmicQueryFeatures.StringContains, new[] { typeof(string) }, Array.Empty<CosmicQueryComparisonOperator>());
        Assert.False(profile.Admit(contains).IsError); Assert.True(profile.Admit(metadata).IsError);
        var full = new CosmicQueryCapabilities(CosmicQueryFeatures.StringContains | CosmicQueryFeatures.Metadata, new[] { typeof(string) }, Array.Empty<CosmicQueryComparisonOperator>());
        Assert.False(full.Admit(metadata).IsError);
        Assert.True(new CosmicQueryCapabilities(CosmicQueryFeatures.None, new[] { typeof(string) }, Array.Empty<CosmicQueryComparisonOperator>()).Admit(contains).IsError);
    }
}
