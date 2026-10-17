using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class SuiRealPreparationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RecoveredFeeBindsSignedGasOwnerRatherThanAssumingSender(bool foreignPayer)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try{
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet","fixture-chain");
            var attempt=new UsageProviderAttempt{Provider="SuiOASIS",ProviderRequestId="original",NativePreparation=new(){
                ChainId="fixture-chain",Sender="different-transaction-sender",SignedTransactionBase64="AQID",
                Signature=foreignPayer?"fixture-verified-foreign-payer":"fixture-verified"}};
            var result=await provider.GetPreparedNativeFeeReceiptAsync(attempt);
            Assert.AreEqual(foreignPayer,result.IsError);
            if(foreignPayer){Assert.IsInstanceOfType<InvalidDataException>(result.Exception);Assert.AreEqual(JsonValueKind.Undefined,result.Result.ValueKind);}
            else Assert.AreEqual("fixture-owner",result.Result.GetProperty("payer").GetString());
        }finally{Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }
    [TestMethod]
    public async Task PreparedRecoveryRejectsForeignProviderChainAndMissingIdentityBeforeLookup()
    {
        using var provider=new SuiOASIS("http://127.0.0.1:1","localnet","fixture-chain");
        foreach(var attempt in new UsageProviderAttempt?[]{null,
            new(){Provider="CosmosBlockChainOASIS",ProviderRequestId="original",NativePreparation=new(){ChainId="fixture-chain"}},
            new(){Provider="SuiOASIS",ProviderRequestId="original",NativePreparation=new(){ChainId="foreign-chain"}},
            new(){Provider="SuiOASIS",ProviderRequestId="original"},
            new(){Provider="SuiOASIS",NativePreparation=new(){ChainId="fixture-chain"}},
            new(){Provider="SuiOASIS",ProviderRequestId="original",NativePreparation=new(){ChainId="fixture-chain",Sender="0x1",SignedTransactionBase64="AQID",Signature="invalid"}}}){
            var result=await provider.GetPreparedNativeFeeReceiptAsync(attempt!);
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Exception);
            Assert.AreEqual(System.Text.Json.JsonValueKind.Undefined,result.Result.ValueKind);
        }
    }
    [TestMethod]
    public void StorageAndNativeOperationsRetainOneSdkClient()
    {
        using var provider=new SuiOASIS("http://127.0.0.1:1","localnet");
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var backend=typeof(SuiOASIS).GetProperty("Backend",flags)!.GetValue(provider)!;
        Assert.IsInstanceOfType<NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PersistedPrivateKeyValueBackend>(backend);
        backend=backend.GetType().GetField("_inner",flags)!.GetValue(backend)!;
        var storageClient=backend.GetType().GetProperty("Client",flags)!.GetValue(backend);
        var nativeClient=typeof(SuiOASIS).GetField("_sdkClient",flags)!.GetValue(provider);
        Assert.AreSame(storageClient,nativeClient,"Storage composition must not allocate a second signing transport.");
    }
    [TestMethod]
    public async Task MissingPersistedEncryptionKeyFailsBeforeStorageActivation()
    {
        using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",storageEncryptionKeyResolver:()=>null!);
        var result=await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        StringAssert.Contains(result.Message,"separately persisted");
    }
    [DataTestMethod]
    [DataRow("rpcEndpoint")] [DataRow("network")] [DataRow("chainId")]
    public async Task OperationArgumentsCannotReplaceConfiguredNetwork(string field)
    {
        var type=typeof(SuiOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.SuiOASIS.SuiSdkClient")!;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        using var client=(IDisposable)Activator.CreateInstance(type,flags,null,new object[]{"http://127.0.0.1:1","localnet","chain","key","package","storage","currency"},null)!;
        object args=field=="rpcEndpoint"?new{rpcEndpoint="https://untrusted.invalid"}:field=="network"?new{network="mainnet"}:(object)new{chainId="replacement"};
        var operation=(Task<JsonElement>)type.GetMethod("InvokeAsync",flags)!.Invoke(client,new[]{(object)"put",args,CancellationToken.None})!;
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>operation);
    }
    private sealed class OfflineAuthority(string user):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var body=await request.Content!.ReadAsStringAsync(token);
            object response;
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

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RealSignedEvidenceIsVerifiedThroughCSharpWithoutLedgerSubmission(bool aggregateBudget)
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
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"),"Use the exclusive retained owned replica, not a user Mongo service.");
        var database="usage_sui_signed_test_"+Guid.NewGuid().ToString("N");
        var mongo=new MongoDB.Driver.MongoClient(connection);
        var store=new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.MongoUsageOutboxStore(connection,database);
        try {
            await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");
            using var authorityHttp=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(authorityHttp,"https://offline-authority.test","WEB6","test-only-service-key");
            var executor=new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)});
            var execution=await executor.BeginAsync("test-only-bearer",user,"signed-test-idempotency",new string('a',64),"POST /offline-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,
                EstimatedCostUsd=1m,MaximumNativeFeeCostUsd=aggregateBudget?0.000001m:null},deadline.Token);
            var operationId=execution.OperationId;
            if(aggregateBudget){
                var time=DateTime.UtcNow.AddSeconds(-1);
                execution=execution.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(operationId),"SuiOASIS","offline-test-chain","MIST","sui",9,
                    1m,"synthetic-test-price","v1","test-catalogue",time,time,time,time.AddMinutes(5))});
            }
            var output=child.StandardOutput.ReadToEndAsync(deadline.Token);
            var error=child.StandardError.ReadToEndAsync(deadline.Token);
            await child.WaitForExitAsync(deadline.Token);
            Assert.AreEqual(0,child.ExitCode);Assert.AreEqual("",await error);
            using var signed=JsonDocument.Parse(await output);
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet");
            await provider.VerifyOriginalSignedPreparationAsync(signed.RootElement,deadline.Token);
            var bound=JsonSerializer.Deserialize<Dictionary<string,object>>(signed.RootElement.GetRawText())!;
            bound["chainId"]="offline-test-chain";
            bound["gasBudgetMist"]="0"; // Untrusted JSON must not replace the signed 1000-MIST budget.
            using var boundEvidence=JsonDocument.Parse(JsonSerializer.Serialize(bound));
            using var server=provider.CreateNativePreparationServer(execution,"signed-preparation","offline-test-chain",signed.RootElement.GetProperty("sender").GetString()!);
            var serve=server.ServeAsync(deadline.Token);
            using(var pipe=new System.IO.Pipes.NamedPipeClientStream(".",server.Endpoint.Substring(server.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous)){
                await pipe.ConnectAsync(deadline.Token);
                var frame=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=boundEvidence.RootElement})+"\n");
                await pipe.WriteAsync(frame,deadline.Token);await pipe.FlushAsync(deadline.Token);
                using var reader=new StreamReader(pipe);
                using var ack=JsonDocument.Parse(await reader.ReadLineAsync(deadline.Token) ?? throw new InvalidOperationException("Verification acknowledgement absent."));
                Assert.IsTrue(ack.RootElement.GetProperty("persisted").GetBoolean());
                Assert.AreEqual(signed.RootElement.GetProperty("transactionHash").GetString(),ack.RootElement.GetProperty("transactionHash").GetString());
            }
            await serve;
            Assert.AreEqual(signed.RootElement.GetProperty("transactionHash").GetString(),server.PreparedTransactionId);
            var persisted=await store.GetAsync(operationId,deadline.Token);
            Assert.AreEqual(1,persisted.ProviderAttempts.Count);
            Assert.AreEqual(server.PreparedTransactionId,persisted.ProviderAttempts[0].ProviderRequestId);
            Assert.AreEqual(signed.RootElement.GetProperty("signature").GetString(),persisted.ProviderAttempts[0].NativePreparation.Signature);
            Assert.AreEqual(signed.RootElement.GetProperty("signedTransactionBase64").GetString(),persisted.ProviderAttempts[0].NativePreparation.SignedTransactionBase64);
            Assert.IsNull(persisted.ProviderAttempts[0].AcknowledgedAtUtc,"Preparation must not claim ledger acknowledgement.");
            Assert.AreEqual(aggregateBudget?0.000001m:0m,persisted.NativeFeeExposureUsd);
            if(aggregateBudget){
                var maximum=persisted.ProviderAttempts[0].NativePreparation.FeeMaximums.Single();
                Assert.AreEqual("1000",maximum.NativeMaximumUnits);
                Assert.AreEqual("MIST",maximum.Denomination);
                Assert.AreEqual(NativeFeeMaximumCoverage.AllFees,maximum.Coverage);
            }else Assert.IsNull(persisted.ProviderAttempts[0].NativePreparation.FeeMaximums);
            var altered=JsonSerializer.Deserialize<Dictionary<string,object>>(signed.RootElement.GetRawText()) ?? throw new InvalidOperationException("Signed test evidence is absent.");
            altered["signature"]="invalid";
            using var invalid=JsonDocument.Parse(JsonSerializer.Serialize(altered));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.VerifyOriginalSignedPreparationAsync(invalid.RootElement,deadline.Token));
            altered["chainId"]="offline-test-chain";
            using var invalidBound=JsonDocument.Parse(JsonSerializer.Serialize(altered));
            using var rejectedServer=provider.CreateNativePreparationServer(execution,"invalid-signature","offline-test-chain",signed.RootElement.GetProperty("sender").GetString()!);
            var rejectedServe=rejectedServer.ServeAsync(deadline.Token);
            using(var pipe=new System.IO.Pipes.NamedPipeClientStream(".",rejectedServer.Endpoint.Substring(rejectedServer.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous)){
                await pipe.ConnectAsync(deadline.Token);
                var frame=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=invalidBound.RootElement})+"\n");
                await pipe.WriteAsync(frame,deadline.Token);await pipe.FlushAsync(deadline.Token);
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>rejectedServe);
                using var reader=new StreamReader(pipe);
                Assert.IsNull(await reader.ReadLineAsync(deadline.Token),"Rejected signed evidence must not receive an acknowledgement.");
            }
            Assert.IsNull(rejectedServer.PreparedTransactionId);
            if(aggregateBudget){
                Assert.ThrowsException<UsageProtocolException>(()=>provider.CreateNativePreparationServer(execution,"wrong-chain","different-chain",signed.RootElement.GetProperty("sender").GetString()!));
            }else{
            using var wrongChainServer=provider.CreateNativePreparationServer(execution,"wrong-chain","different-chain",signed.RootElement.GetProperty("sender").GetString()!);
            var wrongChainServe=wrongChainServer.ServeAsync(deadline.Token);
            using(var pipe=new System.IO.Pipes.NamedPipeClientStream(".",wrongChainServer.Endpoint.Substring(wrongChainServer.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous)){
                await pipe.ConnectAsync(deadline.Token);
                var frame=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=boundEvidence.RootElement})+"\n");
                await pipe.WriteAsync(frame,deadline.Token);await pipe.FlushAsync(deadline.Token);
                var mismatch=await Assert.ThrowsExceptionAsync<UsageProtocolException>(()=>wrongChainServe);
                Assert.AreEqual("NATIVE_PREPARATION_IDENTITY_MISMATCH",mismatch.Code);
                using var reader=new StreamReader(pipe);
                Assert.IsNull(await reader.ReadLineAsync(deadline.Token));
            }
            Assert.IsNull(wrongChainServer.PreparedTransactionId);
            }
            var afterRejection=await store.GetAsync(operationId,deadline.Token);
            Assert.AreEqual(1,afterRejection.ProviderAttempts.Count);
            Assert.AreEqual(persisted.NativePreparationBytes,afterRejection.NativePreparationBytes);
            Assert.IsFalse(provider.IsProviderActivated);
        } finally {
            try {if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}}
            finally {await mongo.DropDatabaseAsync(database);}
        }
    }
}
