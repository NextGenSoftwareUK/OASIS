using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlFloatingGroupedPacketTests
{
    private static BsonDocument Packet(int width) => new()
    {
        { "_id", new BsonDocument("k0", "coffee") },
        { "k0", new BsonDocument { { "s", 2 }, { "v", "coffee" } } },
        { "resultMember", true }, { "inputs", 2L }, { "work", 2L }, { "invalid", 0L },
        { "a0present", 2L }, { "a1present", 2L },
        { "a0parts", Words(width) }, { "a1parts", Words(width) }
    };

    private static BsonArray Words(int width)
        => new(Enumerable.Range(0, width).Select(index => new BsonInt64(index == width - 1 ? 3 : 0)));

    private static CosmicGroupedRow Read(BsonDocument packet, bool single)
    {
        if (single)
            return HolonRepository.ReadNativeCqlGrouped(packet, CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
                .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<float>(h, "reading")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<float>(h, "reading")) }).Result);
        return HolonRepository.ReadNativeCqlGrouped(packet, CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<double>(h, "reading")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<double>(h, "reading")) }).Result);
    }

    [DataTestMethod]
    [DataRow(true)] [DataRow(false)]
    public void EachDeclaredDomainUsesExactCoreSumAndMean(bool single)
    {
        var row = Read(Packet(single ? 10 : 70), single);
        if (single)
        {
            Assert.AreEqual(3f * float.Epsilon, row.Values[0]);
            Assert.AreEqual(2f * float.Epsilon, row.Values[1]);
        }
        else
        {
            Assert.AreEqual(3d * double.Epsilon, row.Values[0]);
            Assert.AreEqual(2d * double.Epsilon, row.Values[1]);
        }
    }

    [DataTestMethod]
    [DataRow(9)] [DataRow(11)] [DataRow(69)] [DataRow(71)]
    public void WidthCannotBeInferredOrTruncated(int width)
        => Assert.ThrowsException<InvalidDataException>(() => Read(Packet(width), width < 20));

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public void EveryWordRequiresExactInt64Type(int representation)
    {
        var packet = Packet(10);
        packet["a0parts"].AsBsonArray[0] = representation == 0 ? new BsonDouble(0)
            : representation == 1 ? new BsonDecimal128(Decimal128.Zero) : (BsonValue)new BsonInt32(0);
        Assert.ThrowsException<InvalidDataException>(() => Read(packet, true));
    }

    [TestMethod]
    public void OffPageMarkersCannotHideMissingRequiredParticipation()
    {
        var packet = Packet(10); packet["resultMember"] = false; packet["a0present"] = 1L;
        Assert.ThrowsException<InvalidDataException>(() => Read(packet, true));
    }

    [TestMethod]
    public void UnexpectedFieldsCannotImpersonateAnotherNumericShape()
    {
        var packet = Packet(70); packet["a0high"] = new BsonDecimal128(Decimal128.Zero);
        Assert.ThrowsException<InvalidDataException>(() => Read(packet, false));
    }
}
