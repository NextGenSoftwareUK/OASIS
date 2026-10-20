using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicChildResolutionTests
{
    [Fact]
    public void ResolutionPreservesRoleOrderAndActualLoadedReferencesWithoutAcceptingThem()
    {
        var parent=Guid.NewGuid();
        var first=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent};
        var second=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent};
        var loaded=new Dictionary<Guid,IHolon>{{first.Id,first},{second.Id,second}};
        var manifest=new HolonChildRoleManifest(1,parent,new[]{
            new HolonChildRole("Zomes",new[]{second.Id,first.Id}),new HolonChildRole("Children",Array.Empty<Guid>())});
        var count=0;
        var roles=HolonChildRoleResolution.Resolve(manifest,id=>{count++;return loaded[id];});
        count.Should().Be(2);
        roles["Zomes"].Should().Equal(second,first);roles["Children"].Should().BeEmpty();
        first.IsNewHolon.Should().BeTrue();first.Original.Should().BeNull();
        Action mutate=()=>((IList<IHolon>)roles["Zomes"]).Clear();
        mutate.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData("missing")] [InlineData("identity")] [InlineData("parent")]
    public void InvalidLaterMemberCannotReturnPartialRoleMembership(string fault)
    {
        var parent=Guid.NewGuid();var first=Guid.NewGuid();var second=Guid.NewGuid();
        var manifest=new HolonChildRoleManifest(1,parent,new[]{new HolonChildRole("Children",new[]{first,second})});
        IReadOnlyDictionary<string,IReadOnlyList<IHolon>>? result=null;
        Action resolve=()=>result=HolonChildRoleResolution.Resolve(manifest,id=>id==first
            ? new Holon{Id=id,ParentHolonId=parent}
            :fault=="missing" ? null! :new Holon{Id=fault=="identity" ? Guid.NewGuid():id,
                ParentHolonId=fault=="parent" ? Guid.NewGuid():parent});
        resolve.Should().Throw<System.IO.InvalidDataException>();
        result.Should().BeNull();
    }
}
