using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.AkashOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlAkashHostedAdmissionTests
{
    [Fact]
    public void ProviderOwnerCopyIdentityKeepsTheExisting128CharacterBoundary()
    {
        var actor = Guid.NewGuid();
        const string address = "akash1u5cdg7k3gl43mukca4aeultuz8x2j68mgwn28e";
        var mapped = CqlHostedDemoIdentities.Create(new() { Provider = "AkashOASIS", Keys = new[] { address } }, actor);
        Assert.Equal(address, Assert.Single(mapped.Records).NativeKey);
        Assert.Throws<ArgumentException>(() => CqlHostedDemoIdentities.Create(new() {
            Provider = "AkashOASIS", Keys = new[] { new string('a',129) } }, actor));
    }

    [Fact]
    public void ReadCatalogueRequiresIndependentAkashPermissionAndTariff()
    {
        var providers = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS,
            ProviderType.AzureCosmosDBOASIS, ProviderType.ActivityPubOASIS, ProviderType.IPFSOASIS,
            ProviderType.SOLIDOASIS, ProviderType.AWSS3OASIS, ProviderType.OracleDBOASIS, ProviderType.SolanaOASIS, ProviderType.GoogleCloudOASIS, ProviderType.EthereumOASIS, ProviderType.PolygonOASIS, ProviderType.AkashOASIS };
        var tariffs = providers.Select(provider => new StorageQueryTariff(provider, 0, "demo-read-v1")).ToArray();
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            providers.Take(4), tariffs, null, providers.Skip(4));
        Assert.Equal(providers, factory.QueryProviders);
        Assert.Contains(ProviderType.AkashOASIS, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs,
            new[] { new StorageAttemptTariff(ProviderType.AkashOASIS,"SaveHolon",0,"demo-read-v1") }, providers.Skip(4)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs, null, providers.Skip(4).Take(9)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs.Take(13), null, providers.Skip(4)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers, tariffs.Append(tariffs[0])));
    }

    [Fact]
    public async Task PublicProviderPresetsPlanNativeThreeTwoAndMongoPairsWithoutSigningOrComputeProvisioning()
    {
        var keys = new[] { "akash1qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq7cdc78", "akash1lllllllllllllllllllllllllllllllljfvhed", "akash1u5cdg7k3gl43mukca4aeultuz8x2j68mgwn28e" };
        using var source = AkashPublicProviderMetadataSource.FromJson(JsonSerializer.Serialize(new {
            schema=1,apiUrl="https://example.invalid/",providerOwners=keys,sourceOwner=Guid.NewGuid()}));
        var rows = new List<object>();
        foreach (var count in new[] { 3, 2 })
        {
            var selection = new { orderBy = "None", take = 8, filters = new[] {
                new { metadataPath = new[] { AkashPublicProviderMetadataSource.OwnerMetadata },
                    scalar = "String", @operator = "In", value = keys.Take(count).ToArray() } } };
            rows.Add(new { id = "native-" + count, label = "Akash indexed provider metadata", request = new {
                provider = "AkashOASIS", selection.orderBy, selection.take, selection.filters } });
            rows.Add(new { id = "paired-" + count, label = "Akash indexed provider metadata + Mongo", operation = "Federation", request = new {
                scope = "Named", providers = new[] { "MongoDBOASIS", "AkashOASIS" }, source = selection,
                identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object> {
            ["AkashOASIS"] = rows }));
        var planned = examples.For(ProviderType.AkashOASIS, source.QueryCapabilities,
            _ => CosmicQueryCapabilities.ForStringMetadataKeyLookup(AkashPublicProviderMetadataSource.OwnerMetadata, 44, 8));
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));

        // Discovery uses the same capability snapshot after explicit read activation.
        var proxy = System.Reflection.DispatchProxy.Create<CqlHostedSourcesTests.QueryStorage, CqlHostedSourcesTests.Adapter>();
        var state = (CqlHostedSourcesTests.Adapter)(object)proxy;
        state.Type = ProviderType.AkashOASIS; state.Active = true; state.Capabilities = source.QueryCapabilities;
        var registry = new ReadyRegistry(proxy);
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.MongoDBOASIS }, new[] { new StorageQueryTariff(ProviderType.MongoDBOASIS, 0, "demo-v1"),
                new StorageQueryTariff(ProviderType.AkashOASIS, 0, "demo-v1") }, null, new[] { ProviderType.AkashOASIS });
        using var hosted = new CqlHostedSources(admission, registry, NullLogger<CqlHostedSources>.Instance, examples);
        await hosted.StartAsync(default); await hosted.ExecuteTask!;
        var description = Assert.Single(hosted.Describe(true).Where(row => row.Provider == "AkashOASIS"));
        Assert.True(description.Available);
        Assert.Equal(4, description.Examples.Count);
        // Mongo is intentionally absent: source rows stay available; its pairs are explicitly unavailable.
        Assert.All(description.Examples.Where(row => row.Operation == "Rows"), row => Assert.True(row.Available));
        Assert.All(description.Examples.Where(row => row.Operation == "Federation"), row => Assert.False(row.Available));
    }

    private sealed class ReadyRegistry(NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider source) : ICqlHostedSourceRegistry
    {
        public NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider Find(ProviderType provider)
            => provider == ProviderType.AkashOASIS ? source : null!;
        public Task ActivateReadSourceAsync(ProviderType provider, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.Equal(ProviderType.AkashOASIS, provider);
            return Task.CompletedTask;
        }
    }
}
