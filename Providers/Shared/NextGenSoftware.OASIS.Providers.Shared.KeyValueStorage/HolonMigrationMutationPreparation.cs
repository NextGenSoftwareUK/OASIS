using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests")]

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Immutable exact evidence to approve before starting native mutation.
/// This is neither approval nor a durable commit decision. Native CAS must still
/// verify the observed states; constructing this object never touches a provider.</summary>
public sealed class HolonMigrationMutationPreparation
{
    public string PlanFingerprint { get; }
    public string AuthorizationContextDigest { get; }
    public string DeliveryObservationDigest { get; }
    public string ReplacementObservationDigest { get; }
    public string MutationPolicyBinding { get; }
    public HolonReplicaHeadReadConsistency ApprovedSourceConsistency { get; }
    public HolonReplicaHeadReadConsistency ApprovedReplacementConsistency { get; }
    public string DeliveryReviewJson { get; }
    public string ReplacementReviewJson { get; }
    public string CanonicalJson { get; }
    public string Fingerprint { get; }

    public HolonMigrationMutationPreparation(HolonReplicationMigrationBarrierPlan plan,
        HolonMigrationDeliveryReconciliationReport delivery,
        HolonMigrationReplacementReport replacement, string mutationPolicyBinding)
        : this(plan, delivery, replacement, mutationPolicyBinding,
            HolonReplicaHeadReadConsistency.NativeSnapshot, HolonReplicaHeadReadConsistency.NativeSnapshot) { }

    /// <summary>Explicit consistency admission. Weaker evidence must be selected
    /// by the host, retained in the approval fingerprint and matched to the original
    /// source review. It never substitutes for native CAS before business writes.</summary>
    public HolonMigrationMutationPreparation(HolonReplicationMigrationBarrierPlan plan,
        HolonMigrationDeliveryReconciliationReport delivery,
        HolonMigrationReplacementReport replacement, string mutationPolicyBinding,
        HolonReplicaHeadReadConsistency approvedSourceConsistency,
        HolonReplicaHeadReadConsistency approvedReplacementConsistency)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(replacement);
        HolonReplicationMigrationAuthorizationContext.ValidateToken(mutationPolicyBinding);

        if (delivery.PlanFingerprint != plan.Fingerprint || replacement.PlanFingerprint != plan.Fingerprint
            || replacement.StorageFingerprint != plan.Context.ReplacementRoute.TargetStorage.BindingFingerprint)
            throw new ArgumentException("Mutation preparation must use observations of the exact original plan and replacement store.");

        if (!Enum.IsDefined(approvedSourceConsistency) || !Enum.IsDefined(approvedReplacementConsistency))
            throw new ArgumentException("Explicit supported evidence consistency is required.");
        using var originalPlan = JsonDocument.Parse(plan.CanonicalJson);
        using var originalPreview = JsonDocument.Parse(originalPlan.RootElement.GetProperty("previewMaterial").GetString());
        if (originalPreview.RootElement.GetProperty("SourceConsistency").GetInt32() != (int)approvedSourceConsistency)
            throw new ArgumentException("Mutation source consistency must match the original independently reviewed plan.");

        // These observations cannot authorize treatment of another route or an
        // unresolved worker. A wider migration needs its own reviewed disposition.
        if (delivery.SourceConsistency != approvedSourceConsistency
            || delivery.Obligations.Any(r => r.RequiresNativeReconciliation
                || r.SourceState == HolonDeliveryState.InFlight
                || r.SourceState == HolonDeliveryState.ReconciliationRequired
                || r.Kind == HolonMigrationDeliveryObservationKind.OtherAffectedRoute
                || r.Kind == HolonMigrationDeliveryObservationKind.AdditionalAffectedObligation))
            throw new ArgumentException("Unresolved workers, affected routes or weak observations require fresh migration review.");

        if (replacement.RequiredConsistency != approvedReplacementConsistency
            || replacement.PageConsistencies.Count == 0
            || replacement.PageConsistencies.Any(c => c != approvedReplacementConsistency)
            || replacement.Records.Any(r => r.Kind != HolonMigrationReplacementKind.EmptyAtObservation
                && r.Kind != HolonMigrationReplacementKind.MatchingOriginalLineage
                && r.Kind != HolonMigrationReplacementKind.MatchingReplacementLineage)
            || !replacement.Records.Select(r => r.Expected.HolonId).SequenceEqual(plan.Scopes[0].HolonIds))
            throw new ArgumentException("Replacement selection, consistency or ownership conflicts prevent mutation preparation.");

