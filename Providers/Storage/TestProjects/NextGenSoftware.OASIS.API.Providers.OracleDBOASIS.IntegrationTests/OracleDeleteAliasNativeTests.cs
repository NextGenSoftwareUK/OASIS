using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    private sealed class DeleteAliasCommitOwner(string connection):OracleDBOASIS(connection)
    {
        public bool LoseNextCommit;
        public int Commits;
        public readonly IOException Lost=new("owned delete alias acknowledgement lost");
        protected override async Task CommitLifecycleTransactionAsync(OracleTransaction transaction)
        {
            await base.CommitLifecycleTransactionAsync(transaction);Commits++;
            if(LoseNextCommit){LoseNextCommit=false;throw Lost;}
        }
    }

    public static IEnumerable<object[]> DeleteAliasCases()
    {
        foreach(var legacy in new[]{false,true})foreach(var key in new[]{false,true})
        foreach(var sync in new[]{false,true})foreach(var soft in legacy?new[]{true}:new[]{false,true})
        foreach(var lost in new[]{false,true})yield return new object[]{legacy,key,sync,soft,lost};
    }

    [DataTestMethod][DynamicData(nameof(DeleteAliasCases),DynamicDataSourceType.Method)]
    public async Task NativeHolonDeleteAliasesCountAcknowledgedMutationOnly(bool legacy,bool key,bool synchronous,bool soft,bool lost)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,"delete-alias.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var name="owned-delete-alias-"+id.ToString("N");
        var provider=new DeleteAliasCommitOwner(connection){IsVersionControlEnabled=true};IOASISStorageProvider storage=provider;
        await using var native=new OracleConnection(connection);await native.OpenAsync();Assert.IsNull(await Read(native,"OASIS_HOLONS",id));
        var bad=new List<string>();
        async Task<OASISResult<bool>> Delete()
        {
            if(!legacy)return key
                ?synchronous?provider.DeleteHolonWithSoftDelete(id.ToString(),soft):await provider.DeleteHolonWithSoftDeleteAsync(id.ToString(),soft)
                :synchronous?provider.DeleteHolonWithSoftDelete(id,soft):await provider.DeleteHolonWithSoftDeleteAsync(id,soft);
            var source=key?synchronous?storage.DeleteHolon(id.ToString()):await storage.DeleteHolonAsync(id.ToString())
                :synchronous?storage.DeleteHolon(id):await storage.DeleteHolonAsync(id);
            var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,bool>(source);result.Result=source.Result!=null;return result;
        }
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{state="before-owned-delete-alias",id,name,legacy,key,synchronous,soft,lost}));
            Assert.IsFalse((await provider.SaveHolonAsync(new Holon{Id=id,Name=name,IsActive=true},saveChildren:false)).IsError);
            provider.Commits=0;provider.LoseNextCommit=lost;
            var result=await Delete();var after=await Read(native,"OASIS_HOLONS",id);
            Assert.AreEqual(1,provider.Commits,"Observe the original commit once; never replay a lost response.");
            if(soft){
                Assert.IsNotNull(after);using var parsed=JsonDocument.Parse(after);Assert.AreEqual(id,parsed.RootElement.GetProperty("Id").GetGuid());
                Assert.AreEqual(name,parsed.RootElement.GetProperty("Name").GetString());Assert.IsFalse(parsed.RootElement.GetProperty("IsActive").GetBoolean());
                Assert.AreNotEqual(DateTime.MinValue,parsed.RootElement.GetProperty("DeletedDate").GetDateTime());
            }else Assert.IsNull(after);
            if(result.IsError!=lost||result.IsDeleted==lost||result.DeletedCount!=(lost?0:1)||result.IsSaved||result.SavedCount!=0)
                bad.Add($"first acknowledgement: error={result.IsError},deleted={result.IsDeleted},count={result.DeletedCount}");
            Assert.AreEqual(lost,NativeMutationReconciliation.IsRequired(result.Exception));
            if(lost){Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(result.Exception);Assert.AreSame(provider.Lost,result.Exception.InnerException);}
            if(soft&&!lost){
                var repeat=await Delete();
                Assert.AreEqual(1,provider.Commits);Assert.AreEqual(after,await Read(native,"OASIS_HOLONS",id));
                if(repeat.IsError||!repeat.IsDeleted||repeat.DeletedCount!=0)bad.Add("existing tombstone must acknowledge state without claiming another mutation");
            }
            if(after!=null){
                await using var cleanup=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",native){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=after;
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());
            }
            Assert.IsNull(await Read(native,"OASIS_HOLONS",id));File.Delete(journal);
        }
        finally{await provider.DeActivateProviderAsync();}
        Assert.AreEqual(0,bad.Count,string.Join(Environment.NewLine,bad));
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeDetailDeleteInterfaceAliasesPreserveProfileAndOtherSelectors(bool usernameAsync,bool soft)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,"detail-alias.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var name="owned-detail-alias-"+id.ToString("N");
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};IOASISStorageProvider storage=provider;
        await using var native=new OracleConnection(connection);await native.OpenAsync();
        Assert.IsNull(await Read(native,"OASIS_AVATARS",id));Assert.IsNull(await Read(native,"OASIS_AVATAR_DETAILS",id));
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{state="before-owned-detail-alias",id,name,usernameAsync,soft}));
            Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar{Id=id,Username=name,Email=name+"@example.invalid"})).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true})).IsError);
            var profile=await Read(native,"OASIS_AVATARS",id);
            var result=usernameAsync?await storage.DeleteAvatarDetailByUsernameAsync(name,soft):storage.DeleteAvatarDetailByEmail(name+"@example.invalid",soft);
            var after=await Read(native,"OASIS_AVATAR_DETAILS",id);Assert.AreEqual(profile,await Read(native,"OASIS_AVATARS",id));
            if(soft){Assert.IsNotNull(after);using var parsed=JsonDocument.Parse(after);Assert.IsFalse(parsed.RootElement.GetProperty("IsActive").GetBoolean());Assert.AreNotEqual(DateTime.MinValue,parsed.RootElement.GetProperty("DeletedDate").GetDateTime());}
            else Assert.IsNull(after);
            foreach(var row in new[]{("OASIS_AVATAR_DETAILS",after),("OASIS_AVATARS",profile)})if(row.Item2!=null){
                await using var cleanup=new OracleCommand($"DELETE FROM {row.Item1} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",native){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Item2;
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(native,row.Item1,id));
            }
            File.Delete(journal);Assert.IsFalse(result.IsError);Assert.IsTrue(result.Result);Assert.IsTrue(result.IsDeleted);Assert.AreEqual(1,result.DeletedCount);
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
