using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    public static IEnumerable<object[]> AvatarProjectionCases(){
        for(var route=0;route<10;route++)foreach(var synchronous in new[]{false,true})foreach(var deleted in new[]{false,true})yield return new object[]{route,synchronous,deleted};
    }

    [DataTestMethod][DynamicData(nameof(AvatarProjectionCases),DynamicDataSourceType.Method)]
    public async Task NativeAvatarProjectionMismatchCannotBeHiddenOrRepaired(int route,bool synchronous,bool deleted)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"avatar-projection-{route}-{synchronous}-{deleted}.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var marker="owned-oracle-projection-"+id.ToString("N");
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        await using var verifier=new OracleConnection(connection);
        var observed=new Dictionary<string,string>();int? observedFlag=null;var safe=false;
        async Task Observe(){
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"}){
                var raw=await Read(verifier,table,id);if(raw==null){observed.Remove(table);if(table=="OASIS_AVATARS")observedFlag=null;continue;}
                using var document=JsonDocument.Parse(raw);Assert.AreEqual(id,document.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(marker,document.RootElement.GetProperty("Username").GetString());observed[table]=raw;
                if(table=="OASIS_AVATARS"){
                    await using var flag=new OracleCommand("SELECT IS_DELETED FROM OASIS_AVATARS WHERE ID=:Id",verifier){BindByName=true};flag.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();observedFlag=Convert.ToInt32(await flag.ExecuteScalarAsync());
                }
            }
            safe=true;
        }
        async Task Acknowledge<T>(Task<OASISResult<T>> operation){safe=false;var result=await operation;Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception),result.Exception?.ToString());await Observe();Assert.IsFalse(result.IsError,result.Exception?.ToString());}
        static OASISResult<bool> Envelope<T>(OASISResult<T> value)=>OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<T,bool>(value);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"})Assert.IsNull(await Read(verifier,table,id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,marker,route,synchronous,deleted,state="before-owned-avatar-projection-corruption"}));file.Flush(true);
            }
            var profile=new Avatar{Id=id,Username=marker,Email=marker+"@example.invalid"};
            var detail=new AvatarDetail{Id=id,Username=marker,Email=profile.Email,XP=1234,Karma=77};
            await Acknowledge(provider.SaveAvatarAsync(profile));await Acknowledge(provider.SaveAvatarDetailAsync(detail));
            if(deleted)await Acknowledge(provider.DeleteAvatarAsync(id,true));
            safe=false;
            await using(var corrupt=new OracleCommand("UPDATE OASIS_AVATARS SET IS_DELETED=:Wrong WHERE ID=:Id AND IS_DELETED=:Original AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true}){
                corrupt.Parameters.Add("Wrong",OracleDbType.Int32).Value=deleted?0:1;corrupt.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();corrupt.Parameters.Add("Original",OracleDbType.Int32).Value=observedFlag;corrupt.Parameters.Add("Expected",OracleDbType.Clob).Value=observed["OASIS_AVATARS"];Assert.AreEqual(1,await corrupt.ExecuteNonQueryAsync());
            }
            await Observe();var originals=new Dictionary<string,string>(observed);var originalFlag=observedFlag;
            safe=false;OASISResult<bool> result;
            switch(route){
                case 0:result=Envelope(synchronous?provider.SaveAvatar(profile):await provider.SaveAvatarAsync(profile));break;
                case 1:result=Envelope(synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail));break;
                case 2:result=Envelope(synchronous?provider.DeleteAvatar(id,true):await provider.DeleteAvatarAsync(id,true));break;
                case 3:result=Envelope(synchronous?provider.DeleteAvatar(id,false):await provider.DeleteAvatarAsync(id,false));break;
                case 4:case 6:result=Envelope(synchronous?provider.LoadAvatar(id,route==6?1:0):await provider.LoadAvatarAsync(id,route==6?1:0));break;
                case 5:case 7:result=Envelope(synchronous?provider.LoadAllAvatars(route==7?1:0):await provider.LoadAllAvatarsAsync(route==7?1:0));break;
                case 8:result=Envelope(synchronous?provider.LoadAvatarByUsername(marker):await provider.LoadAvatarByUsernameAsync(marker));break;
                default:result=Envelope(synchronous?provider.LoadAvatarByEmail(profile.Email):await provider.LoadAvatarByEmailAsync(profile.Email));break;
            }
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception),result.Exception?.ToString());await Observe();
            Assert.IsTrue(result.IsError,"A canonical/native lifecycle mismatch must fail explicitly, not disappear or be repaired.");Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
            Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.SavedCount);Assert.AreEqual(0,result.DeletedCount);
            Assert.AreEqual(originalFlag,observedFlag);foreach(var original in originals)Assert.AreEqual(original.Value,await Read(verifier,original.Key,id));
        }finally{
            if(safe){
                foreach(var row in observed){
                    await using var cleanup=new OracleCommand($"DELETE FROM {row.Key} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0"+(row.Key=="OASIS_AVATARS"?" AND IS_DELETED=:Flag":""),verifier){BindByName=true};
                    cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Value;if(row.Key=="OASIS_AVATARS")cleanup.Parameters.Add("Flag",OracleDbType.Int32).Value=observedFlag;
                    Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,row.Key,id));
                }
                foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"})Assert.IsNull(await Read(verifier,table,id));File.Delete(journal);
            }
            await provider.DeActivateProviderAsync();
        }
    }
}
