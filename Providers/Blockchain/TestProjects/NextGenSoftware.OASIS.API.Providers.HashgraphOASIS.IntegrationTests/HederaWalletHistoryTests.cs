using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaWalletHistoryTests
{
    private static WalletTransaction Map(string json)
    {
        using var document = JsonDocument.Parse(json);
        return (WalletTransaction)typeof(HashgraphOASIS).GetMethod("MapMirrorWalletTransaction", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { document.RootElement, "0.0.1001" })!;
    }

    private const string Record = """
        {"transaction_id":"0.0.1001-1700000000-000000001","consensus_timestamp":"1700000000.123456789",
         "result":"SUCCESS","name":"CRYPTOTRANSFER","transfers":[
          {"account":"0.0.1001","amount":-9007199254740993},{"account":"0.0.1001","amount":-7},
          {"account":"0.0.1002","amount":9007199254740993}]}
        """;

    [TestMethod]
    public void ExactWalletDeltaIncludesFeesWithoutInventingCounterparty()
    {
        var result = Map(Record);
        Assert.AreEqual("-9007199254741000", result.RawAmount);
        Assert.AreEqual("tinybar", result.AmountUnit);
        Assert.AreEqual("0.0.1001-1700000000-000000001", result.NativeTransactionId);
        Assert.AreEqual(Guid.Empty, result.TransactionId);
        Assert.IsNull(result.FromWalletAddress); Assert.IsNull(result.ToWalletAddress);
        Assert.AreEqual(DateTime.UnixEpoch.AddSeconds(1700000000).AddTicks(1234567), result.CreatedDate);
    }

    [TestMethod]
    public void FailedRecordRetainsActualExecutionResultAndAppliedFeeDelta()
    {
        var result = Map(Record.Replace("SUCCESS", "CONTRACT_REVERT_EXECUTED"));
        Assert.AreEqual("CONTRACT_REVERT_EXECUTED", result.NativeExecutionResult);
        Assert.AreEqual("CONTRACT_REVERT_EXECUTED", result.NativeError);
        Assert.AreEqual("-9007199254741000", result.RawAmount);
    }

    [DataTestMethod]
    [DataRow("transaction_id", "")]
    [DataRow("consensus_timestamp", "bad")]
    [DataRow("result", "")]
    [DataRow("name", "")]
    public void IncompleteRecordsCannotBecomeSuccessfulHistory(string field, string value)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Record)!;
        data[field] = JsonSerializer.SerializeToElement(value);
        Assert.ThrowsException<TargetInvocationException>(() => Map(JsonSerializer.Serialize(data)));
    }
}
