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
    private sealed class DoubleMappedHttpSource(Source stored, Guid mappingId,
        IReadOnlyList<VirtualHolonProjectionRead> rows) : ICqlV1Providers, ICqlVirtualMappingReader
    {
        public IOASISProvider Resolve(ProviderType? requested) => stored.Resolve(requested);
        public HolonManager CreateManager(IOASISProvider provider) => stored.CreateManager(provider);
        public Task<OASISResult<IReadOnlyList<VirtualHolonProjectionRead>>> QueryVirtualMappingAsync(
            Guid id, IAvatar avatar, CqlVirtualProjection projection, CancellationToken token)
        {
            Assert.Equal(mappingId, id); Assert.Equal(Actor, avatar.Id);
            Assert.Equal(typeof(double), Assert.Single(projection.Matches).InputType);
            Assert.Equal(typeof(double), Assert.Single(projection.Columns).ValueType);
            return Task.FromResult(new OASISResult<IReadOnlyList<VirtualHolonProjectionRead>>(rows) { IsLoaded = true });
        }
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ComputedAliasProjectionHttpMixedDoubleRetainsWireDomainAndPagesAfterLiveWinner(ProviderType provider)
    {
        var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, Actor, provider, "people",
            VirtualHolonMapping.SandboxConnectionName, "test-native://double",
            new Dictionary<string, string> { ["score"] = "nativeRate" });
        var request = new CqlProjectionV1Request
        {
            Provider = provider.ToString(), VirtualMappingId = mapping.Id, OrderBy = "None", Take = 1, Skip = 1,
            SelectedOrder = new[] { new CqlGroupedV1Ordering { Alias = "Score", Descending = true } },
            Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "score" }, Scalar = CqlV1Scalar.Double,
                Operator = CqlV1Operator.GreaterThanOrEqual, Value = JsonSerializer.SerializeToElement("1") } },
            Columns = new[] { new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
                new CqlProjectionV1Column { MetadataPath = new[] { "score" }, ValueType = "Double", Alias = "Score" } }
        };
        var projection = CqlV1Compiler.CompileVirtualProjection(request);
        var reader = new Mock<INativeDataProjectionQueryProvider>(MockBehavior.Strict);
        reader.SetupGet(value => value.NativeDataProviderType).Returns(provider);
        reader.Setup(value => value.DescribeVirtualQueryConnection(null)).Returns(mapping.ConnectionIdentity);
        reader.Setup(value => value.QueryNativeProjectionRecordsAsync("people",
            It.IsAny<IReadOnlyList<NativeRecordFilter>>(), It.IsAny<IReadOnlyList<NativeProjectionColumn>>(),
            100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IReadOnlyList<NativeProjectionRow>>(new[] {
                new NativeProjectionRow("people", "one", new[] { new NativeProjectionCell(NativeProjectionValueState.Present, 3.25d) })
            }) { IsLoaded = true });
        var read = await mapping.QueryProjectionAsync(Actor, reader.Object, null, projection.Matches, projection.Columns, 100);
        Assert.False(read.IsError, read.Message);
        var native = Assert.Single(read.Result);
        var stored = new Source(provider);
        var adapter = ProjectionAdapter(stored, provider, new[] {
            new CosmicProjectionRow(native.Id, 9d), new CosmicProjectionRow(Guid.NewGuid(), 7.5d),
            new CosmicProjectionRow(Guid.NewGuid(), 1.5d) });
        var capabilities = new CosmicQueryCapabilities(Capabilities.Features | CosmicQueryFeatures.Metadata,
            Capabilities.CanonicalFields.ToArray(), new[] { new CosmicQueryMetadataCapability(typeof(double),
                new[] { CosmicQueryComparisonOperator.GreaterThanOrEqual }) });
        stored.Adapter.SetupGet(value => value.QueryCapabilities).Returns(MixedSnapshotCapabilities(capabilities));
        adapter.SetupGet(value => value.QueryCapabilities).Returns(MixedSnapshotCapabilities(capabilities));
        adapter.SetupGet(value => value.ProjectionCapabilities).Returns(new CosmicProjectionCapabilities(
            new[] { new CosmicProjectionFieldCapability("Id", typeof(Guid)) }, new[] { typeof(double) }));
        var source = new DoubleMappedHttpSource(stored, mapping.Id, read.Result);
        var admitted = Admission(provider, maximumProviderAttempts: 4);
        var result = MixedProjectionResponse(await admitted.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source, mixed: true)));
        var row = Assert.Single(result.GetProperty("rows").EnumerateArray());
        Assert.Equal(native.Id.ToString("D"), row[0].GetString());
        Assert.Equal("3.25", row[1].GetString());
        Assert.Equal("Double", result.GetProperty("columns")[1].GetProperty("valueType").GetString());
        var provenance = Assert.Single(result.GetProperty("provenance").EnumerateArray());
        Assert.Equal("Virtual", provenance.GetProperty("representation").GetString());
        Assert.True(provenance.GetProperty("alsoMaterialized").GetBoolean());
        Assert.Equal(2, admitted.Receipts.Count);
    }
}
