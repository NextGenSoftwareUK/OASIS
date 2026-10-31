using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class CommittedHolonReplicaPayloadTests
{
    private static Holon Record()
    {
        var row = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(),
            Name = "committed", CreatedDate = DateTime.UtcNow, MetaData = new Dictionary<string, object> {
                ["nested"] = new JObject { ["value"] = 9007199254740993L } } };
        CosmicConcurrency.SetProviderRevisionId(row, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        return row;
    }

    private static CommittedHolonReplicaRequest Request(Holon root, bool children = true, int depth = 0)
        => new(Guid.NewGuid(), "owned-source", ProviderType.SQLLiteDBOASIS, Guid.NewGuid(),
            ConditionalHolonOperation.Save, new[] { root }, children, true, depth);

    [TestMethod]
    public void FrozenPayloadPreservesGraphAndReceiptFingerprintAfterCallerChanges()
    {
        var root = Record(); var child = Record(); child.ParentHolonId = root.Id;
        root.Children = new List<IHolon> { child };
        var request = Request(root);
        var expected = CommittedHolonReplicaPlan.Create(request, ProviderType.MongoDBOASIS);
        var json = CommittedHolonReplicaPayload.Capture(request);
        root.Name = "later"; child.Name = "later child"; root.Children.Clear();
        var restored = CommittedHolonReplicaPayload.Read(json);
        var actual = CommittedHolonReplicaPlan.Create(restored, ProviderType.MongoDBOASIS);
        Assert.AreEqual(expected.Fingerprint, actual.Fingerprint);
        Assert.AreEqual(2, actual.Nodes.Count);
        Assert.AreEqual(child.Id, actual.Nodes[0].SelectedChildIds[0]);
        restored.Roots[0].Name = "consumer edit";
        Assert.AreEqual(expected.Fingerprint, CommittedHolonReplicaPlan.Create(
            CommittedHolonReplicaPayload.Read(json), ProviderType.MongoDBOASIS).Fingerprint);
    }

    [TestMethod]
    public void DepthFrontierDoesNotCaptureUnselectedDescendants()
    {
        var root = Record(); var child = Record(); var grandchild = Record();
        root.Children = new List<IHolon> { child }; child.Children = new List<IHolon> { grandchild };
        var request = Request(root, depth: 1);
        var expected = CommittedHolonReplicaPlan.Create(request, ProviderType.Neo4jOASIS);
        var actual = CommittedHolonReplicaPlan.Create(CommittedHolonReplicaPayload.Read(
            CommittedHolonReplicaPayload.Capture(request)), ProviderType.Neo4jOASIS);
        Assert.AreEqual(2, actual.Nodes.Count);
        Assert.AreEqual(expected.Fingerprint, actual.Fingerprint);
    }

    [TestMethod]
    public void WireValidationPreservesExistingEmptyActorSemanticsForHostPolicy()
    {
        // The committed request contract permits an unspecified actor. The finite actor
        // grant rejects it; wire validation must not silently redefine that policy boundary.
        var request=new CommittedHolonReplicaRequest(Guid.NewGuid(),"owned-source",ProviderType.SQLLiteDBOASIS,
            Guid.Empty,ConditionalHolonOperation.Save,new[]{Record()});
        var restored=CommittedHolonReplicaPayload.Read(CommittedHolonReplicaPayload.Capture(request));
        Assert.AreEqual(Guid.Empty,restored.ActorId);
        Assert.AreEqual(CommittedHolonReplicaPlan.Create(request,ProviderType.MongoDBOASIS).Fingerprint,
            CommittedHolonReplicaPlan.Create(restored,ProviderType.MongoDBOASIS).Fingerprint);
    }

    [DataTestMethod]
    [DataRow("format")] [DataRow("unknown-edge")] [DataRow("duplicate-node")]
    [DataRow("cycle")] [DataRow("orphan")] [DataRow("unknown-field")]
    [DataRow("missing-policy")] [DataRow("duplicate-field")] [DataRow("trailing")]
    [DataRow("envelope-case")] [DataRow("node-case")]
    public void InvalidEnvelopeRejectsBeforeAnyProviderCall(string kind)
    {
        var root = Record(); var child = Record(); root.Children = new List<IHolon> { child };
        var payload = JObject.Parse(CommittedHolonReplicaPayload.Capture(Request(root)));
        var nodes = (JArray)payload["Nodes"];
        switch (kind)
        {
            case "format": payload["Format"] = 2; break;
            case "unknown-edge": nodes[0]["Children"] = new JArray(Guid.NewGuid()); break;
            case "duplicate-node": nodes.Add(nodes[0].DeepClone()); break;
            case "cycle": nodes[1]["Children"] = new JArray(root.Id); break;
            case "orphan": nodes[0]["Children"] = new JArray(); break;
            case "unknown-field": payload["FutureBehavior"] = true; break;
            case "missing-policy": payload.Remove("Recursive"); break;
            case "envelope-case":
                var recursive=payload["Recursive"];payload.Remove("Recursive");payload["recursive"]=recursive;break;
            case "node-case":
                var record=nodes[0]["RecordJson"];((JObject)nodes[0]).Remove("RecordJson");nodes[0]["recordJson"]=record;break;
            case "duplicate-field":
                Assert.ThrowsException<InvalidDataException>(() => CommittedHolonReplicaPayload.Read(
                    payload.ToString().Insert(1, "\"Format\":1,"))); return;
            case "trailing":
                Assert.ThrowsException<InvalidDataException>(() => CommittedHolonReplicaPayload.Read(payload.ToString() + " {}")); return;
        }
        Assert.ThrowsException<InvalidDataException>(() => CommittedHolonReplicaPayload.Read(payload.ToString()));
    }

    [DataTestMethod]
    [DataRow("format-string")] [DataRow("format-float")] [DataRow("source-number")]
    [DataRow("authority-name")] [DataRow("authority-string-number")]
    [DataRow("operation-name")] [DataRow("operation-string-number")]
    [DataRow("save-children-string")] [DataRow("recursive-string")]
    [DataRow("depth-string")] [DataRow("depth-float")]
    [DataRow("operation-guid-format")] [DataRow("actor-guid-format")]
    [DataRow("root-guid-format")] [DataRow("child-guid-format")]
    public void WireScalarTypesCannotBeCoercedIntoACommittedIntent(string kind)
    {
        var root=Record();var child=Record();root.Children=new List<IHolon>{child};
        var payload=JObject.Parse(CommittedHolonReplicaPayload.Capture(Request(root)));
        switch(kind)
        {
            case "format-string":payload["Format"]="1";break;
            case "format-float":payload["Format"]=1.0;break;
            case "source-number":payload["SourceId"]=123;break;
            case "authority-name":payload["SourceAuthority"]=ProviderType.SQLLiteDBOASIS.ToString();break;
            case "authority-string-number":payload["SourceAuthority"]=((int)ProviderType.SQLLiteDBOASIS).ToString();break;
            case "operation-name":payload["Operation"]=ConditionalHolonOperation.Save.ToString();break;
            case "operation-string-number":payload["Operation"]=((int)ConditionalHolonOperation.Save).ToString();break;
            case "save-children-string":payload["SaveChildren"]="true";break;
            case "recursive-string":payload["Recursive"]="true";break;
            case "depth-string":payload["MaxChildDepth"]="0";break;
            case "depth-float":payload["MaxChildDepth"]=0.0;break;
            case "operation-guid-format":payload["OperationId"]=Guid.Parse((string)payload["OperationId"]).ToString("N");break;
            case "actor-guid-format":payload["ActorId"]=Guid.Parse((string)payload["ActorId"]).ToString("B");break;
            case "root-guid-format":payload["Roots"][0]=root.Id.ToString("N");break;
            case "child-guid-format":payload["Nodes"][0]["Children"][0]=child.Id.ToString("N");break;
        }
        Assert.ThrowsException<InvalidDataException>(()=>CommittedHolonReplicaPayload.Read(payload.ToString()));
    }
}
