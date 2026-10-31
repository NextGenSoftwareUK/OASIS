using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class BigQuerySearchTests
{
    private static SearchTextGroup Text(string text)=>new(){SearchQuery=text,SearchHolons=true,HolonType=HolonType.All};
    private static SearchParams Query(params ISearchGroupBase[] groups)=>new(){SearchOnlyForCurrentAvatar=false,SearchGroups=groups.ToList()};
    private static BigQueryCollectionTests.Rows Rows(params IHolon[] rows)=>new(){Selected=rows.Select(StoredRecordJson.Serialize).ToArray(),AvatarSelected=Array.Empty<string>()};

    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)][DataRow(false,3)][DataRow(true,3)]
    public async Task SharedGroupOperatorsAndTypesSelectExactRoots(bool sync,int kind)
    {
        var date=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
        var first=new Holon{Id=Guid.NewGuid(),Name="oak",Version=1,CreatedDate=date};var second=new Holon{Id=Guid.NewGuid(),Name="oak grove",Version=3,CreatedDate=date.AddDays(1)};
        var query=kind==0?Query(new SearchNumberGroup{SearchHolons=true,HolonType=HolonType.All,Number=2,NumberOperator=SearchOperatorType.GreaterThan}):
            kind==1?Query(new SearchDateGroup{SearchHolons=true,HolonType=HolonType.All,Date=date,DateOperator=SearchOperatorType.GreaterThan}):
            kind==2?Query(Text("oak"),new SearchTextGroup{SearchHolons=true,HolonType=HolonType.All,SearchQuery="grove",PreviousSearchGroupOperator=SearchParamGroupOperator.And}):
            Query(new SearchTextGroup{HolonType=HolonType.All,SearchQuery="grove"});
        var provider=BigQueryCollectionTests.Create(Rows(first,second));var result=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());CollectionAssert.AreEqual(new[]{second.Id},result.Result.SearchResultHolons.Select(row=>row.Id).ToArray());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);Assert.AreEqual(1,result.Result.NumberOfResults);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task QuerySnapshotPrecedesNativeRead(bool sync)
    {
        var first=new Holon{Id=Guid.NewGuid(),Name="oak"};var other=new Holon{Id=Guid.NewGuid(),Name="pine"};var group=Text("oak");var query=Query(group);var client=Rows(first,other);
        client.BeforeRead=()=>group.SearchQuery="pine";var provider=BigQueryCollectionTests.Create(client);var result=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());CollectionAssert.AreEqual(new[]{first.Id},result.Result.SearchResultHolons.Select(row=>row.Id).ToArray());
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RecursiveParentScopeUsesExistingStoredGraph(bool sync)
    {
        var parent=Guid.NewGuid();var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent,Name="oak"};var grandchild=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id,Name="oak"};
        var provider=BigQueryCollectionTests.Create(Rows(child,grandchild));var query=Query(Text("oak"));query.ParentId=parent;query.Recursive=true;
        var result=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());CollectionAssert.AreEqual(new[]{child.Id,grandchild.Id},result.Result.SearchResultHolons.Select(row=>row.Id).ToArray());
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task AvatarSearchUsesCommonOwnershipAndMetadataScope(bool sync,bool metadata)
    {
        var owner=Guid.NewGuid();var avatar=new Avatar{Id=Guid.NewGuid(),CreatedByAvatarId=owner,MetaData=new(){{"tag","pine"}}};var client=Rows();client.AvatarSelected=new[]{StoredRecordJson.Serialize(avatar)};
        var query=Query();if(metadata)query.FilterByMetaData=new(){{"tag","oak"}};else{query.SearchOnlyForCurrentAvatar=true;query.AvatarId=owner;}
        var provider=BigQueryCollectionTests.Create(client);var result=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(metadata?0:1,result.Result.SearchResultAvatars.Count);
    }
    [DataTestMethod][DataRow(false,false,1)][DataRow(false,true,1)][DataRow(true,false,1)][DataRow(true,true,1)]
    [DataRow(false,false,2)][DataRow(false,true,2)][DataRow(true,false,2)][DataRow(true,true,2)]
    public async Task SourceReadFailureNeverBecomesAnEmptySuccessfulSearch(bool sync,bool keepGoing,int failAt)
    {
        var cause=new IOException("Controlled search source failure.");var calls=0;var client=Rows();client.BeforeRead=()=>{if(++calls==failAt)throw cause;};var provider=BigQueryCollectionTests.Create(client);
        var result=sync?provider.Search(Query(),loadChildren:false,continueOnError:keepGoing):await provider.SearchAsync(Query(),loadChildren:false,continueOnError:keepGoing);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.AreSame(cause,result.Exception);Assert.AreEqual(failAt,calls);
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    [DataRow(false,3)][DataRow(true,3)][DataRow(false,4)][DataRow(true,4)][DataRow(false,5)][DataRow(true,5)]
    public async Task InvalidSearchRejectsBeforeNativeDispatch(bool sync,int kind)
    {
        var called=false;var client=Rows();client.BeforeRead=()=>called=true;var provider=BigQueryCollectionTests.Create(client);var query=Query();
        if(kind==1)query.SearchOnlyForCurrentAvatar=true;if(kind==3)query.MetaKeyValuePairMatchMode=(MetaKeyValuePairMatchMode)999;if(kind==4)query.SearchGroups.Add(Text(" "));
        var result=sync?provider.Search(kind==0?null!:query,loadChildren:false,maxChildDepth:kind==5?-1:0,version:kind==2?-1:0):await provider.SearchAsync(kind==0?null!:query,loadChildren:false,maxChildDepth:kind==5?-1:0,version:kind==2?-1:0);
        Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Exception);Assert.IsFalse(called);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
    }
}
