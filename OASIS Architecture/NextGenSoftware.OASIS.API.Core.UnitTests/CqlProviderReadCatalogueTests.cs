using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private static IConfiguration CatalogueConfiguration(Guid id) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> {
            [CqlPublicReadCatalogue.ConfigurationKey] = JsonSerializer.Serialize(
                new Dictionary<string, Guid[]> { ["OracleDBOASIS"] = new[] { id } }) }).Build();

    [Fact]
    public void PublicCatalogueCapturesHostGrantAndNeverAcceptsAnotherAuthority()
    {
        var id = Guid.NewGuid();
        var ids = new[] { id };
        var policy = new CqlPublicReadCatalogue(ProviderType.OracleDBOASIS, ids);
        ids[0] = Guid.NewGuid();
        var first = policy.Authorize(Actor, ProviderType.OracleDBOASIS).Result;
        Assert.True(first.CanRead(new Holon { Id = id }));
        Assert.False(first.CanRead(new Holon { Id = ids[0], CreatedByAvatarId = Actor, IsPublic = true }));
        var other = Guid.NewGuid();
        Assert.Equal(other, policy.Authorize(other, ProviderType.OracleDBOASIS).Result.ActorId);
        Assert.True(policy.Authorize(Actor, ProviderType.MongoDBOASIS).IsError);
        Assert.True(policy.Authorize(Guid.Empty, ProviderType.OracleDBOASIS).IsError);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{\"Default\":[]}")]
    [InlineData("{\"OracleDBOASIS\":[],\"OracleDBOASIS\":[]}")]
    [InlineData("{\"OracleDBOASIS\":null}")]
    [InlineData("{\"OracleDBOASIS\":[\"00000000-0000-0000-0000-000000000000\"]}")]
    [InlineData("{\"OracleDBOASIS\":[\"not-a-guid\"]}")]
    public void InvalidPublicCatalogueCannotPartiallyRegisterPolicies(string json)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            [CqlPublicReadCatalogue.ConfigurationKey] = json }).Build();
        var services = new ServiceCollection();
        Assert.ThrowsAny<ArgumentException>(() => CqlPublicReadCatalogue.Enroll(services, configuration));
        Assert.Empty(services);
    }

    [Fact]
    public void CatalogueConfigurationIsCapturedAndCannotReplaceAnExistingKeyedPolicy()
    {
        var id = Guid.NewGuid();
        var configuration = CatalogueConfiguration(id);
        var services = new ServiceCollection();
        CqlPublicReadCatalogue.Enroll(services, configuration);
        var count = services.Count;
        Assert.Throws<ArgumentException>(() => CqlPublicReadCatalogue.Enroll(services, configuration));
        Assert.Equal(count, services.Count);
        using var provider = services.BuildServiceProvider();
        configuration[CqlPublicReadCatalogue.ConfigurationKey] = "{}";
        Assert.Null(provider.GetService<ICqlReadAccessPolicy>());
        Assert.Null(provider.GetKeyedService<ICqlReadAccessPolicy>(ProviderType.MongoDBOASIS));
        Assert.True(provider.GetRequiredKeyedService<ICqlReadAccessPolicy>(ProviderType.OracleDBOASIS)
            .Authorize(Actor, ProviderType.OracleDBOASIS).Result.CanRead(new Holon { Id = id }));
    }

    [Fact]
    public void PublicCatalogueRejectsDuplicateOversizeAndUnboundedInput()
    {
        var id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new CqlPublicReadCatalogue(ProviderType.OracleDBOASIS, new[] { id, id }));
        Assert.Throws<ArgumentException>(() => new CqlPublicReadCatalogue(ProviderType.OracleDBOASIS,
            Enumerable.Range(0, 129).Select(_ => Guid.NewGuid()).ToArray()));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            [CqlPublicReadCatalogue.ConfigurationKey] = new string(' ', 32769) }).Build();
        Assert.Throws<ArgumentException>(() => CqlPublicReadCatalogue.Enroll(new ServiceCollection(), configuration));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KeyedOracleGrantDoesNotChangeMongoOwnerPublicOrResolveTargetsTwice(bool oracle)
    {
        var id = Guid.NewGuid();
        var type = oracle ? ProviderType.OracleDBOASIS : ProviderType.MongoDBOASIS;
        var source = new Source(type);
        source.Rows.Add(new Holon { Id = id, CreatedByAvatarId = oracle ? Guid.NewGuid() : Actor });
        source.Adapter.SetupGet(adapter => adapter.QueryCapabilities).Returns(new CosmicQueryCapabilities(
            Capabilities.Features | CosmicQueryFeatures.Membership, Capabilities.CanonicalFields.ToArray()));
        var admitted = Admission(type);
        using var services = new ServiceCollection().AddConfiguredCqlReadAccessPolicy(CatalogueConfiguration(id)).BuildServiceProvider();
        admitted.Http.RequestServices = services;
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http,
            new CqlV1Request { Provider = type.ToString(), OrderBy = oracle ? "None" : "Id" }, source));
        Assert.IsType<FileContentResult>(result);
        Assert.Equal(1, source.Lookups);
        Assert.Equal(Actor, source.Observed!.Query.ReadAccess.ActorId);
        Assert.Equal(oracle ? ProviderType.OracleDBOASIS : (ProviderType?)null, source.Observed.Query.ReadAccess.Authority);
        Assert.False(source.Observed.Query.ReadAccess.CanRead(new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid() }));
        Assert.Single(admitted.Receipts);
    }

    [Fact]
    public async Task NullKeyedPolicyIsAnErrorRatherThanAnOwnerPublicGrant()
    {
        var source = new Source(ProviderType.OracleDBOASIS);
        var admitted = Admission(ProviderType.OracleDBOASIS);
        using var services = new ServiceCollection().AddKeyedSingleton<ICqlReadAccessPolicy>(
            ProviderType.OracleDBOASIS, (_, _) => null!).BuildServiceProvider();
        admitted.Http.RequestServices = services;
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http,
            new CqlV1Request { Provider = "OracleDBOASIS", OrderBy = "None" }, source));
        Assert.Equal("CQL_ACCESS_POLICY_INVALID", Code(result));
        Assert.Equal(0, source.Managers);
        Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task GlobalPolicyRejectionCannotFallThroughToAKeyedPublicGrant()
    {
        var id = Guid.NewGuid();
        var source = new Source(ProviderType.OracleDBOASIS);
        var admitted = Admission(ProviderType.OracleDBOASIS);
        var global = new HostedReadPolicy((_, _) => new() { IsError = true, ErrorCode = "denied" });
        using var services = new ServiceCollection().AddConfiguredCqlReadAccessPolicy(CatalogueConfiguration(id))
            .AddSingleton<ICqlReadAccessPolicy>(global).BuildServiceProvider();
        admitted.Http.RequestServices = services;
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http,
            new CqlV1Request { Provider = "OracleDBOASIS", OrderBy = "None" }, source));
        Assert.Equal("CQL_ACCESS_FORBIDDEN", Code(result));
        Assert.Equal(1, global.Calls);
        Assert.Equal(0, source.Managers);
    }

    [Fact]
    public async Task OracleMongoFederationKeepsIndependentAccessPoliciesAndPhysicalReceipts()
    {
        var types = new[] { ProviderType.MongoDBOASIS, ProviderType.OracleDBOASIS };
        var sources = new FederationSources();
        sources.Items.Clear();
        var oracleId = Guid.NewGuid();
        foreach (var type in types)
        {
            var source = new Source(type);
            source.Rows.Add(new Holon { Id = type == ProviderType.OracleDBOASIS ? oracleId : Guid.NewGuid(),
                CreatedByAvatarId = type == ProviderType.OracleDBOASIS ? Guid.NewGuid() : Actor });
            source.Adapter.SetupGet(adapter => adapter.QueryCapabilities).Returns(new CosmicQueryCapabilities(
                Capabilities.Features | CosmicQueryFeatures.Membership, Capabilities.CanonicalFields.ToArray()));
            sources.Items.Add(type, source);
        }
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var handle = (UsageExecutionHandle)admitted.Http.Items[Web4UsageMeasurement.ExecutionKey]!;
        var policy = new PreparedStorageExecution(handle, "oracle-mongo-test", new Dictionary<ProviderType, string>(), types,
            types.Select(type => new StorageQueryTariff(type, 0, "test-v1")));
        using var services = new ServiceCollection().AddConfiguredCqlReadAccessPolicy(CatalogueConfiguration(oracleId)).BuildServiceProvider();
        admitted.Http.RequestServices = services;
        var request = new CqlFederationV1Request { Scope = "Named", Providers = types.Select(type => type.ToString()).ToArray(),
            Source = new CqlV1Request { OrderBy = "None" }, Take = 8, MaximumProviderAttempts = 2 };
        var result = await policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(admitted.Http, request, sources));
        var response = Assert.IsType<FileContentResult>(result);
        using var document = JsonDocument.Parse(response.FileContents);
        Assert.Equal(2, document.RootElement.GetProperty("result").GetProperty("observedInputRows").GetInt32());
        Assert.Equal(2, admitted.Receipts.Count);
        Assert.Null(sources.Items[ProviderType.MongoDBOASIS].Observed!.Query.ReadAccess.Authority);
        Assert.Equal(ProviderType.OracleDBOASIS, sources.Items[ProviderType.OracleDBOASIS].Observed!.Query.ReadAccess.Authority);
        Assert.All(sources.Items.Values, source => Assert.Equal(1, source.Lookups));
    }
}
