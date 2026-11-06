using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlFastlyHostedConfigurationTests
{
    private const string Prefix = "CQL_FASTLY_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "STORE_ID"] = "teachingStore",
        [Prefix + "READER_TOKEN"] = "controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-2222-4222-8222-222222222222"
    };

    [Fact]
    public void RegistersOnlyAnExplicitInactiveFastlyProfile()
    {
        var services = new ServiceCollection();
        services.AddFastlyReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddFastlyReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.FastlyOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("STORE_ID", "store/foreign")]
    [InlineData("STORE_ID", "store?key=other")]
    [InlineData("READER_TOKEN", null)]
    [InlineData("READER_TOKEN", "invalid token")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "AAAAAAAA-2222-4222-8222-222222222222")]
    public void RejectsInvalidFastlySettings(string name, string? value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddFastlyReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
