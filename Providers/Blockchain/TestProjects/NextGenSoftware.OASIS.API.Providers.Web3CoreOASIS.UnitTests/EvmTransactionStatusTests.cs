using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Providers.Shared.Evm;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public class EvmTransactionStatusTests
{
    private const string Tx="0x1111111111111111111111111111111111111111111111111111111111111111";
    [DataTestMethod]
    [DataRow("success")][DataRow("failed")][DataRow("missing")][DataRow("unfinalized")]
    [DataRow("mismatch")][DataRow("no-status")][DataRow("bad-status")][DataRow("noncanonical")]
    [DataRow("no-final")][DataRow("transport")][DataRow("unsupported-final")]
    public async Task RealNethereumRequestsUseFinalizedTagAndHonestOutcome(string mode)
    {
        var client=DispatchProxy.Create<IClient,StatusRpc>();var rpc=(StatusRpc)(object)client;rpc.Mode=mode;
        var result=await EvmTransactionStatus.ReadAsync(Tx,()=>Task.FromResult(new OASISResult<Web3>{Result=new Web3(client)}));
        if(mode is "success" or "failed" or "missing" or "unfinalized"){
            Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);
            Assert.AreEqual(mode=="success"?BridgeTransactionStatus.Completed:mode=="failed"?BridgeTransactionStatus.Failed:BridgeTransactionStatus.Pending,result.Result);
        }else{
            Assert.IsTrue(result.IsError);Assert.AreEqual(BridgeTransactionStatus.Pending,result.Result);
            if(mode is "transport" or "unsupported-final")Assert.AreSame(rpc.Cause,result.Exception);
            else Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
        }
        Assert.AreEqual(1,rpc.ReceiptReads);
        if(mode=="missing")Assert.AreEqual(0,rpc.BlockReads);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task InvalidHashOrCancellationStopsBeforeInitialization(bool cancel)
    {
        using var source=new CancellationTokenSource();if(cancel)source.Cancel();var calls=0;
        var result=await EvmTransactionStatus.ReadAsync(cancel?Tx:"invalid",()=>{calls++;throw new AssertFailedException();},source.Token);
        Assert.IsTrue(result.IsError);Assert.AreEqual(BridgeTransactionStatus.Pending,result.Result);Assert.AreEqual(0,calls);
    }
    [TestMethod]
    public async Task CancellationAfterSdkDispatchBoundsWaitingWithoutClaimingHttpAbort()
    {
        var client=DispatchProxy.Create<IClient,StatusRpc>();var rpc=(StatusRpc)(object)client;rpc.Mode="blocked";
        using var cancellation=new CancellationTokenSource();
        try{
            var running=EvmTransactionStatus.ReadAsync(Tx,()=>Task.FromResult(new OASISResult<Web3>{Result=new Web3(client)}),cancellation.Token);
            await rpc.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));cancellation.Cancel();
            var result=await running.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(result.IsError);Assert.AreEqual(BridgeTransactionStatus.Pending,result.Result);Assert.IsInstanceOfType(result.Exception,typeof(OperationCanceledException));
            Assert.AreEqual(1,rpc.ReceiptReads);Assert.AreEqual(0,rpc.BlockReads);
            var requestTask=(Task)rpc.Pending!.GetType().GetProperty("Task")!.GetValue(rpc.Pending)!;
            Assert.IsFalse(requestTask.IsCompleted,"Borrowed SDK request is not falsely described as aborted.");
        }finally{if(rpc.Pending!=null)rpc.Pending.GetType().GetMethod("SetResult")!.Invoke(rpc.Pending,new object?[]{null});}
    }
    [TestMethod]
    public async Task ActivationFailurePreservesCauseAndErrorCode()
    {
        var cause=new IOException("owned initialization failure");
        var result=await EvmTransactionStatus.ReadAsync(Tx,()=>Task.FromResult(new OASISResult<Web3>{IsError=true,Exception=cause,Message=cause.Message,ErrorCode="OWNED"}));
        Assert.IsTrue(result.IsError);Assert.AreSame(cause,result.Exception);Assert.AreEqual("OWNED",result.ErrorCode);Assert.AreEqual(BridgeTransactionStatus.Pending,result.Result);
    }
    public class StatusRpc:DispatchProxy
    {
        public string Mode="success";public int ReceiptReads,BlockReads;
        public readonly IOException Cause=new("owned status RPC failure");
        public readonly TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public object? Pending;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name.StartsWith("get_")||method.Name.StartsWith("set_"))return null;
            if(method.Name!="SendRequestAsync")throw new InvalidOperationException(method.Name);
            var request=(RpcRequest)args![0]!;string json;var hash="0x"+new string('2',64);
            if(request.Method=="eth_getTransactionReceipt"){
                ReceiptReads++;if(Mode=="transport")throw Cause;
                if(Mode=="blocked"){
                    var sourceType=typeof(TaskCompletionSource<>).MakeGenericType(method.ReturnType.GetGenericArguments()[0]);
                    Pending=Activator.CreateInstance(sourceType);Started.TrySetResult();return sourceType.GetProperty("Task")!.GetValue(Pending);
                }
                Assert.AreEqual(Tx,request.RawParameters[0]);
                json=Mode=="missing"?"null":JsonConvert.SerializeObject(new{transactionHash=Mode=="mismatch"?hash:Tx,blockHash=hash,blockNumber="0x5",status=Mode=="no-status"?null:Mode=="bad-status"?"0x2":Mode=="failed"?"0x0":"0x1"});
            }else if(request.Method=="eth_getBlockByNumber"){
                BlockReads++;
                var parameter=(Nethereum.RPC.Eth.DTOs.BlockParameter)request.RawParameters[0];
                if(BlockReads==1){Assert.AreEqual("finalized",parameter.GetRPCParamAsNumber());if(Mode=="unsupported-final")throw Cause;}
                else Assert.AreEqual(new System.Numerics.BigInteger(5),parameter.GetRPCParamAsNumber());
                json=Mode=="no-final"?"null":JsonConvert.SerializeObject(new{number=Mode=="unfinalized"?"0x4":"0x5",hash=Mode=="noncanonical"&&BlockReads>1?Tx:hash,transactions=Array.Empty<string>()});
            }else throw new AssertFailedException("Unexpected RPC: "+request.Method);
            var type=method.ReturnType.GetGenericArguments()[0];var response=JsonConvert.DeserializeObject(json,type);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(type).Invoke(null,new[]{response});
        }
    }
}
