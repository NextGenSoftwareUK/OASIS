using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class PartialGraphSelectionTests
{
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
