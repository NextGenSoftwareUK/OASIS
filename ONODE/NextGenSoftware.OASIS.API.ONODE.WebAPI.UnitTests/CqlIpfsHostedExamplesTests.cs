using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlIpfsHostedExamplesTests
{
    [Fact]
    public async Task ShippingDependenciesCanParseTheNativeAcknowledgedCidSelectionBeforeAnyIo()
    {
        var provider = new NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IPFSOASIS(new OASISDNA
            { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { StorageProviders = new StorageProviderSettings
                { IPFSOASIS = new IPFSOASISSettings { ConnectionString = "http://127.0.0.1:1" } } } }) { EnableCqlCidKeySets = true };
        var keys = new[] { "QmWjVxGF3RrAGEJSMdJyPnPSwVG6iGt6eUW21DxuU8PMqz", "QmUWJZDunZAVtYhQNFq9QU6P6KEGMpycHpDAbkZ5vv58g6", "QmbTA4mFzsvECPbjV5DPVu1eD4veDPn5SZUCYY8hqu1gqm" };
        var query = CosmicQuery<NextGenSoftware.OASIS.API.Core.Holons.Holon>.Create(CosmicQueryScope.Named(ProviderType.IPFSOASIS).Result)
            .Where(record => keys.Contains(CosmicQueryMetadata.Value<string>(record, "IPFSRecordCid"))).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.IPFSOASIS,
            new[] { new CosmicQueryTarget(ProviderType.IPFSOASIS, provider.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.True(result.ErrorCode == "CQL_PROVIDER_INACTIVE", result.Exception?.ToString() ?? result.ErrorCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeIpfsMembershipMustBeExplicitBeforePublishingLeafAndMongoExamples(bool enabled)
    {
        var examples = new List<object>();
        foreach (var count in new[] { 3, 2 })
        {
            var source = new
            {
                orderBy = "None", take = 10, includeMetadata = true,
                filters = new[] { new { metadataPath = new[] { "IPFSRecordCid" }, scalar = "String", @operator = "In",
                    value = Enumerable.Range(0, count).Select(index => "Qm" + new string((char)('a' + index), 44)).ToArray() } }
            };
            examples.Add(new { id = "native-" + count, label = "Native IPFS records", request = new
                { provider = "IPFSOASIS", source.orderBy, source.take, source.includeMetadata, source.filters } });
            examples.Add(new { id = "paired-" + count, label = "IPFS and Mongo", operation = "Federation", request = new
                { scope = "Named", providers = new[] { "MongoDBOASIS", "IPFSOASIS" }, source,
                    identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 10 } });
        }
        var config = CqlHostedQueryExamples.FromJson(JsonSerializer.Serialize(new Dictionary<string, object> { ["IPFSOASIS"] = examples }));
        var provider = new NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IPFSOASIS(new OASISDNA
            { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { StorageProviders = new StorageProviderSettings
                { IPFSOASIS = new IPFSOASISSettings { ConnectionString = "http://127.0.0.1:1" } } } }) { EnableCqlCidKeySets = enabled };
        if (!enabled)
            Assert.Throws<ArgumentException>(() => config.For(ProviderType.IPFSOASIS, provider.QueryCapabilities));
        else
        {
            var published = config.For(ProviderType.IPFSOASIS, provider.QueryCapabilities,
                _ => CosmicQueryCapabilities.ForStringMetadataKeyLookup("IPFSRecordCid", 128, 8));
            Assert.Equal(4, published.Count);
            Assert.All(published, example => Assert.True(example.Available));
            Assert.Equal(new[] { "Rows", "Federation", "Rows", "Federation" }, published.Select(example => example.Operation));
        }
        Assert.False(provider.IsProviderActivated); // Planning must not probe the native service.
    }
}
