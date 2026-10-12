using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class SuiRealPreparationTests
{
    [TestMethod]
    public void PreparationFactoryCannotInventAnAdmittedExecution()
    {
        using var provider=new SuiOASIS("http://127.0.0.1:1","localnet");
        Assert.ThrowsException<ArgumentNullException>(()=>provider.CreateNativePreparationServer(null!,"attempt","chain","sender"));
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task PreparedProviderEntryPointRejectsMissingExecutionAndPreCancellation()
    {
        using var provider=new SuiOASIS("http://127.0.0.1:1","localnet");
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(()=>provider.ExecutePreparedNativeAsync("put",new{key="test",value="test"},null!,"attempt","chain","sender"));
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>provider.ExecutePreparedNativeAsync("put",null!,null!,"attempt","chain","sender",cancelled.Token));
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task RealSignedEvidenceIsVerifiedThroughCSharpWithoutLedgerSubmission()
    {
        var bridge=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE") ?? Path.Combine(AppContext.BaseDirectory,"SuiSdkBridge","bridge.mjs");
        var start=new ProcessStartInfo("node") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(bridge)};
        start.ArgumentList.Add("--input-type=module");start.ArgumentList.Add("-e");
        start.ArgumentList.Add("""
            import {Ed25519Keypair} from '@mysten/sui/keypairs/ed25519';
            import {Transaction} from '@mysten/sui/transactions';
            import {toBase58} from '@mysten/sui/utils';
            const signer=Ed25519Keypair.fromSecretKey(new Uint8Array(32).fill(7));
            const tx=new Transaction();tx.setSender(signer.toSuiAddress());tx.setGasPrice(1);tx.setGasBudget(1000);
            tx.setGasPayment([{objectId:'0x'+'2'.repeat(64),version:'1',digest:toBase58(new Uint8Array(32).fill(1))}]);
            const bytes=await tx.build();const {signature}=await signer.signTransaction(bytes);
            process.stdout.write(JSON.stringify({transactionHash:await tx.getDigest(),sender:signer.toSuiAddress(),signature,signedTransactionBase64:Buffer.from(bytes).toString('base64')}));
            """);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var child=Process.Start(start) ?? throw new InvalidOperationException("Cannot start retained signing SDK.");
        try {
            var output=child.StandardOutput.ReadToEndAsync(deadline.Token);
            var error=child.StandardError.ReadToEndAsync(deadline.Token);
            await child.WaitForExitAsync(deadline.Token);
            Assert.AreEqual(0,child.ExitCode);Assert.AreEqual("",await error);
            using var signed=JsonDocument.Parse(await output);
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet");
            await provider.VerifyOriginalSignedPreparationAsync(signed.RootElement,deadline.Token);
            var verifiedCalls=0;
            using var server=new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer(async(evidence,token)=>{
                await provider.VerifyOriginalSignedPreparationAsync(evidence,token);
                verifiedCalls++;
            });
            var serve=server.ServeAsync(deadline.Token);
            using(var pipe=new System.IO.Pipes.NamedPipeClientStream(".",server.Endpoint.Substring(server.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous)){
                await pipe.ConnectAsync(deadline.Token);
                var frame=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=signed.RootElement})+"\n");
                await pipe.WriteAsync(frame,deadline.Token);await pipe.FlushAsync(deadline.Token);
                using var reader=new StreamReader(pipe);
                using var ack=JsonDocument.Parse(await reader.ReadLineAsync(deadline.Token) ?? throw new InvalidOperationException("Verification acknowledgement absent."));
                Assert.IsTrue(ack.RootElement.GetProperty("persisted").GetBoolean());
                Assert.AreEqual(signed.RootElement.GetProperty("transactionHash").GetString(),ack.RootElement.GetProperty("transactionHash").GetString());
                Assert.AreEqual(1,verifiedCalls);
            }
            await serve;
            Assert.AreEqual(signed.RootElement.GetProperty("transactionHash").GetString(),server.PreparedTransactionId);
            var altered=JsonSerializer.Deserialize<Dictionary<string,object>>(signed.RootElement.GetRawText()) ?? throw new InvalidOperationException("Signed test evidence is absent.");
            altered["signature"]="invalid";
            using var invalid=JsonDocument.Parse(JsonSerializer.Serialize(altered));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.VerifyOriginalSignedPreparationAsync(invalid.RootElement,deadline.Token));
            Assert.IsFalse(provider.IsProviderActivated);
        } finally {if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}}
    }
}
