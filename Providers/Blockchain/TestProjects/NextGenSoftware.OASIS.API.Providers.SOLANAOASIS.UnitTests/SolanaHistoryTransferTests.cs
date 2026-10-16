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
    public async Task HistoryReadsExactSdkTransferDataAndRejectsUnverifiableDetails(string mode)
    {
        var rpc=DispatchProxy.Create<IRpcClient,TransferRpc>();var proxy=(TransferRpc)(object)rpc;proxy.Mode=mode;
        var operatorAccount=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",operatorAccount.PrivateKey.Key,operatorAccount.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);
        var result=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress=proxy.Sender.PublicKey.Key});
        if(mode is "valid" or "failed" or "multiple")
        {
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(mode=="multiple" ? 2:1,result.Result.Count);
            var row=result.Result[0];Assert.AreEqual("1234567890123456789",row.RawAmount);Assert.AreEqual("lamports",row.AmountUnit);
            Assert.AreEqual(proxy.Sender.PublicKey.Key,row.FromWalletAddress);Assert.AreEqual(proxy.Recipient.PublicKey.Key,row.ToWalletAddress);
            Assert.AreEqual(mode=="failed" ? "failed":"succeeded",row.NativeExecutionResult);
            if(mode=="multiple") Assert.AreEqual("1",result.Result[1].RawAmount);
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
            if(Mode=="truncated") data=data[..^1];
            var instructions=new List<object>{new{ProgramIdIndex=2,Accounts=new[]{Mode=="bad-index" ? 7:1,0},Data=Encoders.Base58.EncodeData(data)}};
            if(Mode=="multiple") instructions.Add(new{ProgramIdIndex=2,Accounts=new[]{1,0},Data=Encoders.Base58.EncodeData(SystemProgram.Transfer(Sender.PublicKey,Recipient.PublicKey,1).Data)});
            // Recipient precedes sender: account-list order must not determine the direction.
            return Respond(method,Newtonsoft.Json.JsonConvert.SerializeObject(new{Slot=Mode=="wrong-slot" ? 11:10,
                Transaction=new{Signatures=new[]{Mode=="wrong-signature" ? "other":"owned-signature"},
                    Message=new{AccountKeys=new[]{Recipient.PublicKey.Key,Sender.PublicKey.Key,SystemProgram.ProgramIdKey.Key},Instructions=instructions}},
                Meta=new{Error=Mode=="wrong-status" ? new{Type="AccountNotFound"}:error}}));
        }
    }
}
