using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public enum HolonReplicationReceiptComparisonKind
{
    MatchesOriginalReceipt, MissingReplicaHead, UntrackedCurrentRecord,
    SourceLineageConflict, LogicalVersionConflict, DeletionConflict,
    ContentConflict, PortableContentEvidenceMissing
}

/// <summary>One redacted source/target comparison; no mutation authorization.</summary>
public sealed class HolonReplicationReceiptComparison
{
    public ConditionalHolonReceiptRecordEvidence Original { get; }
    public HolonReplicaHeadObservation Target { get; }
    public HolonReplicationReceiptComparisonKind Kind { get; }
    internal HolonReplicationReceiptComparison(ConditionalHolonReceiptRecordEvidence original,
        HolonReplicaHeadObservation target, string sourceId, ProviderType sourceAuthority)
    {
        Original = original; Target = target;
        var head = target.Head;
        Kind = head == null ? target.HasCurrentRecord ? HolonReplicationReceiptComparisonKind.UntrackedCurrentRecord
                : HolonReplicationReceiptComparisonKind.MissingReplicaHead
            : head.SourceId != sourceId || head.SourceAuthority != sourceAuthority ? HolonReplicationReceiptComparisonKind.SourceLineageConflict
            : head.Version != original.Version || head.VersionId != original.VersionId || head.PreviousVersionId != original.PreviousVersionId
                ? HolonReplicationReceiptComparisonKind.LogicalVersionConflict
            : head.HardDeleted != original.HardDeleted ? HolonReplicationReceiptComparisonKind.DeletionConflict
            : head.PortableContentDigest == null ? HolonReplicationReceiptComparisonKind.PortableContentEvidenceMissing
            : head.PortableContentDigest != original.PortableContentDigest ? HolonReplicationReceiptComparisonKind.ContentConflict
            : HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt;
    }
}

/// <summary>Observations taken at separate times, not a global snapshot, signed preview
/// or permission to adopt/reset/rebind any head or obligation.</summary>
public sealed class HolonReplicationReceiptPreflightReport
{
    public string Format => "cosmic-receipt-preflight-v1";
    public string ContentDigestFormat => ConditionalHolonReceiptEvidence.ContentFormat;
    public string OriginalRequestFingerprint { get; }
    public ProviderType SourceAuthority { get; }
    public ProviderType TargetAuthority { get; }
    public Guid OperationId { get; }
    public Guid ActorId { get; }
    public ConditionalHolonOperation Operation { get; }
    public string OriginalSourceId { get; }
    public string SourceStorageFingerprint { get; }
    public string TargetStorageFingerprint { get; }
    public int OriginalSelectedRecordCount { get; }
    public bool IsCompleteOriginalGraph => Records.Count == OriginalSelectedRecordCount;
    public HolonReplicaHeadReadConsistency TargetConsistency { get; }
    public HolonReplicaHeadReadConsistency RequiredTargetConsistency { get; }
    public IReadOnlyList<HolonReplicationReceiptComparison> Records { get; }
    public string ObservationDigest { get; }
    internal string DigestMaterial { get; }
    internal HolonReplicationReceiptPreflightReport(ConditionalHolonReceiptEvidence evidence, string sourceId,
        HolonReplicaHeadReadResult target, HolonReplicaHeadReadConsistency requiredConsistency)
    {
        OriginalRequestFingerprint = evidence.OriginalRequestFingerprint;
        SourceAuthority = evidence.SourceAuthority; TargetAuthority = evidence.ComparisonTargetAuthority;
        OperationId = evidence.OperationId; ActorId = evidence.ActorId; Operation = evidence.Operation;
        OriginalSourceId = sourceId; SourceStorageFingerprint = evidence.SourceStorageFingerprint;
        TargetStorageFingerprint = evidence.ComparisonTargetFingerprint;
        OriginalSelectedRecordCount = evidence.OriginalSelectedRecordCount; TargetConsistency = target.Consistency;
        RequiredTargetConsistency = requiredConsistency;
        Records = Array.AsReadOnly(evidence.Records.Select((record, index) =>
            new HolonReplicationReceiptComparison(record, target.Records[index], sourceId, evidence.SourceAuthority)).ToArray());
        var material = JsonSerializer.Serialize(new { Format, ContentDigestFormat, OriginalRequestFingerprint,
            OperationId, ActorId, Operation, OriginalSourceId, SourceAuthority, TargetAuthority,
            SourceStorageFingerprint, TargetStorageFingerprint, OriginalSelectedRecordCount,
            RequiredTargetConsistency, TargetConsistency, Records });
        DigestMaterial = material;
        ObservationDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }
}

