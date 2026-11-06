using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlDruidHostedConfigurationTests
{
    private const string Prefix = "CQL_DRUID_QUERY_";

    [Fact]
    public void ExplicitSettingsRegisterAnInactiveReadProfile()
    {
        var services = new ServiceCollection();
        services.AddDruidReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddDruidReadSource(Configuration(Valid()));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.DruidOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("ORIGIN", "http://druid.example.com")]
    [InlineData("ORIGIN", "https://127.0.0.1")]
    [InlineData("ORIGIN", "https://foreign@druid.example.com")]
    [InlineData("ORIGIN", "https://druid.example.com/druid/v2/sql")]
    [InlineData("ORIGIN", "https://druid.example.com:8888")]
    [InlineData("READER_USERNAME", "foreign:reader")]
    [InlineData("READER_PASSWORD", "")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    public void IncompleteOrUnsafeSettingsFailClosed(string field, string value)
    {
        var services = new ServiceCollection();
        var settings = Valid();
        settings[Prefix + field] = value;
        Assert.Throws<InvalidOperationException>(() => services.AddDruidReadSource(Configuration(settings)));
        Assert.Empty(services);
    }

    [Fact]
    public void DruidRequiresReadOnlyPermissionAndExplicitTariff()
    {
        const ProviderType provider = ProviderType.DruidOASIS;
        Assert.Equal(provider, CqlV1Compiler.RequestedProvider("DruidOASIS", includeCloudReadProfile: true));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.RequestedProvider("DruidOASIS"));
        var ordinary = new[] { ProviderType.MongoDBOASIS };
        var tariffs = new[] { new StorageQueryTariff(ordinary[0], 0, "demo-read-v1"),
            new StorageQueryTariff(provider, 0, "demo-read-v1") };
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            ordinary, tariffs, null, new[] { provider });
        Assert.Contains(provider, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), ordinary, tariffs[..1], null, new[] { provider }));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), ordinary, tariffs,
            new[] { new StorageAttemptTariff(provider, "SaveHolon", 0, "demo-read-v1") }, new[] { provider }));
    }

    private static IConfiguration Configuration(Dictionary<string, string?> settings) =>
        new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

    private static Dictionary<string, string?> Valid() => new()
    {
        [Prefix + "ORIGIN"] = "https://druid.example.com",
        [Prefix + "READER_USERNAME"] = "cql_reader",
        [Prefix + "READER_PASSWORD"] = "fixture-password",
        [Prefix + "PROBE_ID"] = "10000000-0000-4000-8000-000000000001"
    };
}
