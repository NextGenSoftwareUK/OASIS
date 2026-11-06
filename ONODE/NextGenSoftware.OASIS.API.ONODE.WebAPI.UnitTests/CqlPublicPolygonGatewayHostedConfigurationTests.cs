using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlPublicPolygonGatewayHostedConfigurationTests
{
    private const string Owner = "a5808c3e-7031-47e7-92de-48a659568f1b";

    [Fact]
    public void NoGatewayRegistersWithoutExplicitConfiguration()
    {
        var services = new ServiceCollection();
        services.AddPublicPolygonGatewayReadSources(new ConfigurationBuilder().Build());
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("CQL_TENDERLY_POLYGON_QUERY_SOURCE_OWNER", ProviderType.TenderlyOASIS, "TenderlyPolygonAccountAddress")]
    [InlineData("CQL_QUICKNODE_POLYGON_QUERY_SOURCE_OWNER", ProviderType.QuickNodeOASIS, "QuickNodePolygonAccountAddress")]
    public void ConfiguringOneGatewayRegistersItsOwnReadProfile(string setting, ProviderType provider, string metadataKey)
    {
        var services = new ServiceCollection();
        services.AddPublicPolygonGatewayReadSources(Settings(setting, Owner));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(provider, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
        var query = Assert.IsAssignableFrom<ICosmicQueryStorageProvider>(profile.Storage);
        Assert.Equal(metadataKey, query.QueryCapabilities.StringKeySetLookup.MetadataKey);
    }

    [Theory]
    [InlineData("CQL_TENDERLY_POLYGON_QUERY_SOURCE_OWNER", "https://foreign.example")]
    [InlineData("CQL_QUICKNODE_POLYGON_QUERY_SOURCE_OWNER", "00000000-0000-0000-0000-000000000000")]
    public void InvalidTeachingOwnerFailsClosed(string setting, string owner)
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddPublicPolygonGatewayReadSources(Settings(setting, owner)));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData(ProviderType.TenderlyOASIS)]
    [InlineData(ProviderType.QuickNodeOASIS)]
    public void GatewayRequiresIndependentReadOnlyTariff(ProviderType provider)
    {
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

    private static IConfiguration Settings(string name, string value) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [name] = value }).Build();
}
