using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlDaprHostedConfigurationTests
{
    private const string Prefix = "CQL_DAPR_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "STORE"] = "cosmic-teaching",
        [Prefix + "GRPC_ENDPOINT"] = "http://127.0.0.1:54862/",
        [Prefix + "SIDECAR_TOKEN"] = "controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-2222-4222-8222-222222222222"
    };

    [Fact]
    public void RegistersOnlyAnExplicitInactiveDaprProfile()
    {
        var services = new ServiceCollection();
        services.AddDaprReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddDaprReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.DaprOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("STORE", "store/foreign")]
    [InlineData("GRPC_ENDPOINT", "http://localhost:54862/")]
    [InlineData("SIDECAR_TOKEN", null)]
    [InlineData("SIDECAR_TOKEN", "invalid token")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "AAAAAAAA-2222-4222-8222-222222222222")]
    public void RejectsInvalidDaprSettings(string name, string? value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddDaprReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
