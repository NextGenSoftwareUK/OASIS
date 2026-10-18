using System.Net;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubAccountReadTests
{
    private const string Account="""{"id":"12345678901234567890","username":"alice","acct":"alice@remote.invalid","url":"https://remote.invalid/@alice","display_name":"Alice","password":"must-ignore","email":"must-ignore@example.invalid"}""";
    private sealed class Transport : HttpMessageHandler
    {
        public string Body=Account;public string? Path;public int Calls;public HttpStatusCode Status=HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;
            if(request.RequestUri!.AbsolutePath=="/api/v2/instance")return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(ActivityPubInstanceContractTests.InstanceJson)});
            Path=request.RequestUri.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(Status){Content=new StringContent(Body,System.Text.Encoding.UTF8,"application/json")});
        }
    }
    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public async Task NativeIdAndHandleMapOneStablePublicProfile(bool native,bool sync)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=native?(sync?provider.LoadAvatarByProviderKey("12345678901234567890"):await provider.LoadAvatarByProviderKeyAsync("12345678901234567890")):
            (sync?provider.LoadAvatarByUsername("alice@remote.invalid"):await provider.LoadAvatarByUsernameAsync("alice@remote.invalid"));
        Assert.False(result.IsError,result.Exception?.ToString());var avatar=result.Result;
        Assert.NotEqual(Guid.Empty,avatar.Id);Assert.Equal("alice@remote.invalid",avatar.Username);
        Assert.Equal("12345678901234567890",avatar.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        Assert.Equal("alice@remote.invalid",avatar.ProviderUsername[ProviderType.ActivityPubOASIS]);
        Assert.True(string.IsNullOrEmpty(avatar.Password));Assert.True(string.IsNullOrEmpty(avatar.Email));
        Assert.Equal(native?"/api/v1/accounts/12345678901234567890":"/api/v1/accounts/lookup?acct=alice%40remote.invalid",transport.Path);
        var repeated=await provider.LoadAvatarByUsernameAsync("alice@remote.invalid");Assert.Equal(avatar.Id,repeated.Result.Id);
    }
    [Theory][InlineData("{}")][InlineData("[]")][InlineData("not-json")][InlineData("{\"id\":\"wrong\",\"username\":\"alice\",\"acct\":\"alice\",\"url\":\"https://remote.invalid/@alice\"}")]
    public async Task InvalidOrDifferentAccountCannotProduceSuccess(string body)
    {
        var transport=new Transport{Body=body};using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadAvatarByProviderKeyAsync("12345678901234567890");Assert.True(result.IsError);Assert.Null(result.Result);Assert.NotNull(result.Exception);
    }
    [Theory][InlineData(" ",0)][InlineData("alice",2)]
    public async Task InvalidSelectorOrHistoryFailsBeforeAnyHttp(string selector,int version)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadAvatarByUsernameAsync(selector,version);Assert.True(result.IsError);Assert.Equal(0,transport.Calls);
    }
    [Fact]
    public async Task MissingAccountPreservesNativeNotFound()
    {
        var transport=new Transport{Status=HttpStatusCode.NotFound};using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadAvatarByUsernameAsync("missing");Assert.True(result.IsError);
        Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
        Assert.Equal(HttpStatusCode.NotFound,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task DetailProjectionIsSeparateAndUsesSameNativeIdentity(bool sync)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var detail=sync?provider.LoadAvatarDetailByUsername("alice@remote.invalid"):await provider.LoadAvatarDetailByUsernameAsync("alice@remote.invalid");
        Assert.False(detail.IsError,detail.Exception?.ToString());Assert.Equal("alice@remote.invalid",detail.Result.Username);
        Assert.True(string.IsNullOrEmpty(detail.Result.Email));
        Assert.Equal("12345678901234567890",detail.Result.ProviderUniqueStorageKey[ProviderType.ActivityPubOASIS]);
        var avatar=await provider.LoadAvatarByUsernameAsync("alice@remote.invalid");Assert.False(avatar.IsError);
        Assert.Equal(avatar.Result.Id,detail.Result.Id);Assert.NotSame(avatar.Result.MetaData,detail.Result.MetaData);
        detail.Result.MetaData["MastodonDisplayName"]="changed";Assert.Equal("Alice",avatar.Result.MetaData["MastodonDisplayName"]);
    }
    [Theory][InlineData(" ",0)][InlineData("alice",1)]
    public async Task DetailInvalidInputDoesNotSendHttp(string selector,int version)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadAvatarDetailByUsernameAsync(selector,version);Assert.True(result.IsError);Assert.Equal(0,transport.Calls);
    }
    [Fact]
    public async Task MalformedDetailDoesNotProduceFabricatedIdentity()
    {
        var transport=new Transport{Body="{}"};using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        var result=await provider.LoadAvatarDetailByUsernameAsync("alice");Assert.True(result.IsError);Assert.Null(result.Result);Assert.IsType<InvalidDataException>(result.Exception);
    }
}
