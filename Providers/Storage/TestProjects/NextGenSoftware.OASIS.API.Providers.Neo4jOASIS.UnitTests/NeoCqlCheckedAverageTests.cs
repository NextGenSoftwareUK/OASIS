using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlCheckedAverageTests
{
    private static Dictionary<string, object> Packet() => new()
    {
        ["high"] = 0L, ["low"] = 1L, ["present"] = 3L, ["inputs"] = 3L,
        ["valid"] = true, ["traversalCycle"] = false, ["traversalLimit"] = false,
        ["prefixOverflow"] = true
    };

    [TestMethod]
    public void OrderedPrefixEvidenceIsMandatoryEvenForAnInRangeFinalSum()
    {
        var packet = Packet();
        var decoded = Neo4jOASIS.DecodeCqlCheckedNumeric(packet, true);
        Assert.AreEqual(true, decoded.PrefixOverflow);
        Assert.AreEqual(1L, decoded.Low);
        Assert.AreEqual(3L, decoded.Count);
        packet.Remove("prefixOverflow");
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlCheckedNumeric(packet, true));
    }

    [DataTestMethod]
    [DataRow(0L)]
    [DataRow(1L)]
    public void NumericFlagsCannotImpersonateBooleanAcknowledgements(long flag)
    {
        var packet = Packet();
        packet["prefixOverflow"] = flag;
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlCheckedNumeric(packet, true));
    }

    [TestMethod]
    public void OrdinaryAndCheckedSchemasCannotBeInterchanged()
    {
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlNumeric(Packet()));
        var packet = Packet();
        packet["prefixOverflow"] = false;
        Assert.AreEqual(false, Neo4jOASIS.DecodeCqlCheckedNumeric(packet, true).PrefixOverflow);
        packet["extra"] = 0;
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlCheckedNumeric(packet, true));
    }
}
