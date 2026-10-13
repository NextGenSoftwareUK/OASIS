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
    private readonly AsyncLocal<Func<string, string, PreparedSubmissionServer>> _storagePreparation = new();
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
        => await InvokeStorageMutationAsync("put", key, new { key, value }, cancellationToken);

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => await InvokeStorageMutationAsync("delete", key, new { key }, cancellationToken);

    internal async Task<T> WithStoragePreparationAsync<T>(Func<Task<T>> operation,
        Func<string, string, PreparedSubmissionServer> preparation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(preparation);
        if (_storagePreparation.Value != null) throw new InvalidOperationException("Cosmos storage execution ownership cannot be nested.");
        _storagePreparation.Value = preparation;
        try { return await operation(); }
        finally { _storagePreparation.Value = null; }
    }

    private async Task InvokeStorageMutationAsync(string operation, string key, object args, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var owner = _storagePreparation.Value;
        using var server = owner?.Invoke(operation, key);
        if (owner != null && server == null) throw new InvalidOperationException("Storage owner returned no preparation gate.");
        await InvokeCoreAsync(operation, args, token, server);
    }

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
        // The SDK probe validates contract owner against the configured signer without paid mutation.
    }

    internal Task<JsonElement> InvokeAsync(string operation, object args = null, CancellationToken cancellationToken = default)
        => InvokeCoreAsync(operation, args, cancellationToken, null);
    internal Task<JsonElement> InvokePreparedAsync(string operation, object args, PreparedSubmissionServer server, CancellationToken token)
        => InvokeCoreAsync(operation, args, token, server ?? throw new ArgumentNullException(nameof(server)));
    private async Task<JsonElement> InvokeCoreAsync(string operation, object args, CancellationToken cancellationToken, PreparedSubmissionServer preparationServer)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        var isMutation = operation is "put" or "delete" or "execute" or "transfer" or "upload" or "instantiate";
        if (preparationServer != null && !isMutation) throw new ArgumentException("Preparation gate is only valid for a native mutation.");
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
                if(property.Name is "rpcEndpoint" or "chainId" or "addressPrefix")
                    throw new ArgumentException("SDK arguments cannot override the configured Cosmos network identity.");
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
        start.Environment.Remove("OASIS_NATIVE_PREPARATION_PIPE");
        start.Environment.Remove("OASIS_NATIVE_PREPARATION_MODULE");
        preparationServer?.ConfigureChild(start, Path.Combine(AppContext.BaseDirectory, "CosmosSdkBridge", "prepared-submission.mjs"));
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Cosmos SDK bridge.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        var preparationTask = preparationServer?.ServeAsync(timeout.Token);
        var preparationObserved = false;
        var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
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
            if (preparationTask != null && !preparationTask.IsCompletedSuccessfully)
                throw InvalidEnvelope(true, "Cosmos mutation success lacks completed durable preparation acknowledgement.");
            if (isMutation && (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("transactionHash", out var hash)
                || hash.ValueKind != JsonValueKind.String
                || !System.Text.RegularExpressions.Regex.IsMatch(hash.GetString() ?? "", @"\A[0-9a-fA-F]{64}\z")))
                throw InvalidEnvelope(true, "Cosmos SDK mutation result lacks transaction identity; native outcome is not established.");
            if (preparationServer?.PreparedTransactionId != null
                && !string.Equals(payload.GetProperty("transactionHash").GetString(), preparationServer.PreparedTransactionId, StringComparison.OrdinalIgnoreCase))
                throw new CosmosNativeMutationReconciliationException("Cosmos reply does not match the persisted original identity.", preparationServer.PreparedTransactionId, null);
            if (operation == "upload" && (!payload.TryGetProperty("codeId", out var codeId)
                || codeId.ValueKind != JsonValueKind.Number || !codeId.TryGetInt64(out var numericCodeId) || numericCodeId <= 0)
                || operation == "instantiate" && (!payload.TryGetProperty("contractAddress", out var address)
                || address.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(address.GetString())))
                throw new CosmosNativeMutationReconciliationException(
                    "Cosmos mutation reply retains an original hash but lacks valid deployment result fields; reconcile before retrying.",
                    payload.GetProperty("transactionHash").GetString(), null);
            return payload.Clone();
        }
        catch (InvalidOperationException ex) when (!preparationObserved && preparationServer?.PreparedTransactionId != null)
        {
            var original = preparationServer.PreparedTransactionId;
            if (ex is CosmosNativeMutationReconciliationException native && string.Equals(native.OriginalTransactionId, original, StringComparison.OrdinalIgnoreCase)) throw;
            throw new CosmosNativeMutationReconciliationException("Cosmos process failure after durable preparation; reconcile the original before retrying.", original, null, ex);
        }
        catch (OperationCanceledException ex)
        {
            if (isMutation)
            {
                const string message = "Cosmos native mutation process was interrupted with outcome unverified. Reconcile before retrying; process termination does not cancel a ledger submission.";
                if (cancellationToken.IsCancellationRequested) throw new CosmosNativeMutationCanceledException(message, ex, cancellationToken, preparationServer?.PreparedTransactionId);
                throw new CosmosNativeMutationTimeoutException(message, ex, preparationServer?.PreparedTransactionId);
            }
            throw;
        }
        catch (Exception ex) when (!preparationObserved && isMutation && (ex is IOException || ex is System.Text.DecoderFallbackException))
        { throw new CosmosNativeMutationReconciliationException("Cosmos SDK process I/O failed; native outcome is not established.", preparationServer?.PreparedTransactionId, null, ex); }
        finally
        {
            timeout.Cancel();
            preparationServer?.Dispose();
            if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
            try { await Task.WhenAll(outputTask, errorTask); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is IOException || ex is System.Text.DecoderFallbackException) { }
            if (preparationTask != null)
            {
                try { await preparationTask; }
                catch (OperationCanceledException) { }
                catch (Exception) when (preparationObserved) { }
            }
        }
    }

    private static Exception InvalidEnvelope(bool isMutation, string message, Exception cause = null)
        => isMutation ? new CosmosNativeMutationReconciliationException(message, null, null, cause) : new InvalidDataException(message, cause);

    public void Dispose() => _disposed = true;
}
