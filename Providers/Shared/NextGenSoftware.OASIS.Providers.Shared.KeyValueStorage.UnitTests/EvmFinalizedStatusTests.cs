using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class EvmFinalizedStatusTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
    public async Task CancellationDuringObservationCannotReturnPendingOrDispatchNextRpc(int stage)
    {
        using var source=new CancellationTokenSource();var finalityReads=0;var canonicalReads=0;
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>EvmFinalizedStatus.ObserveAsync(Tx,
            _=>{if(stage<2)source.Cancel();return Task.FromResult<EvmFinalizedStatus.Receipt>(stage==0?null:new(Tx,Hash,5,1));},
            _=>{finalityReads++;if(stage==2)source.Cancel();return Task.FromResult(new EvmFinalizedStatus.Block(Hash,stage==2?4:5));},
            (_,_)=>{canonicalReads++;source.Cancel();return Task.FromResult(new EvmFinalizedStatus.Block(Hash,5));},source.Token));
        Assert.AreEqual(stage<2?0:1,finalityReads);Assert.AreEqual(stage==3?1:0,canonicalReads);
    }
    private const string Tx="0x1111111111111111111111111111111111111111111111111111111111111111";
    private const string Hash="0x2222222222222222222222222222222222222222222222222222222222222222";
    private static Task<BridgeTransactionStatus> Observe(EvmFinalizedStatus.Receipt receipt,EvmFinalizedStatus.Block finalized,EvmFinalizedStatus.Block canonical)
        =>EvmFinalizedStatus.ObserveAsync(Tx,_=>Task.FromResult(receipt),_=>Task.FromResult(finalized),(_,_)=>Task.FromResult(canonical));
    [DataTestMethod][DataRow(0)][DataRow(1)]
    public async Task FinalizedExecutionIsFailedOrCompletedNotCanceled(int status)
        =>Assert.AreEqual(status==1?BridgeTransactionStatus.Completed:BridgeTransactionStatus.Failed,
            await Observe(new(Tx,Hash,5,status),new(Hash,5),new(Hash,5)));
    [TestMethod]
    public async Task MissingReceiptIsPendingWithoutOtherRpcCalls()
    {
        var result=await EvmFinalizedStatus.ObserveAsync(Tx,_=>Task.FromResult<EvmFinalizedStatus.Receipt>(null),
            _=>throw new AssertFailedException("No receipt means no finality query."),(_,_)=>throw new AssertFailedException());
        Assert.AreEqual(BridgeTransactionStatus.Pending,result);
    }
    [TestMethod]
    public async Task IncludedButNotFinalIsPending()=>Assert.AreEqual(BridgeTransactionStatus.Pending,await Observe(new(Tx,Hash,6,1),new(Hash,5),null));
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)]
    public async Task InconsistentEvidenceNeverCompletes(int mode)
    {
        var receipt=new EvmFinalizedStatus.Receipt(mode==0?Hash:Tx,Hash,5,mode==1?null:mode==2?2:1);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Observe(receipt,mode==3?null:new(Hash,5),new(mode==4?Tx:Hash,5)));
    }
    [TestMethod]
    public async Task CancellationStopsBeforeAnyRpc()
    {
        using var source=new CancellationTokenSource();source.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>EvmFinalizedStatus.ObserveAsync(Tx,
            _=>throw new AssertFailedException(),_=>throw new AssertFailedException(),(_,_)=>throw new AssertFailedException(),source.Token));
    }
}
