using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests;

public partial class PostgreSqlNativeAcceptanceTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)]
    public async Task NativeNumberedHistoryPersistsAcrossProviderInstances(int kind,bool synchronous)
    {
        var connection=Environment.GetEnvironmentVariable("POSTGRESQLOASIS_CONNECTIONSTRING")!;
        var settings=new NpgsqlConnectionStringBuilder(connection);
        Assert.AreEqual("127.0.0.1",settings.Host);Assert.AreEqual(15432,settings.Port);Assert.AreEqual("oasis_provider_test",settings.Database);
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_POSTGRES_TEST_WORK")!,$"native-history-{kind}-{synchronous}.pending.json");Assert.IsFalse(File.Exists(journal));
        var id=Guid.NewGuid();var marker="owned-pg-history-"+id.ToString("N");
        var owner=Guid.NewGuid();var childId=Guid.NewGuid();
        var table=new[]{"oasis_avatars","oasis_avatar_details","oasis_holons"}[kind];
        IHolonBase source=kind==0?new Avatar{Id=id,Username=marker,Email=marker+"@example.invalid"}:kind==1?new AvatarDetail{Id=id,Username=marker,Email=marker+"@example.invalid"}:new Holon{Id=id};
        source.CreatedByAvatarId=owner;if(source is IHolon root)root.ParentHolonId=owner;
        void SetLabel(string label){if(source is IAvatar avatar)avatar.FirstName=label;else if(source is AvatarDetail detail)detail.FirstName=label;else source.Name=label;source.MetaData["phase"]=label;}
        string Label(IHolonBase record)=>record is IAvatar avatar?avatar.FirstName:record is AvatarDetail detail?detail.FirstName:record.Name;
        SetLabel("original");source.MetaData=new(){["owned-fixture"]=marker,["phase"]="original",["amount"]=0.1234567890123456789012345678m};
        var provider=new PostgreSQLOASIS(connection){IsVersionControlEnabled=true};
        var reopened=new PostgreSQLOASIS(connection){IsVersionControlEnabled=false};
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        await using var verifier=new NpgsqlConnection(connection);await verifier.OpenAsync();
        async Task<string?> Stored(){await using var read=new NpgsqlCommand($"SELECT data_json FROM {table} WHERE id=@id",verifier);read.Parameters.AddWithValue("id",id);return (string?)await read.ExecuteScalarAsync();}
        Assert.IsNull(await Stored());
        if(kind==2){await using var absent=new NpgsqlCommand("SELECT count(*) FROM oasis_holons WHERE id=@id",verifier);absent.Parameters.AddWithValue("id",childId);Assert.AreEqual(0L,await absent.ExecuteScalarAsync());}
        using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,childId,table,marker,state="before-native-history"}));file.Flush(true);}
        string? observed=null;string? observedChild=null;var childAttempted=false;
        async Task Save(PostgreSQLOASIS owner){
            if(kind==0){var saved=synchronous?owner.SaveAvatar((IAvatar)source):await owner.SaveAvatarAsync((IAvatar)source);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);Assert.AreEqual(1,saved.SavedCount);}
            else if(kind==1){var saved=synchronous?owner.SaveAvatarDetail((IAvatarDetail)source):await owner.SaveAvatarDetailAsync((IAvatarDetail)source);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);Assert.AreEqual(1,saved.SavedCount);}
            else{var saved=synchronous?owner.SaveHolon((IHolon)source,saveChildren:false):await owner.SaveHolonAsync((IHolon)source,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);Assert.AreEqual(1,saved.SavedCount);}
            var body=await Stored();Assert.IsNotNull(body);using var json=JsonDocument.Parse(body);
            Assert.AreEqual(id,json.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(marker,json.RootElement.GetProperty("MetaData").GetProperty("owned-fixture").GetString());observed=body;
        }
        async Task<IHolonBase> Read(int version){
            if(kind==0){var result=synchronous?reopened.LoadAvatar(id,version):await reopened.LoadAvatarAsync(id,version);Assert.IsFalse(result.IsError,result.Exception?.ToString());return result.Result;}
            if(kind==1){var result=synchronous?reopened.LoadAvatarDetail(id,version):await reopened.LoadAvatarDetailAsync(id,version);Assert.IsFalse(result.IsError,result.Exception?.ToString());return result.Result;}
            var holon=synchronous?reopened.LoadHolon(id,loadChildren:true,loadChildrenFromProvider:true,version:version):await reopened.LoadHolonAsync(id,loadChildren:true,loadChildrenFromProvider:true,version:version);Assert.IsFalse(holon.IsError,holon.Exception?.ToString());return holon.Result;
        }
        try{
            await Save(provider);Assert.AreEqual(1,source.Version);var version1=source.VersionId;Assert.AreNotEqual(Guid.Empty,version1);
            if(kind==2){
                var child=new Holon{Id=childId,ParentHolonId=id,Name="child-first",MetaData=new(){["owned-fixture"]=marker}};
                for(var index=0;index<2;index++){
                    childAttempted=true;child.Name=index==0?"child-first":"child-current";
                    var saved=synchronous?provider.SaveHolon(child,saveChildren:false):await provider.SaveHolonAsync(child,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                    await using var read=new NpgsqlCommand("SELECT data_json FROM oasis_holons WHERE id=@id",verifier);read.Parameters.AddWithValue("id",childId);
                    var body=(string?)await read.ExecuteScalarAsync();Assert.IsNotNull(body);using var json=JsonDocument.Parse(body);
                    Assert.AreEqual(childId,json.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(marker,json.RootElement.GetProperty("MetaData").GetProperty("owned-fixture").GetString());observedChild=body;
                }
            }
            SetLabel("second");await Save(provider);Assert.AreEqual(2,source.Version);Assert.AreEqual(version1,source.PreviousVersionId);
            Assert.AreEqual("original",Label(await Read(1)));Assert.AreEqual("second",Label(await Read(0)));
            // Turning off new history creation must not discard an existing archive.
            SetLabel("third");await Save(reopened);Assert.AreEqual(3,source.Version);
            Assert.AreEqual("original",Label(await Read(1)));Assert.AreEqual("second",Label(await Read(2)));Assert.AreEqual("third",Label(await Read(3)));
            if(kind==0){var selected=await reopened.LoadAllAvatarsAsync(1);Assert.IsFalse(selected.IsError,selected.Message);Assert.AreEqual("original",selected.Result.Single(row=>row.Id==id).FirstName);Assert.AreEqual("original",(await reopened.LoadAvatarByUsernameAsync(marker,1)).Result.FirstName);Assert.AreEqual("original",(await reopened.LoadAvatarByEmailAsync(marker+"@example.invalid",1)).Result.FirstName);}
            else if(kind==1){var selected=await reopened.LoadAllAvatarDetailsAsync(1);Assert.IsFalse(selected.IsError,selected.Message);Assert.AreEqual("original",((AvatarDetail)selected.Result.Single(row=>row.Id==id)).FirstName);Assert.AreEqual("original",((AvatarDetail)(await reopened.LoadAvatarDetailByUsernameAsync(marker,1)).Result).FirstName);Assert.AreEqual("original",((AvatarDetail)(await reopened.LoadAvatarDetailByEmailAsync(marker+"@example.invalid",1)).Result).FirstName);}
            else{
                void Check(IHolon root){Assert.AreEqual("original",root.Name);Assert.AreEqual(1,root.Version);var child=root.Children.Single();Assert.AreEqual(childId,child.Id);Assert.AreEqual(2,child.Version);Assert.AreEqual("child-current",child.Name);}
                Check((IHolon)await Read(1));
                var all=await reopened.LoadAllHolonsAsync(loadChildrenFromProvider:true,version:1);Assert.IsFalse(all.IsError,all.Message);Check(all.Result.Single(row=>row.Id==id));
                var parent=await reopened.LoadHolonsForParentAsync(owner,loadChildrenFromProvider:true,version:1);Assert.IsFalse(parent.IsError,parent.Message);Check(parent.Result.Single(row=>row.Id==id));
                var metadata=await reopened.LoadHolonsByMetaDataAsync("phase","original",loadChildrenFromProvider:true,version:1);Assert.IsFalse(metadata.IsError,metadata.Message);Check(metadata.Result.Single(row=>row.Id==id));
                var pairs=await reopened.LoadHolonsByMetaDataAsync(new Dictionary<string,string>{{"phase","original"}},MetaKeyValuePairMatchMode.All,loadChildrenFromProvider:true,version:1);Assert.IsFalse(pairs.IsError,pairs.Message);Check(pairs.Result.Single(row=>row.Id==id));
                var search=await reopened.SearchAsync(new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new(){new SearchTextGroup{SearchQuery="original",SearchHolons=true}}},version:1);Assert.IsFalse(search.IsError,search.Message);Check(search.Result.SearchResultHolons.Single(row=>row.Id==id));
                var export=await reopened.ExportAllAsync(1);Assert.IsFalse(export.IsError,export.Message);Check(export.Result.Single(row=>row.Id==id));
                var owned=await reopened.ExportAllDataForAvatarByIdAsync(owner,1);Assert.IsFalse(owned.IsError,owned.Message);Check(owned.Result.Single(row=>row.Id==id));
            }
            using var native=JsonDocument.Parse(observed!);Assert.AreEqual(2,native.RootElement.GetProperty("_oasis_record_history").GetProperty("records").GetArrayLength());
        }finally{
            if(observedChild!=null){await using var cleanup=new NpgsqlCommand("DELETE FROM oasis_holons WHERE id=@id AND data_json=@observed",verifier);cleanup.Parameters.AddWithValue("id",childId);cleanup.Parameters.AddWithValue("observed",observedChild);Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}
            if(observed!=null){await using var cleanup=new NpgsqlCommand($"DELETE FROM {table} WHERE id=@id AND data_json=@observed",verifier);cleanup.Parameters.AddWithValue("id",id);cleanup.Parameters.AddWithValue("observed",observed);Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Stored());if(!childAttempted||observedChild!=null)File.Delete(journal);}
            await provider.DeActivateProviderAsync();await reopened.DeActivateProviderAsync();
        }
    }
}
