using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public enum HolonReplicationMigrationFinding
{
    DeliveryInFlight, IncompleteOriginalGraph, TargetDoesNotMatchOriginalReceipt,
    OriginalReplicaReceiptsUnverified, OriginalPayloadReceiptBindingUnverified,
    NativeWriteAndDeliveryFencesRequired, ReplacementTargetEvidenceUnverified,
    OriginalReplicaReceiptAbsent
}

/// <summary>Immutable review evidence for one route within a captured lineage
/// window. Other routes are retained so review cannot silently omit obligations.
/// This is neither a quiescent snapshot nor an executable migration plan.</summary>
public sealed class HolonReplicationMigrationPreview
{
    public string Format => "cosmic-lineage-migration-preview-v3";
    public CosmicStorageIdentity SourceStorage { get; }
    public CosmicStorageIdentity OriginalTargetStorage { get; }
    public string OriginalSourceId { get; }
    public string OriginalTargetId { get; }
    public HolonReplicationRouteBinding ReplacementRoute { get; }
    public long ThroughSequence { get; }
    public HolonReplicaHeadReadConsistency SourceConsistency { get; }
    public HolonReplicaHeadReadConsistency RequiredReplicaReceiptConsistency { get; }
    public IReadOnlyList<HolonReplicationOperationObligation> Obligations { get; }
    public IReadOnlyList<HolonReplicationReceiptPreflightReport> Receipts { get; }
    public IReadOnlyList<CommittedHolonReplicaReceiptEvidence> ReplicaReceipts { get; }
    public IReadOnlyList<HolonReplicationPayloadReceiptProof> PayloadProofs { get; }
    public IReadOnlyList<HolonReplicationMigrationFinding> Findings { get; }
    public string PreviewDigest { get; }
    internal string DigestMaterial { get; }

    internal HolonReplicationMigrationPreview(CosmicStorageIdentity source, CosmicStorageIdentity target,
        string sourceId, string targetId, HolonReplicationRouteBinding replacement, long through,
        HolonReplicaHeadReadConsistency consistency, IEnumerable<HolonReplicationOperationObligation> rows,
        IEnumerable<HolonReplicationReceiptPreflightReport> receipts,
        IEnumerable<CommittedHolonReplicaReceiptEvidence> replicaReceipts,
        HolonReplicaHeadReadConsistency requiredReplicaReceiptConsistency,
        IEnumerable<HolonReplicationPayloadReceiptProof> payloadProofs)
    {
        SourceStorage = source; OriginalTargetStorage = target;
        OriginalSourceId = sourceId; OriginalTargetId = targetId; ReplacementRoute = replacement;
        ThroughSequence = through; SourceConsistency = consistency;
        RequiredReplicaReceiptConsistency = requiredReplicaReceiptConsistency;
        Obligations = Array.AsReadOnly(rows.ToArray()); Receipts = Array.AsReadOnly(receipts.ToArray());
        ReplicaReceipts = Array.AsReadOnly(replicaReceipts.ToArray());
        PayloadProofs = Array.AsReadOnly(payloadProofs.ToArray());
        var findings = new List<HolonReplicationMigrationFinding>();
        if (Obligations.Any(r => r.TargetId == targetId && r.State == HolonDeliveryState.InFlight))
            findings.Add(HolonReplicationMigrationFinding.DeliveryInFlight);
        if (Receipts.Any(r => !r.IsCompleteOriginalGraph)) findings.Add(HolonReplicationMigrationFinding.IncompleteOriginalGraph);
        if (Receipts.Any(r => r.Records.Any(c => c.Kind != HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt)))
            findings.Add(HolonReplicationMigrationFinding.TargetDoesNotMatchOriginalReceipt);
        // A matching latest head never substitutes for the original replica receipt.
        if (ReplicaReceipts.Count != Receipts.Count)
            findings.Add(HolonReplicationMigrationFinding.OriginalReplicaReceiptsUnverified);
        if (ReplicaReceipts.Any(r => r.ReceiptState == ConditionalHolonReceiptState.Absent))
            findings.Add(HolonReplicationMigrationFinding.OriginalReplicaReceiptAbsent);
        if (PayloadProofs.Count != Receipts.Count)
            findings.Add(HolonReplicationMigrationFinding.OriginalPayloadReceiptBindingUnverified);
        findings.Add(HolonReplicationMigrationFinding.NativeWriteAndDeliveryFencesRequired);
        findings.Add(HolonReplicationMigrationFinding.ReplacementTargetEvidenceUnverified);
        Findings = Array.AsReadOnly(findings.ToArray());
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { Format,
            source = source.BindingFingerprint, target = target.BindingFingerprint,
            OriginalSourceId, OriginalTargetId, replacementSource = replacement.SourceId,
            replacementTarget = replacement.TargetId, replacementPolicy = replacement.PolicyBinding,
            ThroughSequence, SourceConsistency, RequiredReplicaReceiptConsistency, Obligations,
            receipts = Receipts.Select(r => r.ObservationDigest).ToArray(), ReplicaReceipts, PayloadProofs, Findings });
        DigestMaterial = System.Text.Encoding.UTF8.GetString(bytes);
        PreviewDigest = Convert.ToHexString(SHA256.HashData(bytes));
    }

    /// <summary>The host derives operatorId from authentication. Admission is a
    /// separate check; unresolved findings cannot be bypassed by obtaining a grant.</summary>
    public HolonReplicationMigrationAuthorizationContext AuthorizationContext(
        Guid migrationId, Guid operatorId, string administrativePolicyBinding)
        => new(migrationId, operatorId, SourceStorage, OriginalTargetStorage, OriginalSourceId,
            OriginalTargetId, ReplacementRoute, PreviewDigest, administrativePolicyBinding);
}

