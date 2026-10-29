using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Redacted unfinished-route observation. An observed native descriptor is
/// provenance, not current authorization or permission to migrate a replica head.</summary>
public sealed class HolonReplicationMigrationHead
{
    public long Sequence { get; }
    public Guid OperationId { get; }
    public string TargetId { get; }
    public HolonDeliveryState State { get; }
    public int AttemptCount { get; }
    public string ErrorCode { get; }
    public bool RequiresNativeBindingProof { get; }
    public string TargetStorageFingerprint { get; }
    internal HolonReplicationMigrationHead(HolonDeliveryInspection head)
    {
        Sequence = head.Sequence; OperationId = head.OperationId; TargetId = head.TargetId;
        State = head.State; AttemptCount = head.AttemptCount; ErrorCode = head.ErrorCode;
        RequiresNativeBindingProof = head.NativeRoute == null;
        TargetStorageFingerprint = head.NativeRoute?.TargetStorage.BindingFingerprint;
    }
}

/// <summary>A single bounded native observation, not a cross-page snapshot or migration approval.</summary>
public sealed class HolonReplicationMigrationAuditPage
{
    public string SourceStorageFingerprint { get; }
    public IReadOnlyList<HolonReplicationMigrationHead> Heads { get; }
    public string NextAfterTargetId { get; }
    public string ObservationDigest { get; }
    internal HolonReplicationMigrationAuditPage(CosmicStorageIdentity source, HolonReplicationRoutePage page, string after)
    {
        SourceStorageFingerprint = source.BindingFingerprint;
        Heads = Array.AsReadOnly(page.Heads.Select(head => new HolonReplicationMigrationHead(head)).ToArray());
        NextAfterTargetId = page.NextAfterTargetId;
        // Versioned, fixed-property-order material binds the exact observed page and cursor.
        // Business payloads, endpoint configuration and exception text are excluded.
        var material = JsonSerializer.Serialize(new {
            format = 1, source = SourceStorageFingerprint, after, next = NextAfterTargetId,
            heads = Heads.Select(head => new {
                head.Sequence, head.OperationId, head.TargetId, state = head.State.ToString(),
                head.AttemptCount, head.ErrorCode, head.RequiresNativeBindingProof, head.TargetStorageFingerprint
            }).ToArray()
        });
        ObservationDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }
}

/// <summary>Operator preflight inventory only. It never captures/initializes store metadata,
/// discovers a destination, claims work, retries a mutation, resets state or rewrites lineage.</summary>
public static class HolonReplicationMigrationAudit
{
    public static async Task<OASISResult<HolonReplicationMigrationAuditPage>> ReadPageAsync(
        IBoundHolonReplicationOutboxSource source, CosmicStorageIdentity expectedSource,
        int pageSize = 64, string afterTargetId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (source == null || expectedSource == null)
            return Error("COSMIC_MIGRATION_AUDIT_OPTIONS_INVALID", "An explicit source and previously captured native binding are required.");
        try { HolonReplicationRoutePage.ValidateRequest(pageSize, afterTargetId); }
        catch (ArgumentException cause)
        { return Error("COSMIC_MIGRATION_AUDIT_OPTIONS_INVALID", cause.Message, cause); }

        var opened = source.GetBoundHolonReplicationOutbox(expectedSource);
        if (opened == null || opened.IsError || opened.Result == null)
            return Failure(opened, "COSMIC_MIGRATION_AUDIT_SOURCE_UNAVAILABLE");
        var observed = await opened.Result.ReadRouteHeadsAsync(pageSize, afterTargetId, cancellationToken).ConfigureAwait(false);
        if (observed == null || observed.IsError || observed.Result == null)
            return Failure(observed, "COSMIC_MIGRATION_AUDIT_NO_OBSERVATION");
        var page = observed.Result;
        var previous = afterTargetId;
        foreach (var head in page.Heads)
        {
            if (head.Sequence <= 0 || head.OperationId == Guid.Empty || head.AttemptCount < 0
                || previous != null && StringComparer.Ordinal.Compare(head.TargetId, previous) <= 0
                || page.Heads.Count > pageSize)
                return Error("COSMIC_MIGRATION_AUDIT_INVALID_PAGE", "The source returned an invalid or out-of-bound route page.");
            if (head.NativeRoute != null && head.NativeRoute.SourceStorage.BindingFingerprint != expectedSource.BindingFingerprint)
                return Error("COSMIC_MIGRATION_AUDIT_SOURCE_CHANGED", "A route descriptor names a different physical source; no partial audit is returned.");
            previous = head.TargetId;
        }
        var report = new OASISResult<HolonReplicationMigrationAuditPage>(new(expectedSource, page, afterTargetId));
        ObserveWarning(report, opened);
        ObserveWarning(report, observed);
        return report;
    }

    private static OASISResult<HolonReplicationMigrationAuditPage> Failure<T>(OASISResult<T> original, string code)
    {
        var result = Error(original?.ErrorCode ?? code, original?.Message ?? "No source result was returned.", original?.Exception);
        if (original != null) { result.ErrorCount = Math.Max(1, original.ErrorCount); ObserveWarning(result, original); }
        return result;
    }
    private static void ObserveWarning<T>(OASISResult<HolonReplicationMigrationAuditPage> report, OASISResult<T> original)
    {
        report.IsWarning |= original.IsWarning;
        report.WarningCount += original.WarningCount;
        if (original.Exception != null && report.Exception != original.Exception)
            report.Exception = report.Exception == null ? original.Exception : new AggregateException(report.Exception, original.Exception);
        report.InnerMessages.AddRange(original.InnerMessages);
    }
    private static OASISResult<HolonReplicationMigrationAuditPage> Error(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
