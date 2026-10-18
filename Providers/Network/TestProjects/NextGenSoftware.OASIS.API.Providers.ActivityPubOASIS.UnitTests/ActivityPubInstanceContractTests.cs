using System.Net;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubInstanceContractTests
{
    internal const string InstanceJson="""{"domain":"mastodon.invalid","title":"Test server","version":"4.5.0","configuration":{}}""";
    private sealed class Transport(string body,HttpStatusCode status=HttpStatusCode.OK) : HttpMessageHandler
    {
        public int Calls;public string? Path;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;Path=request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(status){Content=new StringContent(body,System.Text.Encoding.UTF8,"application/json")});
        }
    }
    [Theory][InlineData("{}")][InlineData("[]")][InlineData("not-json")]
    [InlineData("{\"domain\":\"mastodon.invalid\",\"title\":\"test\",\"version\":\"3.5.0\",\"configuration\":{}}")]
    public async Task InvalidOrUnsupportedInstanceCannotActivate(string body)
    {
        var transport=new Transport(body);using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.ActivateProviderAsync();Assert.True(result.IsError);Assert.False(provider.IsProviderActivated);
        Assert.NotNull(result.Exception);Assert.Equal(1,transport.Calls);
    }
    [Fact]
    public async Task SupportedInstanceUsesOnlyV2Endpoint()
    {
        var transport=new Transport(InstanceJson);using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.ActivateProviderAsync();Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.Result);Assert.True(provider.IsProviderActivated);Assert.Equal(1,transport.Calls);Assert.Equal("/api/v2/instance",transport.Path);
    }
    [Fact]
    public async Task HttpFailureRetainsStatusWithoutLegacyFallback()
    {
        var transport=new Transport("",HttpStatusCode.NotFound);using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.ActivateProviderAsync();Assert.True(result.IsError);
        Assert.Equal(HttpStatusCode.NotFound,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);Assert.Equal(1,transport.Calls);
    }
}
