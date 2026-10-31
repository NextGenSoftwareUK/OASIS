using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod]
    [DataRow(0,false,false)][DataRow(1,false,false)][DataRow(2,false,false)]
    [DataRow(0,true,false)][DataRow(1,true,false)][DataRow(2,true,false)]
    [DataRow(0,false,true)][DataRow(1,false,true)][DataRow(2,false,true)]
    [DataRow(0,true,true)][DataRow(1,true,true)][DataRow(2,true,true)]
    public async Task NativeAvatarDeletionResultMetadataDefinesKnownOutcome(int selector,bool synchronous,bool soft)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var id=Guid.NewGuid();var marker="owned-delete-result-"+id.ToString("N");var email=marker+"@example.invalid";
        var tables=new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"};var journal=Path.Combine(work,"avatar-delete-result.pending.json");
        var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            foreach(var table in tables)Assert.IsNull(await Read(verifier,table,id));Assert.IsFalse(File.Exists(journal));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,marker,email,selector,synchronous,soft,state="before-avatar-delete-result"}));file.Flush(true);
            }
            var profile=await provider.SaveAvatarAsync(new Avatar{Id=id,Username=marker,Email=email});Assert.IsFalse(profile.IsError,profile.Exception?.ToString());
            var detail=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=marker,Email=email});Assert.IsFalse(detail.IsError,detail.Exception?.ToString());
            Task<OASISResult<bool>> Delete()=>selector==0
                ?synchronous?Task.FromResult(provider.DeleteAvatar(id,soft)):provider.DeleteAvatarAsync(id,soft)
                :selector==1?synchronous?Task.FromResult(provider.DeleteAvatarByUsername(marker,soft)):provider.DeleteAvatarByUsernameAsync(marker,soft)
                :synchronous?Task.FromResult(provider.DeleteAvatarByEmail(email,soft)):provider.DeleteAvatarByEmailAsync(email,soft);
            var first=await Delete();Assert.IsFalse(first.IsError,first.Exception?.ToString());Assert.IsTrue(first.Result);Assert.AreEqual(3,provider.CommitCalls);
            var observed=new Dictionary<string,string>();
            foreach(var table in tables){
                var raw=await Read(verifier,table,id);
                if(!soft){Assert.IsNull(raw);continue;}
                Assert.IsNotNull(raw);using var record=JsonDocument.Parse(raw);
                Assert.AreEqual(id,record.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(marker,record.RootElement.GetProperty("Username").GetString());
                Assert.IsTrue(record.RootElement.GetProperty("IsDeleted").GetBoolean());Assert.AreEqual(2,record.RootElement.GetProperty("Version").GetInt32());observed.Add(table,raw);
            }
            OASISResult<bool>? repeated=null;
            if(soft){
                repeated=await Delete();Assert.IsFalse(repeated.IsError,repeated.Exception?.ToString());Assert.IsTrue(repeated.Result);Assert.AreEqual(3,provider.CommitCalls);
                foreach(var original in observed)Assert.AreEqual(original.Value,await Read(verifier,original.Key,id));
                await using var transaction=(OracleTransaction)await verifier.BeginTransactionAsync();
                foreach(var original in observed){
                    var flag=original.Key=="OASIS_AVATARS"?" AND IS_DELETED=1":"";
                    await using var cleanup=new OracleCommand($"DELETE FROM {original.Key} WHERE ID=:Id AND USERNAME=:Name AND EMAIL=:Email AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0{flag}",verifier){BindByName=true,Transaction=transaction};
                    cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Name",OracleDbType.Varchar2).Value=marker;
                    cleanup.Parameters.Add("Email",OracleDbType.Varchar2).Value=email;cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=original.Value;
                    Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());
                }
                await transaction.CommitAsync();
            }
            foreach(var table in tables)Assert.IsNull(await Read(verifier,table,id));File.Delete(journal);
            // Count/flag assertions follow exact outcome inspection/cleanup, so a metadata red never abandons a verified fixture.
            Assert.IsTrue(first.IsDeleted);Assert.IsTrue(first.IsSaved);Assert.AreEqual(1,first.DeletedCount);Assert.AreEqual(0,first.SavedCount);
            if(repeated!=null){Assert.IsTrue(repeated.IsDeleted);Assert.IsFalse(repeated.IsSaved);Assert.AreEqual(1,repeated.DeletedCount);Assert.AreEqual(0,repeated.SavedCount);}
        }finally{await provider.DeActivateProviderAsync();}
    }
}
