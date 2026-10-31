using Google.Cloud.BigQuery.V2;
using Google.Apis.Bigquery.v2.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQueryVersionReadTests
{
    internal sealed class Client(string stored,string[]? children=null):BigQueryClient
    {
        public int Calls;
        public override Task<BigQueryResults> ExecuteQueryAsync(string sql,IEnumerable<BigQueryParameter> parameters,QueryOptions? queryOptions=null,GetQueryResultsOptions? resultsOptions=null,CancellationToken token=default)
        {
            Calls++;var args=parameters?.ToDictionary(row=>row.Name,row=>row.Value)??new();var current=OasisJson.ParseStoredObject(stored);var selected=true;
            if(args.TryGetValue("value",out var sought)){
                var property=sql.Contains("WHERE username =",StringComparison.Ordinal)?"Username":sql.Contains("WHERE email =",StringComparison.Ordinal)?"Email":"ParentHolonId";
                selected=current[property]?.Value<string>()==sought?.ToString();
            }
            var values=selected?(args.ContainsKey("id")?new[]{stored}:new[]{stored}.Concat(children??Array.Empty<string>()).ToArray()):Array.Empty<string>();
            return Task.FromResult(new BigQueryResults(this,new GetQueryResultsResponse{JobComplete=true,JobReference=new(){ProjectId="my-project",JobId="owned-version-read"},TotalRows=(ulong)values.Length,Schema=new(){Fields=new List<TableFieldSchema>{new(){Name="doc",Type="STRING"}}},Rows=values.Select(value=>new TableRow{F=new List<TableCell>{new(){V=value}}}).ToList()},null,new GetQueryResultsOptions()));
        }
    }
    private static string History(IHolonBase old,IHolonBase current)
    {
        var json=OasisJson.ParseStoredObject(StoredRecordJson.Serialize(current));json["id"]=current.Id.ToString();json[EmbeddedRecordHistory.PropertyName]=new JObject{["schema"]=1,["records"]=new JArray(OasisJson.ParseStoredObject(StoredRecordJson.Serialize(old)))};return json.ToString(Newtonsoft.Json.Formatting.None);
    }
    private static BigQueryOASIS Provider(Client client){var provider=new BigQueryOASIS("my-project");typeof(BigQueryOASIS).GetField("_client",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(provider,client);return provider;}
    public static IEnumerable<object[]> Cases(){foreach(var route in Enumerable.Range(0,7))foreach(var sync in new[]{false,true})foreach(var version in new[]{-1,0,1,2,3})yield return new object[]{route,sync,version};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task PointReadSelectsExactCanonicalRootVersion(int route,bool sync,int version)
    {
        var id=Guid.NewGuid();IHolonBase old=route<3?new Avatar{Id=id,Username="old-name",Email="old@example.invalid"}:route<6?new AvatarDetail{Id=id,Username="old-name",Email="old@example.invalid"}:new Holon{Id=id};old.Version=1;old.Description="old";
        var current=StoredRecordJson.Project<Holon>(new Holon{Id=id});IHolonBase latest=route<3?new Avatar{Id=id,Username="new-name",Email="new@example.invalid"}:route<6?new AvatarDetail{Id=id,Username="new-name",Email="new@example.invalid"}:current;latest.Version=2;latest.Description="new";
        var client=new Client(History(old,latest));var provider=Provider(client);bool error,loaded;int count;IHolonBase? value;
        var username=version==1?"old-name":"new-name";var email=version==1?"old@example.invalid":"new@example.invalid";
        if(route<3){var r=route==0?(sync?provider.LoadAvatar(id,version):await provider.LoadAvatarAsync(id,version)):route==1?(sync?provider.LoadAvatarByUsername(username,version):await provider.LoadAvatarByUsernameAsync(username,version)):(sync?provider.LoadAvatarByEmail(email,version):await provider.LoadAvatarByEmailAsync(email,version));error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;value=r.Result;}
        else if(route<6){var r=route==3?(sync?provider.LoadAvatarDetail(id,version):await provider.LoadAvatarDetailAsync(id,version)):route==4?(sync?provider.LoadAvatarDetailByUsername(username,version):await provider.LoadAvatarDetailByUsernameAsync(username,version)):(sync?provider.LoadAvatarDetailByEmail(email,version):await provider.LoadAvatarDetailByEmailAsync(email,version));error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;value=r.Result;}
        else{var r=sync?provider.LoadHolon(id,loadChildren:false,version:version):await provider.LoadHolonAsync(id,loadChildren:false,version:version);error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;value=r.Result;}
        var absent=version==3||version<0;Assert.AreEqual(absent,error);Assert.AreEqual(!absent,loaded);Assert.AreEqual(absent?0:1,count);if(absent)Assert.IsNull(value);else{Assert.IsNotNull(value);Assert.AreEqual(version==1?1:2,value.Version);Assert.AreEqual(version==1?"old":"new",value.Description);}if(version<0)Assert.AreEqual(0,client.Calls);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task HistoricalParentAndTypeUseSelectedRootNotCurrentNativeIndex(bool sync,bool parent)
    {
        var id=Guid.NewGuid();var originalParent=Guid.NewGuid();var old=new Holon{Id=id,Version=1,HolonType=HolonType.Park,ParentHolonId=originalParent};var latest=new Holon{Id=id,Version=2,HolonType=HolonType.Quest,ParentHolonId=Guid.NewGuid()};var provider=Provider(new(History(old,latest)));
        var result=parent?(sync?provider.LoadHolonsForParent(originalParent,HolonType.Park,loadChildren:false,version:1):await provider.LoadHolonsForParentAsync(originalParent,HolonType.Park,loadChildren:false,version:1)):(sync?provider.LoadAllHolons(HolonType.Park,loadChildren:false,version:1):await provider.LoadAllHolonsAsync(HolonType.Park,loadChildren:false,version:1));
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);Assert.AreEqual(id,result.Result.Single().Id);Assert.AreEqual(1,result.Result.Single().Version);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task HistoricalRootKeepsIndependentCurrentChildVersion(bool sync)
    {
        var root=new Holon{Id=Guid.NewGuid(),Version=1};var latest=new Holon{Id=root.Id,Version=2};var child=new Holon{Id=Guid.NewGuid(),Version=8,ParentHolonId=root.Id};var provider=Provider(new(History(root,latest),new[]{StoredRecordJson.Serialize(child)}));
        var result=sync?provider.LoadHolon(root.Id,loadChildrenFromProvider:true,version:1):await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true,version:1);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,result.Result.Version);Assert.AreEqual(8,result.Result.Children.Single().Version);Assert.AreEqual(child.Id,result.Result.Children.Single().Id);
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task CorruptHistoryFailsWithoutCurrentRecordFallback(bool sync,int kind)
    {
        var old=new Holon{Id=Guid.NewGuid(),Version=1};var latest=new Holon{Id=old.Id,Version=2};var json=OasisJson.ParseStoredObject(History(old,latest));
        if(kind==0)((JArray)json[EmbeddedRecordHistory.PropertyName]!["records"]!).Add(OasisJson.ParseStoredObject(StoredRecordJson.Serialize(old)));
        if(kind==1)json[EmbeddedRecordHistory.PropertyName]!["records"]![0]!["Id"]=Guid.NewGuid().ToString();if(kind==2)json["id"]=Guid.NewGuid().ToString();
        var provider=Provider(new(json.ToString(Newtonsoft.Json.Formatting.None)));var result=sync?provider.LoadHolon(old.Id,loadChildren:false,version:1):await provider.LoadHolonAsync(old.Id,loadChildren:false,version:1);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,5)][DataRow(true,5)][DataRow(false,1)][DataRow(true,1)]
    public async Task CanonicalRecordWithoutArchiveOnlySatisfiesItsOwnVersion(bool sync,int version)
    {
        var source=new Holon{Id=Guid.NewGuid(),Version=5};var provider=Provider(new(StoredRecordJson.Serialize(source)));var result=sync?provider.LoadHolon(source.Id,loadChildren:false,version:version):await provider.LoadHolonAsync(source.Id,loadChildren:false,version:version);
        Assert.AreEqual(version==1,result.IsError);if(version==1)Assert.IsNull(result.Result);else{Assert.IsTrue(result.IsLoaded);Assert.AreEqual(5,result.Result.Version);}
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task AllCollectionsSelectHistoricalCanonicalRoot(bool sync,int kind)
    {
        var id=Guid.NewGuid();IHolonBase old=kind==0?new Avatar{Id=id}:kind==1?new AvatarDetail{Id=id}:new Holon{Id=id};IHolonBase current=kind==0?new Avatar{Id=id}:kind==1?new AvatarDetail{Id=id}:new Holon{Id=id};old.Version=1;current.Version=2;var provider=Provider(new(History(old,current)));
        IHolonBase selected;bool error,loaded;int count;
        if(kind==0){var r=sync?provider.LoadAllAvatars(1):await provider.LoadAllAvatarsAsync(1);error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;selected=r.Result.Single();}
        else if(kind==1){var r=sync?provider.LoadAllAvatarDetails(1):await provider.LoadAllAvatarDetailsAsync(1);error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;selected=r.Result.Single();}
        else{var r=sync?provider.LoadAllHolons(loadChildren:false,version:1):await provider.LoadAllHolonsAsync(loadChildren:false,version:1);error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;selected=r.Result.Single();}
        Assert.IsFalse(error);Assert.IsTrue(loaded);Assert.AreEqual(1,count);Assert.AreEqual(id,selected.Id);Assert.AreEqual(1,selected.Version);
    }
    public static IEnumerable<object[]> TombstoneCases(){foreach(var kind in new[]{0,1,2})foreach(var sync in new[]{false,true})foreach(var historical in new[]{false,true})yield return new object[]{kind,sync,historical};}
    [DataTestMethod][DynamicData(nameof(TombstoneCases),DynamicDataSourceType.Method)]
    public async Task DeletionVisibilityBelongsToSelectedVersion(int kind,bool sync,bool historical)
    {
        var id=Guid.NewGuid();IHolonBase old=kind==0?new Avatar{Id=id}:kind==1?new AvatarDetail{Id=id}:new Holon{Id=id};IHolonBase current=kind==0?new Avatar{Id=id}:kind==1?new AvatarDetail{Id=id}:new Holon{Id=id};old.Version=1;current.Version=2;current.DeletedDate=DateTime.UtcNow;var provider=Provider(new(History(old,current)));var version=historical?1:0;
        bool error,loaded;int count;IHolonBase? selected;
        if(kind==0){var r=sync?provider.LoadAvatar(id,version):await provider.LoadAvatarAsync(id,version);error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;selected=r.Result;}
        else if(kind==1){var r=sync?provider.LoadAvatarDetail(id,version):await provider.LoadAvatarDetailAsync(id,version);error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;selected=r.Result;}
        else{var r=sync?provider.LoadHolon(id,loadChildren:false,version:version):await provider.LoadHolonAsync(id,loadChildren:false,version:version);error=r.IsError;loaded=r.IsLoaded;count=r.LoadedCount;selected=r.Result;}
        Assert.AreEqual(!historical,error);Assert.AreEqual(historical,loaded);Assert.AreEqual(historical?1:0,count);if(historical){Assert.IsNotNull(selected);Assert.IsFalse(selected.IsDeleted);Assert.AreEqual(1,selected.Version);}else Assert.IsNull(selected);
    }
}
