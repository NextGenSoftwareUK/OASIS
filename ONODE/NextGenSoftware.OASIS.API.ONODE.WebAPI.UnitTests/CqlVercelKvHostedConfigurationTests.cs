using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlVercelKvHostedConfigurationTests
{
    private const string Prefix = "CQL_VERCEL_KV_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "ENDPOINT"] = "https://teaching.upstash.io/",
        [Prefix + "READER_TOKEN"] = "controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-2222-4222-8222-222222222222"
    };

    [Fact]
    public void RegistersOnlyAnExplicitInactiveVercelKvProfile()
    {
        var services = new ServiceCollection();
        services.AddVercelKvReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddVercelKvReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.VercelKVOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("ENDPOINT", "https://teaching.upstash.io/foreign")]
    [InlineData("ENDPOINT", "https://teaching.upstash.io.example/")]
    [InlineData("READER_TOKEN", null)]
    [InlineData("READER_TOKEN", "invalid token")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "AAAAAAAA-2222-4222-8222-222222222222")]
    public void RejectsInvalidVercelKvSettings(string name, string? value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddVercelKvReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
