using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class OasisJsonStoredObjectTests
{
    [DataTestMethod]
    [DataRow("{")][DataRow("[]")][DataRow("null")][DataRow("1")]
    public void InvalidExternalRecordIsDataCorruption(string json)
        =>Assert.ThrowsException<InvalidDataException>(()=>OasisJson.ParseStoredObject(json));

    [TestMethod]
    public void ExactValuesAndStringsRemainJsonDataWithoutTypeActivation()
    {
        const string json="{\"$type\":\"System.Diagnostics.Process, System.Diagnostics.Process\",\"value\":1.0000000000000000000000000001,\"wide\":1e100,\"nested\":[1,true,null,\"2026-01-01T00:00:00Z\"]}";
        var first=OasisJson.ParseStoredObject(json);
        using var document=System.Text.Json.JsonDocument.Parse(first.ToString());
        Assert.AreEqual(1.0000000000000000000000000001m,document.RootElement.GetProperty("value").GetDecimal());
        Assert.AreEqual(1e100,document.RootElement.GetProperty("wide").GetDouble());
        Assert.AreEqual(JTokenType.String,first["nested"][3].Type);
        Assert.AreEqual("System.Diagnostics.Process, System.Diagnostics.Process",first["$type"].Value<string>());
        first["nested"][0]=99;
        Assert.AreEqual("1",OasisJson.ParseStoredObject(json)["nested"][0].ToString());
    }
}
