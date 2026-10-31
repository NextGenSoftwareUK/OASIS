using System.Net;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubHolonStorageBoundaryTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(
                request.RequestUri!.AbsolutePath=="/api/v2/instance"?ActivityPubInstanceContractTests.InstanceJson:"{}")});
        }
    }
    [Theory][InlineData(0,false)][InlineData(0,true)][InlineData(1,false)][InlineData(1,true)][InlineData(2,false)][InlineData(2,true)]
    public async Task GenericRecordSaveBatchAndImportCannotPublishPrivateMetadata(int operation,bool sync)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("https://mastodon.invalid","controlled-token",transport);
        IHolon holon=new Holon{Id=Guid.NewGuid(),Name="private-canary",MetaData=new(){["private-key"]="private-canary"}};
        if(operation==0){
            var result=sync?provider.SaveHolon(holon):await provider.SaveHolonAsync(holon);
            Assert.True(result.IsError);Assert.Null(result.Result);Assert.IsType<NotSupportedException>(result.Exception);
        }else if(operation==1){
            var result=sync?provider.SaveHolons(new[]{holon}):await provider.SaveHolonsAsync(new[]{holon});
            Assert.True(result.IsError);Assert.Null(result.Result);Assert.IsType<NotSupportedException>(result.Exception);
        }else{
            var result=sync?provider.Import(new[]{holon}):await provider.ImportAsync(new[]{holon});
            Assert.True(result.IsError);Assert.False(result.Result);Assert.IsType<NotSupportedException>(result.Exception);
        }
        Assert.Equal(0,transport.Calls);Assert.False(provider.IsProviderActivated);Assert.Equal("private-canary",holon.MetaData["private-key"]);
    }
}
