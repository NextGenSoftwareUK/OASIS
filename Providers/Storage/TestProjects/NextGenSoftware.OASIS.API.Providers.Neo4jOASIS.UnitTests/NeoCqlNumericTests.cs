using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlNumericTests
{
    private static Dictionary<string, object> Packet() => new()
    {
        ["high"] = long.MinValue, ["low"] = long.MaxValue,
        ["present"] = 2L, ["inputs"] = 2L,
        ["valid"] = true, ["traversalCycle"] = false, ["traversalLimit"] = false
    };

    [TestMethod]
    public void PacketPreservesExactSignedLimbs()
    {
        var value = Neo4jOASIS.DecodeCqlNumeric(Packet());
        Assert.AreEqual(long.MinValue, value.High);
        Assert.AreEqual(long.MaxValue, value.Low);
        Assert.AreEqual(2L, value.Count);
    }

    [TestMethod]
    public void FloatingPointAndAdditionalFieldsCannotImpersonateAPacket()
    {
        var packet = Packet();
        packet["high"] = 1.0;
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlNumeric(packet));
        packet = Packet();
        packet["extra"] = 0L;
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlNumeric(packet));
    }

    [TestMethod]
    public void ImpossibleParticipationAndExcessWorkCannotPublish()
    {
        var packet = Packet();
        packet["present"] = 3L;
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.DecodeCqlNumeric(packet));
        packet = Packet();
        packet["inputs"] = (long)int.MaxValue + 1;
        Exception? failure = null;
        try { Neo4jOASIS.DecodeCqlNumeric(packet); }
        catch (Exception cause) { failure = cause; }
        Assert.IsNotNull(failure);
        Assert.AreEqual("NeoCqlLimitException", failure.GetType().Name);
    }
}
