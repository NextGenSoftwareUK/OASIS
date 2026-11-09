using System;
using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class CommittedHolonReplicaReceiptProjectionTests
{
    private sealed class Fixture
    {
        public readonly Holon Row = new() { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(), Name = "private-historical-value" };
        public readonly CosmicStorageIdentity Target = new(ProviderType.SQLLiteDBOASIS, new string('A',64), Guid.NewGuid());
        public CommittedHolonReplicaReceiptReadRequest Request;
        public CommittedHolonReplicaPlan Plan;
        public string Json;
        public Fixture()
        {
            CosmicConcurrency.SetProviderRevisionId(Row, ProviderType.Neo4jOASIS, Guid.NewGuid());
            var original = new CommittedHolonReplicaRequest(Guid.NewGuid(), "original-source", ProviderType.Neo4jOASIS,
                Guid.NewGuid(), ConditionalHolonOperation.Save, new[] { Row }, saveChildren: false);
            Request = new(original, Target); Plan = CommittedHolonReplicaReceiptProjection.Prepare(Request);
            var acknowledged = StoredRecordJson.Project<Holon>(Row);
            CosmicConcurrency.SetProviderRevisionId(acknowledged, Target.Authority, Guid.NewGuid());
            Json = ConditionalHolonReceipt.Write(new[] { acknowledged }, Plan);
        }
        public CommittedHolonReplicaReceiptEvidence Project(string fingerprint, string json)
            => CommittedHolonReplicaReceiptProjection.Project(Request, Plan, fingerprint, json, HolonReplicaHeadReadConsistency.NativeSnapshot);
    }

    [TestMethod]
    public void HistoricalAcknowledgementIsVerifiedWithoutCurrentHeadOrBusinessValuesInResult()
    {
        var f = new Fixture(); var evidence = f.Project(f.Plan.Fingerprint, f.Json);
        Assert.AreEqual(ConditionalHolonReceiptState.Committed, evidence.ReceiptState);
        Assert.AreEqual(f.Request.Original.OperationId, evidence.OperationId);
        Assert.AreEqual(f.Request.Original.ActorId, evidence.ActorId);
        Assert.AreEqual(f.Target.BindingFingerprint, evidence.TargetStorageFingerprint);
        Assert.AreEqual(1, evidence.SelectedRecordCount); Assert.IsNotNull(evidence.AcknowledgementDigest);
        Assert.IsFalse(JsonSerializer.Serialize(evidence).Contains("private-historical-value"));
        Assert.AreEqual(evidence.AcknowledgementDigest, f.Project(f.Plan.Fingerprint, f.Json).AcknowledgementDigest);
    }

    [TestMethod]
    public void ReceiptAbsenceIsExplicitAndCannotCarryAnAcknowledgement()
    {
        var f = new Fixture(); var evidence = f.Project(null, null);
        Assert.AreEqual(ConditionalHolonReceiptState.Absent, evidence.ReceiptState); Assert.IsNull(evidence.AcknowledgementDigest);
        Assert.ThrowsException<ArgumentException>(() => new CommittedHolonReplicaReceiptEvidence(f.Request,
            f.Plan.Fingerprint, ConditionalHolonReceiptState.Absent, new string('B',64), 1, HolonReplicaHeadReadConsistency.NativeSnapshot));
        Assert.ThrowsException<ArgumentException>(() => new CommittedHolonReplicaReceiptEvidence(f.Request,
            f.Plan.Fingerprint, ConditionalHolonReceiptState.Committed, null, 1, HolonReplicaHeadReadConsistency.NativeSnapshot));
    }

    [DataTestMethod]
    [DataRow("fingerprint")] [DataRow("missing-json")] [DataRow("missing-fingerprint")]
    [DataRow("checksum")] [DataRow("changed-original")] [DataRow("forged-ack")]
    public void InvalidOrChangedOriginalAcknowledgementsAreNeverReportedAsCommitted(string fault)
    {
        var f = new Fixture(); var fingerprint = f.Plan.Fingerprint; var json = f.Json;
        if (fault == "fingerprint") fingerprint = new string('F',64);
        if (fault == "missing-json") json = null;
        if (fault == "missing-fingerprint") fingerprint = null;
        if (fault == "checksum") json = json.Replace("private-historical-value", "tampered-historical-value");
        if (fault == "changed-original") f.Row.Name = "different-original";
        if (fault == "forged-ack")
        {
            var forged = StoredRecordJson.Project<Holon>(f.Row); forged.Name = "forged-ack";
            CosmicConcurrency.SetProviderRevisionId(forged, f.Target.Authority, Guid.NewGuid());
            json = ConditionalHolonReceipt.Write(new[] { forged }, f.Plan);
        }
        Assert.ThrowsException<InvalidDataException>(() => f.Project(fingerprint, json));
    }

    [TestMethod]
    public void StrictReceiptReadCannotBePresentedAsAWeakerObservation()
    {
        var f = new Fixture();
        Assert.ThrowsException<ArgumentException>(() => CommittedHolonReplicaReceiptProjection.Project(f.Request,
            f.Plan, f.Plan.Fingerprint, f.Json, HolonReplicaHeadReadConsistency.ReadCommittedValidated));
        Assert.ThrowsException<ArgumentException>(() => new CommittedHolonReplicaReceiptEvidence(f.Request,
            f.Plan.Fingerprint, ConditionalHolonReceiptState.Absent, null, 0, HolonReplicaHeadReadConsistency.NativeSnapshot));
    }
}
