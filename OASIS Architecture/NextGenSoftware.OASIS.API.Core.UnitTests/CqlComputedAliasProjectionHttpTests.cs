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
    private static CqlProjectionV1Request ComputedAliasRequest() => new()
    {
        OrderBy = "None", Take = 1,
        Columns = new[] { new CqlProjectionV1Column
        {
            Alias = "Units", Field = "Name", Function = CqlV1Function.Utf16Length, Nullable = true
        } },
        SelectedOrder = new[] { new CqlGroupedV1Ordering { Alias = "Units", Descending = true } }
    };

    private static Mock<ICosmicQueryProjectionCompositionStorageProvider> ComputedAliasAdapter(
        Source source, ProviderType provider, bool malformed = false, bool inaccessible = false)
    {
        ProjectionAdapter(source, provider, Array.Empty<CosmicProjectionRow>());
        var adapter = CompositionAdapter(source, CompositionPacket());
        adapter.SetupGet(value => value.ProjectionCapabilities).Returns(new CosmicProjectionCapabilities(
            new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>())
            .WithFunctions(CosmicProjectionFunction.Utf16Length));
        adapter.SetupGet(value => value.CompositionCapabilities).Returns(new CosmicProjectionCompositionCapabilities(
            CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging,
            CosmicQueryFeatures.None, Array.Empty<CosmicQueryMetadataCapability>(), new[] { typeof(int?) }));
        adapter.Setup(value => value.ExecuteCosmicProjectionCompositionAsync(
            It.IsAny<CosmicProjectionCompositionPlan<Holon, CosmicProjectionRecord>>(),
            It.IsAny<CosmicProjectionCompositionExecutionLimits>(), It.IsAny<CancellationToken>()))
            .Returns((CosmicProjectionCompositionPlan<Holon, CosmicProjectionRecord> plan,
                CosmicProjectionCompositionExecutionLimits limits, CancellationToken token) =>
            {
                Assert.Null(plan.Query.Source.Limit);
                Assert.Equal(Actor, plan.Query.Source.ReadAccess.ActorId);
                Assert.Equal(100, limits.MaximumInputRows);
                Assert.Equal(1600, limits.MaximumStageRows);
                Assert.IsType<CosmicSelectedOrderStage>(plan.Query.ResultStages[0]);
                object cell = malformed ? (object)"6" : 6;
                var ordering = plan.Query.Source.Orderings.Select(term =>
                    term.Path.Segments.Single() == "Id" ? (object)CompositionId :
                        throw new InvalidOperationException("Computed order must not be pushed onto the original string.")).ToArray();
                var packet = new CosmicProjectionCompositionPacket(
                    new[] { new CosmicProjectionInputRow(CompositionId, !inaccessible,
                        inaccessible ? Guid.Empty : Actor, new[] { cell }, ordering) },
                    plan.Query.ResultStages.Select(_ => new CosmicProjectionStageProof(new[] { CompositionId })).ToArray(),
                    new[] { new CosmicProjectionCompositionRow(CompositionId, cell) },
                    1, plan.Query.ResultStages.Count);
                return Task.FromResult(new OASISResult<CosmicProjectionCompositionPacket>(packet) { IsLoaded = true });
            });
        return adapter;
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ComputedAliasProjectionHttpUsesNativeCompositionAndIntegerWire(ProviderType provider)
    {
        var source = new Source(provider);
        var adapter = ComputedAliasAdapter(source, provider);
        var admitted = Admission(provider);
        var result = await admitted.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, ComputedAliasRequest(), source));
        using var document = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
        var body = document.RootElement.GetProperty("result");
        Assert.Equal("cql-projection-v1", body.GetProperty("version").GetString());
        Assert.Equal(provider.ToString(), body.GetProperty("provider").GetString());
        Assert.Equal(6, body.GetProperty("rows")[0][0].GetInt32());
        Assert.Equal("Int32", body.GetProperty("columns")[0].GetProperty("valueType").GetString());
        Assert.True(body.GetProperty("columns")[0].GetProperty("nullable").GetBoolean());
        Assert.Single(admitted.Receipts);
        Assert.Null(source.Observed);
        adapter.Verify(value => value.ExecuteCosmicProjectionCompositionAsync(
            It.IsAny<CosmicProjectionCompositionPlan<Holon, CosmicProjectionRecord>>(),
            It.IsAny<CosmicProjectionCompositionExecutionLimits>(), It.IsAny<CancellationToken>()), Times.Once);
        source.Storage.As<ICosmicQueryProjectionStorageProvider>().Verify(value => value.ExecuteCosmicProjectionAsync(
            It.IsAny<CosmicProjectionPlan<Holon, CosmicProjectionRecord>>(), It.IsAny<int>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ComputedAliasProjectionHttpRejectsInvalidDomainAndPrivateParticipation(bool malformed)
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        ComputedAliasAdapter(source, ProviderType.MongoDBOASIS, malformed, !malformed);
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var result = await admitted.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, ComputedAliasRequest(), source));
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Code(result));
        Assert.IsType<ObjectResult>(result);
    }
}
