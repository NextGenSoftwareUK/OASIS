using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class CommittedHolonReplicaPlanTests
{
    private static Holon Record()
    {
        var record=new Holon{Id=Guid.NewGuid(),Name="committed",Version=1,VersionId=Guid.NewGuid(),
            CreatedDate=new DateTime(2026,10,19,0,0,0,DateTimeKind.Utc),CreatedByAvatarId=Guid.NewGuid()};
        CosmicConcurrency.SetProviderRevisionId(record,ProviderType.SQLLiteDBOASIS,Guid.NewGuid());return record;
    }
    private static CommittedHolonReplicaRequest Request(Holon record,Guid operation,string source="owned-primary")
        =>new(operation,source,ProviderType.SQLLiteDBOASIS,record.CreatedByAvatarId,ConditionalHolonOperation.Save,new[]{record},saveChildren:false);
    [TestMethod]
    public void PlanDetachesIntentAndIgnoresOnlyNativeObservations()
    {
        var record=Record();var operation=Guid.NewGuid();var plan=CommittedHolonReplicaPlan.Create(Request(record,operation),ProviderType.Neo4jOASIS);
        record.ProviderUniqueStorageKey[ProviderType.Neo4jOASIS]="spoofed target key";
        record.ProviderMetaData[ProviderType.Neo4jOASIS]=new Dictionary<string,string>{{"local-secret-reference","not portable"}};
        CosmicConcurrency.SetProviderRevisionId(record,ProviderType.SQLLiteDBOASIS,Guid.NewGuid());
        Assert.AreEqual(plan.Fingerprint,CommittedHolonReplicaPlan.Create(Request(record,operation),ProviderType.Neo4jOASIS).Fingerprint);
        record.Name="later change";Assert.AreEqual("committed",plan.Nodes[0].Snapshot.Name);
        Assert.AreNotEqual(plan.Fingerprint,CommittedHolonReplicaPlan.Create(Request(record,operation),ProviderType.Neo4jOASIS).Fingerprint);
    }
    [DataTestMethod]
    [DataRow("version")] [DataRow("audit")] [DataRow("source")] [DataRow("business")]
    public void ReceiptBindingIncludesCommittedLineageAuditSourceAndBusinessData(string changed)
    {
        var record=Record();var operation=Guid.NewGuid();var original=CommittedHolonReplicaPlan.Create(Request(record,operation),ProviderType.Neo4jOASIS);
        var source="owned-primary";
        if(changed=="version"){record.PreviousVersionId=record.VersionId;record.VersionId=Guid.NewGuid();record.Version++;}
        if(changed=="audit")record.CreatedByAvatarId=Guid.NewGuid();
        if(changed=="business")record.Name="different";
        if(changed=="source")source="different-primary";
        Assert.AreNotEqual(original.Fingerprint,CommittedHolonReplicaPlan.Create(Request(record,operation,source),ProviderType.Neo4jOASIS).Fingerprint);
    }
    [DataTestMethod]
    [DataRow("version-zero")] [DataRow("version-id")] [DataRow("initial-parent")]
    [DataRow("missing-parent")] [DataRow("self-parent")] [DataRow("source-token")]
    public void UncommittedOrIncoherentSourceRejectsBeforeIo(string invalid)
    {
        var row=Record();
        switch(invalid)
        {
            case "version-zero":row.Version=0;break;
            case "version-id":row.VersionId=Guid.Empty;break;
            case "initial-parent":row.PreviousVersionId=Guid.NewGuid();break;
            case "missing-parent":row.Version=2;break;
            case "self-parent":row.Version=2;row.PreviousVersionId=row.VersionId;break;
            case "source-token":CosmicConcurrency.SetProviderRevisionId(row,ProviderType.SQLLiteDBOASIS,Guid.Empty);break;
        }
        Assert.ThrowsException<ArgumentException>(()=>CommittedHolonReplicaPlan.Create(Request(row,Guid.NewGuid()),ProviderType.Neo4jOASIS));
    }
    [TestMethod]
    public void ReceiptRoundTripPreservesLogicalRevisionAndRejectsRestampedAcknowledgement()
    {
        var row=Record();var plan=CommittedHolonReplicaPlan.Create(Request(row,Guid.NewGuid()),ProviderType.Neo4jOASIS);
        var acknowledged=OasisJson.Project<Holon>(row);
        CosmicConcurrency.SetProviderRevisionId(acknowledged,ProviderType.Neo4jOASIS,Guid.NewGuid());
        var receipt=ConditionalHolonReceipt.Write(new[]{acknowledged},(IAtomicHolonWritePlan)plan);
        Assert.AreEqual(row.VersionId,ConditionalHolonReceipt.Read(receipt,(IAtomicHolonWritePlan)plan)[0].VersionId);
        acknowledged.VersionId=Guid.NewGuid();
        var invalid=ConditionalHolonReceipt.Write(new[]{acknowledged},(IAtomicHolonWritePlan)plan);
        Assert.ThrowsException<InvalidDataException>(()=>ConditionalHolonReceipt.Read(invalid,(IAtomicHolonWritePlan)plan));
    }
    public sealed class GenericMetadata<T>
    {
        public List<int> Values { get; set; } = new() { 7 };
    }

    [TestMethod]
    public void PortableAcknowledgementPreservesValuesAcrossJsonAndClrContainersWithoutRebindingIntent()
    {
        var source = Record(); var operation = Guid.NewGuid();
        source.MetaData["tree"] = new Newtonsoft.Json.Linq.JObject {
            ["flags"] = new Newtonsoft.Json.Linq.JArray(true, false), ["nested"] = new Newtonsoft.Json.Linq.JObject { ["value"] = 7 } };
        source.Nodes = new System.Collections.ObjectModel.ObservableCollection<INode>();
        source.MetaData["maps"] = new Dictionary<string, object>[] { new() { ["value"] = 7 } };
        var plan = CommittedHolonReplicaPlan.Create(Request(source, operation), ProviderType.Neo4jOASIS);
        var ack = OasisJson.Project<Holon>(source); ack.Nodes = new List<INode>();
        ack.MetaData["tree"] = new Dictionary<string, object> {
            ["flags"] = new object[] { true, false }, ["nested"] = new Dictionary<string, object> { ["value"] = 7 } };
        ack.MetaData["maps"] = new object[] { new Dictionary<string, object> { ["value"] = 7 } };
        CosmicConcurrency.SetProviderRevisionId(ack, ProviderType.Neo4jOASIS, Guid.NewGuid());
        plan.ValidateAcknowledgedRecord(ack);
        var receipt = ConditionalHolonReceipt.Write(new[] { ack }, (IAtomicHolonWritePlan)plan);
        Assert.AreEqual(source.VersionId, ConditionalHolonReceipt.Read(receipt, (IAtomicHolonWritePlan)plan)[0].VersionId);
        Assert.AreNotEqual(plan.Fingerprint, CommittedHolonReplicaPlan.Create(Request(ack, operation), ProviderType.Neo4jOASIS).Fingerprint,
            "Original intent byte binding must remain exact even when a target acknowledgement uses equivalent containers.");
    }

    [DataTestMethod]
    [DataRow("value")] [DataRow("order")] [DataRow("missing")]
    public void PortableAcknowledgementStillRejectsChangedValuesOrderAndMissingFields(string changed)
    {
        var source = Record(); source.MetaData["tree"] = new Newtonsoft.Json.Linq.JObject { ["array"] = new Newtonsoft.Json.Linq.JArray(1, 2) };
        var plan = CommittedHolonReplicaPlan.Create(Request(source, Guid.NewGuid()), ProviderType.Neo4jOASIS);
        var values = changed == "order" ? new object[] { 2, 1 } : changed == "value" ? new object[] { 1, 3 } : new object[] { 1, 2 };
        var ack = OasisJson.Project<Holon>(source); ack.MetaData["tree"] = changed == "missing" ? new Dictionary<string, object>()
            : new Dictionary<string, object> { ["array"] = values };
        CosmicConcurrency.SetProviderRevisionId(ack, ProviderType.Neo4jOASIS, Guid.NewGuid());
        Assert.ThrowsException<InvalidDataException>(() => plan.ValidateAcknowledgedRecord(ack));
    }

    [TestMethod]
    public void CustomObjectTypesWithArrayArgumentsRemainBound()
    {
        var source = Record(); source.MetaData["typed"] = new GenericMetadata<object[]>();
        var plan = CommittedHolonReplicaPlan.Create(Request(source, Guid.NewGuid()), ProviderType.Neo4jOASIS);
        var ack = OasisJson.Project<Holon>(source); ack.MetaData["typed"] = new GenericMetadata<string[]>();
        CosmicConcurrency.SetProviderRevisionId(ack, ProviderType.Neo4jOASIS, Guid.NewGuid());
        Assert.ThrowsException<InvalidDataException>(() => plan.ValidateAcknowledgedRecord(ack));
    }
}
