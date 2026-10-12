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
    [DataRow(0)]
    [DataRow(5)]
    public void NativeHistoryPreservesExactUnitsAndBothDirections(int code)
    {
        var hash = new string('a',64);
        using var json = JsonDocument.Parse("{\"code\":"+code+",\"hash\":\""+hash+"\",\"messageIndex\":0,\"from\":\"sender\",\"to\":\"recipient\",\"amountUnits\":\"9007199254740993\",\"denom\":\"uatom\",\"time\":\"2026-10-12T00:00:00Z\",\"memo\":\"fixture\"}");
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
            Assert.AreEqual(code == 0 ? null : "Cosmos execution code 5",result.NativeError);
            Assert.AreEqual(wallet=="sender"?TransactionType.Debit:TransactionType.Credit,result.TransactionType);
        }
    }
}
