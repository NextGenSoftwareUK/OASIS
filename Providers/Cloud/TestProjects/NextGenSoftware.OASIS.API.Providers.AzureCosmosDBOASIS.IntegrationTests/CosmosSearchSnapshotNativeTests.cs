using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public async Task NativeSearchRetainsCapturedScopeDuringCosmosRead(int scenario)
    {
        var owner=Guid.NewGuid();var other=Guid.NewGuid();var parent=Guid.NewGuid();var alternateParent=Guid.NewGuid();
        var first=new Holon{Id=Guid.NewGuid(),Name="oak",Description="east",CreatedByAvatarId=owner,ParentHolonId=parent,MetaData=new(){{"zone","north"}}};
        var rows=new[]{first,new Holon{Id=Guid.NewGuid(),Name="pine",Description="oak",CreatedByAvatarId=owner,ParentHolonId=alternateParent,MetaData=new(){{"zone","south"}}},new Holon{Id=Guid.NewGuid(),Name="oak",CreatedByAvatarId=other,ParentHolonId=parent,MetaData=new(){{"zone","north"}}}};
        var journal=$"C:/Source/OASIS/TestResults/CosmosDBCurrent/search-snapshot-{scenario}.pending.json";Assert.IsFalse(File.Exists(journal));
        var container=_client.GetContainer(DatabaseName,"holonItems");
        async Task<(string Json,string Etag)> Read(Guid id){using var response=await container.ReadItemStreamAsync(id.ToString(),new PartitionKey(id.ToString()));response.EnsureSuccessStatusCode();using var reader=new StreamReader(response.Content);return(await reader.ReadToEndAsync(),response.Headers.ETag);}
        foreach(var row in rows){using var absent=await container.ReadItemStreamAsync(row.Id.ToString(),new PartitionKey(row.Id.ToString()));Assert.AreEqual(HttpStatusCode.NotFound,absent.StatusCode);}
        using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(row=>row.Id),owner,other,parent,alternateParent,scenario,state="before-owned-cosmos-search-snapshot"}));file.Flush(true);}
        foreach(var row in rows){var saved=await _provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
        var before=new Dictionary<Guid,(string Json,string Etag)>();foreach(var row in rows)before[row.Id]=await Read(row.Id);
        var group=new SearchTextGroup{SearchQuery="oak",SearchHolons=true,HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,ParentId=parent,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new List<ISearchGroupBase>{group}};
        var task=_provider.SearchAsync(query,loadChildren:false);var suspended=!task.IsCompleted;
        switch(scenario){case 0:query.AvatarId=other;break;case 1:group.SearchQuery="pine";query.ParentId=alternateParent;query.FilterByMetaData["zone"]="south";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=alternateParent;break;case 5:query.SearchOnlyForCurrentAvatar=false;break;}
        var result=await task;
        foreach(var row in rows){var actual=await Read(row.Id);Assert.AreEqual(before[row.Id].Json,actual.Json);Assert.AreEqual(before[row.Id].Etag,actual.Etag);using var deleted=await container.DeleteItemStreamAsync(row.Id.ToString(),new PartitionKey(row.Id.ToString()),new ItemRequestOptions{IfMatchEtag=actual.Etag});Assert.AreEqual(HttpStatusCode.NoContent,deleted.StatusCode);using var absent=await container.ReadItemStreamAsync(row.Id.ToString(),new PartitionKey(row.Id.ToString()));Assert.AreEqual(HttpStatusCode.NotFound,absent.StatusCode);}
        await container.ReadContainerAsync();File.Delete(journal);
        Assert.IsTrue(suspended,"Native search must be observed incomplete before editing its caller request; no retry on an already completed schedule.");Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.Result.NumberOfResults);Assert.AreEqual(first.Id,result.Result.SearchResultHolons.Single().Id);
    }
}
