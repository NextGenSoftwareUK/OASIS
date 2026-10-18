using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubLifecycleTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public int Calls; public int Disposals;
        public HttpStatusCode Status=HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Assert.Equal("/api/v2/instance",request.RequestUri!.AbsolutePath);
            Calls++;return Task.FromResult(new HttpResponseMessage(Status){Content=new StringContent(ActivityPubInstanceContractTests.InstanceJson)});
        }
        protected override void Dispose(bool disposing){if(disposing)Disposals++;base.Dispose(disposing);}
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task DeactivationRetainsOneTransportForReactivation(bool synchronous)
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("http://127.0.0.1:1","",transport);
        for(var cycle=0;cycle<2;cycle++){
            var activated=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();
            Assert.False(activated.IsError,activated.Message);Assert.True(provider.IsProviderActivated);
            var deactivated=synchronous?provider.DeActivateProvider():await provider.DeActivateProviderAsync();
            Assert.False(deactivated.IsError);Assert.False(provider.IsProviderActivated);Assert.Equal(0,transport.Disposals);
        }
        Assert.Equal(2,transport.Calls);provider.Dispose();provider.Dispose();Assert.Equal(1,transport.Disposals);
        var disposed=await provider.ActivateProviderAsync();Assert.True(disposed.IsError);Assert.False(provider.IsProviderActivated);
        Assert.IsType<ObjectDisposedException>(disposed.Exception);
    }

    [Fact]
    public async Task ReactivationUsesRetainedTransportAfterDeactivation()
    {
        var transport=new Transport();using var provider=new ActivityPubOASIS("http://127.0.0.1:1","",transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        Assert.False((await provider.DeActivateProviderAsync()).IsError);
        Assert.Equal(0,transport.Disposals);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        Assert.Equal(2,transport.Calls);
    }

    [Fact]
    public async Task RejectedActivationDoesNotPublishActiveStateAndCanBeCorrected()
    {
        var transport=new Transport{Status=HttpStatusCode.Unauthorized};using var provider=new ActivityPubOASIS("http://127.0.0.1:1","",transport);
        Assert.True((await provider.ActivateProviderAsync()).IsError);Assert.False(provider.IsProviderActivated);
        transport.Status=HttpStatusCode.OK;
        Assert.False((await provider.ActivateProviderAsync()).IsError);Assert.True(provider.IsProviderActivated);
    }

    [Fact]
    public async Task RealLoopbackHttpTransportReactivatesWithoutReconstruction()
    {
        using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        var port=((IPEndPoint)listener.LocalEndpoint).Port;
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var serving=Task.Run(async()=>{
            for(var i=0;i<2;i++){
                using var client=await listener.AcceptTcpClientAsync(deadline.Token);
                using var stream=client.GetStream();using var reader=new StreamReader(stream,Encoding.ASCII,false,1024,true);
                var line=await reader.ReadLineAsync(deadline.Token);Assert.Equal("GET /api/v2/instance HTTP/1.1",line);
                while(!string.IsNullOrEmpty(await reader.ReadLineAsync(deadline.Token))){}
                var body=ActivityPubInstanceContractTests.InstanceJson;
                await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\nConnection: close\r\n\r\n{body}"),deadline.Token);
            }
        });
        using var provider=new ActivityPubOASIS($"http://127.0.0.1:{port}");
        try{
            for(var i=0;i<2;i++){
                var result=await provider.ActivateProviderAsync();Assert.False(result.IsError,result.Message);Assert.True(provider.IsProviderActivated);
                Assert.False((await provider.DeActivateProviderAsync()).IsError);Assert.False(provider.IsProviderActivated);
            }
            await serving;
        }finally{deadline.Cancel();listener.Stop();}
    }
}
