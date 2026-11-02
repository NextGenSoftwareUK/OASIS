using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.EclipseOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlEclipseHostedAdmissionTests
{
    [Fact]
    public void NameCopyIdentityKeepsTheExisting128CharacterBoundary()
    {
        var actor = Guid.NewGuid();
        const string address = "SysvarC1ock11111111111111111111111111111111";
        var mapped = CqlHostedDemoIdentities.Create(new() { Provider = "EclipseOASIS", Keys = new[] { address } }, actor);
        Assert.Equal(address, Assert.Single(mapped.Records).NativeKey);
        Assert.Throws<ArgumentException>(() => CqlHostedDemoIdentities.Create(new() {
            Provider = "EclipseOASIS", Keys = new[] { new string('a',129) } }, actor));
    }

    [Fact]
    public void ReadCatalogueRequiresIndependentEclipsePermissionAndTariff()
    {
        var providers = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS,
            ProviderType.AzureCosmosDBOASIS, ProviderType.ActivityPubOASIS, ProviderType.IPFSOASIS,
            ProviderType.SOLIDOASIS, ProviderType.AWSS3OASIS, ProviderType.OracleDBOASIS, ProviderType.SolanaOASIS, ProviderType.GoogleCloudOASIS, ProviderType.EthereumOASIS, ProviderType.PolygonOASIS, ProviderType.EclipseOASIS };
        var tariffs = providers.Select(provider => new StorageQueryTariff(provider, 0, "demo-read-v1")).ToArray();
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            providers.Take(4), tariffs, null, providers.Skip(4));
        Assert.Equal(providers, factory.QueryProviders);
        Assert.Contains(ProviderType.EclipseOASIS, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs,
            new[] { new StorageAttemptTariff(ProviderType.EclipseOASIS,"SaveHolon",0,"demo-read-v1") }, providers.Skip(4)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs, null, providers.Skip(4).Take(9)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs.Take(13), null, providers.Skip(4)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers, tariffs.Append(tariffs[0])));
    }

    [Fact]
    public async Task PublicNamePresetsPlanNativeThreeTwoAndMongoPairsWithoutSigningOrTransactionSubmission()
    {
        var keys = new[] { "11111111111111111111111111111111", "SysvarC1ock11111111111111111111111111111111", "SysvarRent111111111111111111111111111111111" };
        using var source = EclipsePublicAccountMetadataSource.FromJson(JsonSerializer.Serialize(new {
            schema=1,rpcUrl="https://example.invalid/",addresses=keys,genesisHash="EAQLJCV2mh23BsK2P9oYpV5CHVLDNHTxYss3URrNmg3s",sourceOwner=Guid.NewGuid()}));
        var rows = new List<object>();
        foreach (var count in new[] { 3, 2 })
        {
            var selection = new { orderBy = "None", take = 8, filters = new[] {
                new { metadataPath = new[] { EclipsePublicAccountMetadataSource.AddressMetadata },
                    scalar = "String", @operator = "In", value = keys.Take(count).ToArray() } } };
            rows.Add(new { id = "native-" + count, label = "Eclipse account metadata", request = new {
                provider = "EclipseOASIS", selection.orderBy, selection.take, selection.filters } });
            rows.Add(new { id = "paired-" + count, label = "Eclipse account metadata + Mongo", operation = "Federation", request = new {
                scope = "Named", providers = new[] { "MongoDBOASIS", "EclipseOASIS" }, source = selection,
                identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object> {
            ["EclipseOASIS"] = rows }));
        var planned = examples.For(ProviderType.EclipseOASIS, source.QueryCapabilities,
            _ => CosmicQueryCapabilities.ForStringMetadataKeyLookup(EclipsePublicAccountMetadataSource.AddressMetadata, 44, 8));
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));

        // Discovery uses the same capability snapshot after explicit read activation.
        var proxy = System.Reflection.DispatchProxy.Create<CqlHostedSourcesTests.QueryStorage, CqlHostedSourcesTests.Adapter>();
        var state = (CqlHostedSourcesTests.Adapter)(object)proxy;
        state.Type = ProviderType.EclipseOASIS; state.Active = true; state.Capabilities = source.QueryCapabilities;
        var registry = new ReadyRegistry(proxy);
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.MongoDBOASIS }, new[] { new StorageQueryTariff(ProviderType.MongoDBOASIS, 0, "demo-v1"),
                new StorageQueryTariff(ProviderType.EclipseOASIS, 0, "demo-v1") }, null, new[] { ProviderType.EclipseOASIS });
        using var hosted = new CqlHostedSources(admission, registry, NullLogger<CqlHostedSources>.Instance, examples);
        await hosted.StartAsync(default); await hosted.ExecuteTask!;
        var description = Assert.Single(hosted.Describe(true).Where(row => row.Provider == "EclipseOASIS"));
        Assert.True(description.Available);
        Assert.Equal(4, description.Examples.Count);
        // Mongo is intentionally absent: source rows stay available; its pairs are explicitly unavailable.
        Assert.All(description.Examples.Where(row => row.Operation == "Rows"), row => Assert.True(row.Available));
        Assert.All(description.Examples.Where(row => row.Operation == "Federation"), row => Assert.False(row.Available));
    }

    private sealed class ReadyRegistry(NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider source) : ICqlHostedSourceRegistry
    {
        public NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider Find(ProviderType provider)
            => provider == ProviderType.EclipseOASIS ? source : null!;
        public Task ActivateReadSourceAsync(ProviderType provider, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.Equal(ProviderType.EclipseOASIS, provider);
            return Task.CompletedTask;
        }
    }
}
