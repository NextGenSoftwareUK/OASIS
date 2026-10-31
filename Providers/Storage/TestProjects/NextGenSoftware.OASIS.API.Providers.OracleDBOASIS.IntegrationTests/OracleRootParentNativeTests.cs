using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    public static IEnumerable<object[]> RootParentCases()
    {
        foreach(var byKey in new[]{false,true})
        foreach(var synchronous in new[]{false,true})
        foreach(var typed in new[]{false,true})
        foreach(var version in new[]{0,1})yield return new object[]{byKey,synchronous,typed,version};
    }

    [DataTestMethod][DynamicData(nameof(RootParentCases),DynamicDataSourceType.Method)]
    public async Task NativeRootParentSelectionAgreesAtLatestAndHistoricalVersions(bool byKey,bool synchronous,bool typed,int version)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"root-parent-{byKey}-{synchronous}-{typed}-{version}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var ids=Enumerable.Range(0,4).Select(_=>Guid.NewGuid()).ToArray();var foreignParent=Guid.NewGuid();
        var marker="owned-root-parent-"+ids[0].ToString("N");
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        await using var verifier=new OracleConnection(connection);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            foreach(var id in ids)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,foreignParent,marker,byKey,synchronous,typed,version,state="before-owned-root-parent"}));file.Flush(true);
            }
            var roots=ids.Select((id,index)=>new Holon{Id=id,Name=marker+"-"+index+"-v1",ParentHolonId=index==1?foreignParent:Guid.Empty,HolonType=index==3?HolonType.Quest:HolonType.Holon,IsActive=true}).ToArray();
            foreach(var root in roots){var saved=await provider.SaveHolonAsync(root,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
            roots[0].Name=marker+"-0-v2";
            var update=await provider.SaveHolonAsync(roots[0],saveChildren:false);Assert.IsFalse(update.IsError,update.Exception?.ToString());
            var deletion=await provider.DeleteHolonWithSoftDeleteAsync(ids[2],true);Assert.IsFalse(deletion.IsError,deletion.Exception?.ToString());
            var type=typed?HolonType.Holon:HolonType.All;
            var result=byKey
                ?synchronous?provider.LoadHolonsForParent(Guid.Empty.ToString(),type,loadChildren:false,version:version):await provider.LoadHolonsForParentAsync(Guid.Empty.ToString(),type,loadChildren:false,version:version)
                :synchronous?provider.LoadHolonsForParent(Guid.Empty,type,loadChildren:false,version:version):await provider.LoadHolonsForParentAsync(Guid.Empty,type,loadChildren:false,version:version);
            var returned=result.Result?.ToList();
            // Observe/retire only the four declared fixtures, never other root results.
            foreach(var id in ids)
            {
                await using var read=new OracleCommand("SELECT DATA_JSON,HOLON_TYPE,IS_DELETED,PARENT_HOLON_ID FROM OASIS_HOLONS WHERE ID=:Id",verifier){BindByName=true};
                read.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
                string json;int nativeType,nativeDeleted;byte[]? nativeParent;
                await using(var reader=await read.ExecuteReaderAsync()){
                    Assert.IsTrue(await reader.ReadAsync());json=reader.GetString(0);nativeType=reader.GetInt32(1);nativeDeleted=reader.GetInt32(2);nativeParent=reader.IsDBNull(3)?null:(byte[])reader.GetValue(3);
                    using var parsed=JsonDocument.Parse(json);Assert.AreEqual(id,parsed.RootElement.GetProperty("Id").GetGuid());StringAssert.StartsWith(parsed.RootElement.GetProperty("Name").GetString()!,marker);
                    Assert.AreEqual(id==ids[1],nativeParent!=null);if(nativeParent!=null)Assert.AreEqual(foreignParent,new Guid(nativeParent));
                    Assert.IsFalse(await reader.ReadAsync());
                }
                await using var cleanup=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND HOLON_TYPE=:Type AND IS_DELETED=:Deleted AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0 AND "+(nativeParent==null?"PARENT_HOLON_ID IS NULL":"PARENT_HOLON_ID=:Parent"),verifier){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Type",OracleDbType.Int32).Value=nativeType;cleanup.Parameters.Add("Deleted",OracleDbType.Int32).Value=nativeDeleted;cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=json;
                if(nativeParent!=null)cleanup.Parameters.Add("Parent",OracleDbType.Raw).Value=nativeParent;
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            }
            File.Delete(journal);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsNotNull(returned);
            Assert.IsTrue(returned.Any(row=>row.Id==ids[0]),"The root saved as native NULL parent must be selectable by the domain Guid.Empty parent.");
            Assert.AreEqual(marker+(version==0?"-0-v2":"-0-v1"),returned.Single(row=>row.Id==ids[0]).Name);
            Assert.IsFalse(returned.Any(row=>row.Id==ids[1]));
            Assert.AreEqual(version==1,returned.Any(row=>row.Id==ids[2]));
            Assert.AreEqual(!typed,returned.Any(row=>row.Id==ids[3]));
            foreach(var row in returned){Assert.AreEqual(Guid.Empty,row.ParentHolonId);Assert.IsFalse(row.IsDeleted);if(typed)Assert.AreEqual(HolonType.Holon,row.HolonType);}
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
