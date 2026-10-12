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
        var frame=JsonSerializer.Serialize(new{protocol,evidence=new{transactionHash=identity}})+"\n";
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
        var start=new System.Diagnostics.ProcessStartInfo("node") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
        start.ArgumentList.Add("--input-type=module");start.ArgumentList.Add("-e");
        start.ArgumentList.Add("import {pathToFileURL} from 'node:url'; const {preparationClient}=await import(pathToFileURL(process.argv[1]).href); try {await preparationClient(process.argv[2],5000)({transactionHash:'original',signedTransactionBase64:'AQID'});} catch {process.exitCode=2;}");
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory,"prepared-submission.mjs"));start.ArgumentList.Add(server.Endpoint);
        using var process=System.Diagnostics.Process.Start(start);
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
        await writer.WriteLineAsync("{\"protocol\":\"oasis.native-preparation.v1\",\"evidence\":{\"transactionHash\":\"original\"}}");
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
