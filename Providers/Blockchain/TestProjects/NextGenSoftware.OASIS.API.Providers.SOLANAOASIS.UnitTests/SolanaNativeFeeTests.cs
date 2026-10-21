using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;
using System.Text.Json;
using System.Net;
using System.Reflection;
using System.Text;
using Solnet.Rpc;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaNativeFeeTests
{
    [DataTestMethod][DataRow("normal")][DataRow("failed")][DataRow("missingFee")][DataRow("missingTx")][DataRow("rpcError")]
    public async Task ActualSdkWireReceiptPreservesFeesAndRejectsMissingEvidence(string mode)
    {
        using var handler=new FeeWireHandler(mode);using var http=new HttpClient(handler){BaseAddress=new Uri("http://127.0.0.1:1")};
        var rpc=ClientFactory.GetClient("http://127.0.0.1:1",null,http,null);var account=new Account();
        var provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);
        var receipt=await provider.GetNativeFeeReceiptAsync(Signature);
        CollectionAssert.AreEqual(new[]{"getGenesisHash","getTransaction"},handler.Methods.ToArray());
        if(mode is "normal" or "failed"){
            Assert.IsFalse(receipt.IsError,receipt.Exception?.ToString());Assert.IsTrue(receipt.IsLoaded);
            Assert.AreEqual("5000",receipt.Result.GetProperty("netUnits").GetString());
            Assert.AreEqual(mode=="normal",receipt.Result.GetProperty("success").GetBoolean());
        }else{
            Assert.IsTrue(receipt.IsError);Assert.IsFalse(receipt.IsLoaded);
            Assert.AreEqual(JsonValueKind.Undefined,receipt.Result.ValueKind);
        }
    }

    private sealed class FeeWireHandler(string mode):HttpMessageHandler
    {
        public List<string> Methods=new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            using var parsed=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var method=parsed.RootElement.GetProperty("method").GetString()!;Methods.Add(method);
            object? result;
            if(method=="getGenesisHash")result=Genesis;
            else{
                Assert.AreEqual("getTransaction",method,"Fee observation must never submit or request signing.");
                var parameters=parsed.RootElement.GetProperty("params");Assert.AreEqual(Signature,parameters[0].GetString());
                if(parameters.GetArrayLength()>1 && parameters[1].TryGetProperty("commitment",out var commitment))
                    Assert.AreEqual("finalized",commitment.GetString());
                var native=Native();
                if(mode=="failed")native["meta"]!["err"]="AccountNotFound";
                if(mode=="missingFee")((JObject)native["meta"]!).Remove("fee");
                result=mode=="missingTx"?null:native;
            }
            var envelope=method=="getTransaction" && mode=="rpcError"
                ? JObject.FromObject(new{jsonrpc="2.0",id=0,error=new{code=-32005,message="Node unhealthy"}})
                : JObject.FromObject(new{jsonrpc="2.0",id=0,result});
            return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(envelope.ToString(),Encoding.UTF8,"application/json")};
        }
    }

    private static readonly string Signature=Encoders.Base58.EncodeData(Enumerable.Repeat((byte)1,64).ToArray());
    private static readonly string Genesis=Encoders.Base58.EncodeData(Enumerable.Repeat((byte)2,32).ToArray());
    private static JObject Native()=>JObject.FromObject(new{slot=42,transaction=new{signatures=new[]{Signature},
        message=new{accountKeys=new[]{Genesis},header=new{numRequiredSignatures=1}}},meta=new{fee=5000,err=(object?)null}});
    private static JsonElement Map(JObject native){using var document=JsonDocument.Parse(native.ToString());return SolanaOASIS.MapSolanaNativeFeeReceipt(Signature,Genesis,document.RootElement);}

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void ExactFeeRemainsChargeableForSuccessfulAndFailedExecution(bool failed)
    {
        var native=Native();if(failed)native["meta"]!["err"]=JObject.Parse("{\"InstructionError\":[0,\"Custom\"]}");
        native["meta"]!["fee"]=ulong.MaxValue;
        var receipt=Map(native);
        Assert.AreEqual(ulong.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture),receipt.GetProperty("netUnits").GetString());
        Assert.AreEqual(!failed,receipt.GetProperty("success").GetBoolean());
        Assert.AreEqual(Genesis,receipt.GetProperty("chainIdentifier").GetString());
        Assert.AreEqual("transaction-fee-only",receipt.GetProperty("coverage").GetString());
    }

    [DataTestMethod][DataRow("missingFee")][DataRow("negativeFee")][DataRow("fractionalFee")]
    [DataRow("missingOutcome")][DataRow("signature")][DataRow("header")][DataRow("payer")][DataRow("slot")]
    public void MissingOrAmbiguousEvidenceCannotBecomeAZeroCostReceipt(string corruption)
    {
        var native=Native();switch(corruption){
            case "missingFee":((JObject)native["meta"]!).Remove("fee");break;
            case "negativeFee":native["meta"]!["fee"]=-1;break;
            case "fractionalFee":native["meta"]!["fee"]=1.5;break;
            case "missingOutcome":((JObject)native["meta"]!).Remove("err");break;
            case "signature":native["transaction"]!["signatures"]![0]=Genesis;break;
            case "header":native["transaction"]!["message"]!["header"]!["numRequiredSignatures"]=0;break;
            case "payer":native["transaction"]!["message"]!["accountKeys"]![0]="invalid";break;
            case "slot":native["slot"]=0;break;
        }
        Exception? failure=null;try{Map(native);}catch(Exception cause){failure=cause;}
        Assert.IsNotNull(failure,"Malformed receipt must fail rather than produce billable evidence.");
    }

    [TestMethod]
    public async Task InvalidSignatureFailsBeforeNetworkAccess()
    {
        var account=new Account();IOASISNativeFeeReceiptProvider provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key);
        var receipt=await provider.GetNativeFeeReceiptAsync("invalid");
        Assert.IsTrue(receipt.IsError);Assert.IsFalse(receipt.IsLoaded);Assert.AreEqual(JsonValueKind.Undefined,receipt.Result.ValueKind);
    }

    [TestMethod]
    public async Task CancelledReceiptReadDoesNotContactTheRpcEndpoint()
    {
        var account=new Account();IOASISNativeFeeReceiptProvider provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var receipt=await provider.GetNativeFeeReceiptAsync(Signature,cancellation.Token);
        Assert.IsTrue(receipt.IsError);Assert.IsFalse(receipt.IsLoaded);
        Assert.IsInstanceOfType<OperationCanceledException>(receipt.Exception);
        Assert.AreEqual(JsonValueKind.Undefined,receipt.Result.ValueKind);
    }
}
