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
    [DataTestMethod]
    [DataRow(0,false,true)][DataRow(1,false,true)][DataRow(2,false,true)][DataRow(3,false,true)][DataRow(4,false,true)]
    [DataRow(0,true,true)][DataRow(1,true,true)][DataRow(2,true,true)][DataRow(3,true,true)][DataRow(4,true,true)]
    [DataRow(0,false,false)][DataRow(1,false,false)][DataRow(2,false,false)][DataRow(3,false,false)][DataRow(4,false,false)]
    [DataRow(0,true,false)][DataRow(1,true,false)][DataRow(2,true,false)][DataRow(3,true,false)][DataRow(4,true,false)]
    [DataRow(5,false,true)][DataRow(5,true,true)][DataRow(5,false,false)][DataRow(5,true,false)]
    public async Task NativeLifecycleGuardsRejectStaleAndRelatedActiveSaves(int mode,bool synchronous,bool history)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"lifecycle-guard-{mode}-{synchronous}-{history}.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var name="owned-oracle-lifecycle-"+id.ToString("N");
        var targets=mode==2||mode==5?new[]{"OASIS_HOLONS"}:new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"};var operation=Guid.NewGuid();
        var observed=new Dictionary<string,string>();var safe=false;
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=history};
        await using var verifier=new OracleConnection(connection);
        async Task Observe(){
            foreach(var table in targets){
                var json=await Read(verifier,table,id);if(json==null){observed.Remove(table);continue;}
                using var parsed=JsonDocument.Parse(json);Assert.AreEqual(id,parsed.RootElement.GetProperty("Id").GetGuid());
                Assert.AreEqual(name,parsed.RootElement.GetProperty(table=="OASIS_HOLONS"?"Name":"Username").GetString());observed[table]=json;
            }
            safe=true;
        }
        async Task Acknowledge<T>(Task<OASISResult<T>> operation){
            safe=false;var result=await operation;
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception),result.Exception?.ToString());
            await Observe();Assert.IsFalse(result.IsError,result.Exception?.ToString());
        }
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            foreach(var table in targets)Assert.IsNull(await Read(verifier,table,id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,targets,name,operation,state="before-native-lifecycle-guard"}));file.Flush(true);
            }
            var profile=new Avatar{Id=id,Username=name,Email=name+"@example.invalid"};
            var detail=new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid",XP=4321,Karma=77};
            var holon=new Holon{Id=id,Name=name};
            if(mode==2||mode==5){await Acknowledge(provider.SaveHolonAsync(holon,saveChildren:false));await Acknowledge(provider.DeleteHolonWithSoftDeleteAsync(id,true));}
            else{
                if(mode!=4)await Acknowledge(provider.SaveAvatarAsync(profile));
                if(mode!=3)await Acknowledge(provider.SaveAvatarDetailAsync(detail));
                if(mode==4)await Acknowledge(provider.DeleteAvatarDetailAsync(id,true));
                else await Acknowledge(provider.DeleteAvatarAsync(id,true));
            }
            var tombstones=new Dictionary<string,string>(observed);IHolonBase candidate=mode==0||mode==4?profile:mode==1||mode==3?detail:holon;var originalVersion=candidate.Version;
            safe=false;
            OASISResult<IHolonBase> result;
            if(mode==0||mode==4){var saved=synchronous?provider.SaveAvatar(profile):await provider.SaveAvatarAsync(profile);result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IHolonBase>(saved);}
            else if(mode==1||mode==3){var saved=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatarDetail,IHolonBase>(saved);}
            else if(mode==5){
                var current=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>(tombstones["OASIS_HOLONS"]);
                var authority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS;
                await AssertReceiptAbsent(verifier,operation);
                var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,Guid.NewGuid(),authority,new[]{new HolonWriteCondition(id,CosmicConcurrency.GetProviderRevisionId(current,authority))}),ConditionalHolonOperation.Save,new[]{holon},saveChildren:false);
                var saved=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
                Assert.IsFalse(NativeMutationReconciliation.IsRequired(saved.Exception),saved.Exception?.ToString());
                await AssertReceiptAbsent(verifier,operation);
                await using var guard=new OracleCommand("SELECT COUNT(*) FROM OASIS_WRITE_OPERATION_LOCKS WHERE OPERATION_ID=:Id",verifier){BindByName=true};guard.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();Assert.AreEqual(0,Convert.ToInt32(await guard.ExecuteScalarAsync()));
                result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolonBase>(saved);
            }
            else{var saved=synchronous?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IHolonBase>(saved);}
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception),result.Exception?.ToString());await Observe();
            Assert.IsTrue(result.IsError,"Ordinary active save cannot revive a committed tombstone or its deleted identity pair.");
            Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);
            Assert.AreEqual(originalVersion,candidate.Version);
            foreach(var table in targets){Assert.AreEqual(tombstones.GetValueOrDefault(table),await Read(verifier,table,id),"Rejected save must preserve exact committed tombstone data.");}
        }finally{
            if(safe){
                foreach(var row in observed){
                    await using var cleanup=new OracleCommand($"DELETE FROM {row.Key} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                    cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Value;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());
                }
                foreach(var table in targets)Assert.IsNull(await Read(verifier,table,id));File.Delete(journal);
            }
            await provider.DeActivateProviderAsync();
        }
    }
}
