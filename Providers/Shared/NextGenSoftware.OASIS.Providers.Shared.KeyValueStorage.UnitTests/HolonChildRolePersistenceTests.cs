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
}
