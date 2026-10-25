using System.Net;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubStatusBatchTests
{
    private sealed class Transport:HttpMessageHandler
    {
        public int Calls;
        public string Body="[]";
        public HttpStatusCode Status=HttpStatusCode.OK;
        public Action? BeforeBatch;
        public Action? DuringActivation;
        public string? BatchPath;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;
            if(request.RequestUri!.AbsolutePath=="/api/v2/instance"){
                DuringActivation?.Invoke();return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(ActivityPubInstanceContractTests.InstanceJson)});}
            BatchPath=request.RequestUri.PathAndQuery;
            BeforeBatch?.Invoke();
            return Task.FromResult(new HttpResponseMessage(Status){Content=new StringContent(Body)});
        }
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task NoCompleteStatusCollectionIsInventedFromTheExplicitIdEndpoint(bool sync)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=sync?provider.LoadAllHolons():await provider.LoadAllHolonsAsync();
        Assert.True(result.IsError);Assert.IsType<NotSupportedException>(result.Exception);Assert.Null(result.Result);
        Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);Assert.Equal(0,transport.Calls);
    }

    private static object Row(string id)=>new{id,uri="https://remote.invalid/statuses/"+id,content="<p>native batch</p>",created_at="2026-01-01T00:00:00Z",account=new{id="42",acct="owner"}};

    [Theory][InlineData(false,0)][InlineData(true,0)][InlineData(false,1)][InlineData(true,1)][InlineData(false,2)][InlineData(true,2)]
    public async Task NativeSubsetIsCanonicalCountedAndReturnedInRequestedOrder(bool sync,int count)
    {
        var transport=new Transport{Body=JsonSerializer.Serialize(new[]{Row("2"),Row("1")}.Take(count))};
        using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=sync?provider.LoadStatusBatch(new[]{"1","2"}):await provider.LoadStatusBatchAsync(new[]{"1","2"});
        Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsLoaded);Assert.Equal(count,result.LoadedCount);
        Assert.Equal(count==2?new[]{"1","2"}:count==1?new[]{"2"}:Array.Empty<string>(),result.Result.Select(row=>row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]));
        Assert.All(result.Result,row=>{Assert.NotEqual(Guid.Empty,row.Id);Assert.NotEqual(Guid.Empty,row.CreatedByAvatarId);Assert.Equal("<p>native batch</p>",row.MetaData["MastodonContentHtml"]);Assert.Empty(row.Children);});
        Assert.Equal("/api/v1/statuses?id%5B%5D=1&id%5B%5D=2",transport.BatchPath);Assert.Equal(2,transport.Calls);
    }

    [Fact]
    public async Task IdSnapshotPrecedesActivationAndEnumerationIsBounded()
    {
        var ids=new[]{"1","2"};var transport=new Transport{Body=JsonSerializer.Serialize(new[]{Row("1")}),DuringActivation=()=>ids[0]="99"};
        using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadStatusBatchAsync(ids);Assert.False(result.IsError,result.Exception?.ToString());
        Assert.Equal("99",ids[0]);Assert.Equal("1",Assert.Single(result.Result).ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Equal("/api/v1/statuses?id%5B%5D=1&id%5B%5D=2",transport.BatchPath);
        var enumerated=0;IEnumerable<string> Endless(){while(true){enumerated++;if(enumerated>41)throw new Exception("Unbounded enumeration");yield return enumerated.ToString();}}
        var calls=transport.Calls;var oversized=await provider.LoadStatusBatchAsync(Endless());
        Assert.IsType<ArgumentOutOfRangeException>(oversized.Exception);Assert.Equal(41,enumerated);Assert.Equal(calls,transport.Calls);
    }

    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)]
    public async Task InvalidInputNeverDispatches(int scenario)
    {
        IEnumerable<string>? ids=scenario switch{0=>null,1=>Array.Empty<string>(),2=>new[]{"1","1"},3=>new[]{""},4=>new[]{" "},5=>new[]{"1/2"},6=>new[]{new string('1',129)},_=>Enumerable.Range(1,41).Select(id=>id.ToString())};
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadStatusBatchAsync(ids!);Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);Assert.Equal(0,transport.Calls);
        Assert.IsAssignableFrom<ArgumentException>(result.Exception);
    }

    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)]
    public async Task BadAcknowledgementDoesNotPublishAValidPrefix(int scenario)
    {
        var transport=new Transport{Body=scenario switch{0=>"{}",1=>"[",2=>JsonSerializer.Serialize(new[]{Row("1"),Row("1")}),3=>JsonSerializer.Serialize(new[]{Row("1"),Row("3")}),4=>JsonSerializer.Serialize(new[]{Row("1"),Row("2"),Row("3")}),5=>"[{\"id\":\"1\"}]",_=>"[]"},Status=scenario==6?HttpStatusCode.PartialContent:scenario==7?HttpStatusCode.Unauthorized:HttpStatusCode.OK};
        using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadStatusBatchAsync(new[]{"1","2"});Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);Assert.Equal(2,transport.Calls);
        if(scenario==7)Assert.Equal(HttpStatusCode.Unauthorized,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);
    }
}
