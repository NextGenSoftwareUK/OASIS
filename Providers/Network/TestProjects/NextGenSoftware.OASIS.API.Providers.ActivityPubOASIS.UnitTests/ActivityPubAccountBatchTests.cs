using System.Net;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubAccountBatchTests
{
    private sealed class Transport:HttpMessageHandler
    {
        public int Calls;
        public string Body="[]";
        public HttpStatusCode Status=HttpStatusCode.OK;
        public Action? DuringActivation;
        public string? BatchPath;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;
            if(request.RequestUri!.AbsolutePath=="/api/v2/instance"){
                DuringActivation?.Invoke();return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(ActivityPubInstanceContractTests.InstanceJson)});}
            BatchPath=request.RequestUri.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(Status){Content=new StringContent(Body)});
        }
    }

    private static void Rejected<T>(OASISResult<T> result,Transport transport)
    {
        Assert.True(result.IsError);Assert.IsType<NotSupportedException>(result.Exception);
        Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);Assert.Equal(0,transport.Calls);
    }

    [Theory][InlineData(false,false,0)][InlineData(false,true,0)][InlineData(true,false,0)][InlineData(true,true,0)]
    [InlineData(false,false,1)][InlineData(false,true,1)][InlineData(true,false,1)][InlineData(true,true,1)]
    public async Task WholeAccountCollectionsCannotBeInventedFromAnIdBatchEndpoint(bool detail,bool sync,int version)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        if(detail)Rejected(sync?provider.LoadAllAvatarDetails(version):await provider.LoadAllAvatarDetailsAsync(version),transport);
        else Rejected(sync?provider.LoadAllAvatars(version):await provider.LoadAllAvatarsAsync(version),transport);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public void GeographicCollectionsWithoutNativeCoordinatesRejectBeforeActivation(bool avatars)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var network=(IOASISNETProvider)provider;
        if(avatars)Rejected(network.GetAvatarsNearMe(0,0,100),transport);
        else Rejected(network.GetHolonsNearMe(0,0,100,HolonType.All),transport);
        Assert.False(provider.IsProviderActivated);
    }

    private static object Row(string id)=>new{id,username="local"+id,acct="person"+id+"@remote.invalid",url="https://remote.invalid/@person"+id,display_name="Native "+id,email="must-not-publish@example.invalid",password="must-not-publish"};
    private static void Batch<T>(OASISResult<IReadOnlyList<T>> result,string[] expected) where T:IHolonBase
    {
        Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsLoaded);Assert.Equal(expected.Length,result.LoadedCount);
        Assert.Equal(expected,result.Result.Select(row=>row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]));
        Assert.All(result.Result,row=>{Assert.NotEqual(Guid.Empty,row.Id);Assert.Equal("https://mastodon.invalid",row.MetaData["MastodonInstance"]);
            Assert.Equal("https://remote.invalid/@person"+row.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS],row.MetaData["MastodonProfileUrl"]);
            if(row is IAvatar avatar){Assert.True(string.IsNullOrEmpty(avatar.Email));Assert.True(string.IsNullOrEmpty(avatar.Password));}
            if(row is IAvatarDetail detail)Assert.True(string.IsNullOrEmpty(detail.Email));});
    }
    private static void Failed<T>(OASISResult<IReadOnlyList<T>> result)
    {Assert.True(result.IsError);Assert.NotNull(result.Exception);Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);}

    [Theory][InlineData(false,0)][InlineData(true,0)][InlineData(false,1)][InlineData(true,1)][InlineData(false,2)][InlineData(true,2)]
    public async Task AccountAndDetailSubsetsPreserveOrderIdentityAndPublicFieldBoundary(bool sync,int count)
    {
        foreach(var detail in new[]{false,true}){
            var transport=new Transport{Body=JsonSerializer.Serialize(new[]{Row("2"),Row("1")}.Take(count))};
            using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
            var ids=new[]{"1","2"};var expected=count==2?ids:count==1?new[]{"2"}:Array.Empty<string>();
            if(detail)Batch(sync?provider.LoadAccountDetailBatch(ids):await provider.LoadAccountDetailBatchAsync(ids),expected);
            else Batch(sync?provider.LoadAccountBatch(ids):await provider.LoadAccountBatchAsync(ids),expected);
            Assert.Equal("/api/v1/accounts?id%5B%5D=1&id%5B%5D=2",transport.BatchPath);Assert.Equal(2,transport.Calls);
        }
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task AccountInputSnapshotAndBoundedEnumerationPrecedeActivation(bool detail)
    {
        var ids=new[]{"1","2"};var transport=new Transport{Body=JsonSerializer.Serialize(new[]{Row("1")}),DuringActivation=()=>ids[0]="99"};
        using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        if(detail)Batch(await provider.LoadAccountDetailBatchAsync(ids),new[]{"1"});else Batch(await provider.LoadAccountBatchAsync(ids),new[]{"1"});
        Assert.Equal("99",ids[0]);Assert.Equal("/api/v1/accounts?id%5B%5D=1&id%5B%5D=2",transport.BatchPath);
        var enumerated=0;IEnumerable<string> Endless(){while(true){enumerated++;if(enumerated>41)throw new Exception("Unbounded input");yield return enumerated.ToString();}}
        var calls=transport.Calls;
        if(detail)Failed(await provider.LoadAccountDetailBatchAsync(Endless()));else Failed(await provider.LoadAccountBatchAsync(Endless()));
        Assert.Equal(41,enumerated);Assert.Equal(calls,transport.Calls);
    }

    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)]
    public async Task InvalidAccountIdsNeverDispatch(int scenario)
    {
        IEnumerable<string>? ids=scenario switch{0=>null,1=>Array.Empty<string>(),2=>new[]{"1","1"},3=>new[]{""},4=>new[]{" "},5=>new[]{"1/2"},6=>new[]{new string('1',129)},_=>Enumerable.Range(1,41).Select(id=>id.ToString())};
        foreach(var detail in new[]{false,true}){var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
            if(detail)Failed(await provider.LoadAccountDetailBatchAsync(ids!));else Failed(await provider.LoadAccountBatchAsync(ids!));
            Assert.Equal(0,transport.Calls);Assert.False(provider.IsProviderActivated);}
    }

    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)]
    public async Task InvalidAccountResponseNeverPublishesAPartialCollection(int scenario)
    {
        foreach(var detail in new[]{false,true}){
            var transport=new Transport{Body=scenario switch{0=>"{}",1=>"[",2=>JsonSerializer.Serialize(new[]{Row("1"),Row("1")}),3=>JsonSerializer.Serialize(new[]{Row("1"),Row("3")}),4=>JsonSerializer.Serialize(new[]{Row("1"),Row("2"),Row("3")}),5=>"[{\"id\":\"1\"}]",_=>"[]"},Status=scenario==6?HttpStatusCode.PartialContent:scenario==7?HttpStatusCode.Unauthorized:HttpStatusCode.OK};
            using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
            if(detail)Failed(await provider.LoadAccountDetailBatchAsync(new[]{"1","2"}));else Failed(await provider.LoadAccountBatchAsync(new[]{"1","2"}));
            Assert.Equal(2,transport.Calls);
        }
    }
}
