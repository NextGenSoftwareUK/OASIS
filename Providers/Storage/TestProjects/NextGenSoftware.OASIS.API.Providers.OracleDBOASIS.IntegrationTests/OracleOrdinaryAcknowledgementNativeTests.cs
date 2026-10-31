using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(true,false,false)][DataRow(false,true,false)][DataRow(true,true,false)]
    [DataRow(false,false,true)][DataRow(true,false,true)][DataRow(false,true,true)][DataRow(true,true,true)]
    public async Task NativeOrdinaryIdentitySaveCountsOnlyAcknowledgedCommit(bool detail,bool synchronous,bool loseAck)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;var id=Guid.NewGuid();var marker="owned-ordinary-ack-"+id.ToString("N");
        var table=detail?"OASIS_AVATAR_DETAILS":"OASIS_AVATARS";var journal=Path.Combine(work,"ordinary-identity-ack.pending.json");
        var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true,LoseNextCommit=loseAck};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            Assert.IsNull(await Read(verifier,table,id));Assert.IsFalse(File.Exists(journal));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,table,marker,synchronous,loseAck,state="before-native-ordinary-identity-ack"}));file.Flush(true);
            }
            int count;
            if(detail){var row=new AvatarDetail{Id=id,Username=marker,Email=marker+"@example.invalid"};var result=synchronous?provider.SaveAvatarDetail(row):await provider.SaveAvatarDetailAsync(row);Check(result);count=result.SavedCount;Assert.AreEqual(loseAck?0:1,row.Version);}
            else{var row=new Avatar{Id=id,Username=marker,Email=marker+"@example.invalid"};var result=synchronous?provider.SaveAvatar(row):await provider.SaveAvatarAsync(row);Check(result);count=result.SavedCount;Assert.AreEqual(loseAck?0:1,row.Version);}
            Assert.AreEqual(1,provider.CommitCalls);
            var original=await Read(verifier,table,id);Assert.IsNotNull(original);using(var parsed=JsonDocument.Parse(original)){
                Assert.AreEqual(id,parsed.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(marker,parsed.RootElement.GetProperty("Username").GetString());Assert.AreEqual(1,parsed.RootElement.GetProperty("Version").GetInt32());
            }
            await using(var cleanup=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND USERNAME=:Name AND EMAIL=:Email AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true}){
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Name",OracleDbType.Varchar2).Value=marker;
                cleanup.Parameters.Add("Email",OracleDbType.Varchar2).Value=marker+"@example.invalid";cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=original;
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());
            }
            Assert.IsNull(await Read(verifier,table,id));File.Delete(journal);
            Assert.AreEqual(loseAck?0:1,count,"Only native commits acknowledged to this operation may be counted.");
        }finally{await provider.DeActivateProviderAsync();}
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.AreEqual(loseAck,result.IsError,result.Exception?.ToString());Assert.AreEqual(!loseAck,result.IsSaved);
            if(loseAck){Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(result.Exception);Assert.AreSame(provider.Lost,result.Exception.InnerException);}
        }
    }
}
