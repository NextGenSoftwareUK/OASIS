using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class PolkadotWalletHistoryTests
{
    [TestMethod]
    public void SharedEvidenceSurvivesInterfaceTypedWebJsonSerialization()
    {
        IWalletTransaction value=new WalletTransaction{RawAmount="3",AmountEvidence="applied-event",NativeEvent="assets.Transferred",
            NativeBlockHash="0x"+new string('a',64),NativeBlockNumber=2,NativeExtrinsicIndex=1,NativeEventIndex=3,NativeExecutionResult="failed"};
        using var document=JsonDocument.Parse(JsonSerializer.Serialize(value,new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.AreEqual("applied-event",document.RootElement.GetProperty("amountEvidence").GetString());
        Assert.AreEqual("failed",document.RootElement.GetProperty("nativeExecutionResult").GetString());
        Assert.AreEqual(2ul,document.RootElement.GetProperty("nativeBlockNumber").GetUInt64());
        Assert.AreEqual(3,document.RootElement.GetProperty("nativeEventIndex").GetInt32());
    }

    [TestMethod]
    public void LegacyImplementersNeedNoNewMembersAndDoNotClaimObservedEffects()
    {
        IWalletTransaction legacy=new LegacyTransaction();
        Assert.IsNull(legacy.AmountEvidence);Assert.IsNull(legacy.NativeEvent);Assert.IsNull(legacy.NativeBlockHash);
        Assert.IsNull(legacy.NativeBlockNumber);Assert.IsNull(legacy.NativeExtrinsicIndex);Assert.IsNull(legacy.NativeEventIndex);
    }
    [DataTestMethod][DataRow("accepted",false)][DataRow("accepted",true)][DataRow("two-events",false)]
    [DataRow("failed",false)][DataRow("generic",false)][DataRow("rpc-error",false)][DataRow("wrong-network",false)]
    [DataRow("wrong-range",false)][DataRow("wrong-block",false)][DataRow("wrong-wallet",false)][DataRow("bad-amount",false)][DataRow("bad-classification",false)]
    public async Task HistoryPreservesNativeEvidenceAndRejectsIncompleteOrMalformedResults(string mode,bool synchronous)
    {
        // Real child process, controlled data only; native finalized history proven separately.
        var priorBridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var priorOutcome=Environment.GetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",mode);
        try {
            using var provider=new PolkadotOASIS("http://127.0.0.1:1");PolkadotAssetMutationTests.Activate(provider);
            var request=new GetWeb3TransactionsRequest{WalletAddress="0x"+new string('c',64),FromBlock=1,ToBlock=1};
            var result=synchronous ? provider.GetTransactions(request) : await provider.GetTransactionsAsync(request);
            var succeeds=mode is "accepted" or "two-events" or "failed" or "generic";
            Assert.AreEqual(!succeeds,result.IsError,result.Exception?.ToString());
            if(!succeeds){Assert.IsNull(result.Result);return;}
            Assert.AreEqual(mode=="two-events" ? 2 : 1,result.Result.Count);
            Assert.AreEqual(result.Result.Count,result.Result.Select(row=>row.TransactionId).Distinct().Count());
            var row=result.Result.First();Assert.AreNotEqual(Guid.Empty,row.TransactionId);
            Assert.AreEqual("0x"+new string('b',64),row.NativeTransactionId);
            Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000).UtcDateTime,row.CreatedDate);
            Assert.AreEqual(mode=="failed" ? "failed" : "succeeded",row.NativeExecutionResult);
            Assert.AreEqual(mode=="generic" ? null : "18446744073709551616",row.RawAmount);
            Assert.AreEqual(mode=="generic" ? "not-interpreted" : "applied-event",row.AmountEvidence);
            Assert.AreEqual(1ul,row.NativeBlockNumber);Assert.AreEqual("0x"+new string('a',64),row.NativeBlockHash);
            Assert.AreEqual(1,row.NativeExtrinsicIndex);Assert.AreEqual(mode=="generic" ? (int?)null : 2,row.NativeEventIndex);
            Assert.AreEqual(mode=="generic" ? null : "balances.Transfer",row.NativeEvent);
            if(mode=="generic")StringAssert.Contains(row.Description,"not a verified zero");
            else Assert.AreEqual(18446744.07370955d,row.Amount,0.00000001d);
        } finally {
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",priorBridge);
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",priorOutcome);
        }
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task InvalidRangeFailsBeforeSdkTransport(bool reversed)
    {
        using var provider=new PolkadotOASIS("http://127.0.0.1:1");PolkadotAssetMutationTests.Activate(provider);
        var result=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress="wallet",FromBlock=reversed ? 2ul : 0ul,ToBlock=reversed ? 1ul : 128ul});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
    }

    private sealed class LegacyTransaction : IWalletTransaction {
        public Guid TransactionId{get;set;}
        public string FromWalletAddress{get;set;}="";
        public string ToWalletAddress{get;set;}="";
        public double Amount{get;set;}
        public string Description{get;set;}="";
        public DateTime CreatedDate{get;set;}
        public Core.Enums.TransactionType TransactionType{get;set;}
        public Core.Enums.TransactionCategory TransactionCategory{get;set;}
    }
}
