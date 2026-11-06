using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlCockroachDbHostedConfigurationTests
{
    private const string Prefix = "CQL_COCKROACHDB_QUERY_";
    private const string Host = "oasis-demo.aws-us-east-1.cockroachlabs.cloud";
    private const string Probe = "aaaaaaaa-1111-4111-8111-111111111111";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "HOST"] = Host,
        [Prefix + "READER_PASSWORD"] = "controlled;Host=untrusted.invalid",
        [Prefix + "PROBE_ID"] = Probe
    };

    [Fact]
    public async Task RegistersAnInactiveReadProfileOnlyWhenConfigured()
    {
        var services = new ServiceCollection();
        services.AddCockroachDbReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddCockroachDbReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.CockroachDBOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var result = await Assert.IsAssignableFrom<ICosmicReadOnlyQueryActivation>(profile.Storage)
            .ActivateQuerySourceAsync(cancelled.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("HOST", "localhost")]
    [InlineData("HOST", "oasis-demo.cockroachlabs.cloud.attacker.invalid")]
    [InlineData("HOST", "user@oasis-demo.cockroachlabs.cloud")]
    [InlineData("HOST", "oasis-demo..cockroachlabs.cloud")]
    [InlineData("HOST", "-oasis-demo.cockroachlabs.cloud")]
    [InlineData("READER_PASSWORD", "")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "AAAAAAAA-1111-4111-8111-111111111111")]
    public void RejectsInvalidOrNonCanonicalSettings(string name, string value)
    {
        var values = Settings();
        values[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddCockroachDbReadSource(new ConfigurationBuilder().AddInMemoryCollection(values).Build()));
        Assert.Empty(services);
    }
}
