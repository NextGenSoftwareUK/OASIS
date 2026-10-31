using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

/// <summary>
/// Runs the pinned official starknet.js SDK (SdkBridge/bridge.mjs) for Stark-curve keys, account addresses and
/// account deployment. One JSON request goes to stdin (secrets never appear in arguments or the environment) and one
/// {ok, result} envelope comes back on stdout. SDK diagnostics on stderr are drained but never surfaced.
/// </summary>
public interface IStarknetSdkBridge
{
    Task<JsonElement> InvokeAsync(object request, CancellationToken cancellationToken = default);
}

public sealed class StarknetSdkBridge : IStarknetSdkBridge
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(180);

    public async Task<JsonElement> InvokeAsync(object request, CancellationToken cancellationToken = default)
    {
        var input = JsonSerializer.Serialize(request);
        var bridge = Environment.GetEnvironmentVariable("OASIS_STARKNET_SDK_BRIDGE")
            ?? Path.Combine(AppContext.BaseDirectory, "StarknetSdkBridge", "bridge.mjs");
        if (!File.Exists(bridge))
            throw new FileNotFoundException("Pinned Starknet SDK bridge is missing; provision its locked dependencies once (npm ci in SdkBridge).", bridge);

        var start = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("OASIS_STARKNET_NODE") ?? "node",
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false, true),
            StandardOutputEncoding = new UTF8Encoding(false, true),
            StandardErrorEncoding = new UTF8Encoding(false, true),
            UseShellExecute = false, CreateNoWindow = true
        };
        start.ArgumentList.Add(bridge);
        cancellationToken.ThrowIfCancellationRequested();

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start the Starknet SDK bridge.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(Deadline);
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteAsync(input.AsMemory(), deadline.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(deadline.Token);
            var output = await stdout;
            _ = await stderr; // Drained, never surfaced: it may echo SDK request material.
            using var envelope = JsonDocument.Parse(output);
            var root = envelope.RootElement;
            if (!root.GetProperty("ok").GetBoolean())
            {
                var message = root.TryGetProperty("error", out var error) ? error.GetString() : "Starknet SDK bridge failed.";
                if (root.TryGetProperty("transactionHash", out var hash) && !string.IsNullOrWhiteSpace(hash.GetString()))
                    throw new StarknetTransactionReconciliationException(hash.GetString(), new InvalidOperationException(message));
                throw new InvalidOperationException(message);
            }
            return root.GetProperty("result").Clone();
        }
        finally
        {
            deadline.Cancel();
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            try { await Task.WhenAll(stdout, stderr); }
            catch (Exception error) when (error is IOException or DecoderFallbackException) { }
        }
    }
}

/// <summary>A Starknet transaction was submitted but its outcome is not confirmed; reconcile by hash, never replay.</summary>
public sealed class StarknetTransactionReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }

    public StarknetTransactionReconciliationException(string transactionHash, Exception cause)
        : base($"Submitted Starknet transaction {transactionHash} requires reconciliation before any replay.", cause)
        => OriginalTransactionId = transactionHash;
}
