using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests;

public class ActivityPubEndpointSecurityTests
{
    [Theory]
    [InlineData("http://public.invalid")]
    [InlineData("https://name:synthetic@pod.invalid")]
    [InlineData("https://pod.invalid/api/v1")]
    [InlineData("https://pod.invalid/?q=1")]
    [InlineData("https://pod.invalid/#fragment")]
    public void UnsafeOrAmbiguousInstanceRootRejectsBeforeNetwork(string root)
        =>Assert.Throws<ArgumentException>(()=>{using var provider=new ActivityPubOASIS(root,"synthetic-token");});

    [Theory][InlineData(302)][InlineData(307)][InlineData(308)]
    public async Task DefaultTransportDoesNotActivateThroughRedirectedInstance(int status)
    {
        using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        var port=((IPEndPoint)listener.LocalEndpoint).Port;var requests=new List<string>();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var serving=Task.Run(async()=>{
            try{
                while(true){
                    using var connection=await listener.AcceptTcpClientAsync(timeout.Token);using var stream=connection.GetStream();
                    using var reader=new StreamReader(stream,Encoding.ASCII,false,1024,true);
                    var line=await reader.ReadLineAsync(timeout.Token);requests.Add(line!);
                    var headerBytes=0;
                    while(true){var header=await reader.ReadLineAsync(timeout.Token);if(string.IsNullOrEmpty(header))break;headerBytes+=header.Length;if(headerBytes>16384)throw new InvalidDataException("Unbounded request header.");}
                    var body=ActivityPubInstanceContractTests.InstanceJson;
                    var response=line=="GET /api/v2/instance HTTP/1.1"
                        ?$"HTTP/1.1 {status} Redirect\r\nLocation: http://127.0.0.1:{port}/redirected\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"
                        :$"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\nConnection: close\r\n\r\n{body}";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(response),timeout.Token);
                }
            }catch(OperationCanceledException)when(timeout.IsCancellationRequested){}
        });
        try{
            using var provider=new ActivityPubOASIS($"http://127.0.0.1:{port}");
            var result=await provider.ActivateProviderAsync().WaitAsync(TimeSpan.FromSeconds(6));
            Assert.True(result.IsError);Assert.False(provider.IsProviderActivated);
            Assert.Equal((HttpStatusCode)status,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);
        }finally{timeout.Cancel();await serving;}
        Assert.Equal("GET /api/v2/instance HTTP/1.1",Assert.Single(requests));
    }
}
