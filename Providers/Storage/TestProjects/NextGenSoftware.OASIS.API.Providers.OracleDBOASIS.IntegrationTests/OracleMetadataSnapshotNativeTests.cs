using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    private sealed class MetadataReadProbe(string connection):OracleDBOASIS(connection)
    {
        public Action? AfterRead;public int Reads;
        protected override async Task<List<T>> ReadRecordCollectionAsync<T>(string table,int version,bool exportAllVersions=false)
        {var rows=await base.ReadRecordCollectionAsync<T>(table,version,exportAllVersions);Reads++;AfterRead?.Invoke();return rows;}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeMetadataReadCapturesPairsBeforeRealSqlReadCompletes(bool sync)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"metadata-snapshot-{sync}.pending.json");Assert.IsFalse(File.Exists(journal));
        var first=new Holon{Id=Guid.NewGuid(),Name="owned metadata snapshot",IsActive=true};var other=new Holon{Id=Guid.NewGuid(),Name="owned other metadata",IsActive=true};
        var key="metadata-snapshot-"+first.Id.ToString("N");first.MetaData[key]="park";other.MetaData[key]="city";
        var provider=new MetadataReadProbe(connection){IsVersionControlEnabled=true};await using var verifier=new OracleConnection(connection);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",first.Id));Assert.IsNull(await Read(verifier,"OASIS_HOLONS",other.Id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await JsonSerializer.SerializeAsync(file,new{ids=new[]{first.Id,other.Id},key,stage="before-owned-metadata-snapshot-create"});file.Flush(true);}
            foreach(var row in new[]{first,other}){Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);row.Description="second version";Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);}
            var original=new[]{(Id:first.Id,Json:await Read(verifier,"OASIS_HOLONS",first.Id)),(Id:other.Id,Json:await Read(verifier,"OASIS_HOLONS",other.Id))};
            foreach(var version in new[]{0,1})foreach(var mode in new[]{MetaKeyValuePairMatchMode.All,MetaKeyValuePairMatchMode.Any}){
                var pairs=new Dictionary<string,string>{{key,"park"}};provider.AfterRead=()=>pairs[key]="city";
                var selected=sync?provider.LoadHolonsByMetaData(pairs,mode,loadChildren:false,version:version):await provider.LoadHolonsByMetaDataAsync(pairs,mode,loadChildren:false,version:version);
                Assert.IsFalse(selected.IsError,selected.Exception?.ToString());Assert.IsTrue(selected.IsLoaded);Assert.AreEqual(1,selected.LoadedCount);Assert.AreEqual(first.Id,selected.Result.Single().Id);Assert.AreEqual(version==0?2:1,selected.Result.Single().Version);
                Assert.AreEqual("city",pairs[key],"Caller mutation actually occurred after the real SQL read.");
            }
            Assert.AreEqual(4,provider.Reads);provider.AfterRead=null;
            foreach(var row in original)Assert.AreEqual(row.Json,await Read(verifier,"OASIS_HOLONS",row.Id),"Reads cannot rewrite canonical history.");
            await using(var transaction=verifier.BeginTransaction()){
                foreach(var row in original){await using var delete=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true,Transaction=transaction};delete.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Json;Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());}
                transaction.Commit();
            }
            foreach(var row in original)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
