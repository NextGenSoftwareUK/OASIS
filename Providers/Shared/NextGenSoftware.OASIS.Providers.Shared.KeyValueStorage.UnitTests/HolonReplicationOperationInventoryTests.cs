using System;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonReplicationOperationInventoryTests
{
    private readonly CosmicStorageIdentity source = new(ProviderType.SQLLiteDBOASIS, new string('A', 64), Guid.NewGuid());
    private readonly Guid operation = Guid.NewGuid();
    private readonly Guid actor = Guid.NewGuid();
    private HolonReplicationOperationObligation Row(string target, HolonDeliveryState state = HolonDeliveryState.Pending,
        string lease = "private-claim-token", long expiry = 1, Guid? differentOperation = null)
    {
        var inspection = new HolonDeliveryInspection(target == "a" ? 1 : target == "b" ? 2 : target == "\uFFFF" ? 3 : 4, differentOperation ?? operation, target,
            state, state == HolonDeliveryState.Pending ? 0 : 1, null, "private-exception-text", null);
        return HolonReplicationOperationObservation.Capture(inspection, "owned-source", source.Authority, actor,
            new string('B', 64), lease, "private-worker", expiry, 0, state == HolonDeliveryState.Completed ? 1 : 0, null);
    }
    private HolonReplicationOperationReadRequest Request(int size = 64, string after = null)
        => new(source, operation, size, after);

    [TestMethod]
    public void CompleteAndNonHeadObligationsRemainVisibleAndRedacted()
    {
        var rows = new[] { Row("a", HolonDeliveryState.Completed), Row("b") };
        var page = new HolonReplicationOperationPage(Request(), rows, false, HolonReplicaHeadReadConsistency.NativeSnapshot);
        Assert.AreEqual(2, page.Obligations.Count); Assert.AreEqual(HolonDeliveryState.Completed, page.Obligations[0].State);
        Assert.IsNull(page.NextAfterTargetId);
        var json = JsonSerializer.Serialize(page);
        foreach (var hidden in new[] { "private-claim-token", "private-worker", "private-exception-text" }) Assert.IsFalse(json.Contains(hidden, StringComparison.Ordinal));
        Assert.AreEqual(source.BindingFingerprint, page.SourceStorageFingerprint);
    }

    [TestMethod]
    public void HiddenLeaseRenewalChangesStateAndPageDigestsWithoutExposingTheLease()
    {
        var first = Row("a", HolonDeliveryState.InFlight);
        var changed = Row("a", HolonDeliveryState.InFlight, expiry: 2);
        Assert.AreNotEqual(first.StateDigest, changed.StateDigest);
        var original = new HolonReplicationOperationPage(Request(), new[] { first }, false, HolonReplicaHeadReadConsistency.NativeSnapshot);
        var renewed = new HolonReplicationOperationPage(Request(), new[] { changed }, false, HolonReplicaHeadReadConsistency.NativeSnapshot);
        Assert.AreNotEqual(original.ObservationDigest, renewed.ObservationDigest);
        Assert.AreEqual(original.ObservationDigest,
            new HolonReplicationOperationPage(Request(), new[] { first }, false, HolonReplicaHeadReadConsistency.NativeSnapshot).ObservationDigest);
        Assert.AreNotEqual(first.StateDigest, Row("a", HolonDeliveryState.InFlight, lease: "different-token").StateDigest);
    }

    [DataTestMethod]
    [DataRow("foreign-operation")] [DataRow("duplicate")] [DataRow("out-of-order")]
    [DataRow("cursor-repeat")] [DataRow("over-limit")] [DataRow("empty-more")] [DataRow("weak")]
    public void InvalidScopeOrderingAndConsistencyCannotProducePartialPage(string fault)
    {
        var request = Request(fault == "over-limit" ? 1 : 64, fault == "cursor-repeat" ? "a" : null);
        var rows = fault switch {
            "foreign-operation" => new[] { Row("a", differentOperation: Guid.NewGuid()) },
            "duplicate" => new[] { Row("a"), Row("a") },
            "out-of-order" => new[] { Row("b"), Row("a") },
            "over-limit" => new[] { Row("a"), Row("b") },
            "empty-more" => Array.Empty<HolonReplicationOperationObligation>(), _ => new[] { Row("a") } };
        Assert.ThrowsException<ArgumentException>(() => new HolonReplicationOperationPage(request, rows, fault == "empty-more",
            fault == "weak" ? HolonReplicaHeadReadConsistency.ReadCommittedValidated : HolonReplicaHeadReadConsistency.NativeSnapshot));
    }

    [TestMethod]
    public void CursorIsBoundToExactNativePageAndUnicodeBinaryOrdering()
    {
        var first = new HolonReplicationOperationPage(Request(1), new[] { Row("a") }, true, HolonReplicaHeadReadConsistency.NativeSnapshot);
        Assert.AreEqual("a", first.NextAfterTargetId);
        var next = new HolonReplicationOperationPage(Request(1, first.NextAfterTargetId), new[] { Row("b") }, false, HolonReplicaHeadReadConsistency.NativeSnapshot);
        Assert.IsNull(next.NextAfterTargetId); Assert.AreNotEqual(first.ObservationDigest, next.ObservationDigest);
        var unicode = new HolonReplicationOperationPage(Request(), new[] { Row("a"), Row("\uFFFF"), Row("\U0001F600") }, false,
            HolonReplicaHeadReadConsistency.NativeSnapshot);
        Assert.AreEqual(3, unicode.Obligations.Count);
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(257)]
    public void PageBoundsAreExplicit(int size) => Assert.ThrowsException<ArgumentOutOfRangeException>(() => Request(size));
}
