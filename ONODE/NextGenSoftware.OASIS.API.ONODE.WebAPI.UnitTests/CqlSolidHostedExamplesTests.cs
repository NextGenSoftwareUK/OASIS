using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Provider = NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.SOLIDOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

/// <summary>
/// Exercise the shipping HTTP example compiler against SOLID's actual adapter.
/// These checks do not claim that a Pod has been provisioned or deployed.
/// </summary>
public class CqlSolidHostedExamplesTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PublicPresetsRequireExplicitNativeMembershipAndMongoDependency(bool enabled)
    {
        // Resource identities are public selection keys, never credentials or grants.
        var keys = new[]
        {
            "bf89482b-470c-45aa-b750-b19fa6d8e9ea",
            "bf89482b-470c-45aa-b750-b19fa6d8e9eb",
            "bf89482b-470c-45aa-b750-b19fa6d8e9ec"
        };

        var examples = new List<object>();

        foreach (var count in new[] { 3, 2 })
        {
            var source = new
            {
                orderBy = "None",
                take = 10,
                includeMetadata = true,
                filters = new[] { new
                {
                    metadataPath = new[] { Provider.CqlRecordIdKey },
                    scalar = "String",
                    @operator = "In",
                    value = keys.Take(count).ToArray()
                } }
            };

            examples.Add(new
            {
                id = "native-" + count,
                label = "SOLID native resource selection",
                request = new { provider = "SOLIDOASIS", source.orderBy, source.take, source.includeMetadata, source.filters }
            });

            examples.Add(new
            {
                id = "paired-" + count,
                label = "SOLID and private Mongo snapshots",
                operation = "Federation",
                request = new
                {
                    scope = "Named",
                    providers = new[] { "MongoDBOASIS", "SOLIDOASIS" },
                    source,
                    identityMode = "PreserveCopies",
                    maximumProviderAttempts = 2,
                    take = 10
                }
            });
        }

        var configuration = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(
            new Dictionary<string, object> { ["SOLIDOASIS"] = examples }));

        // No native request may run while compiling discovery's ready examples.
        using var provider = new Provider("https://pod.invalid/demo/", "",
            Enumerable.Range(1, 32).Select(value => (byte)value).ToArray())
        {
            EnableCqlRecordKeySets = enabled
        };

        if (!enabled)
        {
            Assert.Throws<ArgumentException>(() => configuration.For(ProviderType.SOLIDOASIS, provider.QueryCapabilities));
        }
        else
        {
            CosmicQueryCapabilities Capability(ProviderType type) => type switch
            {
                ProviderType.SOLIDOASIS => provider.QueryCapabilities,
                ProviderType.MongoDBOASIS => CosmicQueryCapabilities.ForStringMetadataKeyLookup(Provider.CqlRecordIdKey, 36, 8),
                _ => throw new InvalidOperationException("An example introduced an unconfigured provider.")
            };

            var published = configuration.For(ProviderType.SOLIDOASIS, provider.QueryCapabilities, Capability);

            Assert.Equal(new[] { "Rows", "Federation", "Rows", "Federation" }, published.Select(example => example.Operation));
            Assert.All(published, example => Assert.True(example.Available));

            // A missing Mongo connection must disable only the paired examples.
            var withoutMongo = configuration.For(ProviderType.SOLIDOASIS, provider.QueryCapabilities);

            Assert.All(withoutMongo.Where(example => example.Operation == "Rows"), example => Assert.True(example.Available));
            Assert.All(withoutMongo.Where(example => example.Operation == "Federation"), example =>
            {
                Assert.False(example.Available);
                Assert.Equal("CQL_DEMO_TARGET_UNAVAILABLE", example.ErrorCode);
            });
        }

        Assert.False(provider.IsProviderActivated);
    }
}
