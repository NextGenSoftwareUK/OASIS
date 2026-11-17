using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicStringLengthTests
{
    [Fact]
    public void CompilationPreservesTheUnderlyingStringDomain()
    {
        var compiled = CosmicQueryExpression.Compile<Holon>(row => row.Name.Length >= 2);
        Assert.False(compiled.IsError, compiled.Message);
        var length = Assert.IsType<CosmicQueryStringLength>(compiled.Result);
        Assert.Equal(typeof(string), length.Path.ValueType);
        Assert.Equal(CosmicQueryScalarKind.String, length.Path.ScalarKind);
        Assert.Equal(new[] { nameof(Holon.Name) }, length.Path.Segments);
        Assert.Equal(CosmicQueryComparisonOperator.GreaterThanOrEqual, length.Operator);
        Assert.Equal(2L, length.Value);
    }

    [Fact]
    public void ReversedLongBoundPreservesTheComparison()
    {
        var compiled = CosmicQueryExpression.Compile<Holon>(row => 3L < row.Name.Length);
        Assert.False(compiled.IsError, compiled.Message);
        var length = Assert.IsType<CosmicQueryStringLength>(compiled.Result);
        Assert.Equal(CosmicQueryComparisonOperator.GreaterThan, length.Operator);
        Assert.Equal(3L, length.Value);
    }

    [Fact]
    public void CapturedBoundsAreFrozenPerCompilation()
    {
        var bound = 2;
        Expression<Func<Holon, bool>> expression = row => row.Name.Length == bound;
        var first = CosmicQueryExpression.Compile(expression);
        bound = 9;
        var second = CosmicQueryExpression.Compile(expression);
        Assert.False(first.IsError, first.Message);
        Assert.False(second.IsError, second.Message);
        Assert.Equal(2L, Assert.IsType<CosmicQueryStringLength>(first.Result).Value);
        Assert.Equal(9L, Assert.IsType<CosmicQueryStringLength>(second.Result).Value);
    }

    [Fact]
    public void FloatingBoundsAndFieldLengthComparisonsRemainUnsupported()
    {
        Assert.True(CosmicQueryExpression.Compile<Holon>(row => row.Name.Length > 1.5).IsError);
        Assert.True(CosmicQueryExpression.Compile<Holon>(row => row.Name.Length == row.Version).IsError);
    }

    [Fact]
    public void NativeAdmissionRequiresTheFeatureAndTheDeclaredField()
    {
        var predicate = CosmicQueryExpression.Compile<Holon>(row => row.Name.Length > 1).Result;
        var name = new CosmicQueryFieldCapability(nameof(Holon.Name), typeof(string));
        Assert.True(new CosmicQueryCapabilities(CosmicQueryFeatures.None, name)
            .AdmitPredicates(new[] { predicate }).IsError);
        Assert.False(new CosmicQueryCapabilities(CosmicQueryFeatures.StringLength, name)
            .AdmitPredicates(new[] { predicate }).IsError);
        Assert.True(new CosmicQueryCapabilities(CosmicQueryFeatures.StringLength,
            new CosmicQueryFieldCapability(nameof(Holon.Description), typeof(string)))
            .AdmitPredicates(new[] { predicate }).IsError);
    }

    [Fact]
    public void MetadataLengthRetainsRootRestrictions()
    {
        var predicate = CosmicQueryExpression.Compile<Holon>(
            row => CosmicQueryMetadata.Value<string>(row, "allowed", "label").Length == 2).Result;
        var capabilities = new CosmicQueryCapabilities(
            CosmicQueryFeatures.StringLength | CosmicQueryFeatures.Metadata,
            Array.Empty<CosmicQueryFieldCapability>(),
            new[] { new CosmicQueryMetadataCapability(typeof(string),
                Array.Empty<CosmicQueryComparisonOperator>()) });
        Assert.False(capabilities.WithMetadataRoots(new[] { "allowed" })
            .AdmitPredicates(new[] { predicate }).IsError);
        Assert.True(capabilities.WithMetadataRoots(new[] { "other" })
            .AdmitPredicates(new[] { predicate }).IsError);
    }
}
