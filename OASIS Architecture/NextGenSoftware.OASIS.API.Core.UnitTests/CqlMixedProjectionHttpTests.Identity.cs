using System.Text.Json;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private sealed class IdentityMappedHttpSource(Source stored, Guid mappingId,
        IReadOnlyList<VirtualHolonProjectionRead> rows) : ICqlV1Providers, ICqlVirtualMappingReader
    {
        public IOASISProvider Resolve(ProviderType? requested) => stored.Resolve(requested);
        public HolonManager CreateManager(IOASISProvider provider) => stored.CreateManager(provider);
        public Task<OASISResult<IReadOnlyList<VirtualHolonProjectionRead>>> QueryVirtualMappingAsync(
            Guid id, IAvatar avatar, CqlVirtualProjection projection, CancellationToken token)
        {
            Assert.Equal(mappingId, id); Assert.Equal(Actor, avatar.Id);
            Assert.Empty(projection.Columns); Assert.Empty(projection.ProjectedFields);
            return Task.FromResult(new OASISResult<IReadOnlyList<VirtualHolonProjectionRead>>(rows) { IsLoaded = true });
        }
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ComputedAliasProjectionHttpMixedSelectsOnlyLogicalIdentityWithoutNativeFields(ProviderType provider)
    {
        var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, Actor, provider, "people",
            VirtualHolonMapping.SandboxConnectionName, "test-native://identity",
            new Dictionary<string, string> { ["tier"] = "nativeTier" });
        var request = MappedProjectionHttpRequest(mapping.Id, provider);
        request.Columns = new[] { request.Columns[0] };
        var projection = CqlV1Compiler.CompileVirtualProjection(request);
        var reader = new Mock<INativeDataProjectionQueryProvider>(MockBehavior.Strict);
        reader.SetupGet(value => value.NativeDataProviderType).Returns(provider);
        reader.Setup(value => value.DescribeVirtualQueryConnection(null)).Returns(mapping.ConnectionIdentity);
        reader.Setup(value => value.QueryNativeProjectionRecordsAsync("people",
            It.IsAny<IReadOnlyList<NativeRecordFilter>>(), It.IsAny<IReadOnlyList<NativeProjectionColumn>>(),
            100, null, It.IsAny<CancellationToken>()))
            .Returns((string source, IReadOnlyList<NativeRecordFilter> filters, IReadOnlyList<NativeProjectionColumn> columns,
                int limit, NativeConnection connection, CancellationToken token) =>
            {
                Assert.Empty(columns);
                Assert.Equal("nativeTier", Assert.Single(filters).Field);
                return Task.FromResult(new OASISResult<IReadOnlyList<NativeProjectionRow>>(new[] {
                    new NativeProjectionRow("people", "one", Array.Empty<NativeProjectionCell>())
                }) { IsLoaded = true });
            });
        var read = await mapping.QueryProjectionAsync(Actor, reader.Object, null, projection.Matches, projection.Columns, 100);
        Assert.False(read.IsError, read.Message);
        var native = Assert.Single(read.Result); Assert.Empty(native.Cells); Assert.Empty(native.Columns);
        var otherId = Guid.NewGuid();
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, new[] {
            new CosmicProjectionRow(native.Id), new CosmicProjectionRow(otherId) });
        var source = new IdentityMappedHttpSource(stored, mapping.Id, read.Result);
        var admitted = Admission(provider, maximumProviderAttempts: 3);
        var result = MixedProjectionResponse(await admitted.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source, mixed: true)));
        Assert.Single(result.GetProperty("columns").EnumerateArray());
        var rows = result.GetProperty("rows").EnumerateArray().ToArray();
        Assert.Equal(new[] { native.Id, otherId }.OrderBy(id => id),
            rows.Select(row => Guid.Parse(row[0].GetString()!)));
        Assert.All(rows, row => Assert.Equal(1, row.GetArrayLength()));
        var provenance = result.GetProperty("provenance").EnumerateArray();
        Assert.True(Assert.Single(provenance.Where(item => item.GetProperty("representation").GetString() == "Virtual"))
            .GetProperty("alsoMaterialized").GetBoolean());
        Assert.Single(admitted.Receipts);
    }
}
