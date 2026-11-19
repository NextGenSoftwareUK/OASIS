using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public enum HolonMigrationReplacementKind
{
    EmptyAtObservation, MatchingOriginalLineage, MatchingReplacementLineage,
    UntrackedLocalRecord, ForeignLineage, LogicalVersionConflict, DeletionConflict,
    PortableContentUnverified, ContentConflict
}
public sealed record HolonMigrationReplacementRecord(ConditionalHolonReceiptRecordEvidence Expected,
    bool HasCurrentRecord, HolonReplicaHeadEvidence Head, HolonMigrationReplacementKind Kind);

/// <summary>Per-page native observations under held barriers, not write admission.
/// Empty does not prove historical receipt absence; matching lineage does not prove
/// ownership by this migration. Adoption still needs exact CAS and migration receipts.</summary>
public sealed class HolonMigrationReplacementReport
{
    public string PlanFingerprint { get; }
    public string StorageFingerprint { get; }
    public HolonReplicaHeadReadConsistency RequiredConsistency { get; }
    public IReadOnlyList<HolonReplicaHeadReadConsistency> PageConsistencies { get; }
    public IReadOnlyList<HolonMigrationReplacementRecord> Records { get; }
    public string ObservationDigest { get; }
    internal HolonMigrationReplacementReport(HolonReplicationMigrationBarrierPlan plan,
        HolonReplicaHeadReadConsistency required, IEnumerable<HolonReplicaHeadReadConsistency> pages, IEnumerable<HolonMigrationReplacementRecord> rows)
    {
        PlanFingerprint = plan.Fingerprint; StorageFingerprint = plan.Context.ReplacementRoute.TargetStorage.BindingFingerprint;
        RequiredConsistency = required; PageConsistencies = Array.AsReadOnly(pages.ToArray()); Records = Array.AsReadOnly(rows.ToArray());
        ObservationDigest = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
            format = "cosmic-migration-replacement-review-v1", PlanFingerprint, StorageFingerprint, RequiredConsistency, PageConsistencies, Records })));
    }
}
public static class HolonMigrationReplacementReview
{
    private static ConditionalHolonReceiptRecordEvidence[] Expectations(HolonReplicationMigrationBarrierPlan plan)
    {
        using var archive = JsonDocument.Parse(plan.CanonicalJson);
        var expected = new Dictionary<Guid, ConditionalHolonReceiptRecordEvidence>();
        foreach (var summary in archive.RootElement.GetProperty("receiptMaterials").EnumerateArray())
        {
            using var receipt = JsonDocument.Parse(summary.GetString());
            foreach (var comparison in receipt.RootElement.GetProperty("Records").EnumerateArray())
            {
                var row = comparison.GetProperty("Original");
                var value = new ConditionalHolonReceiptRecordEvidence(row.GetProperty("HolonId").GetGuid(), row.GetProperty("VersionId").GetGuid(),
                    row.GetProperty("PreviousVersionId").GetGuid(), row.GetProperty("Version").GetInt32(), row.GetProperty("SourceProviderRevisionId").GetGuid(),
                    row.GetProperty("HardDeleted").GetBoolean(), row.GetProperty("PortableContentDigest").GetString());
                if (expected.TryGetValue(value.HolonId, out var previous) && JsonSerializer.Serialize(previous) != JsonSerializer.Serialize(value))
                    throw new HolonMigrationBarrierPlanException("COSMIC_MIGRATION_REPLACEMENT_EXPECTATION_AMBIGUOUS", "Different original versions require fresh source evidence; no latest version was guessed.");
                expected[value.HolonId] = value;
            }
        }
        var values = expected.Values.OrderBy(r => r.HolonId.ToString("D"), StringComparer.Ordinal).ToArray();
        if (!values.Select(r => r.HolonId).SequenceEqual(plan.Scopes[0].HolonIds)) throw new InvalidDataException("Original evidence differs from protected graph scope.");
        return values;
    }
    private static HolonMigrationReplacementKind Classify(HolonReplicationMigrationBarrierPlan plan,
        ConditionalHolonReceiptRecordEvidence expected, HolonReplicaHeadObservation actual)
    {
        var head = actual.Head;
        if (head == null) return actual.HasCurrentRecord ? HolonMigrationReplacementKind.UntrackedLocalRecord : HolonMigrationReplacementKind.EmptyAtObservation;
        if (head.SourceAuthority != plan.Context.SourceStorage.Authority || head.SourceId != plan.Context.OriginalSourceId
            && head.SourceId != plan.Context.ReplacementRoute.SourceId) return HolonMigrationReplacementKind.ForeignLineage;
        if (head.Version != expected.Version || head.VersionId != expected.VersionId || head.PreviousVersionId != expected.PreviousVersionId)
            return HolonMigrationReplacementKind.LogicalVersionConflict;
        if (head.HardDeleted != expected.HardDeleted) return HolonMigrationReplacementKind.DeletionConflict;
        if (head.PortableContentDigest == null) return HolonMigrationReplacementKind.PortableContentUnverified;
        if (head.PortableContentDigest != expected.PortableContentDigest) return HolonMigrationReplacementKind.ContentConflict;
        return head.SourceId == plan.Context.OriginalSourceId ? HolonMigrationReplacementKind.MatchingOriginalLineage : HolonMigrationReplacementKind.MatchingReplacementLineage;
    }
    public static async Task<OASISResult<HolonMigrationReplacementReport>> ReadAsync(HolonReplicationMigrationBarrierPlan plan,
        IHolonReplicationMigrationAuthorizationPolicy policy, IEnumerable<HolonMigrationBarrierParticipant> participants,
        HolonReplicaHeadReadConsistency requiredConsistency = HolonReplicaHeadReadConsistency.NativeSnapshot,
        int pageSize = 64, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan); ArgumentNullException.ThrowIfNull(policy);
        OASISResult<HolonMigrationReplacementReport> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pageSize < 1 || pageSize > 256 || !Enum.IsDefined(requiredConsistency))
                return Error("COSMIC_MIGRATION_REPLACEMENT_OPTIONS_INVALID", "Explicit bounded page size and supported consistency are required.");
            var expected = Expectations(plan);
            var evidence = await HolonMigrationEvidenceReadScope.OpenAsync(plan, policy, participants, cancellationToken).ConfigureAwait(false);
            var store = plan.Context.ReplacementRoute.TargetStorage;
            var participant = evidence.Participants.Single(p => p.Storage.BindingFingerprint == store.BindingFingerprint);
            if (participant.Provider is not IHolonReplicaHeadAuditStorageProvider native)
                return Error("COSMIC_MIGRATION_REPLACEMENT_CAPABILITY_REQUIRED", "The exact fenced replacement must audit protected heads and current content.");
            var rows = new List<HolonMigrationReplacementRecord>(); var consistencies = new List<HolonReplicaHeadReadConsistency>();
            foreach (var group in expected.Chunk(pageSize))
            {
                var request = new HolonReplicaHeadReadRequest(store, group.Select(r => r.HolonId), requiredConsistency);
                var observed = await native.ReadHolonReplicaHeadsAsync(request, cancellationToken).ConfigureAwait(false);
                if (observed == null || observed.IsError || observed.Result == null)
                    return Error(observed?.ErrorCode ?? "COSMIC_MIGRATION_REPLACEMENT_UNVERIFIED", "Native replacement observation failed.", observed?.Exception);
                evidence.Observe(observed);
                var page = observed.Result;
                if (page.StorageFingerprint != store.BindingFingerprint || !page.Records.Select(r => r.HolonId).SequenceEqual(request.HolonIds)
                    || requiredConsistency == HolonReplicaHeadReadConsistency.NativeSnapshot && page.Consistency != requiredConsistency)
                    throw new InvalidDataException("Replacement native binding, complete record selection or consistency differs.");
                consistencies.Add(page.Consistency);
                rows.AddRange(group.Select((original, index) => new HolonMigrationReplacementRecord(original, page.Records[index].HasCurrentRecord,
                    page.Records[index].Head, Classify(plan, original, page.Records[index]))));
            }
            await evidence.CompleteAsync().ConfigureAwait(false);
            return evidence.ApplyWarnings(new OASISResult<HolonMigrationReplacementReport>(new(plan, requiredConsistency, consistencies, rows))
                { IsLoaded = true, Message = "Replacement observations only; no overwrite/adoption, historical receipt absence or cutover permission is inferred." });
        }
        catch (Exception error)
        {
            return Error(error is HolonMigrationEvidenceReadException native ? native.Code : error is HolonMigrationBarrierPlanException scope ? scope.Code
                : error is OperationCanceledException ? "COSMIC_MIGRATION_REPLACEMENT_CANCELLED" : "COSMIC_MIGRATION_REPLACEMENT_UNVERIFIED",
                "No partial replacement report or mutation was returned.", error is HolonMigrationEvidenceReadException failure ? failure.InnerException ?? failure : error);
        }
    }
}
