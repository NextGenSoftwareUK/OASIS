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
    [TestMethod]
    public async Task AllVersionsAreAscendingDetachedAndIncludeDeletedCurrent()
    {
        var codec=new EmbeddedRecordHistory();var row=new Holon{Id=Guid.NewGuid(),Name="first"};
        var first=await codec.StageAsync(row,null,Decode);
        Assert.AreEqual(1,codec.ReadAll(row.Id,first,Decode).Count);
        row.Name="deleted";row.DeletedDate=DateTime.UtcNow;
        var second=await codec.StageAsync(row,first,Decode);var before=second.ToString();
        var all=codec.ReadAll(row.Id,second,Decode);
        Assert.AreEqual(2,all.Count);Assert.AreEqual(1,all[0].Version);Assert.AreEqual(2,all[1].Version);
        Assert.AreEqual("first",all[0].Name);Assert.AreNotEqual(DateTime.MinValue,all[1].DeletedDate);
        all[0].Name="caller edit";Assert.AreEqual(before,second.ToString());
        Assert.AreEqual("first",codec.ReadAll(row.Id,second,Decode)[0].Name);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task AllVersionsRejectCorruptArchiveBeforeReturningAnyRecords(int mode)
    {
        var codec=new EmbeddedRecordHistory("native_revision");var row=new Holon{Id=Guid.NewGuid()};
        var first=await codec.StageAsync(row,null,Decode);var second=await codec.StageAsync(row,first,Decode);
        var archive=(JArray)second[EmbeddedRecordHistory.PropertyName]!["records"]!;
        if(mode==0)archive.Add(archive[0]!.DeepClone());
        else if(mode==1)archive[0]!["Id"]=Guid.NewGuid().ToString();
        else archive[0]!["native_revision"]="invalid";
        Assert.ThrowsException<InvalidDataException>(()=>codec.ReadAll(row.Id,second,Decode));
    }

    [DataTestMethod]
    [DataRow("1",true)][DataRow("1.0",false)][DataRow("1e0",false)][DataRow("\"1\"",false)]
    public async Task ExactObjectParserRetainsStrictIntegerSchemaContract(string schema,bool valid)
    {
        var codec=new EmbeddedRecordHistory();var source=new Holon{Id=Guid.NewGuid()};
        var staged=await codec.StageAsync(source,null,Decode);
        var parsed=OasisJson.ParseStoredObject(staged.ToString());
        parsed[EmbeddedRecordHistory.PropertyName]["schema"]=new JRaw(schema);
        if(valid)Assert.AreEqual(1,(await codec.ReadAsync(source.Id,parsed,0,Decode)).Version);
        else await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>codec.ReadAsync(source.Id,parsed,0,Decode));
    }

    [DataTestMethod]
    [DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task StagingAndArchiveRetainExactNumericMetadata(bool customEncode,bool wide)
    {
        var codec=new EmbeddedRecordHistory();var id=Guid.NewGuid();
        var firstValue=wide?(object)1e100:1.0000000000000000000000000001m;
        var nextValue=wide?(object)double.BitIncrement(1e100):1.0000000000000000000000000002m;
        var source=new Holon{Id=id,MetaData=new(){["precise"]=firstValue}};
        Holon Exact(string json)=>NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStoredHolon(json);
        Task<JObject> Stage(JObject previous)=>customEncode
            ?codec.StageAsync(source,previous,Exact,row=>OasisJson.Serialize(row))
            :codec.StageAsync(source,previous,Exact);
        var first=await Stage(null);
        source.MetaData["precise"]=nextValue;
        var second=await Stage(first);
        var historical=await codec.ReadAsync(id,second,1,Exact);
        var current=await codec.ReadAsync(id,second,0,Exact);
        Assert.AreEqual(0,source.Version);
        Assert.AreEqual(1,historical.Version);Assert.AreEqual(2,current.Version);
        AssertNumber(firstValue,historical.MetaData["precise"],wide);
        AssertNumber(nextValue,current.MetaData["precise"],wide);
        current.MetaData["precise"]="local edit";
        AssertNumber(nextValue,(await codec.ReadAsync(id,second,0,Exact)).MetaData["precise"],wide);
    }

    private static void AssertNumber(object expected,object actual,bool wide)
    {
        if(wide)Assert.AreEqual((double)expected,double.Parse(actual.ToString(),System.Globalization.CultureInfo.InvariantCulture));
        else Assert.AreEqual((decimal)expected,decimal.Parse(actual.ToString(),System.Globalization.CultureInfo.InvariantCulture));
    }

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
