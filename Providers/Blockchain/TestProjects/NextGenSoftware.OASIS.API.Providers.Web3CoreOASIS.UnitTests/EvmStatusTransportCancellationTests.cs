using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Providers.Shared.Evm;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public class EvmStatusTransportCancellationTests
{
    private const string Tx = "0x" + "1111111111111111111111111111111111111111111111111111111111111111";

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task OwnedStatusCancellationAbortsHeadersOrBodyAndPreservesConcurrentRequest(bool body)
    {
        using var handler = new Transport(body);
        var sdk = new Web3(new RpcClient(new Uri("http://unused.invalid/"), httpClientHandler: handler));
        using var cancellation = new CancellationTokenSource();
        var running = EvmTransactionStatus.ReadAsync(Tx,
            () => Task.FromResult(new OASISResult<Web3> { Result = sdk }), cancellation.Token);
        try
        {
            await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            // This call shares the actual SDK/HTTP handler but has another async context.
            var concurrent = sdk.Eth.ChainId.SendRequestAsync();
            cancellation.Cancel();
            var result = await running.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(result.IsError);
            Assert.AreEqual(BridgeTransactionStatus.Pending, result.Result);
            Assert.AreEqual(31337, (int)(await concurrent).Value);
            Assert.IsTrue(await handler.Aborted.Task.WaitAsync(TimeSpan.FromSeconds(2)),
                "Underlying transport must observe cancellation, not merely detached waiting.");
            Assert.AreEqual(31337, (int)(await sdk.Eth.ChainId.SendRequestAsync()).Value);
            if (body)
            {
                await handler.Body!.DisposedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.IsTrue(handler.Body.Disposed, "Cancelled response body must be disposed.");
            }
        }
        finally { handler.Release.TrySetResult(); await handler.Completed.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
    }

    [TestMethod]
    public async Task NestedScopeRestoresOuterTokenAndPreCancellationDispatchesNothing()
    {
        using var handler = new Transport(false);
        var sdk = new Web3(new RpcClient(new Uri("http://unused.invalid/"), httpClientHandler: handler));
        using var outer = new CancellationTokenSource();
        using var inner = new CancellationTokenSource();
        using (EvmOperationRpc.Enter(outer.Token))
        {
            using (EvmOperationRpc.Enter(inner.Token))
            {
                inner.Cancel();
                try { await sdk.Eth.ChainId.SendRequestAsync(); Assert.Fail("Pre-cancelled request dispatched."); }
                catch (Exception cause) when (IsSdkCancellation(cause)) { }
                Assert.AreEqual(0, handler.Requests);
            }
            Assert.AreEqual(31337, (int)(await sdk.Eth.ChainId.SendRequestAsync()).Value);
            outer.Cancel();
            try { await sdk.Eth.ChainId.SendRequestAsync(); Assert.Fail("Outer cancellation was lost."); }
            catch (Exception cause) when (IsSdkCancellation(cause)) { }
            Assert.AreEqual(1, handler.Requests);
        }
        Assert.AreEqual(31337, (int)(await sdk.Eth.ChainId.SendRequestAsync()).Value);
    }

    private static bool IsSdkCancellation(Exception cause) => cause is RpcClientTimeoutException ||
        cause is RpcClientUnknownException { InnerException: OperationCanceledException };

    [TestMethod]
    public async Task FailedHttpResponseIsDisposedWithoutDisposingSharedTransport()
    {
        using var handler = new FailureTransport();
        var sdk = new Web3(new RpcClient(new Uri("http://unused.invalid/"), httpClientHandler: handler));
        using var cancellation = new CancellationTokenSource();
        using (EvmOperationRpc.Enter(cancellation.Token))
        {
            try { await sdk.Eth.ChainId.SendRequestAsync(); Assert.Fail("HTTP failure was accepted."); }
            catch (RpcClientUnknownException cause) { Assert.IsInstanceOfType(cause.InnerException, typeof(HttpRequestException)); }
            Assert.IsTrue(handler.Content.Disposed);
            handler.Fail = false;
            Assert.AreEqual(31337, (int)(await sdk.Eth.ChainId.SendRequestAsync()).Value);
        }
    }

    private sealed class FailureTransport : EvmOperationRpc.HttpHandler
    {
        internal bool Fail = true;
        internal readonly RecordingContent Content = new("HTTP failure");
        protected override Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(Fail ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = Content } :
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":\"0x7a69\"}") });
    }

    private sealed class RecordingContent(string value) : StringContent(value)
    {
        internal bool Disposed;
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }

    private sealed class Transport(bool body) : EvmOperationRpc.HttpHandler
    {
        internal readonly TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<bool> Aborted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal SlowContent? Body;
        internal int Requests;

        protected override async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Requests);
            var json = await request.Content!.ReadAsStringAsync(token);
            if (json.Contains("eth_chainId"))
                return new(HttpStatusCode.OK) { Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":\"0x7a69\"}", Encoding.UTF8, "application/json") };
            if (body)
            {
                Body = new SlowContent(this);
                return new(HttpStatusCode.OK) { Content = Body };
            }
            Started.TrySetResult();
            try { await Release.Task.WaitAsync(token); }
            catch (OperationCanceledException) { Aborted.TrySetResult(true); throw; }
            finally { Completed.TrySetResult(); }
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":null}") };
        }
    }

    private sealed class SlowContent(Transport owner) : HttpContent
    {
        internal bool Disposed;
        internal readonly TaskCompletionSource DisposedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken token)
        {
            owner.Started.TrySetResult();
            try { await owner.Release.Task.WaitAsync(token); }
            catch (OperationCanceledException) { owner.Aborted.TrySetResult(true); throw; }
            finally { owner.Completed.TrySetResult(); }
        }
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
            DisposedSignal.TrySetResult();
        }
    }
}
