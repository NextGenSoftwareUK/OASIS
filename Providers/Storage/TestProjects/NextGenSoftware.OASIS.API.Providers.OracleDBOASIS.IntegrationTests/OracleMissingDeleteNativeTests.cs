using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    public static IEnumerable<object[]> MissingDeleteCases()
    {
        foreach(var sync in new[]{false,true})foreach(var soft in new[]{false,true}){
            foreach(var selector in new[]{"id","key","username","email"})yield return new object[]{"avatar",selector,sync,soft};
            foreach(var selector in new[]{"id","username","email"})yield return new object[]{"detail",selector,sync,soft};
            foreach(var selector in new[]{"id","key"})yield return new object[]{"holon-helper",selector,sync,soft};
            if(!soft)foreach(var selector in new[]{"id","key"})yield return new object[]{"holon",selector,sync,soft};
        }
    }

    [DataTestMethod][DynamicData(nameof(MissingDeleteCases),DynamicDataSourceType.Method)]
    public async Task NativeMissingDeleteIsKnownAbsenceWithoutAcknowledgedMutation(string kind,string selector,bool sync,bool soft)
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        await using var admin=new OracleConnection(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));await admin.OpenAsync();
        var connection=await RestrictedConnectionAsync(admin,work);
        await using var native=new OracleConnection(connection);await native.OpenAsync();
        async Task Empty(){
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS","OASIS_WRITE_RECEIPTS","OASIS_WRITE_PREPARATIONS","OASIS_WRITE_OPERATION_LOCKS"}){
                await using var query=new OracleCommand($"SELECT COUNT(*) FROM {table}",native);
                Assert.AreEqual(0,Convert.ToInt32(await query.ExecuteScalarAsync()),"Restricted scope must be empty; preserve unrelated state.");
            }
        }
        await Empty();var id=Guid.NewGuid();var name="absent-delete-"+id.ToString("N");
        var provider=new DeleteAliasCommitOwner(connection);IOASISStorageProvider storage=provider;
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            OASISResult<bool> result;
            if(kind=="avatar")result=selector switch{
                "id"=>sync?storage.DeleteAvatar(id,soft):await storage.DeleteAvatarAsync(id,soft),
                "key"=>sync?storage.DeleteAvatar(id.ToString(),soft):await storage.DeleteAvatarAsync(id.ToString(),soft),
                "username"=>sync?storage.DeleteAvatarByUsername(name,soft):await storage.DeleteAvatarByUsernameAsync(name,soft),
                _=>sync?storage.DeleteAvatarByEmail(name+"@example.invalid",soft):await storage.DeleteAvatarByEmailAsync(name+"@example.invalid",soft)};
            else if(kind=="detail")result=selector switch{
                "id"=>sync?storage.DeleteAvatarDetail(id,soft):await storage.DeleteAvatarDetailAsync(id,soft),
                "username"=>sync?storage.DeleteAvatarDetailByUsername(name,soft):await storage.DeleteAvatarDetailByUsernameAsync(name,soft),
                _=>sync?storage.DeleteAvatarDetailByEmail(name+"@example.invalid",soft):await storage.DeleteAvatarDetailByEmailAsync(name+"@example.invalid",soft)};
            else if(kind=="holon-helper")result=selector=="id"
                ?sync?provider.DeleteHolonWithSoftDelete(id,soft):await provider.DeleteHolonWithSoftDeleteAsync(id,soft)
                :sync?provider.DeleteHolonWithSoftDelete(id.ToString(),soft):await provider.DeleteHolonWithSoftDeleteAsync(id.ToString(),soft);
            else{
                var raw=selector=="id"?sync?storage.DeleteHolon(id):await storage.DeleteHolonAsync(id)
                    :sync?storage.DeleteHolon(id.ToString()):await storage.DeleteHolonAsync(id.ToString());
                result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,bool>(raw);result.Result=raw.IsDeleted;
            }
            // Check independent native state before result assertions; no cleanup/replay is needed
            // for targets proven absent before the call. Unexpected rows remain for inspection.
            await Empty();Assert.AreEqual(0,provider.Commits);
            Assert.IsTrue(result.IsError);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
            Assert.IsNull(result.Exception);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.DeletedCount);
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
