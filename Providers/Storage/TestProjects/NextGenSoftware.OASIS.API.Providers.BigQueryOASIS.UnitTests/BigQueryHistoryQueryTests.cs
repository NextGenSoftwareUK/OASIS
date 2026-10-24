using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQueryHistoryQueryTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task AvatarSearchMatchesSelectedHistoricalIdentity(bool sync,bool historical)
    {
        var provider=Provider(new());var avatar=new Avatar{Username="old-name",Email="owned@example.invalid"};var first=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(first.IsError,first.Exception?.ToString());avatar.Username="new-name";var second=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(second.IsError,second.Exception?.ToString());
        var query=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new(){new SearchTextGroup{SearchAvatars=true,HolonType=HolonType.All,SearchQuery="old-name"}}};var version=historical?1:0;
        var result=sync?provider.Search(query,loadChildren:false,version:version):await provider.SearchAsync(query,loadChildren:false,version:version);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(historical?1:0,result.LoadedCount);Assert.AreEqual(historical?1:0,result.Result.SearchResultAvatars.Count);Assert.AreEqual(0,result.Result.SearchResultHolons.Count);if(historical)Assert.AreEqual(1,result.Result.SearchResultAvatars.Single().Version);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task HistoricalOwnerExportDoesNotDiscloseForeignChildMembership(bool sync,bool explicitMembership)
    {
        var provider=Provider(new());var owner=Guid.NewGuid();var child=new Holon{CreatedByAvatarId=Guid.NewGuid(),Description="foreign private"};var root=new Holon{CreatedByAvatarId=owner};
        if(explicitMembership){root.Children=new List<IHolon>{child};var saved=await provider.SaveHolonAsync(root);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
        else{var saved=await provider.SaveHolonAsync(root,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());child.ParentHolonId=root.Id;child.Version=8;var savedChild=await provider.SaveHolonAsync(child,saveChildren:false);Assert.IsFalse(savedChild.IsError,savedChild.Exception?.ToString());}
        var result=sync?provider.ExportAllDataForAvatarById(owner,1):await provider.ExportAllDataForAvatarByIdAsync(owner,1);
        Assert.AreEqual(explicitMembership,result.IsError);if(explicitMembership){Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);}else{Assert.AreEqual(1,result.LoadedCount);Assert.AreEqual(root.Id,result.Result.Single().Id);Assert.AreEqual(0,result.Result.Single().Children.Count);}
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task CorruptArchiveExportFailsWithoutPartialOrCurrentFallback(bool sync,bool owned)
    {
        var owner=Guid.NewGuid();var current=new Holon{Id=Guid.NewGuid(),Version=2,CreatedByAvatarId=owner};var foreign=new Holon{Id=Guid.NewGuid(),Version=1,CreatedByAvatarId=owner};var document=OasisJson.ParseStoredObject(StoredRecordJson.Serialize(current));document["id"]=current.Id.ToString();document[EmbeddedRecordHistory.PropertyName]=new Newtonsoft.Json.Linq.JObject{["schema"]=1,["records"]=new Newtonsoft.Json.Linq.JArray(OasisJson.ParseStoredObject(StoredRecordJson.Serialize(foreign)))};
        var client=new BigQueryGraphWriteTests.Ledger();client.Records[current.Id.ToString()]=document.ToString(Newtonsoft.Json.Formatting.None);var provider=Provider(client);var result=owned?(sync?provider.ExportAllDataForAvatarById(owner,-1):await provider.ExportAllDataForAvatarByIdAsync(owner,-1)):(sync?provider.ExportAll(-1):await provider.ExportAllAsync(-1));
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);Assert.AreEqual(0,client.Writes);
    }
    private static BigQueryOASIS Provider(BigQueryGraphWriteTests.Ledger client){var provider=new BigQueryOASIS("my-project"){IsVersionControlEnabled=true};typeof(BigQueryOASIS).GetField("_client",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(provider,client);return provider;}
    private static async Task<(BigQueryOASIS Provider,Avatar Avatar,Holon Root,Holon Child)> Seed()
    {
        var client=new BigQueryGraphWriteTests.Ledger();var provider=Provider(client);var avatar=new Avatar{Username="current-owner",Email="current@example.invalid",Version=5};var savedAvatar=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(savedAvatar.IsError,savedAvatar.Exception?.ToString());
        var root=new Holon{Description="first",CreatedByAvatarId=avatar.Id};var first=await provider.SaveHolonAsync(root,saveChildren:false);Assert.IsFalse(first.IsError,first.Exception?.ToString());
        var child=new Holon{Description="child",Version=8,ParentHolonId=root.Id,CreatedByAvatarId=avatar.Id};var childSaved=await provider.SaveHolonAsync(child,saveChildren:false);Assert.IsFalse(childSaved.IsError,childSaved.Exception?.ToString());
        root.Description="second";var second=await provider.SaveHolonAsync(root,saveChildren:false);Assert.IsFalse(second.IsError,second.Exception?.ToString());var deleted=await provider.DeleteHolonSoftAsync(root.Id);Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());
        return(provider,avatar,root,child);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task HistoricalSearchSelectsRootAndIndependentCurrentChildren(bool sync,bool children)
    {
        var data=await Seed();var query=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new(){new SearchTextGroup{SearchHolons=true,HolonType=HolonType.All,SearchQuery="first"}}};
        var result=sync?data.Provider.Search(query,loadChildren:children,version:1):await data.Provider.SearchAsync(query,loadChildren:children,version:1);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);var root=result.Result.SearchResultHolons.Single();Assert.AreEqual(data.Root.Id,root.Id);Assert.AreEqual(1,root.Version);Assert.AreEqual(children?1:0,root.Children.Count);if(children){Assert.AreEqual(data.Child.Id,root.Children.Single().Id);Assert.AreEqual(8,root.Children.Single().Version);}
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task OwnerExportVersionIsIndependentOfCurrentAvatarVersion(bool sync,int route)
    {
        var data=await Seed();var p=data.Provider;var result=route==0?(sync?p.ExportAllDataForAvatarById(data.Avatar.Id,1):await p.ExportAllDataForAvatarByIdAsync(data.Avatar.Id,1)):route==1?(sync?p.ExportAllDataForAvatarByUsername(data.Avatar.Username,1):await p.ExportAllDataForAvatarByUsernameAsync(data.Avatar.Username,1)):(sync?p.ExportAllDataForAvatarByEmail(data.Avatar.Email,1):await p.ExportAllDataForAvatarByEmailAsync(data.Avatar.Email,1));
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);var root=result.Result.Single();Assert.AreEqual(data.Root.Id,root.Id);Assert.AreEqual(1,root.Version);Assert.AreEqual(8,root.Children.Single().Version);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task AllVersionExportIsFlatAndFiltersDeletedRevisions(bool sync,bool owned)
    {
        var data=await Seed();var result=owned?(sync?data.Provider.ExportAllDataForAvatarById(data.Avatar.Id,-1):await data.Provider.ExportAllDataForAvatarByIdAsync(data.Avatar.Id,-1)):(sync?data.Provider.ExportAll(-1):await data.Provider.ExportAllAsync(-1));
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(3,result.LoadedCount);CollectionAssert.AreEqual(new[]{1,2},result.Result.Where(row=>row.Id==data.Root.Id).Select(row=>row.Version).ToArray());Assert.IsTrue(result.Result.All(row=>!row.IsDeleted&&row.Children.Count==0));Assert.AreEqual(8,result.Result.Single(row=>row.Id==data.Child.Id).Version);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task AllVersionOwnerScopeUsesEachArchivedRevision(bool sync,bool secondOwner)
    {
        var provider=Provider(new());var first=Guid.NewGuid();var second=Guid.NewGuid();var source=new Holon{CreatedByAvatarId=first};var firstSave=await provider.SaveHolonAsync(source,saveChildren:false);Assert.IsFalse(firstSave.IsError,firstSave.Exception?.ToString());source.CreatedByAvatarId=second;var secondSave=await provider.SaveHolonAsync(source,saveChildren:false);Assert.IsFalse(secondSave.IsError,secondSave.Exception?.ToString());
        var result=sync?provider.ExportAllDataForAvatarById(secondOwner?second:first,-1):await provider.ExportAllDataForAvatarByIdAsync(secondOwner?second:first,-1);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,result.LoadedCount);Assert.AreEqual(secondOwner?2:1,result.Result.Single().Version);Assert.AreEqual(secondOwner?second:first,result.Result.Single().CreatedByAvatarId);
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)][DataRow(false,3)][DataRow(true,3)]
    public async Task ExportBelowMinusOneRejectsBeforeAnyNativeLookup(bool sync,int route)
    {
        var client=new BigQueryGraphWriteTests.Ledger();var provider=Provider(client);var id=Guid.NewGuid();var result=route==0?(sync?provider.ExportAll(-2):await provider.ExportAllAsync(-2)):route==1?(sync?provider.ExportAllDataForAvatarById(id,-2):await provider.ExportAllDataForAvatarByIdAsync(id,-2)):route==2?(sync?provider.ExportAllDataForAvatarByUsername("owner",-2):await provider.ExportAllDataForAvatarByUsernameAsync("owner",-2)):(sync?provider.ExportAllDataForAvatarByEmail("owner@example.invalid",-2):await provider.ExportAllDataForAvatarByEmailAsync("owner@example.invalid",-2));
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);Assert.IsNull(result.Result);Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Writes);
    }
}
