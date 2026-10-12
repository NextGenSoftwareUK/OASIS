using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

internal static class HederaSdkClient
{
    private static long _lastTransactionTicks;

    internal static string CreateTransactionId(string sender)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(sender ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
            throw new ArgumentException("A numeric Hedera sender account ID is required.");
        long prior, next;
        do
        {
            prior = Interlocked.Read(ref _lastTransactionTicks);
            // The SDK backdates valid starts by 3-8 seconds for consensus clock skew.
            // Keep deterministic monotonic IDs within that safety margin; never regenerate on submission.
            next = Math.Max(DateTime.UtcNow.AddSeconds(-8).Ticks, checked(prior + 1));
        } while (Interlocked.CompareExchange(ref _lastTransactionTicks, next, prior) != prior);
        var epochTicks = next - DateTime.UnixEpoch.Ticks;
        return sender + "@" + (epochTicks / TimeSpan.TicksPerSecond).ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "." + ((epochTicks % TimeSpan.TicksPerSecond) * 100).ToString("D9", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static async Task<JsonElement> InvokeAsync(string action, object request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var script = Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE")
            ?? Path.Combine(AppContext.BaseDirectory, "HederaSdkBridge", "bridge.mjs");
        if (!Path.IsPathFullyQualified(script) || !File.Exists(script))
            throw new InvalidOperationException("The installed Hedera SDK bridge path is missing or not absolute.");
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("OASIS_NODE_PATH") ?? "node")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false), StandardOutputEncoding = new UTF8Encoding(false, true),
            StandardErrorEncoding = new UTF8Encoding(false, true), WorkingDirectory = Path.GetDirectoryName(script)
        };
        start.ArgumentList.Add(script);
        var requestJson = JsonSerializer.Serialize(new { action, request });
        using var requestDocument = JsonDocument.Parse(requestJson);
        var originalId = requestDocument.RootElement.GetProperty("request").TryGetProperty("transactionId", out var requestedId)
            ? requestedId.GetString() : null;
        var originalApprovalId = requestDocument.RootElement.GetProperty("request").TryGetProperty("approvalTransactionId", out var requestedApprovalId)
            ? requestedApprovalId.GetString() : null;
        var isMutation = action is "storagePut" or "storageDelete" or "fileCreate" or "fileUpdate" or "fileDelete"
            or "transfer" or "tokenTransfer" or "tokenMint" or "tokenBurn" or "nftTransfer" or "nftMint" or "nftBurn"
            or "nftEscrowLock" or "nftEscrowUnlock" or "tokenEscrowLock" or "tokenEscrowUnlock"
            or "bytecodeFileCreate" or "bytecodeFileAppend" or "contractCreate" or "nftCollectionCreate"
            or "tokenCollectionDelete" or "nftMetadataUpdate";
        if (isMutation && string.IsNullOrWhiteSpace(originalId))
            throw new ArgumentException("Hedera native mutations require an explicit original transaction ID before SDK launch.");
        using var process = new Process { StartInfo = start };
        if (!process.Start()) throw new InvalidOperationException("Hedera SDK process could not start.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            // Signing credentials travel through stdin, never command arguments or logs.
            await process.StandardInput.WriteAsync(requestJson.AsMemory(), timeout.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);
            var response = await output;
            var failure = await error;
            if (process.ExitCode != 0)
            {
                string transactionId = originalId;
                string approvalId = null, mutationId = null, stage = null, status = null;
                bool nativeIdReported = false;
                Exception evidenceParseError = null;
                try
                {
                    using var parsed = JsonDocument.Parse(failure);
                    if (parsed.RootElement.TryGetProperty("transactionId", out var id))
                    {
                        var reportedId = id.GetString();
                        if (reportedId != null && reportedId != originalId && reportedId != originalApprovalId)
                            throw new InvalidDataException("SDK error identity does not match an original requested transaction.");
                        transactionId = reportedId ?? originalId;
                        nativeIdReported = reportedId != null;
                    }
                    if (parsed.RootElement.TryGetProperty("approvalTransactionId", out var approval)) approvalId = approval.GetString();
                    if (parsed.RootElement.TryGetProperty("mutationTransactionId", out var mutation)) mutationId = mutation.GetString();
                    if ((approvalId != null && approvalId != originalApprovalId) || (mutationId != null && mutationId != originalId))
                        throw new InvalidDataException("SDK escrow error identities do not match the original requested transactions.");
                    if (parsed.RootElement.TryGetProperty("stage", out var phase)) stage = phase.GetString();
                    if (parsed.RootElement.TryGetProperty("status", out var nativeStatus) && nativeStatus.ValueKind == JsonValueKind.String)
                    {
                        var value = nativeStatus.GetString();
                        if (System.Text.RegularExpressions.Regex.IsMatch(value ?? "", @"\A[A-Z][A-Z0-9_]{0,100}\z")) status = value;
                    }
                }
                catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is InvalidDataException)
                {
                    evidenceParseError = ex;
                    transactionId = originalId;
                    approvalId = null; mutationId = null; stage = null; status = null;
                }
                var reconciliation = (approvalId == null ? "" : $" Confirmed approval: {approvalId}.")
                    + (mutationId == null ? "" : $" Original escrow mutation: {mutationId}.")
                    + (stage == null ? "" : $" Failure stage: {stage}.")
                    + (status == null ? "" : $" Native status: {status}.");
                var message = (transactionId == null ? "Hedera SDK operation failed."
                    : $"Hedera verified success was not established for {transactionId}. Track the original transaction before retrying.") + reconciliation;
                if (isMutation && (approvalId != null || stage is "submission" or "receipt" or "approval" or "mutation" or "readback"
                    || (nativeIdReported && stage is not ("construction" or "signing" or "validation")) || (evidenceParseError != null && originalId != null)))
                    throw new HederaNativeMutationReconciliationException(message,
                        new HederaNativeMutationEvidence(transactionId, approvalId, mutationId, stage, status), evidenceParseError);
                throw new InvalidOperationException(message, evidenceParseError);
            }
            JsonDocument parsedResponse;
            try { parsedResponse = JsonDocument.Parse(response); }
            catch (JsonException ex) when (isMutation)
            {
                throw new HederaNativeMutationReconciliationException("Hedera mutation process returned invalid JSON; reconcile the original transaction before retrying.",
                    new HederaNativeMutationEvidence(originalId, null, originalId, "process-output-unverified", null), ex);
            }
            using var document = parsedResponse;
            if (isMutation && (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("transactionId", out var returnedId) || returnedId.ValueKind != JsonValueKind.String
                || returnedId.GetString() != originalId
                || !document.RootElement.TryGetProperty("status", out var returnedStatus) || returnedStatus.ValueKind != JsonValueKind.String
                || returnedStatus.GetString() != "SUCCESS"))
                throw new HederaNativeMutationReconciliationException("Hedera mutation reply does not establish SUCCESS for the original transaction; reconcile before retrying.",
                    new HederaNativeMutationEvidence(originalId, null, originalId, "process-output-unverified", null));
            if (isMutation && document.RootElement.TryGetProperty("approvalTransactionId", out var returnedApproval)
                && returnedApproval.ValueKind != JsonValueKind.Null
                && (returnedApproval.ValueKind != JsonValueKind.String || returnedApproval.GetString() != originalApprovalId))
                throw new HederaNativeMutationReconciliationException("Hedera mutation reply reports an unverified approval identity; reconcile original transactions before retrying.",
                    new HederaNativeMutationEvidence(originalId, null, originalId, "process-output-unverified", null));
            return document.RootElement.Clone();
        }
        catch (OperationCanceledException errorException)
        {
            var reason = cancellationToken.IsCancellationRequested ? "canceled" : "timed out";
            var message = $"Hedera SDK operation {reason}."
                + (originalId == null ? "" : $" Original transaction: {originalId}.")
                + (originalApprovalId == null ? "" : $" Possible approval transaction (outcome unverified): {originalApprovalId}.")
                + " Reconcile original transactions before retrying; process termination does not cancel a ledger submission.";
            if (cancellationToken.IsCancellationRequested)
            {
                if (isMutation)
                    throw new HederaNativeMutationCanceledException(message,
                        new HederaNativeMutationEvidence(originalId, originalApprovalId, originalId, "process-canceled", null), errorException, cancellationToken);
                throw new OperationCanceledException(message, errorException, cancellationToken);
            }
            if (isMutation)
                throw new HederaNativeMutationTimeoutException(message,
                    new HederaNativeMutationEvidence(originalId, originalApprovalId, originalId, "process-timed-out", null), errorException);
            throw new TimeoutException(message, errorException);
        }
        catch (Exception transportError) when (isMutation && (transportError is IOException || transportError is DecoderFallbackException))
        {
            throw new HederaNativeMutationReconciliationException(
                "Hedera SDK process communication failed after launch; reconcile original transactions before retrying.",
                new HederaNativeMutationEvidence(originalId, originalApprovalId, originalId, "process-communication-unverified", null), transportError);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            // Observe reader completion even when cancellation terminates the child.
            try { await Task.WhenAll(output, error); }
            // These reader faults were already handled above (or accompany cancellation).
            // Observing them here must not replace the primary reconciliation exception.
            catch (Exception readerError) when (readerError is OperationCanceledException || readerError is IOException || readerError is DecoderFallbackException) { }
        }
    }
}
