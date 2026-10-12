using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaNativeFeeTests
{
    private const string Id = "0.0.1001@1700000000.000000001";
    private static JsonElement Map(string fee = "9007199254740993", string status = "SUCCESS", int nonce = 0,
        string scheduled = "false", string operation = "TOKENMINT")
    {
        using var document = JsonDocument.Parse($$"""
        {"transactions":[{"transaction_id":"0.0.1001-1700000000-000000001","nonce":{{nonce}},
        "scheduled":{{scheduled}},"parent_consensus_timestamp":null,"name":"{{operation}}",
        "result":"{{status}}","consensus_timestamp":"1700000001.000000001","charged_tx_fee":{{fee}}}]}
        """);
        return (JsonElement)typeof(HashgraphOASIS).GetMethod("MapNativeFeeReceipt", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { document.RootElement, Id, "testnet" })!;
    }

    [TestMethod]
    public void ChargedFeePreservesExactIntegerAndNativePayer()
    {
        var receipt = Map();
        Assert.AreEqual("9007199254740993", receipt.GetProperty("netUnits").GetString());
        Assert.AreEqual("0.0.1001", receipt.GetProperty("payer").GetString());
        Assert.IsTrue(receipt.GetProperty("success").GetBoolean());
    }

    [DataTestMethod]
    [DataRow("-1", "SUCCESS", 0, "false", "TOKENMINT")]
    [DataRow("1", "UNKNOWN", 0, "false", "TOKENMINT")]
    [DataRow("1", "SUCCESS", 1, "false", "TOKENMINT")]
    [DataRow("1", "SUCCESS", 0, "true", "TOKENMINT")]
    [DataRow("1", "SUCCESS", 0, "false", "ETHEREUMTRANSACTION")]
    public void UnattributableOrUnconfirmedFeesAreRejected(string fee, string status, int nonce, string scheduled, string operation)
        => Assert.ThrowsException<TargetInvocationException>(() => Map(fee, status, nonce, scheduled, operation));

    [TestMethod]
    public async Task OriginalChargedFailureIsReadWithoutActivationOrSubmission()
    {
        var provider = new HashgraphOASIS("https://testnet.mirrornode.hedera.com", "testnet", "296");
        var receipt = await provider.GetNativeFeeReceiptAsync("0.0.10993779@1791758571.546470406");
        Assert.IsFalse(receipt.IsError, receipt.Message);
        Assert.AreEqual("105954", receipt.Result.GetProperty("netUnits").GetString());
        Assert.AreEqual("0.0.10993779", receipt.Result.GetProperty("payer").GetString());
        Assert.IsFalse(receipt.Result.GetProperty("success").GetBoolean());
        Assert.AreEqual("INSUFFICIENT_TX_FEE", receipt.Result.GetProperty("nativeStatus").GetString());
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
