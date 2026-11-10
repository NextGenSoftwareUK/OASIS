using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using Xunit;

namespace SubscriptionLedger.ProtocolTests;

/// <summary>The shared WEB5-WEB10 client forwards the optional WEB4 app account headers on usage/authorize only when both are set.</summary>
[Collection("AppEnv")]
public sealed class AppHeaderForwardingTests : IDisposable
{
    private readonly string _id = Environment.GetEnvironmentVariable("OASIS_APP_ID");
    private readonly string _key = Environment.GetEnvironmentVariable("OASIS_APP_KEY");

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("OASIS_APP_ID", _id);
        Environment.SetEnvironmentVariable("OASIS_APP_KEY", _key);
    }

    private sealed class Capture : HttpMessageHandler
    {
        public HttpRequestMessage Request;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"statusCode\":200,\"allowed\":true}", System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private static async Task<HttpRequestMessage> Authorize(string appId, string appKey)
    {
        var handler = new Capture();
        var client = new Web4SubscriptionAuthorizationClient(new HttpClient(handler), "https://web4.example.com", "WEB6", "service-key", appId, appKey);
        try { await client.AuthorizeUsageAsync("bearer", new UsageAuthorizationRequest()); } catch (Exception) { /* only the outgoing request matters */ }
        return handler.Request;
    }

    [Fact]
    public async Task BothAppValuesAreSentOnUsageAuthorize()
    {
        Environment.SetEnvironmentVariable("OASIS_APP_ID", null); Environment.SetEnvironmentVariable("OASIS_APP_KEY", null);
        var request = await Authorize("demo-app", "secret-key");
        Assert.Equal("demo-app", request.Headers.GetValues("X-OASIS-App-Id").Single());
        Assert.Equal("secret-key", request.Headers.GetValues("X-OASIS-App-Key").Single());
        Assert.Equal("WEB6", request.Headers.GetValues("X-OASIS-Service").Single());
    }

    [Fact]
    public async Task EnvironmentVariablesAreTheFallback()
    {
        Environment.SetEnvironmentVariable("OASIS_APP_ID", "env-app"); Environment.SetEnvironmentVariable("OASIS_APP_KEY", "env-key");
        var request = await Authorize(null, null);
        Assert.Equal("env-app", request.Headers.GetValues("X-OASIS-App-Id").Single());
        Assert.Equal("env-key", request.Headers.GetValues("X-OASIS-App-Key").Single());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("demo-app", null)]
    [InlineData(null, "secret-key")]
    [InlineData("  ", "secret-key")]
    public async Task NothingOrAPartialPairSendsNoAppHeaders(string id, string key)
    {
        Environment.SetEnvironmentVariable("OASIS_APP_ID", null); Environment.SetEnvironmentVariable("OASIS_APP_KEY", null);
        var request = await Authorize(id, key);
        Assert.False(request.Headers.Contains("X-OASIS-App-Id"));
        Assert.False(request.Headers.Contains("X-OASIS-App-Key"));
    }
}
