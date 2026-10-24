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
    [InlineData("Min", long.MinValue)]
    [InlineData("Max", long.MaxValue)]
    public async Task SelectedScalarHttpUsesTypedIntentExactWireAndExistingReceipt(string operation, long value)
    {
        var type = ProviderType.MongoDBOASIS;
        var source = new Source(type); var scalar = ScalarAdapter(source, type);
        var reads = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(long) });
        scalar.SetupGet(s => s.ScalarCapabilities).Returns(new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(),
            new[] { new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, reads), new CosmicScalarSelectionCapability(CosmicScalarOperation.Max, reads) }));
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
    [InlineData("Min", false)] [InlineData("Max", false)]
    [InlineData("Sum", false)] [InlineData("Average", false)]
    [InlineData("Count", true)] [InlineData("LongCount", true)] [InlineData("Any", true)]
    public async Task ScalarSelectionPresenceMatchesItsOperationBeforeDispatch(string operation, bool selection)
    {
        var type = ProviderType.MongoDBOASIS; var source = new Source(type); ScalarAdapter(source, type);
        var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Operation = operation, Selection = selection ? new() { Field = "Version" } : null }, source));
        Assert.Equal("CQL_REQUEST_INVALID", Code(result)); Assert.Equal(0, source.Managers); Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task CountPermissionCannotGrantSelectedScalarHttpRead()
    {
        var type = ProviderType.MongoDBOASIS; var source = new Source(type); ScalarAdapter(source, type);
        var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Operation = "Min", Selection = new() { Field = "Version" } }, source));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Code(result)); Assert.Equal(0, source.Managers); Assert.Empty(admitted.Receipts);
    }
    private static Mock<ICosmicQueryScalarStorageProvider> SelectedHttpAdapter(Source source, ProviderType type,
        CosmicProjectionCapabilities reads)
    {
        var scalar = ScalarAdapter(source, type);
        scalar.SetupGet(s => s.ScalarCapabilities).Returns(new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(),
            new[] { new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, reads), new CosmicScalarSelectionCapability(CosmicScalarOperation.Max, reads) }));
        return scalar;
    }

    [Fact]
    public async Task SelectedScalarNullableEmptyValueIsTypedNullOnWire()
    {
        var type = ProviderType.MongoDBOASIS; var source = new Source(type);
        var scalar = SelectedHttpAdapter(source, type,
            new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(long?) }));
        scalar.Setup(s => s.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, long?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<long?>((long?)null) { IsLoaded = true });
        var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Operation = "Min", Selection = new() { MetadataPath = new[] { "stock" }, ValueType = "Int64", Nullable = true } }, source));
        using var json = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
        var body = json.RootElement.GetProperty("result");
        Assert.Equal("Int64", body.GetProperty("valueType").GetString()); Assert.Equal(JsonValueKind.Null, body.GetProperty("value").ValueKind);
        Assert.Single(admitted.Receipts);
    }

    [Fact]
    public async Task SelectedCanonicalGuidUsesSharedExactWireEncoder()
    {
        var type = ProviderType.MongoDBOASIS; var source = new Source(type); var id = Guid.NewGuid();
        var scalar = SelectedHttpAdapter(source, type,
            new CosmicProjectionCapabilities(new[] { new CosmicProjectionFieldCapability("Id", typeof(Guid)) }, Array.Empty<Type>()));
        scalar.Setup(s => s.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<Guid>(id) { IsLoaded = true });
        var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Operation = "Max", Selection = new() { Field = "Id" } }, source));
        using var json = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
        Assert.Equal("Guid", json.RootElement.GetProperty("result").GetProperty("valueType").GetString());
        Assert.Equal(id.ToString("D"), json.RootElement.GetProperty("result").GetProperty("value").GetString());
    }

    [Fact]
    public async Task SelectedScalarOversizedResponseIsAtomicAndKeepsTheAcknowledgedReceipt()
    {
        var type = ProviderType.MongoDBOASIS; var source = new Source(type);
        var scalar = SelectedHttpAdapter(source, type,
            new CosmicProjectionCapabilities(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>()));
        scalar.Setup(s => s.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<string>(new string('x', 1024 * 1024 + 1)) { IsLoaded = true });
        var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Operation = "Min", Selection = new() { Field = "Name" } }, source));
        Assert.Equal("CQL_RESPONSE_LIMIT", Code(result)); Assert.Equal(413, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Single(admitted.Receipts); Assert.Equal(1, source.Managers);
    }

}
