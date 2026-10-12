using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using System.Diagnostics;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class CosmosPreparationTests
{
    private sealed class OfflineAuthority(string user):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var body=await request.Content!.ReadAsStringAsync(token);object response;
            if(request.RequestUri!.AbsolutePath.EndsWith("authorize")){
                var input=JsonSerializer.Deserialize<UsageAuthorizationRequest>(body)!;
                response=new SubscriptionAuthorizationResult{Allowed=true,StatusCode=200,UserId=user,OperationId=input.OperationId,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(1)};
            }else if(request.RequestUri.AbsolutePath.EndsWith("start")){
                var input=JsonSerializer.Deserialize<UsageStartRequest>(body)!;
                response=new UsageStartResult{Started=true,OperationId=input.OperationId,Status="executing"};
            }else throw new InvalidOperationException("Unexpected offline authority request.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(response),System.Text.Encoding.UTF8,"application/json")};
        }
    }
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
        const string connection="mongodb://127.0.0.1:27017/?serverSelectionTimeoutMS=3000";
        var database="usage_cosmos_signed_test_"+Guid.NewGuid().ToString("N");
        var mongo=new MongoDB.Driver.MongoClient(connection);
        try{
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only-bearer",user,"cosmos-signed-test",new string('a',64),"POST /offline-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            var output=child.StandardOutput.ReadToEndAsync(deadline.Token);var error=child.StandardError.ReadToEndAsync(deadline.Token);
            await child.WaitForExitAsync(deadline.Token);Assert.AreEqual(0,child.ExitCode);Assert.AreEqual("",await error);
            using var signed=JsonDocument.Parse(await output);
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1");
            await provider.VerifyOriginalSignedPreparationAsync(signed.RootElement,deadline.Token);
            using var server=provider.CreateNativePreparationServer(execution,"signed-attempt","offline-test-chain",signed.RootElement.GetProperty("sender").GetString()!);
            var serving=server.ServeAsync(deadline.Token);
            using(var pipe=new System.IO.Pipes.NamedPipeClientStream(".",server.Endpoint.Substring(server.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous)){
                await pipe.ConnectAsync(deadline.Token);
                var frame=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=signed.RootElement})+"\n");
                await pipe.WriteAsync(frame,deadline.Token);await pipe.FlushAsync(deadline.Token);
                using var reader=new StreamReader(pipe);
                using var ack=JsonDocument.Parse(await reader.ReadLineAsync(deadline.Token) ?? throw new InvalidOperationException("Preparation acknowledgement absent."));
                Assert.IsTrue(ack.RootElement.GetProperty("persisted").GetBoolean());
                Assert.AreEqual(signed.RootElement.GetProperty("transactionHash").GetString(),ack.RootElement.GetProperty("transactionHash").GetString());
            }
            await serving;
            var stored=await store.GetAsync(execution.OperationId,deadline.Token);
            Assert.AreEqual(1,stored.ProviderAttempts.Count);Assert.AreEqual(server.PreparedTransactionId,stored.ProviderAttempts[0].ProviderRequestId);
            Assert.AreEqual(signed.RootElement.GetProperty("signDocBase64").GetString(),stored.ProviderAttempts[0].NativePreparation.SignDocBase64);
            Assert.AreEqual(signed.RootElement.GetProperty("signedTransactionBase64").GetString(),stored.ProviderAttempts[0].NativePreparation.SignedTransactionBase64);
            Assert.IsNull(stored.ProviderAttempts[0].AcknowledgedAtUtc,"Preparation is not ledger acceptance.");
            using var duplicateServer=provider.CreateNativePreparationServer(execution,"signed-attempt","offline-test-chain",signed.RootElement.GetProperty("sender").GetString()!);
            var duplicateServing=duplicateServer.ServeAsync(deadline.Token);
            using(var pipe=new System.IO.Pipes.NamedPipeClientStream(".",duplicateServer.Endpoint.Substring(duplicateServer.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous)){
                await pipe.ConnectAsync(deadline.Token);
                var frame=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=signed.RootElement})+"\n");
                await pipe.WriteAsync(frame,deadline.Token);await pipe.FlushAsync(deadline.Token);
                var conflict=await Assert.ThrowsExceptionAsync<UsageProtocolException>(()=>duplicateServing);
                Assert.AreEqual("PROVIDER_ATTEMPT_ALREADY_STARTED",conflict.Code);
                using var reader=new StreamReader(pipe);Assert.IsNull(await reader.ReadLineAsync(deadline.Token));
            }
            Assert.IsNull(duplicateServer.PreparedTransactionId);
            var afterDuplicate=await store.GetAsync(execution.OperationId,deadline.Token);
            Assert.AreEqual(1,afterDuplicate.ProviderAttempts.Count);
            Assert.AreEqual(stored.NativePreparationBytes,afterDuplicate.NativePreparationBytes);
            var altered=JsonSerializer.Deserialize<Dictionary<string,object>>(signed.RootElement.GetRawText())!;altered["chainId"]="foreign-chain";
            using var invalid=JsonDocument.Parse(JsonSerializer.Serialize(altered));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.VerifyOriginalSignedPreparationAsync(invalid.RootElement,deadline.Token));
            altered["chainId"]="offline-test-chain";altered["signDocBase64"]="AQID";
            using var invalidDocument=JsonDocument.Parse(JsonSerializer.Serialize(altered));
            using var rejectedServer=provider.CreateNativePreparationServer(execution,"invalid-document","offline-test-chain",signed.RootElement.GetProperty("sender").GetString()!);
            var rejectedServing=rejectedServer.ServeAsync(deadline.Token);
            using(var pipe=new System.IO.Pipes.NamedPipeClientStream(".",rejectedServer.Endpoint.Substring(rejectedServer.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous)){
                await pipe.ConnectAsync(deadline.Token);
                var frame=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=invalidDocument.RootElement})+"\n");
                await pipe.WriteAsync(frame,deadline.Token);await pipe.FlushAsync(deadline.Token);
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>rejectedServing);
                using var reader=new StreamReader(pipe);Assert.IsNull(await reader.ReadLineAsync(deadline.Token));
            }
            Assert.IsNull(rejectedServer.PreparedTransactionId);
            var afterRejection=await store.GetAsync(execution.OperationId,deadline.Token);
            Assert.AreEqual(1,afterRejection.ProviderAttempts.Count);
            Assert.AreEqual(stored.NativePreparationBytes,afterRejection.NativePreparationBytes);
            Assert.IsFalse(provider.IsProviderActivated);
        }finally{
            try{if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}}
            finally{await mongo.DropDatabaseAsync(database);}
        }
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
