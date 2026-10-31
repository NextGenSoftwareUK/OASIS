using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeIdentityDeclaredCharacterCapacityPreservesUnicode(bool detail,bool unicode)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"identity-character-{detail}-{unicode}.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var prefix=id.ToString("N");
        var username=prefix+new string(unicode?'界':'a',256-prefix.Length);var email=prefix+"@example.invalid";
        var table=detail?"OASIS_AVATAR_DETAILS":"OASIS_AVATARS";
        var provider=new OracleDBOASIS(connection);
        await using var verifier=new OracleConnection(connection);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            Assert.IsNull(await Read(verifier,table,id));
            await using(var schema=new OracleCommand("SELECT CHAR_LENGTH,CHAR_USED,DATA_LENGTH FROM USER_TAB_COLUMNS WHERE TABLE_NAME=:TableName AND COLUMN_NAME='USERNAME'",verifier){BindByName=true}){
                schema.Parameters.Add("TableName",OracleDbType.Varchar2).Value=table;
                await using var reader=await schema.ExecuteReaderAsync();Assert.IsTrue(await reader.ReadAsync());
                Assert.IsTrue(reader.GetInt32(0)>=256);
                Console.WriteLine($"Owned identity {id}: declared characters={reader.GetInt32(0)}, semantics={reader.GetString(1)}, bytes={reader.GetInt32(2)}; input UTF16 length={username.Length}.");
            }
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,table,username,email,detail,unicode,state="before-owned-identity-character"}));file.Flush(true);
            }
            bool failed;Exception? cause;
            if(detail){var result=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=username,Email=email,Karma=17,XP=31});failed=result.IsError;cause=result.Exception;}
            else{var result=await provider.SaveAvatarAsync(new Avatar{Id=id,Username=username,Email=email});failed=result.IsError;cause=result.Exception;}
            var actual=await Read(verifier,table,id);
            if(failed)
            {
                // Reconcile this ORIGINAL server rejection, never retry a potentially unknown write.
                var native=cause;while(native!=null&&native is not OracleException)native=native.InnerException;
                Assert.IsInstanceOfType<OracleException>(native,cause?.ToString());
                Assert.AreEqual(12899,((OracleException)native!).Number,cause?.ToString());
                Assert.IsNull(actual,"The original ORA-12899 statement rejection must leave no owned record.");
                Console.WriteLine($"Original owned operation {id} rejected by Oracle ORA-12899; independent native absence confirmed, no replay.");
            }
            else
            {
                Assert.IsNotNull(actual);using var parsed=JsonDocument.Parse(actual);
                Assert.AreEqual(id,parsed.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(username,parsed.RootElement.GetProperty("Username").GetString());
                await using var cleanup=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND USERNAME=:Username AND EMAIL=:Email AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Username",OracleDbType.Varchar2).Value=username;
                cleanup.Parameters.Add("Email",OracleDbType.Varchar2).Value=email;cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,table,id));
            }
            File.Delete(journal);
            Assert.IsFalse(failed,"Activation accepted 256-character capacity, but a 256-character identity could not round-trip: "+cause);
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
