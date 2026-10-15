using System.Net;
using System.Text;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

// Owned loopback-only fault injector: forwards the real signed transaction, then withholds its hash.
internal sealed class Web3AcceptedWriteProxy : IAsyncDisposable
{
    private readonly HttpListener listener=new();
    private readonly HttpClient upstream=new() {Timeout=TimeSpan.FromSeconds(10)};
    private readonly Task loop;
    internal ConcurrentQueue<string> AcceptedHashes {get;}=new();
    internal Web3AcceptedWriteProxy()
    {
        listener.Prefixes.Add("http://127.0.0.1:18546/");listener.Start();loop=RunAsync();
    }
    private async Task RunAsync()
    {
        while(listener.IsListening)
        {
            HttpListenerContext context;
            try {context=await listener.GetContextAsync();}
            catch(HttpListenerException) when(!listener.IsListening) {return;}
            catch(ObjectDisposedException) when(!listener.IsListening) {return;}
            try
            {
                using var reader=new StreamReader(context.Request.InputStream,Encoding.UTF8);
                var body=await reader.ReadToEndAsync();var request=JObject.Parse(body);
                using var response=await upstream.PostAsync("http://127.0.0.1:18545/",new StringContent(body,Encoding.UTF8,"application/json"));
                var content=await response.Content.ReadAsByteArrayAsync();
                if((string?)request["method"]=="eth_sendRawTransaction")
                {
                    var result=JObject.Parse(Encoding.UTF8.GetString(content));
                    var hash=(string?)result["result"];
                    if(string.IsNullOrEmpty(hash)) throw new InvalidOperationException("Fault proxy did not receive native transaction acceptance: "+result);
                    AcceptedHashes.Enqueue(hash);
                    context.Response.StatusCode=503;
                    content=Encoding.UTF8.GetBytes("Native transaction accepted; response deliberately withheld by owned test proxy.");
                }
                else context.Response.StatusCode=(int)response.StatusCode;
                context.Response.ContentType="application/json";context.Response.ContentLength64=content.Length;
                await context.Response.OutputStream.WriteAsync(content);
            }
            finally {context.Response.Close();}
        }
    }
    public async ValueTask DisposeAsync()
    {
        listener.Stop();listener.Close();
        try {await loop.WaitAsync(TimeSpan.FromSeconds(15));}
        finally {upstream.Dispose();}
    }
}
