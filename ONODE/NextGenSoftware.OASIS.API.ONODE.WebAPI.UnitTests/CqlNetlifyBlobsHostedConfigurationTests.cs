using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlNetlifyBlobsHostedConfigurationTests
{
    private const string Prefix = "CQL_NETLIFY_BLOBS_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "SITE_ID"] = "aaaaaaaa-1111-4111-8111-111111111111",
        [Prefix + "STORE"] = "oasis",
        [Prefix + "OBJECT_ORIGIN"] = "https://objects.example",
        [Prefix + "READER_TOKEN"] = "controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-2222-4222-8222-222222222222"
    };

    [Fact]
    public void RegistersOnlyAnExplicitInactiveNetlifyProfile()
    {
        var services = new ServiceCollection();
        services.AddNetlifyBlobsReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddNetlifyBlobsReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.NetlifyBlobsOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("SITE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("STORE", "oasis/foreign")]
    [InlineData("OBJECT_ORIGIN", "https://objects.example/foreign")]
    [InlineData("OBJECT_ORIGIN", "https://127.0.0.1")]
    [InlineData("OBJECT_ORIGIN", "https://objects.example:443")]
    [InlineData("OBJECT_ORIGIN", "http://objects.example")]
    [InlineData("READER_TOKEN", null)]
    [InlineData("PROBE_ID", "AAAAAAAA-2222-4222-8222-222222222222")]
    public void RejectsInvalidNetlifySettings(string name, string? value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddNetlifyBlobsReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
