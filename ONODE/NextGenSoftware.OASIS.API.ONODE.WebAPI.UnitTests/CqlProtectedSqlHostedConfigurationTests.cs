using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlProtectedSqlHostedConfigurationTests
{
    private const string Prefix = "CQL_DRUID_QUERY_";

    [Theory]
    [InlineData("DRUID", ProviderType.DruidOASIS)]
    [InlineData("PINOT", ProviderType.PinotOASIS)]
    public void ExplicitSettingsRegisterAnInactiveReadProfile(string product, ProviderType provider)
    {
        var services = new ServiceCollection();
        AddSource(product, services, new ConfigurationBuilder().Build());
        Assert.Empty(services);
        AddSource(product, services, Configuration(Valid(product)));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(provider, profile.Storage.ProviderType.Value);
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

    [Theory]
    [InlineData(ProviderType.DruidOASIS)]
    [InlineData(ProviderType.PinotOASIS)]
    public void ProtectedSqlProvidersRequireReadOnlyPermissionAndExplicitTariff(ProviderType provider)
    {
        Assert.Equal(provider, CqlV1Compiler.RequestedProvider(provider.ToString(), includeCloudReadProfile: true));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.RequestedProvider(provider.ToString()));
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

    [Fact]
    public void PinotRejectsAMissingProtectedReaderPassword()
    {
        var services = new ServiceCollection();
        var settings = Valid("PINOT");
        settings.Remove("CQL_PINOT_QUERY_READER_PASSWORD");
        Assert.Throws<InvalidOperationException>(() => services.AddPinotReadSource(Configuration(settings)));
        Assert.Empty(services);
    }

    private static void AddSource(string product, IServiceCollection services, IConfiguration configuration)
    {
        if (product == "DRUID") services.AddDruidReadSource(configuration);
        else services.AddPinotReadSource(configuration);
    }

    private static Dictionary<string, string?> Valid(string product = "DRUID")
    {
        var prefix = "CQL_" + product + "_QUERY_";
        return new()
        {
            [prefix + "ORIGIN"] = "https://query.example.com",
            [prefix + "READER_USERNAME"] = "cql_reader",
            [prefix + "READER_PASSWORD"] = "fixture-password",
            [prefix + "PROBE_ID"] = "10000000-0000-4000-8000-000000000001"
        };
    }
}
