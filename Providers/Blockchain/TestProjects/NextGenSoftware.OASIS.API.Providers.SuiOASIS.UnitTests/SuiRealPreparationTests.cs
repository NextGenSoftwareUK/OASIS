using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class SuiRealPreparationTests
{
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
        const string connection="mongodb://127.0.0.1:27017/?serverSelectionTimeoutMS=3000";
        var database="usage_sui_signed_test_"+Guid.NewGuid().ToString("N");
        var mongo=new MongoDB.Driver.MongoClient(connection);
        var store=new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.MongoUsageOutboxStore(connection,database);
        try {
            await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");
            using var authorityHttp=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(authorityHttp,"https://offline-authority.test","WEB6","test-only-service-key");
            var executor=new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)});
            var execution=await executor.BeginAsync("test-only-bearer",user,"signed-test-idempotency",new string('a',64),"POST /offline-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            var operationId=execution.OperationId;
            var output=child.StandardOutput.ReadToEndAsync(deadline.Token);
            var error=child.StandardError.ReadToEndAsync(deadline.Token);
            await child.WaitForExitAsync(deadline.Token);
            Assert.AreEqual(0,child.ExitCode);Assert.AreEqual("",await error);
            using var signed=JsonDocument.Parse(await output);
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet");
            await provider.VerifyOriginalSignedPreparationAsync(signed.RootElement,deadline.Token);
            var bound=JsonSerializer.Deserialize<Dictionary<string,object>>(signed.RootElement.GetRawText())!;
            bound["chainId"]="offline-test-chain";
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
            var altered=JsonSerializer.Deserialize<Dictionary<string,object>>(signed.RootElement.GetRawText()) ?? throw new InvalidOperationException("Signed test evidence is absent.");
            altered["signature"]="invalid";
            using var invalid=JsonDocument.Parse(JsonSerializer.Serialize(altered));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.VerifyOriginalSignedPreparationAsync(invalid.RootElement,deadline.Token));
            var acceptedInvalid=0;
            using var rejectedServer=new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PreparedSubmissionServer(async(evidence,token)=>{
                await provider.VerifyOriginalSignedPreparationAsync(evidence,token);
                acceptedInvalid++;
            });
            var rejectedServe=rejectedServer.ServeAsync(deadline.Token);
            using(var pipe=new System.IO.Pipes.NamedPipeClientStream(".",rejectedServer.Endpoint.Substring(rejectedServer.Endpoint.LastIndexOf('\\')+1),System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.Asynchronous)){
                await pipe.ConnectAsync(deadline.Token);
                var frame=System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{protocol="oasis.native-preparation.v1",evidence=invalid.RootElement})+"\n");
                await pipe.WriteAsync(frame,deadline.Token);await pipe.FlushAsync(deadline.Token);
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>rejectedServe);
                using var reader=new StreamReader(pipe);
                Assert.IsNull(await reader.ReadLineAsync(deadline.Token),"Rejected signed evidence must not receive an acknowledgement.");
            }
            Assert.AreEqual(0,acceptedInvalid);
            Assert.IsNull(rejectedServer.PreparedTransactionId);
            Assert.AreEqual(1,(await store.GetAsync(operationId,deadline.Token)).ProviderAttempts.Count);
            Assert.IsFalse(provider.IsProviderActivated);
        } finally {
            try {if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}}
            finally {await mongo.DropDatabaseAsync(database);}
        }
    }
}
