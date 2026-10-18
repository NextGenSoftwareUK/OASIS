using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using Solnet.Rpc;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaHistoryWireTests
{
    [DataTestMethod]
    [DataRow("{\"jsonrpc\":\"2.0\",\"id\":0,\"error\":{\"code\":-32005,\"message\":\"Node is unhealthy\"}}")]
    [DataRow("{\"jsonrpc\":\"2.0\",\"id\":0,\"result\":null}")]
    public async Task WireRpcFailureAndMissingResultCannotBecomeEmptySuccess(string response)
    {
        using var handler=new WireHandler(response);using var http=new HttpClient(handler){BaseAddress=new Uri("http://127.0.0.1:1")};
        var owner=new Account();var rpc=ClientFactory.GetClient("http://127.0.0.1:1",null,http,null);
        var provider=new SolanaOASIS("http://127.0.0.1:1",owner.PrivateKey.Key,owner.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);
        var result=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress=owner.PublicKey.Key});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsNotNull(result.Exception);Assert.AreEqual(1,handler.Calls);
    }

    [DataTestMethod]
    [DataRow("null","succeeded")]
    [DataRow("\"AccountNotFound\"","failed")]
    [DataRow("{\"InstructionError\":[0,{\"Custom\":7}]}","failed")]
    public async Task ActualSdkDecodesWireExecutionErrorAndSendsFinalizedRequest(string error,string status)
    {
        var signature=Solnet.Wallet.Utilities.Encoders.Base58.EncodeData(new byte[64]);
        using var handler=new WireHandler("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":[{\"signature\":\""+signature+"\",\"slot\":20,\"err\":"+error+",\"memo\":null,\"blockTime\":1700000000,\"confirmationStatus\":\"finalized\"}]}");
        using var http=new HttpClient(handler){BaseAddress=new Uri("http://127.0.0.1:1")};var owner=new Account();
        var rpc=ClientFactory.GetClient("http://127.0.0.1:1",null,http,null);
        var provider=new SolanaOASIS("http://127.0.0.1:1",owner.PrivateKey.Key,owner.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);
        var result=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress=owner.PublicKey.Key});
        Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);Assert.AreEqual(1,result.Result.Count);
        Assert.AreEqual(signature,result.Result[0].NativeTransactionId);Assert.AreEqual(status,result.Result[0].NativeExecutionResult);
        if(status=="failed") Assert.IsFalse(string.IsNullOrWhiteSpace(result.Result[0].NativeError));
        Assert.AreEqual(new DateTime(2023,11,14,22,13,20,DateTimeKind.Utc),result.Result[0].CreatedDate);
        using var request=JsonDocument.Parse(handler.HistoryRequest!);var root=request.RootElement;
        Assert.AreEqual("getSignaturesForAddress",root.GetProperty("method").GetString());
        var parameters=root.GetProperty("params");Assert.AreEqual(owner.PublicKey.Key,parameters[0].GetString());
        // Solnet6.1.0 deliberately omits default finalized/1000 config (verified against pinned SDK source).
        Assert.AreEqual(1,parameters.GetArrayLength(),handler.HistoryRequest);Assert.AreEqual(2,handler.Calls);
    }

    private sealed class WireHandler(string response) : HttpMessageHandler
    {
        public string? Request,HistoryRequest;public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Calls++;Request=await request.Content!.ReadAsStringAsync(cancellationToken);
            using var parsed=JsonDocument.Parse(Request);
            if(parsed.RootElement.GetProperty("method").GetString()=="getTransaction")
            {
                using var history=JsonDocument.Parse(response);var row=history.RootElement.GetProperty("result")[0];
                var detail=System.Text.Json.JsonSerializer.Serialize(new{jsonrpc="2.0",id=0,result=new{slot=20,
                    transaction=new{signatures=new[]{row.GetProperty("signature").GetString()},message=new{accountKeys=Array.Empty<string>(),instructions=Array.Empty<object>()}},
                    meta=new{err=row.GetProperty("err")}}});
                return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(detail,Encoding.UTF8,"application/json")};
            }
            HistoryRequest=Request;
            return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(response,Encoding.UTF8,"application/json")};
        }
    }
}
