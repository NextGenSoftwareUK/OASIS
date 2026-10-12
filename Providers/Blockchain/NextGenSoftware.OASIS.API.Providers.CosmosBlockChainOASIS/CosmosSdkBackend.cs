using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

// All requests use the official CosmJS client. Secrets travel on stdin, never argv.
internal sealed class CosmosSdkBackend : IKeyValueBackend, IDisposable
{
    private readonly string _rpcEndpoint, _chainId, _privateKey, _contractAddress;
    private readonly string _addressPrefix, _gasPrice, _nodeExecutable, _bridgePath;
    private bool _disposed;
    internal string ChainId => _chainId;

    public CosmosSdkBackend(string rpcEndpoint, string chainId, string privateKey, string contractAddress,
        string addressPrefix = "cosmos", string gasPrice = "0.025uatom", string nodeExecutable = null, string bridgePath = null)
    {
        if (string.IsNullOrWhiteSpace(rpcEndpoint) || !Uri.TryCreate(rpcEndpoint, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https")) throw new ArgumentException("HTTP(S) Cosmos RPC endpoint is required.", nameof(rpcEndpoint));
        if (string.IsNullOrWhiteSpace(chainId)) throw new ArgumentException("Expected Cosmos chain ID is required.", nameof(chainId));
        _rpcEndpoint = rpcEndpoint;
        _chainId = chainId;
        _privateKey = privateKey;
        _contractAddress = contractAddress;
        _addressPrefix = addressPrefix;
        _gasPrice = gasPrice;
        _nodeExecutable = nodeExecutable ?? Environment.GetEnvironmentVariable("OASIS_COSMOS_NODE") ?? "node";
        _bridgePath = bridgePath ?? Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE")
            ?? Path.Combine(AppContext.BaseDirectory, "CosmosSdkBridge", "bridge.mjs");
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await InvokeAsync("get", new { key }, cancellationToken);
        return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => await InvokeAsync("put", new { key, value }, cancellationToken);

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => await InvokeAsync("delete", new { key }, cancellationToken);

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        prefix ??= string.Empty;
        var keys = new List<string>();
        string cursor = null;
        while (true)
        {
            var page = await InvokeAsync("list", new { prefix, startAfter = cursor }, cancellationToken);
            var count = 0;
            foreach (var element in page.EnumerateArray())
            {
                var key = element.GetString();
                if (key == null || !key.StartsWith(prefix, StringComparison.Ordinal)
                    || (cursor != null && string.CompareOrdinal(key, cursor) <= 0))
                    throw new InvalidDataException("Cosmos storage returned a non-advancing or out-of-prefix page.");
                keys.Add(key);
                cursor = key;
                count++;
            }
            if (count == 0) return keys;
        }
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_privateKey))
            throw new InvalidOperationException("Cosmos storage requires its owner signing key.");
        await InvokeAsync("probe", null, cancellationToken);
        // Verify actual write authority as well as RPC/query reachability.
        var key = "oasis/probe/" + Guid.NewGuid().ToString("N");
        await PutAsync(key, key, cancellationToken);
        if (await GetAsync(key, cancellationToken) != key)
            throw new InvalidDataException("Cosmos authenticated storage readback did not match the committed value.");
        await DeleteAsync(key, cancellationToken);
    }

