using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidRedirectTests
{
    [Theory][InlineData(302)][InlineData(307)][InlineData(308)]
    public async Task DefaultHttpTransportDoesNotFollowResourceRedirects(int status)
    {
        using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        var port=((IPEndPoint)listener.LocalEndpoint).Port;
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(8));var requests=new List<string>();
        var serving=Task.Run(async()=>{
            try{
                while(true){
                    using var client=await listener.AcceptTcpClientAsync(timeout.Token);using var stream=client.GetStream();
                    var bytes=new List<byte>();var one=new byte[1];
                    while(await stream.ReadAsync(one,timeout.Token)>0){
                        bytes.Add(one[0]);if(bytes.Count>=4&&Encoding.ASCII.GetString(bytes.ToArray()).EndsWith("\r\n\r\n"))break;
                        if(bytes.Count>16384)throw new InvalidDataException("Unbounded request header.");
                    }
                    var line=Encoding.ASCII.GetString(bytes.ToArray()).Split("\r\n")[0];requests.Add(line);
                    var redirect=line.Contains(" / HTTP/");
                    var response=redirect?$"HTTP/1.1 {status} Redirect\r\nLocation: http://127.0.0.1:{port}/redirected\r\nContent-Length: 0\r\nConnection: close\r\n\r\n":
                        "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(response),timeout.Token);
                }
            }catch(OperationCanceledException)when(timeout.IsCancellationRequested){}
        });
        try{
            using var provider=new SOLIDOASIS($"http://127.0.0.1:{port}/","");
            var result=await provider.ActivateProviderAsync().WaitAsync(TimeSpan.FromSeconds(6));
            Assert.True(result.IsError);Assert.False(provider.IsProviderActivated);
            Assert.Equal((HttpStatusCode)status,Assert.IsType<HttpRequestException>(result.Exception).StatusCode);
        }finally{timeout.Cancel();await serving;}
        Assert.Equal("GET / HTTP/1.1",Assert.Single(requests));
    }
}
