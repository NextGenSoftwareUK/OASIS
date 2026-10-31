using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public class PolkadotRpcActivationTests
{
    private sealed class RpcHandler(string mode):HttpMessageHandler
    {
        public int Requests;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Requests++;
            if(request.Method!=HttpMethod.Post)return new(HttpStatusCode.OK){Content=new StringContent("not a JSON RPC response")};
            if(mode=="html")return new(HttpStatusCode.OK){Content=new StringContent("<html>not a node</html>")};
            if(mode=="http-error")return new(HttpStatusCode.BadGateway){Content=new StringContent("owned upstream failure")};
            using var json=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var id=json.RootElement.GetProperty("id").GetInt32();var method=json.RootElement.GetProperty("method").GetString();
            Assert.AreEqual("2.0",json.RootElement.GetProperty("jsonrpc").GetString());
            CollectionAssert.Contains(new[]{"system_chain","chain_getBlockHash"},method);
            object? value=method=="system_chain"?(mode=="wrong-chain"?"Unreviewed Network":"Polkadot")
                :mode=="bad-genesis"?"not-a-hash":"0x"+new string(mode=="hash-mismatch"?'b':'a',64);
            if(mode=="null-result")value=null;
            var envelope=new Dictionary<string,object?>{{"jsonrpc",mode=="rpc-version"?"1.0":"2.0"},{"id",mode=="id-mismatch"?id+100:id},{"result",value}};
            if(mode=="rpc-error"){envelope.Remove("result");envelope["error"]=new{code=-32601,message="owned RPC error"};}
            return new(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(envelope),Encoding.UTF8,"application/json")};
        }
    }

    [DataTestMethod]
    [DataRow("success",false)][DataRow("success",true)][DataRow("wrong-chain",false)][DataRow("wrong-chain",true)]
    [DataRow("rpc-error",false)][DataRow("rpc-error",true)][DataRow("id-mismatch",false)][DataRow("id-mismatch",true)]
    [DataRow("null-result",false)][DataRow("null-result",true)][DataRow("bad-genesis",false)][DataRow("bad-genesis",true)]
    [DataRow("html",false)][DataRow("html",true)][DataRow("hash-mismatch",false)][DataRow("hash-mismatch",true)]
    [DataRow("http-error",false)][DataRow("http-error",true)][DataRow("rpc-version",false)][DataRow("rpc-version",true)]
    public async Task ActivationRequiresValidRpcAndConfiguredNetwork(string mode,bool synchronous)
    {
        var provider=new PolkadotOASIS("https://owned.invalid",mode=="hash-mismatch"?"0x"+new string('a',64):"polkadot");
        using var handler=new RpcHandler(mode);using var client=new HttpClient(handler){BaseAddress=new Uri("https://owned.invalid")};
        var field=typeof(PolkadotOASIS).GetField("_httpClient",BindingFlags.Instance|BindingFlags.NonPublic)!;
        ((HttpClient)field.GetValue(provider)!).Dispose();field.SetValue(provider,client);
        try
        {
            var result=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();
            if(mode=="success")
            {Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.Result);Assert.IsTrue(provider.IsProviderActivated);Assert.AreEqual(2,handler.Requests);}
            else{Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(provider.IsProviderActivated);}
        }
        finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task DeactivationAllowsSameClientToBeReused(bool synchronous)
    {
        var provider=new PolkadotOASIS("https://owned.invalid");using var handler=new RpcHandler("success");
        using var client=new HttpClient(handler){BaseAddress=new Uri("https://owned.invalid")};
        var field=typeof(PolkadotOASIS).GetField("_httpClient",BindingFlags.Instance|BindingFlags.NonPublic)!;
        ((HttpClient)field.GetValue(provider)!).Dispose();field.SetValue(provider,client);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            Assert.IsFalse(provider.IsProviderActivated);
            var result=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(provider.IsProviderActivated);Assert.AreEqual(4,handler.Requests);
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
