using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class JsonElementMetadataTests
{
    [DataTestMethod]
    [DataRow("\"text\"")] [DataRow("0.0005")] [DataRow("true")] [DataRow("null")]
    [DataRow("[1,\"two\",null]")] [DataRow("{\"nested\":{\"value\":3}}")]
    public void CanonicalSnapshotPreservesJsonElementValueWithoutClrTypeMetadata(string json)
    {
        using var document=JsonDocument.Parse(json);
        var id=Guid.NewGuid();
        var source=new Holon {Id=id,MetaData=new Dictionary<string,object> {{"value",document.RootElement.Clone()}}};
        var encoded=OasisJson.Serialize(source);
        Assert.IsFalse(encoded.Contains("System.Text.Json.JsonElement",StringComparison.Ordinal));
        Assert.IsTrue(JToken.DeepEquals(JToken.Parse(json),JObject.Parse(encoded)["MetaData"]!["value"]));
        var decoded=OasisJson.Deserialize<Holon>(encoded);
        Assert.AreEqual(id,decoded.Id);
        var reencoded=OasisJson.Serialize(decoded);
        Assert.IsTrue(JToken.DeepEquals(JToken.Parse(json),JObject.Parse(reencoded)["MetaData"]!["value"]));
    }

    [TestMethod]
    public void JsonElementDoesNotAuthorizeEmbeddedClrTypeNames()
    {
        using var document=JsonDocument.Parse("{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\",\"value\":\"no\"}");
        var source=new Holon {MetaData=new Dictionary<string,object> {{"value",document.RootElement.Clone()}}};
        Assert.ThrowsException<Newtonsoft.Json.JsonSerializationException>(()=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
    }
}
