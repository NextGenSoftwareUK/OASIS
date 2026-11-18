using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlStringLengthWireTests_SelectedCount
{
    [Theory]
    [InlineData("CountValues")]
    [InlineData("CountDistinct")]
    public void CountCaptureFreezesTheComputedStringInput(string operation)
    {
        var request = new CqlScalarV1Request { Operation = operation, Selection = new()
        {
            MetadataPath = new[] { "label" }, ValueType = "String", Nullable = true,
            Function = CqlV1Function.Utf16Length
        }};
        var capture = CqlRequestCapture.Capture(request);
        request.Selection.Function = null;
        request.Selection.MetadataPath[0] = "changed";
        Assert.Equal(CqlV1Function.Utf16Length, capture.Selection.Function);
        Assert.Equal("label", Assert.Single(capture.Selection.MetadataPath));
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var roundTrip = JsonSerializer.Deserialize<CqlScalarV1Request>(JsonSerializer.Serialize(capture, options), options);
        Assert.Equal(CqlV1Function.Utf16Length, roundTrip.Selection.Function);
        capture.Selection.Function = null;
        Assert.DoesNotContain("\"function\"", JsonSerializer.Serialize(capture, options));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectedCountKeepsComputedNullFilteringAndDistinctStages(bool distinct)
    {
        var source = CosmicQuery<Holon>.Create();
        var query = CqlV1Compiler.CompileSelectedCount(source, row => (int?)row.Name.Length, distinct);
        var column = Assert.Single(query.Columns);
        Assert.Equal(CosmicProjectionFunction.Utf16Length, column.Function);
        Assert.Equal(typeof(string), column.Path.ValueType);
        Assert.Equal(typeof(int?), column.ValueType);
        Assert.Equal(distinct ? 2 : 1, query.ResultStages.Count);
    }
}
