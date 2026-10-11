using System.Net;
using System.Text;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronGridHistoryTests
{
    [Theory]
    [InlineData("{\"success\":false,\"data\":[],\"meta\":{}}")]
    [InlineData("{\"success\":true,\"data\":[{\"block_timestamp\":124}],\"meta\":{}}")]
    [InlineData("{\"success\":true,\"data\":[],\"meta\":{\"fingerprint\":\"stuck\"}}")]
    public async Task FailedOutOfBoundsOrNonAdvancingPageCannotBecomeSuccessfulEmptyHistory(string page)
    {
        var handler = new Handler("{\"blockID\":\"" + new string('a', 64) + "\"}", page);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new TronGridTokenHistoryClient(http, new string('a', 64)).ReadAsync("account", 123));
    }

    private sealed class Handler(params string[] responses) : HttpMessageHandler
    {
        public List<string> Requests = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Requests.Add(request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(responses[Requests.Count - 1], Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public async Task HistoryPreservesFixedBoundsAndFollowsFingerprints()
    {
        var handler = new Handler("{\"blockID\":\"" + new string('a', 64) + "\"}",
            "{\"success\":true,\"data\":[{\"block_timestamp\":1,\"value\":\"9007199254740993\"}],\"meta\":{\"fingerprint\":\"next/value\"}}",
            "{\"success\":true,\"data\":[{\"block_timestamp\":2}],\"meta\":{}}");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var records = await new TronGridTokenHistoryClient(http, new string('a', 64)).ReadAsync("account", 123);
        Assert.Equal(2, records.Count);
        Assert.Equal("9007199254740993", records[0].GetProperty("value").GetString());
        Assert.All(handler.Requests.Skip(1), url => Assert.Contains("max_timestamp=123", url));
        Assert.Contains("fingerprint=next%2Fvalue", handler.Requests[2]);
    }

    [Fact]
    public async Task WrongIndexerNetworkFailsBeforeReadingHistory()
    {
        var handler = new Handler("{\"blockID\":\"" + new string('b', 64) + "\"}");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new TronGridTokenHistoryClient(http, new string('a', 64)).ReadAsync("account", 123));
        Assert.Single(handler.Requests);
    }
}
