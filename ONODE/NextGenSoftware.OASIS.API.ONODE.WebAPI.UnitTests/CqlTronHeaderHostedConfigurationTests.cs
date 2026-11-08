using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlTronHeaderHostedConfigurationTests
{
    private const string CatalogueSetting = "CQL_TRON_PUBLIC_HEADER_QUERY_SOURCE_JSON";
    private const string KeySetting = "COSMIC_TRON_PUBLIC_READER_KEY";
    private const string Catalogue = """
        {"schema":1,"rpcUrl":"https://api.trongrid.io/walletsolidity/getblock",
         "genesisHash":"00000000000000001ebf88508a03865c71d452e25f4d51194196a1d22b6653dc",
         "consistency":"SolidityNodeSolidified","blockHeights":["1","2","3"],
         "sourceOwner":"374ef405-02b9-4424-b2c6-3e187104a262"}
        """;

    [Fact]
    public void AbsentConfigurationDoesNotRegisterASource()
    {
        var services = new ServiceCollection();
        services.AddReviewedNetworkReadSources(Settings(null, null), sandboxEnabled: true);
        Assert.Empty(services);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PartialConfigurationCannotSelectAnonymousAccess(bool missingKey)
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddReviewedNetworkReadSources(
            Settings(missingKey ? Catalogue : null, missingKey ? null : "controlled-reader"), true));
        Assert.Empty(services);
    }

    [Fact]
    public void ConfiguredReaderRequiresSandboxMode()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddReviewedNetworkReadSources(
            Settings(Catalogue, "controlled-reader")));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("contains space")]
    [InlineData("two,keys")]
    [InlineData("control\r\nheader")]
    public void InvalidReaderKeyFailsBeforeAnyActivation(string key)
    {
        var services = new ServiceCollection();
        services.AddReviewedNetworkReadSources(Settings(Catalogue, key), true);
        using var container = services.BuildServiceProvider();
        Assert.Throws<ArgumentException>(() => container.GetServices<CqlReadSourceProfile>().ToArray());
    }

    [Fact]
    public void ExplicitPairCreatesOnlyAnInactiveReadOnlyProfile()
    {
        var services = new ServiceCollection();
        services.AddReviewedNetworkReadSources(Settings(Catalogue, "controlled-reader"), true);
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        var source = Assert.IsType<TronPublicHeaderMetadataSource>(profile.Storage);
        Assert.False(source.IsProviderActivated);
        Assert.Equal(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, source.QueryProviderType);
        Assert.Same(CqlReadSourceActivationPolicy.None, profile.ActivationPolicy);
    }

    [Fact]
    public void CredentialInsideCatalogueIsRejected()
    {
        var services = new ServiceCollection();
        var embedded = Catalogue.Replace("\"schema\":1", "\"readerKey\":\"forbidden\",\"schema\":1");
        services.AddReviewedNetworkReadSources(Settings(embedded, "controlled-reader"), true);
        using var container = services.BuildServiceProvider();
        Assert.Throws<ArgumentException>(() => container.GetServices<CqlReadSourceProfile>().ToArray());
    }

    private static IConfiguration Settings(string? catalogue, string? key) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> {
            [CatalogueSetting] = catalogue, [KeySetting] = key }).Build();
}
