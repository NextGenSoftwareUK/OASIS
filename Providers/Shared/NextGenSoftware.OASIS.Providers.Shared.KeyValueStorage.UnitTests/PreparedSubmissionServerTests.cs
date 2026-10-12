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
