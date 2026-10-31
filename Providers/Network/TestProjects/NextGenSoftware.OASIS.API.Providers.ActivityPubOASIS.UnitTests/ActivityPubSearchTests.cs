using System.Net;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubSearchTests
{
    private sealed class GraphTransport(bool fail):HttpMessageHandler
    {
        public int Pages;public int Contexts;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var path=request.RequestUri!.AbsolutePath;
            if(path=="/api/v2/instance")return Reply(ActivityPubInstanceContractTests.InstanceJson);
            if(path=="/api/v1/statuses/1/context"){
                Contexts++;return fail?Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)):Reply("{\"ancestors\":[],\"descendants\":[]}");
            }
            Assert.Equal("/api/v2/search",path);Pages++;
            return Reply(Pages==1?"{\"statuses\":[{\"id\":\"1\",\"uri\":\"https://remote.invalid/statuses/1\",\"content\":\"oak\",\"created_at\":\"2026-01-02T03:04:05Z\",\"account\":{\"id\":\"42\",\"acct\":\"alice\"},\"in_reply_to_id\":null,\"visibility\":\"public\",\"replies_count\":0}]}":"{\"statuses\":[]}");
        }
        private static Task<HttpResponseMessage> Reply(string body)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body)});
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task SearchChildLoadingUsesExistingNativeGraphReader(bool children)
    {
        var transport=new GraphTransport(false);using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var result=await provider.SearchAsync(Query(),loadChildren:children,recursive:false,maxChildDepth:1);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Single(result.Result.SearchResultHolons);Assert.Equal(children?1:0,transport.Contexts);Assert.Equal(2,transport.Pages);
    }
    [Fact]
    public async Task SearchGraphFailureCannotPublishFlatPartialResult()
    {
        var transport=new GraphTransport(true);using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var result=await provider.SearchAsync(Query(),loadChildren:true);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Equal(HttpStatusCode.ServiceUnavailable,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);Assert.Equal(1,transport.Contexts);
    }
    private sealed class ActivationFailure(IOException cause):HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){Calls++;throw cause;}
    }
    [Fact]
    public async Task ActivationFailureKeepsOriginalCauseWithoutSearchOrRetry()
    {
        var cause=new IOException("owned activation read failure");var transport=new ActivationFailure(cause);
        using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var result=await provider.SearchAsync(Query(),loadChildren:false);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Same(cause,result.Exception);Assert.Equal(1,transport.Calls);
    }
    [Fact]
    public async Task DisposedSearchFailsBeforeAnyTransportCall()
    {
        var transport=new Transport("{\"statuses\":[]}");var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);provider.Dispose();
        var result=await provider.SearchAsync(Query(),loadChildren:false);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.IsType<ObjectDisposedException>(result.Exception);Assert.Equal(0,transport.Calls);
    }
    private sealed class Transport(string body):HttpMessageHandler
    {
        public int Searches;public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;var activation=request.RequestUri!.AbsolutePath=="/api/v2/instance";
            if(!activation){Searches++;Assert.Equal("/api/v2/search?q=oak%20%26%20seed&type=statuses&limit=40&offset=0",request.RequestUri.PathAndQuery);}
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(activation?ActivityPubInstanceContractTests.InstanceJson:body)});
        }
    }
    private static SearchParams Query()=>new(){SearchOnlyForCurrentAvatar=false,SearchGroups=new(){new SearchTextGroup{SearchQuery="oak & seed",SearchHolons=true}}};
    [Theory][InlineData(false)][InlineData(true)]
    public async Task AuthorScopeUsesExistingInstanceScopedAccountProjection(bool matches)
    {
        var transport=new CompoundTransport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var hash=System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("oasis-mastodon-account-v1\nhttps://mastodon.invalid\n42"));
        var author=new Guid(hash.AsSpan(0,16));var query=Query();query.SearchOnlyForCurrentAvatar=true;query.AvatarId=matches?author:Guid.NewGuid();
        var result=await provider.SearchAsync(query,loadChildren:false);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(matches?2:0,result.Result.SearchResultHolons.Count());
        Assert.All(result.Result.SearchResultHolons,value=>Assert.Equal(author,value.CreatedByAvatarId));Assert.Equal(2,transport.Reads);
    }
    private sealed class CompoundTransport:HttpMessageHandler
    {
        public int Reads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            if(request.RequestUri!.AbsolutePath=="/api/v2/instance")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(ActivityPubInstanceContractTests.InstanceJson)});
            Reads++;var ids=request.RequestUri.Query.Contains("offset=40")?Array.Empty<int>():request.RequestUri.Query.Contains("q=other")?new[]{2,3}:new[]{1,2};
            var statuses=ids.Select(id=>"{\"id\":\""+id+"\",\"uri\":\"https://remote.invalid/statuses/"+id+"\",\"content\":\"oak\",\"created_at\":\"2026-01-02T03:04:05Z\",\"account\":{\"id\":\"42\",\"acct\":\"alice\"},\"in_reply_to_id\":null,\"visibility\":\"public\"}");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"statuses\":["+string.Join(",",statuses)+"]}")});
        }
    }
    [Theory][InlineData(false,false)][InlineData(true,false)][InlineData(false,true)][InlineData(true,true)]
    public async Task CompoundTextGroupsCombineCompleteNativeMembership(bool and,bool third)
    {
        var transport=new CompoundTransport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var query=Query();query.SearchGroups.Add(new SearchTextGroup{SearchQuery="other",SearchHolons=true,PreviousSearchGroupOperator=and?NextGenSoftware.OASIS.API.Core.Enums.SearchParamGroupOperator.And:NextGenSoftware.OASIS.API.Core.Enums.SearchParamGroupOperator.Or});
        if(third)query.SearchGroups.Add(new SearchTextGroup{SearchQuery="third",SearchHolons=true,PreviousSearchGroupOperator=and?NextGenSoftware.OASIS.API.Core.Enums.SearchParamGroupOperator.Or:NextGenSoftware.OASIS.API.Core.Enums.SearchParamGroupOperator.And});
        var result=await provider.SearchAsync(query,loadChildren:false);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(third?6:4,transport.Reads);
        var ids=result.Result.SearchResultHolons.Select(value=>value.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.ActivityPubOASIS]).Order().ToArray();
        Assert.Equal(third?new[]{"1","2"}:and?new[]{"2"}:new[]{"1","2","3"},ids);
    }
    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public async Task MetadataUsesSharedAllAnyMatcher(bool any,bool matches)
    {
        var transport=new CompoundTransport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var query=Query();query.MetaKeyValuePairMatchMode=any?NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.Any:NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All;
        query.FilterByMetaData=new(){["MastodonVisibility"]=matches?"public":"private",["MastodonContentHtml"]=any?"missing":"oak"};
        var result=await provider.SearchAsync(query,loadChildren:false);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(matches?2:0,result.Result.SearchResultHolons.Count());Assert.Equal(2,transport.Reads);
    }
    private sealed class Pages(bool repeat,bool fail,bool endless=false):HttpMessageHandler
    {
        public int Reads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            if(request.RequestUri!.AbsolutePath=="/api/v2/instance")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(ActivityPubInstanceContractTests.InstanceJson)});
            Assert.EndsWith("&limit=40&offset="+(Reads*40),request.RequestUri.PathAndQuery);Reads++;
            if(fail&&Reads==2)return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            var id=repeat?"123":Reads.ToString();
            var body=endless||Reads<=2?"{\"statuses\":[{\"id\":\""+id+"\",\"uri\":\"https://remote.invalid/statuses/"+id+"\",\"content\":\"oak\",\"created_at\":\"2026-01-02T03:04:05Z\",\"account\":{\"id\":\"42\",\"acct\":\"alice\"},\"in_reply_to_id\":null,\"visibility\":\"public\"}]}":"{\"statuses\":[]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body)});
        }
    }
    [Fact]
    public async Task EndlessUniquePagesFailAtSafetyBoundWithoutPartialPayload()
    {
        var transport=new Pages(false,false,true);using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var result=await provider.SearchAsync(Query(),loadChildren:false);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.IsType<InvalidDataException>(result.Exception);Assert.Equal(100,transport.Reads);
    }
    [Fact]
    public async Task OversizedPageFailsBeforeStatusProjection()
    {
        var transport=new Transport("{\"statuses\":["+string.Join(",",Enumerable.Repeat("{}",41))+"]}");
        using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var result=await provider.SearchAsync(Query(),loadChildren:false);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Contains("exceeds requested limit",Assert.IsType<InvalidDataException>(result.Exception).Message);Assert.Equal(1,transport.Searches);
    }
    [Theory][InlineData("")][InlineData(" ")]
    public async Task MissingSearchAuthorityCannotStartRemoteSearch(string token)
    {
        var transport=new Transport("{\"statuses\":[]}");using var provider=new ActivityPubOASIS("https://mastodon.invalid",token,transport);
        var result=await provider.SearchAsync(Query(),loadChildren:false);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.IsType<UnauthorizedAccessException>(result.Exception);Assert.Equal(0,transport.Calls);
    }
    [Fact]
    public async Task SearchConsumesShortPagesUntilExplicitEmptyPage()
    {
        var transport=new Pages(false,false);using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var result=await provider.SearchAsync(Query(),loadChildren:false);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(2,result.Result.SearchResultHolons.Count());Assert.Equal(3,transport.Reads);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task FailedOrRepeatedPageCannotPublishPartialSuccess(bool fail)
    {
        var transport=new Pages(!fail,fail);using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var result=await provider.SearchAsync(Query(),loadChildren:false);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Equal(2,transport.Reads);
        if(fail)Assert.Equal(HttpStatusCode.ServiceUnavailable,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);
        else Assert.IsType<InvalidDataException>(result.Exception);
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)]
    public async Task UnsupportedScopeCannotTriggerUnscopedRemoteSearch(int kind)
    {
        var transport=new Transport("{\"statuses\":[]}");using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var query=Query();
        if(kind==0)query.SearchOnlyForCurrentAvatar=true;
        if(kind==1)query.AvatarId=Guid.NewGuid();
        if(kind==2)query.ParentId=Guid.NewGuid();
        if(kind==3)((SearchTextGroup)query.SearchGroups[0]).SearchProviderKeys=true;
        if(kind==4)query.SearchGroups.Add(new SearchGroupBase());
        if(kind==5)((SearchTextGroup)query.SearchGroups[0]).SearchIds=true;
        var result=await provider.SearchAsync(query,loadChildren:false,version:kind==6?1:0);
        Assert.True(result.IsError);Assert.Null(result.Result);
        if(kind==0)Assert.IsType<ArgumentException>(result.Exception);else Assert.IsType<NotSupportedException>(result.Exception);
        Assert.Equal(0,transport.Calls);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task NormalSearchParamsReadsItsTextGroup(bool sync)
    {
        var transport=new Transport("{\"statuses\":[]}");using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var result=sync?provider.Search(Query(),loadChildren:false):await provider.SearchAsync(Query(),loadChildren:false);
        Assert.False(result.IsError,result.Exception?.ToString()??result.Message);Assert.NotNull(result.Result);Assert.Empty(result.Result.SearchResultHolons);Assert.Equal(1,transport.Searches);
    }
    [Theory][InlineData("{}")][InlineData("{\"statuses\":null}")][InlineData("{\"statuses\":{}}")]
    public async Task MissingStatusCollectionCannotBecomeSuccessfulSearch(string body)
    {
        var transport=new Transport(body);using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-test-token",transport);
        var result=await provider.SearchAsync(Query(),loadChildren:false);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.IsType<InvalidDataException>(result.Exception);Assert.Equal(1,transport.Searches);
    }
}
