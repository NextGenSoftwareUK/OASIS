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
    private static Mock<ICosmicQueryProjectionCompositionStorageProvider> ComputedScalarHttpAdapter(
        Source source, ProviderType provider, bool malformed = false, bool inaccessible = false)
    {
        var adapter = ComputedAliasAdapter(source, provider);
        var reads = new CosmicProjectionCapabilities(
            new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>())
            .WithFunctions(CosmicProjectionFunction.Utf16Length);
        adapter.SetupGet(value => value.CompositionCapabilities).Returns(new CosmicProjectionCompositionCapabilities(
            CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging,
            CosmicQueryFeatures.None, Array.Empty<CosmicQueryMetadataCapability>(), new[] { typeof(int?) }, 32,
            numericReductions: new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(),
                new[] { CosmicScalarOperation.Sum, CosmicScalarOperation.Average }.Select(operation =>
                    new CosmicScalarSelectionCapability(operation, reads, new[] { typeof(int?) })).ToArray())));
        adapter.Setup(value => value.ExecuteCosmicProjectionCompositionAsync(
            It.IsAny<CosmicProjectionCompositionPlan<Holon, int?>>(),
            It.IsAny<CosmicProjectionCompositionExecutionLimits>(), It.IsAny<CancellationToken>()))
            .Returns((CosmicProjectionCompositionPlan<Holon, int?> plan,
                CosmicProjectionCompositionExecutionLimits limits, CancellationToken token) =>
            {
                Assert.True(token.CanBeCanceled);
                Assert.Equal(Actor, plan.Query.Source.ReadAccess.ActorId);
                var column = Assert.Single(plan.Query.Columns);
                Assert.Equal(CosmicProjectionFunction.Utf16Length, column.Function);
                Assert.Equal(typeof(string), column.Path.ValueType);
                Assert.Equal(typeof(int?), column.ValueType);
                object cell = malformed ? (object)"6" : 6;
                var ordering = plan.Query.Source.Orderings.Select(term =>
                    term.Path.Segments.Single() == "Id" ? (object)CompositionId : "coffee").ToArray();
                var packet = new CosmicProjectionCompositionPacket(
                    new[] { new CosmicProjectionInputRow(CompositionId, !inaccessible,
                        inaccessible ? Guid.Empty : Actor, new[] { cell }, ordering) },
                    plan.Query.ResultStages.Select(_ => new CosmicProjectionStageProof(new[] { CompositionId })).ToArray(),
                    new[] { new CosmicProjectionCompositionRow(CompositionId, cell) }, 1, plan.Query.ResultStages.Count,
                    null, plan.NumericReduction == null ? null : new CosmicProjectedNumericPacket(1, new[] { 0L, 6L }));
                return Task.FromResult(new OASISResult<CosmicProjectionCompositionPacket>(packet) { IsLoaded = true });
            });
        return adapter;
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS, "Sum")]
    [InlineData(ProviderType.MongoDBOASIS, "Average")]
    [InlineData(ProviderType.MongoDBOASIS, "Min")]
    [InlineData(ProviderType.MongoDBOASIS, "Max")]
    [InlineData(ProviderType.SQLLiteDBOASIS, "Sum")]
    [InlineData(ProviderType.SQLLiteDBOASIS, "Average")]
    [InlineData(ProviderType.SQLLiteDBOASIS, "Min")]
    [InlineData(ProviderType.SQLLiteDBOASIS, "Max")]
    [InlineData(ProviderType.Neo4jOASIS, "Sum")]
    [InlineData(ProviderType.Neo4jOASIS, "Average")]
    [InlineData(ProviderType.Neo4jOASIS, "Min")]
    [InlineData(ProviderType.Neo4jOASIS, "Max")]
    public async Task ComputedAliasProjectionHttpScalarUsesOneNativeOwnerAndExactWire(
        ProviderType provider, string operation)
    {
        var source = new Source(provider);
        var adapter = ComputedScalarHttpAdapter(source, provider);
        var admitted = Admission(provider);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request
            {
                Operation = operation, OrderBy = "None",
                Selection = new() { Field = "Name", Nullable = true, Function = CqlV1Function.Utf16Length }
            }, source));
        using var document = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
        var body = document.RootElement.GetProperty("result");
        Assert.Equal("cql-scalar-v1", body.GetProperty("version").GetString());
        Assert.Equal(provider.ToString(), body.GetProperty("provider").GetString());
        Assert.Equal(operation, body.GetProperty("operation").GetString());
        Assert.Equal(operation == "Average" ? "Decimal" : "Int32", body.GetProperty("valueType").GetString());
        if (operation == "Average") Assert.Equal("6", body.GetProperty("value").GetString());
        else Assert.Equal(6, body.GetProperty("value").GetInt32());
        Assert.Single(admitted.Receipts);
        Assert.Null(source.Observed);
        adapter.Verify(value => value.ExecuteCosmicProjectionCompositionAsync(
            It.IsAny<CosmicProjectionCompositionPlan<Holon, int?>>(),
            It.IsAny<CosmicProjectionCompositionExecutionLimits>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ComputedAliasProjectionHttpScalarRejectsInvalidOrPrivateParticipation(bool inaccessible)
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        ComputedScalarHttpAdapter(source, ProviderType.MongoDBOASIS,
            malformed: !inaccessible, inaccessible: inaccessible);
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Operation = "Sum", OrderBy = "None",
                Selection = new() { Field = "Name", Nullable = true, Function = CqlV1Function.Utf16Length } }, source));
        Assert.IsType<ObjectResult>(result);
        Assert.Single(admitted.Receipts);
    }
}
