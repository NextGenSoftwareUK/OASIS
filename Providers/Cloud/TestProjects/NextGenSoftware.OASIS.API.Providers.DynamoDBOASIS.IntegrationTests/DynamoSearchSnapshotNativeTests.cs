using System.Text.Json;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    public static IEnumerable<object[]> SearchSnapshotCases(){foreach(var scenario in Enumerable.Range(0,6))foreach(var sync in new[]{false,true})yield return new object[]{scenario,sync};}
    [DataTestMethod][DynamicData(nameof(SearchSnapshotCases),DynamicDataSourceType.Method)]
    public async Task NativeSearchKeepsOriginalScopeAndNestedMasks(int scenario,bool sync)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var owner=Guid.NewGuid();var other=Guid.NewGuid();var parent=Guid.NewGuid();var alternateParent=Guid.NewGuid();
        var first=new Holon{Id=Guid.NewGuid(),Name="oak",Description="east",CreatedByAvatarId=owner,ParentHolonId=parent,MetaData=new(){{"zone","north"}}};
        var rows=new[]{first,new Holon{Id=Guid.NewGuid(),Name="pine",Description="oak",CreatedByAvatarId=owner,ParentHolonId=alternateParent,MetaData=new(){{"zone","south"}}},new Holon{Id=Guid.NewGuid(),Name="oak",CreatedByAvatarId=other,ParentHolonId=parent,MetaData=new(){{"zone","north"}}}};
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,$"search-snapshot-{scenario}-{sync}.pending.json");Assert.IsFalse(File.Exists(journal));
        var group=new SearchTextGroup{SearchQuery="oak",SearchHolons=true,HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,ParentId=parent,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new List<ISearchGroupBase>{group}};
        void Change(){switch(scenario){case 0:query.AvatarId=other;break;case 1:group.SearchQuery="pine";query.ParentId=alternateParent;query.FilterByMetaData["zone"]="south";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=alternateParent;break;case 5:query.SearchOnlyForCurrentAvatar=false;break;}}
        var provider=new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new FaultClient();using var verifier=Client();field.SetValue(provider,client);
        async Task<Dictionary<string,AttributeValue>> Read(Guid id)=>(await verifier.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true})).Item;
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);foreach(var row in rows)Assert.AreEqual(0,(await Read(row.Id)).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(row=>row.Id),owner,other,parent,alternateParent,scenario,sync,state="before-owned-search-snapshot"}));file.Flush(true);}
            foreach(var row in rows)await SavePublic(provider,row);
            var before=new Dictionary<Guid,string>();foreach(var row in rows)before[row.Id]=(await Read(row.Id))["data_json"].S;
            var writes=client.Writes;client.AfterScan=()=>{Change();return Task.CompletedTask;};
            var result=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.Result.NumberOfResults);Assert.AreEqual(first.Id,result.Result.SearchResultHolons.Single().Id);Assert.IsNull(client.AfterScan);Assert.AreEqual(writes,client.Writes);Assert.AreEqual(0,client.Transactions);
            foreach(var row in rows){var actual=(await Read(row.Id))["data_json"].S;Assert.AreEqual(before[row.Id],actual);await RemoveOwned(verifier,"OasisHolons",row.Id,actual);}File.Delete(journal);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
