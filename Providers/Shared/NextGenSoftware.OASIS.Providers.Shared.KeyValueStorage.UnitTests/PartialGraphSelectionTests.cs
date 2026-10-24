using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class PartialGraphSelectionTests
{
    [TestMethod]
    public void ReceiptShapedConditionalReplayCanBindIntentWithoutOrdinarySaveAuthority()
    {
        var authority=API.Core.Enums.ProviderType.OracleDBOASIS;var actor=Guid.NewGuid();
        var root=new Holon{Id=Guid.NewGuid(),Name="original conditional intent"};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};root.Children.Add(child);
        var write=new ConditionalHolonWrite(Guid.NewGuid(),actor,authority,new[]{new HolonWriteCondition(root.Id,null),new HolonWriteCondition(child.Id,null)});
        var original=ConditionalHolonPlan.Create(new ConditionalHolonWriteRequest(write,ConditionalHolonOperation.Save,new[]{root}),authority);
        var copy=API.Core.Serialization.StoredRecordJson.Project<Holon>(root);
        copy.Children=new[]{API.Core.Serialization.StoredRecordJson.Project<Holon>(child)};
        foreach(var row in new[]{copy}.Concat(copy.Children))
        {
            row.Version=1;row.VersionId=Guid.NewGuid();row.CreatedByAvatarId=actor;
            ((Holon)row).ChildRoleManifestJson=API.Core.Serialization.HolonChildRoleManifestJson.Serialize(
                HolonChildCollectionState.Capture(row).CreateManifest(row.Id,member=>member.Id));
        }
        var replay=ConditionalHolonPlan.Create(new ConditionalHolonWriteRequest(write,ConditionalHolonOperation.Save,new[]{copy}),authority);
        Assert.AreEqual(original.Fingerprint,replay.Fingerprint,"Acknowledged metadata must not change the original idempotency intent.");
        Assert.ThrowsException<InvalidOperationException>(()=>HolonGraphWritePlan.CreateForSave(new[]{copy},true,true,0,0),
            "Binding replay intent is not permission to issue a fresh ordinary replacement.");
    }

    [DataTestMethod]
    [DataRow(false)][DataRow(true)]
    public void ConditionalCopyCannotTreatOmittedOrFlattenedStoredRolesAsComplete(bool flattened)
    {
        var authority=API.Core.Enums.ProviderType.OracleDBOASIS;
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};
        if(flattened)root.Children.Add(child);
        root.ChildRoleManifestJson=API.Core.Serialization.HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,root.Id,
            flattened?new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Components",new[]{child.Id})}
                :new[]{new HolonChildRole("Children",new[]{child.Id})}));
        var rows=new[]{root}.Concat(root.Children).ToArray();
        var write=new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),authority,rows.Select(row=>new HolonWriteCondition(row.Id,Guid.NewGuid())));
        Assert.ThrowsException<InvalidOperationException>(()=>ConditionalHolonPlan.Create(
            new ConditionalHolonWriteRequest(write,ConditionalHolonOperation.Save,new[]{root}),authority));
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public void ConditionalCallerPlanMustRejectIncompleteMembership(bool persisted,bool acceptChanges,bool explicitRoles)
    {
        var authority=API.Core.Enums.ProviderType.OracleDBOASIS;
        var root=new Holon{Id=Guid.NewGuid()};
        if(persisted)root.ChildRoleManifestJson=API.Core.Serialization.HolonChildRoleManifestJson.Serialize(
            new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole("Children",new[]{Guid.NewGuid()})}));
        HolonChangeTracking.MarkIncompleteChildSelection(root);
        if(acceptChanges)root.AcceptChanges();
        var write=new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),authority,new[]{new HolonWriteCondition(root.Id,Guid.NewGuid())});
        var proposed=HolonChildCollectionState.Capture(root).CreateManifest(root.Id,child=>child.Id);
        var request=new ConditionalHolonWriteRequest(write,ConditionalHolonOperation.Save,new[]{root},true,true,0,
            submittedRelationships:explicitRoles?new Dictionary<Guid,HolonChildRoleManifest>{{root.Id,proposed}}:null);
        Assert.ThrowsException<InvalidOperationException>(()=>ConditionalHolonPlan.Create(request,authority));
        var scalar=new ConditionalHolonWriteRequest(write,ConditionalHolonOperation.Save,new[]{root},saveChildren:false);
        Assert.IsFalse(ConditionalHolonPlan.Create(scalar,authority).Nodes.Single().SelectsChildren);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void NativeReceiptViewDoesNotPretendToHydrateStoredTypedOrExcludedRoles(bool excluded)
    {
        var root=new Holon{Id=Guid.NewGuid(),DeletedDate=excluded?DateTime.UtcNow:DateTime.MinValue};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};
        if(!excluded)root.Children.Add(child);
        var roles=excluded?new[]{new HolonChildRole("Children",new[]{child.Id})}
            :new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Components",new[]{child.Id})};
        root.ChildRoleManifestJson=API.Core.Serialization.HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,root.Id,roles));
        var restored=ConditionalHolonReceipt.Read(ConditionalHolonReceipt.Write(new[]{root}));
        var acknowledged=ConditionalHolonAcknowledgement.Create(restored,excluded?ConditionalHolonOperation.SoftDelete:ConditionalHolonOperation.Save);
        Assert.IsFalse(acknowledged.IsError);Assert.AreEqual(excluded,acknowledged.IsDeleted);
        Assert.AreEqual(root.ChildRoleManifestJson,((Holon)restored.Single()).ChildRoleManifestJson);
        Assert.AreEqual(excluded?0:1,restored.Single().Children.Count);
        Assert.ThrowsException<InvalidOperationException>(()=>HolonGraphWritePlan.CreateForSave(restored,true,true,0,0));
    }

    [TestMethod]
    public void StructuralReceiptRoundTripDoesNotInventCallerMutationAuthority()
    {
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};root.Children.Add(child);
        foreach(var row in new[]{root,child})row.ChildRoleManifestJson=API.Core.Serialization.HolonChildRoleManifestJson.Serialize(
            HolonChildCollectionState.Capture(row).CreateManifest(row.Id,value=>value.Id));
        var json=ConditionalHolonReceipt.Write(new[]{root});var restored=ConditionalHolonReceipt.Read(json);
        Assert.AreEqual(child.Id,restored.Single().Children.Single().Id);
        Assert.ThrowsException<InvalidOperationException>(()=>HolonGraphWritePlan.CreateForSave(restored,true,true,0,0));
        var acknowledged=ConditionalHolonAcknowledgement.Create(restored,ConditionalHolonOperation.Save);
        Assert.IsTrue(acknowledged.IsSaved);
        Assert.ThrowsException<InvalidOperationException>(()=>HolonGraphWritePlan.CreateForSave(restored,true,true,0,0),
            "Receipt acknowledgement reports the native outcome; it does not hydrate typed caller membership.");
        foreach(var row in restored.SelectMany(root=>new[]{root}.Concat(root.Children)))
        {
            var manifest=API.Core.Serialization.HolonChildRoleManifestJson.Deserialize(((Holon)row).ChildRoleManifestJson);
            var members=row.Children.ToDictionary(child=>child.Id);
            Assert.IsFalse(HolonChildRoleHydration.ApplyFullySelected(row,manifest,id=>members[id]).IsError);
        }
        Assert.AreEqual(2,HolonGraphWritePlan.CreateForSave(restored,true,true,0,0).Nodes.Count);
    }

    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public void ProviderGraphPlanCannotReplaceIncompleteMembership(bool persisted,bool acceptChanges)
    {
        var root=new Holon{Id=Guid.NewGuid()};
        if(persisted)root.ChildRoleManifestJson=API.Core.Serialization.HolonChildRoleManifestJson.Serialize(
            new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole("Children",new[]{Guid.NewGuid()})}));
        HolonChangeTracking.MarkIncompleteChildSelection(root);
        if(acceptChanges)root.AcceptChanges();
        Assert.ThrowsException<InvalidOperationException>(()=>HolonGraphWritePlan.CreateForSave(new[]{root},true,true,0,0));
        var scalar=HolonGraphWritePlan.CreateForSave(new[]{root},false,false,0,0);
        Assert.AreEqual(1,scalar.Nodes.Count);Assert.IsFalse(scalar.Nodes.Single().SelectsChildren);
        Assert.AreEqual(0,root.Version);
    }
}
