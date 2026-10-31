using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Source = NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.AwsS3PublicMetadataSource;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlAwsS3PublicExamplesTests
{
    [Fact]
    public void ShippingAdmissionAcceptsEightExplicitReadTariffsIncludingQueryOnlyS3()
    {
        // Regression: valid presets alone did not exercise host startup. The
        // deployed eight-provider catalogue must pass the real admission owner.
        var providers = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS,
            ProviderType.Neo4jOASIS, ProviderType.AzureCosmosDBOASIS, ProviderType.ActivityPubOASIS,
            ProviderType.IPFSOASIS, ProviderType.SOLIDOASIS, ProviderType.AWSS3OASIS };
        var tariffs = providers.Select(provider => new StorageQueryTariff(provider, 0, "sandbox-demo-free-v2")).ToArray();
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            providers.Take(4), tariffs, null, providers.Skip(4));

        Assert.Equal(providers, admission.QueryProviders);
        Assert.Contains(ProviderType.AWSS3OASIS, admission.QueryOnlyProviders);

        // Adding S3 to the supported set grants neither implicit permission nor
        // permission to duplicate/overflow the bounded server catalogue.
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers.Take(7), tariffs));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers, tariffs.Append(tariffs[0])));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), providers,
            new[] { new StorageQueryTariff(ProviderType.AWSS3OASIS, -1, "sandbox-demo-free-v2") }));
    }

    [Fact]
    public void PublicMetadataPresetsUseActualSourceCapabilitiesAndKeepMongoDependencyExplicit()
    {
        var keys = new[] { "observations/one.nc", "observations/two.nc", "observations/three.nc" };
        var examples = new List<object>();

        foreach (var count in new[] { 3, 2 })
        {
            var selection = new
            {
                orderBy = "None", take = 8, includeMetadata = true,
                filters = new[] { new
                {
                    metadataPath = new[] { Source.ObjectKeyMetadata }, scalar = "String", @operator = "In",
                    value = keys.Take(count).ToArray()
                } }
            };
            examples.Add(new { id = "native-" + count, label = "Public S3 object metadata", request = new
                { provider = "AWSS3OASIS", selection.orderBy, selection.take, selection.includeMetadata, selection.filters } });
            examples.Add(new { id = "paired-" + count, label = "S3 metadata + my Mongo records", operation = "Federation", request = new
                { scope = "Named", providers = new[] { "MongoDBOASIS", "AWSS3OASIS" }, source = selection,
                    identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 } });
        }

        // Anonymous client construction performs no native requests. These
        // checks exercise HTTP capture/planning, not public-source readiness.
        using var client = new AmazonS3Client(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
        using var source = new Source(client, "public-test-bucket", keys, Guid.NewGuid());
        var configuration = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(
            new Dictionary<string, object> { ["AWSS3OASIS"] = examples }));

        var available = configuration.For(ProviderType.AWSS3OASIS, source.QueryCapabilities,
            _ => CosmicQueryCapabilities.ForStringMetadataKeyLookup(Source.ObjectKeyMetadata, 128, 8));
        Assert.Equal(new[] { "Rows", "Federation", "Rows", "Federation" }, available.Select(example => example.Operation));
        Assert.All(available, example => Assert.True(example.Available));

        var withoutMongo = configuration.For(ProviderType.AWSS3OASIS, source.QueryCapabilities);
        Assert.All(withoutMongo.Where(example => example.Operation == "Rows"), example => Assert.True(example.Available));
        Assert.All(withoutMongo.Where(example => example.Operation == "Federation"), example =>
        {
            Assert.False(example.Available);
            Assert.Equal("CQL_DEMO_TARGET_UNAVAILABLE", example.ErrorCode);
        });
        Assert.False(source.IsProviderActivated);
    }
}
