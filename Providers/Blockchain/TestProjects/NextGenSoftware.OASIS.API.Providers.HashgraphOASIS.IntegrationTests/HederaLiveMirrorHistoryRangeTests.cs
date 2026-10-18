using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public sealed class HederaLiveMirrorHistoryRangeTests
{
    [TestMethod,TestCategory("LiveMirror")]
    public async Task DefaultHistoryFindsRetainedOriginalWithoutStorageActivationOrPayment()
    {
        var provider=new HashgraphOASIS("https://testnet.mirrornode.hedera.com/api/v1/","testnet");
        try{
            var result=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress="0.0.10993779"});
            Assert.IsFalse(result.IsError,result.Message);
            Assert.IsTrue(result.Result.Any(row=>row.NativeTransactionId=="0.0.10993779-1792286952-544962538"));
            Assert.IsFalse(provider.IsProviderActivated);
            Console.WriteLine("Default unbounded public mirror history includes retained original, without storage activation or paid SDK work; records="+result.Result.Count);
        }finally{((HttpClient)typeof(HashgraphOASIS).GetField("_httpClient",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(provider)!).Dispose();}
    }

    [TestMethod,TestCategory("LiveMirror")]
    public async Task ExistingTestnetOriginalUsesRealMirrorBlockRangeWithoutStorageActivationOrPayment()
    {
        const string account="0.0.10993779";
        const string original="0.0.10993779-1792286952-544962538";
        const string timestamp="1792286959.639631104";
        const ulong number=41917811;
        const string endpoint="https://testnet.mirrornode.hedera.com/api/v1/";
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(150));var token=deadline.Token;
        using var http=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){BaseAddress=new Uri(endpoint),Timeout=TimeSpan.FromSeconds(20)};
        using var block=JsonDocument.Parse(await http.GetStringAsync("blocks/"+number,token));
        Assert.AreEqual(number,block.RootElement.GetProperty("number").GetUInt64());
        var interval=block.RootElement.GetProperty("timestamp");
        var from=interval.GetProperty("from").GetString()!;var to=interval.GetProperty("to").GetString()!;
        Assert.IsTrue(decimal.Parse(from,CultureInfo.InvariantCulture)<=decimal.Parse(timestamp,CultureInfo.InvariantCulture));
        Assert.IsTrue(decimal.Parse(timestamp,CultureInfo.InvariantCulture)<=decimal.Parse(to,CultureInfo.InvariantCulture));
        using var before=JsonDocument.Parse(await http.GetStringAsync("transactions/"+original,token));
        var native=before.RootElement.GetProperty("transactions").EnumerateArray().Single(row=>row.GetProperty("transaction_id").GetString()==original
            &&row.GetProperty("consensus_timestamp").GetString()==timestamp);
        var delta=native.GetProperty("transfers").EnumerateArray().Where(row=>row.GetProperty("account").GetString()==account).Sum(row=>row.GetProperty("amount").GetInt64());
        var provider=new HashgraphOASIS(endpoint,"testnet");Assert.IsFalse(provider.IsProviderActivated);
        try{
            var request=new GetWeb3TransactionsRequest{WalletAddress=account,FromBlock=number,ToBlock=number};
            foreach(var result in new[]{provider.GetTransactions(request),await provider.GetTransactionsAsync(request)}){
                Assert.IsFalse(result.IsError,result.Message);
                var record=result.Result.Single(row=>row.NativeTransactionId==original);
                Assert.AreEqual(delta.ToString(CultureInfo.InvariantCulture),record.RawAmount);
                Assert.AreEqual(native.GetProperty("result").GetString(),record.NativeExecutionResult);
                Assert.AreEqual(native.GetProperty("name").GetString(),record.NativeOperation);
            }
            foreach(var bound in new[]{number-1,number+1}){
                var excluded=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress=account,FromBlock=bound,ToBlock=bound});
                Assert.IsFalse(excluded.IsError,excluded.Message);Assert.IsFalse(excluded.Result.Any(row=>row.NativeTransactionId==original));
            }
            var future=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress=account,FromBlock=ulong.MaxValue,ToBlock=ulong.MaxValue});
            Assert.IsFalse(future.IsError,future.Message);Assert.AreEqual(0,future.Result.Count);Assert.IsFalse(provider.IsProviderActivated);
            using var after=JsonDocument.Parse(await http.GetStringAsync("transactions/"+original,token));
            var retained=after.RootElement.GetProperty("transactions").EnumerateArray().Single(row=>row.GetProperty("transaction_id").GetString()==original
                &&row.GetProperty("consensus_timestamp").GetString()==timestamp);
            Assert.AreEqual(native.GetRawText(),retained.GetRawText());
            Console.WriteLine(JsonSerializer.Serialize(new{account,original,block=number,from,to,consensusTimestamp=timestamp,walletDeltaTinybar=delta,
                nativeResult=native.GetProperty("result").GetString(),newNativeSubmissions=0,newPaidQueries=0,storageActivated=false,
                originalMirrorRecordUnchanged=true,mirrorIndexEvidenceOnly=true,hostedAcceptance=false}));
        }finally{((HttpClient)typeof(HashgraphOASIS).GetField("_httpClient",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(provider)!).Dispose();}
    }
}
