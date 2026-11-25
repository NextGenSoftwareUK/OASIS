using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS;

/// <summary>
/// Runs the pinned official Miden SDK (SdkBridge/worker.mjs: @miden-sdk/miden-sdk with its native SQLite store) as one long-lived
/// process, because each client start re-syncs the chain. Requests are single JSON lines on stdin (secrets never appear in
/// arguments or the environment); each reply is one stdout line carrying <see cref="ReplyPrefix"/>. SDK logging on
/// stdout or stderr is drained and never surfaced.
/// </summary>
public interface IMidenSdkBridge : IDisposable
{
    Task<JsonElement> InvokeAsync(string operation, object arguments = null, CancellationToken cancellationToken = default);
}

public sealed class MidenSdkBridge : IMidenSdkBridge
{
    private const string ReplyPrefix = "@@OASIS-MIDEN@@";
    private static readonly TimeSpan Deadline = TimeSpan.FromMinutes(15);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Process _process;
    private long _nextId;

    public async Task<JsonElement> InvokeAsync(string operation, object arguments = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        var request = arguments == null ? new JsonObject() : JsonSerializer.SerializeToNode(arguments)?.AsObject() ?? new JsonObject();
        var id = Interlocked.Increment(ref _nextId);
        request["id"] = id;
        request["operation"] = operation;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var process = _process ??= Start();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(Deadline);
            try
            {
                await process.StandardInput.WriteLineAsync(request.ToJsonString().AsMemory(), deadline.Token);
                await process.StandardInput.FlushAsync(deadline.Token);
                while (true)
                {
                    var line = await process.StandardOutput.ReadLineAsync(deadline.Token)
                        ?? throw new InvalidOperationException("The Miden SDK worker exited before replying.");
                    if (!line.StartsWith(ReplyPrefix, StringComparison.Ordinal)) continue; // SDK logging
                    using var reply = JsonDocument.Parse(line.AsMemory(ReplyPrefix.Length));
                    var root = reply.RootElement;
                    if (!root.TryGetProperty("id", out var replyId) || replyId.ValueKind != JsonValueKind.Number || replyId.GetInt64() != id)
                        throw new InvalidDataException("The Miden SDK worker replied out of order.");
                    if (root.GetProperty("ok").GetBoolean()) return root.GetProperty("result").Clone();
                    var message = root.TryGetProperty("error", out var error) ? error.GetString() : "The Miden SDK worker failed.";
                    if (root.TryGetProperty("txId", out var hash) && hash.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(hash.GetString()))
                        throw new MidenTransactionReconciliationException(hash.GetString(), new InvalidOperationException(message));
                    throw new InvalidOperationException(message);
                }
            }
            catch (Exception error) when (error is OperationCanceledException or IOException or InvalidDataException)
            {
                // A worker that timed out or broke protocol cannot be trusted with the next request's reply.
                Stop();
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private static Process Start()
    {
        var worker = Environment.GetEnvironmentVariable("OASIS_MIDEN_SDK_BRIDGE")
            ?? Path.Combine(AppContext.BaseDirectory, "MidenSdkBridge", "worker.mjs");
        if (!File.Exists(worker))
            throw new FileNotFoundException("Pinned Miden SDK worker is missing; provision its locked dependencies once (npm ci in MidenSdkBridge).", worker);
        if (!File.Exists(Path.Combine(Path.GetDirectoryName(worker)!, "oasis_kv.masm")))
            throw new FileNotFoundException("The oasis::kv component source is missing beside the Miden SDK worker.", worker);

        var start = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("OASIS_MIDEN_NODE") ?? "node",
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false, true),
            StandardOutputEncoding = new UTF8Encoding(false, false),
            StandardErrorEncoding = new UTF8Encoding(false, false),
            UseShellExecute = false, CreateNoWindow = true
        };
        start.ArgumentList.Add(worker);
        start.Environment["LOG_LEVEL"] = "warn";
        var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start the Miden SDK worker.");
        process.ErrorDataReceived += (_, _) => { }; // drained, never surfaced: it may echo SDK request material
        process.BeginErrorReadLine();
        return process;
    }

    private void Stop()
    {
        var process = Interlocked.Exchange(ref _process, null);
        if (process == null) return;
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            process.WaitForExit(10_000);
        }
        catch (InvalidOperationException) { } // already exited
        finally { process.Dispose(); }
    }

    public void Dispose()
    {
        Stop();
        _gate.Dispose();
    }
}

/// <summary>An Miden transaction was submitted but not confirmed as succeeded; reconcile by hash, never replay blindly.</summary>
public sealed class MidenTransactionReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }

    public MidenTransactionReconciliationException(string transactionHash, Exception cause)
        : base($"Submitted Miden transaction {transactionHash} requires reconciliation before any replay.", cause)
        => OriginalTransactionId = transactionHash;
}
