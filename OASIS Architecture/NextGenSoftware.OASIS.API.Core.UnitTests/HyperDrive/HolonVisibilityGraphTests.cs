using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class HolonVisibilityGraphTests
{
    [Theory]
    [InlineData(0,false)] [InlineData(0,true)] [InlineData(1,false)] [InlineData(1,true)]
    [InlineData(2,false)] [InlineData(2,true)] [InlineData(3,false)] [InlineData(3,true)]
    public async Task AllEightScopedDefaultsUseTheSharedGraphProjection(int route,bool asynchronous)
    {
        var actor=Guid.NewGuid();var parent=Guid.NewGuid();
        var root=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent,CreatedByAvatarId=actor};
        var hidden=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=Guid.NewGuid()};
        root.Children=new[]{hidden};var filter=new Dictionary<string,string>{{"kind","tree"}};
        var source=new OASISResult<IEnumerable<IHolon>>(new[]{root}){IsLoaded=true,LoadedCount=1};
        var provider=new Mock<IOASISStorageProvider>{CallBase=true};
        provider.Setup(x=>x.LoadAllHolons(HolonType.All,true,true,0,0,true,false,0)).Returns(source);
        provider.Setup(x=>x.LoadAllHolonsAsync(HolonType.All,true,true,0,0,true,false,0)).ReturnsAsync(source);
        provider.Setup(x=>x.LoadHolonsForParent(parent,HolonType.All,true,true,0,0,true,false,0)).Returns(source);
        provider.Setup(x=>x.LoadHolonsForParentAsync(parent,HolonType.All,true,true,0,0,true,false,0)).ReturnsAsync(source);
        provider.Setup(x=>x.LoadHolonsByMetaData("kind","tree",HolonType.All,true,true,0,0,true,false,0)).Returns(source);
        provider.Setup(x=>x.LoadHolonsByMetaDataAsync("kind","tree",HolonType.All,true,true,0,0,true,false,0)).ReturnsAsync(source);
        provider.Setup(x=>x.LoadHolonsByMetaData(filter,MetaKeyValuePairMatchMode.All,HolonType.All,true,true,0,0,true,false,0)).Returns(source);
        provider.Setup(x=>x.LoadHolonsByMetaDataAsync(filter,MetaKeyValuePairMatchMode.All,HolonType.All,true,true,0,0,true,false,0)).ReturnsAsync(source);
        IOASISStorageProvider contract=provider.Object;
        var result=route switch {
            0=>asynchronous?await contract.LoadAllHolonsAsync(actor):contract.LoadAllHolons(actor),
            1=>asynchronous?await contract.LoadHolonsForParentAsync(parent,actor):contract.LoadHolonsForParent(parent,actor),
            2=>asynchronous?await contract.LoadHolonsByMetaDataAsync("kind","tree",actor):contract.LoadHolonsByMetaData("kind","tree",actor),
            _=>asynchronous?await contract.LoadHolonsByMetaDataAsync(filter,MetaKeyValuePairMatchMode.All,actor):contract.LoadHolonsByMetaData(filter,MetaKeyValuePairMatchMode.All,actor)};
        result.IsError.Should().BeFalse(result.Exception?.ToString());result.IsLoaded.Should().BeTrue();
        result.Result.Single().Children.Should().BeEmpty();root.Children.Should().ContainSingle();
    }

    private static OASISResult<IEnumerable<IHolon>> Select(OASISResult<IEnumerable<IHolon>> source, Guid actor, bool includePublic=false)
    {
        var provider=new Mock<IOASISStorageProvider>{CallBase=true};
        provider.Setup(x=>x.LoadAllHolons(HolonType.All,true,true,0,0,true,false,0)).Returns(source);
        return ((IOASISStorageProvider)provider.Object).LoadAllHolons(actor,includePublic);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FilteredViewCannotReplaceChildrenEvenAfterAcceptChanges(bool persisted)
    {
        var actor=Guid.NewGuid();var root=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=Guid.NewGuid()};
        root.Children=new List<IHolon>{child};
        if(persisted)root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(
            HolonChildCollectionState.Capture(root).CreateManifest(root.Id,node=>node.Id));
        var selected=Select(new OASISResult<IEnumerable<IHolon>>(new[]{root}),actor);
        selected.IsError.Should().BeFalse(selected.Exception?.ToString());
        var copy=(Holon)selected.Result.Single();copy.Should().NotBeSameAs(root);
        copy.Children.Should().BeEmpty();
        copy.ChildRoleManifestJson.Should().Be(root.ChildRoleManifestJson);
        copy.AcceptChanges();
        Action save=()=>{using var plan=HolonSavePlan.Create(new[]{copy},true,true,0);};
        save.Should().Throw<InvalidOperationException>();
        using var rootOnly=HolonSavePlan.Create(new[]{copy},false,false,0);
        root.Children.Should().ContainSingle().Which.Should().BeSameAs(child);
    }

    [Fact]
    public void HiddenBranchesAreNotTraversedOrHoisted()
    {
        var actor=Guid.NewGuid();var root=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor};
        var hidden=new ThrowingBranch{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=Guid.NewGuid()};
        root.Children=new List<IHolon>{hidden};
        var selected=Select(new OASISResult<IEnumerable<IHolon>>(new[]{root}),actor,true);
        selected.IsError.Should().BeFalse(selected.Exception?.ToString());
        selected.Result.Single().Children.Should().BeEmpty();
    }

    [Theory]
    [InlineData("null-root")] [InlineData("null-child")] [InlineData("empty-id")]
    [InlineData("cycle")] [InlineData("duplicate")] [InlineData("wrong-parent")]
    public void MalformedVisibleGraphsFailWithoutPartialPayload(string fault)
    {
        var actor=Guid.NewGuid();var root=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=actor};
        root.Children=new List<IHolon>{child};
        switch(fault)
        {
            case "null-child":root.Children=new IHolon[]{null!};break;
            case "empty-id":child.Id=Guid.Empty;break;
            case "cycle":child.Children=new[]{root};break;
            case "duplicate":root.Children=new[]{child,child};break;
            case "wrong-parent":child.ParentHolonId=Guid.NewGuid();break;
        }
        var original=new IOException("original provider cause");
        var source=new OASISResult<IEnumerable<IHolon>>(fault=="null-root"?new IHolon[]{root,null!}:new[]{root})
            {IsError=true,Exception=original,LoadedCount=8};
        var selected=Select(source,actor);
        selected.IsError.Should().BeTrue();selected.Result.Should().BeNull();selected.LoadedCount.Should().Be(0);
        selected.ErrorCode.Should().Be("HOLON_VISIBILITY_PROJECTION_FAILED");
        ((AggregateException)selected.Exception).InnerExceptions.Should().Contain(original);
        source.LoadedCount.Should().Be(8);source.Result.Should().NotBeNull();source.Exception.Should().BeSameAs(original);
    }

    [Fact]
    public void ScopeDoesNotConsumeOrRedactTheProviderFrameForLaterReaders()
    {
        var actor=Guid.NewGuid();var owned=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor};
        var foreign=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid()};
        var source=new OASISResult<IEnumerable<IHolon>>(new[]{owned,foreign}){LoadedCount=2,Message="provider evidence",
            MetaData=new Dictionary<string,string>{{"partialHolonReads","true"},{"failedHolonReads","original identities"}}};
        var selected=Select(source,actor);
        selected.IsError.Should().BeFalse();selected.LoadedCount.Should().Be(1);
        selected.MetaData.Should().NotContainKey("failedHolonReads");
        source.Result.Should().Equal(owned,foreign);source.LoadedCount.Should().Be(2);
        source.Message.Should().Be("provider evidence");source.MetaData["failedHolonReads"].Should().Be("original identities");
        var unscoped=Select(source,Guid.Empty);
        unscoped.Result.Should().Equal(owned,foreign);unscoped.MetaData["failedHolonReads"].Should().Be("original identities");
    }

    [Fact]
    public void TypedRoleAndNestedOwnershipArePreservedOnDetachedCopies()
    {
        var actor=Guid.NewGuid();var root=new RoleOwner{Id=Guid.NewGuid(),CreatedByAvatarId=actor};
        var child=new RoleOwner{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=actor};
        var hidden=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id,CreatedByAvatarId=Guid.NewGuid()};
        child.Extras.Add(hidden);root.Extras.Add(child);
        var selected=Select(new OASISResult<IEnumerable<IHolon>>(new[]{root}),actor);
        selected.IsError.Should().BeFalse(selected.Exception?.ToString());
        var copy=selected.Result.Single().Should().BeOfType<RoleOwner>().Subject;
        copy.Extras.Single().Should().BeOfType<RoleOwner>().Which.Extras.Should().BeEmpty();
        copy.Children.Should().BeEmpty();root.Extras.Single().Should().BeSameAs(child);child.Extras.Should().ContainSingle();
    }

    public sealed class ThrowingBranch:Holon
    {
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()
            =>throw new InvalidOperationException("A hidden branch must never be captured.");
    }

    public sealed class RoleOwner:Holon
    {
        public List<IHolon> Extras{get;set;}=new();
        public override IReadOnlyCollection<IHolon> AllChildren=>Children.Concat(Extras).ToArray();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>new[]{
            new HolonChildCollection(nameof(Children),Children,members=>Children=members.ToList()),
            new HolonChildCollection(nameof(Extras),Extras,members=>Extras=members.ToList())};
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings()=>new[]{
            HolonChildRoleBinding.Create<IHolon>(nameof(Children),members=>Children=members.ToList()),
            HolonChildRoleBinding.Create<IHolon>(nameof(Extras),members=>Extras=members.ToList())};
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ScopedCollectionMustNotExposeForeignPrivateDescendants(bool asynchronous, bool includePublic)
    {
        var actor = Guid.NewGuid();
        var root = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = actor };
        var owned = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, CreatedByAvatarId = actor };
        var visible = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, CreatedByAvatarId = Guid.NewGuid(), IsPublic = true };
        var hidden = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, CreatedByAvatarId = Guid.NewGuid() };
        root.Children = new List<IHolon> { owned, visible, hidden };
        var source = new OASISResult<IEnumerable<IHolon>>(new[] { root }) { LoadedCount = 1 };
        var provider = new Mock<IOASISStorageProvider> { CallBase = true };
        provider.Setup(x => x.LoadAllHolons(HolonType.All, true, true, 0, 0, true, false, 0)).Returns(source);
        provider.Setup(x => x.LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, 0)).ReturnsAsync(source);
        IOASISStorageProvider contract = provider.Object;
        var result = asynchronous
            ? await contract.LoadAllHolonsAsync(actor, includePublic)
            : contract.LoadAllHolons(actor, includePublic);

        result.IsError.Should().BeFalse();
        result.Result.Single().Children.Select(child => child.Id).Should()
            .Equal(includePublic ? new[] { owned.Id, visible.Id } : new[] { owned.Id });
        root.Children.Should().Equal(new IHolon[] { owned, visible, hidden }, "scoping must not mutate the provider's source graph");
    }
}
