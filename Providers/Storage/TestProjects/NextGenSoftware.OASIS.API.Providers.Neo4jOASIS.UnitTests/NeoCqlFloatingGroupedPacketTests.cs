using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlFloatingGroupedPacketTests
{
    private static List<object> Words(int width)
        => Enumerable.Range(0, width).Select(index => (object)(index == width - 1 ? 3L : 0L)).ToList();

    private static Dictionary<string, object> Packet(int width) => new()
    {
        ["inputs"] = 2L, ["resultMember"] = true, ["valid"] = true,
        ["traversalCycle"] = false, ["traversalLimit"] = false, ["payloadLimit"] = false,
        ["keys"] = new List<object> { new Dictionary<string, object> { ["s"] = 2L, ["v"] = "coffee" } },
        ["aggregates"] = new List<object>
        {
            new Dictionary<string, object> { ["present"] = 2L, ["parts"] = Words(width) },
            new Dictionary<string, object> { ["present"] = 2L, ["parts"] = Words(width) }
        }
    };

    private static Dictionary<string, object> First(Dictionary<string, object> packet)
        => (Dictionary<string, object>)((List<object>)packet["aggregates"])[0];

    private static CosmicGroupedRow Read(Dictionary<string, object> packet, bool single)
    {
        if (single)
            return Neo4jOASIS.DecodeCqlGrouped(packet, CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
                .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<float>(h, "reading")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<float>(h, "reading")) }).Result);
        return Neo4jOASIS.DecodeCqlGrouped(packet, CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<double>(h, "reading")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<double>(h, "reading")) }).Result);
    }

    [DataTestMethod]
    [DataRow(true)] [DataRow(false)]
    public void DeclaredDomainsPublishExactCoreSumAndMean(bool single)
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
    public void WordCountCannotBeInferredOrTruncated(int width)
        => Assert.ThrowsException<InvalidDataException>(() => Read(Packet(width), width < 20));

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public void EveryWordRequiresExactInt64Type(int representation)
    {
        var packet = Packet(10);
        ((List<object>)First(packet)["parts"])[0] = representation == 0 ? (object)0d : representation == 1 ? (object)0m : 0;
        Assert.ThrowsException<InvalidDataException>(() => Read(packet, true));
    }

    [TestMethod]
    public void OffPageMembershipCannotHideMissingRequiredParticipation()
    {
        var packet = Packet(10); packet["resultMember"] = false; First(packet)["present"] = 1L;
        Assert.ThrowsException<InvalidDataException>(() => Read(packet, true));
    }

    [TestMethod]
    public void UnexpectedFieldsCannotImpersonateAnotherNumericShape()
    {
        var packet = Packet(70); First(packet)["high"] = 0L;
        Assert.ThrowsException<InvalidDataException>(() => Read(packet, false));
    }
}
