using System.Net;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubStatusReadTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public int Calls;public string Body="""{"id":"123","uri":"https://remote.invalid/users/alice/statuses/123","content":"<p>Hello</p>","created_at":"2026-01-02T03:04:05Z","account":{"id":"42","acct":"alice"},"in_reply_to_id":"122","visibility":"public","MetaData":{"secret":"ignore"}}""";
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;var activate=request.RequestUri!.AbsolutePath=="/api/v2/instance";
            if(!activate)Assert.Equal("/api/v1/statuses/123",request.RequestUri.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(activate?ActivityPubInstanceContractTests.InstanceJson:Body)});
        }
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task NativeStatusMapsStableIdentityReplyAndExplicitMetadata(bool sync)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=sync?provider.LoadHolon("123"):await provider.LoadHolonAsync("123");
        Assert.False(result.IsError,result.Exception?.ToString());Assert.NotEqual(Guid.Empty,result.Result.Id);Assert.NotEqual(Guid.Empty,result.Result.ParentHolonId);
        Assert.Equal("123",result.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);Assert.Equal("<p>Hello</p>",result.Result.MetaData["MastodonContentHtml"]);
        Assert.False(result.Result.MetaData.ContainsKey("secret"));Assert.Equal(new DateTime(2026,1,2,3,4,5,DateTimeKind.Utc),result.Result.CreatedDate);
        var repeated=await provider.LoadHolonAsync("123");Assert.Equal(result.Result.Id,repeated.Result.Id);
    }
    [Theory][InlineData("{}")][InlineData("[]")][InlineData("{\"id\":\"different\"}")]
    public async Task MalformedOrWrongNativeStatusIsAnError(string body)
    {
        var transport=new Transport{Body=body};using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadHolonAsync("123");Assert.True(result.IsError);Assert.Null(result.Result);Assert.NotNull(result.Exception);
    }
    [Theory][InlineData(" ",0,0,false)][InlineData("123",1,0,false)][InlineData("123",0,-1,false)][InlineData("123",0,0,true)]
    public async Task InvalidOrUnverifiedOptionsCannotSilentlySucceed(string key,int version,int depth,bool children)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadHolonAsync(key,maxChildDepth:depth,version:version,loadChildrenFromProvider:children);
        Assert.True(result.IsError);Assert.Equal(0,transport.Calls);
    }
}
