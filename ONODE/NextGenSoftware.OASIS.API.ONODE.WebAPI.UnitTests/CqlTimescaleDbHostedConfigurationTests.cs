using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlTimescaleDbHostedConfigurationTests
{
    private const string Prefix = "CQL_TIMESCALEDB_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "HOST"] = "service.project.tsdb.cloud.timescale.com",
        [Prefix + "PORT"] = "32625",
        [Prefix + "READER_PASSWORD"] = "controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-1111-4111-8111-111111111111"
    };

    [Theory]
    [InlineData("service.project.tsdb.cloud.timescale.com")]
    [InlineData("service.tsdb.cloud.tigerdata.com")]
    public async Task RegistersInactiveReadProfileForManagedHost(string host)
    {
        var services = new ServiceCollection();
        services.AddTimescaleDbReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = Settings();
        settings[Prefix + "HOST"] = host;
        services.AddTimescaleDbReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.TimescaleDBOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var result = await Assert.IsAssignableFrom<ICosmicReadOnlyQueryActivation>(profile.Storage)
            .ActivateQuerySourceAsync(cancelled.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
    }

    [Theory]
    [InlineData("HOST", "service.tsdb.cloud.tigerdata.com.invalid")]
    [InlineData("HOST", "service.tsdb.cloud.tigerdata.com:5432")]
    [InlineData("PORT", null)]
    [InlineData("PORT", "05432")]
    [InlineData("PORT", "0")]
    [InlineData("PORT", "65536")]
    public void RejectsInvalidManagedServiceSettings(string name, string? value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddTimescaleDbReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
