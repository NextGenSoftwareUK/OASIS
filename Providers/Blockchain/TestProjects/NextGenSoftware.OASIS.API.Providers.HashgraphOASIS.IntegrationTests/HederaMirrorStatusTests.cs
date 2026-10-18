using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaMirrorStatusTests
{
    [TestMethod]
    public async Task ExistingOwnedTestnetFailureIsReadWithoutSubmission()
    {
        var client = new HashgraphClient("https://testnet.mirrornode.hedera.com");
        var status = await client.GetTransactionStatusAsync("0.0.10993779@1791758571.546470406");
        Assert.AreEqual(BridgeTransactionStatus.InsufficientFundsForFee, status);
    }

    private static object Map(string json) {
        using var document = JsonDocument.Parse(json);
        return typeof(HashgraphClient).GetMethod("MapMirrorTransactionStatus", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { document.RootElement, "0.0.1001@1700000000.000000001" })!;
    }
    [DataTestMethod]
    [DataRow("SUCCESS", BridgeTransactionStatus.Completed)]
    [DataRow("UNKNOWN", BridgeTransactionStatus.Pending)]
    [DataRow("CONTRACT_REVERT_EXECUTED", BridgeTransactionStatus.Failed)]
    [DataRow("INSUFFICIENT_TX_FEE", BridgeTransactionStatus.InsufficientFundsForFee)]
    public void VerifiedIdentityUsesExistingReceiptClassification(string status, BridgeTransactionStatus expected)
        => Assert.AreEqual(expected, Map("{\"transactions\":[" + Row(status) + "]}"));

    private static string Row(string status, int nonce = 0, bool scheduled = false, string parent = "null")
        => $$"""
        {"transaction_id":"0.0.1001-1700000000-000000001","result":"{{status}}",
        "nonce":{{nonce}},"scheduled":{{scheduled.ToString().ToLowerInvariant()}},"parent_consensus_timestamp":{{parent}}}
        """;

    [DataTestMethod]
    [DataRow(1, false, "null")]
    [DataRow(0, true, "null")]
    [DataRow(0, false, "\"1700000000.000000002\"")]
    public void ChildOrScheduledSuccessCannotEstablishOriginalParentOutcome(int nonce, bool scheduled, string parent)
        => Assert.ThrowsException<TargetInvocationException>(() => Map("{\"transactions\":[" + Row("SUCCESS", nonce, scheduled, parent) + "]}"));

    [TestMethod]
    public void DuplicateSubmissionsDoNotReplaceOriginalFailure()
    {
        var duplicate = Row("DUPLICATE_TRANSACTION");
        var original = Row("CONTRACT_REVERT_EXECUTED");
        foreach (var rows in new[] { duplicate + "," + original, original + "," + duplicate })
            Assert.AreEqual(BridgeTransactionStatus.Failed, Map("{\"transactions\":[" + rows + "]}"));
        Assert.ThrowsException<TargetInvocationException>(() => Map("{\"transactions\":[" + duplicate + "]}"));
        Assert.ThrowsException<TargetInvocationException>(() => Map("{\"transactions\":[" + original + "," + Row("SUCCESS") + "]}"));
        Assert.ThrowsException<TargetInvocationException>(() => Map("{\"transactions\":[" + Row("INVENTED") + "]}"));
    }
    [DataTestMethod]
    [DataRow("{\"transactions\":[]}")]
    [DataRow("{\"transactions\":[{\"transaction_id\":\"different\",\"result\":\"SUCCESS\"}]}")]
    [DataRow("{\"transactions\":[{\"transaction_id\":\"0.0.1001-1700000000-000000001\",\"result\":\"INVENTED\"}]}")]
    public void AmbiguousOrUnclassifiedResponsesCannotEstablishOutcome(string json)
        => Assert.ThrowsException<TargetInvocationException>(() => Map(json));
}
