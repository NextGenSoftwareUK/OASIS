using System.Net;
using System.Text;
using System.Security.Cryptography;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubSearchSnapshotTests
{
    private sealed class Transport(Action change):HttpMessageHandler
    {
        public int Pages;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            string body;
            if(request.RequestUri!.AbsolutePath=="/api/v2/instance")
            {
                change();body=ActivityPubInstanceContractTests.InstanceJson;
            }
            else
            {
                Assert.Equal("/api/v2/search",request.RequestUri.AbsolutePath);
                Assert.Contains("q=oak&",request.RequestUri.Query);
                Pages++;
                body=Pages==1 ? "{\"statuses\":["+Status("1","42")+","+Status("2","99")+"]}" : "{\"statuses\":[]}";
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body)});
        }
        private static string Status(string id,string account)=>"{\"id\":\""+id+"\",\"uri\":\"https://remote.invalid/statuses/"+id+"\",\"content\":\"oak\",\"created_at\":\"2026-01-02T03:04:05Z\",\"account\":{\"id\":\""+account+"\",\"acct\":\"test\"},\"in_reply_to_id\":null,\"visibility\":\"public\"}";
    }

    [Theory]
    [InlineData(-1,false)] [InlineData(-1,true)]
    [InlineData(0,false)] [InlineData(0,true)]
    [InlineData(1,false)] [InlineData(1,true)]
    [InlineData(2,false)] [InlineData(2,true)]
    [InlineData(3,false)] [InlineData(3,true)]
    [InlineData(4,false)] [InlineData(4,true)]
    public async Task SearchOwnsValidatedScopeAndAcknowledgesCompleteSelection(int scenario,bool synchronous)
    {
        var hash=SHA256.HashData(Encoding.UTF8.GetBytes("oasis-mastodon-account-v1\nhttps://mastodon.invalid\n42"));
        var author=new Guid(hash.AsSpan(0,16));
        var group=new SearchTextGroup{SearchHolons=true,SearchQuery="oak"};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=author,FilterByMetaData=new(){{"MastodonVisibility","public"}},SearchGroups=new(){group}};
        if(scenario==3){query.MetaKeyValuePairMatchMode=MetaKeyValuePairMatchMode.Any;query.FilterByMetaData["missing"]="missing";}
        void Change(){switch(scenario){case 0:query.AvatarId=Guid.NewGuid();break;case 1:group.SearchQuery="other";break;case 2:query.FilterByMetaData["MastodonVisibility"]="private";break;case 3:query.MetaKeyValuePairMatchMode=MetaKeyValuePairMatchMode.All;break;case 4:query.SearchOnlyForCurrentAvatar=false;break;}}
        var transport=new Transport(Change);
        using var provider=new ActivityPubOASIS("https://mastodon.invalid","synthetic-token",transport);
        var result=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.False(result.IsError,result.Exception?.ToString());
        var selected=Assert.Single(result.Result.SearchResultHolons);
        Assert.Equal(author,selected.CreatedByAvatarId);Assert.Equal("1",selected.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Equal(2,transport.Pages);Assert.Equal(1,result.Result.NumberOfResults);Assert.True(result.IsLoaded);
    }
}
