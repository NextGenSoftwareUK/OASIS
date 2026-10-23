using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    public static IEnumerable<object[]> HolonProjectionCases()
    {
        for(var corruption=0;corruption<4;corruption++)
        for(var route=0;route<6;route++)
        foreach(var synchronous in new[]{false,true})yield return new object[]{corruption,route,synchronous};
    }

    [DataTestMethod][DynamicData(nameof(HolonProjectionCases),DynamicDataSourceType.Method)]
    public async Task NativeHolonProjectionCorruptionIsExplicitBeforeHistoryAndFiltering(int corruption,int route,bool synchronous)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"holon-projection-{corruption}-{route}-{synchronous}.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var parent=Guid.NewGuid();
        var name="owned-holon-projection-"+id.ToString("N");
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        await using var verifier=new OracleConnection(connection);
        (string Json,int Type,int Deleted,Guid Parent)? observed=null;
        async Task<(string Json,int Type,int Deleted,Guid Parent)> Observe()
        {
            await using var read=new OracleCommand("SELECT DATA_JSON,HOLON_TYPE,IS_DELETED,PARENT_HOLON_ID FROM OASIS_HOLONS WHERE ID=:Id",verifier){BindByName=true};
            read.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
            await using var reader=await read.ExecuteReaderAsync();Assert.IsTrue(await reader.ReadAsync());
            var json=reader.GetString(0);using var parsed=JsonDocument.Parse(json);
            Assert.AreEqual(id,parsed.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(name,parsed.RootElement.GetProperty("Name").GetString());
            var snapshot=(json,reader.GetInt32(1),reader.GetInt32(2),new Guid((byte[])reader.GetValue(3)));
            Assert.IsFalse(await reader.ReadAsync());return snapshot;
        }
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,parent,name,corruption,route,synchronous,state="before-owned-holon-projection"}));file.Flush(true);
            }
            var holon=new Holon{Id=id,ParentHolonId=parent,Name=name,IsActive=true};
            for(var version=0;version<2;version++){
                var save=await provider.SaveHolonAsync(holon,saveChildren:false);
                Assert.IsFalse(save.IsError,save.Exception?.ToString());
            }
            if(corruption==3){var deleted=await provider.DeleteHolonWithSoftDeleteAsync(id,true);Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());}
            observed=await Observe();
            var column=corruption==1?"HOLON_TYPE":corruption==2?"PARENT_HOLON_ID":"IS_DELETED";
            var wrongParent=Guid.NewGuid();
            await using(var corrupt=new OracleCommand($"UPDATE OASIS_HOLONS SET {column}=:Wrong WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true}){
                corrupt.Parameters.Add("Wrong",corruption==2?OracleDbType.Raw:OracleDbType.Int32).Value=corruption==2?(object)wrongParent.ToByteArray():corruption==1?observed.Value.Type+100000:corruption==3?0:1;
                corrupt.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();corrupt.Parameters.Add("Expected",OracleDbType.Clob).Value=observed.Value.Json;
                Assert.AreEqual(1,await corrupt.ExecuteNonQueryAsync());
            }
            observed=await Observe();var original=observed.Value;
            var versionSelector=route%2;var selectedParent=corruption==2?wrongParent:parent;
            OASISResult<bool> Envelope<T>(OASISResult<T> result)=>OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<T,bool>(result);
            OASISResult<bool> result=route<2
                ?Envelope(synchronous?provider.LoadHolon(id,loadChildren:false,version:versionSelector):await provider.LoadHolonAsync(id,loadChildren:false,version:versionSelector))
                :route<4?Envelope(synchronous?provider.LoadAllHolons(loadChildren:false,version:versionSelector):await provider.LoadAllHolonsAsync(loadChildren:false,version:versionSelector))
                :Envelope(synchronous?provider.LoadHolonsForParent(selectedParent,loadChildren:false,version:versionSelector):await provider.LoadHolonsForParentAsync(selectedParent,loadChildren:false,version:versionSelector));
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception),result.Exception?.ToString());
            Assert.AreEqual(original,await Observe(),"Read must not repair or alter canonical/native data.");
            // Retire only this fully observed fixture BEFORE final red/green assertions.
            await using(var cleanup=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND HOLON_TYPE=:Type AND IS_DELETED=:Deleted AND PARENT_HOLON_ID=:Parent AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true}){
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Type",OracleDbType.Int32).Value=original.Type;
                cleanup.Parameters.Add("Deleted",OracleDbType.Int32).Value=original.Deleted;cleanup.Parameters.Add("Parent",OracleDbType.Raw).Value=original.Parent.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=original.Json;
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());
            }
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));File.Delete(journal);
            Assert.IsTrue(result.IsError,"Native projection corruption must not disappear as empty/not-found or return a canonical record as if consistent.");
            Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
            Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.SavedCount);Assert.AreEqual(0,result.DeletedCount);
        }
        finally
        {
            // Unexpected outcomes remain journaled. No blanket reset or unobserved cleanup.
            await provider.DeActivateProviderAsync();
        }
    }
}
