using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

internal sealed class SuiSdkClient : IDisposable
{
    private readonly string _rpcEndpoint, _network, _chainId, _privateKey, _packageAddress, _storageObjectId, _currencyObjectId;
    private bool _disposed;

    internal SuiSdkClient(string rpcEndpoint, string network, string chainId, string privateKey,
        string packageAddress, string storageObjectId, string currencyObjectId)
    {
        if (!Uri.TryCreate(rpcEndpoint, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException("HTTP(S) Sui RPC endpoint is required.", nameof(rpcEndpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(network);
        _rpcEndpoint = rpcEndpoint;
        _network = network;
        _chainId = chainId;
        _privateKey = privateKey;
        _packageAddress = packageAddress;
        _storageObjectId = storageObjectId;
        _currencyObjectId = currencyObjectId;
    }

    internal Task<JsonElement> InvokeAsync(string operation, object args = null, CancellationToken cancellationToken = default)
        => InvokeCoreAsync(operation, args, cancellationToken, null);

    internal Task<JsonElement> InvokePreparedAsync(string operation, object args, PreparedSubmissionServer server, CancellationToken cancellationToken)
        => InvokeCoreAsync(operation, args, cancellationToken, server ?? throw new ArgumentNullException(nameof(server)));

    private async Task<JsonElement> InvokeCoreAsync(string operation, object args, CancellationToken cancellationToken, PreparedSubmissionServer preparationServer)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        var isMutation = operation is "put" or "delete" or "putMany" or "deleteMany" or "publish" or "publicationResume"
            or "transfer" or "nftMint" or "nftSend" or "nftBurn" or "nftLock" or "nftUnlock"
            or "tokenMint" or "tokenSend" or "tokenBurn" or "tokenLock" or "tokenUnlock";
        if (preparationServer != null && !isMutation) throw new ArgumentException("Preparation gate is only valid for a native mutation.");
        var path = Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE")
            ?? Path.Combine(AppContext.BaseDirectory, "SuiSdkBridge", "bridge.mjs");
        if (!File.Exists(path)) throw new FileNotFoundException("Install the locked official Mysten SDK bridge.", path);
        var request = new Dictionary<string, object>
        {
            ["operation"] = operation, ["rpcEndpoint"] = _rpcEndpoint, ["network"] = _network,
            ["chainId"] = _chainId, ["privateKey"] = _privateKey,
            ["packageAddress"] = _packageAddress, ["storageObjectId"] = _storageObjectId,
            ["currencyObjectId"] = _currencyObjectId
        };
        if (args != null)
            foreach (var property in args.GetType().GetProperties())
            {
                if (property.Name == "operation") throw new ArgumentException("SDK arguments cannot override the authoritative operation.");
                request[property.Name] = property.GetValue(args);
            }
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("OASIS_SUI_NODE") ?? "node")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardInputEncoding = new System.Text.UTF8Encoding(false, true),
            StandardOutputEncoding = new System.Text.UTF8Encoding(false, true),
            StandardErrorEncoding = new System.Text.UTF8Encoding(false, true)
        };
        start.ArgumentList.Add(path);
        start.Environment.Remove("OASIS_NATIVE_PREPARATION_PIPE");
        start.Environment.Remove("OASIS_NATIVE_PREPARATION_MODULE");
        preparationServer?.ConfigureChild(start, Path.Combine(AppContext.BaseDirectory, "SuiSdkBridge", "prepared-submission.mjs"));
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Sui SDK bridge.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        var preparationTask = preparationServer?.ServeAsync(timeout.Token);
        var preparationObserved = false;
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request).AsMemory(), timeout.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);
            if (preparationTask?.IsFaulted == true)
            {
                preparationObserved = true;
                await preparationTask;
            }
            var response = await output;
            await error; // Drain stderr without exposing possibly sensitive SDK diagnostic text.
            if (string.IsNullOrWhiteSpace(response))
                throw InvalidEnvelope(isMutation, $"Sui SDK exited {process.ExitCode} without a response; native outcome is not established.");
            JsonDocument parsed;
            try { parsed = JsonDocument.Parse(response); }
            catch (JsonException ex) { throw InvalidEnvelope(isMutation, "Sui SDK returned invalid JSON; native outcome is not established.", ex); }
            using var envelope = parsed;
            if (envelope.RootElement.ValueKind != JsonValueKind.Object
                || !envelope.RootElement.TryGetProperty("ok", out var ok)
                || ok.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw InvalidEnvelope(isMutation, "Sui SDK response lacks its required boolean outcome; native outcome is not established.");
            if (process.ExitCode != 0 || !envelope.RootElement.GetProperty("ok").GetBoolean())
            {
                if (envelope.RootElement.TryGetProperty("nativeMutation", out var mutation))
                {
                    if (!envelope.RootElement.TryGetProperty("error", out var errorMessage) || errorMessage.ValueKind != JsonValueKind.String)
                        throw new SuiNativeMutationReconciliationException("Malformed Sui native mutation error envelope; reconcile submission before retrying.", null, null,
                            new InvalidDataException("Native mutation error envelope requires a string error message."));
                    var message = errorMessage.GetString();
                    string transactionHash = null;
                    bool? executionSucceeded = null;
                    try
                    {
                        transactionHash = mutation.GetProperty("transactionHash").GetString();
                        if (transactionHash != null && !IsValidDigest(transactionHash))
                        {
                            transactionHash = null;
                            throw new InvalidDataException("Native mutation digest is not a valid 32-byte base58 identity.");
                        }
                        var status = mutation.GetProperty("executionSucceeded");
                        executionSucceeded = status.ValueKind == JsonValueKind.Null ? null : status.GetBoolean();
                        if (transactionHash == null && executionSucceeded.HasValue)
                            throw new InvalidDataException("Native execution outcome requires a valid original digest.");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is KeyNotFoundException || ex is InvalidDataException)
                    {
                        // Declared mutation evidence cannot become retryable when its schema is malformed.
                        throw new SuiNativeMutationReconciliationException(message, transactionHash, null, ex);
                    }
                    throw new SuiNativeMutationReconciliationException(message, transactionHash, executionSucceeded);
                }
                if (!envelope.RootElement.TryGetProperty("error", out var failure) || failure.ValueKind != JsonValueKind.String || ok.GetBoolean())
                    throw InvalidEnvelope(isMutation, "Sui SDK returned an inconsistent error envelope; native outcome is not established.");
                throw new InvalidOperationException(failure.GetString());
            }
            if (!envelope.RootElement.TryGetProperty("result", out var payload))
                throw InvalidEnvelope(isMutation, "Sui SDK success envelope has no result; native outcome is not established.");
            if (preparationTask != null && !preparationTask.IsCompletedSuccessfully)
                throw InvalidEnvelope(true, "Sui mutation success lacks completed durable preparation acknowledgement.");
            if (isMutation && (payload.ValueKind != JsonValueKind.Object
                || !payload.TryGetProperty("transactionHash", out var hash) || hash.ValueKind != JsonValueKind.String
                || !IsValidDigest(hash.GetString())))
                throw InvalidEnvelope(true, "Sui SDK mutation result lacks transaction identity; native outcome is not established.");
            if (preparationServer?.PreparedTransactionId != null
                && payload.GetProperty("transactionHash").GetString() != preparationServer.PreparedTransactionId)
                throw new SuiNativeMutationReconciliationException("Sui reply does not match the durably prepared original identity; reconcile before retrying.",
                    preparationServer.PreparedTransactionId, null);
            return payload.Clone();
        }
        catch (OperationCanceledException ex)
        {
            if (isMutation)
            {
                const string message = "Sui native mutation process was interrupted with outcome unverified and original signed identity unavailable. Reconcile before retrying; process termination does not cancel a ledger submission.";
                if (cancellationToken.IsCancellationRequested) throw new SuiNativeMutationCanceledException(message, ex, cancellationToken);
                throw new SuiNativeMutationTimeoutException(message, ex);
            }
            throw;
        }
        catch (Exception ex) when (isMutation && (ex is IOException || ex is System.Text.DecoderFallbackException))
        {
            throw InvalidEnvelope(true, "Sui SDK process I/O failed; native outcome is not established.", ex);
        }
        finally
        {
            timeout.Cancel();
            preparationServer?.Dispose();
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            // Observe completed readers; the primary cancellation/I/O failure above stays authoritative.
            try { await Task.WhenAll(output, error); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is IOException || ex is System.Text.DecoderFallbackException) { }
            if (preparationTask != null)
            {
                try { await preparationTask; }
                catch (OperationCanceledException) { }
                catch (Exception) when (preparationObserved) { }
            }
        }
    }

    private static bool IsValidDigest(string digest)
    {
        if (string.IsNullOrEmpty(digest) || digest.Length > 44) return false;
        const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        System.Numerics.BigInteger value = 0;
        foreach (var character in digest)
        {
            var digit = alphabet.IndexOf(character);
            if (digit < 0) return false;
            value = value * 58 + digit;
        }
        var leadingZeros = 0;
        while (leadingZeros < digest.Length && digest[leadingZeros] == '1') leadingZeros++;
        return leadingZeros + (value.IsZero ? 0 : value.ToByteArray(isUnsigned: true, isBigEndian: true).Length) == 32;
    }

    private static Exception InvalidEnvelope(bool isMutation, string message, Exception innerException = null)
        => isMutation ? new SuiNativeMutationReconciliationException(message, null, null, innerException)
            : new InvalidDataException(message, innerException);

    public void Dispose() => _disposed = true;
}
