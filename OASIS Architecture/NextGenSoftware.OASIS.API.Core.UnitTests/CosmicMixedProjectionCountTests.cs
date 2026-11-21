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

public sealed class CosmicMixedProjectionCountTests
{
    private static readonly Guid Actor = Guid.Parse("569f327b-dc28-4af3-96a1-e2ac1dd85ace");

    private sealed record Fixture(CosmicProjectionQuery<Holon, CosmicProjectionRecord> Query, Guid Mapping,
        IReadOnlyList<CosmicProjectionRecord> Stored, IReadOnlyList<VirtualHolonProjectionRead> Native);

    private static async Task<IReadOnlyList<VirtualHolonProjectionRead>> Native(Guid mappingId, int count,
        int readLimit = 100, int version = 1, ProviderType provider = ProviderType.MongoDBOASIS,
        Guid? owner = null, string prefix = "count-")
    {
        var actor = owner ?? Actor;
        var mapping = new VirtualHolonMapping(mappingId, version, actor, provider, "people",
            VirtualHolonMapping.SandboxConnectionName, "count-native://fixture",
            new Dictionary<string, string> { ["tier"] = "nativeTier" });
        var reader = new Mock<INativeDataProjectionQueryProvider>(MockBehavior.Strict);
        reader.SetupGet(value => value.NativeDataProviderType).Returns(provider);
        reader.Setup(value => value.DescribeVirtualQueryConnection(null)).Returns(mapping.ConnectionIdentity);
        IReadOnlyList<NativeProjectionRow> rows = Enumerable.Range(0, count)
            .Select(i => new NativeProjectionRow("people", prefix + i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Array.Empty<NativeProjectionCell>())).ToArray();
        reader.Setup(value => value.QueryNativeProjectionRecordsAsync("people",
            It.IsAny<IReadOnlyList<NativeRecordFilter>>(), It.IsAny<IReadOnlyList<NativeProjectionColumn>>(),
            readLimit, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IReadOnlyList<NativeProjectionRow>>(rows) { IsLoaded = true });
        var read = await mapping.QueryProjectionAsync(actor, reader.Object, null,
            new[] { new NativeRecordFilter("tier", CosmicQueryComparisonOperator.Equal, "gold") },
            Array.Empty<NativeProjectionColumn>(), readLimit);
        Assert.False(read.IsError, read.Message);
        return read.Result;
    }

    private static async Task<Fixture> Create(int stored, int native, int overlap = 0, int readLimit = 100,
        Guid? sourceActor = null, int branchLimit = 100, bool extraColumn = false)
    {
        var mapping = Guid.NewGuid();
        var actor = sourceActor ?? Actor;
        var live = await Native(mapping, native, readLimit);
        var request = new CqlProjectionV1Request
        {
            Provider = ProviderType.MongoDBOASIS.ToString(), VirtualMappingId = mapping, OrderBy = "Id", Take = 1,
            Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "tier" }, Scalar = CqlV1Scalar.String,
                Operator = CqlV1Operator.Equal, Value = JsonSerializer.SerializeToElement("gold") } },
            Columns = new[] { new CqlProjectionV1Column { Field = "Id", Alias = "Identity" } }
        };
        if (extraColumn) request.Columns = new[] { request.Columns[0],
            new CqlProjectionV1Column { MetadataPath = new[] { "tier" }, ValueType = "String", Alias = "Tier" } };
        var query = CqlV1Compiler.CompileProjection(request, actor, ProviderType.MongoDBOASIS,
            CosmicQueryReadAccess.OwnerOrPublic(actor).Result, branchLimit);
        var rows = Enumerable.Range(0, stored).Select(i =>
        {
            var id = i < overlap ? live[i].Id : Guid.NewGuid();
            return extraColumn ? new CosmicProjectionRow(id, "gold") : new CosmicProjectionRow(id);
        }).ToArray();
        var captured = CosmicProjectionResults.Materialize(query, rows);
        Assert.False(captured.IsError, captured.Message);
        return new(query, mapping, captured.Result, live);
    }

    [Theory]
    [InlineData(0, 0, 0, 0L)]
    [InlineData(99, 99, 0, 198L)]
    [InlineData(99, 99, 1, 197L)]
    [InlineData(99, 99, 99, 99L)]
    [InlineData(2, 1, 1, 2L)]
    public async Task CountIncludesTheCompleteDeduplicatedUnionBeforeAnyProjectionPage(
        int stored, int native, int overlap, long expected)
    {
        var fixture = await Create(stored, native, overlap);
        var count = CosmicMixedProjectionResults.Count(fixture.Query, Actor, fixture.Mapping, fixture.Stored, fixture.Native);
        Assert.False(count.IsError, count.Message);
        Assert.True(count.IsLoaded); Assert.Equal(expected, count.Result);
        var page = CosmicMixedProjectionResults.Merge(fixture.Query, Actor, fixture.Mapping, fixture.Stored, fixture.Native,
            new[] { new CosmicMixedProjectionOrdering("Identity") }, 0, 1);
        Assert.False(page.IsError, page.Message);
        Assert.Equal(expected == 0 ? 0 : 1, page.Result.Count);
        if (overlap == stored && overlap > 0)
        {
            Assert.True(Assert.Single(page.Result).AlsoMaterialized);
            Assert.NotNull(page.Result[0].Virtual);
        }
    }

    [Theory]
    [InlineData("stored-full")]
    [InlineData("native-full")]
    [InlineData("shortened")]
    [InlineData("prior-window")]
    public async Task CountNeverAcceptsAnUnprovenCompleteBranch(string invalid)
    {
        var fixture = await Create(invalid == "stored-full" ? 100 : 0,
            invalid == "native-full" ? 100 : 1,
            readLimit: invalid == "shortened" ? 1 : 100,
            branchLimit: invalid == "prior-window" ? 99 : 100);
        var result = CosmicMixedProjectionResults.Count(fixture.Query, Actor, fixture.Mapping, fixture.Stored, fixture.Native);
        Assert.True(result.IsError); Assert.False(result.IsLoaded);
        Assert.Equal(invalid == "prior-window" ? "CQL_MIXED_QUERY_INVALID" : "CQL_VIRTUAL_SOURCE_LIMIT", result.ErrorCode);
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("mapping")]
    [InlineData("duplicate-native")]
    [InlineData("duplicate-stored")]
    [InlineData("null-stored")]
    [InlineData("payload")]
    [InlineData("cancelled")]
    [InlineData("shape")]
    public async Task CountRetainsWholeInputAccessIdentityShapeAndResourceChecks(string invalid)
    {
        var fixture = await Create(2, 1, sourceActor: invalid == "actor" ? Guid.NewGuid() : null,
            extraColumn: invalid == "shape");
        IReadOnlyList<CosmicProjectionRecord> stored = invalid == "null-stored" ? null!
            : invalid == "duplicate-stored" ? new[] { fixture.Stored[0], fixture.Stored[0] } : fixture.Stored;
        IReadOnlyList<VirtualHolonProjectionRead> native = invalid == "duplicate-native"
            ? new[] { fixture.Native[0], fixture.Native[0] } : fixture.Native;
        using var cancellation = new CancellationTokenSource();
        if (invalid == "cancelled") cancellation.Cancel();
        var result = CosmicMixedProjectionResults.Count(fixture.Query, Actor,
            invalid == "mapping" ? Guid.NewGuid() : fixture.Mapping, stored, native,
            invalid == "payload" ? 1 : CosmicMixedProjectionResults.MaximumInputBytes, cancellation.Token);
        Assert.True(result.IsError); Assert.False(result.IsLoaded);
        Assert.Equal(invalid switch {
            "actor" => "CQL_ACCESS_INVALID", "payload" => "CQL_RESULT_LIMIT", "cancelled" => "CQL_QUERY_CANCELLED",
            "shape" => "CQL_MIXED_QUERY_INVALID", _ => "CQL_PROVIDER_RESULT_INVALID"
        }, result.ErrorCode);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("provider")]
    [InlineData("owner")]
    public async Task CountCannotCombineRowsFromDifferentMappingObservations(string invalid)
    {
        var fixture = await Create(0, 1);
        var changed = await Native(fixture.Mapping, 1, version: invalid == "version" ? 2 : 1,
            provider: invalid == "provider" ? ProviderType.Neo4jOASIS : ProviderType.MongoDBOASIS,
            owner: invalid == "owner" ? Guid.NewGuid() : null, prefix: "changed-");
        var result = CosmicMixedProjectionResults.Count(fixture.Query, Actor, fixture.Mapping, fixture.Stored,
            fixture.Native.Concat(changed).ToArray());
        Assert.True(result.IsError); Assert.False(result.IsLoaded);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode);
    }
    [Fact]
    public void ProjectionCaptureRetainsTerminalOperationAndDetachesDisposedFilterValues()
    {
        using var document = JsonDocument.Parse("\"gold\"");
        var mapping = Guid.NewGuid();
        var request = new CqlProjectionV1Request
        {
            ResultOperation = CqlProjectionV1ResultOperation.LongCount, VirtualMappingId = mapping, Take = 1,
            Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "tier" }, Scalar = CqlV1Scalar.String,
                Operator = CqlV1Operator.Equal, Value = document.RootElement } },
            Columns = new[] { new CqlProjectionV1Column { Field = "Id", Alias = "Id" } }
        };
        var captured = CqlRequestCapture.Capture(request);
        document.Dispose();
        request.ResultOperation = CqlProjectionV1ResultOperation.Rows;
        request.VirtualMappingId = Guid.NewGuid(); request.Filters[0].MetadataPath![0] = "changed";
        request.Columns[0].Alias = "changed";
        Assert.Equal(CqlProjectionV1ResultOperation.LongCount, captured.ResultOperation);
        Assert.Equal(mapping, captured.VirtualMappingId);
        Assert.Equal("tier", captured.Filters[0].MetadataPath![0]);
        Assert.Equal("gold", captured.Filters[0].Value.GetString());
        Assert.Equal("Id", captured.Columns[0].Alias);
        Assert.DoesNotContain("ResultOperation", JsonSerializer.Serialize(new CqlProjectionV1Request()));
        Assert.Contains("\"ResultOperation\":\"LongCount\"", JsonSerializer.Serialize(captured));
    }

    [Fact]
    public void ProjectionCaptureKeepsFunctionInputsAndSelectedOrderingIndependent()
    {
        var request = new CqlProjectionV1Request
        {
            Columns = new[] { new CqlProjectionV1Column { MetadataPath = new[] { "name" },
                ValueType = "String", Alias = "Units", Nullable = true, Function = CqlV1Function.Utf16Length } },
            SelectedOrder = new[] { new CqlGroupedV1Ordering { Alias = "Units", Descending = true } }
        };
        var captured = CqlRequestCapture.Capture(request);
        request.Columns[0].Function = null; request.Columns[0].MetadataPath![0] = "changed";
        request.SelectedOrder[0].Alias = "changed"; request.SelectedOrder[0].Descending = false;
        Assert.Equal(CqlV1Function.Utf16Length, captured.Columns[0].Function);
        Assert.Equal("name", captured.Columns[0].MetadataPath![0]);
        Assert.Equal("Units", captured.SelectedOrder![0].Alias);
        Assert.True(captured.SelectedOrder[0].Descending);
    }

}
