using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonGraphRoleManifestTests
{
    [TestMethod]
    public void ConditionalFingerprintBindsRoleIntentWithoutChangingUnmanifestedRequests()
    {
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};root.Children.Add(child);
        var authority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS;
        var write=new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),authority,new[]{new HolonWriteCondition(root.Id,null),new HolonWriteCondition(child.Id,null)});
        string Fingerprint(string role)=>ConditionalHolonPlan.Create(new ConditionalHolonWriteRequest(write,
            ConditionalHolonOperation.Save,new[]{root},true,true,1,new Dictionary<Guid,HolonChildRoleManifest>{{root.Id,
                new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole(role,new[]{child.Id})})}}),authority).Fingerprint;
        Assert.AreEqual(Fingerprint("A"),Fingerprint("A"));
        Assert.AreNotEqual(Fingerprint("A"),Fingerprint("B"));
        var old=new ConditionalHolonWriteRequest(write,ConditionalHolonOperation.Save,new[]{root},true,true,1);
        Assert.AreNotEqual(ConditionalHolonPlan.Create(old,authority).Fingerprint,Fingerprint("A"));
    }

    [TestMethod]
    public void ConditionalPlanRetainsForwardedRolesInsteadOfStagedPhysicalRole()
    {
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};root.Children.Add(child);
        var manifest=new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole("OriginalTypedRole",new[]{child.Id})});
        var authority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS;
        var write=new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),authority,new[]{new HolonWriteCondition(root.Id,null),new HolonWriteCondition(child.Id,null)});
        var request=new ConditionalHolonWriteRequest(write,ConditionalHolonOperation.Save,new[]{root},true,true,1,
            new Dictionary<Guid,HolonChildRoleManifest>{{root.Id,manifest}});
        var plan=ConditionalHolonPlan.Create(request,authority);
        var selected=plan.Nodes.Single(node=>node.Snapshot.Id==root.Id);
        Assert.AreSame(manifest,selected.RelationshipManifest);
        Assert.AreEqual("OriginalTypedRole",selected.RelationshipManifest.Roles.Single().Name);
    }

    public class TypedRoot:Holon
    {
        private readonly List<IHolon> _members=new();
        public void Add(IHolon member)=>_members.Add(member);
        public override IReadOnlyCollection<IHolon> AllChildren=>Children.Concat(_members).ToArray();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()
            =>base.CaptureChildCollections().Concat(new[]{new HolonChildCollection("Members",_members,
                members=>{_members.Clear();_members.AddRange(members);})}).ToArray();
    }

    [TestMethod]
    public void RawPlanPreservesTypedRolesWithAllocatedChildIdentities()
    {
        var root=new TypedRoot();var child=new Holon();root.Add(child);
        var plan=HolonGraphWritePlan.Create(new[]{root},true,true,1,0);
        var parent=plan.Nodes[0];var selectedChild=plan.Nodes[1];
        Assert.AreEqual(parent.Snapshot.Id,parent.RelationshipManifest.ParentId);
        CollectionAssert.AreEqual(new[]{selectedChild.Snapshot.Id},parent.RelationshipManifest.Roles.Single(role=>role.Name=="Members").ChildIds.ToArray());
        Assert.IsNull(selectedChild.RelationshipManifest);
        root.Add(new Holon());
        Assert.AreEqual(1,parent.RelationshipManifest.Roles.Single(role=>role.Name=="Members").ChildIds.Count);
        Assert.AreEqual(Guid.Empty,root.Id);Assert.AreEqual(Guid.Empty,child.Id);
    }

    [TestMethod]
    public void RawRootOnlyPlanDoesNotEmitUnobservedMembership()
    {
        var root=new TypedRoot();root.Add(new Holon());
        var plan=HolonGraphWritePlan.Create(new[]{root},false,true,0,0);
        Assert.AreEqual(1,plan.Nodes.Count);Assert.IsNull(plan.Nodes[0].RelationshipManifest);
    }
}
