using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonRecoveryEvidenceJsonTests
{
    [TestMethod]
    public void PersistedFormatOneBytesRemainStableAcrossNativeStores()
    {
        const string expected="{\"format\":1,\"requiresNativeReconciliation\":true,\"originalTransactionIds\":[\"native\"]}";
        var actual=HolonRecoveryEvidenceJson.Write(new HolonDeliveryRecoveryEvidence(true,new[]{"native"}));Assert.AreEqual(expected,actual);
        var restored=HolonRecoveryEvidenceJson.ReadStored(actual,HolonRecoveryEvidenceJson.Digest(actual),HolonDeliveryState.ReconciliationRequired);
        Assert.AreEqual("native",restored.OriginalTransactionIds[0]);
    }
    [DataTestMethod]
    [DataRow("checksum")] [DataRow("missing-json")] [DataRow("missing-digest")] [DataRow("schema")]
    [DataRow("duplicate")] [DataRow("type")] [DataRow("state")] [DataRow("unknown-state")]
    public void InvalidRecoveryRecordsNeverProduceAnAcceptedOutcome(string fault)
    {
        var json=HolonRecoveryEvidenceJson.Write(new HolonDeliveryRecoveryEvidence(true,new[]{"native"}));
        if(fault=="schema")json=json.Replace("\"format\":1","\"format\":2");
        if(fault=="duplicate")json=json.Insert(1,"\"format\":1,");
        if(fault=="type")json=json.Replace("\"originalTransactionIds\":[\"native\"]","\"originalTransactionIds\":[17]");
        var digest=fault=="checksum" ? "bad":HolonRecoveryEvidenceJson.Digest(json);
        var state=fault=="state" ? HolonDeliveryState.Completed:fault=="unknown-state" ? (HolonDeliveryState)999:HolonDeliveryState.ReconciliationRequired;
        Assert.ThrowsException<InvalidDataException>(()=>HolonRecoveryEvidenceJson.ReadStored(fault=="missing-json" ? null:json,
            fault=="missing-digest" ? null:digest,state));
    }
    [TestMethod]
    public void MissingOldEvidenceRemainsAbsentRatherThanBeingInvented()
        =>Assert.IsNull(HolonRecoveryEvidenceJson.ReadStored(null,null,HolonDeliveryState.Pending));
}
