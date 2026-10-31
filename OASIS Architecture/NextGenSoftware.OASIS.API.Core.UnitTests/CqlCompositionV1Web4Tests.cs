using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private static readonly Guid CompositionId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static CqlCompositionV1Request CompositionRequest() => new()
    {
        Source = new() { Columns = new[]
        {
            new CqlProjectionV1Column { Alias = "name", Field = "Name" },
            new CqlProjectionV1Column { Alias = "stock", MetadataPath = new[] { "stock" }, ValueType = "Int64" }
        } },
        Stages = new[] { new CqlCompositionV1Stage { Operation = "Take", Count = 1 } }
    };
    private static CosmicProjectionCompositionPacket CompositionPacket(bool inaccessible = false) => new(
        new[] { new CosmicProjectionInputRow(CompositionId, !inaccessible, inaccessible ? Guid.NewGuid() : Actor,
            new object[] { "coffee", 9007199254740993L }, new object[] { "coffee", CompositionId }) },
        new[] { new CosmicProjectionStageProof(new[] { CompositionId }) },
        new[] { new CosmicProjectionCompositionRow(CompositionId, "coffee", 9007199254740993L) }, 1, 1);

    private static Mock<ICosmicQueryProjectionCompositionStorageProvider> CompositionAdapter(Source source,
        CosmicProjectionCompositionPacket packet)
    {
        var adapter = source.Storage.As<ICosmicQueryProjectionCompositionStorageProvider>();
        adapter.SetupGet(value => value.ProjectionCapabilities).Returns(new CosmicProjectionCapabilities(
            new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, new[] { typeof(long) }));
        adapter.SetupGet(value => value.CompositionCapabilities).Returns(new CosmicProjectionCompositionCapabilities(
            CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging,
            CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Not | CosmicQueryFeatures.NullLiteral,
            new[] { new CosmicQueryMetadataCapability(typeof(string), Enum.GetValues<CosmicQueryComparisonOperator>()),
                new CosmicQueryMetadataCapability(typeof(long), Enum.GetValues<CosmicQueryComparisonOperator>()) },
            new[] { typeof(string), typeof(long) }));
        adapter.Setup(value => value.ExecuteCosmicProjectionCompositionAsync(
            It.IsAny<CosmicProjectionCompositionPlan<Holon, CosmicProjectionRecord>>(),
            It.IsAny<CosmicProjectionCompositionExecutionLimits>(), It.IsAny<CancellationToken>()))
            .Returns((CosmicProjectionCompositionPlan<Holon, CosmicProjectionRecord> plan,
                CosmicProjectionCompositionExecutionLimits limits, CancellationToken token) =>
            {
                Assert.Equal(Actor, plan.Query.Source.ReadAccess.ActorId);
                Assert.Equal(100, limits.MaximumInputRows); Assert.Equal(1600, limits.MaximumStageRows);
                Assert.Equal(25600, limits.MaximumProofPayloadBytes);
                return Task.FromResult(new OASISResult<CosmicProjectionCompositionPacket>(packet) { IsLoaded = true });
            });
        return adapter;
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public async Task CompositionHttpUsesProtectedGenericExecutionExactWireAndOneReceipt(ProviderType provider)
    {
        var source = new Source(provider); CompositionAdapter(source, CompositionPacket()); var admitted = Admission(provider);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteCompositionAsync(admitted.Http, CompositionRequest(), source));
        using var document = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
        var body = document.RootElement.GetProperty("result");
        Assert.Equal("cql-composition-v1", body.GetProperty("version").GetString());
        Assert.Equal(provider.ToString(), body.GetProperty("provider").GetString());
        Assert.Equal("9007199254740993", body.GetProperty("rows")[0][1].GetString());
        Assert.Equal("Int64", body.GetProperty("columns")[1].GetProperty("valueType").GetString());
        var receipt = Assert.Single(admitted.Receipts); Assert.Equal(1, receipt.Units); Assert.Equal(0.002m, receipt.ActualCostUsd);
        Assert.Null(source.Observed);
        admitted.Store.Verify(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Services.Subscriptions.UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task InvalidCompositionRejectsBeforeLookupOrManager(int fault)
    {
        var source = new Source(ProviderType.SQLLiteDBOASIS); var admitted = Admission(ProviderType.SQLLiteDBOASIS);
        var request = CompositionRequest();
        if (fault == 0) request.Source.Provider = "MongoDBOASIS";
        if (fault == 1) request.Stages[0].Count = 101;
        if (fault == 2) request.Source.Graph = new();
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteCompositionAsync(admitted.Http, request, source));
        Assert.Equal("CQL_REQUEST_INVALID", Code(result)); Assert.Equal(0, source.Lookups); Assert.Equal(0, source.Managers); Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task UnsupportedCompositionAdapterDoesNotAcquireManagerOrAttempt()
    {
        var source = new Source(ProviderType.SQLLiteDBOASIS); var admitted = Admission(ProviderType.SQLLiteDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteCompositionAsync(admitted.Http, CompositionRequest(), source));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Code(result)); Assert.Equal(0, source.Managers); Assert.Empty(admitted.Receipts);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CompositionDeniedActorOrMissingTariffCannotBeginAnAttempt(bool missingTariff)
    {
        var source = new Source(ProviderType.SQLLiteDBOASIS); var adapter = CompositionAdapter(source, CompositionPacket());
        var admitted = Admission(ProviderType.SQLLiteDBOASIS, reservedActor: missingTariff ? null : Guid.NewGuid(), tariff: !missingTariff);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteCompositionAsync(admitted.Http, CompositionRequest(), source));
        Assert.IsType<ObjectResult>(result); Assert.Empty(admitted.Receipts);
        adapter.Verify(value => value.ExecuteCosmicProjectionCompositionAsync(It.IsAny<CosmicProjectionCompositionPlan<Holon, CosmicProjectionRecord>>(),
            It.IsAny<CosmicProjectionCompositionExecutionLimits>(), It.IsAny<CancellationToken>()), Times.Never);
        admitted.Store.Verify(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Services.Subscriptions.UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CompositionRejectsPrivateOrIncompleteNativeProofBeforeWirePublication(bool incomplete)
    {
        var source = new Source(ProviderType.SQLLiteDBOASIS);
        var packet = incomplete ? new CosmicProjectionCompositionPacket(Array.Empty<CosmicProjectionInputRow>(),
            Array.Empty<CosmicProjectionStageProof>(), Array.Empty<CosmicProjectionCompositionRow>(), 1, 1) : CompositionPacket(true);
        CompositionAdapter(source, packet); var admitted = Admission(ProviderType.SQLLiteDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteCompositionAsync(admitted.Http, CompositionRequest(), source));
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Code(result)); Assert.IsType<ObjectResult>(result);
    }

    [Fact]
    public async Task CompositionPreCancellationHasNoProviderLookupOrReceipt()
    {
        var source = new Source(ProviderType.SQLLiteDBOASIS); var admitted = Admission(ProviderType.SQLLiteDBOASIS);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel(); admitted.Http.RequestAborted = cancellation.Token;
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteCompositionAsync(admitted.Http, CompositionRequest(), source));
        Assert.Equal("CQL_QUERY_CANCELLED", Code(result)); Assert.Equal(0, source.Lookups); Assert.Empty(admitted.Receipts);
    }
}
