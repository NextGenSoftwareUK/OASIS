using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Exact native comparison intent for adopting an existing matching
/// receiver graph. Does not grant approval or permit empty/foreign/local records.
/// Native execution must atomically verify fence owner/started preparation,
/// historical admission and every head/current record, then store its receipt.
/// Original historical replica receipts and logical/local revisions stay unchanged.</summary>
public sealed class HolonMigrationReplicaAdoptionPlan
{
    public HolonMigrationFenceRequest Fence { get; }
    public string PreparationFingerprint { get; }
    public string OriginalSourceId { get; }
    public string ReplacementSourceId { get; }
    public IReadOnlyList<HolonMigrationReplacementRecord> Records { get; }
    public string CanonicalJson { get; }
    public string Fingerprint { get; }

    public HolonMigrationReplicaAdoptionPlan(HolonReplicationMigrationBarrierPlan plan,
        HolonMigrationMutationPreparation preparation)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(preparation);
        if (preparation.PlanFingerprint != plan.Fingerprint
            || preparation.AuthorizationContextDigest != plan.Context.ContextDigest)
            throw new ArgumentException("Adoption requires the exact originally prepared plan.");
        var rows = preparation.ReplacementRecords;
        if (rows.Count == 0 || !rows.Select(r => r.Expected.HolonId).SequenceEqual(plan.Scopes[0].HolonIds)
            || rows.Any(r => r.Kind != HolonMigrationReplacementKind.MatchingOriginalLineage
                || r.Head == null || r.HasCurrentRecord == r.Head.HardDeleted
                || r.Head.SourceId != plan.Context.OriginalSourceId
                || r.Head.SourceAuthority != plan.Context.SourceStorage.Authority
                || r.Head.Version != r.Expected.Version || r.Head.VersionId != r.Expected.VersionId
                || r.Head.PreviousVersionId != r.Expected.PreviousVersionId
                || r.Head.HardDeleted != r.Expected.HardDeleted
                || r.Head.PortableContentDigest != r.Expected.PortableContentDigest))
            throw new ArgumentException("Only complete, exactly matching original replica heads admit adoption comparison.");
        OriginalSourceId = plan.Context.OriginalSourceId;
        ReplacementSourceId = plan.Context.ReplacementRoute.SourceId;
        // V2 source identity is independent of destination/policy. Adopting a
        // destination may keep this ID unchanged; the distinct migration receipt
        // still proves admission without inventing a new logical source.
        Fence = plan.Scopes.Single(s => s.ExpectedStorage.BindingFingerprint
            == plan.Context.ReplacementRoute.TargetStorage.BindingFingerprint);
        PreparationFingerprint = preparation.Fingerprint;
        Records = Array.AsReadOnly(rows.ToArray());
        CanonicalJson = JsonSerializer.Serialize(new {
            format = "cosmic-migration-replica-adoption-v1", PreparationFingerprint,
            fence = Fence.Fingerprint, OriginalSourceId, ReplacementSourceId, Records });
        Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalJson)));
    }
}

/// <summary>Optional native atomic existing-replica adoption. Trusted host must
/// enforce current approval; the native store still verifies Started ownership
/// and exact immutable preparation. No implicit ordinary-write fallback.</summary>
public interface IHolonMigrationReplicaAdoptionStorageProvider
{
    System.Threading.Tasks.Task<NextGenSoftware.OASIS.Common.OASISResult<HolonMigrationFenceObservation>> AdoptPreparedHolonReplicaAsync(
        HolonMigrationReplicaAdoptionPlan plan, Guid owner, System.Threading.CancellationToken token = default);
}
