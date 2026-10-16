using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;
[TestClass]
public class SharedSearchContractTests
{
    [DataTestMethod]
    [DataRow("cycle")] [DataRow("duplicate")] [DataRow("empty")]
    public void InvalidParentGraphIsTerminalDataCorruption(string mode)
    {
        var first=new Holon {Id=Guid.NewGuid()};var second=new Holon {Id=Guid.NewGuid()};
        first.ParentHolonId=second.Id;second.ParentHolonId=first.Id;
        if(mode=="duplicate")second.Id=first.Id;
        if(mode=="empty")second.Id=Guid.Empty;
        var candidate=new Holon {Id=Guid.NewGuid(),ParentHolonId=first.Id};
        var query=new SearchParams {SearchOnlyForCurrentAvatar=false,ParentId=Guid.NewGuid(),Recursive=true};
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>StoredRecordSearch.Matches(candidate,false,query,new[] {first,second}));
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task DateAndSelectedDescriptionMaskUseStoredValues(bool synchronous)
    {
        var provider=new TestProvider(new InMemoryBackend());var date=new DateTime(2025,2,3,4,5,6,DateTimeKind.Utc);
        var row=new Holon {Id=Guid.NewGuid(),Name="unselected",Description="oak clue",CreatedDate=date};
        Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
        var text=new SearchTextGroup {SearchHolons=true,HolonType=HolonType.All,SearchQuery="oak",HolonSearchParams=new NextGenSoftware.OASIS.API.Core.Interfaces.SearchHolonParams {Description=true},PreviousSearchGroupOperator=SearchParamGroupOperator.And};
        var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase> {
            new SearchDateGroup {SearchHolons=true,HolonType=HolonType.All,Date=date,DateOperator=SearchOperatorType.EqualTo,
                HolonSearchParams=new NextGenSoftware.OASIS.API.Core.Interfaces.SearchHolonParams {CreatedDate=true}},text}};
        var found=synchronous ? provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(found.IsError,found.Message);Assert.AreEqual(row.Id,found.Result.SearchResultHolons[0].Id);
        text.SearchQuery="unselected";var excluded=await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(excluded.IsError,excluded.Message);Assert.AreEqual(0,excluded.Result.NumberOfResults);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NumericGroupsRespectOwnershipMetadataAndAndCombination(bool synchronous)
    {
        var provider=new TestProvider(new InMemoryBackend());var owner=Guid.NewGuid();
        var match=new Holon {Id=Guid.NewGuid(),Name="owned match",CreatedByAvatarId=owner,MetaData=new() {{"park","oak"}}};
        var other=new Holon {Id=Guid.NewGuid(),Name="owned mismatch",CreatedByAvatarId=owner,MetaData=new() {{"park","pine"}}};
        Assert.IsFalse((await provider.SaveHolonAsync(match,saveChildren:false)).IsError);Assert.IsFalse((await provider.SaveHolonAsync(other,saveChildren:false)).IsError);
        var query=new SearchParams {SearchOnlyForCurrentAvatar=true,AvatarId=owner,FilterByMetaData=new() {{"park","oak"}},MetaKeyValuePairMatchMode=MetaKeyValuePairMatchMode.All,
            SearchGroups=new List<ISearchGroupBase> {new SearchNumberGroup {SearchHolons=true,HolonType=HolonType.All,Number=1,NumberOperator=SearchOperatorType.EqualTo},
                new SearchTextGroup {SearchHolons=true,HolonType=HolonType.All,SearchQuery="owned",PreviousSearchGroupOperator=SearchParamGroupOperator.And}}};
        var result=synchronous ? provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(1,result.Result.NumberOfResults);Assert.AreEqual(match.Id,result.Result.SearchResultHolons[0].Id);
        query.AvatarId=Guid.NewGuid();result=await provider.SearchAsync(query,loadChildren:false);Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(0,result.Result.NumberOfResults);
    }
}
