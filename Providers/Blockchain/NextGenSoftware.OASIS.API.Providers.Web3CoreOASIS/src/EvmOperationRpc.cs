using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Nethereum.RPC.Accounts;

namespace NextGenSoftware.OASIS.Providers.Shared.Evm;

// Source-linked beside EvmTransactionStatus in each adapter. Nethereum retains
// ownership of its HTTP client; a scope cancels one async operation, never a client.
internal static class EvmOperationRpc
{
    private static readonly AsyncLocal<CancellationToken> Current = new();

    internal static Web3 Create(IAccount account, string url) =>
        new(account, new RpcClient(new Uri(url), httpClientHandler: new HttpHandler()));

    internal static IDisposable Enter(CancellationToken token)
    {
        var previous = Current.Value;
        Current.Value = token;
        return new Scope(previous);
    }

    private sealed class Scope(CancellationToken previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }

    internal class HttpHandler : HttpClientHandler
    {
        internal HttpHandler() => MaxConnectionsPerServer = RpcClient.MaximumConnectionsPerServer;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var operation = Current.Value;
            if (!operation.CanBeCanceled)
                return await SendCoreAsync(request, cancellationToken).ConfigureAwait(false);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(operation, cancellationToken);
            linked.Token.ThrowIfCancellationRequested();
            HttpResponseMessage response = null;
            try
            {
                response = await SendCoreAsync(request, linked.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                // HttpClient normally buffers after the handler returns. Complete
                // that read inside the linked-token lifetime, including slow bodies.
                await response.Content.LoadIntoBufferAsync(linked.Token).ConfigureAwait(false);
                return response;
            }
            catch
            {
                response?.Dispose();
                throw;
            }
        }

        protected virtual Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken token) =>
            base.SendAsync(request, token);
    }
}
