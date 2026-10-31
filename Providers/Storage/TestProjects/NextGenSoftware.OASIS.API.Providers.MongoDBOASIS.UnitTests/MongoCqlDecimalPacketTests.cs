using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlDecimalPacketTests
{
    private static BsonDocument Packet(int words = 7) => new()
    {
        { "inputs", 2L }, { "invalid", 0L }, { "present", 2L },
        { "parts", new BsonArray(Enumerable.Range(0, words).Select(_ => new BsonDecimal128(Decimal128.Zero))) }
    };

    [TestMethod]
    public void ExactSignedWordsAreCapturedWithoutAliasingTheRawPacket()
    {
        var packet = Packet(); packet["parts"].AsBsonArray[6] = new BsonDecimal128(new Decimal128(-123));
        var value = HolonRepository.ReadNativeCqlNumeric(packet, false, decimalInput: true, required: true);
        packet["parts"].AsBsonArray[6] = new BsonDecimal128(new Decimal128(456));
        Assert.AreEqual(-123L, value.DecimalParts[6]); Assert.AreEqual(2L, value.Count);
    }

    [TestMethod]
    public void DecimalAndIntegerShapesCannotImpersonateEachOther()
    {
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(Packet(), false));
        var integer = new BsonDocument { { "inputs", 2L }, { "invalid", 0L }, { "present", 2L },
            { "high", new BsonDecimal128(Decimal128.Zero) }, { "low", new BsonDecimal128(Decimal128.Zero) } };
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(integer, false, decimalInput: true));
        var extra = Packet(); extra.Add("extra", 1);
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(extra, false, decimalInput: true));
    }

    [DataTestMethod]
    [DataRow(6)] [DataRow(8)]
    public void DecimalWordCountMustBeExactlySeven(int count)
        => Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(Packet(count), false, decimalInput: true));

    [DataTestMethod]
    [DataRow("1.000000000000000000000000000000001")]
    [DataRow("9223372036854775807.0000000001")]
    [DataRow("NaN")] [DataRow("Infinity")] [DataRow("-Infinity")]
    public void FractionalAndNonFiniteWordsCannotRoundIntoSuccess(string text)
    {
        var packet = Packet(); packet["parts"].AsBsonArray[0] = new BsonDecimal128(Decimal128.Parse(text));
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(packet, false, decimalInput: true));
    }

    [TestMethod]
    public void BinaryFloatingWordsAndMissingRequiredParticipationReject()
    {
        var packet = Packet(); packet["parts"].AsBsonArray[0] = new BsonDouble(0);
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(packet, false, decimalInput: true));
        packet = Packet(); packet["present"] = 1L;
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(packet, false, decimalInput: true, required: true));
        Assert.AreEqual(1L, HolonRepository.ReadNativeCqlNumeric(packet, false, decimalInput: true).Count);
        var integer = new BsonDocument { { "inputs", 2L }, { "invalid", 0L }, { "present", 1L },
            { "high", new BsonDecimal128(Decimal128.Zero) }, { "low", new BsonDecimal128(Decimal128.Zero) } };
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(integer, false, required: true));
    }

    [TestMethod]
    public void DecimalShapeRetainsTraversalErrorPrecedence()
    {
        var envelope = new BsonDocument { { "error", 0 }, { "row", Packet() } };
        Assert.AreEqual(2L, HolonRepository.ReadNativeCqlNumeric(envelope, true, decimalInput: true).Count);
        envelope["error"] = 2;
        Assert.ThrowsException<MongoCqlLimitException>(() => HolonRepository.ReadNativeCqlNumeric(envelope, true, decimalInput: true));
    }
}
