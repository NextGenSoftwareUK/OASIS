using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.FlowOASIS
{
    /// <summary>Runs one request through the pinned Node FCL bridge (SdkBridge/bridge.mjs): JSON on stdin, one envelope on
    /// stdout. The signer key travels only on stdin; stderr is drained and never surfaced because it may echo SDK input.</summary>
    public interface IFlowSdkBridge
    {
        Task<JsonElement> InvokeAsync(object request, CancellationToken cancellationToken = default);
    }

    public sealed class FlowSdkBridge : IFlowSdkBridge
    {
        private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(180);

        public async Task<JsonElement> InvokeAsync(object request, CancellationToken cancellationToken = default)
        {
            var input = JsonSerializer.Serialize(request);
            var bridge = Environment.GetEnvironmentVariable("OASIS_FLOW_SDK_BRIDGE")
                ?? Path.Combine(AppContext.BaseDirectory, "FlowSdkBridge", "bridge.mjs");
            if (!File.Exists(bridge))
                throw new FileNotFoundException("Pinned Flow SDK bridge is missing; provision its locked dependencies once (npm ci in SdkBridge).", bridge);

            var start = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("OASIS_FLOW_NODE") ?? "node",
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(false, true),
                StandardOutputEncoding = new UTF8Encoding(false, true),
                StandardErrorEncoding = new UTF8Encoding(false, true),
                UseShellExecute = false, CreateNoWindow = true
            };
            start.ArgumentList.Add(bridge);
            cancellationToken.ThrowIfCancellationRequested();

            using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start the Flow SDK bridge.");
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
                _ = await stderr;
                using var envelope = JsonDocument.Parse(output);
                var root = envelope.RootElement;
                if (!root.GetProperty("ok").GetBoolean())
                {
                    var message = root.TryGetProperty("error", out var error) ? error.GetString() : "Flow SDK bridge failed.";
                    if (root.TryGetProperty("transactionId", out var id) && id.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(id.GetString()))
                        throw new FlowTransactionReconciliationException(id.GetString(), new InvalidOperationException(message));
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

    /// <summary>A Flow transaction was submitted but its seal was not observed; reconcile by id, never replay.</summary>
    public sealed class FlowTransactionReconciliationException : Exception, INativeMutationReconciliationRequired
    {
        public string OriginalTransactionId { get; }

        public FlowTransactionReconciliationException(string transactionId, Exception cause)
            : base($"Submitted Flow transaction {transactionId} requires reconciliation before any replay.", cause)
            => OriginalTransactionId = transactionId;
    }
}
