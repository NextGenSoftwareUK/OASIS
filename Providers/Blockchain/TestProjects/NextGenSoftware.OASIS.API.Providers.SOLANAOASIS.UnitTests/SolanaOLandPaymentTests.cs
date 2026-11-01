using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Newtonsoft.Json.Linq;
using Solnet.Programs;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaOLandPaymentTests
{
    [DataTestMethod]
    [DataRow("valid")][DataRow("failed")][DataRow("missing-memo")][DataRow("duplicate-memo")]
    [DataRow("invalid-utf8")][DataRow("duplicate-transfer")][DataRow("inner-transfer")]
    public async Task ReceiptReadsFinalizedNativeTransfersAndMemoWithoutSubmitting(string mode)
    {
        using var wire = new PaymentWire(mode);
        var signer = new Account();
        IOASISPaymentReceiptProvider provider = new SolanaOASIS("http://127.0.0.1:1",signer.PrivateKey.Key,signer.PublicKey.Key,wire);
        var result=await provider.GetPaymentReceiptAsync(Signature);
        CollectionAssert.AreEqual(new[]{"getGenesisHash","getTransaction"},wire.Methods.ToArray());
        if(mode is "failed" or "missing-memo" or "duplicate-memo" or "invalid-utf8")
        { Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); return; }
        Assert.IsFalse(result.IsError,result.Exception?.ToString());
        Assert.AreEqual("oland:test-order",result.Result.Memo);
        Assert.AreEqual(Genesis,result.Result.ChainId);
        Assert.IsTrue(result.Result.Finalized); Assert.IsTrue(result.Result.Succeeded);
        Assert.AreEqual(mode is "duplicate-transfer" or "inner-transfer" ? 2:1,result.Result.Transfers.Count);
        foreach(var transfer in result.Result.Transfers)
        {
            Assert.AreEqual(wire.Sender.PublicKey.Key,transfer.FromAddress);
            Assert.AreEqual(wire.Recipient.PublicKey.Key,transfer.ToAddress);
            Assert.AreEqual("123456789",transfer.RawAmount); Assert.AreEqual("lamports",transfer.AmountUnit);
        }
    }
    private static readonly string Signature=Encoders.Base58.EncodeData(Enumerable.Repeat((byte)1,64).ToArray());
    private static readonly string Genesis=Encoders.Base58.EncodeData(Enumerable.Repeat((byte)2,32).ToArray());
    private sealed class PaymentWire(string mode):HttpMessageHandler
    {
        public readonly Account Sender=new(),Recipient=new();
        public readonly List<string> Methods=new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            using var document=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var root=document.RootElement; var method=root.GetProperty("method").GetString()!; Methods.Add(method);
            object result;
            if(method=="getGenesisHash")result=Genesis;
            else
            {
                Assert.AreEqual("getTransaction",method);
                Assert.AreEqual("finalized",root.GetProperty("params")[1].GetProperty("commitment").GetString());
                var transfer=new { programIdIndex=2,accounts=new[]{0,1},data=Encoders.Base58.EncodeData(SystemProgram.Transfer(Sender.PublicKey,Recipient.PublicKey,123456789).Data) };
                var memo=new { programIdIndex=3,accounts=Array.Empty<int>(),data=Encoders.Base58.EncodeData(mode=="invalid-utf8"?new byte[]{255}:Encoding.UTF8.GetBytes("oland:test-order")) };
                var instructions=new List<object>{transfer};
                if(mode!="missing-memo")instructions.Add(memo);
                if(mode=="duplicate-memo")instructions.Add(memo);
                if(mode=="duplicate-transfer")instructions.Add(transfer);
                result=new { slot=42,blockTime=DateTimeOffset.UtcNow.ToUnixTimeSeconds(),version="legacy",
                    transaction=new { signatures=new[]{Signature},message=new {
                        accountKeys=new[]{Sender.PublicKey.Key,Recipient.PublicKey.Key,SystemProgram.ProgramIdKey.Key,"MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr"},
                        header=new { numRequiredSignatures=1,numReadonlySignedAccounts=0,numReadonlyUnsignedAccounts=2 }, instructions } },
                    meta=new { fee=5000,err=mode=="failed"?(object)new { InstructionError=new object[]{0,"Custom"} }:null,
                        innerInstructions=mode=="inner-transfer"?new[]{new { index=0,instructions=new[]{transfer} }}:null } };
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content=new StringContent(JObject.FromObject(new {
                jsonrpc="2.0",id=root.GetProperty("id").GetInt32(),result }).ToString(),Encoding.UTF8,"application/json") };
        }
    }
}
