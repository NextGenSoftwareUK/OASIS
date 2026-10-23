using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    public static IEnumerable<object[]> V2NativeDeleteCases()
    {
        foreach(var soft in new[]{false,true})foreach(var key in new[]{false,true})
        foreach(var sync in new[]{false,true})foreach(var typed in new[]{false,true})
            yield return new object[]{soft,key,sync,typed};
    }

    [DataTestMethod][DynamicData(nameof(V2NativeDeleteCases),DynamicDataSourceType.Method)]
    public async Task NativeV2ManagerDeleteRoutesPreserveIndependentChildren(bool soft,bool key,bool synchronous,bool typed)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,"v2-delete-route.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var id=Guid.NewGuid();var child=Guid.NewGuid();var actor=Guid.NewGuid();var name="owned-v2-delete-"+id.ToString("N");
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        await using var native=new OracleConnection(connection);await native.OpenAsync();
        foreach(var identity in new[]{id,child})Assert.IsNull(await Read(native,"OASIS_HOLONS",identity));
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{state="before-owned-v2-delete-route",id,child,actor,name,soft,key,synchronous,typed}));
            var seeded=await provider.SaveHolonAsync(new Holon{Id=id,Name=name,IsActive=true},saveChildren:false);
            Assert.IsFalse(seeded.IsError,seeded.Exception?.ToString());var originalVersion=seeded.Result.Version;
            var childSaved=await provider.SaveHolonAsync(new Holon{Id=child,ParentHolonId=id,Name=name+"-child",IsActive=true},saveChildren:false);
            Assert.IsFalse(childSaved.IsError,childSaved.Exception?.ToString());
            var childBefore=(await Read(native,"OASIS_HOLONS",child))!;
            var manager=TypedOracleRoleManager(provider); // Existing isolated V2 manager/registered native provider setup.
            OASISResult<IHolon> result;
            if(typed)
            {
                var selected=synchronous
                    ?key?manager.DeleteHolon<Holon>(id.ToString(),actor,soft,ProviderType.OracleDBOASIS):manager.DeleteHolon<Holon>(id,actor,soft,ProviderType.OracleDBOASIS)
                    :key?await manager.DeleteHolonAsync<Holon>(id.ToString(),actor,soft,ProviderType.OracleDBOASIS):await manager.DeleteHolonAsync<Holon>(id,actor,soft,ProviderType.OracleDBOASIS);
                result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Holon,IHolon>(selected,new OASISResult<IHolon>(selected.Result));
            }
            else result=synchronous
                ?key?manager.DeleteHolon(id.ToString(),actor,soft,ProviderType.OracleDBOASIS):manager.DeleteHolon(id,actor,soft,ProviderType.OracleDBOASIS)
                :key?await manager.DeleteHolonAsync(id.ToString(),actor,soft,ProviderType.OracleDBOASIS):await manager.DeleteHolonAsync(id,actor,soft,ProviderType.OracleDBOASIS);
            // An error/unknown outcome preserves its original journal for inspection; never replay.
            Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsNotNull(result.Result);
            var after=await Read(native,"OASIS_HOLONS",id);var childAfter=await Read(native,"OASIS_HOLONS",child);
            var bad=new List<string>();
            if(!result.IsDeleted)bad.Add("Manager did not acknowledge deletion.");
            if(result.Result.Id!=id)bad.Add("Manager returned a different logical identity.");
            if(childAfter!=childBefore)bad.Add("Root deletion changed the independent child's exact stored JSON.");
            if(soft)
            {
                if(after==null)bad.Add("Manager soft deletion physically removed the root.");
                else
                {
                    using var tombstone=JsonDocument.Parse(after);var row=tombstone.RootElement;
                    if(row.GetProperty("DeletedByAvatarId").GetGuid()!=actor||row.GetProperty("DeletedDate").GetDateTime()==DateTime.MinValue)
                        bad.Add("Native tombstone lost the deleting actor or timestamp.");
                    if(row.GetProperty("Version").GetInt32()!=originalVersion+1)bad.Add("Soft deletion did not advance the root's own version once.");
                }
                var historical=await provider.LoadHolonAsync(id,loadChildren:false,version:originalVersion);
                if(historical.IsError||historical.Result?.Name!=name||historical.Result.IsDeleted)
                    bad.Add("Soft deletion lost or changed the original readable root version.");
            }
            else if(after!=null)bad.Add("V2 hard deletion retained a physical root/tombstone/history row.");
            var current=await provider.LoadHolonAsync(id,loadChildren:false);
            // This provider reports point-read absence as a not-found error, not an empty success.
            if(!current.IsError||current.Result!=null||current.IsLoaded||current.LoadedCount!=0||current.Exception!=null||
                current.Message?.Contains($"No holon found with ID '{id}'.",StringComparison.Ordinal)!=true)
                bad.Add("Deleted root did not produce the provider's exact unloaded/not-found point-read contract.");
            // Exact-payload cleanup precedes semantic assertions, but never cleans an unknown mutation.
            foreach(var row in new[]{(Id:id,Json:after),(Id:child,Json:childBefore)})if(row.Json!=null)
            {
                await using var cleanup=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",native){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Json;
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(native,"OASIS_HOLONS",row.Id));
            }
            File.Delete(journal);Assert.AreEqual(0,bad.Count,string.Join(Environment.NewLine,bad));
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
