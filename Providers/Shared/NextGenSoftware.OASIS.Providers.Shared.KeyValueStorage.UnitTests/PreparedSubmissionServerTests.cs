using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class PreparedSubmissionServerTests
{
    [TestMethod]
    public void PreparationConfigurationIsRestrictedToOwnedChildEnvironment()
    {
        using var server=new PreparedSubmissionServer((_,_)=>Task.CompletedTask);
        var original=Environment.GetEnvironmentVariable("OASIS_NATIVE_PREPARATION_PIPE");
        var start=new System.Diagnostics.ProcessStartInfo("node"){UseShellExecute=false};
        server.ConfigureChild(start,Path.Combine(AppContext.BaseDirectory,"prepared-submission.mjs"));
        Assert.AreEqual(server.Endpoint,start.Environment["OASIS_NATIVE_PREPARATION_PIPE"]);
        Assert.AreEqual(original,Environment.GetEnvironmentVariable("OASIS_NATIVE_PREPARATION_PIPE"));
        Assert.ThrowsException<ArgumentException>(()=>server.ConfigureChild(start,"relative.mjs"));
        start.UseShellExecute=true;
        Assert.ThrowsException<ArgumentException>(()=>server.ConfigureChild(start,Path.Combine(AppContext.BaseDirectory,"prepared-submission.mjs")));
    }

    private static System.Diagnostics.ProcessStartInfo NodeClient(string endpoint)
    {
        var start=new System.Diagnostics.ProcessStartInfo("node") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
        start.ArgumentList.Add("--input-type=module");start.ArgumentList.Add("-e");
        start.ArgumentList.Add("import {pathToFileURL} from 'node:url'; const {preparationClient}=await import(pathToFileURL(process.argv[1]).href); try {await preparationClient(process.argv[2],5000)({transactionHash:'original',signedTransactionBase64:'AQID'});} catch {process.exitCode=2;}");
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory,"prepared-submission.mjs"));start.ArgumentList.Add(endpoint);
        return start;
    }
    [TestMethod]
    public async Task ParentAcknowledgementFollowsRealMongoPreparedIdentityWrite()
    {
        const string connection="mongodb://127.0.0.1:27017/?serverSelectionTimeoutMS=3000";
        var database="usage_pipe_test_"+Guid.NewGuid().ToString("N");
        var mongo=new MongoDB.Driver.MongoClient(connection);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var store=new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.MongoUsageOutboxStore(connection,database);
        var id=Guid.NewGuid().ToString("N"); var owner=Guid.NewGuid().ToString("N");
        var operation=new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.UsageOutboxOperation {
            Id=id,UserId="owned-test",Service="test",ExecutionOwner=owner,LeaseUntilUtc=DateTime.UtcNow.AddMinutes(1)
        };
        try {
            await store.InitializeAsync(deadline.Token);
            Assert.IsTrue(await store.TryBeginAsync(operation,deadline.Token));
            using var server=new PreparedSubmissionServer(async(evidence,token)=>{
                await store.BeginProviderAttemptAsync(id,owner,new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.UsageProviderAttempt {
                    ClientRequestId="prepared",Provider="CosmosBlockChainOASIS",ProviderRequestId=evidence.GetProperty("transactionHash").GetString(),StartedAtUtc=DateTime.UtcNow
                },token);
            });
            var serving=server.ServeAsync(deadline.Token);
            using var child=System.Diagnostics.Process.Start(NodeClient(server.Endpoint));
            try {
                var output=child.StandardOutput.ReadToEndAsync(deadline.Token);
                var error=child.StandardError.ReadToEndAsync(deadline.Token);
                await serving;await child.WaitForExitAsync(deadline.Token);
                Assert.AreEqual(0,child.ExitCode);Assert.AreEqual("",await output);Assert.AreEqual("",await error);
            } finally {if(!child.HasExited){child.Kill(entireProcessTree:true);await child.WaitForExitAsync();}}
            var persisted=await store.GetAsync(id,deadline.Token);
            Assert.AreEqual(1,persisted.ProviderAttempts.Count);
            Assert.AreEqual("original",persisted.ProviderAttempts[0].ProviderRequestId);
            Assert.IsNull(persisted.ProviderAttempts[0].AcknowledgedAtUtc,"Preparation is not ledger acknowledgement");
            using var repeatedServer=new PreparedSubmissionServer(async(evidence,token)=>{
                await store.BeginProviderAttemptAsync(id,owner,new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.UsageProviderAttempt {
                    ClientRequestId="prepared",Provider="CosmosBlockChainOASIS",ProviderRequestId=evidence.GetProperty("transactionHash").GetString(),StartedAtUtc=DateTime.UtcNow
                },token);
            });
            var repeatedServing=repeatedServer.ServeAsync(deadline.Token);
            using var repeatedChild=System.Diagnostics.Process.Start(NodeClient(repeatedServer.Endpoint));
            try {
                var output=repeatedChild.StandardOutput.ReadToEndAsync(deadline.Token);
                var error=repeatedChild.StandardError.ReadToEndAsync(deadline.Token);
                var conflict=await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Services.Subscriptions.UsageProtocolException>(()=>repeatedServing);
                Assert.AreEqual("PROVIDER_ATTEMPT_ALREADY_STARTED",conflict.Code);
                await repeatedChild.WaitForExitAsync(deadline.Token);
                Assert.AreEqual(2,repeatedChild.ExitCode);Assert.AreEqual("",await output);Assert.AreEqual("",await error);
                Assert.AreEqual(1,(await store.GetAsync(id,deadline.Token)).ProviderAttempts.Count);
            } finally {if(!repeatedChild.HasExited){repeatedChild.Kill(entireProcessTree:true);await repeatedChild.WaitForExitAsync();}}
        } finally {
            // Only this freshly generated owned test database; no shared/user ledger.
            await mongo.DropDatabaseAsync(database);
        }
    }

    [TestMethod]
    public async Task CancelledServerCannotAcknowledgeOrBeReused()
    {
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        var calls=0;
        using var server=new PreparedSubmissionServer((_,_)=>{calls++;return Task.CompletedTask;});
        var cancellation=await Assert.ThrowsExceptionAsync<TaskCanceledException>(()=>server.ServeAsync(cancelled.Token));
        Assert.AreEqual(cancelled.Token,cancellation.CancellationToken);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>server.ServeAsync(CancellationToken.None));
        Assert.AreEqual(0,calls);
    }

    [DataTestMethod]
    [DataRow("wrong-protocol")] [DataRow("missing-identity")] [DataRow("extra-frame")]
    [DataRow("invalid-bytes")] [DataRow("empty-bytes")] [DataRow("noncanonical-bytes")]
    public async Task InvalidFramesNeverReachPersistence(string mode)
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));var calls=0;
        using var server=new PreparedSubmissionServer((_,_)=>{calls++;return Task.CompletedTask;});
        var name=OperatingSystem.IsWindows()?server.Endpoint.Substring(server.Endpoint.LastIndexOf('\\')+1):Path.GetFileName(server.Endpoint).Substring("CoreFxPipe_".Length);
        var serving=server.ServeAsync(deadline.Token);
        using var client=new NamedPipeClientStream(".",name,PipeDirection.InOut,PipeOptions.Asynchronous);
        await client.ConnectAsync(deadline.Token);
        var protocol=mode=="wrong-protocol"?"wrong":"oasis.native-preparation.v1";
        var identity=mode=="missing-identity"?"":"original";
        var encoded=mode=="invalid-bytes"?"!":mode=="empty-bytes"?"":mode=="noncanonical-bytes"?"AQID ":"AQID";
        var frame=JsonSerializer.Serialize(new{protocol,evidence=new{transactionHash=identity,signedTransactionBase64=encoded}})+"\n";
        if(mode=="extra-frame")frame+="{}\n";
        await client.WriteAsync(Encoding.UTF8.GetBytes(frame),deadline.Token);await client.FlushAsync(deadline.Token);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>serving);
        Assert.AreEqual(0,calls);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task RealNodeClientOnlyAcceptsSuccessfulParentPersistence(bool reject)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var calls=0;
        using var server = new PreparedSubmissionServer((evidence, token) => {
            calls++; Assert.AreEqual("AQID", evidence.GetProperty("signedTransactionBase64").GetString());
            if(reject)throw new InvalidOperationException("controlled persistence failure");
            return Task.CompletedTask;
        });
        var serving=server.ServeAsync(deadline.Token);
        using var process=System.Diagnostics.Process.Start(NodeClient(server.Endpoint));
        try {
            var output=process.StandardOutput.ReadToEndAsync(deadline.Token);
            var error=process.StandardError.ReadToEndAsync(deadline.Token);
            if(reject)await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>serving);else await serving;
            await process.WaitForExitAsync(deadline.Token);
            Assert.AreEqual(reject?2:0,process.ExitCode);
            Assert.AreEqual(1,calls);Assert.AreEqual("",await output);Assert.AreEqual("",await error);
        } finally {
            if(!process.HasExited){process.Kill(entireProcessTree:true);await process.WaitForExitAsync();}
        }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task AcknowledgementWaitsForPersistenceAndFailureNeverAcknowledges(bool reject)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var server = new PreparedSubmissionServer(async (evidence, token) => {
            Assert.AreEqual("original", evidence.GetProperty("transactionHash").GetString());
            entered.SetResult(); await release.Task.WaitAsync(token);
            if (reject) throw new InvalidOperationException("controlled persistence failure");
        });
        var name = OperatingSystem.IsWindows() ? server.Endpoint.Substring(server.Endpoint.LastIndexOf('\\') + 1)
            : Path.GetFileName(server.Endpoint).Substring("CoreFxPipe_".Length);
        var serving = server.ServeAsync(deadline.Token);
        using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(deadline.Token);
        using var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen:true) { AutoFlush = true };
        using var reader = new StreamReader(client, new UTF8Encoding(false,true), leaveOpen:true);
        await writer.WriteLineAsync("{\"protocol\":\"oasis.native-preparation.v1\",\"evidence\":{\"transactionHash\":\"original\",\"signedTransactionBase64\":\"AQID\"}}");
        var acknowledgement = reader.ReadLineAsync(deadline.Token).AsTask();
        await entered.Task.WaitAsync(deadline.Token);
        Assert.IsFalse(acknowledgement.IsCompleted, "Unpersisted preparation cannot be acknowledged");
        release.SetResult();
        if (reject) {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => serving);
            Assert.IsNull(await acknowledgement);
        } else {
            var reply = await acknowledgement;
            using var parsed = JsonDocument.Parse(reply);
            Assert.IsTrue(parsed.RootElement.GetProperty("persisted").GetBoolean());
            Assert.AreEqual("original", parsed.RootElement.GetProperty("transactionHash").GetString());
            await serving;
        }
    }
}
