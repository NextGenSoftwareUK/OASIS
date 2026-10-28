using System.Text.Json;
using Moq;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    private CqlIndexedV1Binding WebBinding(Reader reader, Guid? principal = null) => new(reader,
        Mock.Of<ICosmicChainQuerySourceObserver>(), Tariffs(), position, 10, TimeSpan.FromSeconds(60),
        CosmicQueryReadAccess.OwnerOrPublic(principal ?? actor).Result, Manager(), maximumResults: 2);
    private static CqlIndexedV1Request WebRequest() => new() { Alias = "solana-demo",
        Query = new() { OrderBy = "Id", Take = 1 } };

    [Fact]
    public void PreparationFreezesIntentBeforeSingleServerAliasResolutionAndBoundsReservation()
    {
        var f = Fixture("rows"); var request = WebRequest();
        request.MaximumLagSlots = 2; request.MaximumHeadAgeSeconds = 30;
        var registry = new BindingRegistry(WebBinding(f.Reader), () => {
            request.Alias = "other"; request.Query.Take = 99; request.Query.IncludeMetadata = true;
            request.MaximumLagSlots = 999; request.MaximumHeadAgeSeconds = 999;
        });
        var result = CqlIndexedV1Preparation.Prepare(actor, request, registry);
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.Equal(1, registry.Calls); Assert.Equal(0, f.Reader.Calls);
        var prepared = result.Result;
        Assert.Equal("solana-demo", prepared.Alias);
        Assert.Equal(1, prepared.Plan.Query.Limit);
        Assert.False(prepared.IncludeMetadata);
        Assert.Equal(2, prepared.Plan.Query.IndexedSource.MaximumLagSlots);
        Assert.Equal(TimeSpan.FromSeconds(30), prepared.Plan.Query.IndexedSource.MaximumHeadAge);
        Assert.Equal(source.SourceProvider, Assert.Single(prepared.Plan.Targets).Provider);
        Assert.Equal(1m, prepared.MaximumServiceCostUsd);
    }

    [Theory]
    [InlineData("age")] [InlineData("lag")] [InlineData("actor")] [InlineData("limit")] [InlineData("metadata")] [InlineData("metadata-exists")]
    public void PreparationRejectsWeakerFreshnessWrongActorUnsupportedMetadataAndResultLimits(string mode)
    {
        var f = Fixture("rows"); var request = WebRequest();
        if (mode == "age") request.MaximumHeadAgeSeconds = 61;
        if (mode == "lag") request.MaximumLagSlots = 11;
        if (mode == "limit") request.Query.Take = 3;
        if (mode is "metadata" or "metadata-exists") request.Query.Filters = new[] { new CqlV1Filter {
            MetadataPath = new[] { "private" }, Scalar = CqlV1Scalar.String, Operator = mode == "metadata" ? CqlV1Operator.Equal : CqlV1Operator.Exists,
            Value = mode == "metadata" ? JsonSerializer.SerializeToElement("value") : default } };
        var registry = new BindingRegistry(WebBinding(f.Reader, mode == "actor" ? Guid.NewGuid() : actor));
        var result = CqlIndexedV1Preparation.Prepare(actor, request, registry);
        Assert.True(result.IsError); Assert.Null(result.Result);
        Assert.Equal(mode == "actor" ? "CQL_INDEX_BINDING_INVALID" : mode is "metadata" or "metadata-exists"
            ? "CQL_CAPABILITY_UNSUPPORTED" : "CQL_REQUEST_INVALID", result.ErrorCode);
        Assert.Equal(1, registry.Calls); Assert.Equal(0, f.Reader.Calls);
    }

    [Theory]
    [InlineData("alias")] [InlineData("provider")] [InlineData("graph")]
    public void InvalidSelectorsNeverInvokeTheServerResolver(string mode)
    {
        var f = Fixture("rows"); var request = WebRequest();
        if (mode == "alias") request.Alias = "https://caller-host/";
        if (mode == "provider") request.Query.Provider = "SQLLiteDBOASIS";
        if (mode == "graph") request.Query.Graph = new();
        var registry = new BindingRegistry(WebBinding(f.Reader));
        var result = CqlIndexedV1Preparation.Prepare(actor, request, registry);
        Assert.Equal("CQL_REQUEST_INVALID", result.ErrorCode);
        Assert.Equal(0, registry.Calls); Assert.Equal(0, f.Reader.Calls);
    }

    [Fact]
    public void WireContractRejectsCallerEndpointsAndPrices()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CqlIndexedV1Request>("{\"Alias\":\"demo\",\"Endpoint\":\"https://caller/\"}"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CqlIndexedV1Request>("{\"Alias\":\"demo\",\"CostUsd\":0}"));
    }

    private sealed class BindingRegistry(CqlIndexedV1Binding binding, Action? beforeReturn = null) : ICqlIndexedV1Bindings
    {
        public int Calls;
        public OASISResult<CqlIndexedV1Binding> Resolve(Guid authenticatedActor, string alias)
        { Calls++; beforeReturn?.Invoke(); return new(binding); }
    }
}