public static class HolonReplicationReceiptPreflight
{
    /// <summary>Read-only orchestration. The original receipt is verified before the
    /// target is contacted. NativeSnapshot is the default; weaker target reads require opt-in.</summary>
    public static async Task<OASISResult<HolonReplicationReceiptPreflightReport>> ReadAsync(
        IConditionalHolonReceiptEvidenceStorageProvider source, IHolonReplicaHeadAuditStorageProvider target,
        ConditionalHolonReceiptEvidenceRequest request,
        HolonReplicaHeadReadConsistency targetConsistency = HolonReplicaHeadReadConsistency.NativeSnapshot,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (source == null || target == null || !ValidRequest(request, targetConsistency))
                return Error("COSMIC_MIGRATION_PREFLIGHT_OPTIONS_INVALID", "Explicit native providers, original replication lineage and bounded evidence request are required.");
            var original = await source.ReadConditionalHolonReceiptEvidenceAsync(request, cancellationToken).ConfigureAwait(false);
            if (original == null || original.IsError || original.Result == null)
                return Failure(original, "COSMIC_MIGRATION_PREFLIGHT_SOURCE_UNVERIFIED");
            var invalid = ValidateSource(request, original.Result);
            if (invalid != null) { Observe(invalid, original); return invalid; }
            cancellationToken.ThrowIfCancellationRequested();
            var observed = await target.ReadHolonReplicaHeadsAsync(new HolonReplicaHeadReadRequest(
                request.ComparisonTarget, request.HolonIds, targetConsistency), cancellationToken).ConfigureAwait(false);
            if (observed == null || observed.IsError || observed.Result == null)
            {
                var failure = Failure(observed, "COSMIC_MIGRATION_PREFLIGHT_TARGET_UNVERIFIED");
                Observe(failure, original); return failure;
            }
            cancellationToken.ThrowIfCancellationRequested();
            var report = Compare(request, original.Result, observed.Result, targetConsistency);
            Observe(report, original); Observe(report, observed);
            return report;
        }
        catch (Exception cause)
        {
            return Error(cause is OperationCanceledException ? "COSMIC_MIGRATION_PREFLIGHT_CANCELLED" : "COSMIC_MIGRATION_PREFLIGHT_UNVERIFIED",
                "Source/target observations could not be verified; no migration was performed.", cause);
        }
    }

    /// <summary>Validate separately obtained observations without claiming simultaneity.</summary>
    public static OASISResult<HolonReplicationReceiptPreflightReport> Compare(
        ConditionalHolonReceiptEvidenceRequest request, ConditionalHolonWriteOutcome original,
        HolonReplicaHeadReadResult target,
        HolonReplicaHeadReadConsistency requiredConsistency = HolonReplicaHeadReadConsistency.NativeSnapshot)
    {
        if (!ValidRequest(request, requiredConsistency))
            return Error("COSMIC_MIGRATION_PREFLIGHT_OPTIONS_INVALID", "The original replication lineage and explicit native evidence scope are required.");
        var invalid = ValidateSource(request, original);
        if (invalid != null) return invalid;
        if (target == null || target.StorageFingerprint != request.ComparisonTarget.BindingFingerprint
            || !target.Records.Select(row => row.HolonId).SequenceEqual(request.HolonIds))
            return Error("COSMIC_MIGRATION_PREFLIGHT_TARGET_SCOPE_MISMATCH", "Target evidence belongs to another physical store or selected identity set.");
        if (requiredConsistency == HolonReplicaHeadReadConsistency.NativeSnapshot
            && target.Consistency != HolonReplicaHeadReadConsistency.NativeSnapshot)
            return Error("COSMIC_MIGRATION_PREFLIGHT_CONSISTENCY_UNSUPPORTED", "The target observation does not meet the explicitly required consistency.");
        return new(new(original.ReceiptEvidence, request.Original.Write.ReplicationEnrollment.SourceId, target, requiredConsistency)) { IsLoaded = true };
    }

    private static bool ValidRequest(ConditionalHolonReceiptEvidenceRequest request, HolonReplicaHeadReadConsistency consistency)
        => request?.Original.Write.ReplicationEnrollment != null && Enum.IsDefined(typeof(HolonReplicaHeadReadConsistency), consistency)
            && (request.Original.Write.ReplicationEnrollment.SourceStorageIdentity == null
                || request.Original.Write.ReplicationEnrollment.SourceStorageIdentity.BindingFingerprint == request.ExpectedSource.BindingFingerprint);

    private static OASISResult<HolonReplicationReceiptPreflightReport> ValidateSource(
        ConditionalHolonReceiptEvidenceRequest request, ConditionalHolonWriteOutcome original)
    {
        if (original == null || original.ReceiptState != ConditionalHolonReceiptState.Committed || original.ReceiptEvidence == null)
            return Error("COSMIC_MIGRATION_PREFLIGHT_SOURCE_UNVERIFIED", "A committed original receipt with selected-record evidence is required; absence never authorizes replay.");
        var evidence = original.ReceiptEvidence;
        if (original.OperationId != request.Original.Write.OperationId || original.ActorId != request.Original.Write.ActorId
            || original.Authority != request.ExpectedSource.Authority || original.Operation != request.Original.Operation
            || evidence.SourceStorageFingerprint != request.ExpectedSource.BindingFingerprint
            || evidence.ComparisonTargetFingerprint != request.ComparisonTarget.BindingFingerprint
            || evidence.ComparisonTargetAuthority != request.ComparisonTarget.Authority
            || evidence.OriginalSelectedRecordCount != request.Original.Write.Conditions.Count
            || !evidence.Records.Select(row => row.HolonId).SequenceEqual(request.HolonIds))
            return Error("COSMIC_MIGRATION_PREFLIGHT_SOURCE_SCOPE_MISMATCH", "Source evidence belongs to another operation, actor, physical binding or selected identity set.");
        try
        {
            if (evidence.OriginalRequestFingerprint != ConditionalHolonReceiptEvidenceProjection.RequestFingerprint(request.Original))
                return Error("COSMIC_MIGRATION_PREFLIGHT_SOURCE_SCOPE_MISMATCH", "Source evidence does not bind the unchanged original intent.");
        }
        catch (Exception cause) when (cause is ArgumentException or InvalidOperationException or System.IO.InvalidDataException)
        { return Error("COSMIC_MIGRATION_PREFLIGHT_SOURCE_SCOPE_MISMATCH", "The original request changed or cannot be validated.", cause); }
        return null;
    }
    private static OASISResult<HolonReplicationReceiptPreflightReport> Failure<T>(OASISResult<T> original, string code)
    {
        var result = Error(original?.ErrorCode ?? code, original?.Message ?? "Required native evidence is unavailable.", original?.Exception);
        if (original != null) { result.ErrorCount = Math.Max(1, original.ErrorCount); Observe(result, original); }
        return result;
    }
    private static void Observe<T>(OASISResult<HolonReplicationReceiptPreflightReport> report, OASISResult<T> original)
    {
        report.IsWarning |= original.IsWarning; report.WarningCount += original.WarningCount;
        if (original.Exception != null && report.Exception != original.Exception)
            report.Exception = report.Exception == null ? original.Exception : new AggregateException(report.Exception, original.Exception);
        report.InnerMessages.AddRange(original.InnerMessages);
    }
    private static OASISResult<HolonReplicationReceiptPreflightReport> Error(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
