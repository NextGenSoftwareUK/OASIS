using System.Text.Json;
using System.Text.Json.Nodes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlHostedFederationExamplesTests
{
    internal const string Valid = """
        {"ActivityPubOASIS":[{"id":"paired","label":"Public posts and my Mongo records","operation":"Federation","request":{
          "scope":"Named","providers":["MongoDBOASIS","ActivityPubOASIS"],"maximumProviderAttempts":2,
          "source":{"orderBy":"None","take":10,"filters":[{"metadataPath":["NativeKey"],"scalar":"String","operator":"In","value":["101","102","103"]}]}
        }}]}
        """;
    private static CosmicQueryCapabilities Keys(string key = "NativeKey")
        => CosmicQueryCapabilities.ForStringMetadataKeyLookup(key, 128, 40);

    [Fact]
    public void FederationExampleIsCapturedAndPlansEveryActualTargetWithoutPerformingARead()
    {
        var calls = new List<ProviderType>();
        var examples = CqlHostedQueryExamples.FromJson(Valid);
        var row = Assert.Single(examples.For(ProviderType.ActivityPubOASIS, Keys(), target => { calls.Add(target); return Keys(); }));
        Assert.True(row.Available);
        Assert.Equal("Federation", row.Operation);
        Assert.Equal(new[] { ProviderType.MongoDBOASIS }, calls);
        Assert.Equal("Named", row.Request.GetProperty("scope").GetString());
        Assert.Equal(3, row.Request.GetProperty("source").GetProperty("filters")[0].GetProperty("value").GetArrayLength());
        var wire = JsonSerializer.SerializeToElement(row, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("Federation", wire.GetProperty("operation").GetString());
        Assert.True(wire.GetProperty("available").GetBoolean());
    }

    [Fact]
    public void UnavailableDependencyDisablesOnlyFederationAndKeepsTheLeafExample()
    {
        var config = JsonNode.Parse(Valid)!;
        config["ActivityPubOASIS"]!.AsArray().Add(JsonNode.Parse("""
            {"id":"leaf","label":"Public posts","request":{"provider":"ActivityPubOASIS","orderBy":"None",
            "filters":[{"metadataPath":["NativeKey"],"operator":"In","value":["101"]}]}}
            """));
        var rows = CqlHostedQueryExamples.FromJson(config.ToJsonString()).For(ProviderType.ActivityPubOASIS, Keys());
        Assert.False(rows[0].Available);
        Assert.Equal("CQL_DEMO_TARGET_UNAVAILABLE", rows[0].ErrorCode);
        Assert.True(rows[1].Available);
        Assert.Equal("Rows", rows[1].Operation);
        Assert.Equal("Federation", rows[0].Operation); // No replacement query or point fan-out.
    }

    [Fact]
    public void UnsupportedDependencyDoesNotAdvertiseAnExecutableFederation()
    {
        var row = Assert.Single(CqlHostedQueryExamples.FromJson(Valid).For(ProviderType.ActivityPubOASIS,
            Keys(), _ => CosmicQueryCapabilities.ForStringMetadataPointLookup("NativeKey")));
        Assert.False(row.Available);
        Assert.Equal("CQL_DEMO_EXAMPLE_UNSUPPORTED", row.ErrorCode);
        Assert.Equal(2, row.Request.GetProperty("providers").GetArrayLength());
    }

    [Theory]
    [InlineData("operation")]
    [InlineData("owner")]
    [InlineData("duplicate")]
    [InlineData("scope")]
    [InlineData("cursor")]
    [InlineData("provider")]
    [InlineData("attempts")]
    [InlineData("budget")]
    [InlineData("extra")]
    public void ConfigurationCannotRebindFederationOrBypassTheOwningHttpBounds(string fault)
    {
        var config = JsonNode.Parse(Valid)!;
        var example = config["ActivityPubOASIS"]![0]!;
        var request = example["request"]!;
        switch (fault)
        {
            case "operation": example["operation"] = "Delete"; break;
            case "owner": request["providers"]![1] = "Neo4jOASIS"; break;
            case "duplicate": request["providers"]![0] = "ActivityPubOASIS"; break;
            case "scope": request["scope"] = "AllEligible"; break;
            case "cursor": request["enableContinuation"] = true; break;
            case "provider": request["source"]!["provider"] = "MongoDBOASIS"; break;
            case "attempts": request["maximumProviderAttempts"] = 1; break;
            case "budget": request["maximumOutputBytes"] = 1048577; break;
            case "extra": example["url"] = "https://example.invalid"; break;
        }
        Assert.Throws<ArgumentException>(() => CqlHostedQueryExamples.FromJson(config.ToJsonString()));
    }

    [Fact]
    public void ShippedFederationFragmentKeepsExplicitTargetsAndFullSubsetNativeKeys()
    {
        var examples = CqlHostedQueryExamples.FromJson(System.IO.File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "cql-activitypub-federation-examples.json")));
        var rows = examples.For(ProviderType.ActivityPubOASIS, Keys("MastodonStatusId"), _ => Keys("MastodonStatusId"));
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => { Assert.True(row.Available); Assert.Equal("Federation", row.Operation); });
        Assert.Equal(new[] { 3, 2 }, rows.Select(row => row.Request.GetProperty("source").GetProperty("filters")[0]
            .GetProperty("value").GetArrayLength()));
    }
}
