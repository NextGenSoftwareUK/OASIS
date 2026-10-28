using System.Net;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public sealed class ActivityPubCqlKeySetTests
{
    private static string Status(string id, string visibility = "public") =>
        $$"""{"id":"{{id}}","uri":"https://remote.invalid/statuses/{{id}}","content":"<p>Demo</p>","created_at":"2026-01-02T03:04:05Z","account":{"id":"42","acct":"alice"},"visibility":"{{visibility}}"}""";

    private static CosmicQuery<Holon> Query(params string[] keys) => CosmicQuery<Holon>.Create(
        CosmicQueryScope.Named(ProviderType.ActivityPubOASIS).Result)
        .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, ActivityPubOASIS.CqlStatusKey))).Result;

    private static CosmicQueryPlan<Holon> Plan(ActivityPubOASIS provider, CosmicQuery<Holon> query)
    {
        var result = CosmicQueryPlanner.Plan(query, ProviderType.ActivityPubOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ActivityPubOASIS, provider.QueryCapabilities) });
        Assert.False(result.IsError, result.Message);
        return result.Result;
    }

    [Fact]
    public async Task MembershipUsesOneNativeBatchAndCanonicalProvenance()
    {
        var transport = new Transport { Body = "[" + Status("456") + "," + Status("123") + "]" };
        using var provider = new ActivityPubOASIS("https://mastodon.invalid", "", transport) { EnableCqlStatusKeySets = true };
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query("123", "456", "789")));
        Assert.False(result.IsError, result.Message);
        Assert.Equal(new[] { "123", "456" }, result.Result.Select(row => row.MetaData[ActivityPubOASIS.CqlStatusKey]));
        Assert.All(result.Result, row => { Assert.True(row.IsPublic); Assert.Empty(row.Children); Assert.Equal(row.MetaData[ActivityPubOASIS.CqlStatusKey], row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]); });
        Assert.Equal(1, transport.BatchReads);
        Assert.Equal("?id[]=123&id[]=456&id[]=789", Uri.UnescapeDataString(transport.Query!));
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("foreign")]
    [InlineData("private")]
    [InlineData("malformed")]
    public async Task EntireNativeCollectionMustValidateEvenBeforeTakeZero(string kind)
    {
        var second = kind switch { "duplicate" => Status("123"), "foreign" => Status("999"), "private" => Status("456", "private"), _ => "{}" };
        var transport = new Transport { Body = "[" + Status("123") + "," + second + "]" };
        using var provider = new ActivityPubOASIS("https://mastodon.invalid", "", transport) { EnableCqlStatusKeySets = true };
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query("123", "456").Take(0).Result));
        Assert.True(result.IsError); Assert.Null(result.Result); Assert.False(result.IsLoaded);
        Assert.Equal(1, transport.BatchReads);
    }

    [Fact]
    public async Task AdmissionActivationAndNativeKeyValidationPrecedeIo()
    {
        var transport = new Transport();
        using var provider = new ActivityPubOASIS("https://mastodon.invalid", "", transport) { EnableCqlStatusKeySets = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query("123", "456")));
        Assert.Equal("CQL_PROVIDER_INACTIVE", result.ErrorCode);
        result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query("not-decimal")));
        Assert.Equal("CQL_INVALID_SOURCE_KEY", result.ErrorCode);
        using var pointOnly = new ActivityPubOASIS("https://mastodon.invalid", "", new Transport());
        Assert.True(pointOnly.QueryCapabilities.Admit(Query("123", "456")).IsError);
        Assert.Equal(0, transport.BatchReads);
    }

    [Theory]
    [InlineData(404)] [InlineData(401)] [InlineData(500)]
    public async Task NativeBatchFailureNeverFallsBackToPointReads(int code)
    {
        var transport = new Transport { Code = (HttpStatusCode)code };
        using var provider = new ActivityPubOASIS("https://mastodon.invalid", "", transport) { EnableCqlStatusKeySets = true };
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query("123", "456")));
        Assert.True(result.IsError); Assert.Null(result.Result); Assert.Equal(1, transport.BatchReads);
    }

    [Fact]
    public async Task CompletePageOverBudgetIsRejectedRatherThanTruncated()
    {
        var transport = new Transport { Body = "[" + Status("123") + "," + Status("456") + "]" };
        using var provider = new ActivityPubOASIS("https://mastodon.invalid", "", transport) { EnableCqlStatusKeySets = true };
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query("123", "456")), maximumResults: 1);
        Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode); Assert.Null(result.Result);
    }

    private sealed class Transport : HttpMessageHandler
    {
        public string Body = "[]";
        public HttpStatusCode Code = HttpStatusCode.OK;
        public int BatchReads;
        public string? Query;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("mastodon.invalid", request.RequestUri!.Host);
            if (request.RequestUri.AbsolutePath == "/api/v2/instance")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ActivityPubInstanceContractTests.InstanceJson) });
            Assert.Equal("/api/v1/statuses", request.RequestUri.AbsolutePath);
            BatchReads++; Query = request.RequestUri.Query;
            return Task.FromResult(new HttpResponseMessage(Code) { Content = new StringContent(Body) });
        }
    }
}
