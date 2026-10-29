using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>One protected snapshot/header validator shared by replica writes and audit reads.</summary>
public sealed class HolonReplicaHeadRecord
{
    public Holon Snapshot { get; }
    public HolonReplicaHeadEvidence Evidence { get; }
    private HolonReplicaHeadRecord(Holon snapshot, HolonReplicaHeadEvidence evidence)
    { Snapshot = snapshot; Evidence = evidence; }
    public static HolonReplicaHeadRecord Read(Guid id, ProviderType targetAuthority, string sourceId,
        string sourceAuthority, string snapshotJson, string snapshotDigest, string providerRevisionId, bool hardDeleted)
    {
        CosmicConcurrency.ValidateAuthority(targetAuthority);
        if (id == Guid.Empty || snapshotJson == null
            || Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson))) != snapshotDigest)
            throw new InvalidDataException("Committed replica head checksum is invalid.");
        if (!Enum.TryParse<ProviderType>(sourceAuthority, out var authority) || authority.ToString() != sourceAuthority
            || !Guid.TryParse(providerRevisionId, out var revision))
            throw new InvalidDataException("Committed replica head lineage or revision is invalid.");
        var stored = StoredRecordJson.DeserializeStored<Holon>(snapshotJson);
        if (stored == null || stored.Id != id || hardDeleted && !stored.IsDeleted
            || CosmicConcurrency.GetProviderRevisionId(stored, targetAuthority) != revision)
            throw new InvalidDataException("Committed replica head snapshot/revision is inconsistent.");
        var evidence = new HolonReplicaHeadEvidence(sourceId, authority, stored.VersionId,
            stored.PreviousVersionId, stored.Version, revision, hardDeleted, snapshotDigest,
            CommittedHolonReplicaPlan.PortableContentDigest(stored, targetAuthority));
        return new(stored, evidence);
    }
}
