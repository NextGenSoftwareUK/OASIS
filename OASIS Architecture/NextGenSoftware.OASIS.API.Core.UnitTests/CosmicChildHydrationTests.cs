using System.Collections.ObjectModel;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public class CosmicChildHydrationTests
{
    [Fact]
    public void AProviderSelectionRestrictionSurvivesAcceptChangesAndPriorCompleteness()
    {
        var owner=new Owner{Id=Guid.NewGuid()};
        var child=new SpecialChild{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child.Id})});
        owner.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(manifest);
        HolonChildRoleHydration.ApplyFullySelected(owner,manifest,_=>child).IsError.Should().BeFalse();
        owner.AcceptChanges();

        HolonChangeTracking.MarkIncompleteChildSelection(owner);
        owner.Extras.Clear();
        owner.AcceptChanges();

        Action save=()=>{using var plan=Core.Managers.HolonSavePlan.Create(new[]{owner},true,true,0);};
        save.Should().Throw<InvalidOperationException>();
        using var rootOnly=Core.Managers.HolonSavePlan.Create(new[]{owner},false,false,0);
    }

    [Fact]
    public void APartialViewRequiresTheOwnerToRetainItsCompleteStoredManifest()
    {
        var owner=new Owner{Id=Guid.NewGuid()};var child=Guid.NewGuid();
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child})});
        var result=HolonChildRoleHydration.ApplySelectedView(owner,manifest,Array.Empty<Guid>(),
            _=>throw new InvalidOperationException("No omitted child should resolve."));
        result.IsError.Should().BeTrue();owner.Calls.Should().Be(0);
    }

    [Fact]
    public void ViewMembershipUsesPersistedOrderRatherThanSelectionOrder()
    {
        var owner=new Owner{Id=Guid.NewGuid()};
        var first=new SpecialChild{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var second=new SpecialChild{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),
            new HolonChildRole("Extras",new[]{first.Id,Guid.NewGuid(),second.Id})});
        owner.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(manifest);
        var result=HolonChildRoleHydration.ApplySelectedView(owner,manifest,new[]{second.Id,first.Id},id=>id==first.Id?first:second);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        owner.Extras.Should().Equal(first,second);
    }

    [Fact]
    public void APartialCallbackCannotEraseTheCompleteStoreObservation()
    {
        var owner=new Owner{Id=Guid.NewGuid(),Fault="manifest"};
        var child=new SpecialChild{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),
            new HolonChildRole("Extras",new[]{child.Id,Guid.NewGuid()})});
        owner.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(manifest);
        var result=HolonChildRoleHydration.ApplySelectedView(owner,manifest,new[]{child.Id},_=>child);
        result.IsError.Should().BeTrue();result.ErrorCode.Should().Be("COSMIC_CHILD_ROLE_HYDRATION_FAILED");
        owner.Original.Should().BeNull();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SelectedViewsResolveOnlySelectedMembersAndPreserveCompleteStoreRoles(bool empty)
    {
        var owner=new Owner{Id=Guid.NewGuid()};
        var visible=new Holon{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var hidden=Guid.NewGuid();var calls=0;
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),
            new HolonChildRole("Extras",new[]{hidden,visible.Id})});
        var original=Core.Serialization.HolonChildRoleManifestJson.Serialize(manifest);
        owner.ChildRoleManifestJson=original;
        owner.Materialize=source=>{calls++;return Core.Helpers.Mapper<IHolon,SpecialChild>.MapBaseHolonProperties(source);};
        var selected=empty?Array.Empty<Guid>():new[]{visible.Id};
        var resolved=new List<Guid>();
        var result=HolonChildRoleHydration.ApplySelectedView(owner,manifest,selected,id=>{
            resolved.Add(id);if(id!=visible.Id)throw new InvalidOperationException("An omitted member must not resolve.");return visible;
        });
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        resolved.Should().Equal(selected);calls.Should().Be(empty?0:1);
        owner.Extras.Select(child=>child.Id).Should().Equal(selected);
        owner.ChildRoleManifestJson.Should().Be(original);
        owner.Original.Should().BeNull();owner.Calls.Should().Be(2);
        owner.AcceptChanges();
        Action save=()=>{using var plan=Core.Managers.HolonSavePlan.Create(new[]{owner},true,true,0);};
        save.Should().Throw<InvalidOperationException>();
        using var rootOnly=Core.Managers.HolonSavePlan.Create(new[]{owner},false,false,0);
    }

    [Theory]
    [InlineData("null")] [InlineData("empty-id")] [InlineData("duplicate")] [InlineData("foreign")]
    [InlineData("missing")] [InlineData("parent")]
    public void InvalidViewSelectionCannotInvokeAnyRoleFactoryOrCallback(string fault)
    {
        var owner=new Owner{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child.Id})});
        owner.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(manifest);
        var factories=0;owner.Materialize=source=>{factories++;return new SpecialChild();};
        IReadOnlyCollection<Guid>? selected=fault switch{"null"=>null,"empty-id"=>new[]{Guid.Empty},
            "duplicate"=>new[]{child.Id,child.Id},"foreign"=>new[]{Guid.NewGuid()},_=>new[]{child.Id}};
        var result=HolonChildRoleHydration.ApplySelectedView(owner,manifest,selected!,id=>fault switch {
            "missing"=>null!,"parent"=>new Holon{Id=id,ParentHolonId=Guid.NewGuid()},_=>child});
        result.IsError.Should().BeTrue();result.ErrorCode.Should().Be("COSMIC_CHILD_ROLE_HYDRATION_FAILED");
        factories.Should().Be(0);owner.Calls.Should().Be(0);
    }

    [Fact]
    public void APreviouslyCompleteObservationCannotAuthorizeAPartialReplacement()
    {
        var owner=new Owner{Id=Guid.NewGuid()};
        var first=new SpecialChild{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var second=new SpecialChild{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{first.Id,second.Id})});
        owner.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(manifest);
        HolonChildRoleHydration.ApplyFullySelected(owner,manifest,id=>id==first.Id?first:second).IsError.Should().BeFalse();
        owner.AcceptChanges();
        HolonChildRoleHydration.ApplySelectedView(owner,manifest,new[]{first.Id},_=>first).IsError.Should().BeFalse();
        owner.AcceptChanges();
        Action save=()=>{using var plan=Core.Managers.HolonSavePlan.Create(new[]{owner},true,true,0);};
        save.Should().Throw<InvalidOperationException>();
        owner.Extras.Should().ContainSingle().Which.Should().BeSameAs(first);
    }

    [Theory]
    [InlineData("owner")] [InlineData("manifest")] [InlineData("resolver")]
    public void MissingHydrationInputsReturnExplicitErrorsBeforeCallbacks(string missing)
    {
        var owner=new Owner{Id=Guid.NewGuid()};
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",Array.Empty<Guid>())});
        var result=HolonChildRoleHydration.ApplyFullySelected(missing=="owner" ? null!:owner,
            missing=="manifest" ? null!:manifest,missing=="resolver" ? null!:_=>null!);
        result.IsError.Should().BeTrue();result.Exception.Should().BeOfType<ArgumentNullException>();owner.Calls.Should().Be(0);
    }

    [Fact]
    public void UndeclaredOwnerCapabilityReturnsUnsupportedWithoutResolution()
    {
        var parent=Guid.NewGuid();var owner=new Moq.Mock<IHolon>();owner.SetupGet(value=>value.Id).Returns(parent);
        var manifest=new HolonChildRoleManifest(1,parent,new[]{new HolonChildRole("Children",Array.Empty<Guid>())});
        var result=HolonChildRoleHydration.ApplyFullySelected(owner.Object,manifest,_=>throw new InvalidOperationException("must not resolve"));
        result.IsError.Should().BeTrue();result.Exception.Should().BeOfType<NotSupportedException>();
    }

    private sealed class SpecialChild:Holon { }
    private sealed class Owner:Holon
    {
        public List<SpecialChild> Extras=new();
        public int Calls;
        public string Fault="";
        [Newtonsoft.Json.JsonIgnore] public Func<IHolon,SpecialChild>? Materialize;
        public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Extras).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection("Extras",Extras,members=>Extras=members.Cast<SpecialChild>().ToList())}).ToArray();
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings()
        {
            var physical=HolonChildRoleBinding.Create<IHolon>("Children",members=>{Calls++;Children=members.ToList();});
            var extras=HolonChildRoleBinding.Create<SpecialChild>("Extras",members=>{
                Calls++;if(Fault=="callback")throw new InvalidOperationException("owned callback failure");
                Extras=Fault=="output" ? new():members.ToList();
                if(Fault=="manifest")ChildRoleManifestJson=null;
            },Materialize!);
            return Fault switch {"duplicate"=>new[]{physical,physical},"null"=>new[]{physical,null!},
                "limit"=>Enumerable.Repeat(physical,257).ToArray(),_=>new[]{physical,extras}};
        }
    }

    [Theory]
    [InlineData("none")] [InlineData("identity")] [InlineData("parent")] [InlineData("version")]
    [InlineData("previous")] [InlineData("type")] [InlineData("observation")] [InlineData("provider-revision")] [InlineData("null")]
    public void RegisteredFactoriesPreserveMemberObservationsBeforeAnyBindingCallback(string fault)
    {
        var owner=new Owner{Id=Guid.NewGuid()};
        var source=new Holon{Id=Guid.NewGuid(),ParentHolonId=owner.Id,VersionId=Guid.NewGuid(),PreviousVersionId=Guid.NewGuid(),HolonType=Core.Enums.HolonType.Planet};
        CosmicConcurrency.SetProviderRevisionId(source,Core.Enums.ProviderType.SQLLiteDBOASIS,Guid.NewGuid());
        source.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,source.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>())}));
        owner.Materialize=value=>{
            if(fault=="null")return null!;
            var target=Core.Helpers.Mapper<IHolon,SpecialChild>.MapBaseHolonProperties(value);
            if(fault=="identity")target.Id=Guid.NewGuid();if(fault=="parent")target.ParentHolonId=Guid.NewGuid();
            if(fault=="version")target.VersionId=Guid.NewGuid();if(fault=="previous")target.PreviousVersionId=Guid.NewGuid();
            if(fault=="type")target.HolonType=Core.Enums.HolonType.Moon;
            if(fault=="observation")target.ChildRoleManifestJson=null;
            if(fault=="provider-revision")CosmicConcurrency.SetProviderRevisionId(target,Core.Enums.ProviderType.SQLLiteDBOASIS,Guid.NewGuid());
            return target;
        };
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{source.Id})});
        var result=HolonChildRoleHydration.ApplyFullySelected(owner,manifest,_=>source);
        result.IsError.Should().Be(fault!="none");owner.Calls.Should().Be(fault=="none" ? 2:0);
        if(fault=="none")owner.Extras.Should().ContainSingle().Which.Id.Should().Be(source.Id);
        else owner.Extras.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void HydrationUsesRegisteredTypesAndLoadedReferencesWithoutAcceptingTracking(bool empty)
    {
        var owner=new Owner{Id=Guid.NewGuid()};
        var first=new SpecialChild{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var second=new SpecialChild{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        owner.Children.Add(new Holon());owner.Extras.Add(new SpecialChild());
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),
            new HolonChildRole("Extras",empty ? Array.Empty<Guid>():new[]{second.Id,first.Id})});
        var source=new Dictionary<Guid,IHolon>{{first.Id,first},{second.Id,second}};
        owner.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(manifest);
        var result=HolonChildRoleHydration.ApplyFullySelected(owner,manifest,id=>source[id]);
        result.IsError.Should().BeFalse(result.Message+result.Exception);result.Result.Should().BeTrue();
        owner.Children.Should().BeEmpty();owner.Extras.Should().Equal(empty ? Array.Empty<SpecialChild>():new[]{second,first});
        owner.Calls.Should().Be(2);owner.Original.Should().BeNull();owner.IsNewHolon.Should().BeTrue();
        owner.ChildRoleManifestJson.Should().Be(Core.Serialization.HolonChildRoleManifestJson.Serialize(manifest));
    }

    [Theory]
    [InlineData("owner")] [InlineData("unknown")] [InlineData("omitted")]
    [InlineData("duplicate")] [InlineData("null")] [InlineData("limit")]
    [InlineData("missing")] [InlineData("identity")] [InlineData("parent")] [InlineData("type")] [InlineData("observation")]
    public void InvalidSelectedMembershipCannotInvokeAnEarlierRoleCallback(string fault)
    {
        var owner=new Owner{Id=Guid.NewGuid(),Fault=fault};
        var first=new Holon{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var second=new SpecialChild{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var roles=new List<HolonChildRole>{new("Children",new[]{first.Id})};
        if(fault!="omitted")roles.Add(new(fault=="unknown" ? "Unknown":"Extras",new[]{second.Id}));
        var manifest=new HolonChildRoleManifest(1,fault=="owner" ? Guid.NewGuid():owner.Id,roles);
        if(fault=="observation")owner.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(
            new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",Array.Empty<Guid>())}));
        var result=HolonChildRoleHydration.ApplyFullySelected(owner,manifest,id=>{
            if(id==first.Id)return first;
            return fault switch {"missing"=>null!,"identity"=>new SpecialChild{Id=Guid.NewGuid(),ParentHolonId=owner.Id},
                "parent"=>new SpecialChild{Id=id,ParentHolonId=Guid.NewGuid()},"type"=>new Holon{Id=id,ParentHolonId=owner.Id},_=>second};
        });
        result.IsError.Should().BeTrue();result.Result.Should().BeFalse();result.ErrorCode.Should().Be("COSMIC_CHILD_ROLE_HYDRATION_FAILED");
        owner.Calls.Should().Be(0);owner.Children.Should().BeEmpty();owner.Extras.Should().BeEmpty();owner.Original.Should().BeNull();
    }

    [Theory]
    [InlineData("callback")] [InlineData("output")]
    public void FailedTrustedBindingRejectsOwnerWithoutAcceptingBaseline(string fault)
    {
        var owner=new Owner{Id=Guid.NewGuid(),Fault=fault};var child=new SpecialChild{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child.Id})});
        var result=HolonChildRoleHydration.ApplyFullySelected(owner,manifest,_=>child);
        result.IsError.Should().BeTrue();result.Result.Should().BeFalse();owner.Original.Should().BeNull();owner.IsNewHolon.Should().BeTrue();
        owner.Calls.Should().Be(2);
    }

    [Fact]
    public void DefaultPhysicalBindingHydratesFreshHolon()
    {
        var owner=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        var manifest=new HolonChildRoleManifest(1,owner.Id,new[]{new HolonChildRole("Children",new[]{child.Id})});
        var result=HolonChildRoleHydration.ApplyFullySelected(owner,manifest,_=>child);
        result.IsError.Should().BeFalse();owner.Children.Should().ContainSingle().Which.Should().BeSameAs(child);owner.Original.Should().BeNull();
    }
}
