using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    [Theory]
    [InlineData("Int32", false, false)]
    [InlineData("Int64", false, false)]
    [InlineData("Int32", true, false)]
    [InlineData("Int32", true, true)]
    [InlineData("Int64", true, false)]
    [InlineData("Int64", true, true)]
    public async Task NumericAverageHttpPreservesDecimalAndTypedSelection(string inputType, bool optional, bool empty)
    {
        var type = ProviderType.MongoDBOASIS;
        var source = new Source(type);
        var adapter = ScalarAdapter(source, type);
        var domain = inputType == "Int32" ? (optional ? typeof(int?) : typeof(int)) : (optional ? typeof(long?) : typeof(long));
        var reads = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { domain });
        adapter.SetupGet(s => s.ScalarCapabilities).Returns(new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(),
            new[] { new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, reads) }));
        var value = inputType == "Int32" ? 1.25m : 9007199254740993.125m;
        Type? observed = null;
        adapter.Setup(s => s.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, decimal>>(), It.IsAny<CancellationToken>()))
            .Returns((CosmicScalarPlan<Holon, decimal> plan, CancellationToken token) =>
            { observed = plan.Query.Selector.ValueType; return Task.FromResult(new OASISResult<decimal>(value) { IsLoaded = true }); });
        adapter.Setup(s => s.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, decimal?>>(), It.IsAny<CancellationToken>()))
            .Returns((CosmicScalarPlan<Holon, decimal?> plan, CancellationToken token) =>
            { observed = plan.Query.Selector.ValueType; return Task.FromResult(new OASISResult<decimal?>(empty ? null : value) { IsLoaded = true }); });
        var admitted = Admission(type);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Operation = "Average", Selection = new() { MetadataPath = new[] { "stock" }, ValueType = inputType, Nullable = optional } }, source));
        using var json = JsonDocument.Parse(Assert.IsType<FileContentResult>(response).FileContents);
        var body = json.RootElement.GetProperty("result");
        Assert.Equal("Average", body.GetProperty("operation").GetString());
        Assert.Equal("Decimal", body.GetProperty("valueType").GetString());
        if (empty) Assert.Equal(JsonValueKind.Null, body.GetProperty("value").ValueKind);
        else Assert.Equal(value.ToString(CultureInfo.InvariantCulture), body.GetProperty("value").GetString());
        Assert.Equal(domain, observed);
        Assert.Single(admitted.Receipts);
        Assert.Equal(1, source.Managers);
    }
    [Theory]
    [InlineData("Sum", long.MinValue)]
    [InlineData("Sum", long.MaxValue)]
    public async Task NumericSumHttpUsesExactIntegerWireAndExistingReceipt(string operation, long value)
    {
        var type = ProviderType.MongoDBOASIS;
        var source = new Source(type); var scalar = ScalarAdapter(source, type);
        var reads = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(long) });
        scalar.SetupGet(s => s.ScalarCapabilities).Returns(new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(),
            new[] { new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, reads) }));
        CosmicScalarPlan<Holon, long>? observed = null;
        scalar.Setup(s => s.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, long>>(), It.IsAny<CancellationToken>()))
            .Returns((CosmicScalarPlan<Holon, long> plan, CancellationToken token) =>
            { observed = plan; return Task.FromResult(new OASISResult<long>(value) { IsLoaded = true }); });
        var admitted = Admission(type);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Operation = operation, Selection = new() { MetadataPath = new[] { "stock" }, ValueType = "Int64" } }, source));
        using var json = JsonDocument.Parse(Assert.IsType<FileContentResult>(response).FileContents);
        var body = json.RootElement.GetProperty("result");
        Assert.Equal(operation, body.GetProperty("operation").GetString());
        Assert.Equal("Int64", body.GetProperty("valueType").GetString());
        Assert.Equal(value.ToString(System.Globalization.CultureInfo.InvariantCulture), body.GetProperty("value").GetString());
        Assert.NotNull(observed); Assert.Equal(Actor, observed.Query.Source.ReadAccess.ActorId);
        Assert.Equal(new[] { "stock" }, observed.Query.Selector.Segments);
        Assert.Single(admitted.Receipts); Assert.Equal(1, source.Managers);
    }


    [Theory]
    [InlineData("Sum", "String")]
    [InlineData("Average", "String")]
    [InlineData("Sum", "Boolean")]
    [InlineData("Average", "Boolean")]
    public async Task NumericHttpRejectsNonIntegerSelectorsBeforeDispatch(string operation, string valueType)
    {
        var type = ProviderType.MongoDBOASIS;
        var source = new Source(type);
        ScalarAdapter(source, type);
        var admitted = Admission(type);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Operation = operation, Selection = new() { MetadataPath = new[] { "stock" }, ValueType = valueType } }, source));
        Assert.Equal("CQL_REQUEST_INVALID", Code(response));
        Assert.Equal(0, source.Managers);
        Assert.Empty(admitted.Receipts);
    }

}
