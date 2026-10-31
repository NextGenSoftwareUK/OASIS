using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public sealed class CosmosReadCountTests
{
    private sealed class Fixture
    {
        internal readonly Avatar Avatar=new(){Id=Guid.NewGuid(),Username="owned",Email="owned@example.invalid",IsActive=true,MetaData=new(){{"Latitude","10"},{"Longitude","20"}}};
        internal readonly AvatarDetail Detail;
        internal readonly Holon Root=new(){Id=Guid.NewGuid(),ParentHolonId=Guid.NewGuid(),Name="oak",HolonType=HolonType.Park,IsActive=true,MetaData=new(){{"tag","hit"},{"Latitude","10"},{"Longitude","20"}}};
        internal readonly Mock<IAvatarRepository> Avatars=new(MockBehavior.Strict);
        internal readonly Mock<IAvatarDetailRepository> Details=new(MockBehavior.Strict);
        internal readonly Mock<IHolonRepository> Holons=new(MockBehavior.Strict);
        internal readonly AzureCosmosDBOASIS Provider=new(new Uri("https://owned.invalid"),"owned","owned",new()){IsProviderActivated=true};
        internal Fixture(bool empty=false)
        {
            Detail=new(){Id=Avatar.Id,Username=Avatar.Username,Email=Avatar.Email,IsActive=true};
            var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=Root.Id,Name="child",IsActive=true};
            var other=new Holon{Id=Guid.NewGuid(),Name="other",IsActive=true,MetaData=new(){{"Latitude","40"},{"Longitude","20"}}};
            Avatars.Setup(r=>r.GetByIdAsync(Avatar.Id.ToString())).ReturnsAsync(empty?null!:Avatar);
            Details.Setup(r=>r.GetByIdAsync(Avatar.Id.ToString())).ReturnsAsync(empty?null!:Detail);
            Holons.Setup(r=>r.GetByIdAsync(Root.Id.ToString())).ReturnsAsync(empty?null!:Root);
            Avatars.Setup(r=>r.GetListAsync(0)).ReturnsAsync(empty?new List<IAvatar>():new List<IAvatar>{Avatar,new Avatar{Id=Guid.NewGuid(),Username="other",IsActive=true}});
            Details.Setup(r=>r.GetListAsync(0)).ReturnsAsync(empty?new List<IAvatarDetail>():new List<IAvatarDetail>{Detail,new AvatarDetail{Id=Guid.NewGuid(),IsActive=false}});
            Holons.Setup(r=>r.GetListAsync(0)).ReturnsAsync(empty?new List<IHolon>():new List<IHolon>{Root,other,child});
            foreach(var pair in new[]{(Name:"avatarRepository",Value:(object)Avatars.Object),(Name:"avatarDetailRepository",Value:(object)Details.Object),(Name:"holonRepository",Value:(object)Holons.Object)})
                typeof(AzureCosmosDBOASIS).GetField(pair.Name,BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(Provider,pair.Value);
        }
    }
    public static IEnumerable<object[]> Cases(){foreach(var route in Enumerable.Range(0,20))foreach(var sync in route is 17 or 18?new[]{false}:new[]{false,true})foreach(var empty in new[]{false,true})yield return new object[]{route,sync,empty};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task CountsDescribeReturnedRootsNotChildrenOrScannedRecords(int route,bool sync,bool empty)
    {
        var fixture=new Fixture(empty);var p=fixture.Provider;var id=fixture.Avatar.Id;var root=fixture.Root;
        void Check<T>(OASISResult<T> result,int expected,bool point=false){
            Assert.AreEqual(empty&&point,result.IsError,result.Message);Assert.AreEqual(!(empty&&point),result.IsLoaded);
            Assert.AreEqual(empty?0:expected,result.LoadedCount);Assert.IsFalse(result.IsSaved);
            if(empty&&point)Assert.IsNull(result.Result);else Assert.IsNotNull(result.Result);
        }
        if(route==0)Check(sync?p.LoadAvatar(id):await p.LoadAvatarAsync(id),1,true);
        else if(route==1)Check(sync?p.LoadAvatarDetail(id):await p.LoadAvatarDetailAsync(id),1,true);
        else if(route==2)Check(sync?p.LoadHolon(root.Id,loadChildrenFromProvider:true):await p.LoadHolonAsync(root.Id,loadChildrenFromProvider:true),1,true);
        else if(route==3)Check(sync?p.LoadAllAvatars():await p.LoadAllAvatarsAsync(),2);
        else if(route==4)Check(sync?p.LoadAllAvatarDetails():await p.LoadAllAvatarDetailsAsync(),1);
        else if(route==5)Check(sync?p.LoadAllHolons(loadChildrenFromProvider:true):await p.LoadAllHolonsAsync(loadChildrenFromProvider:true),3);
        else if(route==6)Check(sync?p.LoadAvatarByUsername("owned"):await p.LoadAvatarByUsernameAsync("owned"),1,true);
        else if(route==7)Check(sync?p.LoadAvatarByEmail("owned@example.invalid"):await p.LoadAvatarByEmailAsync("owned@example.invalid"),1,true);
        else if(route==8)Check(sync?p.LoadAvatarDetailByUsername("owned"):await p.LoadAvatarDetailByUsernameAsync("owned"),1,true);
        else if(route==9)Check(sync?p.LoadAvatarDetailByEmail("owned@example.invalid"):await p.LoadAvatarDetailByEmailAsync("owned@example.invalid"),1,true);
        else if(route==10)Check(sync?p.LoadHolonsForParent(root.ParentHolonId,loadChildren:false):await p.LoadHolonsForParentAsync(root.ParentHolonId,loadChildren:false),1);
        else if(route==11)Check(sync?p.LoadHolonsForParent(root.ParentHolonId.ToString(),loadChildren:false):await p.LoadHolonsForParentAsync(root.ParentHolonId.ToString(),loadChildren:false),1);
        else if(route==12)Check(sync?p.LoadHolonsByMetaData("tag","hit",loadChildren:false):await p.LoadHolonsByMetaDataAsync("tag","hit",loadChildren:false),1);
        else if(route==13)Check(sync?p.LoadHolonsByMetaData(new(){{"tag","hit"}},MetaKeyValuePairMatchMode.All,loadChildren:false):await p.LoadHolonsByMetaDataAsync(new(){{"tag","hit"}},MetaKeyValuePairMatchMode.All,loadChildren:false),1);
        else if(route==14)Check(sync?p.LoadAllHolons(HolonType.Park,loadChildren:false):await p.LoadAllHolonsAsync(HolonType.Park,loadChildren:false),1);
        else if(route is 15 or 16){
            var query=new SearchParams{SearchOnlyForCurrentAvatar=false};
            if(route==15)query.SearchGroups=new(){new SearchTextGroup{HolonType=HolonType.All,SearchHolons=true,SearchQuery="oak",HolonSearchParams=new SearchHolonParams{Name=true}}};
            var result=sync?p.Search(query,loadChildren:false):await p.SearchAsync(query,loadChildren:false);Check(result,route==15?1:5);
            Assert.AreEqual(result.Result.NumberOfResults,result.LoadedCount);
        }
        else if(route==17)Check(p.GetAvatarsNearMe(10000000,20000000,100),1);
        else if(route==18)Check(p.GetHolonsNearMe(10000000,20000000,100,HolonType.All),1);
        else Check(sync?p.LoadHolon(root.Id.ToString(),loadChildren:false):await p.LoadHolonAsync(root.Id.ToString(),loadChildren:false),1,true);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ProjectionFailureClearsEarlierPointReadCount(bool sync)
    {
        var fixture=new Fixture();fixture.Root.ChildRoleManifestJson="invalid-owned-manifest";
        var result=sync?fixture.Provider.LoadHolon(fixture.Root.Id,loadChildrenFromProvider:true):await fixture.Provider.LoadHolonAsync(fixture.Root.Id,loadChildrenFromProvider:true);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task DeleteDoesNotPublishInternalReadCount(bool sync,bool fail)
    {
        var fixture=new Fixture();var deletion=fixture.Holons.Setup(r=>r.DeleteIfUnchangedAsync(It.IsAny<IHolon>()));
        if(fail)deletion.ThrowsAsync(new IOException("owned delete rejection"));else deletion.Returns(Task.CompletedTask);
        var result=sync?fixture.Provider.DeleteHolon(fixture.Root.Id):await fixture.Provider.DeleteHolonAsync(fixture.Root.Id);
        Assert.AreEqual(fail,result.IsError);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
    }
}
