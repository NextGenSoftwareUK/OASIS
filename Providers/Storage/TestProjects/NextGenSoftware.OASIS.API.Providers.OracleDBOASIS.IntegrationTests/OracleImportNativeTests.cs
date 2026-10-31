using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeImportAcknowledgesEveryPersistedGraphNode(bool synchronous)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"import-count-{synchronous}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var marker="owned-oracle-import-"+Guid.NewGuid().ToString("N");
        var root=new Holon{Id=Guid.NewGuid(),Name=marker+"-root"};
        var child=new Holon{Id=Guid.NewGuid(),Name=marker+"-child"};root.Children.Add(child);
        var rows=new[]{root,child};var observed=new Dictionary<Guid,string>();
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            foreach(var row in rows)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(row=>row.Id),marker,state="before-native-import"}));file.Flush(true);
            }
            var result=synchronous?provider.Import(new[]{root}):await provider.ImportAsync(new[]{root});
            Assert.IsFalse(result.IsError,result.Exception?.ToString());
            foreach(var row in rows){
                var json=await Read(verifier,"OASIS_HOLONS",row.Id);Assert.IsNotNull(json);
                using var document=JsonDocument.Parse(json);Assert.AreEqual(row.Id,document.RootElement.GetProperty("Id").GetGuid());
                Assert.AreEqual(row.Name,document.RootElement.GetProperty("Name").GetString());
                Assert.AreEqual(1,document.RootElement.GetProperty("Version").GetInt32());observed.Add(row.Id,json);
            }
            try{
                Assert.IsTrue(result.Result);Assert.IsTrue(result.IsSaved);Assert.AreEqual(2,result.SavedCount);
                Assert.AreEqual(root.Id,child.ParentHolonId);Assert.AreEqual(1,root.Version);Assert.AreEqual(1,child.Version);
            }finally{
                // Only this acknowledged operation's exact identity/name-verified payloads are disposable.
                foreach(var row in observed){
                    await using var cleanup=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                    cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=row.Key.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Value;
                    Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Key));
                }
                File.Delete(journal);
            }
        }finally{await provider.DeActivateProviderAsync();}
    }
}
