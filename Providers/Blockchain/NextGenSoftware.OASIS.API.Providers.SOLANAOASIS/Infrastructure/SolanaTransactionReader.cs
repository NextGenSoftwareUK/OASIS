using System;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SolSharp.Rpc;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure;

/// <summary>
/// Read-only, public SDK transaction options. No signing, fee estimation, retries or
/// submission. Solnet remains the signer until its wire-format migration is verified.
/// </summary>
internal static class SolanaTransactionReader
{
    // Reuse connections across provider instances and operations. Per-operation clients
    // own only their wrapper, never this transport or a caller-supplied test transport.
    private static readonly SocketsHttpHandler SharedTransport = new()
    {
        AllowAutoRedirect = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    };

    internal static async Task<(string Genesis, JsonElement Transaction)> ReadFeeAsync(
        Uri endpoint, string signature, HttpMessageHandler transport, CancellationToken cancellationToken)
    {
        using var client = OpenClient(endpoint, transport);
        var rpc = new SolanaRpcClient(client);
        var genesis = await rpc.GetGenesisHashAsync(cancellationToken);
        var transaction = await rpc.GetTransactionWithOptionsAsync(signature, FinalizedOptions, cancellationToken);
        if (transaction is null || transaction.Value.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Solana original finalized fee receipt is unavailable.");
        return (genesis, transaction.Value);
    }

    private static readonly GetTransactionOptions FinalizedOptions = new()
    {
        Commitment = SolSharp.Core.Primitives.Commitment.Finalized,
        Encoding = RpcTransactionEncoding.Json,
        MaxSupportedTransactionVersion = 1
    };

    private static HttpClient OpenClient(Uri endpoint, HttpMessageHandler transport) =>
        new(new CompletedReadHandler(transport ?? SharedTransport))
        {
            BaseAddress = endpoint,
            Timeout = Timeout.InfiniteTimeSpan
        };

    internal static async Task<Solnet.Rpc.Models.TransactionMetaSlotInfo> ReadAsync(
        Uri endpoint, string signature, HttpMessageHandler transport = null, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        timeout.Token.ThrowIfCancellationRequested();
        using var client = OpenClient(endpoint, transport);
        var transaction = await new SolanaRpcClient(client).GetTransactionWithOptionsAsync(signature, FinalizedOptions, timeout.Token);
        // A successful RPC result:null is absence. RPC/HTTP/format failures throw;
        // callers must never turn those failures into NotFound or partial history.
        return transaction is null ? null : Map(signature, transaction.Value);
    }

    internal static Solnet.Rpc.Models.TransactionMetaSlotInfo Map(string signature, JsonElement native)
    {
        var version = native.GetProperty("version");
        var format = version.ValueKind == JsonValueKind.String && version.GetString() == "legacy" ? -1
            : version.ValueKind == JsonValueKind.Number ? version.GetInt32() : -2;
        if (format is < -1 or > 1 || (format == -1 && version.ValueKind != JsonValueKind.String))
            throw new InvalidDataException("Unsupported Solana transaction version.");
        if (native.GetProperty("slot").GetUInt64() == 0) throw new InvalidDataException("Finalized transaction slot is missing.");
        var transaction = native.GetProperty("transaction");
        var signatures = transaction.GetProperty("signatures").EnumerateArray().Select(item => item.GetString()).ToArray();
        if (signatures.Length == 0 || signatures[0] != signature)
            throw new InvalidDataException("Solana transaction differs from the requested original signature.");
        var message = transaction.GetProperty("message");
        var keys = message.GetProperty("accountKeys").EnumerateArray().Select(item => item.GetString()).ToList();
        var staticCount = keys.Count;
        var header = message.GetProperty("header");
        var required = header.GetProperty("numRequiredSignatures").GetInt32();
        var readonlySigned = header.GetProperty("numReadonlySignedAccounts").GetInt32();
        var readonlyUnsigned = header.GetProperty("numReadonlyUnsignedAccounts").GetInt32();
        if (required != signatures.Length || required > staticCount || readonlySigned < 0 || readonlySigned >= required
            || readonlyUnsigned < 0 || readonlyUnsigned > staticCount - required)
            throw new InvalidDataException("Solana transaction signer/header identities are invalid.");
        var meta = native.GetProperty("meta");
        if (!meta.TryGetProperty("err", out var nativeError)
            || nativeError.ValueKind is not (JsonValueKind.Null or JsonValueKind.String or JsonValueKind.Object))
            throw new InvalidDataException("Solana native execution outcome is missing or malformed.");
        if (meta.TryGetProperty("loadedAddresses", out var loaded))
        {
            var writable = loaded.GetProperty("writable").EnumerateArray().Select(item => item.GetString()).ToArray();
            var readOnly = loaded.GetProperty("readonly").EnumerateArray().Select(item => item.GetString()).ToArray();
            if (format != 0 && (writable.Length != 0 || readOnly.Length != 0))
                throw new InvalidDataException("Only v0 transactions may contain loaded addresses.");
            // Runtime indices refer to static keys, then writable ALT keys, then readonly ALT keys.
            keys.AddRange(writable);keys.AddRange(readOnly);
        }
        else if (format == 0) throw new InvalidDataException("V0 loaded-address evidence is missing.");
        if (keys.Count == 0 || keys.Count > 256 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Count)
            throw new InvalidDataException("Solana transaction account identities are empty, repeated or exceed index capacity.");
        foreach (var key in keys)
            if (string.IsNullOrWhiteSpace(key) || Solnet.Wallet.Utilities.Encoders.Base58.DecodeData(key).Length != 32)
                throw new InvalidDataException("Solana transaction contains an invalid public key.");
        if (format == 1)
        {
            if (message.GetProperty("transactionConfig").ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("V1 transaction configuration is missing.");
            if (message.TryGetProperty("addressTableLookups", out var lookups) && lookups.GetArrayLength() != 0)
                throw new InvalidDataException("V1 transactions do not use address lookup tables.");
        }
        var model = System.Text.Json.JsonSerializer.Deserialize<Solnet.Rpc.Models.TransactionMetaSlotInfo>(native.GetRawText(),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        if (model?.Transaction?.Message?.Instructions == null || model.Meta == null)
            throw new InvalidDataException("Solana native transaction instructions or metadata are missing.");
        if (nativeError.ValueKind != JsonValueKind.Null && model.Meta.Error == null)
            throw new InvalidDataException("Solana native execution error cannot be represented by the consumer model.");
        model.Transaction.Message.AccountKeys = keys.ToArray();
        foreach (var instruction in model.Transaction.Message.Instructions.Concat(
            model.Meta.InnerInstructions?.SelectMany(group => group.Instructions ?? throw new InvalidDataException("Missing inner instructions."))
            ?? Enumerable.Empty<Solnet.Rpc.Models.InstructionInfo>()))
            if (instruction == null || instruction.ProgramIdIndex < 0 || instruction.ProgramIdIndex >= keys.Count
                || instruction.Accounts == null || instruction.Accounts.Any(index => index < 0 || index >= keys.Count))
                throw new InvalidDataException("Solana transaction instruction references an invalid account index.");
        return model;
    }

    internal static async Task<bool> ExistsPendingAsync(Uri endpoint, string signature,
        HttpMessageHandler transport, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        timeout.Token.ThrowIfCancellationRequested();
        using var client = OpenClient(endpoint, transport);
        var statuses = await new SolanaRpcClient(client).GetSignatureStatusesAsync(
            new[] { signature }, searchTransactionHistory: true, cancellationToken: timeout.Token);
        if (statuses == null || statuses.Count != 1)
            throw new InvalidDataException("Solana status lookup did not match the original signature count.");
        var status = statuses[0];
        if (status == null) return false;
        if (status.Slot == 0) throw new InvalidDataException("Solana transaction status has no native slot.");
        return status.ConfirmationStatus switch
        {
            "processed" or "confirmed" => true,
            "finalized" => throw new InvalidDataException("Solana finalized status exists but its original transaction is unavailable."),
            _ => throw new InvalidDataException("Solana transaction confirmation status is missing or unknown.")
        };
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
            throw new HttpRequestException("Solana finalized transaction reads require a completed HTTP 200 response.", null, status);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _transport.Dispose();
            base.Dispose(disposing);
        }
    }
}
