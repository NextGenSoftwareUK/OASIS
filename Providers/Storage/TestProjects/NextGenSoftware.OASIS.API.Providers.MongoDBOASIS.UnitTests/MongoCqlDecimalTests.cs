using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using Native = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlDecimalTests
{
    [DataTestMethod]
    [DataRow("-79228162514264337593543950335")] [DataRow("0.0000000000000000000000000001")] [DataRow("79228162514264337593543950335")]
    public void BoxedDecimalMetadataUsesExactNumericBson(string text)
    {
        var value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        var native = new Native { MetaData = new() { ["counter"] = value, ["literal"] = text } }.ToBsonDocument();
        Assert.IsTrue(native["MetaData"]["counter"].IsDecimal128);
        Assert.AreEqual((decimal)value, Decimal128.ToDecimal(native["MetaData"]["counter"].AsDecimal128));
        Assert.AreEqual(text, native["MetaData"]["literal"].AsString);
    }

    [TestMethod]
    public void SelectedDecimalPacketsDecodeAllExactNativeIntegerForms()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<decimal>(h, "counter")).Result;
        foreach (var value in new BsonValue[] { new BsonInt32(0), new BsonInt64(long.MaxValue), new BsonDecimal128((decimal)decimal.MaxValue) })
        {
            var result = HolonRepository.ReadNativeCqlProjectionRow(Packet(value), query.Columns);
            Assert.IsInstanceOfType<decimal>(result.Values[0]);
        }
    }

    [TestMethod]
    public void SelectedDecimalPacketsRejectRoundingNonfiniteAndForeignDomains()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<decimal>(h, "counter")).Result;
        foreach (var value in new BsonValue[] { new BsonDouble(1), new BsonString("1"),
            new BsonDecimal128(Decimal128.Parse("79228162514264337593543950336")), new BsonDecimal128(Decimal128.Parse("1E-1000")),
            new BsonDecimal128(Decimal128.Parse("1.000000000000000000000000000000001")),
            new BsonDecimal128(Decimal128.Parse("NaN")), new BsonDecimal128(Decimal128.PositiveInfinity),
            new BsonDecimal128(Decimal128.Parse("1E100")) })
            Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlProjectionRow(Packet(value), query.Columns));
    }

    private static BsonDocument Packet(BsonValue value)
        => new("cells", new BsonArray { new BsonDocument { { "s", 2 }, { "v", value } } });
}
