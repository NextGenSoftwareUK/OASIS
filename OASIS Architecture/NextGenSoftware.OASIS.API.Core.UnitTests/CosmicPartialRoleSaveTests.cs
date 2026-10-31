using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public class CosmicPartialRoleSaveTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExactPrivateAcceptedMembershipCertifiesCompleteLoadedView(bool empty)
    {
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};
        if(!empty)root.Children.Add(child);
        root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,root.Id,
            new[]{new HolonChildRole("Children",empty ? Array.Empty<Guid>():new[]{child.Id})}));
        root.AcceptChanges();root.Children.Clear();
        using var plan=HolonSavePlan.Create(new[]{root},true,true,0);
        plan.RelationshipManifests[root.Id].Roles.Single().ChildIds.Should().BeEmpty();
    }

    [Fact]
    public void UnacceptedChildValuesCannotCertifyParentMembership()
    {
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};root.Children.Add(child);
        root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole("Children",new[]{child.Id})}));
        root.AcceptChanges(includeChildren:false);
        Action save=()=>{using var plan=HolonSavePlan.Create(new[]{root},true,true,0);};
        save.Should().Throw<InvalidOperationException>().WithMessage("*fully observed*");
    }

    [Fact]
    public void InvalidLaterAcknowledgedManifestCannotPublishEarlierCallerState()
    {
        var root=new Holon{Id=Guid.NewGuid(),Name="before"};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};root.Children.Add(child);
        using var plan=HolonSavePlan.Create(new[]{root},true,true,0);plan.Roots[0].Name="after";
        ((Holon)plan.Roots[0].Children.Single()).ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(
            new HolonChildRoleManifest(1,Guid.NewGuid(),new[]{new HolonChildRole("Children",Array.Empty<Guid>())}));
        Action publish=()=>plan.Publish(plan.Roots,_=>new NextGenSoftware.OASIS.Common.OASISResult<bool>(true));
        publish.Should().Throw<InvalidDataException>();root.Name.Should().Be("before");root.Original.Should().BeNull();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CompleteObservationAllowsIntentionalMembershipEdits(bool remove)
    {
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};
        var manifest=new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole("Children",new[]{child.Id})});
        root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(manifest);
        HolonChildRoleHydration.ApplyFullySelected(root,manifest,_=>child).IsError.Should().BeFalse();root.AcceptChanges();
        if(remove)root.Children.Clear();else root.Children.Add(new Holon());
        using var plan=HolonSavePlan.Create(new[]{root},true,true,0);
        plan.RelationshipManifests[root.Id].Roles.Single().ChildIds.Count.Should().Be(remove ? 0:2);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ClearingStoreObservationCannotBypassPrivateOwnership(bool accepted)
    {
        var root=new Holon{Id=Guid.NewGuid()};var manifest=new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>())});
        root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(manifest);
        HolonChildRoleHydration.ApplyFullySelected(root,manifest,_=>null!).IsError.Should().BeFalse();
        if(accepted)root.AcceptChanges();root.ChildRoleManifestJson=null;
        Action save=()=>{using var plan=HolonSavePlan.Create(new[]{root},true,true,0);};
        save.Should().Throw<InvalidOperationException>().WithMessage("*store-owned*");
    }

    [Fact]
    public void SelectedCommittedAcknowledgementRetainsCompleteObservationForNextSave()
    {
        var root=new Holon{Id=Guid.NewGuid()};root.Children.Add(new Holon{ParentHolonId=root.Id});
        using(var plan=HolonSavePlan.Create(new[]{root},true,true,0))
        {
            foreach(var record in plan.Roots.Concat(plan.Roots.SelectMany(value=>value.Children)))
                ((Holon)record).ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(plan.RelationshipManifests[record.Id]);
            plan.Publish(plan.Roots,_=>new NextGenSoftware.OASIS.Common.OASISResult<bool>(true));
        }
        root.Children.Clear();
        using var next=HolonSavePlan.Create(new[]{root},true,true,0);
        next.RelationshipManifests[root.Id].Roles.Single().ChildIds.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public void UnobservedStoredMembershipCannotBecomeASelectedSaveManifest(bool subset,bool accepted)
    {
        var root=new Holon{Id=Guid.NewGuid()};var first=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};
        var hidden=Guid.NewGuid();
        root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,root.Id,
            new[]{new HolonChildRole("Children",new[]{first.Id,hidden})}));
        if(subset)root.Children.Add(first);
        if(accepted)root.AcceptChanges();
        Action save=()=>{using var plan=HolonSavePlan.Create(new[]{root},true,true,0);};
        save.Should().Throw<InvalidOperationException>().WithMessage("*fully observed*");
        using var excluded=HolonSavePlan.Create(new[]{root},false,false,0);
        excluded.RelationshipManifests.Should().BeEmpty();root.ChildRoleManifestJson.Should().Contain(hidden.ToString("D"));
    }
}
