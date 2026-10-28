using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlCheckedAverageTests
{
    private static BsonDocument Packet() => new()
    {
        { "inputs", 3L }, { "invalid", 0L }, { "present", 3L },
        { "high", new BsonDecimal128(Decimal128.Zero) },
        { "low", new BsonDecimal128(new Decimal128(1)) }, { "prefixOverflow", true }
    };

    [TestMethod]
    public void PrefixEvidenceIsRequiredEvenWhenFinalSumFits()
    {
        var packet = Packet();
        var value = HolonRepository.ReadNativeCqlCheckedNumeric(packet, false, true);
        Assert.AreEqual(true, value.PrefixOverflow);
        Assert.AreEqual(1L, value.Low);
        Assert.AreEqual(3L, value.Count);

        packet.Remove("prefixOverflow");
        Assert.ThrowsException<InvalidDataException>(() =>
            HolonRepository.ReadNativeCqlCheckedNumeric(packet, false, true));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public void NumericFlagsCannotImpersonateBooleanAcknowledgements(int flag)
    {
        var packet = Packet();
        packet["prefixOverflow"] = flag;
        Assert.ThrowsException<InvalidDataException>(() =>
            HolonRepository.ReadNativeCqlCheckedNumeric(packet, false, true));
    }

    [TestMethod]
    public void CheckedAndOrdinaryPacketSchemasCannotBeInterchanged()
    {
        Assert.ThrowsException<InvalidDataException>(() =>
            HolonRepository.ReadNativeCqlNumeric(Packet(), false));

        var packet = Packet();
        packet["prefixOverflow"] = false;
        Assert.AreEqual(false, HolonRepository.ReadNativeCqlCheckedNumeric(packet, false, true).PrefixOverflow);
        packet["extra"] = 0;
        Assert.ThrowsException<InvalidDataException>(() =>
            HolonRepository.ReadNativeCqlCheckedNumeric(packet, false, true));
    }
}
