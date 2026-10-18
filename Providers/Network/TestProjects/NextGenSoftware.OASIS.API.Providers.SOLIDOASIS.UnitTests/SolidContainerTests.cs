using System.Net;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidContainerTests
{
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    private sealed class Transport : HttpMessageHandler
    {
        public string Container="";public string Next="";public int MemberReads;
        public readonly Dictionary<string,string> Members=new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var uri=request.RequestUri!;string body;
            if(uri.AbsoluteUri==Root.AbsoluteUri)body="";
            else if(uri.AbsolutePath.EndsWith('/'))body=Container;
            else {MemberReads++;Assert.Equal(Root.Host,uri.Host);Assert.True(Members.TryGetValue(uri.AbsoluteUri,out body!));}
            var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body,System.Text.Encoding.UTF8,"text/turtle")};
            if(Next!=""&&uri.AbsolutePath.EndsWith('/'))response.Headers.TryAddWithoutValidation("Link",Next);
            return Task.FromResult(response);
        }
    }
    [Theory][InlineData(0,false)][InlineData(0,true)][InlineData(1,false)][InlineData(1,true)][InlineData(2,false)][InlineData(2,true)]
    public async Task ContainerMembershipLoadsEveryDistinctEncryptedRecord(int kind,bool synchronous)
    {
        var transport=new Transport();var a=Guid.NewGuid();var b=Guid.NewGuid();
        var category=kind==0?"avatars":kind==1?"avatar-details":"holons";
        transport.Container=$"@prefix ldp: <http://www.w3.org/ns/ldp#> . <> a ldp:BasicContainer; ldp:contains <{a}.ttl>, <{b}.ttl>, <{a}.ttl> .";
        using(var codec=new SolidPrivateRecordCodec(Root,Key()))foreach(var id in new[]{a,b}){
            transport.Members[new Uri(Root,$"oasis/{category}/{id}.ttl").AbsoluteUri]=kind==0
                ?codec.Encode<IAvatar>(new Avatar{Id=id,Username="private record"})
                :kind==1?codec.Encode<IAvatarDetail>(new AvatarDetail{Id=id,Description="private detail"})
                :codec.Encode<IHolon>(new Holon{Id=id,Name="private holon"});
        }
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        IEnumerable<Guid> ids;
        if(kind==0){var result=synchronous?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync();Assert.False(result.IsError,result.Exception?.ToString());ids=result.Result.Select(x=>x.Id);}
        else if(kind==1){var result=synchronous?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync();Assert.False(result.IsError,result.Exception?.ToString());ids=result.Result.Select(x=>x.Id);}
        else {var result=synchronous?provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false);Assert.False(result.IsError,result.Exception?.ToString());ids=result.Result.Select(x=>x.Id);}
        Assert.Equal(new[]{a,b}.OrderBy(x=>x),ids.OrderBy(x=>x));Assert.Equal(2,transport.MemberReads);
    }
    [Theory]
    [InlineData("<https://evil.invalid/private.ttl>")]
    [InlineData("<../avatar-details/09d3be1f-bdd9-4b61-bccf-6377a9f92350.ttl>")]
    [InlineData("\"not a URI\"")]
    public async Task UnsafeMembershipCannotTriggerAnyResourceFetch(string member)
    {
        var transport=new Transport{Container=$"@prefix ldp: <http://www.w3.org/ns/ldp#> . <> a ldp:BasicContainer; ldp:contains {member} ."};
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadAllAvatarsAsync();Assert.True(result.IsError);Assert.Null(result.Result);Assert.Equal(0,transport.MemberReads);
    }
    [Fact]
    public async Task ValidEmptyContainerReturnsAnEmptyCollection()
    {
        var transport=new Transport{Container="@prefix ldp: <http://www.w3.org/ns/ldp#> . <> a ldp:BasicContainer ."};
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadAllAvatarsAsync();Assert.False(result.IsError,result.Message);Assert.Empty(result.Result);Assert.Equal(0,transport.MemberReads);
    }
    [Fact]
    public async Task CorruptMemberDoesNotPublishEarlierRecordsAsCompleteSuccess()
    {
        var a=Guid.Parse("00000000-0000-0000-0000-000000000001");var b=Guid.Parse("00000000-0000-0000-0000-000000000002");
        var transport=new Transport{Container=$"@prefix ldp: <http://www.w3.org/ns/ldp#> . <> a ldp:BasicContainer; ldp:contains <{a}.ttl>, <{b}.ttl> ."};
        using(var codec=new SolidPrivateRecordCodec(Root,Key()))transport.Members[new Uri(Root,$"oasis/avatars/{a}.ttl").AbsoluteUri]=codec.Encode<IAvatar>(new Avatar{Id=a});
        transport.Members[new Uri(Root,$"oasis/avatars/{b}.ttl").AbsoluteUri]="invalid RDF";
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadAllAvatarsAsync();Assert.True(result.IsError);Assert.Null(result.Result);Assert.Equal(2,transport.MemberReads);
    }
    [Fact]
    public async Task PaginationCannotSilentlyReturnFirstPageAsComplete()
    {
        var transport=new Transport{Container="@prefix ldp: <http://www.w3.org/ns/ldp#> . <> a ldp:BasicContainer .",Next="<page2>; rel=\"prev next\""};
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadAllAvatarsAsync();Assert.True(result.IsError);Assert.IsType<NotSupportedException>(result.Exception);Assert.Equal(0,transport.MemberReads);
    }
}
