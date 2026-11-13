using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlGraphStudioHostedConfigurationTests
{
    private const string Setting = "CQL_THE_GRAPH_STUDIO_QUERY_SOURCE_OWNER";

    [Fact]
    public void ExplicitOwnerRegistersOnlyTheReviewedReadProfile()
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { [Setting] = "a5808c3e-7031-47e7-92de-48a659568f1b" }).Build();
        var services = new ServiceCollection();
        services.AddReviewedNetworkReadSources(settings);
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.TheGraphOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
        var source = Assert.IsAssignableFrom<ICosmicQueryStorageProvider>(profile.Storage);
        Assert.Same(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.BoundedIdentityCqlCompiler.Capabilities,
            source.QueryCapabilities);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("A5808C3E-7031-47E7-92DE-48A659568F1B")]
    public void InvalidOwnerFailsBeforeAnySourceIsRegistered(string owner)
    {
        var services = new ServiceCollection();
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { [Setting] = owner }).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddReviewedNetworkReadSources(settings));
        Assert.Empty(services);
    }

    [Fact]
    public void ReadAdmissionRequiresItsOwnTariffAndRejectsWriteTariffs()
    {
        const ProviderType provider = ProviderType.TheGraphOASIS;
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
}
