using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonMigrationMutationPreparationTests
{
    private static HolonMigrationReplacementReport Replacement(HolonReplicationMigrationBarrierPlan plan,
        HolonReplicationMigrationBarrierCoordinatorTests.Setup f, HolonMigrationReplacementKind kind)
        => new(plan, HolonReplicaHeadReadConsistency.NativeSnapshot,
            new[] { HolonReplicaHeadReadConsistency.NativeSnapshot },
            f.Preview.Receipts.SelectMany(r => r.Records).Select(r => r.Original)
                .DistinctBy(r => r.HolonId).OrderBy(r => r.HolonId.ToString("D"), StringComparer.Ordinal)
                .Select(r => new HolonMigrationReplacementRecord(r, false, null, kind)));

    [TestMethod]
    public async Task ExactObservationsAndPolicyAreBoundWithoutNativeWrites()
    {
        var f = new HolonReplicationMigrationBarrierCoordinatorTests.Setup(); await f.Initialize();
        var plan = HolonReplicationMigrationBarrierPlan.Create(f.Preview, f.Context);
        var delivery = new HolonMigrationDeliveryReconciliationReport(plan, 3,
            HolonReplicaHeadReadConsistency.NativeSnapshot, Array.Empty<HolonMigrationDeliveryObservation>());
        var replacement = Replacement(plan, f, HolonMigrationReplacementKind.EmptyAtObservation);
        var first = new HolonMigrationMutationPreparation(plan, delivery, replacement, "mutation-admin");
        var same = new HolonMigrationMutationPreparation(plan, delivery, replacement, "mutation-admin");
        Assert.AreEqual(first.CanonicalJson, same.CanonicalJson);
        Assert.AreEqual(first.Fingerprint, same.Fingerprint);
        Assert.AreNotEqual(first.Fingerprint, new HolonMigrationMutationPreparation(plan, delivery, replacement, "other-admin").Fingerprint);
        var changed = new HolonMigrationDeliveryReconciliationReport(plan, 4,
            HolonReplicaHeadReadConsistency.NativeSnapshot, Array.Empty<HolonMigrationDeliveryObservation>());
        Assert.AreNotEqual(first.Fingerprint, new HolonMigrationMutationPreparation(plan, changed, replacement, "mutation-admin").Fingerprint);
        Assert.IsTrue(f.Providers.All(p => p.Acquires == 0 && p.Reads == 0));
    }

    [DataTestMethod]
    [DataRow(HolonMigrationDeliveryObservationKind.OtherAffectedRoute, HolonDeliveryState.Pending)]
    [DataRow(HolonMigrationDeliveryObservationKind.AdditionalAffectedObligation, HolonDeliveryState.Pending)]
    [DataRow(HolonMigrationDeliveryObservationKind.OriginalReceiverCommitted, HolonDeliveryState.InFlight)]
    [DataRow(HolonMigrationDeliveryObservationKind.OriginalReceiverCommitted, HolonDeliveryState.ReconciliationRequired)]
    public async Task UnresolvedWorkCannotBecomePrepared(HolonMigrationDeliveryObservationKind kind, HolonDeliveryState state)
    {
        var f = new HolonReplicationMigrationBarrierCoordinatorTests.Setup(); await f.Initialize();
        var plan = HolonReplicationMigrationBarrierPlan.Create(f.Preview, f.Context);
        var delivery = new HolonMigrationDeliveryReconciliationReport(plan, 3,
            HolonReplicaHeadReadConsistency.NativeSnapshot,
            new[] { new HolonMigrationDeliveryObservation(1, Guid.NewGuid(), "target", state, new string('A',64), false, false, kind, null) });
        Assert.ThrowsException<ArgumentException>(() => new HolonMigrationMutationPreparation(plan, delivery,
            Replacement(plan, f, HolonMigrationReplacementKind.EmptyAtObservation), "mutation-admin"));
    }

    [DataTestMethod]
    [DataRow(HolonMigrationReplacementKind.ForeignLineage)]
    [DataRow(HolonMigrationReplacementKind.UntrackedLocalRecord)]
    [DataRow(HolonMigrationReplacementKind.LogicalVersionConflict)]
    [DataRow(HolonMigrationReplacementKind.DeletionConflict)]
    [DataRow(HolonMigrationReplacementKind.PortableContentUnverified)]
    [DataRow(HolonMigrationReplacementKind.ContentConflict)]
    public async Task ReplacementConflictsCannotBecomePrepared(HolonMigrationReplacementKind kind)
    {
        var f = new HolonReplicationMigrationBarrierCoordinatorTests.Setup(); await f.Initialize();
        var plan = HolonReplicationMigrationBarrierPlan.Create(f.Preview, f.Context);
        var delivery = new HolonMigrationDeliveryReconciliationReport(plan, 3,
            HolonReplicaHeadReadConsistency.NativeSnapshot, Array.Empty<HolonMigrationDeliveryObservation>());
        Assert.ThrowsException<ArgumentException>(() => new HolonMigrationMutationPreparation(plan, delivery, Replacement(plan, f, kind), "mutation-admin"));
    }
}
