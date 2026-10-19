using System.Net;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubReplyGraphTests
{
    private static object Status(string id,string? parent,int replies)=>new{id,uri="https://remote.invalid/statuses/"+id,content="<p>Native reply</p>",
        created_at="2026-01-02T03:04:05Z",account=new{id="42",acct="alice"},in_reply_to_id=parent,replies_count=replies};
    private sealed class Transport(string mode="valid"):HttpMessageHandler
    {
        public int ContextReads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var path=request.RequestUri!.AbsolutePath;
            if(path=="/api/v2/instance")return Task.FromResult(Reply(ActivityPubInstanceContractTests.InstanceJson));
            if(path=="/api/v1/statuses/123")return Task.FromResult(Reply(JsonSerializer.Serialize(Status("123",null,mode=="truncated"?3:2))));
            Assert.Equal("/api/v1/statuses/123/context",path);ContextReads++;
            if(mode=="http")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            var child=Status("124",mode=="cycle"?"126":"123",1);
            var grand=Status("126",mode=="orphan"?"999":"124",0);
            var values=new List<object>{grand,Status("125","123",0),child};
            if(mode=="duplicate")values.Add(child);
            var body=mode=="malformed"?"{}":JsonSerializer.Serialize(new{ancestors=Array.Empty<object>(),descendants=values});
            var result=Reply(body);if(mode=="refresh")result.Headers.Add("Mastodon-Async-Refresh","https://mastodon.invalid/api/v1/async_refreshes/1");
            return Task.FromResult(result);
        }
        private static HttpResponseMessage Reply(string body)=>new(HttpStatusCode.OK){Content=new StringContent(body,System.Text.Encoding.UTF8,"application/json")};
    }

    [Theory][InlineData(false,true,0,true)][InlineData(true,true,0,true)][InlineData(false,true,1,false)][InlineData(false,false,0,false)]
    public async Task NativeReplyTreeUsesSharedProjectionDepthAndRecursion(bool sync,bool recursive,int depth,bool grandchild)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=sync?provider.LoadHolon("123",recursive:recursive,maxChildDepth:depth,loadChildrenFromProvider:true)
            :await provider.LoadHolonAsync("123",recursive:recursive,maxChildDepth:depth,loadChildrenFromProvider:true);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(1,transport.ContextReads);Assert.Equal(2,result.Result.Children.Count());
        var first=Assert.Single(result.Result.Children.Where(child=>child.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]=="124"));
        Assert.Equal(result.Result.Id,first.ParentHolonId);
        if(grandchild){var nested=Assert.Single(first.Children);Assert.Equal("126",nested.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);Assert.Equal(first.Id,nested.ParentHolonId);}
        else Assert.Empty(first.Children);
    }

    [Theory][InlineData("truncated")][InlineData("orphan")][InlineData("cycle")][InlineData("duplicate")][InlineData("malformed")][InlineData("refresh")][InlineData("http")]
    public async Task IncompleteOrCorruptContextNeverPublishesPartialTree(string mode)
    {
        var transport=new Transport(mode);using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadHolonAsync("123",loadChildrenFromProvider:true);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Equal(1,transport.ContextReads);
        if(mode=="http")Assert.Equal(HttpStatusCode.ServiceUnavailable,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);
    }

    [Fact]
    public async Task DisabledChildrenDoNotReadNativeContext()
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadHolonAsync("123",loadChildren:false,loadChildrenFromProvider:true);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Empty(result.Result.Children);Assert.Equal(0,transport.ContextReads);
    }
}
