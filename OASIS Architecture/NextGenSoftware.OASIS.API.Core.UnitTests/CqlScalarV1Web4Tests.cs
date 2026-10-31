using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    [Fact]
    public void ScalarSourceHasNoImplicitRowPageAndRetainsTheAuthenticatedActor()
    {
        var source = CqlV1Compiler.CompileScalarSource(new CqlScalarV1Request(), Actor, ProviderType.MongoDBOASIS);
        Assert.Null(source.Limit); Assert.Equal(0, source.Offset); Assert.Equal(Actor, source.ReadAccess.ActorId);
        var rows = CqlV1Compiler.Compile(new CqlV1Request(), Actor, ProviderType.MongoDBOASIS);
        Assert.Equal(25, rows.Limit);
    }

    [Theory]
    [InlineData(0)] [InlineData(25)] [InlineData(10000)]
    public void ScalarSourcePreservesExplicitWindowAndRelationshipIntent(int take)
    {
        var request = new CqlScalarV1Request { Skip = 7, Take = take, AnyDescendants = new[]
        {
            new CqlV1DescendantExists { MaximumDepth = 2, MaximumNodes = 3,
                Filters = new[] { new CqlV1Filter { Field = "Name", Value = Json("\"match\"") } } }
        } };
        var source = CqlV1Compiler.CompileScalarSource(request, Actor, ProviderType.SQLLiteDBOASIS);
        Assert.Equal(7, source.Offset); Assert.Equal(take, source.Limit);
        Assert.Equal(Actor, source.ReadAccess.ActorId);
        Assert.Single(source.Predicates.OfType<CosmicQueryAnyDescendant>());
    }

    [Theory]
    [InlineData(-1)] [InlineData(10001)]
    public void ScalarSourceRejectsInvalidExplicitTake(int take)
        => Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileScalarSource(
            new CqlScalarV1Request { Take = take }, Actor, ProviderType.MongoDBOASIS));

    private static Mock<ICosmicQueryScalarStorageProvider> ScalarAdapter(Source source, ProviderType type)
    {
        var scalar = source.Storage.As<ICosmicQueryScalarStorageProvider>();
        scalar.SetupGet(value => value.QueryProviderType).Returns(type);
        scalar.SetupGet(value => value.QueryCapabilities).Returns(Capabilities);
        scalar.SetupGet(value => value.ScalarCapabilities).Returns(new CosmicScalarCapabilities(
            CosmicScalarOperation.Count, CosmicScalarOperation.LongCount, CosmicScalarOperation.Any));
        return scalar;
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ScalarHttpPreservesExactInt64AndTheExistingDurableAttemptReceipt(ProviderType type)
    {
        var source = new Source(type); var scalar = ScalarAdapter(source, type);
        CosmicScalarPlan<Holon, long>? observed = null;
        scalar.Setup(value => value.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, long>>(), It.IsAny<CancellationToken>()))
            .Returns((CosmicScalarPlan<Holon, long> plan, CancellationToken token) =>
            {
                observed = plan;
                return Task.FromResult(new OASISResult<long>(9007199254740993L) { IsLoaded = true });
            });
        var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Provider = type.ToString() }, source));
        var file = Assert.IsType<FileContentResult>(result);
        using var document = JsonDocument.Parse(file.FileContents);
        var body = document.RootElement.GetProperty("result");
        Assert.Equal("cql-scalar-v1", body.GetProperty("version").GetString());
        Assert.Equal("Int64", body.GetProperty("valueType").GetString());
        Assert.Equal(JsonValueKind.String, body.GetProperty("value").ValueKind);
        Assert.Equal("9007199254740993", body.GetProperty("value").GetString());
        Assert.Equal(type.ToString(), body.GetProperty("provider").GetString());
        Assert.Null(observed!.Query.Source.Limit); Assert.Equal(Actor, observed.Query.Source.ReadAccess.ActorId);
        Assert.Null(source.Observed);
        var receipt = Assert.Single(admitted.Receipts);
        Assert.Equal(1, receipt.Units); Assert.Equal(0.002m, receipt.ActualCostUsd);
        admitted.Store.Verify(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Services.Subscriptions.UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Count")] [InlineData("Any")]
    public async Task ScalarHttpUsesTheDeclaredPrimitiveDomain(string operation)
    {
        var type = ProviderType.SQLLiteDBOASIS;
        var source = new Source(type); var scalar = ScalarAdapter(source, type);
        scalar.Setup(value => value.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<int>(137) { IsLoaded = true });
        scalar.Setup(value => value.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<bool>(true) { IsLoaded = true });
        var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Operation = operation }, source));
        using var document = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
        var body = document.RootElement.GetProperty("result");
        Assert.Equal(operation == "Count" ? "Int32" : "Boolean", body.GetProperty("valueType").GetString());
        Assert.Equal(operation == "Count" ? JsonValueKind.Number : JsonValueKind.True, body.GetProperty("value").ValueKind);
    }

    [Fact]
    public async Task ScalarUnsupportedRowAdapterRejectsWithoutManagerOrAttempt()
    {
        var type = ProviderType.MongoDBOASIS;
        var source = new Source(type); var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http, new(), source));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Code(result)); Assert.Equal(0, source.Managers);
        Assert.Empty(admitted.Receipts);
        admitted.Store.Verify(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Services.Subscriptions.UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Sum")] [InlineData("count")] [InlineData("1")]
    public async Task ScalarUnknownOperationRejectsBeforeProviderLookup(string operation)
    {
        var type = ProviderType.MongoDBOASIS;
        var source = new Source(type); var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request { Operation = operation }, source));
        Assert.Equal("CQL_REQUEST_INVALID", Code(result)); Assert.Equal(0, source.Lookups); Assert.Equal(0, source.Managers);
        Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task ScalarRequiresTheExactAuthenticatedReservationBeforeProviderLookup()
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        var admitted = Admission(ProviderType.MongoDBOASIS, reservedActor: Guid.NewGuid());
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http, new(), source));
        Assert.Equal("CQL_ADMISSION_REQUIRED", Code(result)); Assert.Equal(0, source.Lookups);
    }

    [Fact]
    public void ScalarDtoRejectsCallerActorAndRowOutputFields()
    {
        foreach (var field in new[] { "actorId", "includeMetadata", "plan" })
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CqlScalarV1Request>(
                "{\"" + field + "\":true}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
    }

    [Fact]
    public void ScalarRouteCannotConfigureAQueryTariffAboveItsReservedBudget()
    {
        var settings = OrdinaryConfiguration(cost: 1, reservation: 0.1m);
        settings["PREPARED_STORAGE_ADMISSION_JSON"] = settings["PREPARED_STORAGE_ADMISSION_JSON"]!
            .Replace("/api/data/query-cql", "/api/data/query-cql/scalar", StringComparison.Ordinal);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => Services.Subscriptions.ConfiguredStorageAdmission.AddConfiguredStorageAdmission(
            new ServiceCollection(), configuration, true, _ => null!));
    }
}
