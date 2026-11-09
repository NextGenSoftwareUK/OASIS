using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS;
using MongoSource = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlGOMapRouteHostedConfigurationTests
{
    private const string Setting = "CQL_GOMAP_ROUTE_QUERY_SOURCE_OWNER";
    private const ProviderType Provider = ProviderType.GOMapOASIS;
    private const string Owner = "0b05ec72-ce47-49f4-8439-d29d978877bc";

    [Fact]
    public void AbsentConfigurationDoesNotRegisterASource()
    {
        var services = new ServiceCollection();
        services.AddReviewedNetworkReadSources(new ConfigurationBuilder().Build());
        Assert.Empty(services);
    }

    [Fact]
    public void ExplicitCanonicalOwnerRegistersOnlyAnInactiveBoundedReadProfile()
    {
        var services = new ServiceCollection();
        services.AddReviewedNetworkReadSources(Settings(Owner));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.IsType<GOMapRouteQuerySource>(profile.Storage);
        Assert.Equal(Provider, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
        var source = Assert.IsAssignableFrom<ICosmicQueryStorageProvider>(profile.Storage);
        Assert.Same(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.BoundedIdentityCqlCompiler.Capabilities,
            source.QueryCapabilities);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("0B05EC72-CE47-49F4-8439-D29D978877BC")]
    [InlineData("https://foreign.example")]
    [InlineData("1")]
    public void InvalidOwnerFailsBeforeRegistration(string owner)
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddReviewedNetworkReadSources(Settings(owner)));
        Assert.Empty(services);
    }

    [Fact]
    public void QueryOnlyAdmissionRequiresItsOwnReadTariffAndRejectsWrites()
    {
        Assert.Equal(Provider, CqlV1Compiler.RequestedProvider(Provider.ToString(), includeCloudReadProfile: true));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.RequestedProvider(Provider.ToString()));
        var ordinary = new[] { ProviderType.MongoDBOASIS };
        var tariffs = new[] { new StorageQueryTariff(ordinary[0], 0, "demo-read-v1"),
            new StorageQueryTariff(Provider, 0, "demo-read-v1") };
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            ordinary, tariffs, null, new[] { Provider });
        Assert.Contains(Provider, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), ordinary, tariffs[..1], null, new[] { Provider }));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), ordinary, tariffs,
            new[] { new StorageAttemptTariff(Provider, "SaveHolon", 0, "demo-read-v1") }, new[] { Provider }));
    }

    [Fact]
    public void ExactKeyPresetsPlanNativeAndMongoQueriesWithoutActivatingTheSource()
    {
        var owner = Guid.Parse(Owner);
        using var source = GOMapRouteQuerySource.ForReviewedTeachingCatalogue(owner);
        var ids = new[] { "west-north", "north-east", "east-west" }
            .Select(height => GOMapRouteQuerySource.LogicalId(owner, height)).ToArray();
        var rows = new List<object>();
        foreach (var count in new[] { 3, 2 })
        {
            var selection = new { orderBy = "None", take = 8, includeMetadata = true,
                filters = new[] { new { field = "Id", scalar = "Guid", @operator = "In", value = ids.Take(count).ToArray() } } };
            rows.Add(new { id = $"native-{count}", label = "Fixed public Berlin routes", request = new {
                provider = Provider.ToString(), selection.orderBy, selection.take, selection.includeMetadata, selection.filters } });
            rows.Add(new { id = $"paired-{count}", label = "Fixed routes and MongoDB", operation = "Federation", request = new {
                scope = "Named", providers = new[] { nameof(ProviderType.MongoDBOASIS), Provider.ToString() },
                source = selection, identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object> {
            [Provider.ToString()] = rows }));
        Assert.Throws<ArgumentException>(() => examples.For(Provider, source.QueryCapabilities));
        var planned = examples.For(Provider, source.QueryCapabilities,
            target => target == ProviderType.MongoDBOASIS ? MongoSource.CqlCapabilities : null,
            (target, actor) => target == Provider
                ? CosmicQueryReadAccess.AuthoritativeRecords(actor, target, ids).Result
                : CosmicQueryReadAccess.OwnerOrPublic(actor).Result);
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));
        Assert.False(source.IsProviderActivated);
    }

    private static IConfiguration Settings(string owner) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { [Setting] = owner }).Build();
}
