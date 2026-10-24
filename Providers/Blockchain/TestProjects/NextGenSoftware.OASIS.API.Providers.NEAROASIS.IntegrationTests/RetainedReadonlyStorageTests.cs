using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class RetainedReadonlyStorageTests
{
    [TestMethod][TestCategory("RetainedReadonlyInspection")]
    public async Task RetainedSearchAndNearbyRecordsRemainQueryable()
    {
        Assert.AreEqual("http://127.0.0.1:3030",Environment.GetEnvironmentVariable("NEAROASIS_RPCENDPOINT"));
        var provider=new NEAROASIS("http://127.0.0.1:3030","oasis-near-local","oasis-near-local","test.near","test.near","");
        var all=await provider.LoadAllHolonsAsync(HolonType.All,loadChildren:false);
        Assert.IsFalse(all.IsError,$"Retained holon enumeration: {all.Message}");
        var rows=all.Result.ToArray();
        var searchRows=rows.Where(row=>row.Name?.StartsWith("search-",StringComparison.Ordinal)==true &&
            row.MetaData?.TryGetValue("search-marker",out var value)==true && value?.ToString()==row.Name[7..]).ToArray();
        Assert.IsTrue(searchRows.Length>=2,"Original search fixture records must exist; do not create replacements.");
        var selected=searchRows[0];var marker=selected.Name[7..];
        var query=new SearchParams{AvatarId=selected.CreatedByAvatarId,SearchOnlyForCurrentAvatar=true,
            FilterByMetaData=new(){{"search-marker",marker}},SearchGroups=new List<ISearchGroupBase>{
                new SearchTextGroup{SearchQuery=marker,SearchHolons=true,HolonType=HolonType.All},
                new SearchNumberGroup{Number=1,NumberOperator=SearchOperatorType.EqualTo,SearchHolons=true,
                    HolonType=HolonType.All,PreviousSearchGroupOperator=SearchParamGroupOperator.And}}};
        var owned=await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(owned.IsError,$"Retained owner/group search: {owned.Message}");
        Assert.AreEqual(owned.Result.NumberOfResults,owned.LoadedCount);
        CollectionAssert.AreEquivalent(searchRows.Where(row=>row.Name==selected.Name && row.CreatedByAvatarId==selected.CreatedByAvatarId).Select(row=>row.Id).ToArray(),
            owned.Result.SearchResultHolons.Select(row=>row.Id).ToArray());
        query.SearchOnlyForCurrentAvatar=false;
        var everyone=provider.Search(query,loadChildren:false);
        Assert.IsFalse(everyone.IsError,$"Retained unrestricted search: {everyone.Message}");
        Assert.AreEqual(everyone.Result.NumberOfResults,everyone.LoadedCount);
        CollectionAssert.AreEquivalent(searchRows.Where(row=>row.Name==selected.Name).Select(row=>row.Id).ToArray(),
            everyone.Result.SearchResultHolons.Select(row=>row.Id).ToArray());
        query.FilterByMetaData["search-marker"]="not-the-marker";
        var unmatched=await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(unmatched.IsError,$"Retained unmatched search: {unmatched.Message}");
        Assert.AreEqual(0,unmatched.Result.NumberOfResults);
        Assert.AreEqual(0,unmatched.LoadedCount);
        var metadata=await provider.LoadHolonsByMetaDataAsync("search-marker",marker,HolonType.All,loadChildren:false);
        Assert.IsFalse(metadata.IsError,$"Retained metadata query: {metadata.Message}");
        Assert.AreEqual(searchRows.Count(row=>row.Name==selected.Name),metadata.LoadedCount);
        Assert.AreEqual(metadata.Result.Count(),metadata.LoadedCount);
        var nearbyRows=rows.Where(row=>row.Name?.StartsWith("nearby-",StringComparison.Ordinal)==true).ToArray();
        Assert.IsTrue(nearbyRows.Length>0,"Original nearby records must exist; no replacement fixtures.");
        var near=provider.GetHolonsNearMe(0,0,0,HolonType.All);
        Assert.IsFalse(near.IsError,$"Retained nearby query: {near.Message}");
        Assert.AreEqual(near.Result.Count(),near.LoadedCount);
        Assert.IsTrue(nearbyRows.All(row=>near.Result.Any(found=>found.Id==row.Id)),"Original zero-coordinate nearby records must be selected.");
        var distant=provider.GetHolonsNearMe(1000000,0,1,HolonType.All);
        Assert.IsFalse(distant.IsError,$"Retained distant query: {distant.Message}");
        Assert.AreEqual(distant.Result.Count(),distant.LoadedCount);
        Assert.IsFalse(nearbyRows.Any(row=>distant.Result.Any(found=>found.Id==row.Id)),"Original zero-coordinate records must not be selected one degree away.");
    }
}
