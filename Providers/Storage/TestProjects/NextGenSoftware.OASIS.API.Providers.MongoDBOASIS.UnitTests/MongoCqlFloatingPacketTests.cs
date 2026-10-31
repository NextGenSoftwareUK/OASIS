using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlFloatingPacketTests
{
    private static BsonDocument Packet(int width) => new()
    {
        { "inputs", 2L }, { "invalid", 0L }, { "present", 2L },
        { "parts", new BsonArray(Enumerable.Range(0, width).Select(_ => new BsonInt64(0))) }
    };

    [DataTestMethod]
    [DataRow(10)] [DataRow(70)]
    public void DeclaredWordsAreDetachedAndCannotImpersonateDecimal(int width)
    {
        var packet = Packet(width); packet["parts"].AsBsonArray[width - 1] = new BsonInt64(-7);
        var value = HolonRepository.ReadNativeCqlNumeric(packet, false, required: true, floatingWidth: width);
        packet["parts"].AsBsonArray[width - 1] = new BsonInt64(9);
        Assert.AreEqual(-7L, value.FloatingParts[width - 1]); Assert.IsNull(value.DecimalParts);
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(packet, false, decimalInput: true));
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(packet, false));
    }

    [DataTestMethod]
    [DataRow(9)] [DataRow(11)] [DataRow(69)] [DataRow(71)]
    public void NativeWidthCannotBeTruncatedOrInferred(int width)
        => Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(
            Packet(width), false, floatingWidth: width < 20 ? 10 : 70));

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public void EveryWordMustRetainNativeInt64Type(int representation)
    {
        var packet = Packet(10);
        packet["parts"].AsBsonArray[0] = representation == 0 ? new BsonDouble(0)
            : representation == 1 ? new BsonDecimal128(Decimal128.Zero) : (BsonValue)new BsonInt32(0);
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(packet, false, floatingWidth: 10));
    }

    [TestMethod]
    public void RequiredParticipationAndExclusiveDomainAreProven()
    {
        var packet = Packet(10); packet["present"] = 1L;
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(packet, false, required: true, floatingWidth: 10));
        Assert.AreEqual(1L, HolonRepository.ReadNativeCqlNumeric(packet, false, floatingWidth: 10).Count);
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(packet, false, decimalInput: true, floatingWidth: 10));
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(packet, false, floatingWidth: 7));
    }
}
