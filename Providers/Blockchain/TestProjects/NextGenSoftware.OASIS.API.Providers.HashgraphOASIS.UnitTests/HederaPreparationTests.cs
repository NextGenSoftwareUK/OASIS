using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using System.Diagnostics;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class HederaPreparationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    [DataRow(false,true)] [DataRow(true,true)]
    [DataRow(false,false,true)] [DataRow(false,true,true)]
    public async Task SignedEscrowStagesPersistExactEvidenceWithDistinctCorrelations(bool skipApproval,bool fungible=false,bool rejectMutation=false)
    {
        var bridge=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE")??Path.Combine(AppContext.BaseDirectory,"HederaSdkBridge","bridge.mjs");
        var start=new ProcessStartInfo("node"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(bridge)};
        start.ArgumentList.Add("--input-type=module");start.ArgumentList.Add("-e");
        start.ArgumentList.Add("""
            import {PrivateKey,AccountId} from '@hiero-ledger/sdk';
            import {escrowTransaction,escrowApprovalTransaction,tokenEscrowTransaction,tokenEscrowApprovalTransaction} from './escrow.mjs';
            const key=PrivateKey.generateECDSA();
            const request={sender:'0.0.1001',contractId:'0.0.2001',tokenId:'0.0.3001',serial:'1',gas:400000,
              maxFeeTinybars:'100000000',runtimeCodeHash:'0x'+'11'.repeat(32),transactionId:'0.0.1001@1700000000.000000001',
              approvalTransactionId:'0.0.1001@1700000000.000000002',amountUnits:'9007199254740993'};
            const evidence=[];
            const stages=process.argv[1]==='token'
              ?[tokenEscrowApprovalTransaction(request),tokenEscrowTransaction('tokenEscrowLock',request)]
              :[escrowApprovalTransaction(request),escrowTransaction('nftEscrowLock',request)];
            for(const tx of stages){
              tx.setNodeAccountIds([AccountId.fromString('0.0.3')]).freeze();
              const signed=await tx.sign(process.argv[2]==='reject'&&evidence.length===1?PrivateKey.generateECDSA():key);
              evidence.push({transactionHash:tx.transactionId.toString(),sender:request.sender,chainId:'offline-test',signedTransactionBase64:Buffer.from(signed.toBytes()).toString('base64')});
            }
            process.stdout.write(JSON.stringify({publicKey:key.publicKey.toStringDer(),evidence}));
            """);
        start.ArgumentList.Add(fungible?"token":"nft");
        start.ArgumentList.Add(rejectMutation?"reject":"accept");
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var child=Process.Start(start)??throw new InvalidOperationException("Cannot start retained SDK.");
        const string connection="mongodb://127.0.0.1:27017/?serverSelectionTimeoutMS=3000";
        var database="usage_hedera_escrow_test_"+Guid.NewGuid().ToString("N");var mongo=new MongoDB.Driver.MongoClient(connection);
        try{
            var output=child.StandardOutput.ReadToEndAsync(deadline.Token);var error=child.StandardError.ReadToEndAsync(deadline.Token);
            await child.WaitForExitAsync(deadline.Token);Assert.AreEqual(0,child.ExitCode);Assert.AreEqual("",await error);
            using var signed=JsonDocument.Parse(await output);var evidence=signed.RootElement.GetProperty("evidence");
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only-bearer",user,"hedera-escrow-test",new string('a',64),"POST /offline-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            using var server=new HashgraphOASIS().CreateEscrowPreparationServer(execution,"escrow","offline-test","0.0.1001",
                signed.RootElement.GetProperty("publicKey").GetString()!,evidence[0].GetProperty("transactionHash").GetString()!,evidence[1].GetProperty("transactionHash").GetString()!);
            var serving=server.ServeAsync(deadline.Token);
            long approvalBytes=0;
            for(var stage=skipApproval?1:0;stage<2;stage++){
                using var pipe=new System.IO.Pipes.NamedPipeClientStream(".",server.Endpoint.Substring(server.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous);
                await pipe.ConnectAsync(deadline.Token);
                await pipe.WriteAsync(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=evidence[stage]})+"\n"),deadline.Token);
                await pipe.FlushAsync(deadline.Token);using var reader=new StreamReader(pipe);
                if(rejectMutation&&stage==1){
                    await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>serving);
                    Assert.IsNull(await reader.ReadLineAsync(deadline.Token));
                    var rejected=await store.GetAsync(execution.OperationId,deadline.Token);
                    Assert.AreEqual(1,rejected.ProviderAttempts.Count);Assert.AreEqual(approvalBytes,rejected.NativePreparationBytes);
                    Assert.AreEqual(evidence[0].GetProperty("transactionHash").GetString(),server.PreparedTransactionId);
                    Assert.AreEqual(1,server.PreparedTransactionIds.Count);break;
                }
                using var ack=JsonDocument.Parse(await reader.ReadLineAsync(deadline.Token)??throw new InvalidOperationException("Missing acknowledgement."));
                Assert.AreEqual(evidence[stage].GetProperty("transactionHash").GetString(),ack.RootElement.GetProperty("transactionHash").GetString());
                var current=await store.GetAsync(execution.OperationId,deadline.Token);
                Assert.AreEqual(stage+1-(skipApproval?1:0),current.ProviderAttempts.Count);
                if(stage==0)approvalBytes=current.NativePreparationBytes;
            }
            if(!rejectMutation)await serving;var stored=await store.GetAsync(execution.OperationId,deadline.Token);
            for(var index=0;index<stored.ProviderAttempts.Count;index++){
                var stage=index+(skipApproval?1:0);var attempt=stored.ProviderAttempts[index];
                Assert.AreEqual(stage==0?"escrow:approval":"escrow:mutation",attempt.ClientRequestId);
                Assert.AreEqual(evidence[stage].GetProperty("transactionHash").GetString(),attempt.ProviderRequestId);
                Assert.AreEqual(evidence[stage].GetProperty("signedTransactionBase64").GetString(),attempt.NativePreparation.SignedTransactionBase64);
                Assert.IsNull(attempt.AcknowledgedAtUtc,"Signed preparation is not ledger acceptance.");
            }
        }finally{
            try{if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}}
            finally{await mongo.DropDatabaseAsync(database);}
        }
    }
    [DataTestMethod]
    [DataRow("nftEscrowLock",false)] [DataRow("nftEscrowLock",true)]
    [DataRow("tokenEscrowLock",false)] [DataRow("tokenEscrowLock",true)]
    public async Task PreparedEscrowChildAcknowledgesExactOptionalSequence(string action,bool skipApproval)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","prepared.mjs"));
        try{
            const string original="0.0.1001@1700000000.000000001",approval="0.0.1001@1700000000.000000002";
            var ids=new System.Collections.Generic.List<string>();
            using var server=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer.ForOptionalApproval((evidence,token)=>{
                ids.Add(evidence.GetProperty("transactionHash").GetString()!);return Task.CompletedTask;
            },approval,original);
            var client=typeof(HashgraphOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.HederaSdkClient")!;
            var method=client.GetMethod("InvokePreparedAsync",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;
            var operation=(Task<JsonElement>)method.Invoke(null,new object[]{action,new{transactionId=original,approvalTransactionId=approval,mode=skipApproval?"skip-approval":"accept"},server,CancellationToken.None})!;
            Assert.AreEqual(original,(await operation).GetProperty("transactionId").GetString());
            CollectionAssert.AreEqual(skipApproval?new[]{original}:new[]{approval,original},ids.ToArray());
        }finally{Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",previous);}
    }
    [TestMethod]
    public async Task PreparedEntryRejectsMissingExecutionAndPreCancellation()
    {
        var provider=new HashgraphOASIS();
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(()=>provider.ExecutePreparedNativeAsync("transfer",new{},null!,"attempt","testnet","0.0.1001","key"));
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>provider.ExecutePreparedNativeAsync("transfer",new{},null!,"attempt","testnet","0.0.1001","key",cancellation.Token));
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [DataTestMethod]
    [DataRow("accept")] [DataRow("reject")] [DataRow("wrong")] [DataRow("hold")]
    [DataRow("no-ack")] [DataRow("bad-utf8")] [DataRow("bad-json")]
    [DataRow("generic-error")]
    [DataRow("foreign-preparation")]
    [DataRow("accept","nftEscrowUnlock")] [DataRow("accept","tokenEscrowUnlock")]
    [DataRow("reject","nftEscrowUnlock")] [DataRow("reject","tokenEscrowUnlock")]
    public async Task PreparedChildLifecycle(string mode,string action="transfer")
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","prepared.mjs"));
        try{
            var client=typeof(HashgraphOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.HederaSdkClient")!;
            var method=client.GetMethod("InvokePreparedAsync",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;
            var calls=0;var failure=new InvalidOperationException("controlled parent persistence rejection");
            using var server=new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer((_,_)=>{calls++;return mode=="reject"?Task.FromException(failure):Task.CompletedTask;},"0.0.1001@1700000000.000000001");
            using var cancellation=new CancellationTokenSource();if(mode=="hold")cancellation.CancelAfter(1000);
            const string original="0.0.1001@1700000000.000000001";
            var operation=(Task<JsonElement>)method.Invoke(null,new object[]{action,new{transactionId=original,mode},server,cancellation.Token})!;
            if(mode=="reject")Assert.AreSame(failure,await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>operation));
            else if(mode=="foreign-preparation")await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>operation);
            else if(mode=="wrong"||mode=="no-ack"||mode=="bad-utf8"||mode=="bad-json"||mode=="generic-error"){
                var error=await Assert.ThrowsExceptionAsync<HederaNativeMutationReconciliationException>(()=>operation);
                Assert.AreEqual(original,error.OriginalTransactionId);
            }
            else if(mode=="hold"){
                var error=await Assert.ThrowsExceptionAsync<HederaNativeMutationCanceledException>(()=>operation);Assert.AreEqual(cancellation.Token,error.CancellationToken);
            }else Assert.AreEqual(original,(await operation).GetProperty("transactionId").GetString());
            Assert.AreEqual(mode=="no-ack"||mode=="foreign-preparation"?0:1,calls);
        }finally{Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",previous);}
    }
    [TestMethod]
    public async Task PreparedSdkRejectsReadonlyAndEscrowBeforeParentPersistence()
    {
        var client=typeof(HashgraphOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.HederaSdkClient")!;
        var method=client.GetMethod("InvokePreparedAsync",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;
        foreach(var action in new[]{"verifyNativePreparation","nftEscrowLock","tokenEscrowLock"}){
            var calls=0;using var server=new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer((_,_)=>{calls++;return Task.CompletedTask;});
            var operation=(Task<JsonElement>)method.Invoke(null,new object[]{action,new{transactionId="0.0.1001@1700000000.000000001"},server,CancellationToken.None})!;
            await Assert.ThrowsExceptionAsync<ArgumentException>(()=>operation);Assert.AreEqual(0,calls);
        }
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
                var input=JsonSerializer.Deserialize<UsageStartRequest>(body)!;response=new UsageStartResult{Started=true,OperationId=input.OperationId,Status="executing"};
            }else throw new InvalidOperationException("Unexpected offline authority request.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(response),System.Text.Encoding.UTF8,"application/json")};
        }
    }
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
        const string connection="mongodb://127.0.0.1:27017/?serverSelectionTimeoutMS=3000";
        var database="usage_hedera_signed_test_"+Guid.NewGuid().ToString("N");var mongo=new MongoDB.Driver.MongoClient(connection);
        try{
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only-bearer",user,"hedera-signed-test",new string('a',64),"POST /offline-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            var output=child.StandardOutput.ReadToEndAsync(deadline.Token);var error=child.StandardError.ReadToEndAsync(deadline.Token);
            await child.WaitForExitAsync(deadline.Token);Assert.AreEqual(0,child.ExitCode);Assert.AreEqual("",await error);
            using var signed=JsonDocument.Parse(await output);var provider=new HashgraphOASIS();
            var previousPipe=Environment.GetEnvironmentVariable("OASIS_NATIVE_PREPARATION_PIPE");
            var previousModule=Environment.GetEnvironmentVariable("OASIS_NATIVE_PREPARATION_MODULE");
            try{
                Environment.SetEnvironmentVariable("OASIS_NATIVE_PREPARATION_PIPE","controlled-unrelated-pipe");
                Environment.SetEnvironmentVariable("OASIS_NATIVE_PREPARATION_MODULE","controlled-unrelated-module");
                await provider.VerifyOriginalSignedPreparationAsync(signed.RootElement.GetProperty("preparation"),signed.RootElement.GetProperty("publicKey").GetString()!,deadline.Token);
                using var server=provider.CreateNativePreparationServer(execution,"signed-attempt","offline-test","0.0.1001",signed.RootElement.GetProperty("publicKey").GetString()!);
                var serving=server.ServeAsync(deadline.Token);
                using(var pipe=new System.IO.Pipes.NamedPipeClientStream(".",server.Endpoint.Substring(server.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous)){
                    await pipe.ConnectAsync(deadline.Token);
                    var frame=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=signed.RootElement.GetProperty("preparation")})+"\n");
                    await pipe.WriteAsync(frame,deadline.Token);await pipe.FlushAsync(deadline.Token);
                    using var reader=new StreamReader(pipe);
                    using var ack=JsonDocument.Parse(await reader.ReadLineAsync(deadline.Token)??throw new InvalidOperationException("Parent acknowledgement absent."));
                    Assert.IsTrue(ack.RootElement.GetProperty("persisted").GetBoolean());
                    Assert.AreEqual(signed.RootElement.GetProperty("preparation").GetProperty("transactionHash").GetString(),ack.RootElement.GetProperty("transactionHash").GetString());
                    Assert.AreEqual(1,(await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts.Count);
                }
                await serving;
                Assert.AreEqual(signed.RootElement.GetProperty("preparation").GetProperty("transactionHash").GetString(),server.PreparedTransactionId);
                var stored=await store.GetAsync(execution.OperationId,deadline.Token);
                Assert.AreEqual(server.PreparedTransactionId,stored.ProviderAttempts[0].ProviderRequestId);
                Assert.AreEqual(signed.RootElement.GetProperty("preparation").GetProperty("signedTransactionBase64").GetString(),stored.ProviderAttempts[0].NativePreparation.SignedTransactionBase64);
                Assert.IsNull(stored.ProviderAttempts[0].AcknowledgedAtUtc,"Preparation is not consensus acceptance.");
                using var duplicateServer=provider.CreateNativePreparationServer(execution,"signed-attempt","offline-test","0.0.1001",signed.RootElement.GetProperty("publicKey").GetString()!);
                var duplicateServing=duplicateServer.ServeAsync(deadline.Token);
                using(var pipe=new System.IO.Pipes.NamedPipeClientStream(".",duplicateServer.Endpoint.Substring(duplicateServer.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous)){
                    await pipe.ConnectAsync(deadline.Token);
                    var frame=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=signed.RootElement.GetProperty("preparation")})+"\n");
                    await pipe.WriteAsync(frame,deadline.Token);await pipe.FlushAsync(deadline.Token);
                    var conflict=await Assert.ThrowsExceptionAsync<UsageProtocolException>(()=>duplicateServing);
                    Assert.AreEqual("PROVIDER_ATTEMPT_ALREADY_STARTED",conflict.Code);
                    using var reader=new StreamReader(pipe);Assert.IsNull(await reader.ReadLineAsync(deadline.Token));
                }
                Assert.IsNull(duplicateServer.PreparedTransactionId);
                var afterDuplicate=await store.GetAsync(execution.OperationId,deadline.Token);
                Assert.AreEqual(1,afterDuplicate.ProviderAttempts.Count);
                Assert.AreEqual(stored.NativePreparationBytes,afterDuplicate.NativePreparationBytes);
                using var rejectedServer=provider.CreateNativePreparationServer(execution,"wrong-key-attempt","offline-test","0.0.1001",signed.RootElement.GetProperty("otherKey").GetString()!);
                var rejectedServing=rejectedServer.ServeAsync(deadline.Token);
                using(var pipe=new System.IO.Pipes.NamedPipeClientStream(".",rejectedServer.Endpoint.Substring(rejectedServer.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous)){
                    await pipe.ConnectAsync(deadline.Token);
                    var frame=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=signed.RootElement.GetProperty("preparation")})+"\n");
                    await pipe.WriteAsync(frame,deadline.Token);await pipe.FlushAsync(deadline.Token);
                    await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>rejectedServing);
                    using var reader=new StreamReader(pipe);Assert.IsNull(await reader.ReadLineAsync(deadline.Token));
                }
                Assert.IsNull(rejectedServer.PreparedTransactionId);
                var afterRejection=await store.GetAsync(execution.OperationId,deadline.Token);
                Assert.AreEqual(1,afterRejection.ProviderAttempts.Count);
                Assert.AreEqual(stored.NativePreparationBytes,afterRejection.NativePreparationBytes);
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.VerifyOriginalSignedPreparationAsync(signed.RootElement.GetProperty("preparation"),signed.RootElement.GetProperty("otherKey").GetString()!,deadline.Token));
                Assert.AreEqual("controlled-unrelated-pipe",Environment.GetEnvironmentVariable("OASIS_NATIVE_PREPARATION_PIPE"));
            }finally{
                Environment.SetEnvironmentVariable("OASIS_NATIVE_PREPARATION_PIPE",previousPipe);
                Environment.SetEnvironmentVariable("OASIS_NATIVE_PREPARATION_MODULE",previousModule);
            }
            Assert.IsFalse(provider.IsProviderActivated);
        }finally{
            try{if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}}
            finally{await mongo.DropDatabaseAsync(database);}
        }
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
