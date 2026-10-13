using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaReceiptStatusTests
{
    [DataTestMethod]
    [DataRow("SUCCESS", BridgeTransactionStatus.Completed)]
    [DataRow("CONTRACT_REVERT_EXECUTED", BridgeTransactionStatus.Failed)]
    public void ConsensusReceiptBindsStatusToOriginalIdentity(string status, BridgeTransactionStatus expected)
    {
        const string id = "0.0.123@1700000000.123456789";
        using var receipt = System.Text.Json.JsonDocument.Parse($"{{\"transactionId\":\"{id}\",\"status\":\"{status}\"}}");
        var method = typeof(HashgraphOASIS).GetMethod("MapConsensusReceiptStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.AreEqual(expected, method.Invoke(null, new object[] { receipt.RootElement, id }));
    }

    [DataTestMethod]
    [DataRow("{\"transactionId\":\"foreign\",\"status\":\"SUCCESS\"}")]
    [DataRow("{\"status\":\"SUCCESS\"}")]
    [DataRow("{\"transactionId\":null,\"status\":\"SUCCESS\"}")]
    [DataRow("null")]
    public void ForeignOrMissingConsensusIdentityCannotReportCompletion(string json)
    {
        using var receipt = System.Text.Json.JsonDocument.Parse(json);
        var method = typeof(HashgraphOASIS).GetMethod("MapConsensusReceiptStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        var error = Assert.ThrowsException<TargetInvocationException>(() => method.Invoke(null,
            new object[] { receipt.RootElement, "0.0.123@1700000000.123456789" }));
        Assert.IsInstanceOfType(error.InnerException, typeof(InvalidOperationException));
    }
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
