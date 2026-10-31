using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public async Task NativeSearchRetainsCapturedScopeDuringBlobRead(int scenario)
    {
        RequireLive();var owner=Guid.NewGuid();var other=Guid.NewGuid();var parent=Guid.NewGuid();var alternateParent=Guid.NewGuid();
        var first=new Holon{Id=Guid.NewGuid(),Name="oak",Description="east",CreatedByAvatarId=owner,ParentHolonId=parent,MetaData=new(){{"zone","north"}}};
        var rows=new[]{first,new Holon{Id=Guid.NewGuid(),Name="pine",Description="oak",CreatedByAvatarId=owner,ParentHolonId=alternateParent,MetaData=new(){{"zone","south"}}},new Holon{Id=Guid.NewGuid(),Name="oak",CreatedByAvatarId=other,ParentHolonId=parent,MetaData=new(){{"zone","north"}}}};
        foreach(var row in rows){var saved=await _provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
        var container=_service.GetBlobContainerClient(_containers[0]);
        var blobs=rows.ToDictionary(row=>row.Id,row=>container.GetBlobClient(row.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS]));
        var before=new Dictionary<Guid,BlobDownloadResult>();foreach(var row in rows)before[row.Id]=(await blobs[row.Id].DownloadContentAsync()).Value;
        var group=new SearchTextGroup{SearchQuery="oak",SearchHolons=true,HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,ParentId=parent,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new List<ISearchGroupBase>{group}};
        var task=_provider.SearchAsync(query,loadChildren:false);var suspended=!task.IsCompleted;
        switch(scenario){case 0:query.AvatarId=other;break;case 1:group.SearchQuery="pine";query.ParentId=alternateParent;query.FilterByMetaData["zone"]="south";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=alternateParent;break;case 5:query.SearchOnlyForCurrentAvatar=false;break;}
        var result=await task;
        foreach(var row in rows){var actual=(await blobs[row.Id].DownloadContentAsync()).Value;Assert.AreEqual(before[row.Id].Content.ToString(),actual.Content.ToString());Assert.AreEqual(before[row.Id].Details.ETag,actual.Details.ETag);var deleted=await blobs[row.Id].DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=actual.Details.ETag});Assert.IsTrue(deleted.Status>=200&&deleted.Status<300);Assert.IsFalse((await blobs[row.Id].ExistsAsync()).Value);}
        Assert.IsTrue(suspended,"Native search must be observed incomplete before editing its caller request; no retry on completed schedules.");Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.Result.NumberOfResults);Assert.AreEqual(first.Id,result.Result.SearchResultHolons.Single().Id);
    }
}
