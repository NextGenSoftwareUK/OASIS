using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlReviewedNetworkHostedConfigurationTests
{
    private const string Owner = "a5808c3e-7031-47e7-92de-48a659568f1b";

    [Fact]
    public void NoNetworkSourceRegistersWithoutExplicitConfiguration()
    {
        var services = new ServiceCollection();
        services.AddReviewedNetworkReadSources(new ConfigurationBuilder().Build());
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("CQL_DISCORD_PUBLIC_GUILD_QUERY_SOURCE_OWNER", ProviderType.DiscordOASIS)]
    [InlineData("CQL_LIVEPEER_ORCHESTRATOR_QUERY_SOURCE_OWNER", ProviderType.LivepeerOASIS)]
    [InlineData("CQL_PINATA_PUBLIC_BLOCK_QUERY_SOURCE_OWNER", ProviderType.PinataOASIS)]
    [InlineData("CQL_PUSH_PUBLIC_CHANNEL_QUERY_SOURCE_OWNER", ProviderType.PushProtocolOASIS)]
    [InlineData("CQL_LAYERZERO_V1_ENDPOINT_QUERY_SOURCE_OWNER", ProviderType.LayerZeroOASIS)]
    [InlineData("CQL_WORLD_ID_PROTOCOL_QUERY_SOURCE_OWNER", ProviderType.WorldIDOASIS)]
    [InlineData("CQL_RECLAIM_VERIFIER_QUERY_SOURCE_OWNER", ProviderType.ReclaimProtocolOASIS)]
    [InlineData("CQL_DECENTRALAND_SCENE_QUERY_SOURCE_OWNER", ProviderType.DecentralandOASIS)]
    public void ConfiguringOneNetworkSourceRegistersItsOwnReadProfile(string setting, ProviderType provider)
    {
        var services = new ServiceCollection();
        services.AddReviewedNetworkReadSources(Settings(setting, Owner));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(provider, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
        var query = Assert.IsAssignableFrom<ICosmicQueryStorageProvider>(profile.Storage);
        Assert.Same(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.BoundedIdentityCqlCompiler.Capabilities, query.QueryCapabilities);
    }

    [Theory]
    [InlineData("CQL_DISCORD_PUBLIC_GUILD_QUERY_SOURCE_OWNER", "https://foreign.example")]
    [InlineData("CQL_LIVEPEER_ORCHESTRATOR_QUERY_SOURCE_OWNER", "00000000-0000-0000-0000-000000000000")]
    [InlineData("CQL_PINATA_PUBLIC_BLOCK_QUERY_SOURCE_OWNER", "QmP4eZj61tDxCmtb6aqSwQsrxRQAN2gwxTEXkTed9TujHh")]
    [InlineData("CQL_PUSH_PUBLIC_CHANNEL_QUERY_SOURCE_OWNER", "0xb88460bb2696cab9d66013a05dff29a28330689d")]
    [InlineData("CQL_LAYERZERO_V1_ENDPOINT_QUERY_SOURCE_OWNER", "https://foreign.example")]
    [InlineData("CQL_WORLD_ID_PROTOCOL_QUERY_SOURCE_OWNER", "registry")]
    [InlineData("CQL_RECLAIM_VERIFIER_QUERY_SOURCE_OWNER", "00000000-0000-0000-0000-000000000000")]
    [InlineData("CQL_DECENTRALAND_SCENE_QUERY_SOURCE_OWNER", "0,0")]
    public void InvalidTeachingOwnerFailsClosed(string setting, string owner)
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddReviewedNetworkReadSources(Settings(setting, owner)));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData(ProviderType.DiscordOASIS)]
    [InlineData(ProviderType.LivepeerOASIS)]
    [InlineData(ProviderType.PinataOASIS)]
    [InlineData(ProviderType.PushProtocolOASIS)]
    [InlineData(ProviderType.LayerZeroOASIS)]
    [InlineData(ProviderType.WorldIDOASIS)]
    [InlineData(ProviderType.ReclaimProtocolOASIS)]
    [InlineData(ProviderType.DecentralandOASIS)]
    public void NetworkSourceRequiresIndependentReadOnlyTariff(ProviderType provider)
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
