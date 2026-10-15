using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public class Web3TransactionOutcomeTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task PreparedIdentitySurvivesMissingOrConflictingSubmissionReply(bool conflictingReply)
    {
        var cause=new IOException("submission response unavailable");var observations=0;
        var failure=await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>Web3TransactionOutcome.SubmitAndObserveAsync(
            ()=>conflictingReply ? Task.FromResult("different-hash"):Task.FromException<string>(cause),
            _=>{observations++;return Task.FromResult<TransactionReceipt>(null!);},TimeSpan.FromSeconds(1),TimeSpan.FromMilliseconds(1),"original-signed-hash"));
        Assert.AreEqual("original-signed-hash",failure.OriginalTransactionId);Assert.AreEqual(0,observations);
        if(!conflictingReply) Assert.AreSame(cause,failure.InnerException);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task TransportFailureRetainsOriginalCauseAndAvailableHash(bool afterHash)
    {
        var cause=new IOException("owned transport failure");
        var failure=await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>Web3TransactionOutcome.SubmitAndObserveAsync(
            ()=>afterHash ? Task.FromResult("owned-hash"):Task.FromException<string>(cause),
            _=>Task.FromException<TransactionReceipt>(cause),TimeSpan.FromSeconds(1),TimeSpan.FromMilliseconds(1)));
        Assert.AreSame(cause,failure.InnerException);Assert.AreEqual(afterHash ? "owned-hash":string.Empty,failure.OriginalTransactionId);
        Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
    }

    [DataTestMethod]
    [DataRow("revert")] [DataRow("mismatch")] [DataRow("missing-status")]
    public async Task InvalidOrRevertedReceiptCannotBecomeSuccess(string mode)
    {
        var receipt=new TransactionReceipt {TransactionHash=mode=="mismatch" ? "other":"owned",Status=mode=="missing-status" ? null:new HexBigInteger(mode=="revert" ? 0:1)};
        var failure=await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>Web3TransactionOutcome.SubmitAndObserveAsync(
            ()=>Task.FromResult("owned"),_=>Task.FromResult(receipt),TimeSpan.FromSeconds(1),TimeSpan.FromMilliseconds(1)));
        Assert.AreEqual("owned",failure.OriginalTransactionId);Assert.AreSame(receipt,failure.ObservedReceipt);
    }

    [TestMethod]
    public async Task SuccessfulReceiptAndPendingTimeoutRemainDistinct()
    {
        var receipt=new TransactionReceipt {TransactionHash="owned",Status=new HexBigInteger(1)};
        Assert.AreSame(receipt,await Web3TransactionOutcome.SubmitAndObserveAsync(()=>Task.FromResult("owned"),_=>Task.FromResult(receipt),TimeSpan.FromSeconds(1),TimeSpan.FromMilliseconds(1)));
        var timeout=await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>Web3TransactionOutcome.SubmitAndObserveAsync(
            ()=>Task.FromResult("owned"),_=>Task.FromResult<TransactionReceipt>(null!),TimeSpan.FromMilliseconds(10),TimeSpan.FromMilliseconds(1)));
        Assert.AreEqual("owned",timeout.OriginalTransactionId);Assert.IsInstanceOfType(timeout.InnerException,typeof(OperationCanceledException));
    }
}
