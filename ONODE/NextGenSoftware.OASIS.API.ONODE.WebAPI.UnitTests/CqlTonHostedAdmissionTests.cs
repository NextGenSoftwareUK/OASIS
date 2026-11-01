using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.TONOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlTonHostedAdmissionTests
{
    // A paced remote source needs an explicit bounded budget. The HTTP contract
    // must admit the same duration domain as the Core federation planner.
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5000, true)]
    [InlineData(5001, true)]
    [InlineData(30000, true)]
    [InlineData(30001, false)]
    public void HttpDurationAdmissionMatchesCore(int milliseconds, bool accepted)
    {
        var request = new NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlFederationV1Request
        {
            MaximumDurationMilliseconds = milliseconds
        };
        var core = CosmicFederationOptions.Create(maximumDurationMilliseconds: milliseconds);
        Assert.Equal(accepted, !core.IsError);
        if (accepted)
            Assert.Equal(milliseconds, CqlRequestCapture.CaptureAndValidateFederation(request).MaximumDurationMilliseconds);
        else
            Assert.Throws<ArgumentException>(() => CqlRequestCapture.CaptureAndValidateFederation(request));
        Assert.Equal(5000, new NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlFederationV1Request().MaximumDurationMilliseconds);
    }

    [Fact]
    public void ReadCatalogueRequiresIndependentTonPermissionAndTariff()
    {
        var providers = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS,
            ProviderType.AzureCosmosDBOASIS, ProviderType.ActivityPubOASIS, ProviderType.IPFSOASIS,
            ProviderType.SOLIDOASIS, ProviderType.AWSS3OASIS, ProviderType.OracleDBOASIS, ProviderType.SolanaOASIS, ProviderType.GoogleCloudOASIS, ProviderType.EthereumOASIS, ProviderType.PolygonOASIS, ProviderType.TONOASIS };
        var tariffs = providers.Select(provider => new StorageQueryTariff(provider, 0, "demo-read-v1")).ToArray();
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            providers.Take(4), tariffs, null, providers.Skip(4));
        Assert.Equal(providers, factory.QueryProviders);
        Assert.Contains(ProviderType.TONOASIS, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs,
            new[] { new StorageAttemptTariff(ProviderType.TONOASIS,"SaveHolon",0,"demo-read-v1") }, providers.Skip(4)));
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
        var keys = Enumerable.Range(1, 3).Select(value => Convert.ToBase64String(Enumerable.Repeat((byte)value, 32).ToArray())).ToArray();
        using var source = TonPublicHeaderMetadataSource.FromJson(JsonSerializer.Serialize(new {
            schema = 1, restUrl = TonPublicHeaderMetadataSource.PublicEndpoint,
            globalId = -239,
            blocks = keys.Select((key, index) => new { seqno = index + 10, rootHash = key, fileHash = key }), sourceOwner = Guid.NewGuid() }));
        var rows = new List<object>();
        foreach (var count in new[] { 3, 2 })
        {
            var selection = new { orderBy = "None", take = 8, filters = new[] {
                new { metadataPath = new[] { TonPublicHeaderMetadataSource.AddressMetadata },
                    scalar = "String", @operator = "In", value = keys.Take(count).ToArray() } } };
            rows.Add(new { id = "native-" + count, label = "Ton historical public headers", request = new {
                provider = "TONOASIS", selection.orderBy, selection.take, selection.filters } });
            rows.Add(new { id = "paired-" + count, label = "Ton historical public headers + Mongo", operation = "Federation", request = new {
                scope = "Named", providers = new[] { "MongoDBOASIS", "TONOASIS" }, source = selection,
                identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object> {
            ["TONOASIS"] = rows }));
        var planned = examples.For(ProviderType.TONOASIS, source.QueryCapabilities,
            _ => CosmicQueryCapabilities.ForStringMetadataKeyLookup(TonPublicHeaderMetadataSource.AddressMetadata, 44, 8));
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));

        // Discovery uses the same capability snapshot after explicit read activation.
        var proxy = System.Reflection.DispatchProxy.Create<CqlHostedSourcesTests.QueryStorage, CqlHostedSourcesTests.Adapter>();
        var state = (CqlHostedSourcesTests.Adapter)(object)proxy;
        state.Type = ProviderType.TONOASIS; state.Active = true; state.Capabilities = source.QueryCapabilities;
        var registry = new ReadyRegistry(proxy);
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.MongoDBOASIS }, new[] { new StorageQueryTariff(ProviderType.MongoDBOASIS, 0, "demo-v1"),
                new StorageQueryTariff(ProviderType.TONOASIS, 0, "demo-v1") }, null, new[] { ProviderType.TONOASIS });
        using var hosted = new CqlHostedSources(admission, registry, NullLogger<CqlHostedSources>.Instance, examples);
        await hosted.StartAsync(default); await hosted.ExecuteTask!;
        var description = Assert.Single(hosted.Describe(true).Where(row => row.Provider == "TONOASIS"));
        Assert.True(description.Available);
        Assert.Equal(4, description.Examples.Count);
        // Mongo is intentionally absent: source rows stay available; its pairs are explicitly unavailable.
        Assert.All(description.Examples.Where(row => row.Operation == "Rows"), row => Assert.True(row.Available));
        Assert.All(description.Examples.Where(row => row.Operation == "Federation"), row => Assert.False(row.Available));
    }

    private sealed class ReadyRegistry(NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider source) : ICqlHostedSourceRegistry
    {
        public NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider Find(ProviderType provider)
            => provider == ProviderType.TONOASIS ? source : null!;
        public Task ActivateReadSourceAsync(ProviderType provider, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.Equal(ProviderType.TONOASIS, provider);
            return Task.CompletedTask;
        }
    }
}
