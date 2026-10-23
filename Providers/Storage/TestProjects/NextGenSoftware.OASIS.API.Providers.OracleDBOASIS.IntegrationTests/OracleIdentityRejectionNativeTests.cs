using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    private sealed class CapacityCommitOwner(string connection):OracleDBOASIS(connection)
    {
        public OracleException? FailNextCommit;
        protected override async Task CommitLifecycleTransactionAsync(OracleTransaction transaction)
        {
            await base.CommitLifecycleTransactionAsync(transaction);
            if(FailNextCommit is {} cause){FailNextCommit=null;throw cause;}
        }
    }

    public static IEnumerable<object[]> IdentityRejectionCases()
    {
        foreach(var detail in new[]{false,true})foreach(var synchronous in new[]{false,true})
        foreach(var update in new[]{false,true})foreach(var unicode in new[]{false,true})
            yield return new object[]{detail,synchronous,update,unicode};
    }

    private static OracleException CapacityCause(Exception? cause)
    {
        while(cause!=null&&cause is not OracleException)cause=cause.InnerException;
        Assert.IsInstanceOfType<OracleException>(cause);Assert.AreEqual(12899,((OracleException)cause!).Number);
        return (OracleException)cause;
    }

    [DataTestMethod][DynamicData(nameof(IdentityRejectionCases),DynamicDataSourceType.Method)]
    public async Task NativeIdentityColumnRejectionDoesNotBecomeUnknown(bool detail,bool synchronous,bool update,bool unicode)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"identity-rejection-{detail}-{synchronous}-{update}-{unicode}.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var prefix=id.ToString("N");
        var table=detail?"OASIS_AVATAR_DETAILS":"OASIS_AVATARS";var email=prefix+"@example.invalid";
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        await using var verifier=new OracleConnection(connection);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,table,id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,table,prefix,email,detail,synchronous,update,unicode,state="before-original-capacity-rejection"}));file.Flush(true);
            }
            if(update){
                if(detail)Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=prefix,Email=email,Karma=17,XP=31})).IsError);
                else Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar{Id=id,Username=prefix,Email=email})).IsError);
            }
            var before=await Read(verifier,table,id);
            var tooLong=prefix+new string(unicode?'界':'a',257-prefix.Length);
            var avatar=new Avatar{Id=id,Username=tooLong,Email=email};
            var avatarDetail=new AvatarDetail{Id=id,Username=tooLong,Email=email,Karma=999,XP=888};
            var callerVersion=detail?avatarDetail.Version:avatar.Version;
            OASISResult<bool> Envelope<T>(OASISResult<T> value)=>OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<T,bool>(value);
            var result=detail?Envelope(synchronous?provider.SaveAvatarDetail(avatarDetail):await provider.SaveAvatarDetailAsync(avatarDetail))
                :Envelope(synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar));
            _=CapacityCause(result.Exception);
            Assert.AreEqual(before,await Read(verifier,table,id),"Original rejected statement must preserve exact prior data/history or absence.");
            if(before!=null){
                await using var cleanup=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND USERNAME=:Username AND EMAIL=:Email AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Username",OracleDbType.Varchar2).Value=prefix;
                cleanup.Parameters.Add("Email",OracleDbType.Varchar2).Value=email;cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=before;
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());
            }
            Assert.IsNull(await Read(verifier,table,id));File.Delete(journal);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);
            Assert.AreEqual(callerVersion,detail?avatarDetail.Version:avatar.Version);
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception),"An original ORA-12899 from the canonical DML statement is a known rejection, not an unknown commit.");
            Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
        }
        finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeCommitPhaseCapacityErrorRemainsUncertain(bool detail,bool synchronous)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"capacity-commit-{detail}-{synchronous}.pending.json");
        Assert.IsFalse(File.Exists(journal));var rejected=Guid.NewGuid();var committed=Guid.NewGuid();var name="owned-capacity-commit-"+committed.ToString("N");
        var table=detail?"OASIS_AVATAR_DETAILS":"OASIS_AVATARS";
        var provider=new CapacityCommitOwner(connection);
        await using var verifier=new OracleConnection(connection);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            Assert.IsNull(await Read(verifier,table,rejected));Assert.IsNull(await Read(verifier,table,committed));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{rejected,committed,table,name,detail,synchronous,state="before-owned-commit-phase-fault"}));file.Flush(true);
            }
            OracleException original;
            if(detail)original=CapacityCause((await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=rejected,Username=new string('a',257),Email="owned@example.invalid"})).Exception);
            else original=CapacityCause((await provider.SaveAvatarAsync(new Avatar{Id=rejected,Username=new string('a',257),Email="owned@example.invalid"})).Exception);
            Assert.IsNull(await Read(verifier,table,rejected));
            // Fault injection on a DISTINCT operation after a real successful commit.
            provider.FailNextCommit=original;
            Exception? cause;bool failed;int savedCount;bool isSaved;
            if(detail){var value=new AvatarDetail{Id=committed,Username=name,Email="owned@example.invalid",Karma=17,XP=31};var result=synchronous?provider.SaveAvatarDetail(value):await provider.SaveAvatarDetailAsync(value);cause=result.Exception;failed=result.IsError;savedCount=result.SavedCount;isSaved=result.IsSaved;}
            else{var value=new Avatar{Id=committed,Username=name,Email="owned@example.invalid"};var result=synchronous?provider.SaveAvatar(value):await provider.SaveAvatarAsync(value);cause=result.Exception;failed=result.IsError;savedCount=result.SavedCount;isSaved=result.IsSaved;}
            var actual=await Read(verifier,table,committed);Assert.IsNotNull(actual);
            using(var parsed=JsonDocument.Parse(actual)){Assert.AreEqual(committed,parsed.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(name,parsed.RootElement.GetProperty("Username").GetString());}
            await using(var cleanup=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND USERNAME=:Username AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true}){
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=committed.ToByteArray();cleanup.Parameters.Add("Username",OracleDbType.Varchar2).Value=name;cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());
            }
            Assert.IsNull(await Read(verifier,table,committed));Assert.IsNull(await Read(verifier,table,rejected));File.Delete(journal);
            Assert.IsTrue(failed);Assert.IsFalse(isSaved);Assert.AreEqual(0,savedCount);
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(cause),"The same error number AFTER DML acknowledgement must not be downgraded to a known statement rejection.");
            Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(cause);Assert.AreSame(original,cause!.InnerException);
            Assert.AreEqual(committed,((OracleLifecycleReconciliationException)cause).RecordId);
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
