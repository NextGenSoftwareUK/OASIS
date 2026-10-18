using System.Net;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubAvatarStorageBoundaryTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{}")});
        }
    }

    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public async Task GenericAvatarSaveCannotRegisterAnAccountOrPublishPrivateRecords(bool detail,bool synchronous)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("http://127.0.0.1:1","controlled-token",transport);
        if(detail){
            var record=new AvatarDetail{Id=Guid.NewGuid(),Email="private-test@example.invalid",Username="private-test"};
            var result=synchronous?provider.SaveAvatarDetail(record):await provider.SaveAvatarDetailAsync(record);
            Assert.True(result.IsError);Assert.Null(result.Result);Assert.IsType<NotSupportedException>(result.Exception);
        }else{
            var record=new Avatar{Id=Guid.NewGuid(),Email="private-test@example.invalid",Username="private-test",Password="controlled-private-canary"};
            var result=synchronous?provider.SaveAvatar(record):await provider.SaveAvatarAsync(record);
            Assert.True(result.IsError);Assert.Null(result.Result);Assert.IsType<NotSupportedException>(result.Exception);
            Assert.Equal("controlled-private-canary",record.Password);
        }
        Assert.Equal(0,transport.Requests);Assert.False(provider.IsProviderActivated);
    }
}
