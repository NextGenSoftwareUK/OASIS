using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public enum HolonMigrationDeliveryObservationKind { UnrelatedGraph, OriginalReceiverCommitted, OtherAffectedRoute, AdditionalAffectedObligation }
public sealed record HolonMigrationDeliveryObservation(long Sequence, Guid OperationId, string TargetId,
    HolonDeliveryState SourceState, string StateDigest, bool ChangedSinceReview, bool RequiresNativeReconciliation,
    HolonMigrationDeliveryObservationKind Kind, string ReplicaAcknowledgementDigest);

/// <summary>Read-only observations, not global quiescence, source completion or
/// permission to clear/retry/relabel old obligations. Hidden lease changes remain
/// bound by StateDigest for a later native compare-and-swap protocol.</summary>
public sealed class HolonMigrationDeliveryReconciliationReport
{
    public string PlanFingerprint { get; }
    public long ThroughSequence { get; }
    public HolonReplicaHeadReadConsistency SourceConsistency { get; }
    public IReadOnlyList<HolonMigrationDeliveryObservation> Obligations { get; }
    public string ObservationDigest { get; }
    internal HolonMigrationDeliveryReconciliationReport(HolonReplicationMigrationBarrierPlan plan, long through,
        HolonReplicaHeadReadConsistency consistency, IEnumerable<HolonMigrationDeliveryObservation> rows)
    {
        PlanFingerprint = plan.Fingerprint; ThroughSequence = through; SourceConsistency = consistency;
        Obligations = Array.AsReadOnly(rows.ToArray());
        var material = JsonSerializer.SerializeToUtf8Bytes(new { format = "cosmic-migration-delivery-review-v1",
            PlanFingerprint, ThroughSequence, SourceConsistency, Obligations });
        ObservationDigest = Convert.ToHexString(SHA256.HashData(material));
    }
}

public static class HolonMigrationDeliveryReconciliation
{
    private sealed class NativeReadException(string code, Exception cause) : Exception("Native migration evidence read failed.", cause)
    { internal string Code { get; } = code; }

