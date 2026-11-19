using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonReplicationLineageInventoryTests
{
    private readonly CosmicStorageIdentity source = new(ProviderType.SQLLiteDBOASIS, new string('A', 64), Guid.NewGuid());
    private HolonReplicationOperationObligation Row(long sequence, string sourceId = "lineage", HolonDeliveryState state = HolonDeliveryState.Pending)
        => HolonReplicationOperationObservation.Capture(new HolonDeliveryInspection(sequence, Guid.NewGuid(), "target", state,
            state == HolonDeliveryState.Pending ? 0 : 1, null, null, null), sourceId, source.Authority, Guid.NewGuid(),
            new string('B', 64), "hidden-token", "hidden-worker", 1, 0, state == HolonDeliveryState.Completed ? 1 : 0, null);

    [TestMethod]
    public void WindowIncludesMultipleOperationsAndCompletedRowsWithoutClaimingQuiescence()
    {
        var rows = new[] { Row(1, state: HolonDeliveryState.Completed), Row(2) };
        var request = new HolonReplicationLineageReadRequest(source, "lineage", 2);
        var page = new HolonReplicationLineagePage(request, 3, rows, true, HolonReplicaHeadReadConsistency.NativeSnapshot);
        Assert.AreEqual(2L, page.NextAfterSequence); Assert.AreEqual(3L, page.ThroughSequence);
        Assert.AreEqual(2, page.Obligations.Select(row => row.OperationId).Distinct().Count());
        var continued = new HolonReplicationLineagePage(new(source, "lineage", 2, 2, page.ThroughSequence), 3,
            new[] { Row(3) }, false, HolonReplicaHeadReadConsistency.NativeSnapshot);
        Assert.IsNull(continued.NextAfterSequence);
        Assert.IsFalse(System.Text.Json.JsonSerializer.Serialize(page).Contains("hidden-token", StringComparison.Ordinal));
    }

    [DataTestMethod]
    [DataRow("outside-window")] [DataRow("reversed")] [DataRow("other-lineage")]
    [DataRow("changed-boundary")] [DataRow("weaker")] [DataRow("empty-more")]
    public void NativeObservationMustRetainScopeBoundaryOrderingAndConsistency(string fault)
    {
        var request = new HolonReplicationLineageReadRequest(source, "lineage", 2, throughSequence: 2);
        var rows = fault switch { "outside-window" => new[] { Row(3) }, "reversed" => new[] { Row(2), Row(1) },
            "other-lineage" => new[] { Row(1, "other") }, "empty-more" => Array.Empty<HolonReplicationOperationObligation>(), _ => new[] { Row(1) } };
        Assert.ThrowsException<ArgumentException>(() => new HolonReplicationLineagePage(request, fault == "changed-boundary" ? 3 : 2,
            rows, fault == "empty-more", fault == "weaker" ? HolonReplicaHeadReadConsistency.ReadCommittedValidated : HolonReplicaHeadReadConsistency.NativeSnapshot));
    }

    [TestMethod]
    public void SequenceWindowRetainsFullInt64PrecisionAndStableObservationBinding()
    {
        const long value = 9007199254741001L;
        var row = Row(value);
        var request = new HolonReplicationLineageReadRequest(source, "lineage", 1, value - 1, value);
        var page = new HolonReplicationLineagePage(request, value, new[] { row }, false, HolonReplicaHeadReadConsistency.NativeSnapshot);
        Assert.AreEqual(value, page.ThroughSequence);
        Assert.AreEqual(page.ObservationDigest, new HolonReplicationLineagePage(request, value, new[] { row }, false, page.Consistency).ObservationDigest);
        Assert.AreNotEqual(page.ObservationDigest, new HolonReplicationLineagePage(new(source, "lineage", 2, value - 1, value),
            value, new[] { row }, false, page.Consistency).ObservationDigest);
    }

    [DataTestMethod]
    [DataRow(-1L, 2L)] [DataRow(3L, 2L)]
    public void InvalidWindowCannotBeRequested(long after, long through)
        => Assert.ThrowsException<ArgumentOutOfRangeException>(() => new HolonReplicationLineageReadRequest(source, "lineage", afterSequence: after, throughSequence: through));

    [TestMethod]
    public void ContinuationCannotRecaptureANewUpperBoundary()
        => Assert.ThrowsException<ArgumentOutOfRangeException>(() => new HolonReplicationLineageReadRequest(source, "lineage", afterSequence: 1));
}
