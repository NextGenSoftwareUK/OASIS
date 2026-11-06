using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlCloudflareKvHostedConfigurationTests
{
    private const string Prefix = "CQL_CLOUDFLARE_KV_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "ACCOUNT_ID"] = new string('a', 32),
        [Prefix + "NAMESPACE_ID"] = new string('b', 32),
        [Prefix + "READER_TOKEN"] = "controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-2222-4222-8222-222222222222"
    };

    [Fact]
    public void RegistersOnlyAnExplicitInactiveCloudflareKvProfile()
    {
        var services = new ServiceCollection();
        services.AddCloudflareKvReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddCloudflareKvReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.CloudflareOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("ACCOUNT_ID", "../foreign")]
    [InlineData("NAMESPACE_ID", "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbB")]
    [InlineData("READER_TOKEN", null)]
    [InlineData("READER_TOKEN", "invalid token")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "AAAAAAAA-2222-4222-8222-222222222222")]
    public void RejectsInvalidCloudflareKvSettings(string name, string? value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddCloudflareKvReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
