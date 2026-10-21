using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicFieldComparisonQueryTests
{
    [Fact]
    public void CanonicalFieldsCanBeComparedWithoutExecutingHolonAccessors()
    {
        var result = CosmicQueryExpression.Compile<Holon>(h => h.Name == h.Description);
        Assert.False(result.IsError, result.Message);
        var fields = Assert.IsType<CosmicQueryFieldComparison>(result.Result);
        Assert.Equal("Name", Assert.Single(fields.Left.Segments));
        Assert.Equal("Description", Assert.Single(fields.Right.Segments));
    }

    [Fact]
    public void MetadataFieldsCanBeComparedWithoutExecutingMarkerMethods()
    {
        var result = CosmicQueryExpression.Compile<Holon>(h =>
            CosmicQueryMetadata.Value<long>(h, "usage", "used") <
            CosmicQueryMetadata.Value<long>(h, "usage", "limit"));
        Assert.False(result.IsError, result.Message);
        var fields = Assert.IsType<CosmicQueryFieldComparison>(result.Result);
        Assert.Equal(new[]{"usage","used"}, fields.Left.Segments);
        Assert.Equal(new[]{"usage","limit"}, fields.Right.Segments);
        Assert.Equal(CosmicQueryComparisonOperator.LessThan, fields.Operator);
    }

    [Fact]
    public void NullableAndNonNullableFieldsOfTheSameScalarTypeCanBeCompared()
    {
        var result = CosmicQueryExpression.Compile<Holon>(h =>
            CosmicQueryMetadata.Value<int?>(h, "expected") == h.Version);
        Assert.False(result.IsError, result.Message);
    }

    [Fact]
    public void MixedFieldPrecisionRequiresAnExplicitlySupportedContract()
    {
        var result = CosmicQueryExpression.Compile<Holon>(h =>
            CosmicQueryMetadata.Value<long>(h, "expected") == h.Version);
        Assert.True(result.IsError);
    }

    [Fact]
    public void FieldComparisonAdmissionRequiresBothExactFieldContractsAndFeature()
    {
        var query = CosmicQuery<Holon>.Create().Where(h => h.Name == h.Description).Result;
        var name = new CosmicQueryFieldCapability("Name", typeof(string), CosmicQueryComparisonOperator.Equal);
        var description = new CosmicQueryFieldCapability("Description", typeof(string), CosmicQueryComparisonOperator.Equal);
        Assert.True(new CosmicQueryCapabilities(CosmicQueryFeatures.None, name, description).Admit(query).IsError);
        Assert.True(new CosmicQueryCapabilities(CosmicQueryFeatures.FieldComparison, name).Admit(query).IsError);
        Assert.False(new CosmicQueryCapabilities(CosmicQueryFeatures.FieldComparison, name, description).Admit(query).IsError);
    }

    [Fact]
    public void FieldComparisonAdmissionChecksMetadataFeatureExactTypeAndBothOperators()
    {
        var query = CosmicQuery<Holon>.Create().Where(h =>
            CosmicQueryMetadata.Value<int?>(h,"left") < CosmicQueryMetadata.Value<int>(h,"right")).Result;
        var left = new CosmicQueryMetadataCapability(typeof(int?),new[]{CosmicQueryComparisonOperator.LessThan});
        var wrongRight = new CosmicQueryMetadataCapability(typeof(int),new[]{CosmicQueryComparisonOperator.Equal});
        var right = new CosmicQueryMetadataCapability(typeof(int),new[]{CosmicQueryComparisonOperator.LessThan});
        var features = CosmicQueryFeatures.FieldComparison | CosmicQueryFeatures.Metadata;
        Assert.True(new CosmicQueryCapabilities(CosmicQueryFeatures.FieldComparison,
            Array.Empty<CosmicQueryFieldCapability>(),new[]{left,right}).Admit(query).IsError);
        Assert.True(new CosmicQueryCapabilities(features,
            Array.Empty<CosmicQueryFieldCapability>(),new[]{left,wrongRight}).Admit(query).IsError);
        Assert.False(new CosmicQueryCapabilities(features,
            Array.Empty<CosmicQueryFieldCapability>(),new[]{left,right}).Admit(query).IsError);
    }
}
