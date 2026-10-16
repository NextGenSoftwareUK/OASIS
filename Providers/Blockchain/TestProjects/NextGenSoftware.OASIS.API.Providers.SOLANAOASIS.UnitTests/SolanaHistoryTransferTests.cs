using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaHistoryTransferTests
{
    [DataTestMethod]
    [DataRow("valid")] [DataRow("failed")] [DataRow("multiple")]
    [DataRow("missing")] [DataRow("wrong-slot")] [DataRow("wrong-signature")]
    [DataRow("wrong-status")] [DataRow("truncated")] [DataRow("bad-index")]
    [DataRow("inner")] [DataRow("bad-parent")] [DataRow("duplicate-parent")] [DataRow("null-inner")]
    [DataRow("seeded")] [DataRow("seed-length")] [DataRow("seed-truncated")] [DataRow("seed-extra")] [DataRow("seed-utf8")]
    public async Task HistoryReadsExactSdkTransferDataAndRejectsUnverifiableDetails(string mode)
    {
        var rpc=DispatchProxy.Create<IRpcClient,TransferRpc>();var proxy=(TransferRpc)(object)rpc;proxy.Mode=mode;
        var operatorAccount=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",operatorAccount.PrivateKey.Key,operatorAccount.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);
        var result=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress=proxy.Sender.PublicKey.Key});
        if(mode is "valid" or "failed" or "multiple" or "inner" or "seeded")
        {
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(mode is "multiple" or "inner" ? 2:1,result.Result.Count);
            var row=result.Result[0];Assert.AreEqual("1234567890123456789",row.RawAmount);Assert.AreEqual("lamports",row.AmountUnit);
            Assert.AreEqual(proxy.Sender.PublicKey.Key,row.FromWalletAddress);Assert.AreEqual(proxy.Recipient.PublicKey.Key,row.ToWalletAddress);
            Assert.AreEqual(mode=="failed" ? "failed":"succeeded",row.NativeExecutionResult);
            if(mode is "multiple" or "inner") Assert.AreEqual("1",result.Result[1].RawAmount);
            if(mode=="inner") StringAssert.Contains(result.Result[1].Description,"inner instruction 0:0");
            if(mode=="seeded") Assert.AreEqual("SystemProgram.TransferWithSeed",row.NativeOperation);
        }
        else {Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));}
        Assert.AreEqual(2,proxy.Calls);
    }

    public class TransferRpc : SolanaNativeTokenTests.TokenRpc
    {
        public string Mode="valid";public Account Sender=new(),Recipient=new();
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            Calls++;var error=Mode=="failed" ? new{Type="AccountNotFound"}:null;
            if(method!.Name=="GetSignaturesForAddressAsync") return Respond(method,Newtonsoft.Json.JsonConvert.SerializeObject(new[]{new{Signature="owned-signature",Slot=10,Error=error}}));
            if(method.Name!="GetTransactionAsync") throw new InvalidOperationException(method.Name);
            if(Mode=="missing") return Respond(method,"null");
            var data=SystemProgram.Transfer(Sender.PublicKey,Recipient.PublicKey,1234567890123456789UL).Data;
            var seeded=Mode=="seeded" || Mode.StartsWith("seed-");var baseAccount=new Account();
            if(seeded)
            {
                data=SystemProgram.TransferWithSeed(Sender.PublicKey,baseAccount.PublicKey,"oak",SystemProgram.ProgramIdKey,Recipient.PublicKey,1234567890123456789UL).Data;
                if(Mode=="seed-length") System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(12),ulong.MaxValue);
                if(Mode=="seed-truncated") data=data[..^1];
                if(Mode=="seed-extra") data=data.Concat(new byte[]{0}).ToArray();
                if(Mode=="seed-utf8") data[20]=0xff;
            }
            if(Mode=="truncated") data=data[..^1];
            var instructions=new List<object>{new{ProgramIdIndex=2,Accounts=seeded ? new[]{1,3,0}:new[]{Mode=="bad-index" ? 7:1,0},Data=Encoders.Base58.EncodeData(data)}};
            if(Mode=="multiple") instructions.Add(new{ProgramIdIndex=2,Accounts=new[]{1,0},Data=Encoders.Base58.EncodeData(SystemProgram.Transfer(Sender.PublicKey,Recipient.PublicKey,1).Data)});
            object[]? inner=null;
            if(Mode is "inner" or "bad-parent" or "duplicate-parent" or "null-inner")
            {
                var group=new{Index=Mode=="bad-parent" ? 5:0,Instructions=Mode=="null-inner" ? null:new[]{new{ProgramIdIndex=2,Accounts=new[]{1,0},Data=Encoders.Base58.EncodeData(SystemProgram.Transfer(Sender.PublicKey,Recipient.PublicKey,1).Data)}}};
                inner=Mode=="duplicate-parent" ? new object[]{group,group}:new object[]{group};
            }
            // Recipient precedes sender: account-list order must not determine the direction.
            return Respond(method,Newtonsoft.Json.JsonConvert.SerializeObject(new{Slot=Mode=="wrong-slot" ? 11:10,
                Transaction=new{Signatures=new[]{Mode=="wrong-signature" ? "other":"owned-signature"},
                    Message=new{AccountKeys=new[]{Recipient.PublicKey.Key,Sender.PublicKey.Key,SystemProgram.ProgramIdKey.Key,baseAccount.PublicKey.Key},Instructions=instructions}},
                Meta=new{Error=Mode=="wrong-status" ? new{Type="AccountNotFound"}:error,InnerInstructions=inner}}));
        }
    }
}
