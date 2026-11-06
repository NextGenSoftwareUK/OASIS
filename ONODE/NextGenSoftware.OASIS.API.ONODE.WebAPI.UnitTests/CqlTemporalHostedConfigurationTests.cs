using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.TemporalOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlTemporalHostedConfigurationTests
{
    private const string Catalogue = "CQL_TEMPORAL_QUERY_SOURCE_JSON";
    private const string Key = "CQL_TEMPORAL_QUERY_API_KEY";
    private const string Source = """
        {"owner":"a5808c3e-7031-47e7-92de-48a659568f1b","namespace":"oasis-cql.exampleaccount","runs":[{"workflowId":"oasis-teaching-first","runId":"30000000-0000-4000-8000-000000000001"},{"workflowId":"oasis-teaching-second","runId":"30000000-0000-4000-8000-000000000002"}],"probeRunId":"30000000-0000-4000-8000-000000000001"}
        """;

    [Fact]
    public void OnlyAnExplicitCatalogueAndProtectedKeyRegisterALazyReadProfile()
    {
        var services = new ServiceCollection();
        services.AddTemporalReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        Assert.Throws<InvalidOperationException>(() => services.AddTemporalReadSource(Settings(Source, null)));
        Assert.Empty(services);
        services.AddTemporalReadSource(Settings(Source, "fixture-temporal-key"));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.IsType<TemporalTeachingQuerySource>(profile.Storage);
        Assert.False(profile.Storage.IsProviderActivated);
        Assert.Equal(Guid.Parse("6bef46ce-5f1e-8506-93fe-40f019780879"),
            TemporalTeachingQuerySource.LogicalId(Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b"),
                "oasis-cql.exampleaccount", new TemporalTeachingRun("oasis-teaching-first",
                    Guid.Parse("30000000-0000-4000-8000-000000000001"))));
    }

    [Theory]
    [InlineData("oasis-cql.exampleaccount", "https://foreign.example")]
    [InlineData("oasis-teaching-first", "unreviewed-private-workflow")]
    [InlineData("oasis-teaching-first", "Oasis-teaching-first")]
    [InlineData("30000000-0000-4000-8000-000000000002", "30000000-0000-4000-8000-000000000001")]
    [InlineData("\"probeRunId\"", "\"origin\":\"https://foreign.example\",\"probeRunId\"")]
    public void InvalidOrAmbiguousCatalogueFailsClosed(string before, string after)
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddTemporalReadSource(Settings(Source.Replace(before, after), "fixture-temporal-key")));
        Assert.Empty(services);
    }

    [Fact]
    public void ExactRunReaderRequiresAnExplicitReadOnlyTariff()
    {
        const ProviderType provider = ProviderType.TemporalOASIS;
        Assert.Equal(provider, CqlV1Compiler.RequestedProvider(provider.ToString(), includeCloudReadProfile: true));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.RequestedProvider(provider.ToString()));
        var ordinary = new[] { ProviderType.MongoDBOASIS };
        var tariffs = new[] { new StorageQueryTariff(ordinary[0], 0, "demo-read-v1"),
            new StorageQueryTariff(provider, 0, "demo-read-v1") };
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            ordinary, tariffs, null, new[] { provider });
        Assert.Contains(provider, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), ordinary, tariffs[..1], null, new[] { provider }));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), ordinary, tariffs,
            new[] { new StorageAttemptTariff(provider, "SaveHolon", 0, "demo-read-v1") }, new[] { provider }));
    }

    private static IConfiguration Settings(string source, string? key) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [Catalogue] = source,
            [Key] = key
        }).Build();
}
