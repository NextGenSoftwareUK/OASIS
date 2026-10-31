using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQueryEntrypointTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task FullExportIncludesCurrentParentLinkedChildren(bool sync)
    {
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};var provider=BigQueryCollectionTests.Create(new(){Selected=new[]{root,child}.Select(StoredRecordJson.Serialize).ToArray()});
        var result=sync?provider.ExportAll():await provider.ExportAllAsync();Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(2,result.LoadedCount);Assert.AreEqual(child.Id,result.Result.First().Children.Single().Id);
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task ExportFailureKeepsOriginalCauseWithoutPartialData(bool sync,int route)
    {
        var cause=new IOException("Controlled export read failure.");var provider=BigQueryCollectionTests.Create(new(){BeforeRead=()=>throw cause});
        var result=route==0?(sync?provider.ExportAllDataForAvatarById(Guid.NewGuid()):await provider.ExportAllDataForAvatarByIdAsync(Guid.NewGuid())):route==1?(sync?provider.ExportAllDataForAvatarByUsername("owned"):await provider.ExportAllDataForAvatarByUsernameAsync("owned")):(sync?provider.ExportAllDataForAvatarByEmail("owned@example.invalid"):await provider.ExportAllDataForAvatarByEmailAsync("owned@example.invalid"));
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.AreSame(cause,result.Exception);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task EmptyExportOwnerRejectsBeforeNativeRead(bool sync)
    {
        var called=false;var provider=BigQueryCollectionTests.Create(new(){Selected=Array.Empty<string>(),BeforeRead=()=>called=true});var result=sync?provider.ExportAllDataForAvatarById(Guid.Empty):await provider.ExportAllDataForAvatarByIdAsync(Guid.Empty);
        Assert.IsTrue(result.IsError);Assert.IsFalse(called);Assert.IsInstanceOfType<ArgumentException>(result.Exception);
    }
    public static IEnumerable<object[]> AliasCases(){foreach(var avatar in new[]{false,true})foreach(var sync in new[]{false,true})foreach(var count in new[]{0,1,2})yield return new object[]{avatar,sync,count};}
    [DataTestMethod][DynamicData(nameof(AliasCases),DynamicDataSourceType.Method)]
    public async Task ProviderKeySelectionRequiresUniqueRecordAndLoadedCounts(bool avatar,bool sync,int count)
    {
        IHolonBase record=avatar?new Avatar{Id=Guid.NewGuid()}:new Holon{Id=Guid.NewGuid()};record.ProviderUniqueStorageKey=new(){{ProviderType.BigQueryOASIS,"owned-alias"}};
        var encoded=Enumerable.Repeat(StoredRecordJson.Serialize(record),count).ToArray();var provider=BigQueryCollectionTests.Create(new(){Selected=avatar?Array.Empty<string>():encoded,AvatarSelected=avatar?encoded:Array.Empty<string>()});
        bool error,loaded;int loadedCount;object? value;Exception? cause;
        if(avatar){var r=sync?provider.LoadAvatarByProviderKey("owned-alias"):await provider.LoadAvatarByProviderKeyAsync("owned-alias");error=r.IsError;loaded=r.IsLoaded;loadedCount=r.LoadedCount;value=r.Result;cause=r.Exception;}
        else{var r=sync?provider.LoadHolon("owned-alias",loadChildren:false):await provider.LoadHolonAsync("owned-alias",loadChildren:false);error=r.IsError;loaded=r.IsLoaded;loadedCount=r.LoadedCount;value=r.Result;cause=r.Exception;}
        Assert.AreEqual(count!=1,error);Assert.AreEqual(count==1,loaded);Assert.AreEqual(count==1?1:0,loadedCount);if(count==1)Assert.IsNotNull(value);else Assert.IsNull(value);if(count==2)Assert.IsInstanceOfType<InvalidDataException>(cause);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ProviderKeyReadPreservesOriginalSourceError(bool avatar,bool sync)
    {
        var cause=new IOException("Controlled alias read failure.");var provider=BigQueryCollectionTests.Create(new(){BeforeRead=()=>throw cause});
        Exception? actual;if(avatar){var r=sync?provider.LoadAvatarByProviderKey("owned-alias"):await provider.LoadAvatarByProviderKeyAsync("owned-alias");Assert.IsTrue(r.IsError);actual=r.Exception;}
        else{var r=sync?provider.LoadHolon("owned-alias",loadChildren:false):await provider.LoadHolonAsync("owned-alias",loadChildren:false);Assert.IsTrue(r.IsError);actual=r.Exception;}Assert.AreSame(cause,actual);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ProviderKeyPointHonorsGraphOptions(bool sync,bool recursive)
    {
        var root=new Holon{Id=Guid.NewGuid(),ProviderUniqueStorageKey=new(){{ProviderType.BigQueryOASIS,"owned-alias"}}};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};var grand=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id};
        var provider=BigQueryCollectionTests.Create(new(){Selected=new[]{root,child,grand}.Select(StoredRecordJson.Serialize).ToArray()});
        var result=sync?provider.LoadHolon("owned-alias",recursive:recursive,loadChildrenFromProvider:true):await provider.LoadHolonAsync("owned-alias",recursive:recursive,loadChildrenFromProvider:true);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);Assert.AreEqual(child.Id,result.Result.Children.Single().Id);Assert.AreEqual(recursive?1:0,result.Result.Children.Single().Children.Count);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task SearchProjectsOnlySelectedRootGraphs(bool sync,bool children)
    {
        var root=new Holon{Id=Guid.NewGuid(),Name="selected"};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};var unrelated=new Holon{Id=Guid.NewGuid(),Name="unrelated"};unrelated.ParentHolonId=unrelated.Id;
        var provider=BigQueryCollectionTests.Create(new(){Selected=new[]{root,child,unrelated}.Select(StoredRecordJson.Serialize).ToArray(),AvatarSelected=Array.Empty<string>()});
        var query=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new(){new SearchTextGroup{SearchHolons=true,HolonType=HolonType.All,SearchQuery="selected"}}};
        var result=sync?provider.Search(query,loadChildren:children):await provider.SearchAsync(query,loadChildren:children);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,result.LoadedCount);var selected=result.Result.SearchResultHolons.Single();Assert.AreEqual(root.Id,selected.Id);Assert.AreEqual(children?1:0,selected.Children.Count);if(children)Assert.AreEqual(child.Id,selected.Children.Single().Id);
    }
    public static IEnumerable<object[]> ExportCases(){foreach(var sync in new[]{false,true})foreach(var route in new[]{0,1,2})yield return new object[]{sync,route};}
    [DataTestMethod][DynamicData(nameof(ExportCases),DynamicDataSourceType.Method)]
    public async Task AvatarExportsProjectOnlyOwnedRootsAndOwnedChildren(bool sync,int route)
    {
        var avatar=new Avatar{Id=Guid.NewGuid(),Username="owned",Email="owned@example.invalid"};var root=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=avatar.Id};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=avatar.Id};var foreign=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=Guid.NewGuid()};
        var provider=BigQueryCollectionTests.Create(new(){Selected=new[]{root,child,foreign}.Select(StoredRecordJson.Serialize).ToArray(),AvatarSelected=new[]{StoredRecordJson.Serialize(avatar)}});
        var result=route==0?(sync?provider.ExportAllDataForAvatarById(avatar.Id):await provider.ExportAllDataForAvatarByIdAsync(avatar.Id)):route==1?(sync?provider.ExportAllDataForAvatarByUsername("owned"):await provider.ExportAllDataForAvatarByUsernameAsync("owned")):(sync?provider.ExportAllDataForAvatarByEmail("owned@example.invalid"):await provider.ExportAllDataForAvatarByEmailAsync("owned@example.invalid"));
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(2,result.LoadedCount);CollectionAssert.AreEqual(new[]{root.Id,child.Id},result.Result.Select(row=>row.Id).ToArray());Assert.AreEqual(child.Id,result.Result.First().Children.Single().Id);Assert.IsFalse(result.Result.First().Children.Any(row=>row.Id==foreign.Id));
    }
}
