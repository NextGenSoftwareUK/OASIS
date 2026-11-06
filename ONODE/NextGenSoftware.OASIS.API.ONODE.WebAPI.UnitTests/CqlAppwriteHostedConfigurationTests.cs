using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlAppwriteHostedConfigurationTests
{
    private const string Prefix = "CQL_APPWRITE_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "PROJECT_ID"] = "teaching",
        [Prefix + "DATABASE_ID"] = "records",
        [Prefix + "ENDPOINT"] = "https://appwrite.example/v1",
        [Prefix + "READER_TOKEN"] = "controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-2222-4222-8222-222222222222"
    };

    [Fact]
    public void RegistersOnlyAnExplicitInactiveAppwriteProfile()
    {
        var services = new ServiceCollection();
        services.AddAppwriteReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddAppwriteReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.AppwriteOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("ENDPOINT", "http://appwrite.example/v1")]
    [InlineData("ENDPOINT", "https://user@appwrite.example/v1")]
    [InlineData("DATABASE_ID", "../foreign")]
    [InlineData("ENDPOINT", "https://127.0.0.1/v1")]
    [InlineData("READER_TOKEN", null)]
    [InlineData("READER_TOKEN", "invalid token")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "AAAAAAAA-2222-4222-8222-222222222222")]
    public void RejectsInvalidAppwriteSettings(string name, string? value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddAppwriteReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