    internal async Task<JsonElement> InvokeAsync(string operation, object args = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        var isMutation = operation is "put" or "delete" or "execute" or "transfer" or "upload" or "instantiate";
        if (!File.Exists(_bridgePath))
            throw new FileNotFoundException("Install the locked official CosmJS SDK bridge before activating Cosmos.", _bridgePath);
        var request = new Dictionary<string, object>
        {
            ["operation"] = operation, ["rpcEndpoint"] = _rpcEndpoint, ["chainId"] = _chainId,
            ["privateKey"] = _privateKey, ["contractAddress"] = _contractAddress,
            ["addressPrefix"] = _addressPrefix, ["gasPrice"] = _gasPrice
        };
        if (args != null)
            foreach (var property in args.GetType().GetProperties())
            {
                if (property.Name == "operation") throw new ArgumentException("SDK arguments cannot override the authoritative operation.");
                request[property.Name] = property.GetValue(args);
            }
        var start = new ProcessStartInfo
        {
            FileName = _nodeExecutable, RedirectStandardInput = true, RedirectStandardOutput = true,
            RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true,
            StandardInputEncoding = new System.Text.UTF8Encoding(false, true),
            StandardOutputEncoding = new System.Text.UTF8Encoding(false, true),
            StandardErrorEncoding = new System.Text.UTF8Encoding(false, true)
        };
        start.ArgumentList.Add(_bridgePath);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Cosmos SDK bridge.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request).AsMemory(), timeout.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);
            var output = await outputTask;
            await errorTask;
            if (string.IsNullOrWhiteSpace(output))
                throw InvalidEnvelope(isMutation, $"Cosmos SDK exited {process.ExitCode} without a response; native outcome is not established.");
            JsonDocument parsed;
            try { parsed = JsonDocument.Parse(output); }
            catch (JsonException ex) { throw InvalidEnvelope(isMutation, "Cosmos SDK returned invalid JSON; native outcome is not established.", ex); }
            using var envelope = parsed;
            if (envelope.RootElement.ValueKind != JsonValueKind.Object || !envelope.RootElement.TryGetProperty("ok", out var ok)
                || ok.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw InvalidEnvelope(isMutation, "Cosmos SDK response lacks its required boolean outcome; native outcome is not established.");
            if (!envelope.RootElement.GetProperty("ok").GetBoolean() || process.ExitCode != 0)
            {
                if (envelope.RootElement.TryGetProperty("nativeMutation", out var mutation))
                {
                    if (!envelope.RootElement.TryGetProperty("error", out var errorMessage) || errorMessage.ValueKind != JsonValueKind.String)
                        throw new CosmosNativeMutationReconciliationException("Malformed Cosmos native mutation error envelope; reconcile submission before retrying.", null, null,
                            new InvalidDataException("Native mutation error envelope requires a string error message."));
                    string transactionHash = null;
                    bool? executionSucceeded = null;
                    try
                    {
                        transactionHash = mutation.GetProperty("transactionHash").GetString();
                        if (transactionHash != null && !System.Text.RegularExpressions.Regex.IsMatch(transactionHash, @"\A[0-9a-fA-F]{64}\z"))
                        {
                            transactionHash = null;
                            throw new InvalidDataException("Native mutation hash is not a 32-byte hexadecimal identity.");
                        }
                        var status = mutation.GetProperty("executionSucceeded");
                        executionSucceeded = status.ValueKind == JsonValueKind.Null ? null : status.GetBoolean();
                        if (transactionHash == null && executionSucceeded.HasValue)
                            throw new InvalidDataException("Native execution outcome requires a valid original transaction identity.");
                    }
                    catch (Exception ex) when (ex is InvalidOperationException || ex is KeyNotFoundException || ex is InvalidDataException)
                    {
                        throw new CosmosNativeMutationReconciliationException(errorMessage.GetString(), transactionHash, null, ex);
                    }
                    throw new CosmosNativeMutationReconciliationException(errorMessage.GetString(), transactionHash, executionSucceeded);
                }
                if (!envelope.RootElement.TryGetProperty("error", out var failure) || failure.ValueKind != JsonValueKind.String || ok.GetBoolean())
                    throw InvalidEnvelope(isMutation, "Cosmos SDK returned an inconsistent error envelope; native outcome is not established.");
                throw new InvalidOperationException(failure.GetString());
            }
            if (!envelope.RootElement.TryGetProperty("result", out var payload))
                throw InvalidEnvelope(isMutation, "Cosmos SDK success envelope has no result; native outcome is not established.");
            if (isMutation && (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("transactionHash", out var hash)
                || hash.ValueKind != JsonValueKind.String
                || !System.Text.RegularExpressions.Regex.IsMatch(hash.GetString() ?? "", @"\A[0-9a-fA-F]{64}\z")))
                throw InvalidEnvelope(true, "Cosmos SDK mutation result lacks transaction identity; native outcome is not established.");
            return payload.Clone();
        }
        catch (OperationCanceledException ex)
        {
            if (isMutation)
            {
                const string message = "Cosmos native mutation process was interrupted with outcome unverified and original signed identity unavailable. Reconcile before retrying; process termination does not cancel a ledger submission.";
                if (cancellationToken.IsCancellationRequested) throw new CosmosNativeMutationCanceledException(message, ex, cancellationToken);
                throw new CosmosNativeMutationTimeoutException(message, ex);
            }
            throw;
        }
        catch (Exception ex) when (isMutation && (ex is IOException || ex is System.Text.DecoderFallbackException))
        { throw InvalidEnvelope(true, "Cosmos SDK process I/O failed; native outcome is not established.", ex); }
        finally
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
            try { await Task.WhenAll(outputTask, errorTask); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is IOException || ex is System.Text.DecoderFallbackException) { }
        }
    }

    private static Exception InvalidEnvelope(bool isMutation, string message, Exception cause = null)
        => isMutation ? new CosmosNativeMutationReconciliationException(message, null, null, cause) : new InvalidDataException(message, cause);

    public void Dispose() => _disposed = true;
}
