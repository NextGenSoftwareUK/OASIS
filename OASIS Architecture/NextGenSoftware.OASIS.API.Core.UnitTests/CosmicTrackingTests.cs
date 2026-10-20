using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicTrackingTests
{
    [Theory]
    [InlineData(false,false,false)] [InlineData(true,false,false)]
    [InlineData(false,true,false)] [InlineData(true,true,false)]
    [InlineData(false,false,true)] [InlineData(true,false,true)]
    [InlineData(false,true,true)] [InlineData(true,true,true)]
    public async Task V2SearchHonorsChildSourceAndTypeWithoutMutatingProviderObservations(bool synchronous,bool fromProvider,bool filter)
    {
        var f=Fixture(true);var root=new Holon {Id=Guid.NewGuid()};
        var quest=new Holon {Id=Guid.NewGuid(),ParentHolonId=root.Id,HolonType=HolonType.Quest};
        var other=new Holon {Id=Guid.NewGuid(),ParentHolonId=root.Id,HolonType=HolonType.Holon};
        root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,root.Id,
            new[]{new HolonChildRole("Children",new[]{quest.Id,other.Id})}));
        if(fromProvider)root.Children=new List<IHolon>{quest,other};
        var payload=new Core.Objects.Search.SearchResults {SearchResultHolons=new List<IHolon>{root},NumberOfResults=1};
        f.Provider.Setup(p=>p.Search(It.IsAny<Core.Objects.Search.ISearchParams>(),true,true,1,true,0)).Returns(new OASISResult<Core.Interfaces.Search.ISearchResults>(payload));
        f.Provider.Setup(p=>p.SearchAsync(It.IsAny<Core.Objects.Search.ISearchParams>(),true,true,1,true,0)).ReturnsAsync(new OASISResult<Core.Interfaces.Search.ISearchResults>(payload));
        f.Provider.Setup(p=>p.LoadHolon(quest.Id,false,false,0,true,false,0)).Returns(new OASISResult<IHolon>(quest));
        f.Provider.Setup(p=>p.LoadHolon(other.Id,false,false,0,true,false,0)).Returns(new OASISResult<IHolon>(other));
        f.Provider.Setup(p=>p.LoadHolonAsync(quest.Id,false,false,0,true,false,0)).ReturnsAsync(new OASISResult<IHolon>(quest));
        f.Provider.Setup(p=>p.LoadHolonAsync(other.Id,false,false,0,true,false,0)).ReturnsAsync(new OASISResult<IHolon>(other));
        var selectedType=filter ? HolonType.Quest:HolonType.All;
        var result=synchronous ? f.Manager.SearchHolons("owned",Guid.Empty,searchOnlyForCurrentAvatar:false,maxChildDepth:1,
                loadChildrenFromProvider:fromProvider,childHolonType:selectedType,providerType:ProviderType.SQLLiteDBOASIS)
            :await f.Manager.SearchHolonsAsync("owned",Guid.Empty,searchOnlyForCurrentAvatar:false,maxChildDepth:1,
                loadChildrenFromProvider:fromProvider,childHolonType:selectedType,providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var actual=result.Result.Should().ContainSingle().Subject;
        actual.Children.Select(child=>child.Id).Should().Equal(filter ? new[]{quest.Id}:new[]{quest.Id,other.Id});
        actual.Should().BeAssignableTo<Holon>().Which.ChildRoleManifestJson.Should().Be(root.ChildRoleManifestJson);actual.HasHolonChanged().Should().BeFalse();
        root.Children.Count.Should().Be(fromProvider ? 2:0);payload.SearchResultHolons.Single().Should().BeSameAs(root);
        if(fromProvider)
        {
            f.Provider.Verify(p=>p.LoadHolon(It.IsAny<Guid>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>()),Times.Never);
            f.Provider.Verify(p=>p.LoadHolonAsync(It.IsAny<Guid>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>()),Times.Never);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DefaultV2ManagerGraphTraversesDeepMembershipWithoutRecursiveFrames(bool synchronous)
    {
        var f=Fixture(true);const int count=4096;
        var nodes=Enumerable.Range(0,count).Select(index=>new Holon {Id=Guid.NewGuid(),Name="deep owner "+index}).ToArray();
        for(var index=0;index<count;index++)
        {
            if(index>0)nodes[index].ParentHolonId=nodes[index-1].Id;
            nodes[index].ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,nodes[index].Id,
                new[]{new HolonChildRole("Children",index+1<count ? new[]{nodes[index+1].Id}:Array.Empty<Guid>())}));
        }
        var records=nodes.ToDictionary(node=>node.Id);
        f.Provider.Setup(p=>p.LoadAllHolons(HolonType.All,true,true,0,0,true,false,0)).Returns(new OASISResult<IEnumerable<IHolon>>(new[]{nodes[0]}));
        f.Provider.Setup(p=>p.LoadAllHolonsAsync(HolonType.All,true,true,0,0,true,false,0)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(new[]{nodes[0]}));
        f.Provider.Setup(p=>p.LoadHolon(It.IsAny<Guid>(),false,false,0,true,false,0))
            .Returns((Guid id,bool children,bool recursive,int depth,bool keepGoing,bool native,int version)=>new OASISResult<IHolon>(records[id]));
        f.Provider.Setup(p=>p.LoadHolonAsync(It.IsAny<Guid>(),false,false,0,true,false,0))
            .ReturnsAsync((Guid id,bool children,bool recursive,int depth,bool keepGoing,bool native,int version)=>new OASISResult<IHolon>(records[id]));
        var result=synchronous ? f.Manager.LoadAllHolons(cache:false,providerType:ProviderType.SQLLiteDBOASIS)
            :await f.Manager.LoadAllHolonsAsync(providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var cursor=result.Result.Should().ContainSingle().Subject;var seen=1;
        while(cursor.Children.Any()){cursor=cursor.Children.Should().ContainSingle().Subject;seen++;}
        seen.Should().Be(count);cursor.Id.Should().Be(nodes[^1].Id);
        cursor.HasHolonChanged().Should().BeFalse();nodes.Should().OnlyContain(node=>node.Children.Count==0);
    }

    [Theory]
    [InlineData(false,false,false)] [InlineData(true,false,false)]
    [InlineData(false,true,false)] [InlineData(true,true,false)]
    [InlineData(false,false,true)] [InlineData(true,false,true)]
    [InlineData(false,true,true)] [InlineData(true,true,true)]
    public async Task DefaultV2PointLoadOwnsSelectedChildrenAndPreservesNativeFailure(bool synchronous,bool byKey,bool missing)
    {
        var f=Fixture(true);var owner=new Holon {Id=Guid.NewGuid()};
        var child=new Holon {Id=Guid.NewGuid(),ParentHolonId=owner.Id,Name="point member"};
        owner.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,owner.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child.Id})}));
        f.Provider.Setup(p=>p.LoadHolon(owner.Id,true,true,1,true,false,0)).Returns(new OASISResult<IHolon>(owner));
        f.Provider.Setup(p=>p.LoadHolonAsync(owner.Id,true,true,1,true,false,0)).ReturnsAsync(new OASISResult<IHolon>(owner));
        f.Provider.Setup(p=>p.LoadHolon("point-owner",true,true,1,true,false,0)).Returns(new OASISResult<IHolon>(owner));
        f.Provider.Setup(p=>p.LoadHolonAsync("point-owner",true,true,1,true,false,0)).ReturnsAsync(new OASISResult<IHolon>(owner));
        var cause=new IOException("point child unavailable");
        var member=missing ? new OASISResult<IHolon>{IsError=true,ErrorCode="OWNED_POINT_CHILD_FAILURE",Exception=cause}:new OASISResult<IHolon>(child);
        f.Provider.Setup(p=>p.LoadHolon(child.Id,false,false,0,true,false,0)).Returns(member);
        f.Provider.Setup(p=>p.LoadHolonAsync(child.Id,false,false,0,true,false,0)).ReturnsAsync(member);
        var result=byKey
            ? synchronous ? f.Manager.LoadHolon<LoadedRoleOwner>("point-owner",maxChildDepth:1,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonAsync<LoadedRoleOwner>("point-owner",maxChildDepth:1,providerType:ProviderType.SQLLiteDBOASIS)
            : synchronous ? f.Manager.LoadHolon<LoadedRoleOwner>(owner.Id,maxChildDepth:1,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonAsync<LoadedRoleOwner>(owner.Id,maxChildDepth:1,providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().Be(missing);
        if(missing){result.Result.Should().BeNull();result.ErrorCode.Should().Be("OWNED_POINT_CHILD_FAILURE");result.Exception.Should().BeSameAs(cause);}
        else {result.Result.Extras.Should().ContainSingle().Which.Id.Should().Be(child.Id);result.Result.HasHolonChanged().Should().BeFalse();}
        owner.Children.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DefaultV2TypedAllQueryRejectsIncompatibleOwnerSchemasWithoutFilteringRoots(bool synchronous)
    {
        var f=Fixture(true);var owner=new Holon {Id=Guid.NewGuid()};
        var child=new Holon {Id=Guid.NewGuid(),ParentHolonId=owner.Id};
        owner.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,owner.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child.Id})}));
        child.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,child.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>())}));
        var rows=new IHolon[]{owner,child};
        f.Provider.Setup(p=>p.LoadAllHolons(HolonType.All,true,true,0,0,true,false,0)).Returns(new OASISResult<IEnumerable<IHolon>>(rows));
        f.Provider.Setup(p=>p.LoadAllHolonsAsync(HolonType.All,true,true,0,0,true,false,0)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(rows));
        f.Provider.Setup(p=>p.LoadHolon(child.Id,false,false,0,true,false,0)).Returns(new OASISResult<IHolon>(child));
        f.Provider.Setup(p=>p.LoadHolonAsync(child.Id,false,false,0,true,false,0)).ReturnsAsync(new OASISResult<IHolon>(child));
        var result=synchronous ? f.Manager.LoadAllHolons<LoadedRoleOwner>(cache:false,providerType:ProviderType.SQLLiteDBOASIS)
            :await f.Manager.LoadAllHolonsAsync<LoadedRoleOwner>(providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeTrue();result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();
        result.ErrorCode.Should().Be("COSMIC_CHILD_ROLE_HYDRATION_FAILED");
        result.Exception.Message.Should().Contain("stored roles differ");owner.Children.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DefaultV2GraphRejectsUndefinedChildTypeBeforeProviderRead(bool synchronous)
    {
        var f=Fixture(true);
        var result=synchronous ? f.Manager.LoadAllHolons(childHolonType:(HolonType)int.MaxValue,cache:false,providerType:ProviderType.SQLLiteDBOASIS)
            :await f.Manager.LoadAllHolonsAsync(childHolonType:(HolonType)int.MaxValue,providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeTrue();result.ErrorCode.Should().Be("HYPERDRIVE_CHILD_TYPE_INVALID");
        f.Provider.Verify(p=>p.LoadAllHolons(It.IsAny<HolonType>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>()),Times.Never);
        f.Provider.Verify(p=>p.LoadAllHolonsAsync(It.IsAny<HolonType>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>()),Times.Never);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DefaultV2AllQueryAllowsAChildAlsoReturnedAsAnIndependentRoot(bool synchronous)
    {
        var f=Fixture(true);var owner=new Holon {Id=Guid.NewGuid()};
        var child=new Holon {Id=Guid.NewGuid(),ParentHolonId=owner.Id,Name="overlapping member"};
        owner.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,owner.Id,
            new[]{new HolonChildRole("Children",new[]{child.Id})}));
        child.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,child.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>())}));
        var roots=new IHolon[]{owner,child};
        f.Provider.Setup(p=>p.LoadAllHolons(HolonType.All,true,true,0,0,true,false,0)).Returns(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadAllHolonsAsync(HolonType.All,true,true,0,0,true,false,0)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadHolon(child.Id,false,false,0,true,false,0)).Returns(new OASISResult<IHolon>(child));
        f.Provider.Setup(p=>p.LoadHolonAsync(child.Id,false,false,0,true,false,0)).ReturnsAsync(new OASISResult<IHolon>(child));
        var result=synchronous ? f.Manager.LoadAllHolons(cache:false,providerType:ProviderType.SQLLiteDBOASIS)
            :await f.Manager.LoadAllHolonsAsync(providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message+result.Exception);var actual=result.Result.ToArray();actual.Should().HaveCount(2);
        var nested=actual.Single(value=>value.Id==owner.Id).Children.Should().ContainSingle().Subject;
        nested.Id.Should().Be(child.Id);nested.Should().NotBeSameAs(actual.Single(value=>value.Id==child.Id));
        actual.Should().OnlyContain(value=>!value.HasHolonChanged());nested.HasHolonChanged().Should().BeFalse();
        owner.Children.Should().BeEmpty();child.Children.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false,"all")] [InlineData(true,"all")]
    [InlineData(false,"metadata")] [InlineData(true,"metadata")]
    [InlineData(false,"pairs")] [InlineData(true,"pairs")]
    [InlineData(false,"parent")] [InlineData(true,"parent")]
    [InlineData(false,"parentKey")] [InlineData(true,"parentKey")]
    public async Task DefaultV2CollectionLoadResolvesManagerOwnedChildrenBeforeTypedRoles(bool synchronous,string route)
    {
        var f=Fixture(true);
        var owner=new Holon {Id=Guid.NewGuid(),Name="root-only provider observation"};
        var child=new Holon {Id=Guid.NewGuid(),ParentHolonId=owner.Id,Name="separately stored member"};
        var grandchildId=Guid.NewGuid();
        child.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,child.Id,
            new[]{new HolonChildRole("Children",new[]{grandchildId})}));
        owner.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,owner.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child.Id})}));
        var roots=new IHolon[]{owner};var parentId=Guid.NewGuid();owner.ParentHolonId=parentId;
        var depth=route.StartsWith("parent") ? 2:1;
        var pairs=new Dictionary<string,string>{{"owned","member"}};
        f.Provider.Setup(p=>p.LoadHolon(child.Id,false,false,0,true,false,0))
            .Returns(new OASISResult<IHolon>(child));
        f.Provider.Setup(p=>p.LoadHolonAsync(child.Id,false,false,0,true,false,0))
            .ReturnsAsync(new OASISResult<IHolon>(child));
        f.Provider.Setup(p=>p.LoadAllHolons(HolonType.All,true,true,1,0,true,false,0))
            .Returns(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadAllHolonsAsync(HolonType.All,true,true,1,0,true,false,0))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadHolonsByMetaData("owned","member",HolonType.All,true,true,1,0,true,false,0)).Returns(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadHolonsByMetaDataAsync("owned","member",HolonType.All,true,true,1,0,true,false,0)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadHolonsByMetaData(pairs,MetaKeyValuePairMatchMode.All,HolonType.All,true,true,1,0,true,false,0)).Returns(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadHolonsByMetaDataAsync(pairs,MetaKeyValuePairMatchMode.All,HolonType.All,true,true,1,0,true,false,0)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadHolonsForParent(parentId,HolonType.All,true,true,2,0,true,false,0)).Returns(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadHolonsForParentAsync(parentId,HolonType.All,true,true,2,0,true,false,0)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadHolonsForParent("owned-parent",HolonType.All,true,true,2,0,true,false,0)).Returns(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadHolonsForParentAsync("owned-parent",HolonType.All,true,true,2,0,true,false,0)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadHolonsForParent(owner.Id,It.IsAny<HolonType>(),It.IsAny<bool>(),It.IsAny<bool>(),
            It.IsAny<int>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>()))
            .Returns(new OASISResult<IEnumerable<IHolon>>(new IHolon[]{child}));
        f.Provider.Setup(p=>p.LoadHolonsForParentAsync(owner.Id,It.IsAny<HolonType>(),It.IsAny<bool>(),It.IsAny<bool>(),
            It.IsAny<int>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>()))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(new IHolon[]{child}));
        var loaded=route switch {
            "metadata"=>synchronous ? f.Manager.LoadHolonsByMetaData<LoadedRoleOwner>("owned","member",maxChildDepth:depth,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonsByMetaDataAsync<LoadedRoleOwner>("owned","member",maxChildDepth:depth,providerType:ProviderType.SQLLiteDBOASIS),
            "pairs"=>synchronous ? f.Manager.LoadHolonsByMetaData<LoadedRoleOwner>(pairs,MetaKeyValuePairMatchMode.All,maxChildDepth:depth,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonsByMetaDataAsync<LoadedRoleOwner>(pairs,MetaKeyValuePairMatchMode.All,maxChildDepth:depth,providerType:ProviderType.SQLLiteDBOASIS),
            "parent"=>synchronous ? f.Manager.LoadHolonsForParent<LoadedRoleOwner>(parentId,maxChildDepth:depth,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonsForParentAsync<LoadedRoleOwner>(parentId,maxChildDepth:depth,providerType:ProviderType.SQLLiteDBOASIS),
            "parentKey"=>synchronous ? f.Manager.LoadHolonsForParent<LoadedRoleOwner>("owned-parent",maxChildDepth:depth,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonsForParentAsync<LoadedRoleOwner>("owned-parent",maxChildDepth:depth,providerType:ProviderType.SQLLiteDBOASIS),
            _=>synchronous ? f.Manager.LoadAllHolons<LoadedRoleOwner>(maxChildDepth:depth,cache:false,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadAllHolonsAsync<LoadedRoleOwner>(maxChildDepth:depth,providerType:ProviderType.SQLLiteDBOASIS)};
        loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);
        var actual=loaded.Result.Should().ContainSingle().Subject;
        actual.Extras.Should().ContainSingle().Which.Id.Should().Be(child.Id);
        actual.HasHolonChanged().Should().BeFalse();
        owner.Children.Should().BeEmpty("the provider observation must remain detached from manager hydration");
        f.Provider.Verify(p=>p.LoadHolon(grandchildId,It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>()),Times.Never);
        f.Provider.Verify(p=>p.LoadHolonAsync(grandchildId,It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>()),Times.Never);
    }

    [Theory]
    [InlineData(false,false)] [InlineData(true,false)]
    [InlineData(false,true)] [InlineData(true,true)]
    public async Task DefaultV2CollectionMembershipDoesNotResurrectUnlinksOrPublishFailedGraphs(bool synchronous,bool missing)
    {
        var f=Fixture(true);var owner=new Holon {Id=Guid.NewGuid()};var childId=Guid.NewGuid();
        owner.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,owner.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",missing ? new[]{childId}:Array.Empty<Guid>())}));
        var roots=new IHolon[]{owner};
        f.Provider.Setup(p=>p.LoadAllHolons(HolonType.All,true,true,1,0,true,false,0)).Returns(new OASISResult<IEnumerable<IHolon>>(roots));
        f.Provider.Setup(p=>p.LoadAllHolonsAsync(HolonType.All,true,true,1,0,true,false,0)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(roots));
        var nativeCause=new IOException("owned native child read failure");
        var rejected=new OASISResult<IHolon>{IsError=true,ErrorCode="OWNED_MISSING_MEMBER",Message="owned missing member",Exception=nativeCause};
        f.Provider.Setup(p=>p.LoadHolon(childId,false,false,0,true,false,0)).Returns(rejected);
        f.Provider.Setup(p=>p.LoadHolonAsync(childId,false,false,0,true,false,0)).ReturnsAsync(rejected);
        var result=synchronous ? f.Manager.LoadAllHolons<LoadedRoleOwner>(maxChildDepth:1,cache:false,providerType:ProviderType.SQLLiteDBOASIS)
            :await f.Manager.LoadAllHolonsAsync<LoadedRoleOwner>(maxChildDepth:1,providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().Be(missing);
        if(missing){result.Result.Should().BeNull();result.ErrorCode.Should().Be("OWNED_MISSING_MEMBER");result.Exception.Should().BeSameAs(nativeCause);result.IsLoaded.Should().BeFalse();}
        else {var actual=result.Result.Should().ContainSingle().Subject;actual.AllChildren.Should().BeEmpty();actual.HasHolonChanged().Should().BeFalse();}
        owner.Children.Should().BeEmpty();
        f.Provider.Verify(p=>p.LoadHolonsForParent(owner.Id,It.IsAny<HolonType>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>()),Times.Never);
        f.Provider.Verify(p=>p.LoadHolonsForParentAsync(owner.Id,It.IsAny<HolonType>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>()),Times.Never);
    }

    public sealed class RoleMappedHolon:Holon { }
    public sealed class LoadedSpecial:Holon { }
    public sealed class LoadedRoleOwner:Holon
    {
        public List<LoadedSpecial> Extras=new();
        public override System.Collections.ObjectModel.ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Extras).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection("Extras",Extras,members=>Extras=members.Cast<LoadedSpecial>().ToList())}).ToArray();
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings()=>base.GetChildRoleBindings()
            .Concat(new[]{HolonChildRoleBinding.Create<LoadedSpecial>("Extras",members=>Extras=members.ToList(),
                value=>Core.Helpers.Mapper<IHolon,LoadedSpecial>.MapBaseHolonProperties(value))}).ToArray();
    }

    [Theory]
    [InlineData(false,false,true)] [InlineData(true,false,true)]
    [InlineData(false,true,true)] [InlineData(true,true,true)]
    [InlineData(false,true,false)] [InlineData(true,true,false)]
    public async Task GenericV2SearchHydratesOnlySelectedRolesAndTracksValues(bool synchronous,bool missing,bool loadChildren)
    {
        var f=Fixture(true);var source=new Holon{Id=Guid.NewGuid(),Name="search owner"};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=source.Id,Name="search member"};
        if(!missing)source.Children.Add(child);
        source.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,source.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child.Id})}));
        var payload=new Core.Objects.Search.SearchResults {SearchResultHolons=new List<IHolon>{source}};
        f.Provider.Setup(p=>p.Search(It.IsAny<Core.Objects.Search.ISearchParams>(),loadChildren,true,1,true,0))
            .Returns(new OASISResult<Core.Interfaces.Search.ISearchResults>(payload));
        f.Provider.Setup(p=>p.SearchAsync(It.IsAny<Core.Objects.Search.ISearchParams>(),loadChildren,true,1,true,0))
            .ReturnsAsync(new OASISResult<Core.Interfaces.Search.ISearchResults>(payload));
        var result=synchronous ? f.Manager.SearchHolons<LoadedRoleOwner>("search",Guid.Empty,searchOnlyForCurrentAvatar:false,
                loadChildren:loadChildren,maxChildDepth:1,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS)
            :await f.Manager.SearchHolonsAsync<LoadedRoleOwner>("search",Guid.Empty,searchOnlyForCurrentAvatar:false,
                loadChildren:loadChildren,maxChildDepth:1,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().Be(missing && loadChildren,result.Message+result.Exception);
        if(missing && loadChildren)
        {
            result.Result.Should().BeNull();result.ErrorCode.Should().Be("COSMIC_CHILD_ROLE_HYDRATION_FAILED");
        }
        else
        {
            var owner=result.Result.Should().ContainSingle().Subject;
            owner.Extras.Count.Should().Be(loadChildren ? 1:0);
            owner.ChildRoleManifestJson.Should().Be(source.ChildRoleManifestJson);
            owner.HasHolonChanged().Should().BeFalse();
            owner.Name="local search edit";owner.HasHolonChanged().Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false,false,0,true)] [InlineData(true,false,0,true)]
    [InlineData(false,true,0,true)] [InlineData(true,true,0,true)]
    [InlineData(false,false,1,true)] [InlineData(true,false,1,true)]
    [InlineData(false,true,1,true)] [InlineData(true,true,1,true)]
    [InlineData(false,false,0,false)] [InlineData(true,false,0,false)]
    [InlineData(false,true,0,false)] [InlineData(true,true,0,false)]
    [InlineData(false,false,int.MaxValue,true)] [InlineData(true,false,int.MaxValue,true)]
    [InlineData(false,true,int.MaxValue,true)] [InlineData(true,true,int.MaxValue,true)]
    public async Task GenericV2ParentRolesCountParentEdgeAndNonrecursiveFrontier(bool synchronous,bool byKey,int currentDepth,bool recursive)
    {
        var f=Fixture(true);var parentId=Guid.NewGuid();var source=new Holon{Id=Guid.NewGuid(),ParentHolonId=parentId};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=source.Id,Name="parent query descendant"};
        var selected=currentDepth==0 && recursive;
        if(selected)source.Children.Add(child);
        source.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,source.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child.Id})}));
        var rows=new IHolon[]{source};
        f.Provider.Setup(p=>p.LoadHolonsForParent(parentId,HolonType.All,true,recursive,2,currentDepth,true,true,0))
            .Returns(new OASISResult<IEnumerable<IHolon>>(rows));
        f.Provider.Setup(p=>p.LoadHolonsForParentAsync(parentId,HolonType.All,true,recursive,2,currentDepth,true,true,0))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(rows));
        f.Provider.Setup(p=>p.LoadHolonsForParent("parent-role-key",HolonType.All,true,recursive,2,currentDepth,true,true,0))
            .Returns(new OASISResult<IEnumerable<IHolon>>(rows));
        f.Provider.Setup(p=>p.LoadHolonsForParentAsync("parent-role-key",HolonType.All,true,recursive,2,currentDepth,true,true,0))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(rows));
        var result=byKey
            ? synchronous ? f.Manager.LoadHolonsForParent<LoadedRoleOwner>("parent-role-key",recursive:recursive,maxChildDepth:2,currentChildDepth:currentDepth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonsForParentAsync<LoadedRoleOwner>("parent-role-key",recursive:recursive,maxChildDepth:2,currentChildDepth:currentDepth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS)
            : synchronous ? f.Manager.LoadHolonsForParent<LoadedRoleOwner>(parentId,recursive:recursive,maxChildDepth:2,currentChildDepth:currentDepth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonsForParentAsync<LoadedRoleOwner>(parentId,recursive:recursive,maxChildDepth:2,currentChildDepth:currentDepth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var owner=result.Result.Should().ContainSingle().Subject;
        owner.Children.Should().BeEmpty();owner.Extras.Count.Should().Be(selected ? 1:0);
        owner.ChildRoleManifestJson.Should().Be(source.ChildRoleManifestJson);
        owner.HasHolonChanged().Should().BeFalse();
    }

    [Theory]
    [InlineData(false,false,1)] [InlineData(true,false,1)]
    [InlineData(false,true,1)] [InlineData(true,true,1)]
    [InlineData(false,false,2)] [InlineData(true,false,2)]
    [InlineData(false,true,2)] [InlineData(true,true,2)]
    public async Task GenericV2MetadataRolesRespectCurrentDepthAndPreserveFrontier(bool synchronous,bool pairs,int currentDepth)
    {
        var f=Fixture(true);var source=new Holon{Id=Guid.NewGuid()};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=source.Id,Name="metadata child"};
        if(currentDepth<2)source.Children.Add(child);
        source.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,source.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child.Id})}));
        var rows=new IHolon[]{source};var criteria=new Dictionary<string,string>{{"kind","sample"}};
        f.Provider.Setup(p=>p.LoadHolonsByMetaData("kind","sample",HolonType.All,true,true,2,currentDepth,true,true,0))
            .Returns(new OASISResult<IEnumerable<IHolon>>(rows));
        f.Provider.Setup(p=>p.LoadHolonsByMetaDataAsync("kind","sample",HolonType.All,true,true,2,currentDepth,true,true,0))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(rows));
        f.Provider.Setup(p=>p.LoadHolonsByMetaData(It.IsAny<Dictionary<string,string>>(),MetaKeyValuePairMatchMode.All,HolonType.All,true,true,2,currentDepth,true,true,0))
            .Returns(new OASISResult<IEnumerable<IHolon>>(rows));
        f.Provider.Setup(p=>p.LoadHolonsByMetaDataAsync(It.IsAny<Dictionary<string,string>>(),MetaKeyValuePairMatchMode.All,HolonType.All,true,true,2,currentDepth,true,true,0))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(rows));
        var result=pairs
            ? synchronous ? f.Manager.LoadHolonsByMetaData<LoadedRoleOwner>(criteria,MetaKeyValuePairMatchMode.All,maxChildDepth:2,currentChildDepth:currentDepth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonsByMetaDataAsync<LoadedRoleOwner>(criteria,MetaKeyValuePairMatchMode.All,maxChildDepth:2,currentChildDepth:currentDepth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS)
            : synchronous ? f.Manager.LoadHolonsByMetaData<LoadedRoleOwner>("kind","sample",maxChildDepth:2,currentChildDepth:currentDepth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonsByMetaDataAsync<LoadedRoleOwner>("kind","sample",maxChildDepth:2,currentChildDepth:currentDepth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var owner=result.Result.Should().ContainSingle().Subject;
        owner.ChildRoleManifestJson.Should().Be(source.ChildRoleManifestJson);
        owner.Children.Should().BeEmpty();owner.Extras.Count.Should().Be(currentDepth<2 ? 1:0);
        owner.HasHolonChanged().Should().BeFalse();
    }

    [Theory]
    [InlineData(false, false, true)] [InlineData(true, false, true)]
    [InlineData(false, true, true)] [InlineData(true, true, true)]
    [InlineData(false, true, false)] [InlineData(true, true, false)]
    public async Task GenericV2CollectionsHydrateRolesBeforeTrackingAndRejectMissingMembers(bool synchronous,bool missing,bool loadChildren)
    {
        var f=Fixture(true);var source=new Holon{Id=Guid.NewGuid()};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=source.Id,Name="typed collection child"};
        if(!missing)source.Children.Add(child);
        source.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,source.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child.Id})}));
        var rows=new IHolon[]{source};
        f.Provider.Setup(p=>p.LoadAllHolons(HolonType.All,loadChildren,true,1,0,true,true,0))
            .Returns(new OASISResult<IEnumerable<IHolon>>(rows));
        f.Provider.Setup(p=>p.LoadAllHolonsAsync(HolonType.All,loadChildren,true,1,0,true,true,0))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(rows));
        var result=synchronous ? f.Manager.LoadAllHolons<LoadedRoleOwner>(loadChildren:loadChildren,maxChildDepth:1,loadChildrenFromProvider:true,cache:false,providerType:ProviderType.SQLLiteDBOASIS)
            :await f.Manager.LoadAllHolonsAsync<LoadedRoleOwner>(loadChildren:loadChildren,maxChildDepth:1,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().Be(missing && loadChildren,result.Message+result.Exception);
        if(missing && loadChildren)
        {
            result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();
            result.ErrorCode.Should().Be("COSMIC_CHILD_ROLE_HYDRATION_FAILED");
        }
        else if(!loadChildren)
        {
            var owner=result.Result.Should().ContainSingle().Subject;
            owner.Extras.Should().BeEmpty();owner.ChildRoleManifestJson.Should().Be(source.ChildRoleManifestJson);
            owner.HasHolonChanged().Should().BeFalse();
        }
        else
        {
            var owner=result.Result.Should().ContainSingle().Subject;
            owner.Children.Should().BeEmpty();owner.Extras.Should().ContainSingle().Which.Name.Should().Be(child.Name);
            owner.HasHolonChanged().Should().BeFalse();
            owner.Extras[0].Name="local edit";owner.HasHolonChanged().Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void BaseMappingPreservesLogicalRevisionLineage(bool generic)
    {
        var source=new Holon{VersionId=Guid.NewGuid(),PreviousVersionId=Guid.NewGuid()};var target=new RoleMappedHolon();
        if(generic)Core.Helpers.Mapper<IHolon,RoleMappedHolon>.MapBaseHolonProperties(source,target);
        else Core.Helpers.Mapper.MapBaseHolonProperties(source,target);
        target.VersionId.Should().Be(source.VersionId);target.PreviousVersionId.Should().Be(source.PreviousVersionId);
    }

    [Theory]
    [InlineData(false,false,1)] [InlineData(false,false,2)] [InlineData(false,true,1)] [InlineData(false,true,2)]
    [InlineData(true,false,1)] [InlineData(true,false,2)] [InlineData(true,true,1)] [InlineData(true,true,2)]
    public async Task GenericV2LoadsMaterializeRegisteredRolesBeforeAcceptingBaseline(bool synchronous,bool byKey,int depth)
    {
        var f=Fixture(true);var source=new Holon{Id=Guid.NewGuid(),VersionId=Guid.NewGuid(),PreviousVersionId=Guid.NewGuid()};
        var child=new Holon{Id=Guid.NewGuid(),Name="loaded child",ParentHolonId=source.Id,VersionId=Guid.NewGuid()};
        var grand=new Holon{Id=Guid.NewGuid(),Name="loaded grandchild",ParentHolonId=child.Id};
        source.Children.Add(child);if(depth==2)child.Children.Add(grand);
        source.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,source.Id,
            new[]{new HolonChildRole("Children",Array.Empty<Guid>()),new HolonChildRole("Extras",new[]{child.Id})}));
        child.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,child.Id,new[]{new HolonChildRole("Children",new[]{grand.Id})}));
        f.Provider.Setup(p=>p.LoadHolon(source.Id,true,true,depth,true,true,0)).Returns(new OASISResult<IHolon>(source));
        f.Provider.Setup(p=>p.LoadHolonAsync(source.Id,true,true,depth,true,true,0)).ReturnsAsync(new OASISResult<IHolon>(source));
        f.Provider.Setup(p=>p.LoadHolon("typed-role-key",true,true,depth,true,true,0)).Returns(new OASISResult<IHolon>(source));
        f.Provider.Setup(p=>p.LoadHolonAsync("typed-role-key",true,true,depth,true,true,0)).ReturnsAsync(new OASISResult<IHolon>(source));
        var result=byKey
            ? synchronous ? f.Manager.LoadHolon<LoadedRoleOwner>("typed-role-key",maxChildDepth:depth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonAsync<LoadedRoleOwner>("typed-role-key",maxChildDepth:depth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS)
            : synchronous ? f.Manager.LoadHolon<LoadedRoleOwner>(source.Id,maxChildDepth:depth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonAsync<LoadedRoleOwner>(source.Id,maxChildDepth:depth,loadChildrenFromProvider:true,providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message+result.Exception);result.Result.Children.Should().BeEmpty();
        var loaded=result.Result.Extras.Should().ContainSingle().Subject;
        loaded.Name.Should().Be(child.Name);loaded.VersionId.Should().Be(child.VersionId);loaded.Children.Count.Should().Be(depth==1 ? 0:1);
        result.Result.VersionId.Should().Be(source.VersionId);result.Result.PreviousVersionId.Should().Be(source.PreviousVersionId);
        result.Result.HasHolonChanged().Should().BeFalse();source.Children.Should().ContainSingle().Which.Should().BeSameAs(child);
    }

    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public void BaseMappersCopyRelationshipObservationIndependentlyOfRuntimeChildren(bool generic,bool observed)
    {
        var source=new Holon{Id=Guid.NewGuid()};
        var target=new RoleMappedHolon{ChildRoleManifestJson="stale observation"};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=source.Id};source.Children.Add(child);
        if(observed)source.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,source.Id,
            new[]{new HolonChildRole("Children",new[]{child.Id})}));
        if(generic)NextGenSoftware.OASIS.API.Core.Helpers.Mapper<IHolon,RoleMappedHolon>.MapBaseHolonProperties(source,target);
        else NextGenSoftware.OASIS.API.Core.Helpers.Mapper.MapBaseHolonProperties(source,target,mapChildren:false);
        target.ChildRoleManifestJson.Should().Be(source.ChildRoleManifestJson);
        if(!generic)target.Children.Should().BeEmpty();
        target.Original.Should().BeNull("mapping must not accept a tracking baseline");
    }

    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task GenericV2PointLoadsPreserveObservedRelationshipManifest(bool synchronous,bool byKey)
    {
        var f=Fixture(true);var source=new Holon{Id=Guid.NewGuid()};
        source.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,source.Id,
            new[]{new HolonChildRole("Children",new[]{Guid.NewGuid()})}));
        f.Provider.Setup(p=>p.LoadHolon(source.Id,false,true,0,true,false,0)).Returns(new OASISResult<IHolon>(source));
        f.Provider.Setup(p=>p.LoadHolonAsync(source.Id,false,true,0,true,false,0)).ReturnsAsync(new OASISResult<IHolon>(source));
        f.Provider.Setup(p=>p.LoadHolon("role-key",false,true,0,true,false,0)).Returns(new OASISResult<IHolon>(source));
        f.Provider.Setup(p=>p.LoadHolonAsync("role-key",false,true,0,true,false,0)).ReturnsAsync(new OASISResult<IHolon>(source));
        var result=byKey
            ? synchronous ? f.Manager.LoadHolon<RoleMappedHolon>("role-key",loadChildren:false,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonAsync<RoleMappedHolon>("role-key",loadChildren:false,providerType:ProviderType.SQLLiteDBOASIS)
            : synchronous ? f.Manager.LoadHolon<RoleMappedHolon>(source.Id,loadChildren:false,providerType:ProviderType.SQLLiteDBOASIS)
                :await f.Manager.LoadHolonAsync<RoleMappedHolon>(source.Id,loadChildren:false,providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        result.Result.ChildRoleManifestJson.Should().Be(source.ChildRoleManifestJson);
        result.Result.Children.Should().BeEmpty("unselected relationships must remain unhydrated");
        result.Result.HasHolonChanged().Should().BeFalse();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidRelationshipIntentFailsRoutingBeforeAnyProviderInvocation(bool synchronous)
    {
        var f=Fixture(true);
        f.Provider.Object.IsProviderActivated=false;
        f.Provider.Invocations.Clear();
        var source=new Holon{Id=Guid.NewGuid()};
        var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {
            Operation="SaveHolon",Payload=source,PreferredProvider=ProviderType.SQLLiteDBOASIS,
            SubmittedRelationships=new Dictionary<Guid,HolonChildRoleManifest> {
                [source.Id]=new HolonChildRoleManifest(1,source.Id,new[]{new HolonChildRole("Children",new[]{Guid.NewGuid()})}) } };
        var router=new NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive(f.Providers);
        var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
        result.IsError.Should().BeTrue();result.ErrorCode.Should().Be("COSMIC_RELATIONSHIP_INTENT_INVALID");
        f.Provider.Invocations.Should().BeEmpty("invalid relationship planning must fail before activation or storage access");
        source.IsNewHolon.Should().BeTrue();source.Original.Should().BeNull();
    }

    [Theory]
    [InlineData("wrong-key")] [InlineData("corrupt")]
    [InlineData("invalid-json")] [InlineData("null-payload")]
    public void FailedMetadataDecryptionPreservesCiphertextAndExistingValues(string fault)
    {
        var f=Fixture(true);
        var settings=new EncryptionSettings{Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('e',32)};
        f.Manager.OASISDNA.OASIS.Security=new SecuritySettings{HolonDataEncryption=settings};
        var payload=fault=="invalid-json" ? "not-json":fault=="null-payload" ? "null":"{\"private-note\":\"private-value\"}";
        var ciphertext=fault=="corrupt" ? "invalid-ciphertext"
            :NextGenSoftware.OASIS.API.Core.Helpers.PasswordEncryptionHelper.EncryptValue(payload,settings);
        var source=new Holon();source.MetaData["__oasis_enc__"]=ciphertext;source.MetaData["existing"]="preserved";
        if(fault=="wrong-key")source.DataEncryptionOverride=new EncryptionSettings{
            Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('w',32)};

        var result=f.Manager.DecryptHolonMetaData(source);

        result.IsError.Should().BeTrue();result.Result.Should().BeFalse();
        source.MetaData["__oasis_enc__"].Should().Be(ciphertext);
        source.MetaData["existing"].Should().Be("preserved");
        source.MetaData.Should().NotContainKey("private-note");
        source.MetaData.Count.Should().Be(2);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task ConditionalWriteRejectsUnsupportedAuthorityBeforeActivationOrMutation(bool v2, bool synchronous)
    {
        var f = Fixture(v2);
        f.Provider.Object.IsProviderActivated = false;
        f.Provider.Invocations.Clear(); // Ignore fixture registration/initial activation.
        f.Providers.IsAutoFailOverEnabled = true;
        f.Providers.IsAutoReplicationEnabled = true;
        var root = new Holon { Name = "pending" };
        var result = synchronous
            ? f.Manager.SaveHolon(root, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()))
            : await f.Manager.SaveHolonAsync(root, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()));
        result.ErrorCode.Should().Be("COSMIC_CONDITIONAL_WRITE_UNSUPPORTED");
        root.Id.Should().BeEmpty(); root.Version.Should().Be(0);
        f.Provider.Verify(provider => provider.ActivateProvider(), Times.Never);
        f.Provider.Verify(provider => provider.ActivateProviderAsync(), Times.Never);
        f.Provider.Invocations.Where(call => call.Method.Name.StartsWith("SaveHolon")).Should().BeEmpty();
    }

    public class Sample : Holon
    {
        [CustomOASISProperty] public decimal Price { get; set; }
        [CustomOASISProperty(StoreAsJsonString = true)] public List<string> Tags { get; set; } = new();
    }

    [Fact]
    public void SnapshotIsDetachedAndRevertingValuesBecomesClean()
    {
        var h = new Sample { Id = Guid.NewGuid(), Name = "original", Price = 1.25m, Tags = new() { "one" } };
        h.MetaData["nested"] = new Dictionary<string, object> { ["amount"] = 2.75m };
        h.AcceptChanges();
        h.Original.Should().NotBeSameAs(h);
        h.Original.MetaData.Should().NotBeSameAs(h.MetaData);
        h.HasHolonChanged().Should().BeFalse();
        h.Name = "edited"; h.HasHolonChanged().Should().BeTrue();
        h.Name = "original"; h.HasHolonChanged().Should().BeFalse();
        h.Price = 1.30m; h.HasHolonChanged().Should().BeTrue();
        h.Price = 1.25m; h.Tags.Add("two"); h.HasHolonChanged().Should().BeTrue();
        h.Tags.Remove("two"); h.HasHolonChanged().Should().BeFalse();
        ((Dictionary<string, object>)h.MetaData["nested"])["amount"] = 3.75m;
        h.HasHolonChanged().Should().BeTrue();
    }

    [Fact]
    public void RejectRestoresValuesAndOriginalCollectionMembershipWithoutTrustingPublicOriginal()
    {
        var child = new Holon { Id = Guid.NewGuid(), Name = "child" };
        var h = new Sample { Id = Guid.NewGuid(), Price = 9.99m, Tags = new() { "old" }, Children = new List<IHolon> { child } };
        h.AcceptChanges();
        h.Original.Name = "tampered compatibility snapshot";
        h.Price = 4; h.Tags.Add("new"); child.Name = "changed"; h.Children.Clear();
        h.RejectChanges().IsError.Should().BeFalse();
        h.Name.Should().BeNull(); h.Price.Should().Be(9.99m); h.Tags.Should().Equal("old");
        h.Children.Should().ContainSingle().Which.Should().BeSameAs(child);
        child.Name.Should().Be("child"); h.HasHolonChanged().Should().BeFalse();
    }

    [Fact]
    public void ChildFlagsMembershipAndAutoPropertiesAreTracked()
    {
        var child = new Holon { Id = Guid.NewGuid() };
        var h = new Holon { Id = Guid.NewGuid(), Children = new List<IHolon> { child } };
        h.AcceptChanges();
        child.MetaData["value"] = "new";
        h.HasHolonChanged(false).Should().BeFalse(); h.HasHolonChanged().Should().BeTrue();
        h.RejectChanges().IsError.Should().BeFalse();
        h.IsPublic = true; h.HasHolonChanged(false).Should().BeTrue();
        h.IsPublic = false; h.Children.Add(new Holon()); h.HasHolonChanged().Should().BeTrue();
    }

    [Fact]
    public void DetachedMetadataOnlyObjectNeedsSavingAndNotificationsObserveAssignedValues()
    {
        var h = new Holon { MetaData = new() { ["only"] = "metadata" } };
        h.HasHolonChanged().Should().BeTrue();
        var observed = new List<string>();
        h.PropertyChanged += (_, e) => observed.Add(e.PropertyName == "Name" ? h.Name : h.Description);
        h.Name = "new name"; h.Description = "new description";
        observed.Should().Equal("new name", "new description");
    }

    [Fact]
    public void ReparentingThroughNavigationIsDetectedAndLocalUndoRestoresTheReference()
    {
        var original = new Holon { Id = Guid.NewGuid() }; var replacement = new Holon { Id = Guid.NewGuid() };
        var h = new Holon { Id = Guid.NewGuid(), ParentHolon = original, ParentHolonId = original.Id };
        h.AcceptChanges(); h.ParentHolon = replacement;
        h.HasHolonChanged(false).Should().BeTrue();
        h.RejectChanges().IsError.Should().BeFalse();
        h.ParentHolon.Should().BeSameAs(original); h.ParentHolonId.Should().Be(original.Id);
        h.HasHolonChanged().Should().BeFalse();
    }

    [Fact]
    public void TrackingStateIsNotSerializedByEitherApiSerializerOrCanonicalStorage()
    {
        var h = new Holon { Id = Guid.NewGuid(), Name = "tracked" };
        h.AcceptChanges();
        JsonConvert.SerializeObject(h).Should().NotContain("\"Original\"");
        System.Text.Json.JsonSerializer.Serialize(h).Should().NotContain("\"Original\"");
        StoredRecordJson.Serialize<IHolon>(h).Should().NotContain("IsChanged").And.NotContain("IsNewHolon").And.NotContain("IsSaving");
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task NoOpSaveDoesNotWriteIncrementVersionOrReplicate(bool v2, bool async)
    {
        var f = Fixture(v2);
        var h = new Sample { Id = Guid.NewGuid(), Name = "persisted", Version = 7, VersionId = Guid.NewGuid() };
        h.AcceptChanges(); var revision = h.VersionId;
        var r = async ? await f.Manager.SaveHolonAsync<Sample>(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS)
            : f.Manager.SaveHolon<Sample>(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        r.IsError.Should().BeFalse(); r.IsSaved.Should().BeFalse(); r.HasAnyHolonsChanged.Should().BeFalse();
        h.Version.Should().Be(7); h.VersionId.Should().Be(revision);
        f.Provider.Invocations.Should().NotContain(call => call.Method.Name.StartsWith("Save"));
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task RejectedSaveDoesNotPublishIdentityAuditVersionMetadataOrStripNavigation(bool v2, bool async)
    {
        var f = Fixture(v2, reject: true);
        var parent = new Holon { Id = Guid.NewGuid() };
        var h = new Sample { Name = "pending", ParentHolon = parent, Price = 1.25m, Tags = new() { "original" } };
        var originalMetadata = h.MetaData;
        var r = async ? await f.Manager.SaveHolonAsync<Sample>(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS)
            : f.Manager.SaveHolon<Sample>(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        r.IsError.Should().BeTrue(); r.IsSaved.Should().BeFalse();
        h.Id.Should().BeEmpty(); h.Version.Should().Be(0); h.VersionId.Should().BeEmpty();
        h.CreatedDate.Should().Be(default); h.ParentHolonId.Should().BeEmpty();
        h.ParentHolon.Should().BeSameAs(parent); h.MetaData.Should().BeSameAs(originalMetadata).And.BeEmpty();
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task SuccessfulSavePublishesAcknowledgementAndSecondSaveIsClean(bool v2, bool async)
    {
        var f = Fixture(v2); var h = new Sample { Name = "new", Price = 3.25m, Tags = new() { "kept" } };
        var actor = Guid.NewGuid(); var runtimeHelper = h.GlobalHolonData;
        var r = async ? await f.Manager.SaveHolonAsync<Sample>(h, actor, providerType: ProviderType.SQLLiteDBOASIS)
            : f.Manager.SaveHolon<Sample>(h, actor, providerType: ProviderType.SQLLiteDBOASIS);
        r.IsError.Should().BeFalse(r.Message + " " + r.Exception); r.Result.Should().BeSameAs(h);
        h.Id.Should().NotBeEmpty(); h.Version.Should().Be(1); h.CreatedByAvatarId.Should().Be(actor);
        h.HasHolonChanged().Should().BeFalse(); h.Price.Should().Be(3.25m);
        h.GlobalHolonData.Should().BeSameAs(runtimeHelper);
        var second = await f.Manager.SaveHolonAsync(h, actor, providerType: ProviderType.SQLLiteDBOASIS);
        second.IsSaved.Should().BeFalse(); h.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PendingAsyncSavePreservesLaterUserEditsAndTracksAgainstCommittedValues(bool membershipEdit)
    {
        var f = Fixture(true);
        var entered = new TaskCompletionSource<IHolon>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<OASISResult<IHolon>>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Provider.Setup(p => p.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Returns((IHolon staged, bool a, bool b, int c, bool d, bool e) => { entered.SetResult(staged); return finish.Task; });
        var h = new Sample { Name = "submitted", Price = 1.25m };
        var pending = f.Manager.SaveHolonAsync(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        var staged = await entered.Task;
        staged.Should().NotBeSameAs(h); h.Id.Should().BeEmpty();
        if (membershipEdit) h.Children.Add(new Holon()); else h.Name = "edited while saving";
        finish.SetResult(new OASISResult<IHolon>(staged));
        var result = await pending; result.IsError.Should().BeFalse(result.Message);
        h.Id.Should().NotBeEmpty();
        if (!membershipEdit) h.Name.Should().Be("edited while saving");
        h.HasHolonChanged().Should().BeTrue();
    }

    [Fact]
    public async Task PendingSavePreventsOverlappingSaveOnSameEntity()
    {
        var f = Fixture(true); var entered = new TaskCompletionSource<IHolon>();
        var finish = new TaskCompletionSource<OASISResult<IHolon>>();
        f.Provider.Setup(p => p.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Returns((IHolon staged, bool a, bool b, int c, bool d, bool e) => { entered.SetResult(staged); return finish.Task; });
        var h = new Holon { Name = "same entity" };
        var pending = f.Manager.SaveHolonAsync(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        var staged = await entered.Task;
        var duplicate = await f.Manager.SaveHolonAsync(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        duplicate.IsError.Should().BeTrue();
        finish.SetResult(new OASISResult<IHolon>(staged)); (await pending).IsError.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BatchWritesOnlyDirtyRootsAndPublishesNoCallerStateOnFailure(bool reject)
    {
        var f = Fixture(true, reject);
        var clean = new Holon { Id = Guid.NewGuid(), Version = 4 }; clean.AcceptChanges();
        var dirty = new Holon { Name = "dirty" };
        var result = await f.Manager.SaveHolonsAsync(new[] { clean, dirty }, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().Be(reject); clean.Version.Should().Be(4);
        if (reject) dirty.Id.Should().BeEmpty();
        else { dirty.Id.Should().NotBeEmpty(); dirty.HasHolonChanged().Should().BeFalse(); result.Result.Should().Equal(clean, dirty); }
        var invocation = f.Provider.Invocations.Single(i => i.Method.Name == "SaveHolonsAsync");
        ((IEnumerable<IHolon>)invocation.Arguments[0]).Should().ContainSingle();
    }

    [Fact]
    public async Task IdentityMutationAfterAcknowledgementRequiresReconciliationAndRetainsEvidence()
    {
        var f = Fixture(true); var h = new Holon { Name = "before" }; var movedId = Guid.NewGuid();
        f.Provider.Setup(p => p.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon staged, bool a, bool b, int c, bool d, bool e) =>
            { h.Id = movedId; return new OASISResult<IHolon>(staged); });
        var r = await f.Manager.SaveHolonAsync(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        r.ErrorCode.Should().Be("HOLON_SAVE_PUBLICATION_RECONCILIATION_REQUIRED");
        r.Result.Should().NotBeNull(); r.Result.Id.Should().NotBe(movedId);
        h.Id.Should().Be(movedId); h.Version.Should().Be(0);
        NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(r.Exception).Should().BeTrue();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidAcknowledgementNeverPublishesAProviderChangedIdentityOrEmptySuccess(bool missing)
    {
        var f = Fixture(true); var h = new Holon();
        f.Provider.Setup(p => p.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon staged, bool a, bool b, int c, bool d, bool e) =>
            {
                if (missing) return new OASISResult<IHolon>();
                staged.Id = Guid.NewGuid(); return new OASISResult<IHolon>(staged);
            });
        var result = await f.Manager.SaveHolonAsync(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeTrue(); result.IsSaved.Should().BeFalse(); h.Id.Should().BeEmpty(); h.Version.Should().Be(0);
        result.ErrorCode.Should().Be(missing ? "HOLON_SAVE_ACKNOWLEDGEMENT_MISSING" : "HOLON_SAVE_PUBLICATION_RECONCILIATION_REQUIRED");
    }

    [Fact]
    public async Task LoadedValuesAreCleanAfterHydrationAndSubsequentEditIsDetected()
    {
        var f = Fixture(true); var id = Guid.NewGuid();
        f.Provider.Setup(p => p.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon { Id = id, Name = "loaded", IsPublic = true }));
        var result = await f.Manager.LoadHolonAsync(id, loadChildren:false, providerType: ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.HasHolonChanged().Should().BeFalse();
        result.Result.IsPublic = false; result.Result.HasHolonChanged().Should().BeTrue();
    }

    [Fact]
    public async Task GraphSavePublishesChildAcknowledgementsAndPreservesCallerNavigation()
    {
        var f = Fixture(true); var child = new Holon { Name = "child" };
        var h = new Holon { Name = "root", Children = new List<IHolon> { child } };
        child.ParentHolon = h;
        var result = await f.Manager.SaveHolonAsync(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message + " " + result.Exception);
        h.Id.Should().NotBeEmpty(); child.Id.Should().NotBeEmpty(); child.ParentHolonId.Should().Be(h.Id);
        child.ParentHolon.Should().BeSameAs(h); h.Children.Single().Should().BeSameAs(child);
        h.HasHolonChanged().Should().BeFalse();
    }

    [Fact]
    public async Task RemovedChildrenAreNotAcceptedByScalarOnlySave()
    {
        var f = Fixture(true); var child = new Holon { Id = Guid.NewGuid() };
        var h = new Holon { Id = Guid.NewGuid(), Children = new List<IHolon> { child } }; h.AcceptChanges();
        h.Name = "changed scalar"; h.Children.Clear();
        f.Provider.Setup(p => p.SaveHolonAsync(It.IsAny<IHolon>(), false, true, 0, true, false))
            .ReturnsAsync((IHolon value, bool a, bool b, int c, bool d, bool e) => new OASISResult<IHolon>(value));
        var result = await f.Manager.SaveHolonAsync(h, Guid.NewGuid(), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message);
        h.HasHolonChanged(false).Should().BeFalse(); h.HasHolonChanged().Should().BeTrue();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UnselectedDescendantChangesDoNotCauseWrites(bool nonrecursive)
    {
        var f = Fixture(true); var grandchild = new Holon { Id = Guid.NewGuid() };
        var child = new Holon { Id = Guid.NewGuid(), Children = new List<IHolon> { grandchild } };
        var h = new Holon { Id = Guid.NewGuid(), Children = new List<IHolon> { child } }; h.AcceptChanges();
        grandchild.Name = "outside selected depth";
        var result = await f.Manager.SaveHolonAsync(h, Guid.NewGuid(), recursive: !nonrecursive,
            maxChildDepth: nonrecursive ? 0 : 1, providerType: ProviderType.SQLLiteDBOASIS);
        result.IsSaved.Should().BeFalse(); result.IsError.Should().BeFalse();
        f.Provider.Invocations.Should().NotContain(i => i.Method.Name.StartsWith("Save"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidGraphsFailBeforeProviderIoWithoutPublishingAllocatedIds(bool cycle)
    {
        var f = Fixture(true); var h = new Holon(); var child = new Holon();
        h.Children.Add(child);
        if (cycle) child.Children.Add(h); else child.ParentHolonId = Guid.NewGuid();
        var result = await f.Manager.SaveHolonAsync(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeTrue(); h.Id.Should().BeEmpty(); child.Id.Should().BeEmpty();
        f.Provider.Invocations.Should().NotContain(i => i.Method.Name.StartsWith("Save"));
    }

    [Fact]
    public async Task CyclicPersistentValuesFailBeforeIoAndReleaseTheOperationLease()
    {
        var f = Fixture(true); var h = new Holon(); h.MetaData["cycle"] = h.MetaData;
        var failed = await f.Manager.SaveHolonAsync(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        failed.IsError.Should().BeTrue(); h.Id.Should().BeEmpty();
        f.Provider.Invocations.Should().NotContain(i => i.Method.Name.StartsWith("Save"));
        h.MetaData.Clear();
        var retried = await f.Manager.SaveHolonAsync(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        retried.IsError.Should().BeFalse(retried.Message);
    }

    private sealed class UncertainSave : Exception, INativeMutationReconciliationRequired
    {
        public string OriginalTransactionId => "controlled-original-save";
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task LegacyFailoverPublishesTheActualAcknowledgementButNeverReplaysAnUncertainSave(bool synchronous, bool uncertain)
    {
        var f = Fixture(false); var originalFailure = uncertain ? (Exception)new UncertainSave() : new IOException("rejected before commit");
        var rejected = new OASISResult<IHolon> { IsError = true, Message = "primary rejected", Exception = originalFailure };
        f.Provider.Setup(p => p.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false)).Returns(rejected);
        f.Provider.Setup(p => p.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false)).ReturnsAsync(rejected);
        var secondary = new Mock<IOASISStorageProvider>(); secondary.SetupAllProperties();
        secondary.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.LocalFileOASIS);
        secondary.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        secondary.Object.ProviderName = "secondary"; secondary.Object.IsProviderActivated = true;
        foreach (var provider in new[] { f.Provider, secondary })
        {
            provider.Setup(p => p.ActivateProvider()).Returns(new OASISResult<bool>(true));
            provider.Setup(p => p.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
            provider.Setup(p => p.DeActivateProvider()).Returns(new OASISResult<bool>(true));
            provider.Setup(p => p.DeActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        }
        secondary.Setup(p => p.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Returns((IHolon value, bool a, bool b, int c, bool d, bool e) => new OASISResult<IHolon>(value));
        secondary.Setup(p => p.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon value, bool a, bool b, int c, bool d, bool e) => new OASISResult<IHolon>(value));
        f.Providers.RegisterProvider(secondary.Object);
        f.Providers.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS) }).IsError.Should().BeFalse();
        f.Providers.IsAutoFailOverEnabled = true;
        var h = new Holon { Name = "one logical save" };
        var result = synchronous ? f.Manager.SaveHolon(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS)
            : await f.Manager.SaveHolonAsync(h, Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().Be(uncertain, result.Message);
        if (uncertain)
        {
            h.Id.Should().BeEmpty(); h.Version.Should().Be(0); result.Exception.Should().BeSameAs(originalFailure);
            secondary.Invocations.Should().NotContain(call => call.Method.Name.StartsWith("Save"));
        }
        else { h.Id.Should().NotBeEmpty(); h.Version.Should().Be(1); h.HasHolonChanged().Should().BeFalse(); }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task V2VersionExhaustionFailsBeforeProviderIoAndPreservesCallerState(bool synchronous)
    {
        var f=Fixture(true);var root=new Holon{Id=Guid.NewGuid(),Version=int.MaxValue,VersionId=Guid.NewGuid(),Name="pending"};
        root.AcceptChanges();root.Name="edited";
        var result=synchronous ? f.Manager.SaveHolon(root,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS)
            : await f.Manager.SaveHolonAsync(root,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeTrue();root.Version.Should().Be(int.MaxValue);root.Name.Should().Be("edited");
        root.HasHolonChanged().Should().BeTrue();f.Provider.Invocations.Should().NotContain(call=>call.Method.Name.StartsWith("Save"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task V2DeepPreparationAuditsEachSelectedNodeOnceAndStripsOnlyStagedNavigation(bool synchronous)
    {
        var f=Fixture(true);var actor=Guid.NewGuid();
        var rows=Enumerable.Range(0,2048).Select(index=>new Holon{Name="deep-public-"+index}).ToArray();
        for(var index=1;index<rows.Length;index++){rows[index-1].Children.Add(rows[index]);rows[index].ParentHolon=rows[index-1];}
        var result=synchronous ? f.Manager.SaveHolon(rows[0],actor,providerType:ProviderType.SQLLiteDBOASIS)
            : await f.Manager.SaveHolonAsync(rows[0],actor,providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        foreach(var row in rows){row.Version.Should().Be(1);row.CreatedByAvatarId.Should().Be(actor);row.VersionId.Should().NotBeEmpty();}
        for(var index=1;index<rows.Length;index++)
        {rows[index].ParentHolon.Should().BeSameAs(rows[index-1]);rows[index].ParentHolonId.Should().Be(rows[index-1].Id);}
        rows[0].HasHolonChanged().Should().BeFalse();
        var invocation=f.Provider.Invocations.Single(call=>call.Method.Name== (synchronous ? "SaveHolon":"SaveHolonAsync"));
        var staged=(IHolon)invocation.Arguments[0];
        for(var index=0;index<rows.Length;index++)
        {staged.ParentHolon.Should().BeNull();if(index+1<rows.Length)staged=staged.Children.Single();}
    }

    public class PreparationObservation : Holon
    {
        [JsonIgnore,CustomOASISProperty] public int ParentVersionDuringPreparation=>ParentHolon?.Version ?? -1;
        [JsonIgnore,CustomOASISProperty] public bool ParentHasPlaintextDuringPreparation=>ParentHolon?.MetaData.ContainsKey("private-parent-note")==true;
    }
    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task SavePublicationUsesSubmittedEncryptionContext(bool synchronous,bool useOverride)
    {
        var f=Fixture(true);
        f.Manager.OASISDNA.OASIS.Security=new SecuritySettings{HolonDataEncryption=new EncryptionSettings{
            Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('g',32)}};
        var settings=new EncryptionSettings{Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('o',32)};
        if(!useOverride)settings=f.Manager.OASISDNA.OASIS.Security.HolonDataEncryption;
        var source=new Holon{DataEncryptionOverride=useOverride ? settings:null};source.MetaData["private-note"]="submitted";
        OASISResult<IHolon> Acknowledge(IHolon value)
        {
            value.MetaData.Should().ContainKey("__oasis_enc__");
            settings.Rijndael256Key=new string('x',32);
            return new OASISResult<IHolon>(value);
        }
        f.Provider.Setup(provider=>provider.SaveHolon(It.IsAny<IHolon>(),true,true,0,true,false))
            .Returns((IHolon value,bool a,bool b,int c,bool d,bool e)=>Acknowledge(value));
        f.Provider.Setup(provider=>provider.SaveHolonAsync(It.IsAny<IHolon>(),true,true,0,true,false))
            .ReturnsAsync((IHolon value,bool a,bool b,int c,bool d,bool e)=>Acknowledge(value));
        var result=synchronous ? f.Manager.SaveHolon(source,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS)
            :await f.Manager.SaveHolonAsync(source,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        source.MetaData["private-note"].ToString().Should().Be("submitted");
        source.MetaData.Should().NotContainKey("__oasis_enc__");
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task V2PreparationKeepsParentContextUntilChildrenFinishThenEncrypts(bool synchronous)
    {
        var f=Fixture(true);
        f.Manager.OASISDNA.OASIS.Security=new SecuritySettings{HolonDataEncryption=new EncryptionSettings{
            Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('p',32)}};
        var child=new PreparationObservation{Name="observed-child"};
        var root=new Holon{Name="observed-parent",Children=new List<IHolon>{child}};child.ParentHolon=root;
        root.MetaData["private-parent-note"]="public-unit-test-value";
        OASISResult<IHolon> Inspect(IHolon value)
        {
            value.ParentHolon.Should().BeNull();value.Children.Single().ParentHolon.Should().BeNull();
            value.MetaData.Should().ContainKey("__oasis_enc__").And.NotContainKey("private-parent-note");
            var observed=HolonChangeTracking.Clone(value.Children.Single());
            f.Manager.DecryptHolonMetaData(observed).IsError.Should().BeFalse();
            observed.MetaData[nameof(PreparationObservation.ParentVersionDuringPreparation)].ToString().Should().Be("1");
            observed.MetaData[nameof(PreparationObservation.ParentHasPlaintextDuringPreparation)].ToString()!.ToLowerInvariant().Should().Be("true");
            return new OASISResult<IHolon>(value);
        }
        f.Provider.Setup(provider=>provider.SaveHolon(It.IsAny<IHolon>(),true,true,0,true,false))
            .Returns((IHolon value,bool a,bool b,int c,bool d,bool e)=>Inspect(value));
        f.Provider.Setup(provider=>provider.SaveHolonAsync(It.IsAny<IHolon>(),true,true,0,true,false))
            .ReturnsAsync((IHolon value,bool a,bool b,int c,bool d,bool e)=>Inspect(value));
        var result=synchronous ? f.Manager.SaveHolon(root,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS)
            : await f.Manager.SaveHolonAsync(root,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        root.MetaData["private-parent-note"].ToString().Should().Be("public-unit-test-value");child.ParentHolon.Should().BeSameAs(root);
    }

    private static (HolonManager Manager, Mock<IOASISStorageProvider> Provider, ProviderManager Providers) Fixture(bool v2, bool reject = false)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode = v2 ? HyperDriveModes.V2 : HyperDriveModes.Legacy,
            StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
        var p = new Mock<IOASISStorageProvider>(); p.SetupAllProperties();
        p.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        p.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        p.Object.ProviderName = "cosmic-controlled-store"; p.Object.IsProviderActivated = true;
        p.Setup(v => v.ActivateProvider()).Returns(new OASISResult<bool>(true));
        OASISResult<IHolon> Single(IHolon value) => reject
            ? new OASISResult<IHolon> { IsError = true, ErrorCode = "CONTROLLED_REJECTION", Message = "rejected" }
            : new OASISResult<IHolon>(value);
        OASISResult<IEnumerable<IHolon>> Batch(IEnumerable<IHolon> values) => reject
            ? new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = "CONTROLLED_REJECTION", Message = "rejected" }
            : new OASISResult<IEnumerable<IHolon>>(values.ToList());
        p.Setup(v => v.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Returns((IHolon h, bool a, bool b, int c, bool d, bool e) => Single(h));
        p.Setup(v => v.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon h, bool a, bool b, int c, bool d, bool e) => Single(h));
        p.Setup(v => v.SaveHolons(It.IsAny<IEnumerable<IHolon>>(), true, true, 0, 0, true, false))
            .Returns((IEnumerable<IHolon> h, bool a, bool b, int c, int d, bool e, bool g) => Batch(h));
        p.Setup(v => v.SaveHolonsAsync(It.IsAny<IEnumerable<IHolon>>(), true, true, 0, 0, true, false))
            .ReturnsAsync((IEnumerable<IHolon> h, bool a, bool b, int c, int d, bool e, bool g) => Batch(h));
        var providers = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        providers.RegisterProvider(p.Object);
        providers.SetAndActivateCurrentStorageProvider(p.Object).IsError.Should().BeFalse();
        return (new HolonManager(null, dna, providers), p, providers);
    }
}
