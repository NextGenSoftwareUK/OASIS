using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaReceiptStatusTests
{
    private static BridgeTransactionStatus Map(string? status) => (BridgeTransactionStatus)typeof(HashgraphOASIS)
        .GetMethod("MapReceiptStatus", BindingFlags.NonPublic | BindingFlags.Static)!
        .Invoke(null, new object?[] { status })!;

    [DataTestMethod]
    [DataRow("SUCCESS", BridgeTransactionStatus.Completed)]
    [DataRow("UNKNOWN", BridgeTransactionStatus.Pending)]
    [DataRow("RECEIPT_NOT_FOUND", BridgeTransactionStatus.NotFound)]
    [DataRow("TRANSACTION_EXPIRED", BridgeTransactionStatus.Expired)]
    [DataRow("INSUFFICIENT_PAYER_BALANCE", BridgeTransactionStatus.InsufficientFunds)]
    [DataRow("INSUFFICIENT_TX_FEE", BridgeTransactionStatus.InsufficientFundsForFee)]
    [DataRow("CONTRACT_REVERT_EXECUTED", BridgeTransactionStatus.Failed)]
    [DataRow("CONTRACT_EXECUTION_EXCEPTION", BridgeTransactionStatus.Failed)]
    [DataRow("INVALID_SIGNATURE", BridgeTransactionStatus.Failed)]
    [DataRow("INSUFFICIENT_TOKEN_BALANCE", BridgeTransactionStatus.Failed)]
    [DataRow("SENDER_DOES_NOT_OWN_NFT_SERIAL_NO", BridgeTransactionStatus.Failed)]
    public void ActualReceiptSeparatesFailureFromLookupError(string status, BridgeTransactionStatus expected)
        => Assert.AreEqual(expected, Map(status));

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("made-up-status")]
    [DataRow("OK")]
    [DataRow("BUSY")]
    public void UnclassifiedOrPrecheckStatusDoesNotFabricateLedgerOutcome(string? status)
    {
        var error = Assert.ThrowsException<TargetInvocationException>(() => Map(status));
        Assert.IsInstanceOfType(error.InnerException, typeof(InvalidOperationException));
    }
}
