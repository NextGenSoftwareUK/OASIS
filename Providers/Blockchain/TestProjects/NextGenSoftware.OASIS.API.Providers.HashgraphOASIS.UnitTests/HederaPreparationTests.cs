using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using System.Diagnostics;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class HederaPreparationTests
{
    [TestMethod]
    public async Task ActualSignedHederaTransactionVerifiesThroughCSharpWithoutSubmission()
    {
        var bridge=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE")??Path.Combine(AppContext.BaseDirectory,"HederaSdkBridge","bridge.mjs");
        var start=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(bridge)};
        start.ArgumentList.Add("--input-type=module");start.ArgumentList.Add("-e");
        start.ArgumentList.Add("""
            import {PrivateKey,TransferTransaction,AccountId,Hbar,TransactionId} from '@hiero-ledger/sdk';
            const key=PrivateKey.generateECDSA(),other=PrivateKey.generateECDSA();
            const tx=new TransferTransaction().setTransactionId(TransactionId.fromString('0.0.1001@1700000000.000000001'))
              .setNodeAccountIds([AccountId.fromString('0.0.3')]).addHbarTransfer('0.0.1001',Hbar.fromTinybars(-1))
              .addHbarTransfer('0.0.1002',Hbar.fromTinybars(1)).freeze();
            const signed=await tx.sign(key);
            process.stdout.write(JSON.stringify({publicKey:key.publicKey.toStringDer(),otherKey:other.publicKey.toStringDer(),preparation:{transactionHash:tx.transactionId.toString(),sender:'0.0.1001',chainId:'offline-test',signedTransactionBase64:Buffer.from(signed.toBytes()).toString('base64')}}));
            """);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var child=Process.Start(start)??throw new InvalidOperationException("Cannot start retained Hiero SDK.");
        try{
            var output=child.StandardOutput.ReadToEndAsync(deadline.Token);var error=child.StandardError.ReadToEndAsync(deadline.Token);
            await child.WaitForExitAsync(deadline.Token);Assert.AreEqual(0,child.ExitCode);Assert.AreEqual("",await error);
            using var signed=JsonDocument.Parse(await output);var provider=new HashgraphOASIS();
            var previousPipe=Environment.GetEnvironmentVariable("OASIS_NATIVE_PREPARATION_PIPE");
            var previousModule=Environment.GetEnvironmentVariable("OASIS_NATIVE_PREPARATION_MODULE");
            try{
                Environment.SetEnvironmentVariable("OASIS_NATIVE_PREPARATION_PIPE","controlled-unrelated-pipe");
                Environment.SetEnvironmentVariable("OASIS_NATIVE_PREPARATION_MODULE","controlled-unrelated-module");
                await provider.VerifyOriginalSignedPreparationAsync(signed.RootElement.GetProperty("preparation"),signed.RootElement.GetProperty("publicKey").GetString()!,deadline.Token);
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.VerifyOriginalSignedPreparationAsync(signed.RootElement.GetProperty("preparation"),signed.RootElement.GetProperty("otherKey").GetString()!,deadline.Token));
                Assert.AreEqual("controlled-unrelated-pipe",Environment.GetEnvironmentVariable("OASIS_NATIVE_PREPARATION_PIPE"));
            }finally{
                Environment.SetEnvironmentVariable("OASIS_NATIVE_PREPARATION_PIPE",previousPipe);
                Environment.SetEnvironmentVariable("OASIS_NATIVE_PREPARATION_MODULE",previousModule);
            }
            Assert.IsFalse(provider.IsProviderActivated);
        }finally{if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}}
    }
    [TestMethod]
    public async Task PreparationCannotInventExecutionOrTrustedPublicKey()
    {
        var provider=new HashgraphOASIS();
        Assert.ThrowsException<ArgumentNullException>(()=>provider.CreateNativePreparationServer(null!,"attempt","testnet","0.0.1001","key"));
        using var evidence=JsonDocument.Parse("{}");
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>provider.VerifyOriginalSignedPreparationAsync(evidence.RootElement,""));
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
