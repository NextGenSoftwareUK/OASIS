using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod]
    [DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)]
    [DataRow(false,2)][DataRow(true,2)][DataRow(false,3)][DataRow(true,3)]
    public async Task NativeDetailDeleteRejectsAmbiguousOrCorruptSelectorsWithoutMutation(bool soft,int mode)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"detail-selector-{soft}-{mode}.pending.json");
        Assert.IsFalse(File.Exists(journal));var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};var name="owned-detail-selector-"+ids[0].ToString("N");
        var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true};
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            foreach(var id in ids)Assert.IsNull(await Read(verifier,"OASIS_AVATAR_DETAILS",id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,name,soft,mode,state="before-owned-detail-selector"}));file.Flush(true);}
            foreach(var id in ids){
                var saved=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=mode<2?name:name+id.ToString("N"),Email=mode<2?name+"@example.invalid":id.ToString("N")+"@example.invalid",IsActive=true});
                Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            }
            var column=mode%2==0?"USERNAME":"EMAIL";var selector=column=="USERNAME"?name:name+"@example.invalid";
            if(mode>=2){
                await using var corrupt=new OracleCommand($"UPDATE OASIS_AVATAR_DETAILS SET {column}=:Selector WHERE ID=:Id",verifier){BindByName=true};
                corrupt.Parameters.Add("Selector",OracleDbType.Varchar2).Value=selector;corrupt.Parameters.Add("Id",OracleDbType.Raw).Value=ids[0].ToByteArray();
                Assert.AreEqual(1,await corrupt.ExecuteNonQueryAsync());
            }
            var originals=new List<string>();foreach(var id in ids){var json=await Read(verifier,"OASIS_AVATAR_DETAILS",id);Assert.IsNotNull(json);originals.Add(json);}
            var before=provider.CommitCalls;IOASISStorageProvider storage=provider;
            var result=column=="USERNAME"?storage.DeleteAvatarDetailByUsername(selector,soft):await storage.DeleteAvatarDetailByEmailAsync(selector,soft);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
            Assert.AreEqual(before,provider.CommitCalls);
            for(var index=0;index<ids.Length;index++){
                Assert.AreEqual(originals[index],await Read(verifier,"OASIS_AVATAR_DETAILS",ids[index]));
                await using var cleanup=new OracleCommand("DELETE FROM OASIS_AVATAR_DETAILS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=ids[index].ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=originals[index];
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,"OASIS_AVATAR_DETAILS",ids[index]));
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)]
    [DataRow(false,2)][DataRow(true,2)][DataRow(false,3)][DataRow(true,3)]
    public async Task NativeStandaloneDetailLifecyclePreservesAvatarAndReportsLostCommit(bool soft,int mode)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"detail-lifecycle-{soft}-{mode}.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var name="owned-detail-"+id.ToString("N");
        var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true};
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"})Assert.IsNull(await Read(verifier,table,id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,name,soft,mode,state="before-owned-detail-lifecycle"}));file.Flush(true);}
            var savedAvatar=await provider.SaveAvatarAsync(new Avatar{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true});Assert.IsFalse(savedAvatar.IsError,savedAvatar.Exception?.ToString());
            var savedDetail=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true,Karma=123,XP=456});Assert.IsFalse(savedDetail.IsError,savedDetail.Exception?.ToString());
            var avatar=await Read(verifier,"OASIS_AVATARS",id);Assert.IsNotNull(avatar);
            var before=provider.CommitCalls;provider.LoseNextCommit=mode==3;
            IOASISStorageProvider storage=provider;
            var result=mode==1?storage.DeleteAvatarDetailByUsername(name,soft):mode==2?await storage.DeleteAvatarDetailByEmailAsync(name+"@example.invalid",soft):await storage.DeleteAvatarDetailAsync(id,soft);
            Assert.AreEqual(mode==3,result.IsError,result.Exception?.ToString());Assert.AreEqual(mode!=3,result.Result);
            Assert.AreEqual(before+1,provider.CommitCalls);Assert.AreEqual(avatar,await Read(verifier,"OASIS_AVATARS",id));
            if(mode==3){Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(result.Exception);Assert.AreSame(provider.Lost,result.Exception.InnerException);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));}
            var actual=await Read(verifier,"OASIS_AVATAR_DETAILS",id);
            if(soft){Assert.IsNotNull(actual);var detail=OasisJson.DeserializeStored<AvatarDetail>(actual);Assert.IsTrue(detail.IsDeleted);Assert.IsFalse(detail.IsActive);Assert.AreEqual(123L,detail.Karma);Assert.AreEqual(456L,detail.XP);
                using var parsed=JsonDocument.Parse(actual);Assert.AreEqual(2,parsed.RootElement.GetProperty("Version").GetInt32());Assert.IsTrue(parsed.RootElement.TryGetProperty(EmbeddedRecordHistory.PropertyName,out _));
                var repeat=await storage.DeleteAvatarDetailAsync(id);Assert.IsFalse(repeat.IsError);Assert.AreEqual(before+1,provider.CommitCalls);Assert.AreEqual(actual,await Read(verifier,"OASIS_AVATAR_DETAILS",id));
            }else Assert.IsNull(actual);
            foreach(var table in new[]{"OASIS_AVATAR_DETAILS","OASIS_AVATARS"}){
                var expected=await Read(verifier,table,id);if(expected==null)continue;
                await using var cleanup=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=expected;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,table,id));
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
