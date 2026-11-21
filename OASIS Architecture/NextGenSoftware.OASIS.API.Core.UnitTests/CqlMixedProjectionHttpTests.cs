using System.Text.Json;
using Moq;
using Microsoft.AspNetCore.Mvc;
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
    private sealed class MappedProjectionHttpSource(Source stored, Guid mappingId,
        IReadOnlyList<VirtualHolonProjectionRead> rows) : ICqlV1Providers, ICqlVirtualMappingReader
    {
        internal int VirtualCalls;
        public IOASISProvider Resolve(ProviderType? requested) => stored.Resolve(requested);
        public HolonManager CreateManager(IOASISProvider provider) => stored.CreateManager(provider);
        public Task<OASISResult<IReadOnlyList<VirtualHolonProjectionRead>>> QueryVirtualMappingAsync(
            Guid id, IAvatar avatar, CqlVirtualProjection projection, CancellationToken token)
        {
            VirtualCalls++;
            Assert.Equal(mappingId, id); Assert.Equal(Actor, avatar.Id);
            Assert.True(token.CanBeCanceled);
            Assert.Equal("tier", Assert.Single(projection.Matches).Field);
            Assert.Equal(new[] { "Text", "Units", "Again" }, projection.Columns.Select(column => column.Alias));
            return Task.FromResult(new OASISResult<IReadOnlyList<VirtualHolonProjectionRead>>(rows) { IsLoaded = true });
        }
    }

    private static CqlProjectionV1Request MappedProjectionHttpRequest(Guid mappingId, ProviderType provider) => new()
    {
        Provider = provider.ToString(), VirtualMappingId = mappingId, OrderBy = "Id", Take = 5,
        Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "tier" }, Scalar = CqlV1Scalar.String,
            Operator = CqlV1Operator.Equal, Value = JsonSerializer.SerializeToElement("gold") } },
        Columns = new[] {
            new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
            new CqlProjectionV1Column { MetadataPath = new[] { "display" }, ValueType = "String", Alias = "Text" },
            new CqlProjectionV1Column { MetadataPath = new[] { "display" }, ValueType = "String", Alias = "Units",
                Function = CqlV1Function.Utf16Length, Nullable = true },
            new CqlProjectionV1Column { MetadataPath = new[] { "display" }, ValueType = "String", Alias = "Again",
                Function = CqlV1Function.Utf16Length, Nullable = true }
        }
    };

    private static async Task<(Guid MappingId, VirtualHolonProjectionRead Row)> MappedProjectionHttpRow(ProviderType provider, int readLimit = 100)
    {
        var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, Actor, provider, "people",
            VirtualHolonMapping.SandboxConnectionName, "test-native://http",
            new Dictionary<string, string> { ["display"] = "name", ["tier"] = "nativeTier" });
        var projection = CqlV1Compiler.CompileVirtualProjection(MappedProjectionHttpRequest(mapping.Id, provider));
        var reader = new Mock<INativeDataProjectionQueryProvider>(MockBehavior.Strict);
        reader.SetupGet(value => value.NativeDataProviderType).Returns(provider);
        reader.Setup(value => value.DescribeVirtualQueryConnection(null)).Returns(mapping.ConnectionIdentity);
        reader.Setup(value => value.QueryNativeProjectionRecordsAsync("people",
            It.IsAny<IReadOnlyList<NativeRecordFilter>>(), It.IsAny<IReadOnlyList<NativeProjectionColumn>>(),
            readLimit, null, It.IsAny<CancellationToken>()))
            .Returns((string source, IReadOnlyList<NativeRecordFilter> filters, IReadOnlyList<NativeProjectionColumn> columns,
                int limit, NativeConnection connection, CancellationToken token) =>
            {
                Assert.Equal("nativeTier", Assert.Single(filters).Field);
                Assert.All(columns, column => Assert.Equal("name", column.Field));
                IReadOnlyList<NativeProjectionRow> rows = new[] { new NativeProjectionRow("people", "one", new[] {
                    new NativeProjectionCell(NativeProjectionValueState.Present, "😀"),
                    new NativeProjectionCell(NativeProjectionValueState.Present, 2),
                    new NativeProjectionCell(NativeProjectionValueState.Present, 2) }) };
                return Task.FromResult(new OASISResult<IReadOnlyList<NativeProjectionRow>>(rows) { IsLoaded = true });
            });
        var read = await mapping.QueryProjectionAsync(Actor, reader.Object, null, projection.Matches, projection.Columns, readLimit);
        Assert.False(read.IsError, read.Message);
        return (mapping.Id, Assert.Single(read.Result));
    }

    private static CosmicQueryCapabilities MixedSnapshotCapabilities(CosmicQueryCapabilities original)
        => new(original.Features | CosmicQueryFeatures.Not | CosmicQueryFeatures.Metadata | CosmicQueryFeatures.Membership,
            original.CanonicalFields.ToArray(),
            original.MetadataTypes.Where(item => item.ValueType != typeof(string))
                .Append(new CosmicQueryMetadataCapability(typeof(string),
                    new[] { CosmicQueryComparisonOperator.Equal, CosmicQueryComparisonOperator.NotEqual })).ToArray());

    private static void MappedProjectionHttpAdapter(Source source, ProviderType provider, IReadOnlyList<CosmicProjectionRow> rows,
        IReadOnlyList<CosmicProjectionRow>? snapshotRows = null)
    {
        var adapter = ProjectionAdapter(source, provider, rows, snapshotRows);
        var capabilities = new CosmicQueryCapabilities(Capabilities.Features | CosmicQueryFeatures.Metadata,
            Capabilities.CanonicalFields.ToArray(), new[] { new CosmicQueryMetadataCapability(typeof(string),
                new[] { CosmicQueryComparisonOperator.Equal }) });
        source.Adapter.SetupGet(value => value.QueryCapabilities).Returns(MixedSnapshotCapabilities(capabilities));
        adapter.SetupGet(value => value.QueryCapabilities).Returns(MixedSnapshotCapabilities(capabilities));
        adapter.SetupGet(value => value.ProjectionCapabilities).Returns(new CosmicProjectionCapabilities(
            new[] { new CosmicProjectionFieldCapability("Id", typeof(Guid)) }, new[] { typeof(string) })
            .WithFunctions(CosmicProjectionFunction.Utf16Length));
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ComputedAliasProjectionHttpMixedKeepsNativeCellsAndDeduplicatesLogicalIdentity(ProviderType provider)
    {
        var fixture = await MappedProjectionHttpRow(provider);
        var stored = new Source(provider);
        var otherId = Guid.NewGuid();
        MappedProjectionHttpAdapter(stored, provider, new[] {
            new CosmicProjectionRow(fixture.Row.Id, "stored", 6, 6),
            new CosmicProjectionRow(otherId, "important", 9, 9)
        });
        var source = new MappedProjectionHttpSource(stored, fixture.MappingId, new[] { fixture.Row });
        var admitted = Admission(provider, maximumProviderAttempts: 4);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http,
            MappedProjectionHttpRequest(fixture.MappingId, provider), source, mixed: true));
        Assert.True(response is FileContentResult, response is ObjectResult error
            ? "Mixed projection failed: " + Code(response) + " status=" + error.StatusCode : response.GetType().Name);
        using var document = JsonDocument.Parse(Assert.IsType<FileContentResult>(response).FileContents);
        var result = document.RootElement.GetProperty("result");
        var rows = result.GetProperty("rows").EnumerateArray().ToArray();
        Assert.Equal(2, rows.Length);
        var virtualRow = Assert.Single(rows.Where(row => row[0].GetString() == fixture.Row.Id.ToString("D")));
        Assert.Equal("😀", virtualRow[1].GetString());
        Assert.Equal(2, virtualRow[2].GetInt32()); Assert.Equal(2, virtualRow[3].GetInt32());
        Assert.Equal("Int32", result.GetProperty("columns")[2].GetProperty("valueType").GetString());
        Assert.True(result.GetProperty("columns")[2].GetProperty("nullable").GetBoolean());
        var provenance = result.GetProperty("provenance").EnumerateArray().ToArray();
        Assert.True(Assert.Single(provenance.Where(row => row.GetProperty("representation").GetString() == "Virtual"))
            .GetProperty("alsoMaterialized").GetBoolean());
        Assert.Single(provenance.Where(row => row.GetProperty("representation").GetString() == "Materialized"));
        Assert.Equal(1, source.VirtualCalls);
        Assert.Equal(2, admitted.Receipts.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ComputedAliasProjectionHttpMixedRejectsDuplicateVirtualIdentityOrFullStoredBranch(bool duplicate)
    {
        var provider = ProviderType.MongoDBOASIS;
        var fixture = await MappedProjectionHttpRow(provider);
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, duplicate ? Array.Empty<CosmicProjectionRow>() :
            Enumerable.Range(0, 100).Select(_ => new CosmicProjectionRow(Guid.NewGuid(), "stored", 6, 6)).ToArray());
        var source = new MappedProjectionHttpSource(stored, fixture.MappingId,
            duplicate ? new[] { fixture.Row, fixture.Row } : new[] { fixture.Row });
        var admitted = Admission(provider, maximumProviderAttempts: 4);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http,
            MappedProjectionHttpRequest(fixture.MappingId, provider), source, mixed: true));
        Assert.Equal(duplicate ? "CQL_PROVIDER_RESULT_INVALID" : "CQL_VIRTUAL_SOURCE_LIMIT", Code(response));
        Assert.Equal(duplicate ? 1 : 0, source.VirtualCalls);
        Assert.Single(admitted.Receipts);
    }

    [Fact]
    public async Task ComputedAliasProjectionHttpMixedRequiresItsFourAttemptAdmissionBeforeNativeMapping()
    {
        var provider = ProviderType.MongoDBOASIS;
        var fixture = await MappedProjectionHttpRow(provider);
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, Array.Empty<CosmicProjectionRow>());
        var source = new MappedProjectionHttpSource(stored, fixture.MappingId, new[] { fixture.Row });
        var admitted = Admission(provider, maximumProviderAttempts: 2);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http,
            MappedProjectionHttpRequest(fixture.MappingId, provider), source, mixed: true));
        Assert.Equal("CQL_ADMISSION_REQUIRED", Code(response));
        Assert.Equal(0, source.VirtualCalls);
        Assert.Empty(admitted.Receipts);
    }
}
