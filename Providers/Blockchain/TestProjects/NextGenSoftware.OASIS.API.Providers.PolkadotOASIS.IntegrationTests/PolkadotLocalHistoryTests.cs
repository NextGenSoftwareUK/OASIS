using System.Globalization;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotLocalHistoryTests
{
    [DataTestMethod][DataRow("native")][DataRow("transfer")][DataRow("mint")][DataRow("burn")][TestCategory("LocalNative")]
    public async Task OrdinarySyncAndAsyncHistoryFindExistingFinalizedEventsWithoutSigning(string kind)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var directory=Path.GetDirectoryName(Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE_JOURNAL"))!;
        var file=kind switch {
            "native"=>"local-native-pending.json.async.completed.json",
            "transfer"=>"local-asset-transfer-pending.json.async.return.completed.json",
            "mint"=>"local-asset-api-pending.json.async.mint.completed.json",
            _=>"local-asset-api-pending.json.async.burn.completed.json" };
        using var retained=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory,file)));
        var receipt=retained.RootElement.GetProperty("receipt");
        var transaction=receipt.GetProperty("transactionHash").GetString();
        var height=ulong.Parse(receipt.GetProperty("blockNumber").GetString()!,CultureInfo.InvariantCulture);
        const string first="0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c";
        const string second="0x8ee504148e75c34e8f051899b3c6e4241ff18dc1c9211260b6a6a434bedb485f";
        using var provider=new PolkadotOASIS("http://127.0.0.1:19944","0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        // Native and returning-asset cases deliberately read the recipient, not the extrinsic signer.
        var request=new GetWeb3TransactionsRequest{WalletAddress=kind=="native" ? second : first,FromBlock=height,ToBlock=height};
        var asynchronous=await provider.GetTransactionsAsync(request);
        Assert.IsFalse(asynchronous.IsError,asynchronous.Exception?.ToString());
        var synchronous=provider.GetTransactions(request);
        Assert.IsFalse(synchronous.IsError,synchronous.Exception?.ToString());
        var row=asynchronous.Result.Single(item=>item.NativeTransactionId==transaction&&item.RawAmount!=null);
        var repeated=synchronous.Result.Single(item=>item.NativeTransactionId==transaction&&item.RawAmount!=null);
        Assert.AreEqual(row.TransactionId,repeated.TransactionId);Assert.AreNotEqual(Guid.Empty,row.TransactionId);
        Assert.AreEqual("succeeded",row.NativeExecutionResult);Assert.IsNull(row.NativeError);
        Assert.IsTrue(row.CreatedDate>DateTime.UnixEpoch);Assert.AreEqual(DateTimeKind.Utc,row.CreatedDate.Kind);
        Assert.AreEqual(kind=="native" ? "1000000000001" : receipt.GetProperty("observedUnits").GetString(),row.RawAmount);
        Assert.AreEqual(kind=="native" ? "native-base-unit" : "asset:424242:base-unit",row.AmountUnit);
        Assert.AreEqual(kind=="native" ? null : "424242",row.Token);
        Assert.AreEqual(kind=="native" ? 1.000000000001d : kind=="transfer" ? 0.100001d : kind=="mint" ? 1.250001d : 0.500001d,row.Amount,0.000000000001d);
        Assert.AreEqual(kind=="mint" ? "" : kind=="transfer" ? second : first,row.FromWalletAddress);
        Assert.AreEqual(kind=="burn" ? "" : kind=="native" ? second : first,row.ToWalletAddress);
        Assert.AreEqual(Core.Enums.ProviderType.PolkadotOASIS,row.ProviderType);
        Assert.AreEqual("applied-event",row.AmountEvidence);Assert.AreEqual(height,row.NativeBlockNumber);
        Assert.AreEqual(receipt.GetProperty("blockHash").GetString(),row.NativeBlockHash);
        Assert.IsTrue(row.NativeEventIndex>=0);Assert.IsTrue(row.NativeExtrinsicIndex>=0);
        Assert.AreEqual(kind=="native" ? "balances.Transfer" : kind=="transfer" ? "assets.Transferred" : kind=="mint" ? "assets.Issued" : "assets.Burned",row.NativeEvent);
        if(kind=="native") {
            // Read-only regression of the shared dispatch/fee helper against the retained actual native original.
            var fee=await provider.ReadFinalizedNativeReceiptAsync(receipt.GetProperty("blockHash").GetString()!,transaction!);
            Assert.IsFalse(fee.IsError,fee.Exception?.ToString());Assert.IsTrue(fee.Result.GetProperty("dispatchSucceeded").GetBoolean());
            Assert.AreEqual(receipt.GetProperty("actualFeeUnits").GetString(),fee.Result.GetProperty("actualFeeUnits").GetString());
        }
    }
}
