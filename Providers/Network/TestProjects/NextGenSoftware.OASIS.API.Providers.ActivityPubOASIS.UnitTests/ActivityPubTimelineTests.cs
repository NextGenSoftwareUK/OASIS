using System.Net;
using System.Text.Json;
namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubTimelineTests
{
    private sealed class Transport(string mode="valid"):HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;var path=request.RequestUri!.PathAndQuery;
            if(path=="/api/v2/instance")return Task.FromResult(Reply(ActivityPubInstanceContractTests.InstanceJson));
            Assert.Equal("/api/v1/timelines/public?limit=2&local=true&max_id=200",path);
            if(mode=="http")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            object Row(string id)=>new{id,uri="https://remote.invalid/statuses/"+id,content="<p>untrusted</p>",created_at="2026-01-02T03:04:05Z",account=new{id="42",acct="alice"}};
            return Task.FromResult(Reply(mode=="malformed"?"{}":JsonSerializer.Serialize(mode=="empty"?Array.Empty<object>():new[]{Row(mode=="forward"?"201":"199"),Row(mode=="duplicate"?"199":"198")})));
        }
        private static HttpResponseMessage Reply(string text)=>new(HttpStatusCode.OK){Content=new StringContent(text)};
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task NativePageUsesSharedProjectionAndExclusiveCursor(bool sync)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=sync?provider.LoadPublicTimelinePage("200",2,true):await provider.LoadPublicTimelinePageAsync("200",2,true);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(2,result.LoadedCount);Assert.Equal("198",result.Result.OlderThanId);
        Assert.All(result.Result.Statuses,row=>{Assert.NotEqual(Guid.Empty,row.Id);Assert.Equal("<p>untrusted</p>",row.MetaData["MastodonContentHtml"]);});
    }
    [Theory][InlineData("duplicate")][InlineData("forward")][InlineData("malformed")][InlineData("http")]
    public async Task InvalidNativePageNeverPublishesPartialResult(string mode)
    {
        using var provider=new ActivityPubOASIS("https://mastodon.invalid","",new Transport(mode));
        var result=await provider.LoadPublicTimelinePageAsync("200",2,true);Assert.True(result.IsError);Assert.Null(result.Result);
        if(mode=="http")Assert.Equal(HttpStatusCode.Unauthorized,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);
    }
    [Fact]public async Task EmptyPageHasNoCursor()
    {
        using var provider=new ActivityPubOASIS("https://mastodon.invalid","",new Transport("empty"));
        var result=await provider.LoadPublicTimelinePageAsync("200",2,true);Assert.False(result.IsError);Assert.Empty(result.Result.Statuses);Assert.Null(result.Result.OlderThanId);
    }
    [Theory][InlineData(" ",2)][InlineData("1/2",2)][InlineData(null,0)][InlineData(null,41)]
    public async Task InvalidOptionsFailBeforeActivation(string? cursor,int limit)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        Assert.True((await provider.LoadPublicTimelinePageAsync(cursor,limit,true)).IsError);Assert.Equal(0,transport.Calls);
    }
}
