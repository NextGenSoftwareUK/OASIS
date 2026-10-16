using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using Solnet.Rpc;
using Solnet.Rpc.Types;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaHistoryTests
{
    [DataTestMethod]
    [DataRow("paged")] [DataRow("repeat")] [DataRow("transport")]
    public async Task PaginationUsesNativeCursorAndNeverReturnsPartialSuccessOnFailure(string mode)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,HistoryRpc>();var proxy=(HistoryRpc)(object)rpc;proxy.Mode=mode;
        var provider=new SolanaOASIS("http://127.0.0.1:1",owner.PrivateKey.Key,owner.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);
        var result=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress=owner.PublicKey.Key});
        Assert.AreEqual(2,proxy.Calls);Assert.AreEqual("page999",proxy.Before);
        if(mode=="paged") {Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(1000,result.Result.Count);}
        else {Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,mode=="transport" ? typeof(IOException):typeof(InvalidDataException));}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeHistoryPreservesSignaturesFailureAndInclusiveSlotRange(bool synchronous)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,HistoryRpc>();var proxy=(HistoryRpc)(object)rpc;
        var provider=new SolanaOASIS("http://127.0.0.1:1",owner.PrivateKey.Key,owner.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);
        var request=new GetWeb3TransactionsRequest{WalletAddress=owner.PublicKey.Key,FromBlock=10,ToBlock=20};
        var result=synchronous ? provider.GetTransactions(request):await provider.GetTransactionsAsync(request);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsNotNull(result.Result);Assert.AreEqual(2,result.Result.Count);
        Assert.AreEqual("sig20",result.Result[0].NativeTransactionId);Assert.AreEqual("succeeded",result.Result[0].NativeExecutionResult);
        Assert.AreEqual("sig10",result.Result[1].NativeTransactionId);Assert.AreEqual("failed",result.Result[1].NativeExecutionResult);
        Assert.IsFalse(string.IsNullOrEmpty(result.Result[1].NativeError));Assert.IsNull(result.Result[0].RawAmount);
        Assert.AreEqual(Commitment.Finalized,proxy.Commitment);Assert.AreEqual(1,proxy.Calls);
    }

    public class HistoryRpc : SolanaNativeTokenTests.TokenRpc
    {
        public Commitment Commitment;public string? Mode,Before;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="GetTransactionAsync")
            {
                var signature=(string)args![0]!;
                var slot=signature.StartsWith("page") ? 2000-int.Parse(signature[4..]):int.Parse(signature[3..]);
                return Respond(method,Newtonsoft.Json.JsonConvert.SerializeObject(new{Slot=slot,
                    Transaction=new{Signatures=new[]{signature},Message=new{AccountKeys=Array.Empty<string>(),Instructions=Array.Empty<object>()}},
                    Meta=new{Error=signature=="sig10" ? new{Type="AccountNotFound"}:null}}));
            }
            Calls++;if(method!.Name!="GetSignaturesForAddressAsync") throw new InvalidOperationException(method.Name);
            Commitment=(Commitment)args![4]!;
            Before=(string?)args[2];
            if(Mode!=null)
            {
                if(Calls==1) return Respond(method,Newtonsoft.Json.JsonConvert.SerializeObject(Enumerable.Range(0,1000).Select(index=>new{Signature="page"+index,Slot=2000-index})));
                if(Mode=="transport") throw new IOException("owned history transport failure");
                return Respond(method,Mode=="repeat" ? "[{\"Signature\":\"page999\",\"Slot\":1001}]":"[]");
            }
            // Respond materializes SDK models with Newtonsoft, not Solnet's System.Text.Json wire decoder.
            return Respond(method,"[{\"signature\":\"sig21\",\"slot\":21},{\"signature\":\"sig20\",\"slot\":20,\"blockTime\":1700000000},{\"signature\":\"sig10\",\"slot\":10,\"Error\":{\"Type\":\"AccountNotFound\"}},{\"signature\":\"sig9\",\"slot\":9}]");
        }
    }
}
