using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using AxelarProvider = NextGenSoftware.OASIS.API.Providers.AxelarOASIS.AxelarOASIS;
using MongoSource = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlAxelarHostedAdmissionTests
{
    [Fact]
    public void ExactKeyPresetsPlanNativeAndMongoQueries()
    {
        const string provider = nameof(ProviderType.AxelarOASIS);
        var keys = new[] { "37269326", "37269327", "37269328" };
        using var source = AxelarProvider.FromPublicQueryConfiguration(JsonSerializer.Serialize(new
        {
            schema = 1,
            heights = new[] { 37269326, 37269327, 37269328 },
            sourceOwner = Guid.NewGuid()
        }));
        using var profile = new CqlReadSourceProfile(source);
        var sourceRegistry = new CqlHostedSourceRegistry(new[] { profile });
        Assert.Same(source, sourceRegistry.Find(ProviderType.AxelarOASIS));
        Assert.IsNotAssignableFrom<NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider>(source);

        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.MongoDBOASIS },
            new[] { new StorageQueryTariff(ProviderType.AxelarOASIS, 0, "demo-read-v1") },
            null, new[] { ProviderType.AxelarOASIS });
        Assert.Contains(ProviderType.AxelarOASIS, admission.QueryOnlyProviders);
        var rows = new List<object>();
        foreach (int count in new[] { 3, 2 })
        {
            var selection = new
            {
                orderBy = "None", take = 25, includeMetadata = true,
                filters = new[] { new
                {
                    metadataPath = new[] { AxelarProvider.HeightMetadata },
                    scalar = "String", @operator = "In", value = keys.Take(count).ToArray()
                } }
            };
            rows.Add(new { id = $"native-{count}", label = "Axelar mainnet headers", request = new
            {
                provider, selection.orderBy, selection.take, selection.includeMetadata, selection.filters
            } });
            rows.Add(new { id = $"paired-{count}", label = "Axelar and Mongo", operation = "Federation", request = new
            {
                scope = "Named", providers = new[] { nameof(ProviderType.MongoDBOASIS), provider },
                source = selection, identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 25
            } });
        }
        var examples = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [provider] = rows
        }));
        var planned = examples.For(ProviderType.AxelarOASIS, source.QueryCapabilities,
            target => target == ProviderType.MongoDBOASIS ? MongoSource.CqlCapabilities : null);
        Assert.Equal(4, planned.Count);
        Assert.All(planned, example => Assert.True(example.Available, example.ErrorCode));
        Assert.False(source.IsProviderActivated);
    }
}
