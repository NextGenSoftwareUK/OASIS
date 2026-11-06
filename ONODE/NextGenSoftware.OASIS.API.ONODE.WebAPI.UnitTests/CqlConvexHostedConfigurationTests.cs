using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlConvexHostedConfigurationTests
{
    private const string Prefix = "CQL_CONVEX_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "SITE"] = "https://convex.example/",
        [Prefix + "READER_TOKEN"] = "synthetic-convex-teaching-token-123",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-2222-4222-8222-222222222222"
    };

    [Fact]
    public void RegistersOnlyAnExplicitInactiveConvexProfile()
    {
        var services = new ServiceCollection();
        services.AddConvexReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddConvexReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.ConvexOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("SITE", "http://convex.example/")]
    [InlineData("SITE", "https://user@convex.example/")]
    [InlineData("SITE", "https://convex.example/foreign")]
    [InlineData("SITE", "https://127.0.0.1/")]
    [InlineData("READER_TOKEN", null)]
    [InlineData("READER_TOKEN", "invalid token")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "AAAAAAAA-2222-4222-8222-222222222222")]
    public void RejectsInvalidConvexSettings(string name, string? value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddConvexReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
