using System.Globalization;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PostgreSqlNativeAcceptanceTests
{
    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task NativeDuplicateProfileIdentityIsRejected(bool detail,bool email,bool synchronous)
    {
        var connection=Environment.GetEnvironmentVariable("POSTGRESQLOASIS_CONNECTIONSTRING")!;
        var settings=new NpgsqlConnectionStringBuilder(connection);
        Assert.AreEqual("127.0.0.1",settings.Host);Assert.AreEqual(15432,settings.Port);Assert.AreEqual("oasis_provider_test",settings.Database);
        var work=Environment.GetEnvironmentVariable("OASIS_POSTGRES_TEST_WORK")!;
        var journal=Path.Combine(work,$"native-identity-{detail}-{email}-{synchronous}.pending.json");Assert.IsFalse(File.Exists(journal));
        var marker="owned-pg-identity-"+Guid.NewGuid().ToString("N");
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};
        var table=detail?"oasis_avatar_details":"oasis_avatars";
        var provider=new PostgreSQLOASIS(connection);Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        await using var verifier=new NpgsqlConnection(connection);await verifier.OpenAsync();
        foreach(var id in ids){await using var absent=new NpgsqlCommand($"SELECT count(*) FROM {table} WHERE id=@id",verifier);absent.Parameters.AddWithValue("id",id);Assert.AreEqual(0L,await absent.ExecuteScalarAsync());}
        using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,table,marker,state="before-native-duplicate-identity"}));file.Flush(true);
        }
        var observed=new Dictionary<Guid,string>();
        try{
            for(var index=0;index<ids.Length;index++){
                var identity=index==0?marker:marker.ToUpperInvariant();
                if(detail){var saved=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=ids[index],Username=identity,Email=identity,MetaData=new(){["owned-fixture"]=marker}});Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
                else{var saved=await provider.SaveAvatarAsync(new Avatar{Id=ids[index],Username=identity,Email=identity,MetaData=new(){["owned-fixture"]=marker}});Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
                await using var read=new NpgsqlCommand($"SELECT data_json FROM {table} WHERE id=@id",verifier);read.Parameters.AddWithValue("id",ids[index]);
                var body=(string?)await read.ExecuteScalarAsync();Assert.IsNotNull(body);using var json=JsonDocument.Parse(body);
                Assert.AreEqual(ids[index],json.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(marker,json.RootElement.GetProperty("MetaData").GetProperty("owned-fixture").GetString());observed.Add(ids[index],body);
            }
            void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);}
            var query=" "+marker+" ";
            if(detail)Check(email?(synchronous?provider.LoadAvatarDetailByEmail(query):await provider.LoadAvatarDetailByEmailAsync(query)):(synchronous?provider.LoadAvatarDetailByUsername(query):await provider.LoadAvatarDetailByUsernameAsync(query)));
            else Check(email?(synchronous?provider.LoadAvatarByEmail(query):await provider.LoadAvatarByEmailAsync(query)):(synchronous?provider.LoadAvatarByUsername(query):await provider.LoadAvatarByUsernameAsync(query)));
        }finally{
            // Only exact observed synthetic rows can be removed; uncertain writes retain the journal.
            foreach(var row in observed){await using var cleanup=new NpgsqlCommand($"DELETE FROM {table} WHERE id=@id AND data_json=@observed",verifier);cleanup.Parameters.AddWithValue("id",row.Key);cleanup.Parameters.AddWithValue("observed",row.Value);Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}
            if(observed.Count==ids.Length){foreach(var id in ids){await using var absent=new NpgsqlCommand($"SELECT count(*) FROM {table} WHERE id=@id",verifier);absent.Parameters.AddWithValue("id",id);Assert.AreEqual(0L,await absent.ExecuteScalarAsync());}File.Delete(journal);}
            await provider.DeActivateProviderAsync();
        }
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeTextSearchUsesCanonicalNameAndDescription(bool description)
    {
        var connection=Environment.GetEnvironmentVariable("POSTGRESQLOASIS_CONNECTIONSTRING")!;
        var work=Environment.GetEnvironmentVariable("OASIS_POSTGRES_TEST_WORK")!;
        var settings=new NpgsqlConnectionStringBuilder(connection);
        Assert.AreEqual("127.0.0.1",settings.Host);Assert.AreEqual(15432,settings.Port);Assert.AreEqual("oasis_provider_test",settings.Database);
        var journal=Path.Combine(work,$"native-search-{description}.pending.json");Assert.IsFalse(File.Exists(journal));
        var marker="owned-pg-search-"+Guid.NewGuid().ToString("N");
        var row=new Holon{Id=Guid.NewGuid(),Name=description?"unrelated name":marker,Description=description?marker:"unrelated description",MetaData=new(){["owned-fixture"]=marker}};
        var provider=new PostgreSQLOASIS(connection);Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        await using var verifier=new NpgsqlConnection(connection);await verifier.OpenAsync();
        await using(var absent=new NpgsqlCommand("SELECT count(*) FROM oasis_holons WHERE id=@id",verifier)){absent.Parameters.AddWithValue("id",row.Id);Assert.AreEqual(0L,await absent.ExecuteScalarAsync());}
        using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id=row.Id,table="oasis_holons",marker,state="before-native-search-fixture"}));file.Flush(true);
        }
        string? ownedAcknowledgedBody=null;
        try{
            var saved=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);
            await using(var read=new NpgsqlCommand("SELECT data_json FROM oasis_holons WHERE id=@id",verifier)){
                read.Parameters.AddWithValue("id",row.Id);var body=(string?)await read.ExecuteScalarAsync();Assert.IsNotNull(body);
                using var document=JsonDocument.Parse(body);Assert.AreEqual(row.Id,document.RootElement.GetProperty("Id").GetGuid());
                Assert.AreEqual(marker,document.RootElement.GetProperty("MetaData").GetProperty("owned-fixture").GetString());ownedAcknowledgedBody=body;
            }
            var query=new SearchParams{SearchGroups=new(){new SearchTextGroup{SearchQuery=marker}}};
            var found=await provider.SearchAsync(query,loadChildren:false);Assert.IsFalse(found.IsError,found.Exception?.ToString());
            Assert.IsTrue(found.Result.SearchResultHolons.Any(value=>value.Id==row.Id),"Canonical persisted text must be searchable.");
        }finally{
            // Only a known acknowledged, identity/marker-verified fixture is disposable. Unknown saves retain their journal.
            if(ownedAcknowledgedBody!=null){
                await using var cleanup=new NpgsqlCommand("DELETE FROM oasis_holons WHERE id=@id AND data_json=@observed",verifier);
                cleanup.Parameters.AddWithValue("id",row.Id);cleanup.Parameters.AddWithValue("observed",ownedAcknowledgedBody);Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());
                await using var check=new NpgsqlCommand("SELECT count(*) FROM oasis_holons WHERE id=@id",verifier);check.Parameters.AddWithValue("id",row.Id);Assert.AreEqual(0L,await check.ExecuteScalarAsync());File.Delete(journal);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)]
    public async Task RealNpgsqlCreateReplaceReadSelectorsAndGuardedCleanup(int kind,bool synchronous)
    {
        var connection=Environment.GetEnvironmentVariable("POSTGRESQLOASIS_CONNECTIONSTRING");
        var settings=new NpgsqlConnectionStringBuilder(connection);
        Assert.AreEqual("127.0.0.1",settings.Host);Assert.AreEqual(15432,settings.Port);
        Assert.AreEqual("oasis_provider_test",settings.Database);
        var work=Environment.GetEnvironmentVariable("OASIS_POSTGRES_TEST_WORK")!;
        Assert.IsTrue(Directory.Exists(work));
        var journal=Path.Combine(work,$"native-record-{kind}-{synchronous}.pending.json");
        Assert.IsFalse(File.Exists(journal),"Reconcile the original fixture before another write.");
        var id=Guid.NewGuid();var marker="owned-pg-"+id.ToString("N");
        var table=new[]{"oasis_avatars","oasis_avatar_details","oasis_holons"}[kind];
        IHolonBase source=kind==0?new Avatar{Id=id,Username=marker,Email=marker+"@example.invalid"}
            :kind==1?new AvatarDetail{Id=id,Username=marker,Email=marker+"@example.invalid"}:new Holon{Id=id};
        source.Name=marker;source.MetaData=new(){["owned-fixture"]=marker,["amount"]=123.125m};
        var provider=new PostgreSQLOASIS(connection!);
        var activated=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError,activated.Exception?.ToString());Assert.IsTrue(provider.IsProviderActivated);
        await using var verifier=new NpgsqlConnection(connection);await verifier.OpenAsync();
        async Task<string?> Stored()
        {
            await using var read=new NpgsqlCommand($"SELECT data_json FROM {table} WHERE id=@id",verifier);
            read.Parameters.AddWithValue("id",id);return (string?)await read.ExecuteScalarAsync();
        }
        Assert.IsNull(await Stored());
        using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,table,marker,state="before-native-record-save"}));file.Flush(true);
        }
        async Task Save()
        {
            if(kind==0){var result=synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);}
            else if(kind==1){var result=synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);}
            else{var result=synchronous?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);}
        }
        async Task<IHolonBase> Read()
        {
            if(kind==0){var result=synchronous?provider.LoadAvatar(id):await provider.LoadAvatarAsync(id);Assert.IsFalse(result.IsError,result.Exception?.ToString());return result.Result;}
            if(kind==1){var result=synchronous?provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id);Assert.IsFalse(result.IsError,result.Exception?.ToString());return result.Result;}
            var holon=synchronous?provider.LoadHolon(id,loadChildren:false):await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsFalse(holon.IsError,holon.Exception?.ToString());return holon.Result;
        }
        await Save();Assert.AreEqual(id.ToString(),source.ProviderUniqueStorageKey[ProviderType.PostgreSQLOASIS]);
        var first=await Read();Assert.AreEqual(id,first.Id);Assert.AreEqual(source.Name,first.Name);
        Assert.AreEqual(123.125m,decimal.Parse(first.MetaData["amount"].ToString()!,CultureInfo.InvariantCulture));
        if(kind==0){Assert.AreEqual(id,(await provider.LoadAvatarByUsernameAsync(marker)).Result.Id);Assert.AreEqual(id,(await provider.LoadAvatarByEmailAsync(marker+"@example.invalid")).Result.Id);}
        if(kind==1){Assert.AreEqual(id,(await provider.LoadAvatarDetailByUsernameAsync(marker)).Result.Id);Assert.AreEqual(id,(await provider.LoadAvatarDetailByEmailAsync(marker+"@example.invalid")).Result.Id);}
        source.Description="replacement";await Save();Assert.AreEqual("replacement",(await Read()).Description);
        var observed=await Stored();Assert.IsNotNull(observed);
        using(var json=JsonDocument.Parse(observed))Assert.AreEqual(marker,json.RootElement.GetProperty("MetaData").GetProperty("owned-fixture").GetString());
        await using(var cleanup=new NpgsqlCommand($"DELETE FROM {table} WHERE id=@id AND data_json=@observed",verifier)){
            cleanup.Parameters.AddWithValue("id",id);cleanup.Parameters.AddWithValue("observed",observed);Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());
        }
        Assert.IsNull(await Stored());File.Delete(journal);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task ReconcileKnownComputedNameAssertionFixtures()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_POSTGRES_TEST_WORK")!;
        var report=Path.Combine(work,"postgres-native-current.trx");
        var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(report)));
        Assert.AreEqual("4C8A6D8DE1E9E1A696D75E2D7D9A2FC3F729C5B946BC23E7CA6114C60F701C47",hash);
        var fixtures=new[]{("native-record-0-False.pending.json","b3c9fc37-fe9e-4d24-94ee-cc7e7c3b5de2","oasis_avatars"),
            ("native-record-0-True.pending.json","d8c4f101-9793-4d9d-87c3-a323f23d8735","oasis_avatars"),
            ("native-record-1-False.pending.json","301f8269-1fd5-4c2d-93e4-191c15ff016e","oasis_avatar_details"),
            ("native-record-1-True.pending.json","d92dd969-bdc6-44ba-9a4e-d2bbb9550c83","oasis_avatar_details")};
        CollectionAssert.AreEquivalent(fixtures.Select(x=>x.Item1).ToArray(),Directory.GetFiles(work,"*.pending.json").Select(Path.GetFileName).ToArray());
        await using var connection=new NpgsqlConnection(Environment.GetEnvironmentVariable("POSTGRESQLOASIS_CONNECTIONSTRING"));await connection.OpenAsync();
        await using var transaction=await connection.BeginTransactionAsync();
        foreach(var (file,idText,table) in fixtures){
            var id=Guid.Parse(idText);var marker="owned-pg-"+id.ToString("N");
            using var journal=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(work,file)));
            Assert.AreEqual(id,journal.RootElement.GetProperty("id").GetGuid());Assert.AreEqual(table,journal.RootElement.GetProperty("table").GetString());
            Assert.AreEqual(marker,journal.RootElement.GetProperty("marker").GetString());Assert.AreEqual("before-native-record-save",journal.RootElement.GetProperty("state").GetString());
            await using var read=new NpgsqlCommand($"SELECT data_json FROM {table} WHERE id=@id",connection,transaction);read.Parameters.AddWithValue("id",id);
            var json=(string?)await read.ExecuteScalarAsync();Assert.IsNotNull(json);
            using var record=JsonDocument.Parse(json);Assert.AreEqual(id,record.RootElement.GetProperty("Id").GetGuid());
            Assert.AreEqual(marker,record.RootElement.GetProperty("MetaData").GetProperty("owned-fixture").GetString());
            await using var delete=new NpgsqlCommand($"DELETE FROM {table} WHERE id=@id AND data_json=@observed",connection,transaction);
            delete.Parameters.AddWithValue("id",id);delete.Parameters.AddWithValue("observed",json);Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());
        }
        await transaction.CommitAsync();
        foreach(var (file,idText,table) in fixtures){
            await using var check=new NpgsqlCommand($"SELECT count(*) FROM {table} WHERE id=@id",connection);check.Parameters.AddWithValue("id",Guid.Parse(idText));Assert.AreEqual(0L,await check.ExecuteScalarAsync());
            File.Delete(Path.Combine(work,file));
        }
    }
}
