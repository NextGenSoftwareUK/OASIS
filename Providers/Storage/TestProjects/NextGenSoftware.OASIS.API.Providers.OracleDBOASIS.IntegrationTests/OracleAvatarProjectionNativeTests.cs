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
        for(var route=0;route<10;route++)foreach(var synchronous in new[]{false,true})foreach(var deleted in new[]{false,true})yield return new object[]{route,synchronous,deleted,0,false};
        for(var route=0;route<12;route++)foreach(var synchronous in new[]{false,true})foreach(var corruption in new[]{1,2})foreach(var detail in new[]{false,true}){
            if(route>=10&&!detail)continue;yield return new object[]{route,synchronous,false,corruption,detail};
        }
    }

    [DataTestMethod][DynamicData(nameof(AvatarProjectionCases),DynamicDataSourceType.Method)]
    public async Task NativeAvatarProjectionMismatchCannotBeHiddenOrRepaired(int route,bool synchronous,bool deleted,int corruption,bool detailProjection)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"avatar-projection-{route}-{synchronous}-{deleted}-{corruption}-{detailProjection}.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var marker="owned-oracle-projection-"+id.ToString("N");
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        await using var verifier=new OracleConnection(connection);
        var observed=new Dictionary<string,string>();var indexes=new Dictionary<string,(string Username,string Email,int? Flag)>();var safe=false;
        async Task Observe(){
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"}){
                var raw=await Read(verifier,table,id);if(raw==null){observed.Remove(table);indexes.Remove(table);continue;}
                using var document=JsonDocument.Parse(raw);Assert.AreEqual(id,document.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(marker,document.RootElement.GetProperty("Username").GetString());observed[table]=raw;
                await using var native=new OracleCommand($"SELECT USERNAME,EMAIL{(table=="OASIS_AVATARS"?",IS_DELETED":"")} FROM {table} WHERE ID=:Id",verifier){BindByName=true};native.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
                await using var reader=await native.ExecuteReaderAsync();Assert.IsTrue(await reader.ReadAsync());indexes[table]=(reader.GetString(0),reader.GetString(1),table=="OASIS_AVATARS"?reader.GetInt32(2):null);
            }
            safe=true;
        }
        async Task Acknowledge<T>(Task<OASISResult<T>> operation){safe=false;var result=await operation;Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception),result.Exception?.ToString());await Observe();Assert.IsFalse(result.IsError,result.Exception?.ToString());}
        static OASISResult<bool> Envelope<T>(OASISResult<T> value)=>OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<T,bool>(value);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"})Assert.IsNull(await Read(verifier,table,id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,marker,route,synchronous,deleted,corruption,detailProjection,state="before-owned-avatar-projection-corruption"}));file.Flush(true);
            }
            var profile=new Avatar{Id=id,Username=marker,Email=marker+"@example.invalid"};
            var detail=new AvatarDetail{Id=id,Username=marker,Email=profile.Email,XP=1234,Karma=77};
            await Acknowledge(provider.SaveAvatarAsync(profile));await Acknowledge(provider.SaveAvatarDetailAsync(detail));
            if(deleted)await Acknowledge(provider.DeleteAvatarAsync(id,true));
            safe=false;
            var target=detailProjection?"OASIS_AVATAR_DETAILS":"OASIS_AVATARS";var column=corruption==0?"IS_DELETED":corruption==1?"USERNAME":"EMAIL";
            var wrong=marker+"-wrong"+(corruption==2?"@example.invalid":"");
            await using(var corrupt=new OracleCommand($"UPDATE {target} SET {column}=:Wrong WHERE ID=:Id AND USERNAME=:Username AND EMAIL=:Email AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0"+(detailProjection?"":" AND IS_DELETED=:Original"),verifier){BindByName=true}){
                corrupt.Parameters.Add("Wrong",corruption==0?OracleDbType.Int32:OracleDbType.Varchar2).Value=corruption==0?(object)(deleted?0:1):wrong;
                corrupt.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();corrupt.Parameters.Add("Username",OracleDbType.Varchar2).Value=indexes[target].Username;corrupt.Parameters.Add("Email",OracleDbType.Varchar2).Value=indexes[target].Email;
                if(!detailProjection)corrupt.Parameters.Add("Original",OracleDbType.Int32).Value=indexes[target].Flag;corrupt.Parameters.Add("Expected",OracleDbType.Clob).Value=observed[target];Assert.AreEqual(1,await corrupt.ExecuteNonQueryAsync());
            }
            await Observe();var originals=new Dictionary<string,string>(observed);var originalIndexes=new Dictionary<string,(string Username,string Email,int? Flag)>(indexes);
            safe=false;OASISResult<bool> result;
            switch(route){
                case 0:result=Envelope(synchronous?provider.SaveAvatar(profile):await provider.SaveAvatarAsync(profile));break;
                case 1:result=Envelope(synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail));break;
                case 2:result=Envelope(synchronous?provider.DeleteAvatar(id,true):await provider.DeleteAvatarAsync(id,true));break;
                case 3:result=Envelope(synchronous?provider.DeleteAvatar(id,false):await provider.DeleteAvatarAsync(id,false));break;
                case 4:case 6:result=detailProjection?Envelope(synchronous?provider.LoadAvatarDetail(id,route==6?1:0):await provider.LoadAvatarDetailAsync(id,route==6?1:0)):Envelope(synchronous?provider.LoadAvatar(id,route==6?1:0):await provider.LoadAvatarAsync(id,route==6?1:0));break;
                case 5:case 7:result=detailProjection?Envelope(synchronous?provider.LoadAllAvatarDetails(route==7?1:0):await provider.LoadAllAvatarDetailsAsync(route==7?1:0)):Envelope(synchronous?provider.LoadAllAvatars(route==7?1:0):await provider.LoadAllAvatarsAsync(route==7?1:0));break;
                case 8:var username=corruption==1?wrong:marker;result=detailProjection?Envelope(synchronous?provider.LoadAvatarDetailByUsername(username):await provider.LoadAvatarDetailByUsernameAsync(username)):Envelope(synchronous?provider.LoadAvatarByUsername(username):await provider.LoadAvatarByUsernameAsync(username));break;
                case 9:var email=corruption==2?wrong:profile.Email;result=detailProjection?Envelope(synchronous?provider.LoadAvatarDetailByEmail(email):await provider.LoadAvatarDetailByEmailAsync(email)):Envelope(synchronous?provider.LoadAvatarByEmail(email):await provider.LoadAvatarByEmailAsync(email));break;
                default:result=Envelope(synchronous?provider.DeleteAvatarDetail(id,route==10):await provider.DeleteAvatarDetailAsync(id,route==10));break;
            }
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception),result.Exception?.ToString());await Observe();
            Assert.IsTrue(result.IsError,"A canonical/native lifecycle mismatch must fail explicitly, not disappear or be repaired.");Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
            Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.SavedCount);Assert.AreEqual(0,result.DeletedCount);
            foreach(var original in originalIndexes)Assert.AreEqual(original.Value,indexes.GetValueOrDefault(original.Key));foreach(var original in originals)Assert.AreEqual(original.Value,await Read(verifier,original.Key,id));
        }finally{
            if(safe){
                foreach(var row in observed){
                    await using var cleanup=new OracleCommand($"DELETE FROM {row.Key} WHERE ID=:Id AND USERNAME=:Username AND EMAIL=:Email AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0"+(row.Key=="OASIS_AVATARS"?" AND IS_DELETED=:Flag":""),verifier){BindByName=true};
                    cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Value;cleanup.Parameters.Add("Username",OracleDbType.Varchar2).Value=indexes[row.Key].Username;cleanup.Parameters.Add("Email",OracleDbType.Varchar2).Value=indexes[row.Key].Email;if(row.Key=="OASIS_AVATARS")cleanup.Parameters.Add("Flag",OracleDbType.Int32).Value=indexes[row.Key].Flag;
                    Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,row.Key,id));
                }
                foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"})Assert.IsNull(await Read(verifier,table,id));File.Delete(journal);
            }
            await provider.DeActivateProviderAsync();
        }
    }
}
