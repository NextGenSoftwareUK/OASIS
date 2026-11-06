using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlLitestreamHostedConfigurationTests
{
    private const string Prefix = "CQL_LITESTREAM_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "RESTORE_SHA256"] = new string('a', 64),
        [Prefix + "PROBE_ID"] = "aaaaaaaa-1111-4111-8111-111111111111"
    };

    [Fact]
    public async Task RegistersInactiveRestoreSourceAndHonoursCancellation()
    {
        var services = new ServiceCollection();
        services.AddLitestreamReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddLitestreamReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.LitestreamOASIS, profile.Storage.ProviderType.Value);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var result = await Assert.IsAssignableFrom<ICosmicReadOnlyQueryActivation>(profile.Storage)
            .ActivateQuerySourceAsync(cancelled.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("RESTORE_SHA256", null)]
    [InlineData("RESTORE_SHA256", "not-a-digest")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "AAAAAAAA-1111-4111-8111-111111111111")]
    public void RejectsInvalidRestoreSettings(string key, string? value)
    {
        var settings = Settings();
        settings[Prefix + key] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddLitestreamReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
