using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlGroupedDecoderTests
{
    private static Dictionary<string, object> Packet() => new()
    {
        ["inputs"] = 2L, ["valid"] = true, ["traversalCycle"] = false, ["traversalLimit"] = false,
        ["keys"] = new object[] { new Dictionary<string, object> { ["s"] = 2L, ["v"] = "coffee" } },
        ["aggregates"] = new object[] { new Dictionary<string, object> { ["present"] = 2L, ["high"] = 0L, ["low"] = 3L } }
    };
    private static CosmicGroupedRow Read(Dictionary<string, object> packet)
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        return Neo4jOASIS.DecodeCqlGrouped(packet, query);
    }

    [TestMethod]
    public void ExactPacketRetainsIdentityAndSharedNumericValue()
    {
        var row = Read(Packet());
        Assert.AreEqual(2L, row.InputCount); Assert.AreEqual("coffee", row.Keys[0]); Assert.AreEqual(3L, row.Values[1]);
    }

    [DataTestMethod]
    [DataRow(0L)] [DataRow(-1L)]
    public void EmptyOrNegativeGroupCannotPublish(long count)
    {
        var packet = Packet(); packet["inputs"] = count;
        Assert.ThrowsException<InvalidDataException>(() => Read(packet));
    }

    [TestMethod]
    public void IncorrectEnvelopeAndFloatingPointLimbsCannotPublish()
    {
        var packet = Packet(); packet["extra"] = 0;
        Assert.ThrowsException<InvalidDataException>(() => Read(packet));
        packet = Packet(); ((Dictionary<string, object>)((object[])packet["aggregates"])[0])["high"] = 0.0;
        Assert.ThrowsException<InvalidDataException>(() => Read(packet));
        packet = Packet(); packet["aggregates"] = new object[0];
        Assert.ThrowsException<InvalidDataException>(() => Read(packet));
    }

    [DataTestMethod]
    [DataRow(-1L)] [DataRow(1L)] [DataRow(3L)]
    public void RequiredParticipationMustMatchTheInputProof(long count)
    {
        var packet = Packet(); ((Dictionary<string, object>)((object[])packet["aggregates"])[0])["present"] = count;
        Assert.ThrowsException<InvalidDataException>(() => Read(packet));
    }

    [TestMethod]
    public void FinalNumericOverflowKeepsItsOriginalErrorCode()
    {
        var packet = Packet(); var numeric = (Dictionary<string, object>)((object[])packet["aggregates"])[0];
        numeric["high"] = 4294967294L; numeric["low"] = 8589934590L;
        var error = Assert.ThrowsException<NeoCqlGroupedException>(() => Read(packet));
        Assert.AreEqual("CQL_SCALAR_OVERFLOW", error.Code);
    }

    [TestMethod]
    public void NullableEmptySumAndAverageRemainDistinct()
    {
        var packet = Packet();
        packet["aggregates"] = new object[]
        {
            new Dictionary<string, object> { ["present"] = 0L, ["high"] = 0L, ["low"] = 0L },
            new Dictionary<string, object> { ["present"] = 0L, ["high"] = 0L, ["low"] = 0L }
        };
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<long?>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<long?>(h, "stock")) }).Result;
        var row = Neo4jOASIS.DecodeCqlGrouped(packet, query);
        Assert.AreEqual(0L, row.Values[0]); Assert.IsNull(row.Values[1]);
    }
}
