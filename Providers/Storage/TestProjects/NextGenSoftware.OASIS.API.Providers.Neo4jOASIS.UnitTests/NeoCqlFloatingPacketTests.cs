using System.IO;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlFloatingPacketTests
{
    private static Dictionary<string, object> Packet(int width) => new()
    {
        ["parts"] = Enumerable.Range(0, width).Select(_ => (object)0L).ToList(),
        ["present"] = 2L, ["inputs"] = 2L,
        ["valid"] = true, ["traversalCycle"] = false, ["traversalLimit"] = false
    };

    [DataTestMethod]
    [DataRow(10)] [DataRow(70)]
    public void DeclaredWordsRemainDetachedAndCannotImpersonateAnotherDomain(int width)
    {
        var packet = Packet(width); var raw = (List<object>)packet["parts"]; raw[width - 1] = -7L;
        var value = Neo4jOASIS.DecodeCqlNumeric(packet, required: true, floatingWidth: width);
        raw[width - 1] = 9L;
        Assert.AreEqual(-7L, value.FloatingParts[width - 1]); Assert.IsNull(value.DecimalParts);
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlNumeric(packet));
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlNumeric(packet, decimalInput: true));
    }

    [DataTestMethod]
    [DataRow(9)] [DataRow(11)] [DataRow(69)] [DataRow(71)]
    public void WordCountCannotBeInferredOrTruncated(int width)
        => Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlNumeric(
            Packet(width), floatingWidth: width < 20 ? 10 : 70));

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public void EveryWordRequiresExactInt64Type(int representation)
    {
        var packet = Packet(10); var raw = (List<object>)packet["parts"];
        raw[0] = representation == 0 ? (object)0d : representation == 1 ? (object)0m : 0;
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlNumeric(packet, floatingWidth: 10));
    }

    [TestMethod]
    public void RequiredParticipationAndExclusiveDomainMustBeProven()
    {
        var packet = Packet(10); packet["present"] = 1L;
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlNumeric(packet, required: true, floatingWidth: 10));
        Assert.AreEqual(1L, Neo4jOASIS.DecodeCqlNumeric(packet, floatingWidth: 10).Count);
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlNumeric(packet, decimalInput: true, floatingWidth: 10));
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlNumeric(packet, floatingWidth: 7));
    }

    [DataTestMethod]
    [DataRow(true)] [DataRow(false)]
    public void ScalarEnrollmentCannotSilentlyEnrollPlainGroupedArithmetic(bool single)
    {
        var source = CosmicQuery<Holon>.Create();
        if (single)
        {
            var scalar = source.Sum(h => CosmicQueryMetadata.Value<float>(h, "reading")).Result;
            Assert.IsFalse(Neo4jOASIS.CqlScalarCapabilities.Admit(scalar).IsError);
            var grouped = source.GroupBy(h => h.Name).Select(g => g.Sum(h => CosmicQueryMetadata.Value<float>(h, "reading"))).Result;
            Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", Neo4jOASIS.CqlGroupedCapabilities.Admit(grouped).ErrorCode);
        }
        else
        {
            var scalar = source.Average(h => CosmicQueryMetadata.Value<double?>(h, "reading")).Result;
            Assert.IsFalse(Neo4jOASIS.CqlScalarCapabilities.Admit(scalar).IsError);
            var grouped = source.GroupBy(h => h.Name).Select(g => g.Average(h => CosmicQueryMetadata.Value<double?>(h, "reading"))).Result;
            Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", Neo4jOASIS.CqlGroupedCapabilities.Admit(grouped).ErrorCode);
        }
    }
}
