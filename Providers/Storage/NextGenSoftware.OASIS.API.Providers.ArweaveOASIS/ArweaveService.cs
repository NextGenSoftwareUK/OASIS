using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS;

public interface IArweaveService
{
    Task ProbeAsync();
    Task<string> PostTransactionAsync(byte[] data, string contentType, Dictionary<string, string> tags = null);
    Task<byte[]> GetTransactionDataAsync(string txId);
    Task<List<string>> QueryByTagsAsync(Dictionary<string, string> tags);
}

/// <summary>
/// Invokes ArweaveTeam's official arweave-js SDK through a small stdin/stdout bridge.
/// No transaction construction or signing is reimplemented in .NET.
/// </summary>
public sealed class ArweaveService : IArweaveService
{
    private readonly string _walletJson;
    private readonly string _gatewayUrl;
    private readonly string _nodeExecutable;
    private readonly string _sdkBridgePath;
    private readonly bool _mineAfterPost;

    public ArweaveService(
        string walletJson,
        string gatewayUrl = "https://arweave.net",
        string nodeExecutable = null,
        string sdkBridgePath = null,
        bool mineAfterPost = false)
    {
        _walletJson = walletJson;
        _gatewayUrl = gatewayUrl?.TrimEnd('/') ?? "https://arweave.net";
        _nodeExecutable = nodeExecutable
            ?? Environment.GetEnvironmentVariable("OASIS_ARWEAVE_NODE")
            ?? "node";
        _sdkBridgePath = sdkBridgePath
            ?? Environment.GetEnvironmentVariable("OASIS_ARWEAVE_SDK_BRIDGE")
            ?? Path.Combine(AppContext.BaseDirectory, "SdkBridge", "bridge.mjs");
        _mineAfterPost = mineAfterPost
            || string.Equals(Environment.GetEnvironmentVariable("OASIS_ARWEAVE_MINE_AFTER_POST"), "true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task ProbeAsync()
    {
        if (!File.Exists(_sdkBridgePath))
            throw new FileNotFoundException("The official arweave-js SDK bridge was not found. Install its locked npm dependencies before activating ArweaveOASIS.", _sdkBridgePath);
        await InvokeAsync(new { operation = "info", gateway = _gatewayUrl });
    }

    public async Task<string> PostTransactionAsync(byte[] data, string contentType, Dictionary<string, string> tags = null)
    {
        if (string.IsNullOrWhiteSpace(_walletJson))
            throw new InvalidOperationException("An Arweave JWK wallet is required for storage writes.");

        using var wallet = JsonDocument.Parse(_walletJson);
        using var response = await InvokeAsync(new
        {
            operation = "post",
            gateway = _gatewayUrl,
            wallet = wallet.RootElement,
            dataBase64 = Convert.ToBase64String(data ?? Array.Empty<byte>()),
            contentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            tags = tags ?? new Dictionary<string, string>(),
            mineAfterPost = _mineAfterPost
        });
        return RequiredString(response.RootElement, "id");
    }

    public async Task<byte[]> GetTransactionDataAsync(string txId)
    {
        if (string.IsNullOrWhiteSpace(txId))
            return null;
        try
        {
            using var response = await InvokeAsync(new { operation = "get", gateway = _gatewayUrl, id = txId });
            return Convert.FromBase64String(RequiredString(response.RootElement, "dataBase64"));
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<string>> QueryByTagsAsync(Dictionary<string, string> tags)
    {
        using var response = await InvokeAsync(new
        {
            operation = "query",
            gateway = _gatewayUrl,
            tags = tags ?? new Dictionary<string, string>()
        });
        return response.RootElement.GetProperty("ids").EnumerateArray()
            .Select(value => value.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private async Task<JsonDocument> InvokeAsync(object request)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _nodeExecutable,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(_sdkBridgePath);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the official Arweave SDK bridge.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request));
        process.StandardInput.Close();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (string.IsNullOrWhiteSpace(stdout))
            throw new InvalidOperationException($"Arweave SDK bridge returned no response (exit {process.ExitCode}): {stderr}");

        var document = JsonDocument.Parse(stdout);
        if (process.ExitCode != 0 || !document.RootElement.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
        {
            var error = document.RootElement.TryGetProperty("error", out var errorValue) ? errorValue.GetString() : stderr;
            document.Dispose();
            throw new InvalidOperationException($"Arweave SDK operation failed: {error}");
        }
        return document;
    }

    private static string RequiredString(JsonElement element, string property)
    {
        var value = element.GetProperty(property).GetString();
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Arweave SDK response did not contain '{property}'.")
            : value;
    }
}
