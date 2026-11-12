using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS.UnitTests;

[TestClass]
public sealed class ConvexResponseDeadlineTests
{
    [TestMethod]
    public async Task StalledVerificationBodyTimesOutAndNextProbeCanComplete()
    {
        using var testDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var backend = new ConvexRecordBackend($"http://127.0.0.1:{port}", "test-key", http);

        var first = backend.VerifyAsync();
        using (var socket = await listener.AcceptTcpClientAsync(testDeadline.Token))
        {
            await ReplyAsync(socket, "", contentLength: 100, testDeadline.Token);
            var completed = await Task.WhenAny(first, Task.Delay(TimeSpan.FromSeconds(5), testDeadline.Token));
            Assert.AreSame(first, completed, "Receiving response headers must not disable the body deadline.");
            try
            {
                await first;
                Assert.Fail("The incomplete response must time out.");
            }
            catch (OperationCanceledException) when (first.IsCanceled)
            {
                // The caller supplied no cancellation token to the first probe.
            }
        }

        var second = backend.VerifyAsync(testDeadline.Token);
        using var recovered = await listener.AcceptTcpClientAsync(testDeadline.Token);
        const string body = """{"status":"success","value":{"protocol":"oasis-private-records-v1"}}""";
        await ReplyAsync(recovered, body, Encoding.UTF8.GetByteCount(body), testDeadline.Token);
        await second;
    }

    private static async Task ReplyAsync(TcpClient socket, string body, int contentLength, CancellationToken token)
    {
        var stream = socket.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        string? line;
        do
        {
            line = await reader.ReadLineAsync(token);
        } while (!string.IsNullOrEmpty(line));

        var response = $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {contentLength}\r\nConnection: close\r\n\r\n{body}";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(response), token);
    }
}
