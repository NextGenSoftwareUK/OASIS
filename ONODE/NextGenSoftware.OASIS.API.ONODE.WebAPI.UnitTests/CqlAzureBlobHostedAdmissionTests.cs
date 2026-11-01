using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlAzureBlobHostedAdmissionTests
{
    [Fact]
    public void AzureBlobReadCatalogueRequiresIndependentPermissionAndTariff()
    {
        var providers = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS,
            ProviderType.AzureCosmosDBOASIS, ProviderType.ActivityPubOASIS, ProviderType.IPFSOASIS,
            ProviderType.SOLIDOASIS, ProviderType.AWSS3OASIS, ProviderType.OracleDBOASIS, ProviderType.SolanaOASIS, ProviderType.AzureStorageOASIS };
        var tariffs = providers.Select(provider => new StorageQueryTariff(provider, 0, "demo-read-v1")).ToArray();
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            providers.Take(4), tariffs, null, providers.Skip(4));
        Assert.Equal(providers, factory.QueryProviders);
        Assert.Contains(ProviderType.AzureStorageOASIS, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs,
            new[] { new StorageAttemptTariff(ProviderType.AzureStorageOASIS,"SaveHolon",0,"demo-read-v1") }, providers.Skip(4)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs, null, providers.Skip(4).Take(6)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(4), tariffs.Take(10), null, providers.Skip(4)));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers, tariffs.Append(tariffs[0])));
    }

    [Fact]
    public async Task PublicObjectPresetsPlanNativeThreeTwoAndMongoPairsWithoutCloudCredentials()
    {
        var keys = new[] { "imagery/one.tif", "imagery/two.tif", "imagery/three.tif" };
        using var source = AzureBlobPublicMetadataSource.FromJson(JsonSerializer.Serialize(new {
            schema=1,account="publictest",container="public-data",keys,sourceOwner=Guid.NewGuid()}));
        var rows = new List<object>();
        foreach (var count in new[] { 3, 2 })
        {
            var selection = new { orderBy = "None", take = 8, filters = new[] {
                new { metadataPath = new[] { AzureBlobPublicMetadataSource.ObjectKeyMetadata },
                    scalar = "String", @operator = "In", value = keys.Take(count).ToArray() } } };
            rows.Add(new { id = "native-" + count, label = "Azure public blob properties", request = new {
                provider = "AzureStorageOASIS", selection.orderBy, selection.take, selection.filters } });
            rows.Add(new { id = "paired-" + count, label = "Azure public blob properties + Mongo", operation = "Federation", request = new {
                scope = "Named", providers = new[] { "MongoDBOASIS", "AzureStorageOASIS" }, source = selection,
                identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object> {
            ["AzureStorageOASIS"] = rows }));
        var planned = examples.For(ProviderType.AzureStorageOASIS, source.QueryCapabilities,
            _ => CosmicQueryCapabilities.ForStringMetadataKeyLookup(AzureBlobPublicMetadataSource.ObjectKeyMetadata, 44, 8));
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));

        // Discovery uses the same capability snapshot after explicit read activation.
        var proxy = System.Reflection.DispatchProxy.Create<CqlHostedSourcesTests.QueryStorage, CqlHostedSourcesTests.Adapter>();
        var state = (CqlHostedSourcesTests.Adapter)(object)proxy;
        state.Type = ProviderType.AzureStorageOASIS; state.Active = true; state.Capabilities = source.QueryCapabilities;
        var registry = new ReadyRegistry(proxy);
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.MongoDBOASIS }, new[] { new StorageQueryTariff(ProviderType.MongoDBOASIS, 0, "demo-v1"),
                new StorageQueryTariff(ProviderType.AzureStorageOASIS, 0, "demo-v1") }, null, new[] { ProviderType.AzureStorageOASIS });
        using var hosted = new CqlHostedSources(admission, registry, NullLogger<CqlHostedSources>.Instance, examples);
        await hosted.StartAsync(default); await hosted.ExecuteTask!;
        var description = Assert.Single(hosted.Describe(true).Where(row => row.Provider == "AzureStorageOASIS"));
        Assert.True(description.Available);
        Assert.Equal(4, description.Examples.Count);
        // Mongo is intentionally absent: source rows stay available; its pairs are explicitly unavailable.
        Assert.All(description.Examples.Where(row => row.Operation == "Rows"), row => Assert.True(row.Available));
        Assert.All(description.Examples.Where(row => row.Operation == "Federation"), row => Assert.False(row.Available));
    }

    [Theory]
    [InlineData(137, true)]
    [InlineData(512, true)]
    [InlineData(513, false)]
    public void AzureBlobDemoIdentitiesRespectTheNativeKeyBound(int length, bool accepted)
    {
        var request = new NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlDemoIdentityRequest {
            Provider = "AzureStorageOASIS", Keys = new[] { new string('x', length) }
        };
        if (accepted) Assert.Single(CqlHostedDemoIdentities.Create(request, Guid.NewGuid()).Records);
        else Assert.Throws<ArgumentException>(() => CqlHostedDemoIdentities.Create(request, Guid.NewGuid()));
        request.Provider = "GoogleCloudOASIS";
        Assert.Throws<ArgumentException>(() => CqlHostedDemoIdentities.Create(request, Guid.NewGuid()));
    }

    private sealed class ReadyRegistry(NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider source) : ICqlHostedSourceRegistry
    {
        public NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider Find(ProviderType provider)
            => provider == ProviderType.AzureStorageOASIS ? source : null!;
        public Task ActivateReadSourceAsync(ProviderType provider, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.Equal(ProviderType.AzureStorageOASIS, provider);
            return Task.CompletedTask;
        }
    }
}
