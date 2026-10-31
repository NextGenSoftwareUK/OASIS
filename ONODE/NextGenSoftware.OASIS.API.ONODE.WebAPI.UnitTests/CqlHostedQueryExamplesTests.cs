using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlHostedQueryExamplesTests
{
    private const string Valid = """
        {"ActivityPubOASIS":[{"id":"all","label":"Read three public posts","request":{
          "provider":"ActivityPubOASIS","orderBy":"None","take":3,
          "filters":[{"metadataPath":["NativeKey"],"scalar":"String","operator":"In","value":["101","102","103"]}]
        }}]}
        """;

    [Fact]
    public void PublicExampleSurvivesConfigDocumentDisposalAndPassesNativeKeySetPlanning()
    {
        var examples = CqlHostedQueryExamples.FromJson(Valid);
        var row = Assert.Single(examples.For(ProviderType.ActivityPubOASIS,
            CosmicQueryCapabilities.ForStringMetadataKeyLookup("NativeKey", 128, 40)));
        Assert.Equal("all", row.Id);
        Assert.Equal("ActivityPubOASIS", row.Request.GetProperty("provider").GetString());
        Assert.Equal("In", row.Request.GetProperty("filters")[0].GetProperty("operator").GetString());
        Assert.Equal(3, row.Request.GetProperty("filters")[0].GetProperty("value").GetArrayLength());
    }

    [Fact]
    public void UnsupportedExampleIsRejectedWithoutReplacingItWithPointQueries()
    {
        var examples = CqlHostedQueryExamples.FromJson(Valid);
        Assert.Throws<ArgumentException>(() => examples.For(ProviderType.ActivityPubOASIS,
            CosmicQueryCapabilities.ForStringMetadataPointLookup("NativeKey")));
        Assert.Throws<ArgumentException>(() => examples.For(ProviderType.ActivityPubOASIS,
            CosmicQueryCapabilities.ForStringMetadataKeyLookup("OtherKey", 128, 40)));
    }

    [Fact]
    public void AbsentExamplesDoNotFabricateDataOrEnrolAnotherProvider()
    {
        Assert.Empty(CqlHostedQueryExamples.FromJson(null).For(ProviderType.ActivityPubOASIS,
            CosmicQueryCapabilities.ForStringMetadataPointLookup("NativeKey")));
        Assert.Empty(CqlHostedQueryExamples.FromJson(Valid).For(ProviderType.MongoDBOASIS,
            CosmicQueryCapabilities.ForStringMetadataPointLookup("NativeKey")));
    }

    [Theory]
    [InlineData("scope")]
    [InlineData("duplicate-provider")]
    [InlineData("duplicate-request-field")]
    [InlineData("private-field")]
    [InlineData("unknown-example-field")]
    [InlineData("empty-source")]
    public void AmbiguousOrMisownedExamplesCannotBePublished(string fault)
    {
        var json = fault switch
        {
            "scope" => Valid.Replace("\"provider\":\"ActivityPubOASIS\"", "\"provider\":\"MongoDBOASIS\""),
            "duplicate-provider" => "{\"ActivityPubOASIS\":[],\"ActivityPubOASIS\":[]}",
            "duplicate-request-field" => Valid.Replace("\"take\":3", "\"take\":3,\"Take\":4"),
            "private-field" => Valid.Replace("\"take\":3", "\"take\":3,\"actorId\":\"untrusted\""),
            "unknown-example-field" => Valid.Replace("\"id\":\"all\"", "\"id\":\"all\",\"token\":\"not-public\""),
            _ => "{\"ActivityPubOASIS\":[]}"
        };
        var cause = Assert.ThrowsAny<Exception>(() => CqlHostedQueryExamples.FromJson(json));
        Assert.True(cause is ArgumentException or JsonException);
    }

    [Fact]
    public void PublishedExampleListIsNotMutable()
    {
        var examples = CqlHostedQueryExamples.FromJson(Valid).For(ProviderType.ActivityPubOASIS,
            CosmicQueryCapabilities.ForStringMetadataKeyLookup("NativeKey", 128, 40));
        Assert.Throws<NotSupportedException>(() => ((IList<CqlHostedQueryExample>)examples).Clear());
    }

    [Fact]
    public void ShippedPublicExamplesMatchTheActualActivityPubAdapterWithoutNativeIo()
    {
        var examples = CqlHostedQueryExamples.FromJson(System.IO.File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "cql-activitypub-public-examples.json")));
        using var provider = new NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.ActivityPubOASIS("https://example.invalid", "")
            { EnableCqlStatusKeySets = true };
        var rows = examples.For(ProviderType.ActivityPubOASIS, provider.QueryCapabilities);
        Assert.Equal(new[] { "public-posts", "public-subset" }, rows.Select(row => row.Id));
        Assert.Equal(new[] { 3, 2 }, rows.Select(row => row.Request.GetProperty("filters")[0].GetProperty("value").GetArrayLength()));
        Assert.False(provider.IsProviderActivated);
    }
}
