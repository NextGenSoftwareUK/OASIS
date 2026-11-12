using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.FhenixOASIS;
using MongoSource = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlFhenixHostedAdmissionTests
{
    [Fact]
    public void ExactKeyPresetsPlanNativeAndMongoQueries()
    {
        const string provider = nameof(ProviderType.FhenixOASIS);
        var keys = new[] { "1", "11155111", "84532" };
        using var source = FhenixPublicTaskManagerSource.FromJson(JsonSerializer.Serialize(new
        {
            schema = 1,
            chainIds = keys,
            sourceOwner = Guid.NewGuid()
        }));
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.MongoDBOASIS },
            new[] { new StorageQueryTariff(ProviderType.FhenixOASIS, 0, "demo-read-v1") },
            null, new[] { ProviderType.FhenixOASIS });
        Assert.Contains(ProviderType.FhenixOASIS, admission.QueryOnlyProviders);
        var rows = new List<object>();
        foreach (int count in new[] { 3, 2 })
        {
            var selection = new
            {
                orderBy = "None", take = 25, includeMetadata = true,
                filters = new[] { new
                {
                    metadataPath = new[] { FhenixPublicTaskManagerSource.ChainIdMetadata },
                    scalar = "String", @operator = "In", value = keys.Take(count).ToArray()
                } }
            };
            rows.Add(new { id = $"native-{count}", label = "Fhenix CoFHE deployments", request = new
            {
                provider, selection.orderBy, selection.take, selection.includeMetadata, selection.filters
            } });
            rows.Add(new { id = $"paired-{count}", label = "Fhenix and Mongo", operation = "Federation", request = new
            {
                scope = "Named", providers = new[] { nameof(ProviderType.MongoDBOASIS), provider },
                source = selection, identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 25
            } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [provider] = rows
        }));
        var planned = examples.For(ProviderType.FhenixOASIS, source.QueryCapabilities,
            target => target == ProviderType.MongoDBOASIS ? MongoSource.CqlCapabilities : null);
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));
        Assert.False(source.IsProviderActivated);
    }
}
