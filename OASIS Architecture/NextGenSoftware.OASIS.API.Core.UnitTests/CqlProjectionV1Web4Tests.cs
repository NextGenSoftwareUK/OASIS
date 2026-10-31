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
    private static Mock<ICosmicQueryProjectionStorageProvider> ProjectionAdapter(Source source, ProviderType type,
        IReadOnlyList<CosmicProjectionRow> rows)
    {
        var adapter = source.Storage.As<ICosmicQueryProjectionStorageProvider>();
        adapter.SetupGet(value => value.QueryProviderType).Returns(type);
        adapter.SetupGet(value => value.QueryCapabilities).Returns(Capabilities);
        adapter.SetupGet(value => value.ProjectionCapabilities).Returns(new CosmicProjectionCapabilities(
            new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) },
            new[] { typeof(long), typeof(long?), typeof(Guid), typeof(DateTime), typeof(HolonType) }));
        adapter.Setup(value => value.ExecuteCosmicProjectionAsync(
            It.IsAny<CosmicProjectionPlan<Holon, CosmicProjectionRecord>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IReadOnlyList<CosmicProjectionRow>>(rows) { IsLoaded = true });
        return adapter;
    }

    private static CqlProjectionV1Request NameProjection() => new()
    { Columns = new[] { new CqlProjectionV1Column { Alias = "name", Field = "Name" } } };

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ProjectionHttpRetainsExactWireValuesAndOneDurableAttempt(ProviderType type)
    {
        var id = Guid.NewGuid(); var date = new DateTime(638000000000000001, DateTimeKind.Utc);
        var source = new Source(type);
        ProjectionAdapter(source, type, new[] { new CosmicProjectionRow("coffee", 9007199254740993L, null, id, date, HolonType.Quest) });
        var request = NameProjection();
        request.Columns = request.Columns.Concat(new[]
        {
            new CqlProjectionV1Column { Alias = "stock", MetadataPath = new[] { "stock" }, ValueType = "Int64" },
            new CqlProjectionV1Column { Alias = "optional", MetadataPath = new[] { "optional" }, ValueType = "Int64", Nullable = true },
            new CqlProjectionV1Column { Alias = "code", MetadataPath = new[] { "code" }, ValueType = "Guid" },
            new CqlProjectionV1Column { Alias = "when", MetadataPath = new[] { "when" }, ValueType = "DateTime" },
            new CqlProjectionV1Column { Alias = "kind", MetadataPath = new[] { "kind" }, ValueType = "HolonType" }
        }).ToArray();
        var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source));
        using var document = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
        var body = document.RootElement.GetProperty("result");
        Assert.Equal("cql-projection-v1", body.GetProperty("version").GetString());
        Assert.Equal(type.ToString(), body.GetProperty("provider").GetString());
        var row = body.GetProperty("rows")[0];
        Assert.Equal("coffee", row[0].GetString()); Assert.Equal("9007199254740993", row[1].GetString());
        Assert.Equal(JsonValueKind.Null, row[2].ValueKind); Assert.Equal(id.ToString("D"), row[3].GetString());
        Assert.Equal(date.ToString("O"), row[4].GetString()); Assert.Equal("Quest", row[5].GetString());
        var columns = body.GetProperty("columns");
        Assert.Equal("Int64", columns[1].GetProperty("valueType").GetString());
        Assert.True(columns[2].GetProperty("nullable").GetBoolean());
        Assert.Null(source.Observed);
        var receipt = Assert.Single(admitted.Receipts); Assert.Equal(1, receipt.Units); Assert.Equal(0.002m, receipt.ActualCostUsd);
        admitted.Store.Verify(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Services.Subscriptions.UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProjectionUnsupportedAdapterCannotCreateManagerOrAttempt()
    {
        var source = new Source(ProviderType.MongoDBOASIS); var admitted = Admission(ProviderType.MongoDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, NameProjection(), source));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Code(result)); Assert.Equal(0, source.Managers); Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task InvalidProjectionRejectsBeforeProviderLookup()
    {
        var source = new Source(ProviderType.MongoDBOASIS); var admitted = Admission(ProviderType.MongoDBOASIS);
        var request = NameProjection(); request.Columns[0].Alias = "unsafe.field";
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source));
        Assert.Equal("CQL_REQUEST_INVALID", Code(result)); Assert.Equal(0, source.Lookups); Assert.Equal(0, source.Managers); Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task ProjectionReadPermissionsAreCheckedBeforeManagerOrAttempt()
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        var adapter = ProjectionAdapter(source, ProviderType.MongoDBOASIS, Array.Empty<CosmicProjectionRow>());
        adapter.SetupGet(value => value.ProjectionCapabilities).Returns(new CosmicProjectionCapabilities(
            Array.Empty<CosmicProjectionFieldCapability>(), Array.Empty<Type>()));
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, NameProjection(), source));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Code(result)); Assert.Equal(0, source.Managers); Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task ProjectionRequiresTheAuthenticatedReservationBeforeLookup()
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        var admitted = Admission(ProviderType.MongoDBOASIS, reservedActor: Guid.NewGuid());
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, NameProjection(), source));
        Assert.Equal("CQL_ADMISSION_REQUIRED", Code(result)); Assert.Equal(0, source.Lookups);
    }

    [Fact]
    public async Task EscapedProjectionResponseOverflowPublishesNoFileAndRetainsExecutedReceipt()
    {
        var source = new Source(ProviderType.MongoDBOASIS); var text = new string('\0', 70000);
        ProjectionAdapter(source, ProviderType.MongoDBOASIS, new[] { new CosmicProjectionRow(text, text, text) });
        var request = NameProjection();
        request.Columns = Enumerable.Range(0, 3).Select(i => new CqlProjectionV1Column { Alias = "name" + i, Field = "Name" }).ToArray();
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source));
        Assert.Equal("CQL_RESPONSE_LIMIT", Code(result)); Assert.Equal(413, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Single(admitted.Receipts);
    }

    [Fact]
    public async Task WrongNativeProjectionDomainCannotBecomeSuccessfulJson()
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        ProjectionAdapter(source, ProviderType.MongoDBOASIS, new[] { new CosmicProjectionRow(7L) });
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, NameProjection(), source));
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Code(result)); Assert.IsType<ObjectResult>(result);
    }

    [Fact]
    public async Task PreCancelledProjectionCannotLookUpOrDispatchAProvider()
    {
        var source = new Source(ProviderType.MongoDBOASIS); var admitted = Admission(ProviderType.MongoDBOASIS);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel(); admitted.Http.RequestAborted = cancellation.Token;
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, NameProjection(), source));
        Assert.Equal("CQL_QUERY_CANCELLED", Code(result)); Assert.Equal(0, source.Lookups); Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task NonUtcProjectionDateCannotDependOnTheServerTimezone()
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        ProjectionAdapter(source, ProviderType.MongoDBOASIS, new[] { new CosmicProjectionRow(new DateTime(638000000000000001, DateTimeKind.Unspecified)) });
        var request = NameProjection(); request.Columns = new[] { new CqlProjectionV1Column
        { Alias = "when", MetadataPath = new[] { "when" }, ValueType = "DateTime" } };
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source));
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Code(result));
    }
}
