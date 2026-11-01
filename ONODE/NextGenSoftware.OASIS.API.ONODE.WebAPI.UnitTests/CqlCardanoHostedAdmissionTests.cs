using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.CardanoOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlCardanoHostedAdmissionTests
{
    [Fact]
    public void ReadCatalogueRequiresIndependentCardanoPermissionAndTariff()
    {
        var providers = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS,
            ProviderType.AzureCosmosDBOASIS, ProviderType.ActivityPubOASIS, ProviderType.IPFSOASIS,
            ProviderType.SOLIDOASIS, ProviderType.AWSS3OASIS, ProviderType.OracleDBOASIS, ProviderType.SolanaOASIS, ProviderType.GoogleCloudOASIS, ProviderType.EthereumOASIS, ProviderType.PolygonOASIS, ProviderType.CardanoOASIS };
        var tariffs = providers.Select(provider => new StorageQueryTariff(provider, 0, "demo-read-v1")).ToArray();
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            providers.Take(4), tariffs, null, providers.Skip(4));
        Assert.Equal(providers, factory.QueryProviders);
        Assert.Contains(ProviderType.CardanoOASIS, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs,
            new[] { new StorageAttemptTariff(ProviderType.CardanoOASIS,"SaveHolon",0,"demo-read-v1") }, providers.Skip(4)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs, null, providers.Skip(4).Take(9)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs.Take(13), null, providers.Skip(4)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers, tariffs.Append(tariffs[0])));
    }

    [Fact]
    public async Task PublicHeaderPresetsPlanNativeThreeTwoAndMongoPairsWithoutWallets()
    {
        var keys = new[] { new string('1', 64), new string('2', 64), new string('3', 64) };
        using var source = CardanoPublicHeaderMetadataSource.FromJson(JsonSerializer.Serialize(new {
            schema = 1, restUrl = CardanoPublicHeaderMetadataSource.PublicEndpoint,
            networkMagic = CardanoPublicHeaderMetadataSource.NetworkMagic,
            blockHashes = keys, sourceOwner = Guid.NewGuid() }));
        var rows = new List<object>();
        foreach (var count in new[] { 3, 2 })
        {
            var selection = new { orderBy = "None", take = 8, filters = new[] {
                new { metadataPath = new[] { CardanoPublicHeaderMetadataSource.AddressMetadata },
                    scalar = "String", @operator = "In", value = keys.Take(count).ToArray() } } };
            rows.Add(new { id = "native-" + count, label = "Cardano historical public headers", request = new {
                provider = "CardanoOASIS", selection.orderBy, selection.take, selection.filters } });
            rows.Add(new { id = "paired-" + count, label = "Cardano historical public headers + Mongo", operation = "Federation", request = new {
                scope = "Named", providers = new[] { "MongoDBOASIS", "CardanoOASIS" }, source = selection,
                identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object> {
            ["CardanoOASIS"] = rows }));
        var planned = examples.For(ProviderType.CardanoOASIS, source.QueryCapabilities,
            _ => CosmicQueryCapabilities.ForStringMetadataKeyLookup(CardanoPublicHeaderMetadataSource.AddressMetadata, 64, 8));
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));

        // Discovery uses the same capability snapshot after explicit read activation.
        var proxy = System.Reflection.DispatchProxy.Create<CqlHostedSourcesTests.QueryStorage, CqlHostedSourcesTests.Adapter>();
        var state = (CqlHostedSourcesTests.Adapter)(object)proxy;
        state.Type = ProviderType.CardanoOASIS; state.Active = true; state.Capabilities = source.QueryCapabilities;
        var registry = new ReadyRegistry(proxy);
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.MongoDBOASIS }, new[] { new StorageQueryTariff(ProviderType.MongoDBOASIS, 0, "demo-v1"),
                new StorageQueryTariff(ProviderType.CardanoOASIS, 0, "demo-v1") }, null, new[] { ProviderType.CardanoOASIS });
        using var hosted = new CqlHostedSources(admission, registry, NullLogger<CqlHostedSources>.Instance, examples);
        await hosted.StartAsync(default); await hosted.ExecuteTask!;
        var description = Assert.Single(hosted.Describe(true).Where(row => row.Provider == "CardanoOASIS"));
        Assert.True(description.Available);
        Assert.Equal(4, description.Examples.Count);
        // Mongo is intentionally absent: source rows stay available; its pairs are explicitly unavailable.
        Assert.All(description.Examples.Where(row => row.Operation == "Rows"), row => Assert.True(row.Available));
        Assert.All(description.Examples.Where(row => row.Operation == "Federation"), row => Assert.False(row.Available));
    }

    private sealed class ReadyRegistry(NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider source) : ICqlHostedSourceRegistry
    {
        public NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider Find(ProviderType provider)
            => provider == ProviderType.CardanoOASIS ? source : null!;
        public Task ActivateReadSourceAsync(ProviderType provider, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.Equal(ProviderType.CardanoOASIS, provider);
            return Task.CompletedTask;
        }
    }
}
