using System.Net;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubEmailBoundaryTests
{
    private sealed class Transport:HttpMessageHandler
    {
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Requests++;
            var body=request.RequestUri!.AbsolutePath=="/api/v2/instance"?ActivityPubInstanceContractTests.InstanceJson:"[]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body)});
        }
    }
    public static IEnumerable<object[]> Cases(){foreach(var kind in Enumerable.Range(0,3))foreach(var sync in new[]{false,true})foreach(var version in new[]{0,1})yield return new object[]{kind,sync,version};}
    [Theory][MemberData(nameof(Cases))]
    public async Task PrivateEmailCannotBecomePublicAccountSearch(int kind,bool sync,int version)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","",transport);
        const string email="private-canary@example.invalid";
        Exception? cause;bool failed;
        if(kind==0){var result=sync?provider.LoadAvatarByEmail(email,version):await provider.LoadAvatarByEmailAsync(email,version);cause=result.Exception;failed=result.IsError;Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);}
        else if(kind==1){var result=sync?provider.LoadAvatarDetailByEmail(email,version):await provider.LoadAvatarDetailByEmailAsync(email,version);cause=result.Exception;failed=result.IsError;Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);}
        else{var result=sync?provider.ExportAllDataForAvatarByEmail(email,version):await provider.ExportAllDataForAvatarByEmailAsync(email,version);cause=result.Exception;failed=result.IsError;Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);}
        Assert.True(failed);Assert.IsType<NotSupportedException>(cause);Assert.Equal(0,transport.Requests);
        Assert.False(provider.IsProviderActivated);Assert.DoesNotContain(email,cause!.Message);
    }
}
