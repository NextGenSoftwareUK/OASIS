using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public class EthereumStorageReceiptTests
{
    [DataTestMethod]
    [DataRow(1, true)]
    [DataRow(0, false)]
    public void ExplicitReceiptStatusIsRequired(int status, bool success)
    {
        var receipt = new TransactionReceipt { Status = new HexBigInteger(status), TransactionHash = "0x" + new string('1',64) };
        var check = typeof(EthereumOASIS).GetMethod("StorageReceiptSucceeded",BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.AreEqual(success,check.Invoke(null,new object[] {receipt}));
    }

    [DataTestMethod]
    [DataRow("missing-receipt")]
    [DataRow("missing-status")]
    [DataRow("missing-identity")]
    [DataRow("invalid-status")]
    [DataRow("invalid-identity")]
    public void UncertainReceiptsAreReplayFenced(string scenario)
    {
        TransactionReceipt? receipt = scenario == "missing-receipt" ? null : new TransactionReceipt {
            Status = scenario == "missing-status" ? null : new HexBigInteger(scenario == "invalid-status" ? 2 : 1),
            TransactionHash = scenario == "missing-identity" ? null : scenario == "invalid-identity" ? "not-a-native-hash" : "0x" + new string('2',64) };
        var check = typeof(EthereumOASIS).GetMethod("StorageReceiptSucceeded",BindingFlags.Static | BindingFlags.NonPublic)!;
        var error = Assert.ThrowsException<TargetInvocationException>(() => check.Invoke(null,new object?[] {receipt})).InnerException!;
        Assert.IsInstanceOfType(error,typeof(EthereumStorageReceiptReconciliationException));
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(error));
        Assert.AreEqual(receipt?.TransactionHash,((EthereumStorageReceiptReconciliationException)error).OriginalTransactionId);
    }

    [TestMethod]
    public void PartialCommitEvidenceCopiesNativeIdentitiesAndPreservesCause()
    {
        var identity = "0x" + new string('3',64);
        var identities = new List<string> { identity };
        var cause = new IOException("Later storage write failed");
        var error = new EthereumPartialStorageMutationException(identities,cause);
        identities.Clear();
        Assert.AreEqual(identity,error.OriginalTransactionId);
        Assert.AreEqual(1,error.CommittedTransactions.Count);
        Assert.AreSame(cause,error.InnerException);
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(error));
    }
}
