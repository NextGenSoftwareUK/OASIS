using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
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
        Assert.Equal(ProviderType.TRONOASIS, source.QueryProviderType);
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

    [Fact]
    public void QueryAdmissionRequiresIndependentReadTariffAndRejectsWrites()
    {
        const ProviderType provider = ProviderType.TRONOASIS;
        Assert.Equal(provider, CqlV1Compiler.RequestedProvider(provider.ToString(), includeCloudReadProfile: true));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.RequestedProvider(provider.ToString()));
        var ordinary = new[] { ProviderType.MongoDBOASIS };
        var tariffs = new[] { new StorageQueryTariff(ordinary[0], 0, "demo-read-v1"),
            new StorageQueryTariff(provider, 0, "demo-read-v1") };
        var factory = new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(),
            ordinary, tariffs, null, new[] { provider });
        Assert.Contains(provider, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(),
            ordinary, tariffs[..1], null, new[] { provider }));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(),
            ordinary, tariffs, new[] { new StorageAttemptTariff(provider, "SaveHolon", 0, "demo-read-v1") }, new[] { provider }));
    }

    [Fact]
    public void ExactHeightPresetsPlanThreeTwoAndMongoPairsWithoutNativeRequests()
    {
        using var source = TronPublicHeaderMetadataSource.FromAuthenticatedJson(Catalogue, "controlled-reader");
        var rows = new List<object>();
        var owner = Guid.Parse("374ef405-02b9-4424-b2c6-3e187104a262");
        var heights = new[] { "1", "2", "3" };
        var ids = heights.Select(height => NativeRecordHolonMapper.StableHolonId(owner, ProviderType.TRONOASIS,
            "tron-public-solidified:" + TronPublicHeaderMetadataSource.PublicGenesis, height)).ToArray();
        foreach (var count in new[] { 3, 2 })
        {
            // Exercise the captured metadata-path expression used by the shared HTTP
            // fixture, before any native activation or provider request is possible.
            var metadataField = TronPublicHeaderMetadataSource.AddressMetadata;
            var metadataKeys = heights.Take(count).ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TRONOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(owner, ProviderType.TRONOASIS, ids).Result).Result
                .Where(row => metadataKeys.Contains(CosmicQueryMetadata.Value<string>(row, metadataField)));
            Assert.False(query.IsError, query.ErrorCode);
            var plan = CosmicQueryPlanner.Plan(query.Result, ProviderType.TRONOASIS,
                new[] { new CosmicQueryTarget(ProviderType.TRONOASIS, source.QueryCapabilities) });
            Assert.False(plan.IsError, plan.ErrorCode);
            var selection = new { orderBy = "None", take = 8, filters = new[] {
                new { metadataPath = new[] { TronPublicHeaderMetadataSource.AddressMetadata }, scalar = "String",
                    @operator = "In", value = new[] { "1", "2", "3" }.Take(count).ToArray() } } };
            rows.Add(new { id = $"native-{count}", label = "Solidified TRON headers", request = new {
                provider = "TRONOASIS", selection.orderBy, selection.take, selection.filters } });
            rows.Add(new { id = $"paired-{count}", label = "Solidified TRON headers and MongoDB", operation = "Federation", request = new {
                scope = "Named", providers = new[] { "MongoDBOASIS", "TRONOASIS" }, source = selection,
                identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 } });
        }
        var examples = CqlHostedQueryExamples.FromJson(System.Text.Json.JsonSerializer.Serialize(
            new Dictionary<string, object> { ["TRONOASIS"] = rows }));
        var planned = examples.For(ProviderType.TRONOASIS,
            source.QueryCapabilities, _ => NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS.CqlCapabilities);
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));
        Assert.False(source.IsProviderActivated);
    }

    private static IConfiguration Settings(string? catalogue, string? key) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> {
            [CatalogueSetting] = catalogue, [KeySetting] = key }).Build();
}
