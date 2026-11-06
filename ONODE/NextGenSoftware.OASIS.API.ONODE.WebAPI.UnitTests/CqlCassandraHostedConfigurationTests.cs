using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlCassandraHostedConfigurationTests
{
    private const string Prefix = "CQL_CASSANDRA_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "BUNDLE_SHA256"] = new string('a', 64),
        [Prefix + "READER_TOKEN"] = "AstraCS:controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-1111-4111-8111-111111111111"
    };

    [Fact]
    public async Task RegistersInactiveSourceAndHonoursCancellationBeforeBundleAccess()
    {
        var services = new ServiceCollection();
        services.AddCassandraReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddCassandraReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.CassandraOASIS, profile.Storage.ProviderType.Value);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var result = await Assert.IsAssignableFrom<ICosmicReadOnlyQueryActivation>(profile.Storage)
            .ActivateQuerySourceAsync(cancelled.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("BUNDLE_SHA256", "not-a-hash")]
    [InlineData("READER_TOKEN", "wrong-token")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "AAAAAAAA-1111-4111-8111-111111111111")]
    public void RejectsInvalidSettings(string key, string value)
    {
        var settings = Settings();
        settings[Prefix + key] = value;
        var services = new ServiceCollection();
        var error = Record.Exception(() =>
            services.AddCassandraReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.True(error is InvalidOperationException or ArgumentException);
        Assert.Empty(services);
    }

    [Fact]
    public async Task RejectsBundleDigestMismatchBeforeUsingDriver()
    {
        var path = Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllText(path, "controlled");
            using var source = new NextGenSoftware.OASIS.API.Providers.CassandraOASIS.CassandraQuerySource(
                path, new string('a', 64), "AstraCS:controlled", Guid.NewGuid());
            var result = await source.ActivateQuerySourceAsync(default);
            Assert.Equal("CQL_QUERY_SOURCE_UNAVAILABLE", result.ErrorCode);
            Assert.IsType<InvalidDataException>(result.Exception);
            Assert.False(source.IsProviderActivated);
        }
        finally { System.IO.File.Delete(path); }
    }
}
