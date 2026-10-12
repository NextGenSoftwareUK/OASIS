using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using System.Diagnostics;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class HederaPreparationTests
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
