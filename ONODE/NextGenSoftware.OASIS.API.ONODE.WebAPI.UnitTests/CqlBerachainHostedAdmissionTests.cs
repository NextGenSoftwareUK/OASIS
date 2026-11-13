using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.BerachainOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlBerachainHostedAdmissionTests
{
    [Fact]
    public void ReadCatalogueRequiresIndependentBerachainPermissionAndTariff()
    {
        var providers = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS,
            ProviderType.AzureCosmosDBOASIS, ProviderType.ActivityPubOASIS, ProviderType.IPFSOASIS,
            ProviderType.SOLIDOASIS, ProviderType.AWSS3OASIS, ProviderType.OracleDBOASIS, ProviderType.SolanaOASIS, ProviderType.GoogleCloudOASIS, ProviderType.EthereumOASIS, ProviderType.PolygonOASIS, ProviderType.BerachainOASIS };
        var tariffs = providers.Select(provider => new StorageQueryTariff(provider, 0, "demo-read-v1")).ToArray();
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            providers.Take(4), tariffs, null, providers.Skip(4));
        Assert.Equal(providers, factory.QueryProviders);
        Assert.Contains(ProviderType.BerachainOASIS, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs,
            new[] { new StorageAttemptTariff(ProviderType.BerachainOASIS,"SaveHolon",0,"demo-read-v1") }, providers.Skip(4)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs, null, providers.Skip(4).Take(9)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs.Take(13), null, providers.Skip(4)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers, tariffs.Append(tariffs[0])));
    }

    [Fact]
    public async Task PublicAccountPresetsPlanNativeThreeTwoAndMongoPairsWithoutWallets()
    {
        var keys = Enumerable.Range(1, 3).Select(value => "0x" + value.ToString("x40")).ToArray();
        using var source = BerachainPublicAccountMetadataSource.FromJson(JsonSerializer.Serialize(new {
            schema=1,rpcUrl="https://rpc.example.invalid/",chainId="0x138de",addresses=keys,sourceOwner=Guid.NewGuid()}));
        var rows = new List<object>();
        foreach (var count in new[] { 3, 2 })
        {
            var selection = new { orderBy = "None", take = 8, filters = new[] {
                new { metadataPath = new[] { BerachainPublicAccountMetadataSource.AddressMetadata },
                    scalar = "String", @operator = "In", value = keys.Take(count).ToArray() } } };
            rows.Add(new { id = "native-" + count, label = "Berachain public balances", request = new {
                provider = "BerachainOASIS", selection.orderBy, selection.take, selection.filters } });
            rows.Add(new { id = "paired-" + count, label = "Berachain public balances + Mongo", operation = "Federation", request = new {
                scope = "Named", providers = new[] { "MongoDBOASIS", "BerachainOASIS" }, source = selection,
                identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object> {
            ["BerachainOASIS"] = rows }));
        var planned = examples.For(ProviderType.BerachainOASIS, source.QueryCapabilities,
            _ => CosmicQueryCapabilities.ForStringMetadataKeyLookup(BerachainPublicAccountMetadataSource.AddressMetadata, 44, 8));
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));

        // Discovery uses the same capability snapshot after explicit read activation.
        var proxy = System.Reflection.DispatchProxy.Create<CqlHostedSourcesTests.QueryStorage, CqlHostedSourcesTests.Adapter>();
        var state = (CqlHostedSourcesTests.Adapter)(object)proxy;
        state.Type = ProviderType.BerachainOASIS; state.Active = true; state.Capabilities = source.QueryCapabilities;
        var registry = new ReadyRegistry(proxy);
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.MongoDBOASIS }, new[] { new StorageQueryTariff(ProviderType.MongoDBOASIS, 0, "demo-v1"),
                new StorageQueryTariff(ProviderType.BerachainOASIS, 0, "demo-v1") }, null, new[] { ProviderType.BerachainOASIS });
        using var hosted = new CqlHostedSources(admission, registry, NullLogger<CqlHostedSources>.Instance, examples);
        await hosted.StartAsync(default); await hosted.ExecuteTask!;
        var description = Assert.Single(hosted.Describe(true).Where(row => row.Provider == "BerachainOASIS"));
        Assert.True(description.Available);
        Assert.Equal(4, description.Examples.Count);
        // Mongo is intentionally absent: source rows stay available; its pairs are explicitly unavailable.
        Assert.All(description.Examples.Where(row => row.Operation == "Rows"), row => Assert.True(row.Available));
        Assert.All(description.Examples.Where(row => row.Operation == "Federation"), row => Assert.False(row.Available));
    }

    [Theory]
    [InlineData(ProviderType.RoninOASIS)]
    [InlineData(ProviderType.ImmutableZkEVMOASIS)]
    [InlineData(ProviderType.FlowOASIS)]
    [InlineData(ProviderType.EASOASIS)]
    [InlineData(ProviderType.SignProtocolOASIS)]
    public void EvmNamedReadRequiresItsOwnTariffAndRejectsWriteAdmission(ProviderType provider)
    {
        var queryOnly = new[] { provider };
        var tariffs = new[] { new StorageQueryTariff(provider, 0, "demo-read-v1") };
        var bindings = Array.Empty<NativeStorageAdmissionBinding>();
        var admission = new PricedStorageAdmissionFactory(null, bindings, Array.Empty<ProviderType>(),
            tariffs, null, queryOnly);

        Assert.Contains(provider, admission.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null, bindings,
            Array.Empty<ProviderType>(), Array.Empty<StorageQueryTariff>(), null, queryOnly));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null, bindings,
            Array.Empty<ProviderType>(), tariffs,
            new[] { new StorageAttemptTariff(provider, "SaveHolon", 0, "demo-read-v1") }, queryOnly));
    }

    [Theory]
    [InlineData(ProviderType.RoninOASIS, "https://rpc.example.invalid/rpc/", "0x7e4")]
    [InlineData(ProviderType.ImmutableZkEVMOASIS, "https://rpc.example.invalid/", "0x343b")]
    public void EvmPublicPresetCompilesAsANamedRead(ProviderType provider, string rpcUrl, string chainId)
    {
        const string address = "0x0000000000000000000000000000000000000001";
        var json = JsonSerializer.Serialize(new { schema = 1, rpcUrl, chainId,
            addresses = new[] { address }, sourceOwner = Guid.NewGuid() });
        using Providers.Web3CoreOASIS.EvmPublicAccountMetadataSource source = provider == ProviderType.RoninOASIS
            ? Providers.RoninOASIS.RoninPublicAccountMetadataSource.FromJson(json)
            : Providers.ImmutableZkEVMOASIS.ImmutableZkEVMPublicAccountMetadataSource.FromJson(json);
        var rows = new[] { new { id = "native-one", label = "Public balance", request = new
        {
            provider = provider.ToString(), orderBy = "None", take = 8, filters = new[] { new
            {
                metadataPath = new[] { source.QueryCapabilities.StringKeySetLookup.MetadataKey },
                scalar = "String", @operator = "In", value = new[] { address }
            } }
        } } };
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(
            new Dictionary<string, object> { [provider.ToString()] = rows }));

        var preset = Assert.Single(examples.For(provider, source.QueryCapabilities, _ => source.QueryCapabilities));
        Assert.True(preset.Available, preset.ErrorCode);
    }

    [Theory]
    [InlineData("CQL_RONIN_PUBLIC_SOURCE_JSON")]
    [InlineData("CQL_IMMUTABLE_ZKEVM_PUBLIC_SOURCE_JSON")]
    [InlineData("CQL_FLOW_PUBLIC_SOURCE_JSON")]
    [InlineData("CQL_EAS_PUBLIC_SOURCE_JSON")]
    [InlineData("CQL_SIGN_PUBLIC_SOURCE_JSON")]
    public void EvmRegistrationRequiresSandboxMode(string setting)
    {
        var settings = new Microsoft.Extensions.Configuration.ConfigurationManager();
        settings[setting] = "{}";
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddReviewedNetworkReadSources(settings));
        Assert.DoesNotContain(services, entry => entry.ServiceType == typeof(CqlReadSourceProfile));
        services.AddReviewedNetworkReadSources(settings, sandboxEnabled: true);
        Assert.Contains(services, entry => entry.ServiceType == typeof(CqlReadSourceProfile));
    }

    private sealed class ReadyRegistry(NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider source) : ICqlHostedSourceRegistry
    {
        public NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider Find(ProviderType provider)
            => provider == ProviderType.BerachainOASIS ? source : null!;
        public Task ActivateReadSourceAsync(ProviderType provider, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.Equal(ProviderType.BerachainOASIS, provider);
            return Task.CompletedTask;
        }
    }
}
