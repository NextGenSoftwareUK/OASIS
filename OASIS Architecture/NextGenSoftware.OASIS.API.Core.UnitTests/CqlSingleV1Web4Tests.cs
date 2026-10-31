using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    [Theory]
    [InlineData("-3.4028235E+38")] [InlineData("-1E-45")]
    [InlineData("0")] [InlineData("1E-45")]
    [InlineData("1.0000001")] [InlineData("3.4028235E+38")]
    public async Task SingleProjectionHttpReturnsRoundTripStringsWithOneReceipt(string text)
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        var value = float.Parse(text, CultureInfo.InvariantCulture);
        var adapter = ProjectionAdapter(source, ProviderType.MongoDBOASIS, new[] { new CosmicProjectionRow(value) });
        adapter.SetupGet(provider => provider.ProjectionCapabilities).Returns(new CosmicProjectionCapabilities(
            Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(float) }));
        var request = new CqlProjectionV1Request { Columns = new[] { new CqlProjectionV1Column
            { Alias = "Reading", MetadataPath = new[] { "reading" }, ValueType = "Single" } } };
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source));
        using var document = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
        var body = document.RootElement.GetProperty("result");
        Assert.Equal(text, body.GetProperty("rows")[0][0].GetString());
        Assert.Equal("Single", body.GetProperty("columns")[0].GetProperty("valueType").GetString());
        Assert.Single(admitted.Receipts);
    }

    [Theory]
    [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(float.NegativeInfinity)]
    public async Task NonfiniteSingleCannotBecomeSuccessfulHttpJson(float value)
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        var adapter = ProjectionAdapter(source, ProviderType.MongoDBOASIS, new[] { new CosmicProjectionRow(value) });
        adapter.SetupGet(provider => provider.ProjectionCapabilities).Returns(new CosmicProjectionCapabilities(
            Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(float) }));
        var request = new CqlProjectionV1Request { Columns = new[] { new CqlProjectionV1Column
            { Alias = "Reading", MetadataPath = new[] { "reading" }, ValueType = "Single" } } };
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source));
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Code(result));
        Assert.IsType<ObjectResult>(result);
    }
}
