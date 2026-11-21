using System;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlGroupedGuidV1CompilerTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void JsonGuidDistinctCountPreservesExactTypeAndFrozenPath(bool metadata, bool nullable)
    {
        var request = new CqlGroupedV1Request
        {
            Keys = new[] { new CqlProjectionV1Column { Alias = "Category", Field = "Name" } },
            Columns = new[] { new CqlGroupedV1Column
            {
                Alias = "Unique", Operation = "LongCount", Distinct = true,
                Selection = metadata
                    ? new CqlScalarV1Selection { MetadataPath = new[] { "stock" }, ValueType = "Guid", Nullable = nullable }
                    : new CqlScalarV1Selection { Field = "Id" }
            } }
        };
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var decoded = JsonSerializer.Deserialize<CqlGroupedV1Request>(JsonSerializer.Serialize(request, options), options);
        Assert.NotNull(decoded);
        var compiled = CqlV1Compiler.CompileGrouped(decoded, Guid.NewGuid(), null);
        decoded.Columns[0].Distinct = false;
        if (metadata) decoded.Columns[0].Selection.MetadataPath[0] = "changed";
        else decoded.Columns[0].Selection.Field = "Name";

        var column = Assert.Single(compiled.Columns);
        Assert.True(column.Distinct);
        Assert.Equal(CosmicScalarOperation.LongCount, column.Operation);
        Assert.Equal(typeof(long), column.ValueType);
        Assert.Equal(metadata, column.Selector.IsMetadata);
        Assert.Equal(metadata && nullable ? typeof(Guid?) : typeof(Guid), column.Selector.ValueType);
        Assert.Equal(metadata ? "stock" : "Id", Assert.Single(column.Selector.Segments));
    }

    [Fact]
    public void CanonicalGuidDistinctCountCannotOverrideItsInferredType()
    {
        var request = new CqlGroupedV1Request
        {
            Keys = new[] { new CqlProjectionV1Column { Alias = "Category", Field = "Name" } },
            Columns = new[] { new CqlGroupedV1Column
            {
                Alias = "Unique", Operation = "LongCount", Distinct = true,
                Selection = new CqlScalarV1Selection { Field = "Id", ValueType = "Guid", Nullable = true }
            } }
        };
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
    }
}
