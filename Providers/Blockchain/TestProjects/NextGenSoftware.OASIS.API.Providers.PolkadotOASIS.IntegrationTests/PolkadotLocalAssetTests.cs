using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotLocalAssetTests
{
    private const string Sender="0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c";
    private const string Genesis="0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208";
    private static readonly Guid Avatar=Guid.Parse("da06227d-87d7-4c71-a6ef-c88ded5e7201");

    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("LocalNative")]
    public async Task OrdinaryMintAndBurnHaveExactFinalizedBalanceAndSupplyEffects(bool synchronous)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var journal=Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_ASSET_API_JOURNAL")!;
        Assert.IsFalse(string.IsNullOrWhiteSpace(journal));
        Assert.IsFalse(File.Exists(journal),"Preserve/reconcile pending original; never replay.");
        using var provider=new LocalWalletProvider();
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(3));
        foreach(var burn in new[]{false,true}) {
            var before=await State(deadline.Token);
            var store=DispatchProxy.Create<IUsageOutboxStore,PolkadotLocalNativeTests.FlushedAttemptOwner>();
            var observed=(PolkadotLocalNativeTests.FlushedAttemptOwner)(object)store;
            observed.Journal=journal;
            var constructor=typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
                .Single(value=>value.GetParameters().Length==5);
            var execution=(UsageExecutionHandle)constructor.Invoke(new object?[]{
                new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),ExecutionOwner="local-asset-owner",Policy=new UsageEndpointPolicy()},
                store,new Web4UsageLedgerOptions(),null,null});
            var mint=new MintWeb3TokenRequest{MintedByAvatarId=Avatar,TokenAddress="424242",ToWalletAddress=Sender,Amount=1.250001m};
            var remove=new BurnWeb3TokenRequest{BurntByAvatarId=Avatar,TokenAddress="424242",Amount=0.500001m,
                OwnerPublicKey=Sender,OwnerPrivateKey="0x"+string.Concat(Enumerable.Repeat("02",32)),OwnerSeedPhrase=null!};
            // Public local-only key. Test default-wallet adapter does NOT prove Mongo/authorization/decryption.
            var result=await provider.ExecutePreparedNativeOperationAsync(()=>burn
                ? (synchronous ? Task.FromResult(provider.BurnToken(remove)) : provider.BurnTokenAsync(remove))
                : (synchronous ? Task.FromResult(provider.MintToken(mint)) : provider.MintTokenAsync(mint)),
                execution,"local-asset-api",Sender,deadline.Token);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());
            Assert.AreEqual(1,observed.Calls);
            Assert.AreEqual(observed.Attempt.ProviderRequestId,result.Result.TransactionResult);
            var native=observed.Attempt.NativePreparation;
            var original=JsonSerializer.SerializeToElement(new{chainId=Genesis,sender=Sender,transactionHash=observed.Attempt.ProviderRequestId,
                signedTransactionBase64=native.SignedTransactionBase64,signature=native.Signature,signDocBase64=native.SignDocBase64});
            var outcome=await provider.ReadOriginalAssetOutcomeAsync(original,cancellationToken:deadline.Token);
            Assert.IsFalse(outcome.IsError,outcome.Exception?.ToString());
            Assert.IsTrue(outcome.Result.GetProperty("found").GetBoolean());
            var receipt=outcome.Result.GetProperty("receipt");
            Assert.IsTrue(receipt.GetProperty("finalized").GetBoolean());
            Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());
            Assert.IsTrue(receipt.GetProperty("assetEffectMatchesRequest").GetBoolean());
            var units=burn ? "500001" : "1250001";
            Assert.AreEqual(units,receipt.GetProperty("observedUnits").GetString());
            Assert.AreEqual("frame-transaction-fee-only-not-all-asset-native-outlay",receipt.GetProperty("feeCoverage").GetString());
            var after=await State(deadline.Token);
            var delta=BigInteger.Parse(units)*(burn ? -1 : 1);
            Assert.AreEqual(delta,after.Balance-before.Balance);
            Assert.AreEqual(delta,after.Supply-before.Supply);
            using var pending=JsonDocument.Parse(await File.ReadAllTextAsync(journal,deadline.Token));
            Assert.AreEqual(result.Result.TransactionResult,pending.RootElement.GetProperty("ProviderRequestId").GetString());
            var completed=journal+(synchronous ? ".sync" : ".async")+(burn ? ".burn" : ".mint")+".completed.json";
            using(var file=new FileStream(completed,FileMode.Create,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                file.Write(JsonSerializer.SerializeToUtf8Bytes(new{original,receipt,observedDelta=delta.ToString()}));file.Flush(true);
            }
            File.Delete(journal); // Exact confirmed original retained in flushed completion evidence; all failures retain pending.
        }
        Assert.AreEqual(1,provider.WalletLoads);
    }

    internal static async Task<(BigInteger Balance,BigInteger Supply,BigInteger Other)> State(CancellationToken token) {
        var bridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE")!;
        var start=new ProcessStartInfo("node"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true,
            StandardOutputEncoding=new UTF8Encoding(false,true),StandardErrorEncoding=new UTF8Encoding(false,true)};
        start.ArgumentList.Add(Path.Combine(Path.GetDirectoryName(bridge)!,"read-local-asset-state.mjs"));
        using var process=Process.Start(start)!;
        var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try {
            await process.WaitForExitAsync(deadline.Token);
            Assert.AreEqual(0,process.ExitCode,await stderr);
            using var state=JsonDocument.Parse(await stdout);
            return(BigInteger.Parse(state.RootElement.GetProperty("units").GetString()!),BigInteger.Parse(state.RootElement.GetProperty("supply").GetString()!),
                BigInteger.Parse(state.RootElement.GetProperty("otherUnits").GetString()!));
        } finally {
            if(!process.HasExited)process.Kill(true);
            await process.WaitForExitAsync();await Task.WhenAll(stdout,stderr);
        }
    }

    private sealed class LocalWalletProvider : PolkadotOASIS {
        public int WalletLoads;
        public LocalWalletProvider():base("http://127.0.0.1:19944",Genesis){}
        protected override Task<OASISResult<IProviderWallet>> ResolveAssetMintWalletAsync(Guid avatarId) {
            WalletLoads++;
            return Task.FromResult(new OASISResult<IProviderWallet>{Result=new ProviderWallet{AvatarId=avatarId,
                ProviderType=Core.Enums.ProviderType.PolkadotOASIS,WalletAddress=Sender,PublicKey=Sender,
                PrivateKey="0x"+string.Concat(Enumerable.Repeat("02",32))}});
        }
    }
}
