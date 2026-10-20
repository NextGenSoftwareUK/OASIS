using System.Net;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidLookupTests
{
    [Theory]
    [InlineData(false,false,false)][InlineData(false,false,true)][InlineData(false,true,false)][InlineData(false,true,true)]
    [InlineData(true,false,false)][InlineData(true,false,true)][InlineData(true,true,false)][InlineData(true,true,true)]
    public async Task ProfileSelectorsIgnoreDeletedIdentityOwners(bool detail,bool email,bool synchronous)
    {
        var transport=new Transport();using var codec=new SolidPrivateRecordCodec(Root,Key());var deleted=Guid.NewGuid();var live=Guid.NewGuid();
        foreach(var id in new[]{deleted,live}){
            var when=id==deleted?DateTime.UtcNow:DateTime.MinValue;
            if(detail)transport.Records[codec.Resource<IAvatarDetail>(id).AbsoluteUri]=codec.Encode<IAvatarDetail>(new AvatarDetail{Id=id,Username="owned",Email="owned@example.invalid",DeletedDate=when});
            else transport.Records[codec.Resource<IAvatar>(id).AbsoluteUri]=codec.Encode<IAvatar>(new Avatar{Id=id,Username="owned",Email="owned@example.invalid",DeletedDate=when});
        }
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        if(detail){var result=email?(synchronous?provider.LoadAvatarDetailByEmail("owned@example.invalid"):await provider.LoadAvatarDetailByEmailAsync("owned@example.invalid")):(synchronous?provider.LoadAvatarDetailByUsername("owned"):await provider.LoadAvatarDetailByUsernameAsync("owned"));Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(live,result.Result.Id);}
        else{var result=email?(synchronous?provider.LoadAvatarByEmail("owned@example.invalid"):await provider.LoadAvatarByEmailAsync("owned@example.invalid")):(synchronous?provider.LoadAvatarByUsername("owned"):await provider.LoadAvatarByUsernameAsync("owned"));Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(live,result.Result.Id);}
    }
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    private sealed class Transport : HttpMessageHandler
    {
        public int Requests;public readonly Dictionary<string,string> Records=new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Requests++;var uri=request.RequestUri!;
            Assert.DoesNotContain("by-",uri.AbsoluteUri);Assert.DoesNotContain("private-user",uri.AbsoluteUri.ToLowerInvariant());
            Assert.DoesNotContain("target%40",uri.AbsoluteUri.ToLowerInvariant());Assert.Empty(uri.Query);
            var body=uri.AbsoluteUri==Root.AbsoluteUri?"":uri.AbsolutePath.EndsWith('/')
                ?"@prefix ldp: <http://www.w3.org/ns/ldp#> . <> a ldp:BasicContainer"+(Records.Count==0?" .":"; ldp:contains "+string.Join(",",Records.Keys.Select(key=>"<"+key+">"))+" .")
                :Records[uri.AbsoluteUri];
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body,System.Text.Encoding.UTF8,"text/turtle")});
        }
    }
    [Theory]
    [InlineData(false,false,false)][InlineData(false,false,true)][InlineData(false,true,false)][InlineData(false,true,true)]
    [InlineData(true,false,false)][InlineData(true,false,true)][InlineData(true,true,false)][InlineData(true,true,true)]
    public async Task PrivateIdentityLookupUsesDecryptedNativeRecordsNotPublicSearchUrls(bool detail,bool email,bool synchronous)
    {
        var transport=new Transport();var id=Guid.NewGuid();
        using(var codec=new SolidPrivateRecordCodec(Root,Key())){
            if(detail)transport.Records[new Uri(Root,$"oasis/avatar-details/{id}.ttl").AbsoluteUri]=codec.Encode<IAvatarDetail>(new AvatarDetail{Id=id,Username=" Private-User ",Email=" Target@example.invalid "});
            else transport.Records[new Uri(Root,$"oasis/avatars/{id}.ttl").AbsoluteUri]=codec.Encode<IAvatar>(new Avatar{Id=id,Username=" Private-User ",Email=" Target@example.invalid "});
        }
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var query=email?" target@EXAMPLE.invalid ":" PRIVATE-USER ";
        if(detail){
            var result=email?(synchronous?provider.LoadAvatarDetailByEmail(query):await provider.LoadAvatarDetailByEmailAsync(query))
                :(synchronous?provider.LoadAvatarDetailByUsername(query):await provider.LoadAvatarDetailByUsernameAsync(query));
            Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(id,result.Result.Id);
        }else{
            var result=email?(synchronous?provider.LoadAvatarByEmail(query):await provider.LoadAvatarByEmailAsync(query))
                :(synchronous?provider.LoadAvatarByUsername(query):await provider.LoadAvatarByUsernameAsync(query));
            Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(id,result.Result.Id);
        }
        Assert.Equal(3,transport.Requests);
    }
    [Fact]
    public async Task DuplicateIdentityFailsRatherThanReturningAnArbitraryAvatar()
    {
        var transport=new Transport();using(var codec=new SolidPrivateRecordCodec(Root,Key()))foreach(var id in new[]{Guid.NewGuid(),Guid.NewGuid()})
            transport.Records[new Uri(Root,$"oasis/avatars/{id}.ttl").AbsoluteUri]=codec.Encode<IAvatar>(new Avatar{Id=id,Username="duplicate"});
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadAvatarByUsernameAsync("duplicate");Assert.True(result.IsError);Assert.Null(result.Result);Assert.IsType<NextGenSoftware.OASIS.Common.StorageIdentityAmbiguousException>(result.Exception);Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);
    }
    [Fact]
    public async Task MissingIdentityReturnsCanonicalRecordNotFoundClassification()
    {
        var transport=new Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadAvatarByEmailAsync("missing@example.invalid");Assert.True(result.IsError);Assert.Null(result.Result);
        Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task GuidProviderKeyLoadsCanonicalResourceWithoutByKeyRoute(bool holon)
    {
        var id=Guid.NewGuid();var transport=new Transport();using(var codec=new SolidPrivateRecordCodec(Root,Key())){
            transport.Records[new Uri(Root,$"oasis/{(holon?"holons":"avatars")}/{id}.ttl").AbsoluteUri]=holon
                ?codec.Encode<IHolon>(new Holon{Id=id,Name="by key"}):codec.Encode<IAvatar>(new Avatar{Id=id,Username="by key"});
        }
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        if(holon){var result=await provider.LoadHolonAsync(id.ToString("N"),loadChildren:false);Assert.False(result.IsError,result.Message);Assert.Equal(id,result.Result.Id);}
        else {var result=await provider.LoadAvatarByProviderKeyAsync(id.ToString("N"));Assert.False(result.IsError,result.Message);Assert.Equal(id,result.Result.Id);}
        Assert.Equal(2,transport.Requests);
    }
    [Fact]
    public async Task BlankLookupFailsBeforeAnyAdditionalHttpRequest()
    {
        var transport=new Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadAvatarByEmailAsync(" ");Assert.True(result.IsError);Assert.Equal(1,transport.Requests);
    }
}
