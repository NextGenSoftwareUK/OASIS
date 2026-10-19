using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jParentLookupTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ParentLookupPreservesOriginalFailureEvidence(bool synchronous)
    {
        var cause = new IOException("controlled native parent read failure");
        var original = new OASISResult<IHolon> { IsError=true, ErrorCode="controlled-native-failure", Exception=cause,
            Message="original parent read", DetailedMessage="original detailed cause", IsWarning=true,
            MetaData=new Dictionary<string,string>{{"evidence","original"}} };
        var provider = new ParentProbe(original);
        var result = synchronous ? provider.LoadHolonsForParent("parent-key") : await provider.LoadHolonsForParentAsync("parent-key");
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsLoaded); Assert.IsNull(result.Result);
        Assert.AreSame(cause,result.Exception); Assert.AreEqual(original.ErrorCode,result.ErrorCode);
        Assert.AreEqual(original.Message,result.Message); Assert.AreEqual(original.DetailedMessage,result.DetailedMessage);
        Assert.IsTrue(result.IsWarning); Assert.AreEqual("original",result.MetaData["evidence"]);
        Assert.AreEqual(1,provider.Reads);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task MissingParentRetainsNotFoundClassification(bool synchronous)
    {
        var provider = new ParentProbe(new OASISResult<IHolon>{IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="absent parent"});
        var result = synchronous ? provider.LoadHolonsForParent("absent") : await provider.LoadHolonsForParentAsync("absent");
        Assert.IsTrue(result.IsError); Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
        Assert.IsNull(result.Exception); Assert.IsNull(result.Result); Assert.AreEqual(1,provider.Reads);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task EmptySuccessfulParentReplyIsNotCopiedAsSuccess(bool synchronous)
    {
        var provider = new ParentProbe(new OASISResult<IHolon>{IsLoaded=true});
        var result = synchronous ? provider.LoadHolonsForParent("invalid") : await provider.LoadHolonsForParentAsync("invalid");
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsLoaded); Assert.IsNull(result.Result);
        Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
    }

    private sealed class ParentProbe(OASISResult<IHolon> response) : Provider("bolt://127.0.0.1:1","neo4j","controlled")
    {
        public int Reads;
        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey,bool loadChildren=true,bool recursive=true,
            int maxChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        { Reads++; return Task.FromResult(response); }
    }
}
