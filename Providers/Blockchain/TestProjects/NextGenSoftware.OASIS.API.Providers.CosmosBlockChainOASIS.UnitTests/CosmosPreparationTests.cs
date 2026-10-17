using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using System.Diagnostics;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class CosmosPreparationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RecoveryRejectsFeePayerDifferentFromVerifiedSignedAuthority(bool foreignPayer)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-signer.mjs"));
        try{
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1","fixture-chain");
            var attempt=new UsageProviderAttempt{Provider="CosmosBlockChainOASIS",ProviderRequestId=new string('A',64),NativePreparation=new(){
                ChainId="fixture-chain",Sender="different-signer",SignedTransactionBase64="AQID",
                SignDocBase64=foreignPayer?"fixture-verified-foreign-payer":"fixture-verified"}};
            var result=await provider.GetPreparedNativeFeeReceiptAsync(attempt);
            Assert.AreEqual(foreignPayer,result.IsError);
            if(foreignPayer){Assert.IsInstanceOfType<InvalidDataException>(result.Exception);Assert.AreEqual(JsonValueKind.Undefined,result.Result.ValueKind);}
            else Assert.AreEqual("fixture-owner",result.Result.GetProperty("payer").GetString());
        }finally{Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",previous);}
    }
    [TestMethod]
    public async Task AdmittedCancellationReachesDefaultTokenStorageRead()
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(CosmosBlockChainOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.CosmosSdkBackend")!;
        using var backend=(IDisposable)Activator.CreateInstance(type,new object[]{"http://127.0.0.1:1","chain","key","contract","cosmos","0.025uatom","node",Path.Combine(AppContext.BaseDirectory,"missing-owned-bridge.mjs")})!;
        var storage=(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend)backend;
        using var cancellation=new CancellationTokenSource();
        Func<Task<string>> read=()=>{cancellation.Cancel();return storage.GetAsync("owned");};
        Func<string,string,NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer> factory=(_,_)=>throw new AssertFailedException("Read must not create a mutation gate.");
        var scoped=(Task<string>)type.GetMethod("WithStoragePreparationAndCancellationAsync",flags)!.MakeGenericMethod(typeof(string))
            .Invoke(backend,new object[]{read,factory,cancellation.Token})!;
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>scoped);
    }

    [TestMethod]
    public async Task PreparedFeeRecoveryRejectsWrongOrMalformedPersistedAttempt()
    {
        using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1","fixture-chain");
        foreach(var attempt in new UsageProviderAttempt?[]{null,
            new(){Provider="SuiOASIS",ProviderRequestId="original",NativePreparation=new(){ChainId="fixture-chain"}},
            new(){Provider="CosmosBlockChainOASIS",ProviderRequestId="original",NativePreparation=new(){ChainId="foreign"}},
            new(){Provider="CosmosBlockChainOASIS",ProviderRequestId="original"},
            new(){Provider="CosmosBlockChainOASIS",NativePreparation=new(){ChainId="fixture-chain"}},
            new(){Provider="CosmosBlockChainOASIS",ProviderRequestId="original",NativePreparation=new(){ChainId="fixture-chain",Sender="invalid",SignedTransactionBase64="AQID",SignDocBase64="AQID"}}}){
            var result=await provider.GetPreparedNativeFeeReceiptAsync(attempt!);
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Exception);
            Assert.AreEqual(JsonValueKind.Undefined,result.Result.ValueKind);
        }
    }
    [TestMethod]
    public async Task DetachedChildCannotSubmitUsingCompletedStorageOwner()
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(CosmosBlockChainOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.CosmosSdkBackend")!;
        using var backend=(IDisposable)Activator.CreateInstance(type,new object[]{"http://127.0.0.1:1","chain","key","contract","cosmos","0.025uatom","node",Path.Combine(AppContext.BaseDirectory,"Fixtures/nft-signer.mjs")})!;
        var storage=(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend)backend;
        var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? detached=null;var ownerCalls=0;
        Func<Task<string>> action=()=>{
            detached=Task.Run(async()=>{await release.Task;await storage.PutAsync("prepared-storage","fixture");});
            return Task.FromResult("completed");
        };
        Func<string,string,NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer> owner=(_,_)=>{
            ownerCalls++;throw new AssertFailedException("Closed owner cannot create another submission gate");
        };
        var scoped=(Task<string>)type.GetMethod("WithStoragePreparationAsync",flags)!.MakeGenericMethod(typeof(string))
            .Invoke(backend,new object[]{action,owner})!;
        Assert.AreEqual("completed",await scoped);
        release.SetResult();
        var failure=await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>detached!);
        StringAssert.Contains(failure.Message,"already completed");Assert.AreEqual(0,ownerCalls);
    }
    [TestMethod]
    public async Task ConcurrentStorageOwnersStayIsolatedAndNestedOwnerCannotReplaceParent()
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(CosmosBlockChainOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.CosmosSdkBackend")!;
        using var backend=(IDisposable)Activator.CreateInstance(type,new object[]{"http://127.0.0.1:1","chain","key","contract","cosmos","0.025uatom","node",Path.Combine(AppContext.BaseDirectory,"Fixtures/nft-signer.mjs")})!;
        var storage=(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend)backend;
        var method=type.GetMethod("WithStoragePreparationAsync",flags)!.MakeGenericMethod(typeof(string));
        var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered=0;var persisted=new System.Collections.Concurrent.ConcurrentBag<string>();
        Task<string> Scope(Func<Task<string>> action,Func<string,string,NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer> owner)
            => (Task<string>)method.Invoke(backend,new object[]{action,owner})!;
        var operations=new[]{"a","b"}.Select(label=>Scope(async()=>{
            if(Interlocked.Increment(ref entered)==2)ready.SetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>Scope(()=>Task.FromResult("nested"),
                (_,_)=>throw new AssertFailedException("Nested owner must not be invoked")));
            await storage.PutAsync("prepared-storage-"+label,"encrypted fixture");
            return label;
        },(operation,key)=>{
            Assert.AreEqual("put",operation);Assert.AreEqual("prepared-storage-"+label,key);
            return new((_,_)=>{persisted.Add(label);return Task.CompletedTask;});
        })).ToArray();
        CollectionAssert.AreEquivalent(new[]{"a","b"},await Task.WhenAll(operations));
        CollectionAssert.AreEquivalent(new[]{"a","b"},persisted.ToArray());
        await storage.PutAsync("ordinary","fixture");
        Assert.AreEqual(2,persisted.Count);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task StorageScopeUsesExistingPreparationGateAndClearsOwnerAfterFailure(bool reject)
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(CosmosBlockChainOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.CosmosSdkBackend")!;
        using var backend=(IDisposable)Activator.CreateInstance(type,new object[]{"http://127.0.0.1:1","chain","key","contract","cosmos","0.025uatom","node",Path.Combine(AppContext.BaseDirectory,"Fixtures/nft-signer.mjs")})!;
        var calls=0;var observed=new List<string>();var failure=new IOException("controlled storage owner persistence rejection");
        Func<string,string,NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer> owner=(operation,key)=>{
            observed.Add(operation+":"+key);
            return new((_,_)=>{calls++;return reject?Task.FromException(failure):Task.CompletedTask;});
        };
        var storage=(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend)backend;
        Func<Task<string>> action=async()=>{await storage.PutAsync("prepared-storage","encrypted fixture");await storage.DeleteAsync("prepared-storage");return "done";};
        var scoped=(Task<string>)type.GetMethod("WithStoragePreparationAsync",flags)!.MakeGenericMethod(typeof(string))
            .Invoke(backend,new object[]{action,owner})!;
        if(reject)Assert.AreSame(failure,await Assert.ThrowsExceptionAsync<IOException>(()=>scoped));
        else Assert.AreEqual("done",await scoped);
        Assert.AreEqual(reject?1:2,calls);
        CollectionAssert.AreEqual(reject?new[]{"put:prepared-storage"}:new[]{"put:prepared-storage","delete:prepared-storage"},observed);
        await storage.PutAsync("ordinary","fixture");
        Assert.AreEqual(reject?1:2,calls);
    }
    [TestMethod]
    public void StorageAndNativeOperationsRetainOneSdkBackend()
    {
        using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1");
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var storage=typeof(CosmosBlockChainOASIS).GetProperty("Backend",flags)!.GetValue(provider);
        var native=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",flags)!.GetValue(provider);
        Assert.IsInstanceOfType<NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PersistedPrivateKeyValueBackend>(storage);
        var inner=storage!.GetType().GetField("_inner",flags)!.GetValue(storage);
        Assert.AreSame(inner,native,"Encrypted storage must reuse the configured native SDK transport.");
    }
    [TestMethod]
    public async Task StorageActivationInvokesOnlyReadOnlyProbe()
    {
        var type=typeof(CosmosBlockChainOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.CosmosSdkBackend")!;
        using var client=(IDisposable)Activator.CreateInstance(type,new object[]{"http://127.0.0.1:1","chain","controlled-key","contract","cosmos","0.025uatom","node",Path.Combine(AppContext.BaseDirectory,"Fixtures/activation-readonly.mjs")})!;
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await (Task)type.GetMethod("VerifyAsync")!.Invoke(client,new object[]{deadline.Token})!;
    }
    [DataTestMethod]
    [DataRow("rpcEndpoint")] [DataRow("chainId")] [DataRow("addressPrefix")]
    public async Task OperationArgumentsCannotReplaceConfiguredNetwork(string field)
    {
        var type=typeof(CosmosBlockChainOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.CosmosSdkBackend")!;
        using var client=(IDisposable)Activator.CreateInstance(type,new object[]{"http://127.0.0.1:1","chain","key","contract","cosmos","0.025uatom",null!,null!})!;
        object args=field=="rpcEndpoint"?new{rpcEndpoint="https://untrusted.invalid"}:field=="chainId"?new{chainId="replacement"}:(object)new{addressPrefix="other"};
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var operation=(Task<JsonElement>)type.GetMethod("InvokeAsync",flags)!.Invoke(client,new[]{(object)"put",args,CancellationToken.None})!;
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>operation);
    }
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
    [DataRow("reject-io")]
    public async Task PreparedProcessRequiresPersistenceAndRetainsOriginal(string mode)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-signer.mjs"));
        try{
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1");
            var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(provider)!;
            var calls=0;Exception failure=mode=="reject-io" ? new IOException("controlled parent persistence I/O failure") : new InvalidOperationException("controlled parent rejection");
            var rejecting=mode=="reject"||mode=="reject-io";
            using var server=new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer((_,_)=>{calls++;return rejecting?Task.FromException(failure):Task.CompletedTask;});
            using var cancellation=new CancellationTokenSource();if(mode=="prepared-hold")cancellation.CancelAfter(1000);
            var operation=(Task<JsonElement>)backend.GetType().GetMethod("InvokePreparedAsync",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(backend,new object[]{"put",new{mode=rejecting?"prepared":mode},server,cancellation.Token})!;
            if(mode=="reject")Assert.AreSame(failure,await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>operation));
            else if(mode=="reject-io")Assert.AreSame(failure,await Assert.ThrowsExceptionAsync<IOException>(()=>operation));
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
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"),"Owned reusable Mongo required; never use ambient27017.");
        const string database="usage_cosmos_signed_test";
        var mongo=new MongoDB.Driver.MongoClient(connection);
        var passed=false;
        try{
            using(var databases=await mongo.ListDatabaseNamesAsync(cancellationToken:deadline.Token))
                Assert.IsFalse((await MongoDB.Driver.IAsyncCursorExtensions.ToListAsync(databases,deadline.Token)).Contains(database),"Prior signed outbox requires inspection; do not overwrite/delete it.");
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only-bearer",user,"cosmos-signed-test",new string('a',64),"POST /offline-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            var output=child.StandardOutput.ReadToEndAsync(deadline.Token);var error=child.StandardError.ReadToEndAsync(deadline.Token);
            await child.WaitForExitAsync(deadline.Token);Assert.AreEqual(0,child.ExitCode);Assert.AreEqual("",await error);
            using var signed=JsonDocument.Parse(await output);
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1");
            var operationEntered=false;
            foreach(var length in new[]{110,128}){
                await Assert.ThrowsExceptionAsync<ArgumentException>(()=>provider.ExecutePreparedStorageAsync(()=>{
                    operationEntered=true;return Task.FromResult(true);
                },execution,new string('x',length),signed.RootElement.GetProperty("sender").GetString()!,deadline.Token));
            }
            Assert.IsFalse(operationEntered,"Invalid correlation must reject before operation entry/signing.");
            Assert.AreEqual(0,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count);
            Assert.IsTrue(await provider.ExecutePreparedStorageAsync(()=>Task.FromResult(true),execution,new string('x',109),
                signed.RootElement.GetProperty("sender").GetString()!,deadline.Token));
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
            using(var cancelledOwner=new CancellationTokenSource()){
                var backend=(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend)typeof(CosmosBlockChainOASIS)
                    .GetField("_sdkBackend",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(provider)!;
                var canceled=await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>provider.ExecutePreparedStorageAsync(async()=>{
                    cancelledOwner.Cancel();
                    await backend.PutAsync("never-submit-after-owner-cancellation","fixture");
                    return "must not complete";
                },execution,"canceled-storage",signed.RootElement.GetProperty("sender").GetString()!,cancelledOwner.Token));
                Assert.AreEqual(cancelledOwner.Token,canceled.CancellationToken);
                var afterCancellation=await store.GetAsync(execution.OperationId,deadline.Token);
                Assert.AreEqual(1,afterCancellation.ProviderAttempts.Count);
                Assert.AreEqual(stored.NativePreparationBytes,afterCancellation.NativePreparationBytes);
            }
            passed=true;
        }finally{
            try{if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}}
            finally{if(passed)await mongo.DropDatabaseAsync(database);}
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
