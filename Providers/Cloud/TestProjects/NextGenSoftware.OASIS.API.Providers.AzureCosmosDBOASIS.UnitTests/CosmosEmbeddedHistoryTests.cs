using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosEmbeddedHistoryTests
{
    [DataTestMethod][DataRow("avatar")][DataRow("detail")][DataRow("holon")]
    public async Task StagingUsesSharedVersionsWithoutPublishingCallerOrMutatingPrevious(string kind)
    {
        IHolonBase proposed=kind=="avatar" ? new Avatar() : kind=="detail" ? new AvatarDetail{XP=123} : new Holon();
        Func<string,IHolonBase> decode=kind=="avatar" ? json=>OasisJson.DeserializeStored<Avatar>(json)
            : kind=="detail" ? json=>OasisJson.DeserializeStored<AvatarDetail>(json) : json=>OasisJson.DeserializeStored<Holon>(json);
        proposed.Id=Guid.NewGuid(); proposed.MetaData=new Dictionary<string,object>{{"history-marker","first"}};
        var first=await CosmosEmbeddedHistory.StageAsync(proposed,null!,decode);
        var original=(JObject)first.DeepClone();
        proposed.MetaData["history-marker"]="second";
        var second=await CosmosEmbeddedHistory.StageAsync(proposed,first,decode);
        Assert.IsTrue(JToken.DeepEquals(original,first)); Assert.AreEqual(0,proposed.Version); Assert.AreEqual(Guid.Empty,proposed.VersionId);
        var old=await CosmosEmbeddedHistory.ReadAsync(proposed.Id,second,1,decode);
        var latest=await CosmosEmbeddedHistory.ReadAsync(proposed.Id,second,0,decode);
        Assert.AreEqual("first",old.MetaData["history-marker"].ToString()); Assert.AreEqual("second",latest.MetaData["history-marker"].ToString()); Assert.AreEqual(2,latest.Version);
        Assert.AreEqual(old.VersionId,latest.PreviousVersionId); Assert.AreNotEqual(Guid.Empty,old.VersionId);
        Assert.IsNull(await CosmosEmbeddedHistory.ReadAsync(proposed.Id,second,3,decode));
        if(kind=="detail") Assert.AreEqual(123L,((IAvatarDetail)old).XP);
    }

    [DataTestMethod][DataRow("schema")][DataRow("duplicate")][DataRow("identity")][DataRow("future")][DataRow("native")]
    public async Task CorruptArchivesFailInsteadOfReturningLatestOrDroppingHistory(string fault)
    {
        var source=new Holon{Id=Guid.NewGuid()};
        var first=await CosmosEmbeddedHistory.StageAsync(source,null!,Decode);
        var second=await CosmosEmbeddedHistory.StageAsync(source,first,Decode);
        var envelope=(JObject)second[CosmosEmbeddedHistory.PropertyName]!;
        var records=(JArray)envelope["records"]!; var stored=(JObject)records[0]!;
        if(fault=="schema") envelope["schema"]=2;
        if(fault=="duplicate") records.Add(stored.DeepClone());
        if(fault=="identity") stored["Id"]=Guid.NewGuid().ToString();
        if(fault=="future") stored["Version"]=3;
        if(fault=="native") stored["_etag"]="not-domain-data";
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>CosmosEmbeddedHistory.ReadAsync(source.Id,second,0,Decode));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>CosmosEmbeddedHistory.StageAsync(source,second,Decode));
    }

    [TestMethod]
    public async Task ExhaustedVersionDoesNotMutateOrDiscardPreviousDocument()
    {
        var source=new Holon{Id=Guid.NewGuid(),Version=int.MaxValue};
        var prior=await CosmosEmbeddedHistory.StageAsync(source,null!,Decode); var unchanged=prior.ToString();
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>CosmosEmbeddedHistory.StageAsync(source,prior,Decode));
        Assert.AreEqual(unchanged,prior.ToString()); Assert.AreEqual(int.MaxValue,source.Version);
    }

    private static Holon Decode(string json)=>OasisJson.DeserializeStored<Holon>(json);
}
