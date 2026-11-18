using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private static async Task<(Guid MappingId, IReadOnlyList<VirtualHolonProjectionRead> Rows)> CountHttpRows(
        ProviderType provider, int count)
    {
        var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, Actor, provider, "people",
            VirtualHolonMapping.SandboxConnectionName, "test-native://count-http",
            new Dictionary<string, string> { ["tier"] = "nativeTier" });
        var reader = new Mock<INativeDataProjectionQueryProvider>(MockBehavior.Strict);
        reader.SetupGet(value => value.NativeDataProviderType).Returns(provider);
        reader.Setup(value => value.DescribeVirtualQueryConnection(null)).Returns(mapping.ConnectionIdentity);
        IReadOnlyList<NativeProjectionRow> rows = Enumerable.Range(0, count).Select(i =>
            new NativeProjectionRow("people", "n" + i, Array.Empty<NativeProjectionCell>())).ToArray();
        reader.Setup(value => value.QueryNativeProjectionRecordsAsync("people",
            It.IsAny<IReadOnlyList<NativeRecordFilter>>(), It.IsAny<IReadOnlyList<NativeProjectionColumn>>(),
            100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IReadOnlyList<NativeProjectionRow>>(rows) { IsLoaded = true });
        var read = await mapping.QueryProjectionAsync(Actor, reader.Object, null,
            new[] { new NativeRecordFilter("tier", CosmicQueryComparisonOperator.Equal, "gold") },
            Array.Empty<NativeProjectionColumn>(), 100);
        Assert.False(read.IsError, read.Message);
        return (mapping.Id, read.Result);
    }

    private static CqlProjectionV1Request CountHttpRequest(Guid mapping, ProviderType provider)
    {
        var request = MappedProjectionHttpRequest(mapping, provider);
        request.ResultOperation = CqlProjectionV1ResultOperation.LongCount;
        request.Columns = new[] { request.Columns[0] }; request.Take = 1;
        return request;
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ComputedAliasProjectionHttpMixedCountIncludes198IdentitiesBeforePaging(ProviderType provider)
    {
        var live = await CountHttpRows(provider, 99);
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, Enumerable.Range(0, 99)
            .Select(_ => new CosmicProjectionRow(Guid.NewGuid())).ToArray());
        var source = new IdentityMappedHttpSource(stored, live.MappingId, live.Rows);
        var admission = Admission(provider, maximumProviderAttempts: 3);
        var result = MixedProjectionResponse(await admission.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admission.Http, CountHttpRequest(live.MappingId, provider), source, mixed: true)));
        Assert.Equal("cql-scalar-v1", result.GetProperty("version").GetString());
        Assert.Equal("LongCount", result.GetProperty("operation").GetString());
        Assert.Equal("Int64", result.GetProperty("valueType").GetString());
        Assert.Equal("198", result.GetProperty("value").GetString());
        Assert.False(result.GetProperty("canPrepareSnapshot").GetBoolean());
        Assert.Single(admission.Receipts);
    }

    [Theory]
    [InlineData("operation")]
    [InlineData("columns")]
    [InlineData("skip")]
    [InlineData("take")]
    [InlineData("ordering")]
    [InlineData("budget")]
    [InlineData("ordinary-route")]
    public async Task ComputedAliasProjectionHttpMixedCountRejectsInvalidIntentBeforeDispatch(string invalid)
    {
        var provider = ProviderType.MongoDBOASIS;
        var live = await CountHttpRows(provider, 1);
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, Array.Empty<CosmicProjectionRow>());
        var source = new IdentityMappedHttpSource(stored, live.MappingId, live.Rows);
        var request = CountHttpRequest(live.MappingId, provider);
        if (invalid == "operation") request.ResultOperation = (CqlProjectionV1ResultOperation)999;
        if (invalid == "columns") request.Columns = MappedProjectionHttpRequest(live.MappingId, provider).Columns;
        if (invalid == "skip") request.Skip = 1;
        if (invalid == "take") request.Take = 2;
        if (invalid == "ordering") request.Descending = true;
        var admission = Admission(provider, maximumProviderAttempts: invalid == "budget" ? 2 : 3);
        var response = await admission.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admission.Http, request, source, mixed: invalid != "ordinary-route"));
        Assert.Equal(invalid == "budget" ? "CQL_ADMISSION_REQUIRED" : "CQL_REQUEST_INVALID", Code(response));
        stored.Storage.As<ICosmicQueryProjectionStorageProvider>().Verify(value =>
            value.ExecuteCosmicProjectionAsync(It.IsAny<CosmicProjectionPlan<Holon, CosmicProjectionRecord>>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ComputedAliasProjectionHttpMixedCountFreezesItsOperationMappingAndFiltersBeforeProviderCallbacks()
    {
        var provider = ProviderType.MongoDBOASIS;
        var live = await CountHttpRows(provider, 1);
        var request = CountHttpRequest(live.MappingId, provider);
        var stored = new Source(provider);
        IReadOnlyList<CosmicProjectionRow> rows = new[] { new CosmicProjectionRow(live.Rows[0].Id) };
        MappedProjectionHttpAdapter(stored, provider, rows);
        stored.Storage.As<ICosmicQueryProjectionStorageProvider>().Setup(value =>
            value.ExecuteCosmicProjectionAsync(It.IsAny<CosmicProjectionPlan<Holon, CosmicProjectionRecord>>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                request.ResultOperation = CqlProjectionV1ResultOperation.Rows;
                request.VirtualMappingId = Guid.NewGuid(); request.Skip = 1; request.Take = 2;
                request.Filters[0].MetadataPath![0] = "changed";
                request.Columns[0].Alias = "changed";
            })
            .ReturnsAsync(new OASISResult<IReadOnlyList<CosmicProjectionRow>>(rows) { IsLoaded = true });
        var source = new IdentityMappedHttpSource(stored, live.MappingId, live.Rows);
        var admission = Admission(provider, maximumProviderAttempts: 3);
        var result = MixedProjectionResponse(await admission.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admission.Http, request, source, mixed: true)));
        Assert.Equal("LongCount", result.GetProperty("operation").GetString());
        Assert.Equal("1", result.GetProperty("value").GetString());
        Assert.Equal(live.MappingId.ToString("D"), result.GetProperty("virtualMappingId").GetString());
    }
}
