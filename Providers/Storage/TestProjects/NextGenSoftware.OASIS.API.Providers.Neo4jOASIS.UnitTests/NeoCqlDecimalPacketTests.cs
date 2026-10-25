using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlDecimalPacketTests
{
    private static Dictionary<string, object> Packet(int words = 7) => new()
    {
        ["inputs"] = 2L, ["present"] = 2L, ["valid"] = true,
        ["traversalCycle"] = false, ["traversalLimit"] = false,
        ["parts"] = Enumerable.Repeat((object)0L, words).ToArray()
    };

    [TestMethod]
    public void SignedWordsAreCapturedWithoutRawPacketAliasing()
    {
        var packet = Packet(); var parts = (object[])packet["parts"]; parts[6] = -123L;
        var result = Provider.DecodeCqlNumeric(packet, decimalInput: true, required: true);
        parts[6] = 456L;
        Assert.AreEqual(-123L, result.DecimalParts[6]); Assert.AreEqual(2L, result.Count);
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(6)] [DataRow(8)]
    public void DecimalWordCountMustBeExactlySeven(int count)
        => Assert.ThrowsException<InvalidDataException>(() => Provider.DecodeCqlNumeric(Packet(count), decimalInput: true));

    [TestMethod]
    public void DecimalAndIntegerShapesCannotImpersonateEachOther()
    {
        Assert.ThrowsException<InvalidDataException>(() => Provider.DecodeCqlNumeric(Packet()));
        var packet = Packet(); packet.Remove("parts"); packet["high"] = 0L; packet["low"] = 0L;
        Assert.ThrowsException<InvalidDataException>(() => Provider.DecodeCqlNumeric(packet, decimalInput: true));
        packet = Packet(); packet["extra"] = 1L;
        Assert.ThrowsException<InvalidDataException>(() => Provider.DecodeCqlNumeric(packet, decimalInput: true));
    }

    [DataTestMethod]
    [DataRow("float")] [DataRow("text")] [DataRow("null")]
    public void EveryWordMustBeAnExactInt64(string kind)
    {
        var packet = Packet(); ((object[])packet["parts"])[0] = kind switch { "float" => (object)0d, "text" => "0", _ => null! };
        Assert.ThrowsException<InvalidDataException>(() => Provider.DecodeCqlNumeric(packet, decimalInput: true));
    }

    [TestMethod]
    public void RequiredParticipationCannotDisagreeWithInputCount()
    {
        var packet = Packet(); packet["present"] = 1L;
        Assert.ThrowsException<InvalidDataException>(() => Provider.DecodeCqlNumeric(packet, decimalInput: true, required: true));
        Assert.AreEqual(1L, Provider.DecodeCqlNumeric(packet, decimalInput: true).Count);
        packet.Remove("parts"); packet["high"] = 0L; packet["low"] = 0L;
        Assert.ThrowsException<InvalidDataException>(() => Provider.DecodeCqlNumeric(packet, required: true));
    }
}
