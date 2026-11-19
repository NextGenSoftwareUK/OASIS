using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class ConditionalHolonPropertyPlanTests
{
    private static (Holon Record, ConditionalHolonWriteRequest Base) Request(ProviderType authority,
        HolonReplicationEnrollment enrollment = null)
    {
        var record = new Holon { Id = Guid.NewGuid(), Name = "unwritten local name", Description = "selected description",
            Version = 1, VersionId = Guid.NewGuid(), IsActive = true };
        var write = new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), authority,
            new[] { new HolonWriteCondition(record.Id, Guid.NewGuid()) }, enrollment);
        return (record, new ConditionalHolonWriteRequest(write, ConditionalHolonOperation.Save, new[] { record }, false, false, 0));
    }

    private static ConditionalHolonPropertyWriteRequest Select(ConditionalHolonWriteRequest basis, params string[] paths)
        => new(basis, new Dictionary<Guid, IReadOnlyList<string>> { [basis.Roots[0].Id] = paths });

    [DataTestMethod]
    [DataRow(ProviderType.MongoDBOASIS)] [DataRow(ProviderType.SQLLiteDBOASIS)] [DataRow(ProviderType.Neo4jOASIS)]
    public void SelectedAndWholeWritesAndDifferentMasksHaveDifferentNativeIntent(ProviderType authority)
    {
        var f = Request(authority);
        var description = ConditionalHolonPlan.CreatePropertyWrite(Select(f.Base, "/Description"), authority);
        var name = ConditionalHolonPlan.CreatePropertyWrite(Select(f.Base, "/Name"), authority);
        var whole = ConditionalHolonPlan.Create(f.Base, authority);
        Assert.AreNotEqual(description.Fingerprint, name.Fingerprint);
        Assert.AreNotEqual(description.Fingerprint, whole.Fingerprint);
        Assert.AreNotEqual(name.Fingerprint, whole.Fingerprint);
        Assert.AreEqual(f.Base.Write.ActorId, description.ActorId);
        Assert.AreEqual(f.Base.Write.OperationId, description.OperationId);
        Assert.AreEqual(f.Base.Write.Conditions[0].ExpectedRevisionId, description.ExpectedRevisions[f.Record.Id]);
        Assert.IsNull(whole.PropertyWrite);
    }

    [TestMethod]
    public void EquivalentCanonicalSelectionsHaveTheSameNativeFingerprint()
    {
        var f = Request(ProviderType.MongoDBOASIS);
        var left = Select(f.Base, "/Name", "/Description");
        var right = Select(f.Base, "/Description", "/Name");
        var plan = ConditionalHolonPlan.CreatePropertyWrite(left, ProviderType.MongoDBOASIS);
        Assert.AreEqual(plan.Fingerprint,
            ConditionalHolonPlan.CreatePropertyWrite(right, ProviderType.MongoDBOASIS).Fingerprint);
        Assert.AreSame(left, plan.PropertyWrite);
    }

    [TestMethod]
    public void SelectedPlanRetainsFrozenValuesWhenTheCallerChangesAfterCapture()
    {
        var f = Request(ProviderType.MongoDBOASIS);
        var plan = ConditionalHolonPlan.CreatePropertyWrite(Select(f.Base, "/Description"), ProviderType.MongoDBOASIS);
        var intent = plan.Fingerprint;
        f.Record.Name = "later name"; f.Record.Description = "later description";
        Assert.AreEqual("unwritten local name", plan.Nodes[0].Snapshot.Name);
        Assert.AreEqual("selected description", plan.Nodes[0].Snapshot.Description);
        Assert.AreEqual(intent, plan.Fingerprint);
    }

    [TestMethod]
    public void IntegrityBoundReceiptCannotReplayAgainstADifferentSelection()
    {
        var f = Request(ProviderType.MongoDBOASIS);
        var selected = ConditionalHolonPlan.CreatePropertyWrite(Select(f.Base, "/Description"), ProviderType.MongoDBOASIS);
        var other = ConditionalHolonPlan.CreatePropertyWrite(Select(f.Base, "/Name"), ProviderType.MongoDBOASIS);
        var acknowledged = StoredRecordJson.Project<Holon>(f.Record);
        acknowledged.Name = "authoritative unwritten name";
        acknowledged.Version = 2; acknowledged.PreviousVersionId = f.Record.VersionId;
        acknowledged.VersionId = Guid.NewGuid();
        CosmicConcurrency.SetProviderRevisionId(acknowledged, ProviderType.MongoDBOASIS, Guid.NewGuid());
        var json = ConditionalHolonReceipt.Write(new[] { acknowledged }, selected);
        var read = ConditionalHolonReceipt.Read(json, selected);
        Assert.AreEqual("authoritative unwritten name", read[0].Name);
        Assert.AreEqual("selected description", read[0].Description);
        Assert.ThrowsException<InvalidDataException>(() => ConditionalHolonReceipt.Read(json, other));
        Assert.ThrowsException<InvalidDataException>(() => ConditionalHolonReceipt.Read(json,
            ConditionalHolonPlan.Create(f.Base, ProviderType.MongoDBOASIS)));
    }

    [TestMethod]
    public void SelectedPlanCannotSwitchTheRequestedNativeAuthority()
    {
        var f = Request(ProviderType.MongoDBOASIS);
        Assert.ThrowsException<ArgumentException>(() => ConditionalHolonPlan.CreatePropertyWrite(
            Select(f.Base, "/Name"), ProviderType.Neo4jOASIS));
        Assert.ThrowsException<ArgumentNullException>(() => ConditionalHolonPlan.CreatePropertyWrite(null, ProviderType.MongoDBOASIS));
    }

    [TestMethod]
    public void EmbeddedReplicationEnrollmentIsRetainedAndBoundToSelectedIntent()
    {
        var enrollment = new HolonReplicationEnrollment("source", new[] { "replica" });
        var f = Request(ProviderType.MongoDBOASIS, enrollment);
        var plan = ConditionalHolonPlan.CreatePropertyWrite(Select(f.Base, "/Name"), ProviderType.MongoDBOASIS);
        Assert.AreSame(enrollment, plan.ReplicationEnrollment);
        var changed = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(f.Base.Write.OperationId,
            f.Base.Write.ActorId, f.Base.Write.Authority, f.Base.Write.Conditions,
            new HolonReplicationEnrollment("other-source", new[] { "replica" })),
            f.Base.Operation, f.Base.Roots, false, false, 0);
        Assert.AreNotEqual(plan.Fingerprint, ConditionalHolonPlan.CreatePropertyWrite(
            Select(changed, "/Name"), ProviderType.MongoDBOASIS).Fingerprint);
    }

    [TestMethod]
    public void CreatingSelectedIntentDoesNotMutateOrdinaryWriteIntent()
    {
        var f = Request(ProviderType.MongoDBOASIS);
        var before = StoredRecordJson.CaptureRuntimeToken(f.Record).ToString();
        var ordinary = ConditionalHolonPlan.Create(f.Base, ProviderType.MongoDBOASIS).Fingerprint;
        _ = ConditionalHolonPlan.CreatePropertyWrite(Select(f.Base, "/Name"), ProviderType.MongoDBOASIS);
        Assert.AreEqual(before, StoredRecordJson.CaptureRuntimeToken(f.Record).ToString());
        Assert.AreEqual(ordinary, ConditionalHolonPlan.Create(f.Base, ProviderType.MongoDBOASIS).Fingerprint);
    }
}
