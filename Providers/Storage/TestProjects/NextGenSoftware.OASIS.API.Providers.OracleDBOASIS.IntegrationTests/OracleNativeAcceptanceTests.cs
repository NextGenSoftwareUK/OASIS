using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public sealed class OracleNativeAcceptanceTests
{
    [TestMethod]
    public async Task RealSdkCanonicalRecordsSelectorsAndMetadataPaths()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")
            ?? throw new InvalidOperationException("Explicit owned test connection required.");
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")
            ?? throw new InvalidOperationException("Use the retained Oracle environment.");
        var journal=Path.Combine(work,"canonical-records.pending.json");
        var resume=Environment.GetEnvironmentVariable("OASIS_ORACLE_RESUME_CANONICAL")=="1";
        Assert.AreEqual(resume,File.Exists(journal),"Resume requires the original journal; a fresh run requires absence.");
        var provider=new OracleDBOASIS(connection);var id=Guid.NewGuid();var name="owned-oracle-"+id.ToString("N");
        if(resume){
            using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.AreEqual("before-native-canonical-records",intent.RootElement.GetProperty("state").GetString());
            id=intent.RootElement.GetProperty("id").GetGuid();name=intent.RootElement.GetProperty("name").GetString()!;
            Assert.AreEqual("owned-oracle-"+id.ToString("N"),name);
        }
        var avatar=new Avatar{Id=id,Username=name,Email=name+"@example.invalid"};
        var detail=new AvatarDetail{Id=id,Username=name,Email=avatar.Email,Karma=321,XP=456};
        detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name="owned oak seed",Quantity=7,GameSource="Our World"});
        var holon=new Holon{Id=id,Name=name,ParentHolonId=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),
            MetaData=new(){["owned.fixture"]="literal-dot",["owner's key"]="literal-apostrophe",["quote\"key"]="literal-quote"}};
        holon.Children.Add(new Holon{Id=Guid.NewGuid(),Name="must-not-be-embedded"});
        try{
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Exception?.ToString());Assert.IsTrue(provider.IsProviderActivated);
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            if(!resume){
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS"})Assert.IsNull(await Read(verifier,table,id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,name,state="before-native-canonical-records"}));file.Flush(true);
            }
            var savedAvatar=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(savedAvatar.IsError,savedAvatar.Exception?.ToString());
            var savedDetail=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(savedDetail.IsError,savedDetail.Exception?.ToString());
            var savedHolon=await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(savedHolon.IsError,savedHolon.Exception?.ToString());
            }else{
                // Reconcile originals; never submit these writes again.
                var originalHolon=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>((await Read(verifier,"OASIS_HOLONS",id))!);
                Assert.AreEqual(id,originalHolon.Id);Assert.AreEqual(name,originalHolon.Name);
                holon=originalHolon;
            }
            Assert.AreEqual(id,(await provider.LoadAvatarAsync(id)).Result.Id);
            Assert.AreEqual(id,(await provider.LoadAvatarByUsernameAsync(name)).Result.Id);
            var byEmail=await provider.LoadAvatarByEmailAsync(avatar.Email);Assert.IsFalse(byEmail.IsError,byEmail.Exception?.ToString());Assert.AreEqual(id,byEmail.Result.Id);
            foreach(var loaded in new[]{await provider.LoadAvatarDetailAsync(id),await provider.LoadAvatarDetailByUsernameAsync(name),await provider.LoadAvatarDetailByEmailAsync(avatar.Email)}){
                Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(321L,loaded.Result.Karma);Assert.AreEqual(detail.XP,loaded.Result.XP);
                Assert.AreEqual(7,loaded.Result.Inventory.Single().Quantity);
            }
            var readHolon=await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsFalse(readHolon.IsError,readHolon.Exception?.ToString());
            Assert.AreEqual(holon.ParentHolonId,readHolon.Result.ParentHolonId);Assert.AreEqual(0,readHolon.Result.Children.Count);
            foreach(var pair in holon.MetaData){
                var found=await provider.LoadHolonsByMetaDataAsync(pair.Key,(string)pair.Value,holon.HolonType,loadChildren:false);
                Assert.IsFalse(found.IsError,found.Exception?.ToString());Assert.IsTrue(found.Result.Any(row=>row.Id==id));
            }
            var exported=await provider.ExportAllDataForAvatarByIdAsync(holon.CreatedByAvatarId);Assert.IsFalse(exported.IsError,exported.Exception?.ToString());Assert.IsTrue(exported.Result.Any(row=>row.Id==id));
            if(!resume){
                foreach(var synchronous in new[]{false,true}){
                    var oldName=avatar.Username;var oldEmail=avatar.Email;
                    avatar.Username=name+(synchronous?"-sync":"-async");avatar.Email=avatar.Username+"@example.invalid";
                    detail.Username=avatar.Username;detail.Email=avatar.Email;detail.Karma+=11;detail.XP+=17;detail.Inventory[0].Quantity+=2;
                    holon.Name=avatar.Username;holon.ParentHolonId=Guid.NewGuid();holon.MetaData["owned.fixture"]=avatar.Username;
                    var updateAvatar=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);
                    var updateDetail=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
                    var updateHolon=synchronous?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);
                    Assert.IsFalse(updateAvatar.IsError,updateAvatar.Exception?.ToString());Assert.IsTrue(updateAvatar.IsSaved);Assert.AreSame(avatar,updateAvatar.Result);
                    Assert.IsFalse(updateDetail.IsError,updateDetail.Exception?.ToString());Assert.IsTrue(updateDetail.IsSaved);Assert.AreSame(detail,updateDetail.Result);
                    Assert.IsFalse(updateHolon.IsError,updateHolon.Exception?.ToString());Assert.IsTrue(updateHolon.IsSaved);Assert.AreSame(holon,updateHolon.Result);
                    foreach(var loaded in new[]{await provider.LoadAvatarAsync(id),await provider.LoadAvatarByUsernameAsync(avatar.Username),await provider.LoadAvatarByEmailAsync(avatar.Email)}){
                        Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(avatar.Username,loaded.Result.Username);Assert.AreEqual(avatar.Email,loaded.Result.Email);
                    }
                    Assert.IsNull((await provider.LoadAvatarByUsernameAsync(oldName)).Result);Assert.IsNull((await provider.LoadAvatarByEmailAsync(oldEmail)).Result);
                    foreach(var loaded in new[]{await provider.LoadAvatarDetailAsync(id),await provider.LoadAvatarDetailByUsernameAsync(detail.Username),await provider.LoadAvatarDetailByEmailAsync(detail.Email)}){
                        Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(detail.Karma,loaded.Result.Karma);Assert.AreEqual(detail.XP,loaded.Result.XP);Assert.AreEqual(detail.Inventory[0].Quantity,loaded.Result.Inventory.Single().Quantity);
                    }
                    Assert.IsNull((await provider.LoadAvatarDetailByUsernameAsync(oldName)).Result);Assert.IsNull((await provider.LoadAvatarDetailByEmailAsync(oldEmail)).Result);
                    var loadedHolon=await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsFalse(loadedHolon.IsError,loadedHolon.Exception?.ToString());
                    Assert.AreEqual(holon.Name,loadedHolon.Result.Name);Assert.AreEqual(holon.ParentHolonId,loadedHolon.Result.ParentHolonId);
                    var found=await provider.LoadHolonsByMetaDataAsync("owned.fixture",avatar.Username,holon.HolonType,loadChildren:false);Assert.IsFalse(found.IsError,found.Exception?.ToString());Assert.IsTrue(found.Result.Any(row=>row.Id==id));
                    var raw=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<AvatarDetail>((await Read(verifier,"OASIS_AVATAR_DETAILS",id))!);
                    Assert.AreEqual(detail.Karma,raw.Karma);Assert.AreEqual(detail.XP,raw.XP);Assert.AreEqual(detail.Inventory[0].Quantity,raw.Inventory.Single().Quantity);
                }
            }
            var invalidKeyDelete=await provider.DeleteAvatarAsync(name,false);
            Assert.IsTrue(invalidKeyDelete.IsError);Assert.IsInstanceOfType<FormatException>(invalidKeyDelete.Exception);
            Assert.IsNotNull(await Read(verifier,"OASIS_AVATARS",id));
            var keyDelete=await provider.DeleteAvatarAsync(id.ToString(),false);Assert.IsFalse(keyDelete.IsError,keyDelete.Exception?.ToString());Assert.IsTrue(keyDelete.Result);
            Assert.IsNull(await Read(verifier,"OASIS_AVATARS",id));
            // Paired detail deletion is still an explicit implementation gap; do not pretend this owner handles it.
            foreach(var table in new[]{"OASIS_AVATAR_DETAILS","OASIS_HOLONS"}){
                var actual=await Read(verifier,table,id);Assert.IsNotNull(actual);
                await using var delete=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                delete.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;
                Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,table,id));
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();Assert.IsFalse(provider.IsProviderActivated);}
    }

    [TestMethod]
    public async Task DuplicateIdentityIsRejectedInsteadOfSelectingArbitraryAvatar()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"identity-ambiguity.pending.json");
        var resume=Environment.GetEnvironmentVariable("OASIS_ORACLE_RECONCILE_AMBIGUITY")=="1";
        Assert.AreEqual(resume,File.Exists(journal));
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};var name="owned-duplicate-"+ids[0].ToString("N");
        if(resume){
            using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.AreEqual("before-native-duplicate-identities",intent.RootElement.GetProperty("state").GetString());
            ids=intent.RootElement.GetProperty("ids").EnumerateArray().Select(value=>value.GetGuid()).ToArray();
            Assert.AreEqual(2,ids.Length);Assert.AreNotEqual(ids[0],ids[1]);name="owned-duplicate-"+ids[0].ToString("N");
            Assert.AreEqual(name,intent.RootElement.GetProperty("name").GetString());
        }
        var email=name+"@example.invalid";var provider=new OracleDBOASIS(connection);
        try{
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Exception?.ToString());
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            if(!resume){
                foreach(var id in ids)foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"})Assert.IsNull(await Read(verifier,table,id));
                using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                    await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,name,state="before-native-duplicate-identities"}));file.Flush(true);
                }
                foreach(var id in ids){
                    var avatar=await provider.SaveAvatarAsync(new Avatar{Id=id,Username=name,Email=email});Assert.IsFalse(avatar.IsError,avatar.Exception?.ToString());
                    var detail=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=name,Email=email});Assert.IsFalse(detail.IsError,detail.Exception?.ToString());
                }
            }
            foreach(var id in ids){
                var avatar=await provider.LoadAvatarAsync(id);Assert.IsFalse(avatar.IsError,avatar.Exception?.ToString());Assert.AreEqual(name,avatar.Result.Username);Assert.AreEqual(email,avatar.Result.Email);
                var detail=await provider.LoadAvatarDetailAsync(id);Assert.IsFalse(detail.IsError,detail.Exception?.ToString());Assert.AreEqual(name,detail.Result.Username);Assert.AreEqual(email,detail.Result.Email);
            }
            Check(await provider.LoadAvatarByUsernameAsync(name));Check(provider.LoadAvatarByUsername(name));
            Check(await provider.LoadAvatarByEmailAsync(email));Check(provider.LoadAvatarByEmail(email));
            Check(await provider.LoadAvatarDetailByUsernameAsync(name));Check(provider.LoadAvatarDetailByUsername(name));
            Check(await provider.LoadAvatarDetailByEmailAsync(email));Check(provider.LoadAvatarDetailByEmail(email));
            foreach(var id in ids)foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"}){
                var actual=await Read(verifier,table,id);Assert.IsNotNull(actual);
                await using var delete=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                delete.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;
                Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,table,id));
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
        static void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.IsTrue(result.IsError,"Duplicate identity must not silently pick the first record.");Assert.IsNull(result.Result);
            Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
        }
    }

    private static async Task<string?> Read(OracleConnection connection,string table,Guid id)
    {
        await using var command=new OracleCommand($"SELECT DATA_JSON FROM {table} WHERE ID=:Id",connection){BindByName=true};
        command.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
        var value=await command.ExecuteScalarAsync();return value==null||value==DBNull.Value?null:value.ToString();
    }
}
