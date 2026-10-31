using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlHostedSourcesTests
{
    public interface QueryStorage : IOASISStorageProvider, ICosmicQueryStorageProvider { }
    public class Adapter : DispatchProxy
    {
        public ProviderType Type;
        public ProviderType? QueryType;
        public bool Active;
        protected override object Invoke(MethodInfo method, object[] arguments) => method.Name switch
        {
            "get_ProviderType" => new NextGenSoftware.Utilities.EnumValue<ProviderType>(Type),
            "get_QueryProviderType" => QueryType ?? Type,
            "get_IsProviderActivated" => Active,
            "get_QueryCapabilities" => CosmicQueryCapabilities.ForStringMetadataKeyLookup("NativeKey"),
            _ => throw new InvalidOperationException("Discovery must not execute a native operation.")
        };
    }

    private sealed class Registry : ICqlHostedSourceRegistry
    {
        public readonly Dictionary<ProviderType, QueryStorage> Providers = new();
        public readonly List<ProviderType> Activations = new();
        public readonly TaskCompletionSource Attempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource? Release;
        public bool Fail;
        public IOASISStorageProvider Find(ProviderType type) => Providers.GetValueOrDefault(type);
        public QueryStorage Add(ProviderType type, bool active)
        {
            var storage = DispatchProxy.Create<QueryStorage, Adapter>();
            var adapter = (Adapter)(object)storage;
            adapter.Type = type; adapter.Active = active;
            Providers.Add(type, storage);
            return storage;
        }
        public async Task ActivateReadSourceAsync(ProviderType type, CancellationToken cancellationToken)
        {
            Activations.Add(type); Attempted.TrySetResult();
            try
            {
                if (Release != null) await Release.Task.WaitAsync(cancellationToken);
                if (Fail) throw new InvalidOperationException("private-native-token-must-not-leak");
                ((Adapter)(object)Providers[type]).Active = true;
            }
            finally { Finished.TrySetResult(); }
        }
    }

    private static PricedStorageAdmissionFactory Admission() => new(null, Array.Empty<NativeStorageAdmissionBinding>(),
        new[] { ProviderType.MongoDBOASIS },
        new[] { new StorageQueryTariff(ProviderType.MongoDBOASIS, 0, "test-v1"),
            new StorageQueryTariff(ProviderType.ActivityPubOASIS, 0, "test-v1") },
        null, new[] { ProviderType.ActivityPubOASIS });

    private static CqlHostedSources Sources(Registry registry, bool configured = true)
        => new(configured ? Admission() : null, registry, NullLogger<CqlHostedSources>.Instance);

    [Fact]
    public async Task FederationDependencyOutageDoesNotDisableThePublicSourceOrActivateAnotherProvider()
    {
        var registry = new Registry();
        var mongo = (Adapter)(object)registry.Add(ProviderType.MongoDBOASIS, true);
        registry.Add(ProviderType.ActivityPubOASIS, false);
        using var sources = new CqlHostedSources(Admission(), registry, NullLogger<CqlHostedSources>.Instance,
            CqlHostedQueryExamples.FromJson(CqlHostedFederationExamplesTests.Valid));
        await sources.StartAsync(default);
        await sources.ExecuteTask!;
        using var services = new ServiceCollection().AddSingleton(sources).BuildServiceProvider();
        var controller = new CqlV1Controller(null) { ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { RequestServices = services }
        } };
        var v1 = Assert.IsType<OASISResult<object>>(Assert.IsType<OkObjectResult>(controller.Providers()).Value);
        var v2 = Assert.IsType<OASISResult<object>>(Assert.IsType<OkObjectResult>(controller.Providers(2)).Value);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<CqlHostedSourceDescription>>(v1.Result)
            .Single(row => row.Provider == "ActivityPubOASIS").Examples);
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<CqlHostedSourceDescription>>(v2.Result)
            .Single(row => row.Provider == "ActivityPubOASIS").Examples);
        Assert.Empty(sources.Describe().Single(row => row.Provider == "ActivityPubOASIS").Examples);
        Assert.True(sources.Describe(includeFederationExamples: true).Single(row => row.Provider == "ActivityPubOASIS").Examples.Single().Available);
        mongo.Active = false;
        var social = sources.Describe(includeFederationExamples: true).Single(row => row.Provider == "ActivityPubOASIS");
        Assert.True(social.Available);
        Assert.False(social.Examples.Single().Available);
        Assert.Equal("CQL_DEMO_TARGET_UNAVAILABLE", social.Examples.Single().ErrorCode);
        Assert.Equal(new[] { ProviderType.ActivityPubOASIS }, registry.Activations);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void UnsupportedExampleVersionsAreRejectedWithoutImplicitDowngrade(int version)
    {
        var response = Assert.IsType<BadRequestObjectResult>(new CqlV1Controller(null).Providers(version));
        Assert.Equal("CQL_EXAMPLE_SCHEMA_UNSUPPORTED", Assert.IsType<OASISResult<object>>(response.Value).ErrorCode);
    }

    [Fact]
    public void ConfiguredButInactiveSourcesAreNotClaimedAvailableAndDiscoveryDoesNotActivate()
    {
        var registry = new Registry();
        registry.Add(ProviderType.ActivityPubOASIS, false);
        using var sources = Sources(registry);
        Assert.All(sources.Describe(), row => Assert.False(row.Available));
        Assert.Empty(registry.Activations);
    }

    [Fact]
    public async Task HostActivatesOnlyExplicitReadSourcesAndKeepsExistingProviderState()
    {
        var registry = new Registry();
        registry.Add(ProviderType.MongoDBOASIS, true);
        registry.Add(ProviderType.ActivityPubOASIS, false);
        using var sources = Sources(registry);
        await sources.StartAsync(default);
        await registry.Finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await sources.ExecuteTask!;
        Assert.Equal(new[] { ProviderType.ActivityPubOASIS }, registry.Activations);
        Assert.All(sources.Describe(), row => Assert.True(row.Available));
        var social = sources.Describe().Single(row => row.Provider == "ActivityPubOASIS");
        Assert.True(social.QueryOnly);
        Assert.True(social.FederationAvailable); // Actual bounded route admission; each query still needs a complete native plan.
        Assert.True(sources.Describe().Single(row => row.Provider == "MongoDBOASIS").FederationAvailable);
        await sources.StopAsync(default);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingActivationIsNotAdvertisedAsReady(bool alreadyActive)
    {
        var registry = new Registry { Release = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        registry.Add(ProviderType.ActivityPubOASIS, alreadyActive);
        using var sources = Sources(registry);
        await sources.StartAsync(default);
        await registry.Attempted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(sources.Describe().Single(row => row.QueryOnly).Available);
        registry.Release.SetResult();
        await sources.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(sources.Describe().Single(row => row.QueryOnly).Available);
        await sources.StopAsync(default);
    }

    [Fact]
    public async Task FailedOptionalSourceIsExplicitAndNeverSubstitutesMongoOrLeaksNativeDetails()
    {
        var registry = new Registry { Fail = true };
        registry.Add(ProviderType.MongoDBOASIS, true);
        registry.Add(ProviderType.ActivityPubOASIS, false);
        using var sources = Sources(registry);
        await sources.StartAsync(default);
        await registry.Finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await sources.ExecuteTask!;
        var description = sources.Describe();
        var social = description.Single(row => row.QueryOnly);
        Assert.False(social.Available);
        Assert.Equal("CQL_SOURCE_UNAVAILABLE", social.ErrorCode);
        Assert.DoesNotContain("private-native-token", System.Text.Json.JsonSerializer.Serialize(description));
        Assert.True(description.Single(row => !row.QueryOnly).Available);
        Assert.Equal(new[] { ProviderType.ActivityPubOASIS }, registry.Activations);
        await sources.StopAsync(default);
    }

    [Fact]
    public void MismatchedAdapterIdentityDoesNotAppearReady()
    {
        var registry = new Registry();
        var social = registry.Add(ProviderType.ActivityPubOASIS, true);
        ((Adapter)(object)social).QueryType = ProviderType.Neo4jOASIS;
        using var sources = Sources(registry);
        Assert.False(sources.Describe().Single(row => row.QueryOnly).Available);
    }

    [Fact]
    public async Task UnconfiguredHostDoesNotActivateOrAdvertiseSources()
    {
        var registry = new Registry();
        using var sources = Sources(registry, false);
        await sources.StartAsync(default);
        await sources.ExecuteTask!;
        Assert.Empty(sources.Describe()); Assert.Empty(registry.Activations);
        await sources.StopAsync(default);
    }

    [Fact]
    public void ControllerUsesActualCatalogueWithoutNativeExecution()
    {
        var registry = new Registry(); registry.Add(ProviderType.MongoDBOASIS, true);
        using var sources = Sources(registry);
        using var services = new ServiceCollection().AddSingleton(sources).BuildServiceProvider();
        var controller = new CqlV1Controller(null) { ControllerContext = new ControllerContext
            { HttpContext = new DefaultHttpContext { RequestServices = services } } };
        var response = Assert.IsType<OkObjectResult>(controller.Providers());
        var result = Assert.IsType<OASISResult<object>>(response.Value);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<CqlHostedSourceDescription>>(result.Result);
        Assert.True(rows.Single(row => row.Provider == "MongoDBOASIS").Available);
        Assert.Empty(registry.Activations);
    }

    [Theory]
    [InlineData("NativeKey", true)]
    [InlineData("WrongKey", false)]
    public async Task DiscoveryPublishesOnlyExamplesAdmittedByTheActualAdapter(string metadataKey, bool supported)
    {
        var registry = new Registry(); registry.Add(ProviderType.ActivityPubOASIS, false);
        var json = "{\"ActivityPubOASIS\":[{\"id\":\"sample\",\"label\":\"Public sample\",\"request\":{" +
            "\"provider\":\"ActivityPubOASIS\",\"orderBy\":\"None\",\"take\":3,\"filters\":[{" +
            "\"metadataPath\":[\"" + metadataKey + "\"],\"scalar\":\"String\",\"operator\":\"In\",\"value\":[\"101\",\"102\"]}]}}]}";
        using var sources = new CqlHostedSources(Admission(), registry, NullLogger<CqlHostedSources>.Instance,
            CqlHostedQueryExamples.FromJson(json));
        await sources.StartAsync(default); await registry.Finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await sources.ExecuteTask!;
        var row = sources.Describe().Single(entry => entry.QueryOnly);
        Assert.Equal(supported, row.Available);
        if (supported)
        {
            Assert.Single(row.Examples);
            var wire = System.Text.Json.JsonSerializer.Serialize(new OASISResult<object>(sources.Describe()),
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            using var document = System.Text.Json.JsonDocument.Parse(wire);
            var source = document.RootElement.GetProperty("result").EnumerateArray()
                .Single(value => value.GetProperty("provider").GetString() == "ActivityPubOASIS");
            var body = source.GetProperty("examples")[0].GetProperty("request");
            Assert.Equal("ActivityPubOASIS", body.GetProperty("provider").GetString());
            Assert.Equal("In", body.GetProperty("filters")[0].GetProperty("operator").GetString());
            Assert.Equal(2, body.GetProperty("filters")[0].GetProperty("value").GetArrayLength());
        }
        else Assert.Equal("CQL_DEMO_EXAMPLE_UNSUPPORTED", row.ErrorCode);
        Assert.Single(registry.Activations); // No demo query is executed during discovery.
        await sources.StopAsync(default);
    }
}
