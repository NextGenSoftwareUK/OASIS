using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlMetadataPresenceWireTests
{
    private static CosmicQueryPredicate Compile(CqlV1Filter filter)
        => Assert.Single(CqlV1Compiler.Compile(new CqlV1Request { Filters = new[] { filter } },
            Guid.NewGuid(), null).BusinessPredicates);

    [Fact]
    public void MissingNegatesOnlyPathExistence()
    {
        var test = Assert.IsType<CosmicQueryNot>(Compile(new CqlV1Filter
        {
            MetadataPath = new[] { "nested", "value" }, Operator = CqlV1Operator.IsMissing
        }));
        var path = Assert.IsType<CosmicQueryMetadataTest>(test.Operand);
        Assert.Equal(CosmicQueryMetadataTestKind.Exists, path.Kind);
        Assert.Equal(new[] { "nested", "value" }, path.Segments);
    }

    [Fact]
    public void NotNullRequiresPresenceAsWellAsNegatedNull()
    {
        var test = Assert.IsType<CosmicQueryLogical>(Compile(new CqlV1Filter
        {
            MetadataPath = new[] { "nested", "value" }, Operator = CqlV1Operator.IsNotNull
        }));
        Assert.True(test.IsAnd);
        var exists = Assert.IsType<CosmicQueryMetadataTest>(test.Left);
        var not = Assert.IsType<CosmicQueryNot>(test.Right);
        var isNull = Assert.IsType<CosmicQueryMetadataTest>(not.Operand);
        Assert.Equal(CosmicQueryMetadataTestKind.Exists, exists.Kind);
        Assert.Equal(CosmicQueryMetadataTestKind.IsNull, isNull.Kind);
        Assert.Equal(exists.Segments.ToArray(), isNull.Segments.ToArray());
    }

    [Theory]
    [InlineData(CqlV1Operator.IsMissing)]
    [InlineData(CqlV1Operator.IsNotNull)]
    public void PresenceTestsRejectNonTextCanonicalFieldsAndLiteralValues(CqlV1Operator operation)
    {
        Assert.Throws<ArgumentException>(() => Compile(new CqlV1Filter { Field = "Version", Operator = operation }));
        if (operation == CqlV1Operator.IsMissing)
            Assert.Throws<ArgumentException>(() => Compile(new CqlV1Filter { Field = "Name", Operator = operation }));
        Assert.Throws<ArgumentException>(() => Compile(new CqlV1Filter
        {
            Field = "Name", Operator = operation, Value = JsonSerializer.SerializeToElement(true)
        }));
        Assert.Throws<ArgumentException>(() => Compile(new CqlV1Filter
        {
            MetadataPath = new[] { "value" }, Operator = operation, Value = JsonSerializer.SerializeToElement(true)
        }));
    }
}
