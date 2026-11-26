using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

[TestClass]
public sealed class HolonReplicaReviewedHoldCodesTests
{
    [DataTestMethod]
    [DataRow("COSMIC_REPLICA_LINEAGE_CONFLICT")]
    [DataRow("COSMIC_REPLICATION_ROUTE_UNBOUND")]
    public void AcceptsHoldsWithoutNativeExecution(string code) => Assert.IsTrue(HolonReplicaReviewedHoldCodes.IsReviewable(code));

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("COSMIC_REPLICATION_ROUTE_CHANGED")]
    [DataRow("COSMIC_REPLICA_AUTHORIZATION_DENIED")]
    [DataRow("COSMIC_REPLICA_PAYLOAD_INVALID")]
    [DataRow("cosmic_replica_lineage_conflict")]
    public void RefusesEverythingElse(string code) => Assert.IsFalse(HolonReplicaReviewedHoldCodes.IsReviewable(code));
}
