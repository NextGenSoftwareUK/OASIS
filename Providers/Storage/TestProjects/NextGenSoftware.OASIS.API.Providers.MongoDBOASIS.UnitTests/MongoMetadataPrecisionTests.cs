using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Native = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoMetadataPrecisionTests
{
    private static DateTime When(int epoch) => epoch switch {
        0 => new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc).AddTicks(1234567),
        1 => new DateTime(1960, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234567),
        2 => DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc),
        _ => new DateTime(DateTime.MaxValue.Ticks - DateTime.MaxValue.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc) };
    private static Dictionary<string, object> Values(DateTime date) => new() {
        ["when"] = date, ["literal"] = date.ToString("O"), ["bytes"] = new byte[] { 0, 17, 255 },
        ["tree"] = new JObject { ["nested/date~key"] = new JValue(date), ["number"] = 9007199254740993L },
        ["array"] = new object[] { new Dictionary<string, object> { [""] = date }, date } };
    private static void Verify(Dictionary<string, object> values, DateTime date)
    {
        Assert.AreEqual(date, (DateTime)values["when"]);
        Assert.AreEqual(DateTimeKind.Utc, ((DateTime)values["when"]).Kind);
        Assert.AreEqual(date.ToString("O"), values["literal"]);
        CollectionAssert.AreEqual(new byte[] { 0, 17, 255 }, (byte[])values["bytes"]);
        var tree = (Dictionary<string, object>)values["tree"];
        Assert.AreEqual(date, tree["nested/date~key"]); Assert.AreEqual(9007199254740993L, tree["number"]);
        var array = (object[])values["array"];
        Assert.AreEqual(date, ((Dictionary<string, object>)array[0])[""]); Assert.AreEqual(date, array[1]);
        Assert.IsFalse(values.ContainsKey("MetaDataExactUtcTicks"));
    }

    [DataTestMethod]
    [DataRow(0, false)] [DataRow(0, true)] [DataRow(1, false)] [DataRow(1, true)] [DataRow(2, false)] [DataRow(2, true)] [DataRow(3, false)] [DataRow(3, true)]
    public void EntityMetadataRetainsNativeShapesExactDatesAndEscapedPaths(int epoch, bool reverse)
    {
        var date = When(epoch); var original = new Native { HolonId = Guid.NewGuid(), MetaData = Values(date), CreatedDate = date, ModifiedDate = date, DeletedDate = date };
        var document = original.ToBsonDocument();
        Assert.AreEqual(BsonType.DateTime, document["MetaData"]["when"].BsonType);
        Assert.AreEqual(BsonType.Document, document["MetaData"]["tree"].BsonType);
        Assert.AreEqual(BsonType.Binary, document["MetaData"]["bytes"].BsonType);
        if (reverse) document = new BsonDocument(document.Elements.Reverse());
        var loaded = BsonSerializer.Deserialize<Native>(document.ToBson()); Verify(loaded.MetaData, date);
        Assert.AreEqual(date, loaded.CreatedDate); Assert.AreEqual(date, loaded.ModifiedDate); Assert.AreEqual(date, loaded.DeletedDate);
        loaded.MetaData["when"] = date.AddTicks(epoch == 2 ? -1 : 1);
        var rewritten = BsonSerializer.Deserialize<Native>(loaded.ToBson());
        Assert.AreEqual(loaded.MetaData["when"], rewritten.MetaData["when"]);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void EmbeddedCoreMetadataUsesTheSameValidatedRepresentation(bool reverse)
    {
        var date = When(0); var child = new Holon { Id = Guid.NewGuid(), MetaData = Values(date) };
        var document = new Native { Children = new List<IHolon> { child } }.ToBsonDocument();
        if (reverse) document["Children"][0] = new BsonDocument(document["Children"][0].AsBsonDocument.Elements.Reverse());
        var loaded = BsonSerializer.Deserialize<Native>(document.ToBson()); Verify(loaded.Children.Single().MetaData, date);
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void CorruptMetadataPrecisionCannotHydrate(int fault)
    {
        var document = new Native { MetaData = Values(When(0)) }.ToBsonDocument();
        var entries = document["MetaDataExactUtcTicks"].AsBsonArray;
        var entry = entries[0].AsBsonDocument;
        if (fault == 0) entry["Ticks"] = When(0).AddSeconds(1).Ticks;
        else if (fault == 1) entry["Path"] = "/missing";
        else if (fault == 2) entry["Path"] = "/literal";
        else if (fault == 3) entries.Add(entry.DeepClone());
        else if (fault == 4) entry["Path"] = "/bad~2escape";
        else entry["Ticks"] = "invalid";
        Assert.ThrowsException<FormatException>(() => BsonSerializer.Deserialize<Native>(document.ToBson()));
    }

    [TestMethod]
    public void LegacyMetadataWithoutPrecisionDoesNotInventTicks()
    {
        var date = When(0); var document = new Native { MetaData = Values(date) }.ToBsonDocument();
        document.Remove("MetaDataExactUtcTicks"); var loaded = BsonSerializer.Deserialize<Native>(document.ToBson());
        Assert.AreEqual(new BsonDateTime(date).ToUniversalTime(), loaded.MetaData["when"]);
        Assert.IsTrue(loaded.MetaData["literal"] is string);
    }
    [TestMethod]
    public void BusinessMetadataKeyMatchingStorageCompanionAndJsonTextRemainLiteralValues()
    {
        var date = When(0); const string json = "{\"when\":\"literal JSON text\"}";
        var source = new Native { MetaData = new() { ["MetaDataExactUtcTicks"] = date, ["json-text"] = json } };
        var document = source.ToBsonDocument();
        Assert.IsTrue(document["MetaDataExactUtcTicks"].IsBsonArray);
        Assert.IsTrue(document["MetaData"]["MetaDataExactUtcTicks"].IsBsonDateTime);
        var loaded = BsonSerializer.Deserialize<Native>(document.ToBson());
        Assert.AreEqual(date, loaded.MetaData["MetaDataExactUtcTicks"]); Assert.AreEqual(json, loaded.MetaData["json-text"]);
    }
}