        PlanFingerprint = plan.Fingerprint;
        AuthorizationContextDigest = plan.Context.ContextDigest;
        DeliveryObservationDigest = delivery.ObservationDigest;
        ReplacementObservationDigest = replacement.ObservationDigest;
        MutationPolicyBinding = mutationPolicyBinding;
        ApprovedSourceConsistency = approvedSourceConsistency;
        ApprovedReplacementConsistency = approvedReplacementConsistency;
        DeliveryReviewJson = JsonSerializer.Serialize(delivery);
        ReplacementReviewJson = JsonSerializer.Serialize(replacement);
        CanonicalJson = JsonSerializer.Serialize(new {
            format = "cosmic-migration-mutation-preparation-v1", PlanFingerprint,
            AuthorizationContextDigest, DeliveryObservationDigest,
            ReplacementObservationDigest, MutationPolicyBinding, ApprovedSourceConsistency, ApprovedReplacementConsistency, DeliveryReviewJson, ReplacementReviewJson });
        Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalJson)));
    }

    private static HolonMigrationReplacementRecord ReadReplacementRecord(JsonElement row)
    {
        var h = row.GetProperty("Head");
        HolonReplicaHeadEvidence head = h.ValueKind == JsonValueKind.Null ? null : new(
            h.GetProperty("SourceId").GetString(),
            (NextGenSoftware.OASIS.API.Core.Enums.ProviderType)h.GetProperty("SourceAuthority").GetInt32(),
            h.GetProperty("VersionId").GetGuid(), h.GetProperty("PreviousVersionId").GetGuid(),
            h.GetProperty("Version").GetInt32(), h.GetProperty("ProviderRevisionId").GetGuid(),
            h.GetProperty("HardDeleted").GetBoolean(), h.GetProperty("SnapshotDigest").GetString(),
            h.GetProperty("PortableContentDigest").GetString());
        return new(row.GetProperty("Expected").Deserialize<ConditionalHolonReceiptRecordEvidence>(),
            row.GetProperty("HasCurrentRecord").GetBoolean(), head,
            (HolonMigrationReplacementKind)row.GetProperty("Kind").GetInt32());
    }

    /// <summary>Restore only against the independently retained approval fingerprint
    /// and original verified plan. A checksum stored alongside this file is not approval.</summary>
    public static HolonMigrationMutationPreparation ReadCanonical(string json,
        HolonReplicationMigrationBarrierPlan plan, string approvedFingerprint)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (json == null || Encoding.UTF8.GetByteCount(json) > 16 * 1024 * 1024)
            throw new InvalidDataException("Mutation preparation exceeds its bounded evidence limit.");
        using var archive = JsonDocument.Parse(json);
        var root = archive.RootElement;
        using var deliveryJson = JsonDocument.Parse(root.GetProperty("DeliveryReviewJson").GetString());
        using var replacementJson = JsonDocument.Parse(root.GetProperty("ReplacementReviewJson").GetString());
        var d = deliveryJson.RootElement;
        var r = replacementJson.RootElement;
        if (d.GetProperty("PlanFingerprint").GetString() != plan.Fingerprint
            || r.GetProperty("PlanFingerprint").GetString() != plan.Fingerprint
            || r.GetProperty("StorageFingerprint").GetString() != plan.Context.ReplacementRoute.TargetStorage.BindingFingerprint)
            throw new InvalidDataException("Archived observations belong to another plan or store.");
        var delivery = new HolonMigrationDeliveryReconciliationReport(plan,
            d.GetProperty("ThroughSequence").GetInt64(),
            (HolonReplicaHeadReadConsistency)d.GetProperty("SourceConsistency").GetInt32(),
            d.GetProperty("Obligations").Deserialize<HolonMigrationDeliveryObservation[]>());
        var replacement = new HolonMigrationReplacementReport(plan,
            (HolonReplicaHeadReadConsistency)r.GetProperty("RequiredConsistency").GetInt32(),
            r.GetProperty("PageConsistencies").Deserialize<HolonReplicaHeadReadConsistency[]>(),
            r.GetProperty("Records").EnumerateArray().Select(ReadReplacementRecord).ToArray());
        var restored = new HolonMigrationMutationPreparation(plan, delivery, replacement,
            root.GetProperty("MutationPolicyBinding").GetString(),
            (HolonReplicaHeadReadConsistency)root.GetProperty("ApprovedSourceConsistency").GetInt32(),
            (HolonReplicaHeadReadConsistency)root.GetProperty("ApprovedReplacementConsistency").GetInt32());
        if (restored.CanonicalJson != json || restored.Fingerprint != approvedFingerprint)
            throw new InvalidDataException("Preparation differs from the exact independently approved canonical evidence.");
        return restored;
    }

}
