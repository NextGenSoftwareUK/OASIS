using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Provider = NextGenSoftware.OASIS.API.Providers.MoralisOASIS.MoralisOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS.UnitTests;

[TestClass]
public sealed class MoralisActivationTests
{
    [DataTestMethod]
    [DataRow("/api/v2")]
    [DataRow("/api/v2.2/")]
    public async Task ActivationUsesAuthenticatedVersionRouteAndClearsStateAfterDenial(string basePath)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var provider = new Provider("offline-activation-test", $"http://127.0.0.1:{port}{basePath}");

        try
        {
            var versionResponse = ReplyAsync(listener, "200 OK", "{\"version\":\"2.2\"}", deadline.Token);
            var activated = await provider.ActivateProviderAsync().WaitAsync(deadline.Token);
            var requestHeaders = await versionResponse;
            CollectionAssert.Contains(requestHeaders, $"GET {basePath.TrimEnd('/')}/web3/version HTTP/1.1");
            Assert.IsTrue(requestHeaders.Contains("X-API-Key: offline-activation-test", StringComparer.OrdinalIgnoreCase));
            Assert.IsFalse(activated.IsError);
            Assert.IsTrue(activated.Result);
            Assert.IsTrue(provider.IsProviderActivated);

            var deniedResponse = ReplyAsync(listener, "403 Forbidden", "{}", deadline.Token);
            var denied = await provider.ActivateProviderAsync().WaitAsync(deadline.Token);
            await deniedResponse;
            Assert.IsTrue(denied.IsError);
            Assert.IsFalse(denied.Result);
            Assert.AreEqual("MORALIS_ACTIVATION_HTTP_ERROR", denied.ErrorCode);
            Assert.IsFalse(provider.IsProviderActivated);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    private static async Task<List<string>> ReplyAsync(TcpListener listener, string status, string body, CancellationToken token)
    {
        using var connection = await listener.AcceptTcpClientAsync(token);
        using var stream = connection.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var headers = new List<string>();
        while (await reader.ReadLineAsync(token) is { Length: > 0 } line)
            headers.Add(line);

        var response = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{body}");
        await stream.WriteAsync(response, token);
        return headers;
    }
}
