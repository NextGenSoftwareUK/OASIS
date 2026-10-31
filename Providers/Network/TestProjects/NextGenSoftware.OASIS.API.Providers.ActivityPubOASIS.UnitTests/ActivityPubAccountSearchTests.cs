using System.Net;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubAccountSearchTests
{
    private sealed class Transport(string fault=""):HttpMessageHandler
    {
        public int Accounts,Statuses;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            if(request.RequestUri!.AbsolutePath=="/api/v2/instance")return Reply(ActivityPubInstanceContractTests.InstanceJson);
            Assert.Equal("/api/v2/search",request.RequestUri.AbsolutePath);
            var account=request.RequestUri.Query.Contains("type=accounts");
            if(account){Accounts++;Assert.Contains("resolve=false",request.RequestUri.Query);}else Statuses++;
            if(fault=="failed"&&account&&Accounts==2)return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            if(fault=="missing"&&account)return Reply("{\"statuses\":[]}");
            if(fault=="invalid-account"&&account)return Reply("{\"accounts\":[{\"id\":\"1\",\"username\":\"user1\",\"acct\":\"user1\"}]}");
            if(fault=="oversized"&&account)return Reply("{\"accounts\":["+string.Join(',',Enumerable.Repeat("{}",41))+"]}");
            var exhausted=request.RequestUri.Query.Contains("offset=40")&&fault!="repeat";
            var ids=exhausted?Array.Empty<string>():request.RequestUri.Query.Contains("q=other")?new[]{"2","3"}:new[]{"1","2"};
            if(account){var accounts=ids.Select(id=>new{id,username="user"+id,acct="user"+id+"@remote.invalid",url="https://remote.invalid/@user"+id,display_name="Public "+id,email="must-ignore@example.invalid",password="must-ignore"});return Reply(JsonSerializer.Serialize(new{accounts}));}
            var statuses=ids.Take(1).Select(id=>new{id,uri="https://remote.invalid/statuses/"+id,content="oak",created_at="2026-01-02T03:04:05Z",account=new{id="42",acct="author"},in_reply_to_id=(string?)null,visibility="public"});
            return Reply(JsonSerializer.Serialize(new{statuses}));
        }
        private static Task<HttpResponseMessage> Reply(string body)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body)});
    }
    private static SearchParams Query()=>new(){SearchOnlyForCurrentAvatar=false,SearchGroups=new(){new SearchTextGroup{SearchQuery="oak",SearchAvatars=true}}};
    [Theory][InlineData(false)][InlineData(true)]
    public async Task PublicAccountsUseCanonicalProjectionAndExhaustPages(bool sync)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-search-token",transport);
        var query=Query();var result=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(2,result.LoadedCount);Assert.Equal(2,result.Result.NumberOfResults);Assert.Empty(result.Result.SearchResultHolons);
        Assert.Equal(new[]{"user1@remote.invalid","user2@remote.invalid"},result.Result.SearchResultAvatars.Select(row=>row.Username));
        Assert.All(result.Result.SearchResultAvatars,row=>{Assert.True(string.IsNullOrEmpty(row.Email));Assert.True(string.IsNullOrEmpty(row.Password));Assert.Equal(HolonType.Avatar,row.HolonType);});
        // Identity binding uses the same observing-instance/native-ID contract as point reads.
        var bytes=System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("oasis-mastodon-account-v1\nhttps://mastodon.invalid\n1"));
        Assert.Equal(new Guid(bytes.AsSpan(0,16)),result.Result.SearchResultAvatars.First().Id);
        Assert.Equal(2,transport.Accounts);Assert.Equal(0,transport.Statuses);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task AccountGroupsUseExistingLeftToRightOperators(bool and)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-search-token",transport);var query=Query();
        query.SearchGroups.Add(new SearchTextGroup{SearchQuery="other",SearchAvatars=true,PreviousSearchGroupOperator=and?SearchParamGroupOperator.And:SearchParamGroupOperator.Or});
        var result=await provider.SearchAsync(query,loadChildren:false);Assert.False(result.IsError,result.Exception?.ToString());
        Assert.Equal(and?1:3,result.LoadedCount);Assert.Equal(and?1:3,result.Result.SearchResultAvatars.Count);Assert.Equal(4,transport.Accounts);
    }
    [Theory][InlineData("missing")][InlineData("repeat")][InlineData("failed")][InlineData("invalid-account")][InlineData("oversized")]
    public async Task AccountSearchNeverPublishesPartialOrMalformedPages(string fault)
    {
        var transport=new Transport(fault);using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-search-token",transport);
        var result=await provider.SearchAsync(Query(),loadChildren:false);Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);
        if(fault=="failed")Assert.Equal(HttpStatusCode.ServiceUnavailable,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);else Assert.IsType<InvalidDataException>(result.Exception);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task MixedAndImplicitTargetsPublishBothDomainsWithTruthfulCounts(bool implicitTargets)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-search-token",transport);var query=Query();
        query.SearchGroups[0].SearchAvatars=!implicitTargets;query.SearchGroups[0].SearchHolons=!implicitTargets;
        var result=await provider.SearchAsync(query,loadChildren:false);Assert.False(result.IsError,result.Exception?.ToString());
        Assert.Equal(2,result.Result.SearchResultAvatars.Count);Assert.Single(result.Result.SearchResultHolons);Assert.Equal(3,result.LoadedCount);Assert.Equal(3,result.Result.NumberOfResults);
        Assert.Equal(2,transport.Accounts);Assert.Equal(2,transport.Statuses);
    }
    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public async Task AccountScopeUsesProjectedAccountIdentityAndPublicMetadata(bool owner,bool matches)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-search-token",transport);var query=Query();
        if(owner){var bytes=System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("oasis-mastodon-account-v1\nhttps://mastodon.invalid\n1"));query.SearchOnlyForCurrentAvatar=true;query.AvatarId=matches?new Guid(bytes.AsSpan(0,16)):Guid.NewGuid();}
        else query.FilterByMetaData=new(){{"MastodonLocalUsername",matches?"user1":"not-present"}};
        var result=await provider.SearchAsync(query,loadChildren:false);Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(matches?1:0,result.LoadedCount);Assert.Equal(matches?1:0,result.Result.SearchResultAvatars.Count);
        Assert.Equal(2,transport.Accounts);Assert.Equal(0,transport.Statuses);
    }
    [Theory][InlineData(HolonType.Avatar,2,0)][InlineData(HolonType.Holon,0,1)][InlineData(HolonType.Park,0,0)]
    public async Task ExplicitTypeFiltersDoNotMixPublicDomains(HolonType type,int accounts,int statuses)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","owned-search-token",transport);var query=Query();
        query.SearchGroups[0].SearchHolons=true;query.SearchGroups[0].HolonType=type;
        var result=await provider.SearchAsync(query,loadChildren:false);Assert.False(result.IsError,result.Exception?.ToString());
        Assert.Equal(accounts,result.Result.SearchResultAvatars.Count);Assert.Equal(statuses,result.Result.SearchResultHolons.Count);Assert.Equal(accounts+statuses,result.LoadedCount);
    }
    [Fact]
    public async Task PagedAccountSearchRequiresExplicitSearchAuthorityBeforeAnyHttp()
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.SearchAsync(Query(),loadChildren:false);Assert.True(result.IsError);Assert.IsType<UnauthorizedAccessException>(result.Exception);
        Assert.Equal(0,transport.Accounts);Assert.Equal(0,transport.Statuses);Assert.False(provider.IsProviderActivated);
    }
}
