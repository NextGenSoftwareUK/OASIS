using System.Net;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubAccountDeleteBoundaryTests
{
    public static IEnumerable<object[]> Cases()=>from route in Enumerable.Range(0,4) from sync in new[]{false,true} from soft in new[]{false,true} select new object[]{route,sync,soft};
    private sealed class Transport : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(
                request.Method==HttpMethod.Get?ActivityPubInstanceContractTests.InstanceJson:"{}")});
        }
    }
    [Theory][MemberData(nameof(Cases))]
    public async Task GenericAvatarDeletionCannotDeleteRemoteAccountsOrLookupResources(int route,bool synchronous,bool soft)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","controlled-token",transport);
        var result=route switch{
            0=>synchronous?provider.DeleteAvatar(Guid.NewGuid(),soft):await provider.DeleteAvatarAsync(Guid.NewGuid(),soft),
            1=>synchronous?provider.DeleteAvatar("123",soft):await provider.DeleteAvatarAsync("123",soft),
            2=>synchronous?provider.DeleteAvatarByEmail("private@example.invalid",soft):await provider.DeleteAvatarByEmailAsync("private@example.invalid",soft),
            _=>synchronous?provider.DeleteAvatarByUsername("alice",soft):await provider.DeleteAvatarByUsernameAsync("alice",soft)};
        Assert.True(result.IsError);Assert.False(result.Result);Assert.False(result.IsDeleted);
        Assert.IsType<NotSupportedException>(result.Exception);Assert.Equal(0,transport.Calls);Assert.False(provider.IsProviderActivated);
    }
}
