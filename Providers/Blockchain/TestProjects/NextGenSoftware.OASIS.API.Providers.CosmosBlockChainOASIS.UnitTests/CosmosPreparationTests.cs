using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using System.Diagnostics;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class CosmosPreparationTests
{
    [TestMethod]
    public async Task PreparedEntryRejectsMissingExecutionAndPreCancellation()
    {
        using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1");
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(()=>provider.ExecutePreparedNativeAsync("put",null!,null!,"attempt","chain","sender"));
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>provider.ExecutePreparedNativeAsync("put",null!,null!,"attempt","chain","sender",cancellation.Token));
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [DataTestMethod]
    [DataRow("prepared")] [DataRow("reject")] [DataRow("prepared-wrong")] [DataRow("prepared-hold")] [DataRow("valid")]
    [DataRow("prepared-foreign-error")] [DataRow("prepared-bad-utf8")] [DataRow("prepared-bad-json")]
    public async Task PreparedProcessRequiresPersistenceAndRetainsOriginal(string mode)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-signer.mjs"));
        try{
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1");
            var backend=typeof(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.KeyValueStorageProviderBase).GetProperty("Backend",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(provider)!;
            var calls=0;var failure=new InvalidOperationException("controlled parent rejection");
            using var server=new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer((_,_)=>{calls++;return mode=="reject"?Task.FromException(failure):Task.CompletedTask;});
            using var cancellation=new CancellationTokenSource();if(mode=="prepared-hold")cancellation.CancelAfter(1000);
            var operation=(Task<JsonElement>)backend.GetType().GetMethod("InvokePreparedAsync",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(backend,new object[]{"put",new{mode=mode=="reject"?"prepared":mode},server,cancellation.Token})!;
            if(mode=="reject")Assert.AreSame(failure,await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>operation));
            else if(mode=="prepared-hold"){
                var error=await Assert.ThrowsExceptionAsync<CosmosNativeMutationCanceledException>(()=>operation);Assert.AreEqual(new string('a',64),error.OriginalTransactionId);Assert.AreEqual(cancellation.Token,error.CancellationToken);
            }else if(mode=="prepared-wrong"||mode=="valid"||mode=="prepared-foreign-error"||mode=="prepared-bad-utf8"||mode=="prepared-bad-json"){
                var error=await Assert.ThrowsExceptionAsync<CosmosNativeMutationReconciliationException>(()=>operation);Assert.IsNull(error.ExecutionSucceeded);Assert.AreEqual(mode=="valid"?null:new string('a',64),error.OriginalTransactionId);
            }else Assert.AreEqual(new string('a',64),(await operation).GetProperty("transactionHash").GetString());
            Assert.AreEqual(mode=="valid"?0:1,calls);
        }finally{Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",previous);}
    }
    [TestMethod]
    public async Task RealOfflineSignedDocumentVerifiesThroughCSharp()
    {
        var bridge=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE") ?? Path.Combine(AppContext.BaseDirectory,"CosmosSdkBridge","bridge.mjs");
        var start=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(bridge)};
        start.ArgumentList.Add("--input-type=module");start.ArgumentList.Add("-e");
        start.ArgumentList.Add("""
            import {DirectSecp256k1Wallet} from '@cosmjs/proto-signing';
            import {SigningCosmWasmClient} from '@cosmjs/cosmwasm';
            import {TxRaw,SignDoc} from 'cosmjs-types/cosmos/tx/v1beta1/tx';
            import {createHash} from 'node:crypto';
            const wallet=await DirectSecp256k1Wallet.fromKey(new Uint8Array(32).fill(7),'wasm');
            const [{address}]=await wallet.getAccounts();
            const original=wallet.signDirect.bind(wallet);let document;
            wallet.signDirect=async(...args)=>{const result=await original(...args);document=result.signed;return result;};
            const client=await SigningCosmWasmClient.offline(wallet);
            const raw=await client.sign(address,[],{amount:[],gas:'100000'},'offline preparation',{chainId:'offline-test-chain',accountNumber:7,sequence:3});
            const bytes=TxRaw.encode(raw).finish();
            process.stdout.write(JSON.stringify({transactionHash:createHash('sha256').update(bytes).digest('hex').toUpperCase(),chainId:'offline-test-chain',sender:address,signedTransactionBase64:Buffer.from(bytes).toString('base64'),signDocBase64:Buffer.from(SignDoc.encode(document).finish()).toString('base64')}));
            """);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var child=Process.Start(start) ?? throw new InvalidOperationException("Cannot start retained Cosmos signing SDK.");
        try{
            var output=child.StandardOutput.ReadToEndAsync(deadline.Token);var error=child.StandardError.ReadToEndAsync(deadline.Token);
            await child.WaitForExitAsync(deadline.Token);Assert.AreEqual(0,child.ExitCode);Assert.AreEqual("",await error);
            using var signed=JsonDocument.Parse(await output);
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1");
            await provider.VerifyOriginalSignedPreparationAsync(signed.RootElement,deadline.Token);
            var altered=JsonSerializer.Deserialize<Dictionary<string,object>>(signed.RootElement.GetRawText())!;altered["chainId"]="foreign-chain";
            using var invalid=JsonDocument.Parse(JsonSerializer.Serialize(altered));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.VerifyOriginalSignedPreparationAsync(invalid.RootElement,deadline.Token));
            Assert.IsFalse(provider.IsProviderActivated);
        }finally{if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}}
    }
    [TestMethod]
    public void FactoryCannotInventAnExecution()
    {
        using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1");
        Assert.ThrowsException<ArgumentNullException>(()=>provider.CreateNativePreparationServer(null!,"attempt","chain","sender"));
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [TestMethod]
    public async Task InvalidSignedEvidenceFailsWithoutActivatingProvider()
    {
        using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1");
        using var invalid=JsonDocument.Parse("{\"transactionHash\":\"invalid\",\"sender\":\"invalid\",\"signedTransactionBase64\":\"AQID\"}");
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.VerifyOriginalSignedPreparationAsync(invalid.RootElement));
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
