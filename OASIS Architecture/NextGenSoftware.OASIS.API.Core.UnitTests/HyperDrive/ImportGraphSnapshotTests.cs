using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class ImportGraphSnapshotTests
{
    [Fact]
    public void DeepImportRetainsDepthFirstOrderStableIdentitiesAndDetachedAttempts()
    {
        const int count = 10000;
        var source = new List<IHolon>();
        for (var index = 0; index < count; index++)
        {
            var node = new Holon { Name = index.ToString() };
            if (index != 0) source[index - 1].Children.Add(node);
            source.Add(node);
        }
        var plan = ImportGraphSnapshot.Create(new[] { source[0] });
        var flat = plan.CreateFlatAttempt();
        flat.Count.Should().Be(count);
        var current = plan.CreateAttempt().Single();
        for (var index = 0; index < count; index++)
        {
            flat[index].Name.Should().Be(index.ToString());
            current.Id.Should().Be(flat[index].Id);
            flat[index].Children.Should().BeEmpty();
            source[index].Id.Should().BeEmpty();
            source[index].ParentHolonId.Should().BeEmpty();
            if (index > 0) flat[index].ParentHolonId.Should().Be(flat[index - 1].Id);
            if (index < count - 1) current = current.Children.Single();
            else current.Children.Should().BeEmpty();
        }
    }

    [Fact]
    public void FlatAttemptsPublishOnlyAcknowledgedIdentityAndPerProviderKeys()
    {
        var root=new Holon {Name="caller root"};var child=new Holon {Name="caller child"};root.Children.Add(child);
        var plan=ImportGraphSnapshot.Create(new IHolon[]{root});
        var first=plan.CreateFlatAttempt();first.Should().HaveCount(2);first.Should().OnlyContain(row=>row.Children.Count==0);
        first[1].ParentHolonId.Should().Be(first[0].Id);root.Id.Should().BeEmpty();child.Id.Should().BeEmpty();
        first[0].ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS]="native-first";
        first[0].Name="provider should not overwrite caller values";
        plan.PublishAcknowledgedAttempt(first,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS);
        root.Id.Should().Be(first[0].Id);child.Id.Should().Be(first[1].Id);child.ParentHolonId.Should().Be(root.Id);
        root.Name.Should().Be("caller root");root.Children.Single().Should().BeSameAs(child);
        root.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS].Should().Be("native-first");
        var replica=plan.CreateFlatAttempt();replica[0].Id.Should().Be(root.Id);
        replica[0].ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.IPFSOASIS]="native-replica";
        plan.PublishAcknowledgedAttempt(replica,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.IPFSOASIS);
        root.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS].Should().Be("native-first");
        root.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.IPFSOASIS].Should().Be("native-replica");
    }

    [Theory]
    [InlineData("wrong-id")] [InlineData("wrong-parent")] [InlineData("wrong-count")] [InlineData("caller-edited")]
    public void InvalidAcknowledgementCannotPartiallyPublishEarlierNodes(string mode)
    {
        var root=new Holon();var child=new Holon();root.Children.Add(child);
        var plan=ImportGraphSnapshot.Create(new IHolon[]{root});var attempt=plan.CreateFlatAttempt();
        if(mode=="wrong-id") attempt[1].Id=Guid.NewGuid();
        if(mode=="wrong-parent") attempt[1].ParentHolonId=Guid.NewGuid();
        if(mode=="wrong-count") attempt.RemoveAt(1);
        if(mode=="caller-edited") child.Id=Guid.NewGuid();
        var childId=child.Id;
        Action publish=()=>plan.PublishAcknowledgedAttempt(attempt,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS);
        if(mode=="caller-edited") publish.Should().Throw<InvalidOperationException>();
        else publish.Should().Throw<System.IO.InvalidDataException>();
        root.Id.Should().BeEmpty();child.Id.Should().Be(childId);child.ParentHolonId.Should().BeEmpty();
    }

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
