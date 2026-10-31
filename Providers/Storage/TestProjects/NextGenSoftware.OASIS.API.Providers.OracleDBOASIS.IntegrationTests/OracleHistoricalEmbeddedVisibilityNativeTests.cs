using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.ProviderTestSupport;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeHistoricalEmbeddedForeignBranchesArePrunedWithoutMixingVersions(bool sync)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"embedded-export-{sync}.pending.json");
        Assert.IsFalse(File.Exists(journal));var owner=Guid.NewGuid();var id=Guid.NewGuid();var child=Guid.NewGuid();var foreign=Guid.NewGuid();
        var provider=new OracleDBOASIS(connection);await using var verifier=new OracleConnection(connection);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,owner,child,foreign,state="before-owned-legacy-embedded-export"}));file.Flush(true);}
            // Current row belongs to a different actor. The owned historic roots contain
            // their stored legacy child payloads, not separately versioned current rows.
            var current=new Holon{Id=id,CreatedByAvatarId=Guid.NewGuid(),Version=3,Name="owned legacy embedded export current"};
            var saved=await provider.SaveHolonAsync(current,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.AreEqual(3,current.Version);
            var before=(await Read(verifier,"OASIS_HOLONS",id))!;var document=OasisJson.ParseStoredObject(before);var history=new JArray();
            // Canonical writers intentionally omit navigation objects. This fixture
            // represents an externally persisted legacy payload, so add them explicitly.
            foreach(var version in new[]{1,2}){
                var root=new Holon{Id=id,CreatedByAvatarId=owner,Version=version,Name="owned embedded history v"+version};
                root.Children.Add(new Holon{Id=child,ParentHolonId=id,CreatedByAvatarId=owner,Version=version==1?4:9});
                var hidden=new Holon{Id=foreign,ParentHolonId=id,CreatedByAvatarId=Guid.NewGuid(),IsPublic=true};
                hidden.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=foreign,CreatedByAvatarId=owner});root.Children.Add(hidden);
                var legacy=AllVersionExportVisibilityAssertions.Legacy(root);
                Assert.AreEqual(2,((JArray)legacy[nameof(Holon.Children)]!).Count,"Fixture must contain its intended legacy children before dispatch.");
                Assert.AreEqual(2,OasisJson.DeserializeStored<Holon>(legacy.ToString(Newtonsoft.Json.Formatting.None)).Children.Count,"Validate the real restricted decoder before injecting legacy JSON.");
                history.Add(legacy);
            }
            document[EmbeddedRecordHistory.PropertyName]=new JObject{{"schema",1},{"records",history}};
            var original=document.ToString(Newtonsoft.Json.Formatting.None);
            await using(var inject=new OracleCommand("UPDATE OASIS_HOLONS SET DATA_JSON=:Payload WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true}){
                inject.Parameters.Add("Payload",OracleDbType.Clob).Value=original;inject.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
                inject.Parameters.Add("Expected",OracleDbType.Clob).Value=before;Assert.AreEqual(1,await inject.ExecuteNonQueryAsync());}
            var exported=sync?provider.ExportAllDataForAvatarById(owner,-1):await provider.ExportAllDataForAvatarByIdAsync(owner,-1);
            Assert.IsFalse(exported.IsError,exported.Exception?.ToString());Assert.AreEqual(2,exported.LoadedCount);
            var roots=exported.Result.Where(row=>row.Id==id).OrderBy(row=>row.Version).ToArray();Assert.AreEqual(2,roots.Length);
            for(var index=0;index<2;index++){
                CollectionAssert.AreEqual(new[]{child},roots[index].Children.Select(row=>row.Id).ToArray());
                Assert.AreEqual(index==0?4:9,roots[index].Children.Single().Version);
                var rejected=await provider.SaveHolonAsync(roots[index]);Assert.IsTrue(rejected.IsError);Assert.IsInstanceOfType<InvalidOperationException>(rejected.Exception);
            }
            var unscoped=await provider.ExportAllAsync(-1);Assert.IsFalse(unscoped.IsError,unscoped.Exception?.ToString());
            Assert.IsTrue(unscoped.Result.Where(row=>row.Id==id&&row.Version<3).All(row=>row.Children.Count()==2));
            Assert.AreEqual(original,await Read(verifier,"OASIS_HOLONS",id));
            await using(var remove=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true}){
                remove.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();remove.Parameters.Add("Expected",OracleDbType.Clob).Value=original;Assert.AreEqual(1,await remove.ExecuteNonQueryAsync());}
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
