using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SolSharp.Rpc;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure;

/// <summary>
/// Read-only, public SDK transaction options. No signing, fee estimation, retries or
/// submission. Solnet remains the signer until its wire-format migration is verified.
/// </summary>
internal static class SolanaFinalizedFeeReader
{
    // Reuse connections across provider instances and operations. Per-operation clients
    // own only their wrapper, never this transport or a caller-supplied test transport.
    private static readonly SocketsHttpHandler SharedTransport = new()
    {
        AllowAutoRedirect = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    };

    internal static async Task<(string Genesis, JsonElement Transaction)> ReadAsync(
        Uri endpoint, string signature, HttpMessageHandler transport, CancellationToken cancellationToken)
    {
        using var client = new HttpClient(new CompletedReadHandler(transport ?? SharedTransport))
        {
            BaseAddress = endpoint,
            Timeout = Timeout.InfiniteTimeSpan
        };
        var rpc = new SolanaRpcClient(client);
        var genesis = await rpc.GetGenesisHashAsync(cancellationToken);
        var transaction = await rpc.GetTransactionWithOptionsAsync(signature, new GetTransactionOptions
        {
            Commitment = SolSharp.Core.Primitives.Commitment.Finalized,
            Encoding = RpcTransactionEncoding.Json,
            MaxSupportedTransactionVersion = 1
        }, cancellationToken);
        if (transaction is null || transaction.Value.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Solana original finalized fee receipt is unavailable.");
        return (genesis, transaction.Value);
    }

    private sealed class CompletedReadHandler(HttpMessageHandler transport) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _transport = new(transport, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await _transport.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK) return response;
            var status = response.StatusCode;
            response.Dispose();
            throw new HttpRequestException("Solana finalized fee reads require a completed HTTP 200 response.", null, status);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _transport.Dispose();
            base.Dispose(disposing);
        }
    }
}
