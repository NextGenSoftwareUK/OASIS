using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;
using System.Text.Json;
using System.Net;
using System.Text;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaNativeFeeTests
{
    [DataTestMethod][DataRow("normal")][DataRow("v1")][DataRow("failed")][DataRow("missingFee")][DataRow("missingTx")][DataRow("rpcError")]
    [DataRow("http202")][DataRow("http206")][DataRow("http500")][DataRow("wrongId")][DataRow("malformedJson")]
    public async Task ActualSdkWireReceiptPreservesFeesAndRejectsMissingEvidence(string mode)
    {
        using var handler=new FeeWireHandler(mode);var account=new Account();
        var provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key,handler);
        var receipt=await provider.GetNativeFeeReceiptAsync(Signature);
        CollectionAssert.AreEqual(new[]{"getGenesisHash","getTransaction"},handler.Methods.ToArray());
        Assert.AreEqual(Signature,handler.RequestedSignature);
        Assert.AreEqual("finalized",handler.TransactionOptions.GetProperty("commitment").GetString());
        Assert.AreEqual("json",handler.TransactionOptions.GetProperty("encoding").GetString());
        Assert.AreEqual(JsonValueKind.Number,handler.TransactionOptions.GetProperty("maxSupportedTransactionVersion").ValueKind);
        Assert.AreEqual(1,handler.TransactionOptions.GetProperty("maxSupportedTransactionVersion").GetInt32());
        if(mode is "normal" or "v1" or "failed"){
            Assert.IsFalse(receipt.IsError,receipt.Exception?.ToString());Assert.IsTrue(receipt.IsLoaded);
            Assert.AreEqual("5000",receipt.Result.GetProperty("netUnits").GetString());
            Assert.AreEqual(mode!="failed",receipt.Result.GetProperty("success").GetBoolean());
        }else{
            Assert.IsTrue(receipt.IsError);Assert.IsFalse(receipt.IsLoaded);
            Assert.AreEqual(JsonValueKind.Undefined,receipt.Result.ValueKind);
        }
    }

    private sealed class FeeWireHandler(string mode):HttpMessageHandler
    {
        public List<string> Methods=new();
        public bool Disposed;
        public string? RequestedSignature;
        public JsonElement TransactionOptions;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            using var parsed=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var method=parsed.RootElement.GetProperty("method").GetString()!;Methods.Add(method);
            object? result;
            if(method=="getGenesisHash")result=Genesis;
            else{
                Assert.AreEqual("getTransaction",method,"Fee observation must never submit or request signing.");
                var parameters=parsed.RootElement.GetProperty("params");RequestedSignature=parameters[0].GetString();
                Assert.AreEqual(2,parameters.GetArrayLength());
                TransactionOptions=parameters[1].Clone();
                var native=Native();
                if(mode=="v1"){
                    native["version"]=1;
                    native["transaction"]!["message"]!["transactionConfig"]=JObject.FromObject(new{priorityFee=999999,computeUnitLimit=200000});
                }
                if(mode=="failed")native["meta"]!["err"]="AccountNotFound";
                if(mode=="missingFee")((JObject)native["meta"]!).Remove("fee");
                result=mode=="missingTx"?null:native;
            }
            var envelope=method=="getTransaction" && mode=="rpcError"
                ? JObject.FromObject(new{jsonrpc="2.0",id=parsed.RootElement.GetProperty("id").GetInt32(),error=new{code=-32005,message="Node unhealthy"}})
                : JObject.FromObject(new{jsonrpc="2.0",id=parsed.RootElement.GetProperty("id").GetInt32(),result});
            if(method=="getTransaction" && mode=="wrongId")envelope["id"]=-999;
            var status=method=="getTransaction" ? mode switch{
                "http202"=>HttpStatusCode.Accepted,"http206"=>HttpStatusCode.PartialContent,
                "http500"=>HttpStatusCode.InternalServerError,_=>HttpStatusCode.OK}:HttpStatusCode.OK;
            return new HttpResponseMessage(status){Content=new StringContent(
                method=="getTransaction" && mode=="malformedJson" ? "{":envelope.ToString(),Encoding.UTF8,"application/json")};
        }
        protected override void Dispose(bool disposing){Disposed=true;base.Dispose(disposing);}
    }

    [TestMethod]
    public async Task RepeatedReadsDoNotDisposeBorrowedTransport()
    {
        using var handler=new FeeWireHandler("v1");var account=new Account();
        var provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key,handler);
        for(var index=0;index<2;index++){
            var result=await provider.GetNativeFeeReceiptAsync(Signature);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());
            Assert.IsFalse(handler.Disposed,"Only the operation's wrapper may be disposed.");
        }
        Assert.AreEqual(4,handler.Methods.Count);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task CancellationReachesTheInFlightSdkTransport(bool responseBody)
    {
        using var handler=new CancelFeeHandler(responseBody);var account=new Account();
        var provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key,handler);
        using var cancellation=new CancellationTokenSource();
        var pending=provider.GetNativeFeeReceiptAsync(Signature,cancellation.Token);
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        var result=await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);
        Assert.IsInstanceOfType<OperationCanceledException>(result.Exception);
        Assert.IsTrue(handler.Cancelled);Assert.AreEqual(1,handler.Calls);
        if(responseBody)Assert.IsTrue(handler.BodyDisposed,"Cancelled SDK response body must be disposed.");
    }

    private sealed class CancelFeeHandler(bool responseBody):HttpMessageHandler
    {
        public readonly TaskCompletionSource<bool> Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Cancelled;public bool BodyDisposed;public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Calls++;
            if(responseBody)return new HttpResponseMessage(HttpStatusCode.OK){Content=new StreamContent(new WaitingBody(this))};
            Started.TrySetResult(true);
            try{await Task.Delay(Timeout.Infinite,cancellationToken);}
            catch(OperationCanceledException){Cancelled=true;throw;}
            throw new AssertFailedException("Cancelled read must not complete or retry.");
        }

        private sealed class WaitingBody(CancelFeeHandler owner):Stream
        {
            public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
            public override long Length=>throw new NotSupportedException();
            public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
            public override int Read(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
            public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken cancellationToken=default)
            {
                owner.Started.TrySetResult(true);
                try{await Task.Delay(Timeout.Infinite,cancellationToken);}
                catch(OperationCanceledException){owner.Cancelled=true;throw;}
                throw new AssertFailedException("Cancelled body must not complete.");
            }
            public override void Flush()=>throw new NotSupportedException();
            public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
            public override void SetLength(long value)=>throw new NotSupportedException();
            public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
            protected override void Dispose(bool disposing){if(disposing)owner.BodyDisposed=true;base.Dispose(disposing);}
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
