using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class ImportGraphSnapshotTests
{
    public sealed class OwnedDerivedHolon : Holon { public string OwnedValue {get;set;}=""; }

    [Fact]
    public void AttemptsPreserveGraphSubtypeAndOwnedValuesWithoutPublishingOrSharingMutations()
    {
        var root=new OwnedDerivedHolon {Name="owned root",OwnedValue="subtype value"};
        root.MetaData["owned-values"]=new List<string>{"original"};
        var child=new Holon {Name="owned child"};root.Children.Add(child);
        var plan=ImportGraphSnapshot.Create(new IHolon[]{root});
        root.Id.Should().BeEmpty();child.Id.Should().BeEmpty();child.ParentHolonId.Should().BeEmpty();
        var first=plan.CreateAttempt();var staged=first.Single().Should().BeOfType<OwnedDerivedHolon>().Subject;
        staged.OwnedValue.Should().Be("subtype value");staged.Id.Should().NotBeEmpty();
        staged.Children.Single().ParentHolonId.Should().Be(staged.Id);
        var id=staged.Id;var childId=staged.Children.Single().Id;
        staged.Name="provider mutated";staged.Id=Guid.NewGuid();staged.Children.Clear();
        ((List<string>)staged.MetaData["owned-values"]).Add("provider addition");
        root.Name="caller edited after staging";
        var second=plan.CreateAttempt().Single();second.Id.Should().Be(id);second.Name.Should().Be("owned root");
        second.Children.Single().Id.Should().Be(childId);
        ((List<string>)second.MetaData["owned-values"]).Should().Equal("original");
        ((List<string>)root.MetaData["owned-values"]).Should().Equal("original");
        root.Id.Should().BeEmpty();child.Id.Should().BeEmpty();
    }

    [Theory]
    [InlineData("cycle")] [InlineData("duplicate-id")] [InlineData("conflicting-parent")] [InlineData("repeated-root")]
    public void InvalidGraphLeavesEveryCallerIdentityUnchanged(string mode)
    {
        var root=new Holon();var child=new Holon();root.Children.Add(child);
        if(mode=="cycle") child.Children.Add(root);
        if(mode=="duplicate-id") {root.Id=Guid.NewGuid();child.Id=root.Id;}
        if(mode=="conflicting-parent") child.ParentHolonId=Guid.NewGuid();
        var beforeRoot=root.Id;var beforeChild=child.Id;var beforeParent=child.ParentHolonId;
        Action stage=()=>ImportGraphSnapshot.Create(mode=="repeated-root" ? new IHolon[]{root,root}:new IHolon[]{root});
        stage.Should().Throw<ArgumentException>();
        root.Id.Should().Be(beforeRoot);child.Id.Should().Be(beforeChild);child.ParentHolonId.Should().Be(beforeParent);
    }
}
