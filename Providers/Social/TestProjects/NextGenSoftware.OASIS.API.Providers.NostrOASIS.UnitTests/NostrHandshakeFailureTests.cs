using System.Net;
using System.Net.WebSockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.NostrOASIS.UnitTests;

[TestClass]
public sealed class NostrHandshakeFailureTests
{
    private sealed class Refused(HttpStatusCode status) : HttpMessageHandler
    {
        public int Attempts;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Attempts++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.IsNull(request.Headers.Authorization);
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    [DataTestMethod]
    [DataRow(503)] [DataRow(403)]
    public async Task RefusedHandshakeRetainsTypedStatusAndCauseWithoutRetry(int status)
    {
        using var handler = new Refused((HttpStatusCode)status);
        using var invoker = new HttpMessageInvoker(handler, false);
        using var socket = new ClientWebSocket();
        var error = await Assert.ThrowsExceptionAsync<HttpRequestException>(() =>
            NostrPublicRelayRead.ConnectAsync(socket, invoker, new Uri("wss://example.invalid/"), default));
        Assert.AreEqual((HttpStatusCode)status, error.StatusCode);
        Assert.AreEqual("connect", error.Data["COSMIC.NativeStage"]);
        Assert.IsInstanceOfType(error.InnerException, typeof(WebSocketException));
        Assert.AreEqual(1, handler.Attempts);
    }
}
