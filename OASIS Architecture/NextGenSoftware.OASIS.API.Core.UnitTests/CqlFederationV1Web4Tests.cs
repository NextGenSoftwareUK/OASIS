using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private static readonly ProviderType[] FederationProviders =
        { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS };

    private sealed class FederationSources : ICqlV1Providers
    {
        internal readonly Dictionary<ProviderType, Source> Items = FederationProviders.ToDictionary(type => type, type => new Source(type));
        internal ProviderType Default = FederationProviders[0];
        public IOASISStorageProvider Resolve(ProviderType? requested) => Items[requested ?? Default].Resolve(requested);
        public HolonManager CreateManager(IOASISStorageProvider provider) => Items[provider.ProviderType.Value].CreateManager(provider);
        internal void NeverRead()
        {
            foreach (var source in Items.Values)
                source.Adapter.Verify(adapter => adapter.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(),
                    It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    private static (DefaultHttpContext Http, PreparedStorageExecution Policy, List<UsageProviderReceipt> Receipts)
        FederationAdmission()
    {
        var baseAdmission = Admission(FederationProviders[0]);
        var handle = (UsageExecutionHandle)baseAdmission.Http.Items[Web4UsageMeasurement.ExecutionKey]!;
        return (baseAdmission.Http, new PreparedStorageExecution(handle, "cql-federation-test", new Dictionary<ProviderType, string>(),
            FederationProviders, FederationProviders.Select(type => new StorageQueryTariff(type, 0.002m, "test-v1"))), baseAdmission.Receipts);
    }

    private sealed class FederationPolicy : ICqlReadAccessPolicy
    {
        internal readonly Dictionary<ProviderType, Guid[]> Ids = new();
        internal readonly List<ProviderType> Calls = new();
        internal ProviderType? Denied;
        internal Action? OnAuthorize;
        public OASISResult<CosmicQueryReadAccess> Authorize(Guid actor, ProviderType authority)
        {
            Assert.Equal(Actor, actor);
            Calls.Add(authority);
            OnAuthorize?.Invoke();
            return Denied == authority ? new() { IsError = true, ErrorCode = "denied" }
                : CosmicQueryReadAccess.AuthoritativeRecords(actor, authority, Ids.GetValueOrDefault(authority) ?? Array.Empty<Guid>());
        }
    }

    [Theory]
    [InlineData("Named")]
    [InlineData("AllEligible")]
    public async Task HostedFederationAuthorizesAllTargetsBeforePagingAndRetainsThreeExactReceipts(string scope)
    {
        var sources = new FederationSources();
        var policy = new FederationPolicy();
        var names = new[] { "Z", "A", "M" };
        for (var index = 0; index < FederationProviders.Length; index++)
        {
            var provider = FederationProviders[index];
            var row = new Holon { Id = Guid.NewGuid(), Name = names[index], CreatedByAvatarId = Guid.NewGuid() };
            sources.Items[provider].Rows.Add(row);
            policy.Ids.Add(provider, new[] { row.Id });
            var capabilities = new CosmicQueryCapabilities(Capabilities.Features | CosmicQueryFeatures.Membership,
                Capabilities.CanonicalFields.ToArray(), Array.Empty<CosmicQueryMetadataCapability>());
            sources.Items[provider].Adapter.SetupGet(adapter => adapter.QueryCapabilities).Returns(capabilities);
        }
        var admitted = FederationAdmission();
        using var services = new ServiceCollection().AddSingleton<ICqlReadAccessPolicy>(policy).BuildServiceProvider();
        admitted.Http.RequestServices = services;
        var request = new CqlFederationV1Request
        {
            Scope = scope, Providers = scope == "Named" ? FederationProviders.Select(type => type.ToString()).ToArray() : Array.Empty<string>(),
            Skip = 1, Take = 1, Source = new CqlV1Request { Skip = 2, Take = 3 }
        };
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(admitted.Http, request, sources));
        var file = Assert.IsType<FileContentResult>(result);
        using var document = JsonDocument.Parse(file.FileContents);
        var output = document.RootElement.GetProperty("result");
        Assert.Equal("cql-federation-v1", output.GetProperty("version").GetString());
        Assert.Equal("M", output.GetProperty("rows")[0].GetProperty("selected").GetProperty("row").GetProperty("name").GetString());
        Assert.Equal(3, output.GetProperty("observedInputRows").GetInt32());
        Assert.Equal(FederationProviders, policy.Calls);
        Assert.Equal(3, admitted.Receipts.Count);
        Assert.Equal(0.006m, admitted.Receipts.Sum(receipt => receipt.ActualCostUsd));
        foreach (var pair in sources.Items)
        {
            Assert.Equal(1, pair.Value.Lookups);
            Assert.Equal(pair.Key, pair.Value.Observed!.Query.ReadAccess.Authority);
            Assert.Equal(2, pair.Value.Observed.Query.Offset);
            Assert.Equal(3, pair.Value.Observed.Query.Limit);
        }
        Assert.DoesNotContain("providerUniqueStorageKey", System.Text.Encoding.UTF8.GetString(file.FileContents), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DefaultProviderAndActorStayCapturedWhenAuthorizerChangesAmbientState()
    {
        var sources = new FederationSources();
        var admitted = FederationAdmission();
        var policy = new FederationPolicy { OnAuthorize = () =>
        { sources.Default = ProviderType.Neo4jOASIS; admitted.Http.Items["Avatar"] = new Avatar { Id = Guid.NewGuid() }; } };
        sources.Items[ProviderType.MongoDBOASIS].Adapter.SetupGet(adapter => adapter.QueryCapabilities).Returns(
            new CosmicQueryCapabilities(Capabilities.Features | CosmicQueryFeatures.Membership,
                Capabilities.CanonicalFields.ToArray(), Array.Empty<CosmicQueryMetadataCapability>()));
        using var services = new ServiceCollection().AddSingleton<ICqlReadAccessPolicy>(policy).BuildServiceProvider();
        admitted.Http.RequestServices = services;
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(admitted.Http, new(), sources));
        Assert.IsType<FileContentResult>(result);
        Assert.Equal(new[] { ProviderType.MongoDBOASIS }, policy.Calls);
        Assert.Equal(1, sources.Items[ProviderType.MongoDBOASIS].Lookups);
        Assert.Equal(0, sources.Items[ProviderType.Neo4jOASIS].Lookups);
        Assert.Single(admitted.Receipts);
    }

    [Fact]
    public async Task LaterPolicyDenialCannotDispatchAnyEarlierAuthorizedTarget()
    {
        var sources = new FederationSources();
        var admitted = FederationAdmission();
        var policy = new FederationPolicy { Denied = ProviderType.SQLLiteDBOASIS };
        using var services = new ServiceCollection().AddSingleton<ICqlReadAccessPolicy>(policy).BuildServiceProvider();
        admitted.Http.RequestServices = services;
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(admitted.Http,
            new() { Scope = "Named", Providers = FederationProviders.Select(type => type.ToString()).ToArray() }, sources));
        Assert.Equal("CQL_ACCESS_FORBIDDEN", Code(result));
        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        sources.NeverRead();
        Assert.Empty(admitted.Receipts);
        Assert.Equal(2, policy.Calls.Count);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    public async Task MalformedFederationIntentRejectsBeforeLookupOrExecution(int fault)
    {
        var request = new CqlFederationV1Request();
        switch (fault)
        {
            case 0: request.Scope = "all"; break;
            case 1: request.Scope = "Named"; break;
            case 2: request.Scope = "Named"; request.Providers = new[] { "MongoDBOASIS", "MongoDBOASIS" }; break;
            case 3: request.Providers = new[] { "Neo4jOASIS" }; break;
            case 4: request.IdentityMode = "Freshest"; break;
            case 5: request.MaximumDurationMilliseconds = 5001; break;
            case 6: request.MaximumOutputBytes = int.MaxValue; break;
            case 7: request.Source.Provider = "MongoDBOASIS"; break;
            case 8: request.Source.Graph = new(); break;
            case 9: request.MaximumInputBytes = 0; break;
            case 10: request.Scope = "Named"; request.Providers = FederationProviders.Select(type => type.ToString()).ToArray(); request.MaximumProviderAttempts = 1; break;
            case 11: request.Source.Take = 101; break;
        }
        var sources = new FederationSources();
        var admitted = FederationAdmission();
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(admitted.Http, request, sources));
        Assert.Equal("CQL_REQUEST_INVALID", Code(result));
        Assert.All(sources.Items.Values, source => Assert.Equal(0, source.Lookups));
        sources.NeverRead();
        Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task FederationCannotRunWithoutAuthenticatedUsageAdmission()
    {
        var sources = new FederationSources();
        var anonymous = await CqlV1Endpoint.ExecuteFederationAsync(new DefaultHttpContext(), new(), sources);
        Assert.Equal("CQL_AUTHENTICATION_REQUIRED", Code(anonymous));
        var admitted = FederationAdmission();
        var unbound = await CqlV1Endpoint.ExecuteFederationAsync(admitted.Http, new(), sources);
        Assert.Equal("CQL_ADMISSION_REQUIRED", Code(unbound));
        sources.NeverRead();
    }

    [Fact]
    public void FederationWireRejectsClientGrantAndTariffFields()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CqlFederationV1Request>("{\"Access\":{}}"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CqlFederationV1Request>("{\"Tariff\":0}"));
        Assert.True(CosmicQueryHttpRoutes.Contains("/api/data/query-cql/federation"));
    }

    [Fact]
    public async Task FederationChecksActualPublicWireBudgetEvenForAnEmptyCanonicalResult()
    {
        var admitted = FederationAdmission();
        var sources = new FederationSources();
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(admitted.Http,
            new() { MaximumOutputBytes = 250 }, sources));
        Assert.Equal("CQL_FEDERATION_OUTPUT_LIMIT", Code(result));
        Assert.Equal(413, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Single(admitted.Receipts);
    }
}
