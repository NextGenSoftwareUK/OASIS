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
            ?? Path.Combine(AppContext.BaseDirectory, "SdkBridge", "bridge.mjs");
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!File.Exists(_bridgePath))
            throw new FileNotFoundException("Install the locked official CosmJS SDK bridge before activating Cosmos.", _bridgePath);
        var request = new Dictionary<string, object>
        {
            ["operation"] = operation, ["rpcEndpoint"] = _rpcEndpoint, ["chainId"] = _chainId,
            ["privateKey"] = _privateKey, ["contractAddress"] = _contractAddress,
            ["addressPrefix"] = _addressPrefix, ["gasPrice"] = _gasPrice
        };
        if (args != null)
            foreach (var property in args.GetType().GetProperties()) request[property.Name] = property.GetValue(args);
        var start = new ProcessStartInfo
        {
            FileName = _nodeExecutable, RedirectStandardInput = true, RedirectStandardOutput = true,
            RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };
        start.ArgumentList.Add(_bridgePath);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Cosmos SDK bridge.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        try
        {
            var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request).AsMemory(), timeout.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);
            var output = await outputTask;
            var error = await errorTask;
            if (string.IsNullOrWhiteSpace(output))
                throw new InvalidOperationException($"Cosmos SDK exited {process.ExitCode} without a response: {error}");
            using var envelope = JsonDocument.Parse(output);
            if (!envelope.RootElement.GetProperty("ok").GetBoolean() || process.ExitCode != 0)
                throw new InvalidOperationException(envelope.RootElement.TryGetProperty("error", out var failure)
                    ? failure.GetString() : $"Cosmos SDK exited {process.ExitCode}.");
            return envelope.RootElement.GetProperty("result").Clone();
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
    }

    public void Dispose() => _disposed = true;
}
