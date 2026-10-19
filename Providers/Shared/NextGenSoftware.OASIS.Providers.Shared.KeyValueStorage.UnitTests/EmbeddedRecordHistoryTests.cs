using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class EmbeddedRecordHistoryTests
{
    private static Holon Decode(string json)=>OasisJson.DeserializeStored<Holon>(json);
    [TestMethod]
    public async Task SharedCodecRetainsIndependentVersionsWithoutPublishingCallerState()
    {
        var codec=new EmbeddedRecordHistory("native_revision");
        var id=Guid.NewGuid();var proposed=new Holon{Id=id,Name="first",MetaData=new(){["private"]="owned private fixture"}};
        var first=await codec.StageAsync(proposed,null,Decode);
        Assert.AreEqual(0,proposed.Version);Assert.AreEqual(Guid.Empty,proposed.VersionId);
        var initial=await codec.ReadAsync(id,first,0,Decode);Assert.AreEqual(1,initial.Version);
        first["native_revision"]="owned-native-revision";
        proposed.Name="second";
        var second=await codec.StageAsync(proposed,first,Decode);
        Assert.IsNull(second["native_revision"]);Assert.AreEqual(0,proposed.Version);
        var current=await codec.ReadAsync(id,second,0,Decode);var historical=await codec.ReadAsync(id,second,1,Decode);
        Assert.AreEqual(2,current.Version);Assert.AreEqual("second",current.Name);Assert.AreEqual(initial.VersionId,current.PreviousVersionId);
        Assert.AreEqual("first",historical.Name);Assert.AreEqual(initial.VersionId,historical.VersionId);
        Assert.AreEqual("owned private fixture",historical.MetaData["private"].ToString());
        Assert.IsNull(await codec.ReadAsync(id,second,99,Decode));
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task LatestReadsValidateArchiveIdentityUniquenessAndNativeFieldExclusion(int mode)
    {
        var codec=new EmbeddedRecordHistory("native_revision");var id=Guid.NewGuid();var row=new Holon{Id=id,Name="owned"};
        var first=await codec.StageAsync(row,null,Decode);var next=await codec.StageAsync(row,first,Decode);
        var records=(JArray)next[EmbeddedRecordHistory.PropertyName]!["records"]!;
        if(mode==0)records.Add(records[0]!.DeepClone());
        else if(mode==1)records[0]!["Id"]=Guid.NewGuid().ToString();
        else records[0]!["native_revision"]="must-not-be-archived";
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>codec.ReadAsync(id,next,0,Decode));
    }
    [TestMethod]
    public void NativeFieldPolicyIsExplicitAndCopiedAtConstruction()
    {
        var fields=new[]{"native_revision"};var codec=new EmbeddedRecordHistory(fields);fields[0]="sensitive";
        var payload=codec.CanonicalPayload(JObject.Parse("{\"id\":\"native\",\"native_revision\":\"etag\",\"sensitive\":\"must-remain-private\"}"));
        Assert.IsNull(payload["native_revision"]);Assert.AreEqual("must-remain-private",payload["sensitive"]!.Value<string>());
        Assert.ThrowsException<ArgumentException>(()=>new EmbeddedRecordHistory(" "));
    }
}
