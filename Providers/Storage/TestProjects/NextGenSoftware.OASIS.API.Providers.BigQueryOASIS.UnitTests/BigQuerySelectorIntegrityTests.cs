using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQuerySelectorIntegrityTests
{
    public static IEnumerable<object?[]> InvalidCases(){foreach(var route in Enumerable.Range(0,11))foreach(var sync in new[]{false,true})foreach(var value in route is >=3 and <=6?new string?[]{null,""," \t"}:new string?[]{null})yield return new object?[]{route,sync,value};}
    [DataTestMethod][DynamicData(nameof(InvalidCases),DynamicDataSourceType.Method)]
    public async Task InvalidIdentityRejectsBeforeNativeIo(int route,bool sync,string? value)
    {
        var called=false;var provider=BigQueryCollectionTests.Create(new(){Selected=Array.Empty<string>(),BeforeRead=()=>called=true});bool error;Exception? cause;
        if(route is 0 or 3 or 4){var r=route==0?(sync?provider.LoadAvatar(Guid.Empty):await provider.LoadAvatarAsync(Guid.Empty)):route==3?(sync?provider.LoadAvatarByUsername(value!):await provider.LoadAvatarByUsernameAsync(value!)):(sync?provider.LoadAvatarByEmail(value!):await provider.LoadAvatarByEmailAsync(value!));error=r.IsError;cause=r.Exception;Assert.IsFalse(r.IsLoaded);Assert.AreEqual(0,r.LoadedCount);}
        else if(route is 1 or 5 or 6){var r=route==1?(sync?provider.LoadAvatarDetail(Guid.Empty):await provider.LoadAvatarDetailAsync(Guid.Empty)):route==5?(sync?provider.LoadAvatarDetailByUsername(value!):await provider.LoadAvatarDetailByUsernameAsync(value!)):(sync?provider.LoadAvatarDetailByEmail(value!):await provider.LoadAvatarDetailByEmailAsync(value!));error=r.IsError;cause=r.Exception;Assert.IsFalse(r.IsLoaded);Assert.AreEqual(0,r.LoadedCount);}
        else if(route==2){var r=sync?provider.LoadHolon(Guid.Empty,loadChildren:false):await provider.LoadHolonAsync(Guid.Empty,loadChildren:false);error=r.IsError;cause=r.Exception;Assert.IsFalse(r.IsLoaded);Assert.AreEqual(0,r.LoadedCount);}
        else{var r=route<9?(sync?provider.DeleteAvatar(Guid.Empty,softDelete:route==7):await provider.DeleteAvatarAsync(Guid.Empty,softDelete:route==7)):(sync?provider.DeleteHolonSoft(Guid.Empty,softDelete:route==9):await provider.DeleteHolonSoftAsync(Guid.Empty,softDelete:route==9));error=r.IsError;cause=r.Exception;Assert.IsFalse(r.IsDeleted);Assert.AreEqual(0,r.DeletedCount);}
        Assert.IsTrue(error);Assert.IsInstanceOfType<ArgumentException>(cause);Assert.IsFalse(called);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task EmptyParentRemainsValidRootCollectionSelector(bool sync)
    {
        var root=new Holon{Id=Guid.NewGuid(),ParentHolonId=Guid.Empty};var provider=BigQueryCollectionTests.Create(new(){Selected=new[]{StoredRecordJson.Serialize(root)}});
        var result=sync?provider.LoadHolonsForParent(Guid.Empty,loadChildren:false):await provider.LoadHolonsForParentAsync(Guid.Empty,loadChildren:false);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);Assert.AreEqual(root.Id,result.Result.Single().Id);
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)][DataRow(false,3)][DataRow(true,3)]
    public async Task EmptyCanonicalIdentityCannotBecomeLoadedCollectionMember(bool sync,int route)
    {
        var parent=Guid.NewGuid();IHolonBase source=route==0?new Avatar():route==1?new AvatarDetail():new Holon{ParentHolonId=parent};var provider=BigQueryCollectionTests.Create(new(){Selected=new[]{StoredRecordJson.Serialize(source)}});bool error,loaded;int count;object? value;Exception? cause;
        if(route==0){var r=sync?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync();error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;value=r.Result;cause=r.Exception;}
        else if(route==1){var r=sync?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync();error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;value=r.Result;cause=r.Exception;}
        else{var r=route==2?(sync?provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false)):(sync?provider.LoadHolonsForParent(parent,loadChildren:false):await provider.LoadHolonsForParentAsync(parent,loadChildren:false));error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;value=r.Result;cause=r.Exception;}
        Assert.IsTrue(error);Assert.IsFalse(loaded);Assert.AreEqual(0,count);Assert.IsNull(value);Assert.IsInstanceOfType<InvalidDataException>(cause);
    }
    public static IEnumerable<object[]> Cases(){foreach(var route in Enumerable.Range(0,8))foreach(var sync in new[]{false,true})foreach(var corrupt in new[]{false,true})yield return new object[]{route,sync,corrupt};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task NativeSelectorCannotReturnMisboundCanonicalRecord(int route,bool sync,bool corrupt)
    {
        var id=Guid.NewGuid();var parent=Guid.NewGuid();var actualId=corrupt&&route<3?Guid.NewGuid():id;
        var username=corrupt&&route is 3 or 5?"other-user":"requested-user";var email=corrupt&&route is 4 or 6?"other@example.invalid":"requested@example.invalid";
        IHolonBase source=route is 0 or 3 or 4?new Avatar{Id=actualId,Username=username,Email=email}:route is 1 or 5 or 6?new AvatarDetail{Id=actualId,Username=username,Email=email}:new Holon{Id=actualId,ParentHolonId=corrupt&&route==7?Guid.NewGuid():parent};
        var raw=StoredRecordJson.Serialize(source);var client=new BigQueryCollectionTests.Rows{Selected=new[]{raw}};var provider=BigQueryCollectionTests.Create(client);bool error,loaded;int count;object? value;Exception? cause;
        if(route is 0 or 3 or 4){var r=route==0?(sync?provider.LoadAvatar(id):await provider.LoadAvatarAsync(id)):route==3?(sync?provider.LoadAvatarByUsername("requested-user"):await provider.LoadAvatarByUsernameAsync("requested-user")):(sync?provider.LoadAvatarByEmail("requested@example.invalid"):await provider.LoadAvatarByEmailAsync("requested@example.invalid"));error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;value=r.Result;cause=r.Exception;}
        else if(route is 1 or 5 or 6){var r=route==1?(sync?provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id)):route==5?(sync?provider.LoadAvatarDetailByUsername("requested-user"):await provider.LoadAvatarDetailByUsernameAsync("requested-user")):(sync?provider.LoadAvatarDetailByEmail("requested@example.invalid"):await provider.LoadAvatarDetailByEmailAsync("requested@example.invalid"));error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;value=r.Result;cause=r.Exception;}
        else if(route==2){var r=sync?provider.LoadHolon(id,loadChildren:false):await provider.LoadHolonAsync(id,loadChildren:false);error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;value=r.Result;cause=r.Exception;}
        else{var r=sync?provider.LoadHolonsForParent(parent,loadChildren:false):await provider.LoadHolonsForParentAsync(parent,loadChildren:false);error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;value=r.Result;cause=r.Exception;}
        Assert.AreEqual(corrupt,error);Assert.AreEqual(!corrupt,loaded);Assert.AreEqual(corrupt?0:1,count);if(corrupt){Assert.IsNull(value);Assert.IsInstanceOfType<InvalidDataException>(cause);}else Assert.IsNotNull(value);Assert.AreEqual(raw,client.Selected!.Single());Assert.IsNull(client.Stored);
    }
}
