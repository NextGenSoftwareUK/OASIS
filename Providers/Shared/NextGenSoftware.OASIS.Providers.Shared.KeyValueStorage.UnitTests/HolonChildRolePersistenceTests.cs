using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonChildRolePersistenceTests
{
    private static string Manifest(Guid owner, Guid child) => HolonChildRoleManifestJson.Serialize(
        new HolonChildRoleManifest(1,owner,new[]{new HolonChildRole("Children",new[]{child})}));

    [DataTestMethod]
    [DataRow(true,true)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(false,false)]
    public void UnselectedSaveAndDeleteUseAuthoritativePresenceIncludingNull(bool exists,bool observed)
    {
        var source=new Holon {Id=Guid.NewGuid()};
        var plan=HolonGraphWritePlan.Create(new IHolon[]{source},false,true,0,0).Nodes.Single();
        plan.Snapshot.ChildRoleManifestJson=Manifest(source.Id,Guid.NewGuid());
        var stored=observed ? Manifest(source.Id,Guid.NewGuid()):null;
        foreach(var kind in new[]{HolonChildRoleWriteKind.SourceSave,HolonChildRoleWriteKind.SourceDelete})
            Assert.AreEqual(exists ? stored:plan.Snapshot.ChildRoleManifestJson,
                HolonChildRolePersistence.Resolve(plan,kind,exists,stored));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void ReplicaRetainsExactSourceObservationRegardlessOfSelectedOrStoredRoles(bool observed)
    {
        var source=new Holon {Id=Guid.NewGuid()};
        var plan=HolonGraphWritePlan.Create(new IHolon[]{source},true,true,0,0).Nodes.Single();
        var original=observed ? Manifest(source.Id,Guid.NewGuid()):null;
        plan.Snapshot.ChildRoleManifestJson=original;
        Assert.AreEqual(original,HolonChildRolePersistence.Resolve(plan,HolonChildRoleWriteKind.CommittedReplica,true,Manifest(source.Id,Guid.NewGuid())));
    }

    [TestMethod]
    public void SelectedSourceSaveUsesCapturedIntentWithoutMutatingAnyObservation()
    {
        var source=new Holon {Id=Guid.NewGuid()};
        source.Children.Add(new Holon {Id=Guid.NewGuid(),ParentHolonId=source.Id});
        var plan=HolonGraphWritePlan.Create(new IHolon[]{source},true,true,0,0).Nodes[0];
        var expected=HolonChildRoleManifestJson.Serialize(plan.RelationshipManifest);
        source.Children.Clear();
        Assert.AreEqual(expected,HolonChildRolePersistence.Resolve(plan,HolonChildRoleWriteKind.SourceSave,true,null));
        Assert.IsNull(plan.Snapshot.ChildRoleManifestJson);Assert.IsNull(source.ChildRoleManifestJson);
    }

    [TestMethod]
    public void InvalidCapturedAndPersistedOwnershipFailsExplicitly()
    {
        var source=new Holon {Id=Guid.NewGuid()};var plan=new PlannedHolonWrite(source,new Holon {Id=source.Id}) {SelectsChildren=true};
        Assert.ThrowsException<InvalidDataException>(()=>HolonChildRolePersistence.Resolve(plan,HolonChildRoleWriteKind.SourceSave,false,null));
        Assert.ThrowsException<InvalidDataException>(()=>HolonChildRolePersistence.Resolve(plan with {SelectsChildren=false},HolonChildRoleWriteKind.SourceSave,true,Manifest(Guid.NewGuid(),Guid.NewGuid())));
    }
    private sealed class ReadOnceOwner : Holon,IHolon
    {
        private System.Collections.Generic.IList<IHolon> selected = new System.Collections.Generic.List<IHolon>();
        [Newtonsoft.Json.JsonIgnore] public int Reads { get; private set; }
        public new System.Collections.Generic.IList<IHolon> Children
        { get => ++Reads == 1 ? selected : Array.Empty<IHolon>(); set => selected = value; }
    }

    [TestMethod]
    public void AcknowledgedPlanReusesOriginalMembershipWithoutReadingCallerGetterAgain()
    {
        var child = new Holon { Id=Guid.NewGuid() };
        var owner = new ReadOnceOwner { Id=Guid.NewGuid(),Children=new IHolon[]{child} };
        var plan = HolonGraphWritePlan.CreateForSave(new[]{owner},true,true,0,0);
        foreach(var node in plan.Nodes)
        {
            node.Source.Id=node.Snapshot.Id;node.Source.ParentHolonId=node.Snapshot.ParentHolonId;
            ((Holon)node.Source).ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(node.RelationshipManifest);
        }
        foreach(var node in plan.Nodes)HolonChildRolePersistence.PublishAcknowledgedSelection(node);
        Assert.AreEqual(1,owner.Reads);
        Assert.AreEqual(owner.Id,child.ParentHolonId);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void CapturedAcknowledgementRejectsForeignCaptureAndLeavesLaterEditsDirty(bool scalar)
    {
        var owner=new Holon {Id=Guid.NewGuid(),Name="original"};
        owner.Children=new System.Collections.Generic.List<IHolon>{new Holon{Id=Guid.NewGuid(),ParentHolonId=owner.Id}};
        owner.AcceptChanges();
        var captured=HolonChildCollectionState.Capture(owner);
        var manifest=captured.CreateManifest(owner.Id,child=>child.Id);
        owner.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(manifest);
        var foreign=HolonChildCollectionState.Capture(new Holon{Id=owner.Id,Children=owner.Children});
        Assert.ThrowsException<InvalidOperationException>(()=>HolonChangeTracking.RecordAcknowledgedChildSelection(owner,manifest,foreign));
        if(scalar)owner.Name="edited during write";
        else owner.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=owner.Id});
        HolonChangeTracking.RecordAcknowledgedChildSelection(owner,manifest,captured);
        Assert.IsTrue(HolonChangeTracking.HasChanges(owner),"Acknowledgement cannot accept later scalar/membership edits.");
        Assert.AreEqual(scalar?1:2,owner.Children.Count);
    }

}
