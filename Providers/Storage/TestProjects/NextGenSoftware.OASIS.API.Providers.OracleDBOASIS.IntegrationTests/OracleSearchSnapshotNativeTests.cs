using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public async Task NativeSearchRetainsRequestCapturedBeforeSuspendedSqlRead(int scenario)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"search-snapshot-{scenario}.pending.json");Assert.IsFalse(File.Exists(journal));
        var owner=Guid.NewGuid();var other=Guid.NewGuid();var parent=Guid.NewGuid();var alternateParent=Guid.NewGuid();
        var first=new Holon{Id=Guid.NewGuid(),Name="oak",Description="east",CreatedByAvatarId=owner,ParentHolonId=parent,MetaData=new(){{"zone","north"}}};
        var rows=new[]{first,new Holon{Id=Guid.NewGuid(),Name="pine",Description="oak",CreatedByAvatarId=owner,ParentHolonId=alternateParent,MetaData=new(){{"zone","south"}}},new Holon{Id=Guid.NewGuid(),Name="oak",CreatedByAvatarId=other,ParentHolonId=parent,MetaData=new(){{"zone","north"}}}};
        var group=new SearchTextGroup{SearchQuery="oak",SearchHolons=true,HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,ParentId=parent,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new List<ISearchGroupBase>{group}};
        var provider=new OracleDBOASIS(connection);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            foreach(var row in rows)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(row=>row.Id),owner,other,parent,alternateParent,scenario,state="before-owned-search-snapshot"}));file.Flush(true);}
            foreach(var row in rows){var saved=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
            var before=new Dictionary<Guid,string>();foreach(var row in rows)before[row.Id]=(await Read(verifier,"OASIS_HOLONS",row.Id))!;
            var task=provider.SearchAsync(query,loadChildren:false);var suspended=!task.IsCompleted;
            switch(scenario){case 0:query.AvatarId=other;break;case 1:group.SearchQuery="pine";query.ParentId=alternateParent;query.FilterByMetaData["zone"]="south";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=alternateParent;break;case 5:query.SearchOnlyForCurrentAvatar=false;break;}
            var result=await task;
            // Search is read-only. Verify and conditionally clean originals BEFORE
            // asserting the returned selection, so a bad selection cannot leak fixtures.
            foreach(var row in rows){
                Assert.AreEqual(before[row.Id],await Read(verifier,"OASIS_HOLONS",row.Id));
                await using var cleanup=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=before[row.Id];Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));
            }
            File.Delete(journal);
            Assert.IsTrue(suspended,"This schedule requires an observed incomplete native search; no retry or timing-proof claim otherwise.");
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.Result.NumberOfResults);Assert.AreEqual(first.Id,result.Result.SearchResultHolons.Single().Id);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
