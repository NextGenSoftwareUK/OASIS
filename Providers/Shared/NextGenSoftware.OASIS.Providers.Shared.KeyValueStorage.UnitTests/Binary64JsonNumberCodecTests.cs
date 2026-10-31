using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class Binary64JsonNumberCodecTests
{
    [DataTestMethod]
    [DataRow("9007199254740993.125",true)][DataRow("9007199254740993",true)]
    [DataRow("1.0000000000000000000000000001",true)][DataRow("1e400",true)][DataRow("1e-400",true)]
    [DataRow("0",false)][DataRow("1.1",false)][DataRow("1e20",false)][DataRow("9007199254740994",false)]
    public void ExplicitWireRoundTripPreservesNumericValueAndLeavesStringsDistinct(string literal,bool escaped)
    {
        var source=OasisJson.ParseStoredObject("{\"date\":\"2030-01-02T03:04:05.0000000+05:30\",\"text\":\""+literal+"\",\"MetaData\":{\"a.b/~\":["+literal+"]}}");
        var original=source.ToString(Formatting.None);var wire=Binary64JsonNumberCodec.Encode(source);
        Assert.AreEqual(original,source.ToString(Formatting.None));Assert.AreEqual(escaped,wire.Property(Binary64JsonNumberCodec.PropertyName)!=null);
        Assert.AreEqual(literal,wire["text"].Value<string>());
        Assert.AreEqual("2030-01-02T03:04:05.0000000+05:30",wire["date"].Value<string>());
        var native=OasisJson.ParseStoredObject(wire.ToString(Formatting.None));var before=native.ToString(Formatting.None);
        var restored=Binary64JsonNumberCodec.Decode(native);
        Assert.AreEqual(before,native.ToString(Formatting.None));Assert.IsTrue(StoredRecordPayloadEquality.Equivalent(source,restored));
        Assert.AreEqual(JTokenType.String,restored["text"].Type);
        using var json=System.Text.Json.JsonDocument.Parse(restored.ToString(Formatting.None));
        Assert.AreEqual(System.Text.Json.JsonValueKind.Number,json.RootElement.GetProperty("MetaData").GetProperty("a.b/~")[0].ValueKind);
    }

    [DataTestMethod][DataRow("schema")][DataRow("value")][DataRow("duplicate")][DataRow("missing")][DataRow("literal")]
    public void MalformedOrTamperedFramesCannotChangeCanonicalMeaning(string mode)
    {
        var wire=Binary64JsonNumberCodec.Encode(OasisJson.ParseStoredObject("{\"MetaData\":{\"n\":9007199254740993.125}}"));
        var envelope=(JObject)wire[Binary64JsonNumberCodec.PropertyName];var entries=(JArray)envelope["values"];
        if(mode=="schema")envelope["schema"]="unknown";
        if(mode=="value")wire["MetaData"]["n"]="different";
        if(mode=="duplicate")entries.Add(entries[0].DeepClone());
        if(mode=="missing")entries[0]["path"]=new JArray("missing");
        if(mode=="literal")entries[0]["literal"]="1,\"injected\":2";
        Assert.ThrowsException<InvalidDataException>(()=>Binary64JsonNumberCodec.Decode(wire));
    }

    [TestMethod]
    public void ReservedInputCannotMasqueradeAsAStoredEnvelope()
        =>Assert.ThrowsException<InvalidDataException>(()=>Binary64JsonNumberCodec.Encode(new JObject{[Binary64JsonNumberCodec.PropertyName]=new JObject()}));
}
