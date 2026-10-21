using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;
[TestClass]
public class Neo4jChildRolePlanTests
{
    [DataTestMethod]
    [DataRow(true,true)] [DataRow(false,true)] [DataRow(false,false)] [DataRow(true,false)]
    public void MembershipPreparationUsesSelectionAndAuthoritativePersistedObservation(bool select,bool observed)
    {
        var source=new Holon {Id=Guid.NewGuid(),ChildRoleManifestJson="stale caller observation"};
        source.Children.Add(new Holon {Id=Guid.NewGuid(),ParentHolonId=source.Id});
        // Build selected intent before assigning an intentionally stale observation to the snapshot.
        source.ChildRoleManifestJson=null;
        var plan=HolonGraphWritePlan.Create(new IHolon[]{source},select,true,0,0).Nodes[0];
        var target=(Holon)plan.Snapshot;target.ChildRoleManifestJson="stale caller observation";
        var existing=new Holon {Id=source.Id,ChildRoleManifestJson=observed ? "persisted observation":null};
        Provider.ApplyPlannedChildRoleManifest(target,plan,existing,true);
        Assert.AreEqual(select ? HolonChildRoleManifestJson.Serialize(plan.RelationshipManifest):existing.ChildRoleManifestJson,target.ChildRoleManifestJson);
        Provider.ApplyPlannedChildRoleManifest(target,plan,existing,false);
        Assert.AreEqual(existing.ChildRoleManifestJson,target.ChildRoleManifestJson);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void ReplicaMembershipRemainsExactSourceLogicalContent(bool observed)
    {
        var source=new Holon {Id=Guid.NewGuid()};
        var plan=HolonGraphWritePlan.Create(new IHolon[]{source},true,true,0,0).Nodes[0];
        var target=(Holon)plan.Snapshot;
        target.ChildRoleManifestJson=observed ? "source committed roles":null;
        var existing=new Holon {Id=source.Id,ChildRoleManifestJson="unrelated target roles"};
        Provider.ApplyPlannedChildRoleManifest(target,plan,existing,true,true);
        Assert.AreEqual(observed ? "source committed roles":null,target.ChildRoleManifestJson);
    }

}