    /// <summary>Read all current lineage obligations within a fresh bounded window,
    /// not only the original window or route head. Native payloads are transient:
    /// decode graph membership but never return/log their business bodies. Re-read
    /// every held fence afterward; later mutation still needs native CAS/phase gates.</summary>
    public static async Task<OASISResult<HolonMigrationDeliveryReconciliationReport>> ReadAsync(
        HolonReplicationMigrationBarrierPlan plan, IHolonReplicationMigrationAuthorizationPolicy policy,
        IEnumerable<HolonMigrationBarrierParticipant> participants, IHolonReplicationLineageInventoryOutbox source,
        int maxObligations = 4096, int pageSize = 64, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan); ArgumentNullException.ThrowIfNull(policy);
        OASISResult<HolonMigrationDeliveryReconciliationReport> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        var warnings = new List<Exception>(); var warningCount = 0;
        void Observe<T>(OASISResult<T> observed)
        {
            if (!observed.IsWarning) return;
            warningCount += Math.Max(1, observed.WarningCount);
            if (observed.Exception != null) warnings.Add(observed.Exception);
        }
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (source is not IHolonReplicationPayloadAuditOutbox payloads || pageSize < 1 || pageSize > 256 || maxObligations < 1 || maxObligations > 4096)
                return Error("COSMIC_MIGRATION_DELIVERY_OPTIONS_INVALID", "Bounded native lineage and frozen payload readers are required.");
            var context = plan.Context;
            if (policy.BindingIdentity != context.AuthorizationPolicyBinding)
                return Error("COSMIC_MIGRATION_AUTHORIZATION_DENIED", "The exact host policy is required.");
            async Task Authorize()
            {
                var allowed = await policy.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);
                if (allowed == null || allowed.IsError || !allowed.Result)
                    throw new NativeReadException(allowed?.ErrorCode ?? "COSMIC_MIGRATION_AUTHORIZATION_DENIED", allowed?.Exception);
            }
            await Authorize().ConfigureAwait(false);
            var owners = HolonMigrationBarrierParticipants.Bind(plan, participants);
            var target = owners.Single(p => p.Storage.BindingFingerprint == context.OriginalTargetStorage.BindingFingerprint);
            if (target.Provider is not ICommittedHolonReplicaReceiptStorageProvider receiver)
                return Error("COSMIC_MIGRATION_DELIVERY_RECEIPT_CAPABILITY_REQUIRED", "The original fenced receiver must read its native historical receipts.");
            var heldOwners = new Dictionary<string, Guid>(StringComparer.Ordinal);
            async Task CheckFences(bool first)
            {
                foreach (var scope in plan.Scopes)
                {
                    var native = owners.Single(p => p.Storage.BindingFingerprint == scope.ExpectedStorage.BindingFingerprint).Provider;
                    var fence = await native.ReadHolonMigrationFenceAsync(scope, cancellationToken).ConfigureAwait(false);
                    if (fence == null || fence.IsError)
                        throw new NativeReadException(fence?.ErrorCode ?? "COSMIC_MIGRATION_DELIVERY_BARRIER_UNVERIFIED", fence?.Exception);
                    Observe(fence);
                    if (fence.Result == null || fence.Result.Request.Fingerprint != scope.Fingerprint
                        || fence.Result.State != HolonMigrationFenceState.Held
                        || !first && heldOwners[scope.Fingerprint] != fence.Result.OwnerToken)
                        throw new InvalidDataException("Every original native record barrier must remain held by the same owner.");
                    if (first) heldOwners.Add(scope.Fingerprint, fence.Result.OwnerToken);
                }
            }
            await CheckFences(true).ConfigureAwait(false);
            using var envelope = JsonDocument.Parse(plan.CanonicalJson);
            using var reviewed = JsonDocument.Parse(envelope.RootElement.GetProperty("previewMaterial").GetString());
            var material = reviewed.RootElement;
            var consistency = (HolonReplicaHeadReadConsistency)material.GetProperty("SourceConsistency").GetInt32();
            var receiptConsistency = (HolonReplicaHeadReadConsistency)material.GetProperty("RequiredReplicaReceiptConsistency").GetInt32();
            var prior = material.GetProperty("Obligations").EnumerateArray().ToDictionary(r => r.GetProperty("Sequence").GetInt64(), r => r.Clone());
            var originalReceipts = material.GetProperty("ReplicaReceipts").EnumerateArray().ToDictionary(r => r.GetProperty("OperationId").GetGuid(), r => r.Clone());
            var scopeIds = plan.Scopes[0].HolonIds.ToHashSet();
            var rows = new List<HolonReplicationOperationObligation>(); long after = 0; long? upper = null;
            while (true)
            {
                var request = new HolonReplicationLineageReadRequest(context.SourceStorage, context.OriginalSourceId,
                    Math.Min(pageSize, maxObligations - rows.Count), after, upper, consistency);
                var observed = await source.ReadLineageObligationsAsync(request, cancellationToken).ConfigureAwait(false);
                if (observed == null || observed.IsError || observed.Result == null)
                    return Error(observed?.ErrorCode ?? "COSMIC_MIGRATION_DELIVERY_INVENTORY_UNVERIFIED", "Native lineage observation failed.", observed?.Exception);
                Observe(observed);
                var page = observed.Result; upper ??= page.ThroughSequence;
                if (page.SourceStorageFingerprint != context.SourceStorage.BindingFingerprint || page.SourceId != context.OriginalSourceId
                    || page.ThroughSequence != upper || consistency == HolonReplicaHeadReadConsistency.NativeSnapshot && page.Consistency != consistency)
                    throw new InvalidDataException("Native lineage source, upper boundary or consistency changed.");
                foreach (var row in page.Obligations)
                {
                    if (row.Sequence <= after || row.Sequence > upper || row.SourceId != context.OriginalSourceId
                        || row.SourceAuthority != context.SourceStorage.Authority) throw new InvalidDataException("Native lineage row is out of scope.");
                    rows.Add(row); after = row.Sequence;
                }
                if (!page.NextAfterSequence.HasValue) break;
                if (page.Obligations.Count != request.PageSize || page.NextAfterSequence != after)
                    throw new InvalidDataException("Native lineage continuation is incomplete.");
                if (rows.Count >= maxObligations) return Error("COSMIC_MIGRATION_DELIVERY_LIMIT_EXCEEDED", "No partial delivery review is returned.");
            }
            if (rows.Count > maxObligations || rows.GroupBy(r => (r.OperationId, r.TargetId)).Any(g => g.Count() != 1)
                || prior.Keys.Except(rows.Select(r => r.Sequence)).Any())
                throw new InvalidDataException("Reviewed obligations are missing from the current complete window.");
            var reports = new List<HolonMigrationDeliveryObservation>();
            foreach (var row in rows)
            {
                var known = prior.TryGetValue(row.Sequence, out var old);
                if (known && (old.GetProperty("OperationId").GetGuid() != row.OperationId || old.GetProperty("ActorId").GetGuid() != row.ActorId
                    || old.GetProperty("TargetId").GetString() != row.TargetId || old.GetProperty("PayloadDigest").GetString() != row.PayloadDigest))
                    throw new InvalidDataException("Original immutable obligation identity or transport changed.");
                var raw = await payloads.ReadFrozenPayloadAsync(context.SourceStorage, row, consistency, cancellationToken).ConfigureAwait(false);
                if (raw == null || raw.IsError || raw.Result == null)
                    return Error(raw?.ErrorCode ?? "COSMIC_MIGRATION_DELIVERY_PAYLOAD_UNVERIFIED", "Frozen graph evidence failed.", raw?.Exception);
                Observe(raw);
                if (Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw.Result))) != row.PayloadDigest)
                    throw new InvalidDataException("Frozen transport hash differs from the native obligation.");
                var transport = CommittedHolonReplicaPayload.ReadForTarget(raw.Result, row.TargetId);
                if (transport.OperationId != row.OperationId || transport.ActorId != row.ActorId || transport.SourceId != row.SourceId || transport.SourceAuthority != row.SourceAuthority)
                    throw new InvalidDataException("Frozen transport belongs to another operation or source.");
                var graph = CommittedHolonReplicaPlan.Create(transport, context.OriginalTargetStorage.Authority);
                var affected = graph.Nodes.Any(n => scopeIds.Contains(n.Snapshot.Id));
                var kind = !affected ? HolonMigrationDeliveryObservationKind.UnrelatedGraph
                    : !known || graph.Nodes.Any(n => !scopeIds.Contains(n.Snapshot.Id)) ? HolonMigrationDeliveryObservationKind.AdditionalAffectedObligation
                    : row.TargetId != context.OriginalTargetId ? HolonMigrationDeliveryObservationKind.OtherAffectedRoute
                    : HolonMigrationDeliveryObservationKind.OriginalReceiverCommitted;
                string acknowledgement = null;
                if (kind == HolonMigrationDeliveryObservationKind.OriginalReceiverCommitted)
                {
                    var receipt = await receiver.ReadCommittedHolonReplicaReceiptAsync(new(transport, context.OriginalTargetStorage, receiptConsistency), cancellationToken).ConfigureAwait(false);
                    if (receipt == null || receipt.IsError || receipt.Result == null)
                        return Error(receipt?.ErrorCode ?? "COSMIC_MIGRATION_DELIVERY_RECEIPT_UNVERIFIED", "Original receiver acknowledgement could not be read.", receipt?.Exception);
                    Observe(receipt);
                    var e = receipt.Result;
                    if (!originalReceipts.TryGetValue(row.OperationId, out var original) || e.OperationId != row.OperationId || e.ActorId != row.ActorId
                        || e.SourceId != row.SourceId || e.SourceAuthority != row.SourceAuthority || e.Operation != transport.Operation
                        || e.TargetStorageFingerprint != context.OriginalTargetStorage.BindingFingerprint || e.OriginalRequestFingerprint != graph.Fingerprint
                        || e.ReceiptState != ConditionalHolonReceiptState.Committed || e.SelectedRecordCount != graph.Nodes.Count
                        || e.AcknowledgementDigest != original.GetProperty("AcknowledgementDigest").GetString()
                        || receiptConsistency == HolonReplicaHeadReadConsistency.NativeSnapshot && e.Consistency != receiptConsistency)
                        throw new InvalidDataException("Historical acknowledgement differs from its original exact native intent.");
                    acknowledgement = e.AcknowledgementDigest;
                }
                reports.Add(new(row.Sequence, row.OperationId, row.TargetId, row.State, row.StateDigest,
                    !known || row.StateDigest != old.GetProperty("StateDigest").GetString(), row.RequiresNativeReconciliation, kind, acknowledgement));
            }
            await CheckFences(false).ConfigureAwait(false); await Authorize().ConfigureAwait(false);
            return new(new(plan, upper ?? 0, consistency, reports)) { IsLoaded = true, IsWarning = warningCount > 0, WarningCount = warningCount,
                Exception = warnings.Count == 0 ? null : warnings.Count == 1 ? warnings[0] : new AggregateException(warnings),
                Message = "Read-only delivery evidence; in-flight/native-unknown/other-route obligations still require explicit disposition and native CAS." };
        }
        catch (Exception error)
        {
            return Error(error is NativeReadException native ? native.Code : error is HolonMigrationBarrierPlanException scoped ? scoped.Code : error is OperationCanceledException
                ? "COSMIC_MIGRATION_DELIVERY_CANCELLED" : "COSMIC_MIGRATION_DELIVERY_UNVERIFIED", "No partial report or obligation mutation was returned.", error is NativeReadException failure ? failure.InnerException ?? failure : error);
        }
    }
}
