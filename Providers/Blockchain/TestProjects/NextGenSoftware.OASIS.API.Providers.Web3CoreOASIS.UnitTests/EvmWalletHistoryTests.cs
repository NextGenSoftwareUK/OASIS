using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.Evm;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public sealed class EvmWalletHistoryTests
{
    private const string Wallet="0x1111111111111111111111111111111111111111";
    [DataTestMethod]
    [DataRow("normal")] [DataRow("zero")] [DataRow("large-value")]
    [DataRow("too-large")] [DataRow("missing-balance")] [DataRow("transport")]
    public async Task NativeBalanceValidatesSdkAmountAndPreservesReadFailures(string mode)
    {
        var rpc=DispatchProxy.Create<IClient,HistoryRpc>();var proxy=(HistoryRpc)(object)rpc;proxy.Mode=mode;
        var result=await EvmWalletBalance.ReadAsync(new GetWeb3WalletBalanceRequest{WalletAddress=Wallet},()=>Task.FromResult(new OASISResult<Web3>{Result=new Web3(rpc)}));
        if(mode is "normal" or "zero" or "large-value")
        {
            Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
            var raw=mode=="zero" ? System.Numerics.BigInteger.Zero:mode=="large-value" ? (System.Numerics.BigInteger.One<<256)-1:System.Numerics.BigInteger.One;
            Assert.AreEqual((double)raw/1e18,result.Result);Assert.IsTrue(double.IsFinite(result.Result));
        }
        else
        {
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
            if(mode=="transport") Assert.AreSame(proxy.Cause,result.Exception);
            else Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
        }
    }

    [TestMethod]
    public async Task BalanceValidatesBeforeInitializationAndPreservesActivationCause()
    {
        var calls=0;var cause=new IOException("owned initialization failure");
        Task<OASISResult<Web3>> Initialize(){calls++;return Task.FromResult(new OASISResult<Web3>{IsError=true,Exception=cause,ErrorCode="OWNED_INIT_DENIED",Message="owned initialization failure"});}
        var invalid=await EvmWalletBalance.ReadAsync(new GetWeb3WalletBalanceRequest{WalletAddress="invalid"},Initialize);
        Assert.IsTrue(invalid.IsError);Assert.AreEqual(0,calls);
        var denied=await EvmWalletBalance.ReadAsync(new GetWeb3WalletBalanceRequest{WalletAddress=Wallet},Initialize);
        Assert.IsTrue(denied.IsError);Assert.AreEqual(1,calls);Assert.AreSame(cause,denied.Exception);Assert.AreEqual("OWNED_INIT_DENIED",denied.ErrorCode);Assert.IsFalse(denied.IsLoaded);
    }
    [DataTestMethod]
    [DataRow("normal")] [DataRow("reverted")] [DataRow("large-value")]
    [DataRow("bad-status")] [DataRow("missing-block")] [DataRow("missing-receipt")] [DataRow("bad-receipt-hash")]
    [DataRow("anchor-changed")] [DataRow("transport")]
    public async Task ActualSdkHistoryIsExactBoundedAndNeverSkipsUnavailableData(string mode)
    {
        var rpc=DispatchProxy.Create<IClient,HistoryRpc>();var proxy=(HistoryRpc)(object)rpc;proxy.Mode=mode;
        var result=await EvmWalletHistory.ReadAsync(new GetWeb3TransactionsRequest{WalletAddress=Wallet,FromBlock=1,ToBlock=1},()=>Task.FromResult(new OASISResult<Web3>{Result=new Web3(rpc)}));
        if(mode is "normal" or "reverted" or "large-value")
        {
            Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.Result.Count);
            Assert.AreEqual(mode=="large-value" ? ((System.Numerics.BigInteger.One<<256)-1).ToString():"1",result.Result[0].RawAmount);
            Assert.AreEqual("wei",result.Result[0].AmountUnit);Assert.AreEqual("0x"+new string('b',64),result.Result[0].NativeTransactionId);
            Assert.AreEqual(mode=="reverted" ? "reverted":"succeeded",result.Result[0].NativeExecutionResult);Assert.AreEqual(2,proxy.BlockReads);
        }
        else
        {
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
            if(mode=="transport") Assert.AreSame(proxy.Cause,result.Exception);
            else Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
        }
    }

    [DataTestMethod]
    [DataRow(2UL,1UL)] [DataRow(0UL,1000UL)]
    public async Task InvalidRangeFailsBeforeClientInitialization(ulong from,ulong to)
    {
        var calls=0;var result=await EvmWalletHistory.ReadAsync(new GetWeb3TransactionsRequest{WalletAddress=Wallet,FromBlock=from,ToBlock=to},()=>{calls++;throw new InvalidOperationException("must not initialize");});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentOutOfRangeException));Assert.AreEqual(0,calls);
    }

    public class HistoryRpc : DispatchProxy
    {
        public string Mode="normal";public int BlockReads;public readonly IOException Cause=new("owned history transport failure");
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name.StartsWith("get_")) return null;
            if(method.Name.StartsWith("set_")) return null;
            if(method.Name!="SendRequestAsync") throw new InvalidOperationException(method.Name);
            var request=(RpcRequest)args![0]!;string json;
            var hash="0x"+new string('a',64);var transaction="0x"+new string('b',64);
            if(request.Method=="eth_getBalance")
            {
                if(Mode=="transport") throw Cause;
                json=Mode=="missing-balance" ? "null":JsonConvert.SerializeObject(Mode=="zero" ? "0x0":Mode=="large-value" ? "0x"+new string('f',64):Mode=="too-large" ? "0x1"+new string('0',64):"0x1");
            }
            else if(request.Method=="eth_blockNumber") json="\"0x1\"";
            else if(request.Method=="eth_getBlockByNumber")
            {
                BlockReads++;if(Mode=="transport") throw Cause;
                if(Mode=="missing-block") json="null";
                else json=JsonConvert.SerializeObject(new{number="0x1",hash=Mode=="anchor-changed" && BlockReads>1 ? "0x"+new string('d',64):hash,parentHash="0x"+new string('c',64),timestamp="0x65",
                    transactions=new[]{new{hash=transaction,from=Wallet,to="0x2222222222222222222222222222222222222222",value=Mode=="large-value" ? "0x"+new string('f',64):"0x1"}}});
            }
            else if(request.Method=="eth_getTransactionReceipt") json=Mode=="missing-receipt" ? "null":JsonConvert.SerializeObject(new{transactionHash=Mode=="bad-receipt-hash" ? "0x"+new string('e',64):transaction,blockHash=hash,blockNumber="0x1",status=Mode=="bad-status" ? "0x2":Mode=="reverted" ? "0x0":"0x1"});
            else throw new InvalidOperationException(request.Method);
            var type=method.ReturnType.GetGenericArguments()[0];var response=JsonConvert.DeserializeObject(json,type);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(type).Invoke(null,new[]{response});
        }
    }
}
