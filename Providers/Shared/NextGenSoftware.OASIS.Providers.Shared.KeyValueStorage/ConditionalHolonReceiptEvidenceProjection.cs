using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Only call after the native receipt owner has fully verified the original intent.</summary>
public static class ConditionalHolonReceiptEvidenceProjection
{
    public static ConditionalHolonReceiptEvidence Create(ConditionalHolonReceiptEvidenceRequest request,
        IEnumerable<IHolon> verifiedRoots)
    {
        ArgumentNullException.ThrowIfNull(request);
        var graph = HolonGraphWritePlan.Create(verifiedRoots, true, true, 0, 0);
        var records = graph.Nodes.ToDictionary(node => node.Snapshot.Id, node => node.Snapshot);
        if (!records.Keys.ToHashSet().SetEquals(request.Original.Write.Conditions.Select(c => c.HolonId)))
            throw new InvalidDataException("Verified receipt graph differs from the original selected records.");
        return new ConditionalHolonReceiptEvidence(request, records.Count, request.HolonIds.Select(id =>
        {
            var record = records[id];
            var revision = CosmicConcurrency.GetProviderRevisionId(record, request.ExpectedSource.Authority);
            if (!revision.HasValue) throw new InvalidDataException("Receipt has no source-owned revision.");
            return new ConditionalHolonReceiptRecordEvidence(id, record.VersionId, record.PreviousVersionId,
                record.Version, revision.Value, request.Original.Operation == ConditionalHolonOperation.HardDelete,
                CommittedHolonReplicaPlan.PortableContentDigest(record, request.ComparisonTarget.Authority));
        }));
    }
}
