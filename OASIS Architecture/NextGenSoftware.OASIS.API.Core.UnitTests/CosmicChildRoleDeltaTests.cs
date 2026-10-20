using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicChildRoleDeltaTests
{
    [Fact]
    public void MembershipChangesDistinguishUnlinkRoleMoveAndOrder()
    {
        var parent=Guid.NewGuid();var removed=Guid.NewGuid();var moved=Guid.NewGuid();var shifted=Guid.NewGuid();var added=Guid.NewGuid();
        var before=new HolonChildRoleManifest(1,parent,new[]{new HolonChildRole("Children",new[]{removed,moved,shifted})});
        var after=new HolonChildRoleManifest(1,parent,new[]{new HolonChildRole("Children",new[]{shifted,added}),new HolonChildRole("Zomes",new[]{moved})});
        var deltas=HolonChildRoleDelta.Compare(before,after);
        deltas.Should().HaveCount(4);
        deltas.Single(delta=>delta.ChildId==removed).Change.Should().Be(HolonChildRoleChange.Unlinked);
        deltas.Single(delta=>delta.ChildId==added).Change.Should().Be(HolonChildRoleChange.Linked);
        var roleMove=deltas.Single(delta=>delta.ChildId==moved);
        roleMove.Change.Should().Be(HolonChildRoleChange.Moved);roleMove.PreviousRole.Should().Be("Children");roleMove.CurrentRole.Should().Be("Zomes");
        var order=deltas.Single(delta=>delta.ChildId==shifted);
        order.PreviousIndex.Should().Be(2);order.CurrentIndex.Should().Be(0);
    }

    [Fact]
    public void EquivalentMembershipHasNoDeltaAndInputsRemainDetached()
    {
        var parent=Guid.NewGuid();var child=Guid.NewGuid();var ids=new List<Guid>{child};
        var before=new HolonChildRoleManifest(1,parent,new[]{new HolonChildRole("Children",ids)});
        ids.Clear();
        var after=new HolonChildRoleManifest(1,parent,new[]{new HolonChildRole("Children",new[]{child})});
        HolonChildRoleDelta.Compare(before,after).Should().BeEmpty();
    }

    [Fact]
    public void DifferentParentsRequireExplicitCrossParentEvidence()
    {
        var before=new HolonChildRoleManifest(1,Guid.NewGuid(),Array.Empty<HolonChildRole>());
        var after=new HolonChildRoleManifest(1,Guid.NewGuid(),Array.Empty<HolonChildRole>());
        Action compare=()=>HolonChildRoleDelta.Compare(before,after);
        compare.Should().Throw<ArgumentException>();
    }
}
