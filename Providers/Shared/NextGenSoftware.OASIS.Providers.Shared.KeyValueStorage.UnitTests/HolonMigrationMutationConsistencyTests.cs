using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonMigrationMutationConsistencyTests
{
    [DataTestMethod]
    [DataRow(HolonReplicaHeadReadConsistency.NativeSnapshot, HolonReplicaHeadReadConsistency.NativeSnapshot)]
    [DataRow(HolonReplicaHeadReadConsistency.ReadCommittedValidated, HolonReplicaHeadReadConsistency.NativeSnapshot)]
    [DataRow(HolonReplicaHeadReadConsistency.NativeSnapshot, HolonReplicaHeadReadConsistency.ReadCommittedValidated)]
    [DataRow(HolonReplicaHeadReadConsistency.ReadCommittedValidated, HolonReplicaHeadReadConsistency.ReadCommittedValidated)]
    public async Task ExplicitConsistencyIsBoundRestoredAndNeverAutomaticallyDowngraded(
        HolonReplicaHeadReadConsistency source, HolonReplicaHeadReadConsistency replacement)
    {
        var f = new HolonReplicationMigrationPreviewTests.Fixture { SourceConsistency = source };
        f.ReplicaReceipt = row => new(f.Delivered(row));
        f.PayloadProof = (row, receipt) => new(f.Proof(row, receipt, source));
        var preview = await f.Read(); Assert.IsFalse(preview.IsError, preview.Message);
        var context = preview.Result.AuthorizationContext(Guid.NewGuid(), Guid.NewGuid(), "owned-admin");
        var plan = HolonReplicationMigrationBarrierPlan.Create(preview.Result, context);
        var delivery = new HolonMigrationDeliveryReconciliationReport(plan, 3, source, Array.Empty<HolonMigrationDeliveryObservation>());
        var target = new HolonMigrationReplacementReport(plan, replacement, new[] { replacement },
            preview.Result.Receipts.SelectMany(r => r.Records).Select(r => r.Original).DistinctBy(r => r.HolonId)
                .OrderBy(r => r.HolonId.ToString("D"), StringComparer.Ordinal)
                .Select(r => new HolonMigrationReplacementRecord(r, true,
                    new HolonReplicaHeadEvidence(context.OriginalSourceId, context.SourceStorage.Authority, r.VersionId,
                        r.PreviousVersionId, r.Version, Guid.NewGuid(), false, new string('A',64), r.PortableContentDigest),
                    HolonMigrationReplacementKind.MatchingOriginalLineage)));
        if (source != HolonReplicaHeadReadConsistency.NativeSnapshot || replacement != HolonReplicaHeadReadConsistency.NativeSnapshot)
            Assert.ThrowsException<ArgumentException>(() => new HolonMigrationMutationPreparation(plan, delivery, target, "mutation-admin"));
        var explicitPreparation = new HolonMigrationMutationPreparation(plan, delivery, target, "mutation-admin", source, replacement);
        var restored = HolonMigrationMutationPreparation.ReadCanonical(explicitPreparation.CanonicalJson, plan, explicitPreparation.Fingerprint);
        Assert.AreEqual(source, restored.ApprovedSourceConsistency);
        Assert.AreEqual(replacement, restored.ApprovedReplacementConsistency);
        Assert.AreEqual(explicitPreparation.CanonicalJson, restored.CanonicalJson);
        var wrong = source == HolonReplicaHeadReadConsistency.NativeSnapshot
            ? HolonReplicaHeadReadConsistency.ReadCommittedValidated : HolonReplicaHeadReadConsistency.NativeSnapshot;
        Assert.ThrowsException<ArgumentException>(() => new HolonMigrationMutationPreparation(plan, delivery, target, "mutation-admin", wrong, replacement));
        Assert.ThrowsException<ArgumentException>(() => new HolonMigrationMutationPreparation(plan, delivery, target, "mutation-admin", source, (HolonReplicaHeadReadConsistency)999));
    }
}
