using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeExportUsesCurrentIdentityAndIndependentHolonVersions(bool sync,bool email)
    {
        var owner=Guid.NewGuid();var own=Guid.NewGuid();var foreign=Guid.NewGuid();var name="owned-export-"+owner.ToString("N");
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_COSMOSDB_WORK")!,$"export-independent-{sync}-{email}.pending.json");Assert.IsFalse(File.Exists(journal));
        var rows=new[]{(Collection:"avatarItems",Id:owner),(Collection:"avatarDetailItems",Id:owner),(Collection:"holonItems",Id:own),(Collection:"holonItems",Id:foreign)};
        async Task<(string Json,string Etag)> Read(string collection,Guid id){
            using var response=await _client.GetContainer(DatabaseName,collection).ReadItemStreamAsync(id.ToString(),new PartitionKey(id.ToString()));response.EnsureSuccessStatusCode();
            using var reader=new StreamReader(response.Content);return(await reader.ReadToEndAsync(),response.Headers.ETag);
        }
        foreach(var row in rows){using var absent=await _client.GetContainer(DatabaseName,row.Collection).ReadItemStreamAsync(row.Id.ToString(),new PartitionKey(row.Id.ToString()));Assert.AreEqual(HttpStatusCode.NotFound,absent.StatusCode);}
        using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{owner,own,foreign,name,sync,email,includesDetail=true,state="before-owned-independent-export-read-counts"}));file.Flush(true);
        }
        _provider.IsVersionControlEnabled=true;
        var avatar=new Avatar{Id=owner,Username=name,Email=name+"@example.invalid",IsActive=true,MetaData=new(){{"Latitude","10"},{"Longitude","20"}}};
        Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);avatar.Username=name+"-current";avatar.Email=avatar.Username+"@example.invalid";
        Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);Assert.AreEqual(2,avatar.Version);
        var detail=new AvatarDetail{Id=owner,Username=avatar.Username,Email=avatar.Email,IsActive=true};Assert.IsFalse((await _provider.SaveAvatarDetailAsync(detail)).IsError);
        var holon=new Holon{Id=own,CreatedByAvatarId=owner,IsActive=true,HolonType=HolonType.Park,MetaData=new(){{"owned_count","yes"},{"Latitude","10"},{"Longitude","20"}}};
        var other=new Holon{Id=foreign,ParentHolonId=own,CreatedByAvatarId=Guid.NewGuid(),IsActive=true};
        for(var version=1;version<=3;version++){
            holon.Name=name+"-v"+version;other.Name=name+"-foreign-v"+version;
            var saved=await _provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            Assert.IsFalse((await _provider.SaveHolonAsync(other,saveChildren:false)).IsError);
        }
        var originals=new List<(string Json,string Etag)>();foreach(var row in rows)originals.Add(await Read(row.Collection,row.Id));
        void Check<T>(OASISResult<T> result,int expected){Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(expected,result.LoadedCount);Assert.IsFalse(result.IsSaved);}
        Check(sync?_provider.LoadAvatar(owner):await _provider.LoadAvatarAsync(owner),1);
        Check(sync?_provider.LoadAvatarDetail(owner):await _provider.LoadAvatarDetailAsync(owner),1);
        var rootRead=sync?_provider.LoadHolon(own,loadChildrenFromProvider:true):await _provider.LoadHolonAsync(own,loadChildrenFromProvider:true);
        Check(rootRead,1);Assert.AreEqual(1,rootRead.Result.Children.Count);
        Check(sync?_provider.LoadHolon(own.ToString(),loadChildren:false):await _provider.LoadHolonAsync(own.ToString(),loadChildren:false),1);
        Check(sync?_provider.LoadAllAvatars():await _provider.LoadAllAvatarsAsync(),1);
        Check(sync?_provider.LoadAllAvatarDetails():await _provider.LoadAllAvatarDetailsAsync(),1);
        Check(sync?_provider.LoadAllHolons(loadChildrenFromProvider:true):await _provider.LoadAllHolonsAsync(loadChildrenFromProvider:true),2);
        Check(sync?_provider.LoadAvatarByUsername(avatar.Username):await _provider.LoadAvatarByUsernameAsync(avatar.Username),1);
        Check(sync?_provider.LoadAvatarByEmail(avatar.Email):await _provider.LoadAvatarByEmailAsync(avatar.Email),1);
        Check(sync?_provider.LoadAvatarDetailByUsername(avatar.Username):await _provider.LoadAvatarDetailByUsernameAsync(avatar.Username),1);
        Check(sync?_provider.LoadAvatarDetailByEmail(avatar.Email):await _provider.LoadAvatarDetailByEmailAsync(avatar.Email),1);
        Check(sync?_provider.LoadHolonsForParent(own,loadChildren:false):await _provider.LoadHolonsForParentAsync(own,loadChildren:false),1);
        Check(sync?_provider.LoadHolonsForParent(own.ToString(),loadChildren:false):await _provider.LoadHolonsForParentAsync(own.ToString(),loadChildren:false),1);
        Check(sync?_provider.LoadHolonsByMetaData("owned_count","yes",loadChildren:false):await _provider.LoadHolonsByMetaDataAsync("owned_count","yes",loadChildren:false),1);
        Check(sync?_provider.LoadHolonsByMetaData("owned_count","absent",loadChildren:false):await _provider.LoadHolonsByMetaDataAsync("owned_count","absent",loadChildren:false),0);
        Check(sync?_provider.LoadAllHolons(HolonType.Park,loadChildren:false):await _provider.LoadAllHolonsAsync(HolonType.Park,loadChildren:false),1);
        var search=new SearchParams{SearchOnlyForCurrentAvatar=false};
        Check(sync?_provider.Search(search,loadChildren:false):await _provider.SearchAsync(search,loadChildren:false),3);
        search.SearchGroups=new(){new SearchTextGroup{HolonType=HolonType.All,SearchHolons=true,SearchQuery=name+"-v3",HolonSearchParams=new SearchHolonParams{Name=true}}};
        Check(sync?_provider.Search(search,loadChildren:false):await _provider.SearchAsync(search,loadChildren:false),1);
        Check(_provider.GetAvatarsNearMe(10000000,20000000,100),1);Check(_provider.GetHolonsNearMe(10000000,20000000,100,HolonType.All),1);
        Check(_provider.GetHolonsNearMe(-90000000,0,100,HolonType.All),0);
        foreach(var version in new[]{0,1,2,3}){
            var exported=email
                ?sync?_provider.ExportAllDataForAvatarByEmail(avatar.Email,version):await _provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,version)
                :sync?_provider.ExportAllDataForAvatarByUsername(avatar.Username,version):await _provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,version);
            Assert.IsFalse(exported.IsError,exported.Exception?.ToString());Assert.IsTrue(exported.IsLoaded);Assert.AreEqual(1,exported.LoadedCount);
            var expected=version==0?3:version;Assert.AreEqual(own,exported.Result.Single().Id);Assert.AreEqual(expected,exported.Result.Single().Version);Assert.AreEqual(name+"-v"+expected,exported.Result.Single().Name);
        }
        var oldAvatar=email?await _provider.LoadAvatarByEmailAsync(name+"@example.invalid",1):await _provider.LoadAvatarByUsernameAsync(name,1);
        Assert.IsFalse(oldAvatar.IsError);Assert.AreEqual(owner,oldAvatar.Result.Id);Assert.AreEqual(1,oldAvatar.Result.Version);
        var oldExport=email?await _provider.ExportAllDataForAvatarByEmailAsync(name+"@example.invalid",1):await _provider.ExportAllDataForAvatarByUsernameAsync(name,1);
        Assert.IsTrue(oldExport.IsError);Assert.IsNull(oldExport.Result);
        for(var index=0;index<rows.Length;index++){
            var current=await Read(rows[index].Collection,rows[index].Id);Assert.AreEqual(originals[index].Etag,current.Etag);
            Assert.IsTrue(JToken.DeepEquals(JToken.Parse(originals[index].Json),JToken.Parse(current.Json)),"Export must not mutate original documents/history.");
        }
        for(var index=0;index<rows.Length;index++){
            var row=rows[index];var container=_client.GetContainer(DatabaseName,row.Collection);
            using(var removed=await container.DeleteItemStreamAsync(row.Id.ToString(),new PartitionKey(row.Id.ToString()),new ItemRequestOptions{IfMatchEtag=originals[index].Etag}))Assert.AreEqual(HttpStatusCode.NoContent,removed.StatusCode);
            using var absent=await container.ReadItemStreamAsync(row.Id.ToString(),new PartitionKey(row.Id.ToString()));Assert.AreEqual(HttpStatusCode.NotFound,absent.StatusCode);
        }
        File.Delete(journal);
    }
}
