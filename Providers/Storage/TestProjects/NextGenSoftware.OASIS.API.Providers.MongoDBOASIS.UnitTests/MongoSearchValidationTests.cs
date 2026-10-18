using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoSearchValidationTests
{
    [DataTestMethod]
    [DataRow("null",false)] [DataRow("null",true)]
    [DataRow("owner",false)] [DataRow("owner",true)]
    [DataRow("mode",false)] [DataRow("mode",true)]
    [DataRow("blank",false)] [DataRow("blank",true)]
    [DataRow("operator",false)] [DataRow("operator",true)]
    [DataRow("version",false)] [DataRow("version",true)]
    [DataRow("depth",false)] [DataRow("depth",true)]
    [DataRow("group",false)] [DataRow("group",true)]
    public async Task InvalidSearchReturnsTerminalCauseBeforeNativeActivation(string fault,bool synchronous)
    {
        var provider=new Provider("mongodb://127.0.0.1:27028/?serverSelectionTimeoutMS=100","oasis-unit-tests");
        var text=new SearchTextGroup {SearchQuery="owned",SearchHolons=true};
        var query=new SearchParams {SearchOnlyForCurrentAvatar=false,MetaKeyValuePairMatchMode=MetaKeyValuePairMatchMode.All,SearchGroups=new() {text}};
        if(fault=="owner") query.SearchOnlyForCurrentAvatar=true;
        if(fault=="mode") query.MetaKeyValuePairMatchMode=(MetaKeyValuePairMatchMode)999;
        if(fault=="blank") text.SearchQuery=" ";
        if(fault=="operator") text.PreviousSearchGroupOperator=(SearchParamGroupOperator)999;
        if(fault=="group") query.SearchGroups=new() {null!};
        var version=fault=="version" ? -1:0;var depth=fault=="depth" ? -1:0;
        var result=synchronous ? provider.Search(fault=="null" ? null!:query,maxChildDepth:depth,version:version)
            :await provider.SearchAsync(fault=="null" ? null!:query,maxChildDepth:depth,version:version);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        var expected=fault switch {"null"=>typeof(ArgumentNullException),"mode" or "version" or "depth"=>typeof(ArgumentOutOfRangeException),
            "group"=>typeof(NotSupportedException),_=>typeof(ArgumentException)};
        Assert.AreEqual(expected,result.Exception.GetType());
        Assert.IsFalse(provider.IsProviderActivated);Assert.IsNull(provider.Database);
    }
}
