using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotLocalAssetTransferTests
{
    private const string First="0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c";
    private const string Second="0x8ee504148e75c34e8f051899b3c6e4241ff18dc1c9211260b6a6a434bedb485f";
    private const string Genesis="0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208";

    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("LocalNative")]
    public async Task OrdinaryAssetTransfersWorkForAdminAndNonAdminHoldersWithExactEffects(bool synchronous)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var journal=Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_ASSET_TRANSFER_JOURNAL")!;
        Assert.IsFalse(string.IsNullOrWhiteSpace(journal));
        Assert.IsFalse(File.Exists(journal),"Preserve/reconcile pending original; never replay.");
        using var provider=new PolkadotOASIS("http://127.0.0.1:19944",Genesis);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(3));
        foreach(var returning in new[]{false,true}) {
            var before=await PolkadotLocalAssetTests.State(deadline.Token);
            var store=DispatchProxy.Create<IUsageOutboxStore,PolkadotLocalNativeTests.FlushedAttemptOwner>();
            var observed=(PolkadotLocalNativeTests.FlushedAttemptOwner)(object)store;
            observed.Journal=journal;
            var constructor=typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
                .Single(value=>value.GetParameters().Length==5);
            var execution=(UsageExecutionHandle)constructor.Invoke(new object?[]{
                new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),ExecutionOwner="local-asset-transfer-owner",Policy=new UsageEndpointPolicy()},
                store,new Web4UsageLedgerOptions(),null,null});
            var sender=returning ? Second : First;
            var recipient=returning ? First : Second;
            var request=new SendWeb3TokenRequest{FromTokenAddress="424242",FromWalletAddress=sender,ToWalletAddress=recipient,
                Amount=returning ? 0.100001m : 0.250001m,OwnerPublicKey=sender,
                OwnerPrivateKey="0x"+string.Concat(Enumerable.Repeat(returning ? "03" : "02",32)),OwnerSeedPhrase=null!};
            // Public local-only seeds. Second is NOT the asset issuer/admin; ordinary holders must be able to transfer.
            var result=await provider.ExecutePreparedNativeOperationAsync(()=>synchronous
                ? Task.FromResult(provider.SendToken(request)) : provider.SendTokenAsync(request),execution,"local-asset-transfer",sender,deadline.Token);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,observed.Calls);
            Assert.AreEqual(observed.Attempt.ProviderRequestId,result.Result.TransactionResult);
            var native=observed.Attempt.NativePreparation;
            var original=JsonSerializer.SerializeToElement(new{chainId=Genesis,sender,transactionHash=observed.Attempt.ProviderRequestId,
                signedTransactionBase64=native.SignedTransactionBase64,signature=native.Signature,signDocBase64=native.SignDocBase64});
            var outcome=await provider.ReadOriginalAssetOutcomeAsync(original,cancellationToken:deadline.Token);
            Assert.IsFalse(outcome.IsError,outcome.Exception?.ToString());Assert.IsTrue(outcome.Result.GetProperty("found").GetBoolean());
            var receipt=outcome.Result.GetProperty("receipt");
            Assert.IsTrue(receipt.GetProperty("finalized").GetBoolean());Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());
            Assert.IsTrue(receipt.GetProperty("assetEffectMatchesRequest").GetBoolean());
            Assert.AreEqual("transfer",receipt.GetProperty("action").GetString());
            Assert.AreEqual(recipient,receipt.GetProperty("recipient").GetString());
            var units=returning ? "100001" : "250001";
            Assert.AreEqual(units,receipt.GetProperty("observedUnits").GetString());
            Assert.AreEqual("frame-transaction-fee-only-not-all-asset-native-outlay",receipt.GetProperty("feeCoverage").GetString());
            var after=await PolkadotLocalAssetTests.State(deadline.Token);
            var firstDelta=BigInteger.Parse(units)*(returning ? 1 : -1);
            Assert.AreEqual(firstDelta,after.Balance-before.Balance);
            Assert.AreEqual(-firstDelta,after.Other-before.Other);
            Assert.AreEqual(before.Supply,after.Supply);
            var nativeOnlyFee=await provider.ReadPreparedNativeFeeEvidenceAsync(observed.Attempt,deadline.Token);
            Assert.IsTrue(nativeOnlyFee.IsError,"Asset fee coverage must not become native-only NetFinal certification.");
            using var pending=JsonDocument.Parse(await File.ReadAllTextAsync(journal,deadline.Token));
            Assert.AreEqual(result.Result.TransactionResult,pending.RootElement.GetProperty("ProviderRequestId").GetString());
            var completed=journal+(synchronous ? ".sync" : ".async")+(returning ? ".return" : ".forward")+".completed.json";
            using(var file=new FileStream(completed,FileMode.Create,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                file.Write(JsonSerializer.SerializeToUtf8Bytes(new{original,receipt,firstDelta=firstDelta.ToString(),supplyUnchanged=true}));file.Flush(true);
            }
            File.Delete(journal); // Only exact confirmed original, now retained in flushed completion evidence.
        }
    }
}