public static class HolonReplicationMigrationPreviewReader
{
    /// <summary>Collect every obligation in a bounded source window, then read the
    /// original receipt evidence once per selected-route operation. The callback
    /// is host-owned and uses unchanged historical intents; it must not replay them.
    /// No writes, claims, binding discovery or automatic provider fallback occur.</summary>
    public static async Task<OASISResult<HolonReplicationMigrationPreview>> ReadAsync(
        IHolonReplicationLineageInventoryOutbox source, CosmicStorageIdentity expectedSource,
        string originalSourceId, string originalTargetId, CosmicStorageIdentity originalTarget,
        HolonReplicationRouteBinding replacementRoute,
        Func<HolonReplicationOperationObligation, CancellationToken, Task<OASISResult<HolonReplicationReceiptPreflightReport>>> readOriginalReceipt,
        int maxObligations = 4096, int pageSize = 64,
        HolonReplicaHeadReadConsistency sourceConsistency = HolonReplicaHeadReadConsistency.NativeSnapshot,
        CancellationToken cancellationToken = default,
        Func<HolonReplicationOperationObligation, CancellationToken, Task<OASISResult<CommittedHolonReplicaReceiptEvidence>>> readOriginalReplicaReceipt = null,
        HolonReplicaHeadReadConsistency replicaReceiptConsistency = HolonReplicaHeadReadConsistency.NativeSnapshot,
        Func<HolonReplicationOperationObligation, HolonReplicationReceiptPreflightReport, CancellationToken,
            Task<OASISResult<HolonReplicationPayloadReceiptProof>>> readPayloadReceiptProof = null)
    {
        var result = new OASISResult<HolonReplicationMigrationPreview>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (source == null || expectedSource == null || originalTarget == null || replacementRoute == null
                || readOriginalReceipt == null || maxObligations < 1 || maxObligations > 65536
                || replacementRoute.SourceStorage.BindingFingerprint != expectedSource.BindingFingerprint
                || !Enum.IsDefined(typeof(HolonReplicaHeadReadConsistency), replicaReceiptConsistency))
                return Error(result, "COSMIC_MIGRATION_PREVIEW_OPTIONS_INVALID", "Explicit providers, physical route and bounded evidence scope are required.");
            HolonReplicationMigrationAuthorizationContext.ValidateToken(originalSourceId);
            HolonReplicationMigrationAuthorizationContext.ValidateToken(originalTargetId);
            _ = new HolonReplicationLineageReadRequest(expectedSource, originalSourceId, pageSize,
                requiredConsistency: sourceConsistency);
            var rows = new List<HolonReplicationOperationObligation>();
            long after = 0; long? through = null;
            HolonReplicaHeadReadConsistency? actualConsistency = null;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var request = new HolonReplicationLineageReadRequest(expectedSource, originalSourceId,
                    Math.Min(pageSize, maxObligations - rows.Count), after, through, sourceConsistency);
                var read = await source.ReadLineageObligationsAsync(request, cancellationToken).ConfigureAwait(false);
                Observe(result, read);
                if (read == null || read.IsError || read.Result == null)
                    return Error(result, read?.ErrorCode ?? "COSMIC_MIGRATION_PREVIEW_INVENTORY_UNVERIFIED", "Complete source inventory could not be verified.", read?.Exception);
                var page = read.Result;
                if (page.SourceStorageFingerprint != expectedSource.BindingFingerprint || page.SourceId != originalSourceId
                    || through.HasValue && page.ThroughSequence != through.Value || page.ThroughSequence < after
                    || page.Obligations.Count > request.PageSize
                    || actualConsistency.HasValue && actualConsistency != page.Consistency
                    || sourceConsistency == HolonReplicaHeadReadConsistency.NativeSnapshot && page.Consistency != sourceConsistency)
                    return Error(result, "COSMIC_MIGRATION_PREVIEW_INVENTORY_SCOPE_MISMATCH", "The source changed its scope, window or consistency.");
                through = page.ThroughSequence; actualConsistency = page.Consistency;
                foreach (var row in page.Obligations)
                {
                    if (row.Sequence <= after || row.Sequence > through || row.SourceId != originalSourceId
                        || row.SourceAuthority != expectedSource.Authority
                        || row.NativeRoute != null && row.NativeRoute.SourceStorage.BindingFingerprint != expectedSource.BindingFingerprint)
                        return Error(result, "COSMIC_MIGRATION_PREVIEW_INVENTORY_SCOPE_MISMATCH", "The native sequence or physical source changed.");
                    rows.Add(row); after = row.Sequence;
                }
                if (!page.NextAfterSequence.HasValue) break;
                if (page.Obligations.Count != request.PageSize || page.NextAfterSequence != after)
                    return Error(result, "COSMIC_MIGRATION_PREVIEW_INVENTORY_SCOPE_MISMATCH", "The native continuation does not follow its complete page.");
                if (rows.Count >= maxObligations)
                    return Error(result, "COSMIC_MIGRATION_PREVIEW_LIMIT_EXCEEDED", "The explicit review bound was reached; no partial preview is returned.");
            }
            // Even a native page may be internally valid while a cross-page change
            // produces inconsistent actor/payload observations for one operation.
            if (rows.GroupBy(r => r.OperationId).Any(g => g.Select(r => (r.ActorId, r.PayloadDigest)).Distinct().Count() != 1)
                || rows.GroupBy(r => (r.OperationId, r.TargetId)).Any(g => g.Count() != 1))
                return Error(result, "COSMIC_MIGRATION_PREVIEW_OPERATION_CHANGED", "An original operation changed across inventory pages.");
            var selected = rows.Where(r => r.TargetId == originalTargetId).ToArray();
            if (selected.Length == 0)
                return Error(result, "COSMIC_MIGRATION_PREVIEW_ROUTE_ABSENT", "The captured window contains no obligations for the exact selected route.");
            if (selected.Any(r => r.NativeRoute != null && r.NativeRoute.TargetStorage.BindingFingerprint != originalTarget.BindingFingerprint))
                return Error(result, "COSMIC_MIGRATION_PREVIEW_TARGET_SCOPE_MISMATCH", "An original route identifies another physical target.");
            var receipts = new List<HolonReplicationReceiptPreflightReport>();
            var replicaReceipts = new List<CommittedHolonReplicaReceiptEvidence>();
            var payloadProofs = new List<HolonReplicationPayloadReceiptProof>();
            foreach (var row in selected)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var read = await readOriginalReceipt(row, cancellationToken).ConfigureAwait(false);
                Observe(result, read);
                if (read == null || read.IsError || read.Result == null)
                    return Error(result, read?.ErrorCode ?? "COSMIC_MIGRATION_PREVIEW_RECEIPT_UNVERIFIED", "An original receipt could not be verified.", read?.Exception);
                var receipt = read.Result;
                if (receipt.OperationId != row.OperationId || receipt.ActorId != row.ActorId
                    || receipt.OriginalSourceId != originalSourceId || receipt.SourceAuthority != expectedSource.Authority
                    || receipt.TargetAuthority != originalTarget.Authority
                    || receipt.SourceStorageFingerprint != expectedSource.BindingFingerprint
                    || receipt.TargetStorageFingerprint != originalTarget.BindingFingerprint)
                    return Error(result, "COSMIC_MIGRATION_PREVIEW_RECEIPT_SCOPE_MISMATCH", "Receipt evidence differs from the original operation, actor or physical route.");
                receipts.Add(receipt);
                if (readOriginalReplicaReceipt != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var delivered = await readOriginalReplicaReceipt(row, cancellationToken).ConfigureAwait(false);
                    Observe(result, delivered);
                    if (delivered == null || delivered.IsError || delivered.Result == null)
                        return Error(result, delivered?.ErrorCode ?? "COSMIC_MIGRATION_PREVIEW_REPLICA_RECEIPT_UNVERIFIED",
                            "Historical receiver evidence could not be verified.", delivered?.Exception);
                    var evidence = delivered.Result;
                    if (replicaReceiptConsistency == HolonReplicaHeadReadConsistency.NativeSnapshot
                        && evidence.Consistency != replicaReceiptConsistency)
                        return Error(result, "COSMIC_MIGRATION_PREVIEW_REPLICA_RECEIPT_CONSISTENCY_UNSUPPORTED",
                            "Historical receiver evidence does not meet the explicitly required consistency.");
                    if (evidence.OperationId != row.OperationId || evidence.ActorId != row.ActorId
                        || evidence.SourceId != originalSourceId || evidence.SourceAuthority != expectedSource.Authority
                        || evidence.TargetStorageFingerprint != originalTarget.BindingFingerprint
                        || evidence.Operation != receipt.Operation || evidence.SelectedRecordCount != receipt.OriginalSelectedRecordCount)
                        return Error(result, "COSMIC_MIGRATION_PREVIEW_REPLICA_RECEIPT_SCOPE_MISMATCH",
                            "Historical receiver evidence differs from the selected original operation, actor, lineage, graph or physical target.");
                    replicaReceipts.Add(evidence);
                }
                if (readPayloadReceiptProof != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var verified = await readPayloadReceiptProof(row, receipt, cancellationToken).ConfigureAwait(false);
                    Observe(result, verified);
                    if (verified == null || verified.IsError || verified.Result == null)
                        return Error(result, verified?.ErrorCode ?? "COSMIC_MIGRATION_PREVIEW_PAYLOAD_PROOF_UNVERIFIED",
                            "Frozen source transport equivalence could not be verified.", verified?.Exception);
                    var proof = verified.Result;
                    if (proof.OperationId != row.OperationId || proof.ActorId != row.ActorId || proof.Sequence != row.Sequence
                        || proof.TargetId != row.TargetId || proof.PayloadDigest != row.PayloadDigest || proof.StateDigest != row.StateDigest
                        || proof.SourceStorageFingerprint != expectedSource.BindingFingerprint || proof.TargetStorageFingerprint != originalTarget.BindingFingerprint
                        || proof.OriginalRequestFingerprint != receipt.OriginalRequestFingerprint || proof.SourceReceiptObservationDigest != receipt.ObservationDigest)
                        return Error(result, "COSMIC_MIGRATION_PREVIEW_PAYLOAD_PROOF_SCOPE_MISMATCH", "Payload proof differs from the exact observed source obligation or receipt.");
                    if (sourceConsistency == HolonReplicaHeadReadConsistency.NativeSnapshot && proof.RequiredSourceConsistency != sourceConsistency)
                        return Error(result, "COSMIC_MIGRATION_PREVIEW_PAYLOAD_PROOF_CONSISTENCY_UNSUPPORTED", "Payload proof does not meet the explicitly required source consistency.");
                    if (readOriginalReplicaReceipt != null && proof.ReplicaIntentFingerprint != replicaReceipts[replicaReceipts.Count - 1].OriginalRequestFingerprint)
                        return Error(result, "COSMIC_MIGRATION_PREVIEW_PAYLOAD_REPLICA_INTENT_MISMATCH", "The queued transport is not the original historical receiver intent.");
                    payloadProofs.Add(proof);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            result.Result = new(expectedSource, originalTarget, originalSourceId, originalTargetId, replacementRoute,
                through.Value, actualConsistency.Value, rows, receipts, replicaReceipts, replicaReceiptConsistency, payloadProofs);
            result.IsLoaded = true;
            return result;
        }
        catch (Exception cause)
        {
            return Error(result, cause is OperationCanceledException ? "COSMIC_MIGRATION_PREVIEW_CANCELLED" : "COSMIC_MIGRATION_PREVIEW_UNVERIFIED",
                "The complete review preview could not be verified; no migration was performed.", cause);
        }
    }

    private static void Observe<T>(OASISResult<HolonReplicationMigrationPreview> result, OASISResult<T> read)
    {
        if (read == null) return;
        result.IsWarning |= read.IsWarning; result.WarningCount += read.WarningCount;
        result.InnerMessages.AddRange(read.InnerMessages);
        if (read.Exception != null) result.Exception = result.Exception == null ? read.Exception : new AggregateException(result.Exception, read.Exception);
    }
    private static OASISResult<HolonReplicationMigrationPreview> Error(OASISResult<HolonReplicationMigrationPreview> result,
        string code, string message, Exception cause = null)
    {
        result.Result = null; result.IsLoaded = false; result.IsError = true; result.ErrorCount = Math.Max(1, result.ErrorCount);
        result.ErrorCode = code; result.Message = message;
        if (cause != null && result.Exception != cause)
            result.Exception = result.Exception == null ? cause : new AggregateException(result.Exception, cause);
        return result;
    }
}
