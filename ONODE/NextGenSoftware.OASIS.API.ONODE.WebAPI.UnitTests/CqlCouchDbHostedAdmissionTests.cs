using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.CouchDBOASIS;
using MongoSource = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlCouchDbHostedAdmissionTests
{
    [Fact]
    public void ExactKeyPresetsPlanNativeAndMongoQueries()
    {
        var source = new CouchDBOASIS("https://couchdb.example");
        VerifyExactKeyPresets(ProviderType.CouchDBOASIS, source.QueryCapabilities);
        Assert.False(source.IsProviderActivated);
    }

    [Fact]
    public void ChromaExactKeyPresetsPlanNativeAndMongoQueries()
    {
        using var client = new System.Net.Http.HttpClient();
        var source = new NextGenSoftware.OASIS.API.Providers.ChromaOASIS.ChromaOASIS(
            client, "https://chroma.example", "demo-tenant", "demo-database");
        VerifyExactKeyPresets(ProviderType.ChromaOASIS, source.QueryCapabilities);
        Assert.False(source.IsProviderActivated);
    }

    [Fact]
    public void MinioExactKeyPresetsPlanNativeAndMongoQueries()
    {
        var source = new NextGenSoftware.OASIS.API.Providers.MinIOOASIS.MinIOOASIS(
            "https://minio.example", "controlled", "controlled", "owned-teaching", true, Guid.NewGuid());
        VerifyExactKeyPresets(ProviderType.MinIOOASIS, source.QueryCapabilities);
        Assert.False(source.IsProviderActivated);
    }

    [Fact]
    public void PouchDbExactKeyPresetsPlanNativeAndMongoQueries()
    {
        using var client = new System.Net.Http.HttpClient();
        var source = new NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.PouchDBOASIS(
            CqlPouchDbReadSourceRegistration.Origin, null, null, client);
        VerifyExactKeyPresets(ProviderType.PouchDBOASIS, source.QueryCapabilities);
        Assert.False(source.IsProviderActivated);
    }

    [Fact]
    public void MeilisearchExactKeyPresetsPlanNativeAndMongoQueries()
    {
        using var client = new System.Net.Http.HttpClient();
        var source = new NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.MeilisearchOASIS(
            "https://meili.example.test", null, client);
        VerifyExactKeyPresets(ProviderType.MeilisearchOASIS, source.QueryCapabilities);
        Assert.False(source.IsProviderActivated);
    }

    [Fact]
    public void SolrExactKeyPresetsPlanNativeAndMongoQueries()
    {
        using var client = new System.Net.Http.HttpClient();
        var source = new NextGenSoftware.OASIS.API.Providers.SolrOASIS.SolrOASIS(
            "https://solr.example.test", null, null, client);
        VerifyExactKeyPresets(ProviderType.SolrOASIS, source.QueryCapabilities);
        Assert.False(source.IsProviderActivated);
    }

    [Fact]
    public void ArcadeDbExactKeyPresetsPlanNativeAndMongoQueries()
    {
        using var client = new System.Net.Http.HttpClient();
        var source = new NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS.ArcadeDBOASIS(
            "https://arcade.example.test", "reader", "restricted", client);
        VerifyExactKeyPresets(ProviderType.ArcadeDBOASIS, source.QueryCapabilities);
    }

    [Fact]
    public void SurrealDbExactKeyPresetsPlanNativeAndMongoQueries()
    {
        var source = new NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.SurrealDBOASIS(
            "https://surreal.example.test", "reader", "restricted", "teaching", "public_demo", Guid.NewGuid());
        VerifyExactKeyPresets(ProviderType.SurrealDBOASIS, source.QueryCapabilities);
    }

    [Theory]
    [InlineData(ProviderType.ElasticsearchOASIS)]
    [InlineData(ProviderType.OpenSearchOASIS)]
    public void SearchDocumentExactKeyPresetsPlanNativeAndMongoQueries(ProviderType provider)
    {
        using var client = new System.Net.Http.HttpClient();
        var capabilities = provider == ProviderType.ElasticsearchOASIS
            ? new NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.ElasticsearchOASIS("https://elastic.example.test", httpClient: client).QueryCapabilities
            : new NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.OpenSearchOASIS("https://open.example.test", httpClient: client).QueryCapabilities;
        VerifyExactKeyPresets(provider, capabilities);
    }

    private static void VerifyExactKeyPresets(ProviderType providerType, CosmicQueryCapabilities capabilities)
    {
        var provider = providerType.ToString();
        var keys = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.MongoDBOASIS },
            new[] { new StorageQueryTariff(providerType, 0, "demo-read-v1") },
            null, new[] { providerType });
        Assert.Contains(providerType, admission.QueryOnlyProviders);
        var rows = new List<object>();
        foreach (int count in new[] { 3, 2 })
        {
            var suffix = count == 3 ? "records" : "subset";
            var selection = new
            {
                orderBy = "None", take = 25, includeMetadata = true,
                filters = new[] { new
                {
                    field = "Id",
                    scalar = "Guid", @operator = "In", value = keys.Take(count).ToArray()
                } }
            };
            rows.Add(new { id = $"public-{suffix}", label = $"{provider} records", request = new
            {
                provider, selection.orderBy, selection.take, selection.includeMetadata, selection.filters
            } });
            rows.Add(new { id = $"mongo-public-{suffix}", label = $"{provider} and Mongo", operation = "Federation", request = new
            {
                scope = "Named", providers = new[] { nameof(ProviderType.MongoDBOASIS), provider },
                source = selection, identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 25
            } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [provider] = rows
        }));
        var planned = examples.For(providerType, capabilities,
            target => target == ProviderType.MongoDBOASIS ? MongoSource.CqlCapabilities : null,
            (target, actor) => target == providerType
                ? CosmicQueryReadAccess.AuthoritativeRecords(actor, target, keys).Result
                : CosmicQueryReadAccess.OwnerOrPublic(actor).Result);
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));
    }
}
