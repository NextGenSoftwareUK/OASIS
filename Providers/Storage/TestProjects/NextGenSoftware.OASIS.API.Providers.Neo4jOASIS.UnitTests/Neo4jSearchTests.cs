using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jSearchTests
{
    [DataTestMethod]
    [DataRow(false,"null")] [DataRow(true,"null")]
    [DataRow(false,"empty-text")] [DataRow(true,"empty-text")]
    [DataRow(false,"history")] [DataRow(true,"history")]
    public async Task InvalidOrUnsupportedSearchFailsBeforeDriverCalls(bool synchronous,string kind)
    {
        var driver=System.Reflection.DispatchProxy.Create<Neo4j.Driver.IDriver,Neo4jLifecycleTests.DriverProbe>();
        var provider=new Provider("bolt://127.0.0.1:17687","neo4j","owned",()=>driver);
        var search=kind=="null" ? null:new SearchParams {SearchOnlyForCurrentAvatar=false};
        if(kind=="empty-text") search!.SearchGroups=new List<ISearchGroupBase> {new SearchTextGroup {SearchQuery=" "}};
        var result=synchronous ? provider.Search(search!,version:kind=="history" ? 1:0)
            :await provider.SearchAsync(search!,version:kind=="history" ? 1:0);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
        if(kind=="history") Assert.IsInstanceOfType(result.Exception,typeof(NotSupportedException));
        else Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
        Assert.AreEqual(0,((Neo4jLifecycleTests.DriverProbe)driver).Verifications);
    }
}
