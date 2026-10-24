using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlGroupedDecoderTests
{
    private static BsonDocument Packet() => new()
    {
        { "_id", new BsonDocument("k0", "coffee") },
        { "k0", new BsonDocument { { "s", 2 }, { "v", "coffee" } } },
        { "inputs", 2L }, { "work", 2L }, { "invalid", 0L },
        { "a1present", 2L },
        { "a1high", new BsonDecimal128(Decimal128.Zero) },
        { "a1low", new BsonDecimal128(new Decimal128(3L)) }
    };

    private static CosmicGroupedRow Read(BsonDocument packet)
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        return HolonRepository.ReadNativeCqlGrouped(packet, query);
    }

    [TestMethod]
    public void ExactNumericPacketUsesTheSharedNormalizerWithoutConstructingEntities()
    {
        var row = Read(Packet());
        Assert.AreEqual(2L, row.InputCount);
        Assert.AreEqual("coffee", row.Keys[0]);
        Assert.AreEqual(3L, row.Values[1]);
    }

    [DataTestMethod]
    [DataRow("inputs", 0L)]
    [DataRow("work", 1L)]
    [DataRow("invalid", 1L)]
    [DataRow("a1present", -1L)]
    [DataRow("a1present", 3L)]
    [DataRow("a1present", 1L)]
    public void InconsistentParticipationCannotPublish(string name, long value)
    {
        var packet = Packet(); packet[name] = value;
        Assert.ThrowsException<InvalidDataException>(() => Read(packet));
    }

    [TestMethod]
    public void ExcessWorkIsAResourceErrorInsteadOfTruncatedSuccess()
    {
        var packet = Packet(); packet["work"] = (long)int.MaxValue + 1;
        Assert.ThrowsException<MongoCqlLimitException>(() => Read(packet));
    }

    [TestMethod]
    public void FinalOverflowKeepsItsNumericErrorCode()
    {
        var packet = Packet();
        packet["a1high"] = new BsonDecimal128(new Decimal128(4294967294L));
        packet["a1low"] = new BsonDecimal128(new Decimal128(8589934590L));
        var error = Assert.ThrowsException<MongoCqlGroupedException>(() => Read(packet));
        Assert.AreEqual("CQL_SCALAR_OVERFLOW", error.Code);
    }

    [TestMethod]
    public void NullableEmptySumAndAverageKeepDistinctResults()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<long?>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<long?>(h, "stock")) }).Result;
        var packet = Packet();
        packet.Remove("a1present"); packet.Remove("a1high"); packet.Remove("a1low");
        foreach (var name in new[] { "a0", "a1" })
        {
            packet[name + "present"] = 0L;
            packet[name + "high"] = new BsonDecimal128(Decimal128.Zero);
            packet[name + "low"] = new BsonDecimal128(Decimal128.Zero);
        }
        var row = HolonRepository.ReadNativeCqlGrouped(packet, query);
        Assert.AreEqual(0L, row.Values[0]);
        Assert.IsNull(row.Values[1]);
    }

    [TestMethod]
    public void FractionalLimbsAndUnexpectedFieldsAreRejected()
    {
        var packet = Packet(); packet["a1high"] = new BsonDecimal128(Decimal128.Parse("1.000000000000000000000000000000001"));
        Assert.ThrowsException<InvalidDataException>(() => Read(packet));
        packet = Packet(); packet["extra"] = 1;
        Assert.ThrowsException<InvalidDataException>(() => Read(packet));
        packet = Packet(); packet.Remove("k0");
        Assert.ThrowsException<InvalidDataException>(() => Read(packet));
    }
}
