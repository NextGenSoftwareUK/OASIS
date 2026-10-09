using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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

    internal async Task<JsonElement> InvokeAsync(string operation, object args = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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
            foreach (var property in args.GetType().GetProperties()) request[property.Name] = property.GetValue(args);
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("OASIS_SUI_NODE") ?? "node")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardInputEncoding = new System.Text.UTF8Encoding(false, true),
            StandardOutputEncoding = new System.Text.UTF8Encoding(false, true),
            StandardErrorEncoding = new System.Text.UTF8Encoding(false, true)
        };
        start.ArgumentList.Add(path);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Sui SDK bridge.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        try
        {
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request).AsMemory(), timeout.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);
            var response = await output;
            if (string.IsNullOrWhiteSpace(response))
                throw new InvalidOperationException($"Sui SDK exited {process.ExitCode} without a response: {await error}");
            using var envelope = JsonDocument.Parse(response);
            if (process.ExitCode != 0 || !envelope.RootElement.GetProperty("ok").GetBoolean())
                throw new InvalidOperationException(envelope.RootElement.GetProperty("error").GetString());
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
