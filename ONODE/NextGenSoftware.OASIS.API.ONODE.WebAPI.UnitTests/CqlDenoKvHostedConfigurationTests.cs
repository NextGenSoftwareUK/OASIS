using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlDenoKvHostedConfigurationTests
{
    private const string Prefix = "CQL_DENO_KV_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "DATABASE_ID"] = "11111111-1111-4111-8111-111111111111",
        [Prefix + "DATA_ENDPOINT"] = "https://api.deno.com/v2/teaching",
        [Prefix + "READER_TOKEN"] = "controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-2222-4222-8222-222222222222"
    };

    [Fact]
    public void RegistersOnlyAnExplicitInactiveDenoKvProfile()
    {
        var services = new ServiceCollection();
        services.AddDenoKvReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddDenoKvReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.DenoDeployOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("DATA_ENDPOINT", "http://api.deno.com/v2/teaching")]
    [InlineData("DATA_ENDPOINT", "https://user@api.deno.com/v2/teaching")]
    [InlineData("DATABASE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("DATA_ENDPOINT", "https://127.0.0.1/v2")]
    [InlineData("READER_TOKEN", null)]
    [InlineData("READER_TOKEN", "invalid token")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "AAAAAAAA-2222-4222-8222-222222222222")]
    public void RejectsInvalidDenoKvSettings(string name, string? value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddDenoKvReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
