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
        const string provider = nameof(ProviderType.CouchDBOASIS);
        var keys = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var source = new CouchDBOASIS("https://couchdb.example");

        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.MongoDBOASIS },
            new[] { new StorageQueryTariff(ProviderType.CouchDBOASIS, 0, "demo-read-v1") },
            null, new[] { ProviderType.CouchDBOASIS });
        Assert.Contains(ProviderType.CouchDBOASIS, admission.QueryOnlyProviders);
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
            rows.Add(new { id = $"public-{suffix}", label = "CouchDB records", request = new
            {
                provider, selection.orderBy, selection.take, selection.includeMetadata, selection.filters
            } });
            rows.Add(new { id = $"mongo-public-{suffix}", label = "CouchDB and Mongo", operation = "Federation", request = new
            {
                scope = "Named", providers = new[] { nameof(ProviderType.MongoDBOASIS), provider },
                source = selection, identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 25
            } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [provider] = rows
        }));
        var planned = examples.For(ProviderType.CouchDBOASIS, source.QueryCapabilities,
            target => target == ProviderType.MongoDBOASIS ? MongoSource.CqlCapabilities : null,
            (target, actor) => target == ProviderType.CouchDBOASIS
                ? CosmicQueryReadAccess.AuthoritativeRecords(actor, target, keys).Result
                : CosmicQueryReadAccess.OwnerOrPublic(actor).Result);
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));
        Assert.False(source.IsProviderActivated);
    }
}
