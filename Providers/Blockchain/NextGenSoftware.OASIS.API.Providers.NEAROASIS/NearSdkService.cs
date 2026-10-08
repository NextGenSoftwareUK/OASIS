using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

internal sealed class NearSdkService
{
    private readonly string _rpcEndpoint;
    private readonly string _networkId;
    private readonly string _contractId;
    private readonly string _accountId;
    private readonly string _privateKey;
    private readonly string _nodeExecutable;
    private readonly string _bridgePath;

    public NearSdkService(string rpcEndpoint, string networkId, string contractId, string accountId, string privateKey,
        string nodeExecutable = null, string bridgePath = null)
    {
        _rpcEndpoint = rpcEndpoint;
        _networkId = networkId;
        _contractId = contractId;
        _accountId = accountId;
        _privateKey = privateKey;
        _nodeExecutable = nodeExecutable ?? Environment.GetEnvironmentVariable("OASIS_NEAR_NODE") ?? "node";
        _bridgePath = bridgePath ?? Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE")
            ?? Path.Combine(AppContext.BaseDirectory, "SdkBridge", "bridge.mjs");
    }

    public async Task ProbeAsync()
    {
        if (!File.Exists(_bridgePath))
            throw new FileNotFoundException("The official near-api-js SDK bridge was not found. Install its locked npm dependencies before activating NEAROASIS.", _bridgePath);
        using var _ = await InvokeAsync("probe");
    }

    public async Task<string> GetAsync(string key)
    {
        using var response = await InvokeAsync("get", new { key });
        return response.RootElement.ValueKind == JsonValueKind.Null ? null : response.RootElement.GetString();
    }

    public async Task<IReadOnlyList<KeyValuePair<string, string>>> EntriesAsync(string prefix)
    {
        using var response = await InvokeAsync("entries", new { prefix = prefix ?? string.Empty });
        return response.RootElement.EnumerateArray()
            .Select(pair => new KeyValuePair<string, string>(pair[0].GetString(), pair[1].GetString()))
            .ToList();
    }

    public async Task<string> PutAsync(string key, string value)
    {
        EnsureWritable();
        using var response = await InvokeAsync("put", new { key, value });
        return response.RootElement.GetProperty("transactionHash").GetString();
    }

    public async Task<string> DeleteAsync(string key)
    {
        EnsureWritable();
        using var response = await InvokeAsync("delete", new { key });
        return response.RootElement.GetProperty("transactionHash").GetString();
    }

    public async Task<NearKeyPair> GenerateKeyPairAsync()
    {
        using var response = await InvokeAsync("generateKey");
        return new NearKeyPair(
            response.RootElement.GetProperty("publicKey").GetString(),
            response.RootElement.GetProperty("privateKey").GetString(),
            response.RootElement.GetProperty("implicitAccountId").GetString());
    }

    public async Task<NearKeyPair> DerivePublicKeyAsync(string secretKey)
    {
        using var response = await InvokeAsync("derivePublicKey", new { secretKey });
        return new NearKeyPair(
            response.RootElement.GetProperty("publicKey").GetString(),
            secretKey,
            response.RootElement.GetProperty("implicitAccountId").GetString());
    }

    public async Task<string> GetBalanceYoctoAsync(string accountId)
    {
        using var response = await InvokeAsync("balance", new { targetAccountId = accountId });
        return response.RootElement.GetProperty("amount").GetString();
    }

    public async Task<string> TransferAsync(string receiverId, string amountYocto,
        string signerAccountId = null, string signerPrivateKey = null)
    {
        EnsureWritable(signerAccountId, signerPrivateKey);
        using var response = await InvokeAsync("transfer", new
        {
            receiverId,
            amountYocto,
            accountId = signerAccountId ?? _accountId,
            privateKey = signerPrivateKey ?? _privateKey
        });
        return response.RootElement.GetProperty("transactionHash").GetString();
    }

    public async Task<string> CallAsync(string methodName, object args, string targetContractId = null,
        string depositYocto = "0", string signerAccountId = null, string signerPrivateKey = null)
    {
        EnsureWritable(signerAccountId, signerPrivateKey);
        using var response = await InvokeAsync("call", new
        {
            methodName,
            args,
            targetContractId = targetContractId ?? _contractId,
            depositYocto,
            accountId = signerAccountId ?? _accountId,
            privateKey = signerPrivateKey ?? _privateKey
        });
        return response.RootElement.GetProperty("transactionHash").GetString();
    }

    public async Task<JsonElement> ViewAsync(string methodName, object args, string targetContractId = null)
    {
        using var response = await InvokeAsync("view", new { methodName, args, targetContractId = targetContractId ?? _contractId });
        return response.RootElement.Clone();
    }

    public async Task<string> GetTransactionStatusAsync(string transactionHash, string signerAccountId = null)
    {
        using var response = await InvokeAsync("transactionStatus", new
        {
            transactionHash,
            signerAccountId = signerAccountId ?? _accountId
        });
        return response.RootElement.GetProperty("status").GetString();
    }

    private void EnsureWritable(string accountId = null, string privateKey = null)
    {
        if (string.IsNullOrWhiteSpace(accountId ?? _accountId) || string.IsNullOrWhiteSpace(privateKey ?? _privateKey))
            throw new InvalidOperationException("NEAR accountId and privateKey are required for storage writes.");
    }

    private async Task<JsonDocument> InvokeAsync(string operation, object args = null)
    {
        var request = new Dictionary<string, object>
        {
            ["operation"] = operation,
            ["rpcEndpoint"] = _rpcEndpoint,
            ["networkId"] = _networkId,
            ["contractId"] = _contractId,
            ["accountId"] = _accountId,
            ["privateKey"] = _privateKey
        };
        if (args != null)
            foreach (var property in args.GetType().GetProperties())
                request[property.Name] = property.GetValue(args);

        var startInfo = new ProcessStartInfo
        {
            FileName = _nodeExecutable,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(_bridgePath);
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the official NEAR SDK bridge.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request));
        process.StandardInput.Close();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (string.IsNullOrWhiteSpace(stdout))
            throw new InvalidOperationException($"NEAR SDK bridge returned no response (exit {process.ExitCode}): {stderr}");
        using var envelope = JsonDocument.Parse(stdout);
        if (process.ExitCode != 0 || !envelope.RootElement.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
        {
            var error = envelope.RootElement.TryGetProperty("error", out var value) ? value.GetString() : stderr;
            throw new InvalidOperationException($"NEAR SDK operation failed: {error}");
        }
        return JsonDocument.Parse(envelope.RootElement.GetProperty("result").GetRawText());
    }
}

internal sealed record NearKeyPair(string PublicKey, string PrivateKey, string ImplicitAccountId);
