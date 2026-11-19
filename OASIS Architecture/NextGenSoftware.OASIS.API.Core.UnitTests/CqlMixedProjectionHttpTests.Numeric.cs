using System.Text.Json;
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
    private sealed class NumericMappedHttpSource(Source stored, Guid mapping,
        IReadOnlyList<VirtualHolonProjectionRead> rows) : ICqlV1Providers, ICqlVirtualMappingReader
    {
        public NextGenSoftware.OASIS.API.Core.Interfaces.IOASISProvider Resolve(ProviderType? requested) => stored.Resolve(requested);
        public NextGenSoftware.OASIS.API.Core.Managers.HolonManager CreateManager(
            NextGenSoftware.OASIS.API.Core.Interfaces.IOASISProvider provider) => stored.CreateManager(provider);
        public Task<OASISResult<IReadOnlyList<VirtualHolonProjectionRead>>> QueryVirtualMappingAsync(
            Guid id, NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar avatar, CqlVirtualProjection projection, CancellationToken token)
        {
            Assert.Equal(mapping, id); Assert.Equal(Actor, avatar.Id);
            Assert.Equal(NativeRecordFunction.Utf16Length, Assert.Single(projection.Columns).Function);
            return Task.FromResult(new OASISResult<IReadOnlyList<VirtualHolonProjectionRead>>(rows) { IsLoaded = true });
        }
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS, CqlProjectionV1ResultOperation.Min)]
    [InlineData(ProviderType.MongoDBOASIS, CqlProjectionV1ResultOperation.Max)]
    [InlineData(ProviderType.MongoDBOASIS, CqlProjectionV1ResultOperation.Sum)]
    [InlineData(ProviderType.MongoDBOASIS, CqlProjectionV1ResultOperation.Average)]
    [InlineData(ProviderType.SQLLiteDBOASIS, CqlProjectionV1ResultOperation.Min)]
    [InlineData(ProviderType.SQLLiteDBOASIS, CqlProjectionV1ResultOperation.Max)]
    [InlineData(ProviderType.SQLLiteDBOASIS, CqlProjectionV1ResultOperation.Sum)]
    [InlineData(ProviderType.SQLLiteDBOASIS, CqlProjectionV1ResultOperation.Average)]
    [InlineData(ProviderType.Neo4jOASIS, CqlProjectionV1ResultOperation.Min)]
    [InlineData(ProviderType.Neo4jOASIS, CqlProjectionV1ResultOperation.Max)]
    [InlineData(ProviderType.Neo4jOASIS, CqlProjectionV1ResultOperation.Sum)]
    [InlineData(ProviderType.Neo4jOASIS, CqlProjectionV1ResultOperation.Average)]
    public async Task ComputedAliasProjectionHttpMixedNumericReductionUsesTheComputedLiveWinner(
        ProviderType provider, CqlProjectionV1ResultOperation operation)
    {
        var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, Actor, provider, "people",
            VirtualHolonMapping.SandboxConnectionName, "numeric-http://fixture",
            new Dictionary<string, string> { ["tier"] = "nativeTier", ["display"] = "name" });
        var request = MappedProjectionHttpRequest(mapping.Id, provider);
        request.ResultOperation = operation; request.Take = 1;
        request.Columns = new[] { request.Columns[0], request.Columns[2] };
        var compiled = CqlV1Compiler.CompileVirtualProjection(request);
        var reader = new Mock<INativeDataProjectionQueryProvider>(MockBehavior.Strict);
        reader.SetupGet(value => value.NativeDataProviderType).Returns(provider);
        reader.Setup(value => value.DescribeVirtualQueryConnection(null)).Returns(mapping.ConnectionIdentity);
        reader.Setup(value => value.QueryNativeProjectionRecordsAsync("people",
            It.IsAny<IReadOnlyList<NativeRecordFilter>>(), It.IsAny<IReadOnlyList<NativeProjectionColumn>>(),
            100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IReadOnlyList<NativeProjectionRow>>(new[] {
                new NativeProjectionRow("people", "one", new[] { new NativeProjectionCell(NativeProjectionValueState.Present, 2) })
            }) { IsLoaded = true });
        var read = await mapping.QueryProjectionAsync(Actor, reader.Object, null, compiled.Matches, compiled.Columns, 100);
        Assert.False(read.IsError, read.Message);
        var row = Assert.Single(read.Result);
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, new[] {
            new CosmicProjectionRow(row.Id, 100), new CosmicProjectionRow(Guid.NewGuid(), 3) });
        var source = new NumericMappedHttpSource(stored, mapping.Id, read.Result);
        var admission = Admission(provider, maximumProviderAttempts: 3);
        var result = MixedProjectionResponse(await admission.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admission.Http, request, source, mixed: true)));
        Assert.Equal("cql-scalar-v1", result.GetProperty("version").GetString());
        Assert.Equal(operation.ToString(), result.GetProperty("operation").GetString());
        if (operation == CqlProjectionV1ResultOperation.Average)
        {
            Assert.Equal("Decimal", result.GetProperty("valueType").GetString());
            Assert.Equal("2.5", result.GetProperty("value").GetString());
        }
        else
        {
            Assert.Equal("Int32", result.GetProperty("valueType").GetString());
            Assert.Equal(operation == CqlProjectionV1ResultOperation.Min ? 2
                : operation == CqlProjectionV1ResultOperation.Max ? 3 : 5, result.GetProperty("value").GetInt32());
        }
        Assert.False(result.GetProperty("canPrepareSnapshot").GetBoolean());
        Assert.Single(admission.Receipts);
    }
}
