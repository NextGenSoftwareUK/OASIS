using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass]
public class CosmosWalletHistoryProjectionTests
{
    [DataTestMethod]
    [DataRow(-1, 0)]
    [DataRow(0, -1)]
    [DataRow(0, 1)]
    public void InvalidMessageOrSingleSendOutputIndexIsRejected(int messageIndex, int transferIndex)
    {
        var row = new System.Collections.Generic.Dictionary<string, object> {
            ["hash"] = new string('a',64), ["messageIndex"] = messageIndex, ["transferIndex"] = transferIndex,
            ["operation"] = "/cosmos.bank.v1beta1.MsgSend", ["code"] = 0,
            ["from"] = "sender", ["to"] = "recipient", ["amountUnits"] = "1",
            ["denom"] = "uatom", ["time"] = "2026-10-12T00:00:00Z", ["memo"] = "fixture" };
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(row));
        using var provider = new CosmosBlockChainOASIS();
        var method = typeof(CosmosBlockChainOASIS).GetMethod("MapIndexedWalletTransaction", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var error = Assert.ThrowsException<TargetInvocationException>(() => method.Invoke(provider, new object[] { json.RootElement, "sender" }));
        Assert.IsInstanceOfType<InvalidOperationException>(error.InnerException);
    }

    [DataTestMethod]
    [DataRow(0L)]
    [DataRow(5L)]
    public void MultiSendOutputsHaveDistinctStableIdentitiesAndRetainFailedExecution(long code)
    {
        using var provider = new CosmosBlockChainOASIS();
        var method = typeof(CosmosBlockChainOASIS).GetMethod("MapIndexedWalletTransaction", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var ids = new System.Collections.Generic.HashSet<System.Guid>();
        foreach (var index in new[] { 0, 1 })
        {
            var row = new System.Collections.Generic.Dictionary<string, object> {
                ["hash"] = new string('a',64), ["messageIndex"] = 2, ["transferIndex"] = index,
                ["operation"] = "/cosmos.bank.v1beta1.MsgMultiSend", ["code"] = code,
                ["from"] = "sender", ["to"] = "recipient", ["amountUnits"] = "9007199254740993",
                ["denom"] = "uatom", ["time"] = "2026-10-12T00:00:00Z", ["memo"] = "fixture" };
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(row));
            var debit = (IWalletTransaction)method.Invoke(provider, new object[] { json.RootElement, "sender" })!;
            var credit = (IWalletTransaction)method.Invoke(provider, new object[] { json.RootElement, "recipient" })!;
            Assert.IsTrue(ids.Add(debit.TransactionId));
            Assert.AreEqual(debit.TransactionId, credit.TransactionId);
            Assert.AreEqual(debit.NativeTransactionId, credit.NativeTransactionId);
            Assert.AreEqual("9007199254740993", debit.RawAmount);
            Assert.AreEqual("uatom",debit.Token);Assert.AreEqual(ProviderType.CosmosBlockChainOASIS,debit.ProviderType);
            Assert.AreEqual("fixture",debit.MemoText);Assert.AreEqual("Zml4dHVyZQ==",debit.MemoBase64);
            Assert.AreEqual(code == 0 ? "Success" : "Failed", debit.NativeExecutionResult);
            Assert.AreEqual(TransactionType.Debit, debit.TransactionType);
            Assert.AreEqual(TransactionType.Credit, credit.TransactionType);
        }
    }

    [DataTestMethod]
    [DataRow("hash", "fabricated")]
    [DataRow("amountUnits", "-1")]
    [DataRow("amountUnits", "01")]
    [DataRow("amountUnits", "1.5")]
    [DataRow("denom", "other")]
    [DataRow("from", "")]
    public void MalformedIndexerRowIsNotProjectedAsSuccess(string field, string value)
    {
        var row = new System.Collections.Generic.Dictionary<string,object> {
            ["hash"] = new string('a',64), ["messageIndex"] = 0, ["transferIndex"] = 0, ["operation"] = "/cosmos.bank.v1beta1.MsgSend", ["code"] = 0,
            ["from"] = "sender", ["to"] = "recipient", ["amountUnits"] = "1",
            ["denom"] = "uatom", ["time"] = "2026-10-12T00:00:00Z", ["memo"] = "fixture" };
        row[field] = value;
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(row));
        using var provider = new CosmosBlockChainOASIS();
        var method = typeof(CosmosBlockChainOASIS).GetMethod("MapIndexedWalletTransaction",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var error = Assert.ThrowsException<TargetInvocationException>(() => method.Invoke(provider,new object[] {json.RootElement,"sender"}));
        Assert.IsInstanceOfType<InvalidOperationException>(error.InnerException);
    }

    [DataTestMethod]
    [DataRow(0L)]
    [DataRow(5L)]
    [DataRow(4294967295L)]
    public void NativeHistoryPreservesExactUnitsAndBothDirections(long code)
    {
        var hash = new string('a',64);
        using var json = JsonDocument.Parse("{\"transferIndex\":0,\"operation\":\"/cosmos.bank.v1beta1.MsgSend\",\"code\":"+code+",\"hash\":\""+hash+"\",\"messageIndex\":0,\"from\":\"sender\",\"to\":\"recipient\",\"amountUnits\":\"9007199254740993\",\"denom\":\"uatom\",\"time\":\"2026-10-12T00:00:00Z\",\"memo\":\"fixture\"}");
        using var provider = new CosmosBlockChainOASIS();
        var method = typeof(CosmosBlockChainOASIS).GetMethod("MapIndexedWalletTransaction",BindingFlags.Instance|BindingFlags.NonPublic)!;
        foreach (var wallet in new[] {"sender","recipient"})
        {
            var result = (IWalletTransaction)method.Invoke(provider,new object[] {json.RootElement,wallet})!;
            Assert.AreEqual(hash,result.NativeTransactionId);
            Assert.AreEqual("9007199254740993",result.RawAmount);
            Assert.AreEqual("uatom",result.AmountUnit);
            Assert.AreEqual("/cosmos.bank.v1beta1.MsgSend",result.NativeOperation);
            Assert.AreEqual(code == 0 ? "Success" : "Failed",result.NativeExecutionResult);
            Assert.AreEqual(code == 0 ? null : "Cosmos execution code " + code,result.NativeError);
            Assert.AreEqual(wallet=="sender"?TransactionType.Debit:TransactionType.Credit,result.TransactionType);
        }
    }
}
