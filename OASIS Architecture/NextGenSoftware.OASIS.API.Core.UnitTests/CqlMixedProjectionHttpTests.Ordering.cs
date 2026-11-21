using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private static CqlProjectionV1Request OrderedMappedProjectionRequest(Guid mappingId, ProviderType provider,
        params CqlGroupedV1Ordering[] order)
    {
        var request = MappedProjectionHttpRequest(mappingId, provider);
        request.OrderBy = "None"; request.SelectedOrder = order;
        return request;
    }

    private static JsonElement MixedProjectionResponse(IActionResult response)
    {
        Assert.True(response is FileContentResult, response is ObjectResult error
            ? "Mixed ordering failed: " + Code(response) + " status=" + error.StatusCode : response.GetType().Name);
        using var document = JsonDocument.Parse(((FileContentResult)response).FileContents);
        return document.RootElement.GetProperty("result").Clone();
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ComputedAliasProjectionHttpMixedOrdersLiveWinnerBeforeOneGlobalPage(ProviderType provider)
    {
        var fixture = await MappedProjectionHttpRow(provider);
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, new[] {
            new CosmicProjectionRow(Guid.NewGuid(), "", 0, 0),
            new CosmicProjectionRow(fixture.Row.Id, "obsolete!", 9, 9),
            new CosmicProjectionRow(Guid.NewGuid(), "important", 9, 9)
        });
        var source = new MappedProjectionHttpSource(stored, fixture.MappingId, new[] { fixture.Row });
        var request = OrderedMappedProjectionRequest(fixture.MappingId, provider,
            new CqlGroupedV1Ordering { Alias = "Units", Descending = true },
            new CqlGroupedV1Ordering { Alias = "Text" });
        request.Skip = 1; request.Take = 1;
        var admitted = Admission(provider, maximumProviderAttempts: 4);
        var response = await admitted.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source, mixed: true));
        var result = MixedProjectionResponse(response);
        var row = Assert.Single(result.GetProperty("rows").EnumerateArray());
        Assert.Equal(fixture.Row.Id.ToString("D"), row[0].GetString());
        Assert.Equal("😀", row[1].GetString()); Assert.Equal(2, row[2].GetInt32());
        var provenance = Assert.Single(result.GetProperty("provenance").EnumerateArray());
        Assert.Equal("Virtual", provenance.GetProperty("representation").GetString());
        Assert.True(provenance.GetProperty("alsoMaterialized").GetBoolean());
        Assert.Equal(1, source.VirtualCalls);
    }

    [Fact]
    public async Task ComputedAliasProjectionHttpMixedUsesSharedUnicodeScalarOrderingAfterNumericTie()
    {
        var provider = ProviderType.MongoDBOASIS;
        var fixture = await MappedProjectionHttpRow(provider);
        var stored = new Source(provider);
        var other = Guid.NewGuid();
        MappedProjectionHttpAdapter(stored, provider, new[] { new CosmicProjectionRow(other, "\uE000a", 2, 2) });
        var source = new MappedProjectionHttpSource(stored, fixture.MappingId, new[] { fixture.Row });
        var request = OrderedMappedProjectionRequest(fixture.MappingId, provider,
            new CqlGroupedV1Ordering { Alias = "Units" }, new CqlGroupedV1Ordering { Alias = "Text" });
        var admitted = Admission(provider, maximumProviderAttempts: 4);
        var result = MixedProjectionResponse(await admitted.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source, mixed: true)));
        var rows = result.GetProperty("rows").EnumerateArray().ToArray();
        Assert.Equal(other.ToString("D"), rows[0][0].GetString());
        Assert.Equal(fixture.Row.Id.ToString("D"), rows[1][0].GetString());
    }

    [Fact]
    public async Task ComputedAliasProjectionHttpMixedKeepsNullFirstAndProvenanceAligned()
    {
        var provider = ProviderType.MongoDBOASIS;
        var fixture = await MappedProjectionHttpRow(provider);
        var stored = new Source(provider); var empty = Guid.NewGuid();
        MappedProjectionHttpAdapter(stored, provider, new[] { new CosmicProjectionRow(empty, null, null, null) });
        var source = new MappedProjectionHttpSource(stored, fixture.MappingId, new[] { fixture.Row });
        var request = OrderedMappedProjectionRequest(fixture.MappingId, provider,
            new CqlGroupedV1Ordering { Alias = "Units" });
        var admitted = Admission(provider, maximumProviderAttempts: 4);
        var result = MixedProjectionResponse(await admitted.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source, mixed: true)));
        var rows = result.GetProperty("rows").EnumerateArray().ToArray();
        Assert.Equal(empty.ToString("D"), rows[0][0].GetString());
        Assert.Equal(JsonValueKind.Null, rows[0][2].ValueKind);
        var provenance = result.GetProperty("provenance").EnumerateArray().ToArray();
        Assert.Equal("Materialized", provenance[0].GetProperty("representation").GetString());
        Assert.Equal("Virtual", provenance[1].GetProperty("representation").GetString());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("null")]
    [InlineData("source-order")]
    [InlineData("descending-flag")]
    public async Task ComputedAliasProjectionHttpMixedRejectsInvalidOrderBeforeDispatch(string invalid)
    {
        var provider = ProviderType.MongoDBOASIS;
        var fixture = await MappedProjectionHttpRow(provider);
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, Array.Empty<CosmicProjectionRow>());
        var source = new MappedProjectionHttpSource(stored, fixture.MappingId, new[] { fixture.Row });
        var request = OrderedMappedProjectionRequest(fixture.MappingId, provider,
            new CqlGroupedV1Ordering { Alias = invalid == "missing" ? "Unselected" : "Units" });
        if (invalid == "duplicate") request.SelectedOrder = new[] { request.SelectedOrder![0], request.SelectedOrder[0] };
        if (invalid == "null") request.SelectedOrder = new CqlGroupedV1Ordering[] { null! };
        if (invalid == "source-order") request.OrderBy = "Id";
        if (invalid == "descending-flag") request.Descending = true;
        var admitted = Admission(provider, maximumProviderAttempts: 4);
        var response = await admitted.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source, mixed: true));
        Assert.Equal("CQL_REQUEST_INVALID", Code(response));
        Assert.Equal(0, source.VirtualCalls);
    }

    [Fact]
    public async Task ComputedAliasProjectionHttpMixedRejectsShortenedNativeRead()
    {
        var provider = ProviderType.MongoDBOASIS;
        var fixture = await MappedProjectionHttpRow(provider, readLimit: 1);
        Assert.Equal(1, fixture.Row.ReadLimit); Assert.Equal(Actor, fixture.Row.OwnerAvatarId);
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, Array.Empty<CosmicProjectionRow>());
        var source = new MappedProjectionHttpSource(stored, fixture.MappingId, new[] { fixture.Row });
        var admitted = Admission(provider, maximumProviderAttempts: 4);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http,
            MappedProjectionHttpRequest(fixture.MappingId, provider), source, mixed: true));
        Assert.Equal("CQL_VIRTUAL_SOURCE_LIMIT", Code(response));
    }

    [Fact]
    public async Task ComputedAliasProjectionHttpMixedRejectsDuplicateStoredIdentityExplicitly()
    {
        var provider = ProviderType.MongoDBOASIS;
        var fixture = await MappedProjectionHttpRow(provider); var id = Guid.NewGuid();
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, new[] {
            new CosmicProjectionRow(id, "first", 5, 5), new CosmicProjectionRow(id, "second", 6, 6) });
        var source = new MappedProjectionHttpSource(stored, fixture.MappingId, new[] { fixture.Row });
        var admitted = Admission(provider, maximumProviderAttempts: 4);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http,
            MappedProjectionHttpRequest(fixture.MappingId, provider), source, mixed: true));
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Code(response));
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("mapping")]
    [InlineData("payload")]
    [InlineData("cancelled")]
    [InlineData("source-window")]
    public async Task ComputedAliasProjectionHttpMixedSharedOwnerRejectsInvalidWholeInput(string invalid)
    {
        var provider = ProviderType.MongoDBOASIS;
        var fixture = await MappedProjectionHttpRow(provider);
        var request = MappedProjectionHttpRequest(fixture.MappingId, provider);
        var intent = CqlV1Compiler.CompileProjection(request, Actor, provider, null,
            invalid == "source-window" ? 1 : 100);
        var materialized = CosmicProjectionResults.Materialize(intent, Array.Empty<CosmicProjectionRow>()).Result;
        using var cancellation = new CancellationTokenSource();
        if (invalid == "cancelled") cancellation.Cancel();
        var result = CosmicMixedProjectionResults.Merge(intent,
            invalid == "actor" ? Guid.NewGuid() : Actor, invalid == "mapping" ? Guid.NewGuid() : fixture.MappingId,
            materialized, new[] { fixture.Row }, new[] { new CosmicMixedProjectionOrdering("Units") }, 0, 1,
            invalid == "payload" ? 1 : CosmicMixedProjectionResults.MaximumInputBytes, cancellation.Token);
        Assert.True(result.IsError); Assert.Null(result.Result);
        Assert.Equal(invalid switch {
            "payload" => "CQL_RESULT_LIMIT", "cancelled" => "CQL_QUERY_CANCELLED",
            "source-window" => "CQL_MIXED_QUERY_INVALID", _ => "CQL_PROVIDER_RESULT_INVALID"
        }, result.ErrorCode);
    }

    [Fact]
    public async Task ComputedAliasProjectionHttpMixedBreaksEqualSelectedKeysByLogicalId()
    {
        var provider = ProviderType.MongoDBOASIS;
        var fixture = await MappedProjectionHttpRow(provider);
        var first = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var stored = new Source(provider);
        MappedProjectionHttpAdapter(stored, provider, new[] {
            new CosmicProjectionRow(first, "aa", 2, 2), new CosmicProjectionRow(second, "bb", 2, 2) });
        var source = new MappedProjectionHttpSource(stored, fixture.MappingId, new[] { fixture.Row });
        var request = OrderedMappedProjectionRequest(fixture.MappingId, provider,
            new CqlGroupedV1Ordering { Alias = "Units" });
        var admitted = Admission(provider, maximumProviderAttempts: 4);
        var result = MixedProjectionResponse(await admitted.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, request, source, mixed: true)));
        Assert.Equal(new[] { first, second, fixture.Row.Id }.OrderBy(id => id),
            result.GetProperty("rows").EnumerateArray().Select(row => Guid.Parse(row[0].GetString()!)));
    }
}
