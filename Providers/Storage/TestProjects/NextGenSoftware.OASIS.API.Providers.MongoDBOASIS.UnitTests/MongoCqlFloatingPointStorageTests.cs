using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Native = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlFloatingPointStorageTests
{
    [DataTestMethod]
    [DataRow("-3.4028235E+38")] [DataRow("-1.401298E-45")] [DataRow("-0")]
    [DataRow("1.401298E-45")] [DataRow("0.1")] [DataRow("3.4028235E+38")]
    public void SingleMetadataRetainsItsExactNumericRoundTripInEveryContainer(string text)
    {
        var value = float.Parse(text, CultureInfo.InvariantCulture);
        var entity = new Native { MetaData = new()
        {
            ["sample"] = value,
            ["literal"] = text,
            ["nested"] = new System.Collections.Generic.Dictionary<string, object> { ["sample"] = value },
            ["array"] = new object[] { value }
        } };
        var data = entity.ToBsonDocument()["MetaData"];
        foreach (var sample in new[] { data["sample"], data["nested"]["sample"], data["array"][0] })
        {
            Assert.IsTrue(sample.IsDouble, "Single metadata must remain numeric, rather than complex JSON text.");
            var restored = (float)sample.AsDouble;
            Assert.AreEqual(BitConverter.SingleToInt32Bits(value == 0f ? 0f : value),
                BitConverter.SingleToInt32Bits(restored == 0f ? 0f : restored));
        }
        Assert.AreEqual(text, data["literal"].AsString);
    }

    [TestMethod]
    public void NumericSingleStoragePreservesTheCanonicalSnapshotAcknowledgement()
    {
        foreach (var value in new[] { float.MinValue, -float.Epsilon, 0.1f, float.Epsilon, float.MaxValue })
        {
            var input = new Native { MetaData = new() { ["sample"] = value } };
            var loaded = BsonSerializer.Deserialize<Native>(input.ToBsonDocument());
            Assert.IsTrue(Core.Serialization.StoredRecordValueEquality.Equivalent(
                Core.Serialization.StoredRecordJson.CaptureInputToken(input.MetaData),
                Core.Serialization.StoredRecordJson.CaptureInputToken(loaded.MetaData)),
                "Native numeric encoding must preserve the canonical JSON value used by save acknowledgements.");
        }
    }

    [DataTestMethod]
    [DataRow("NaN")] [DataRow("Infinity")] [DataRow("-Infinity")]
    public void NonfiniteSingleStillFailsSerialization(string text)
    {
        var value = float.Parse(text, CultureInfo.InvariantCulture);
        Assert.ThrowsException<BsonSerializationException>(() => new Native { MetaData = new() { ["sample"] = value } }.ToBsonDocument());
    }
}
