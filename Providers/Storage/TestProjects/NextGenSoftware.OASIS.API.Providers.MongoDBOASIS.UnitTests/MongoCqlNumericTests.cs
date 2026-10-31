using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlNumericTests
{
    private static BsonDocument Packet(BsonValue high, BsonValue low) => new()
    { { "inputs", 2L }, { "invalid", 0L }, { "present", 2L }, { "high", high }, { "low", low } };

    [TestMethod]
    public void ExactSignedLimbsRetainTheirFullInt64Domain()
    {
        var value = HolonRepository.ReadNativeCqlNumeric(Packet(new BsonDecimal128(new Decimal128(long.MinValue)),
            new BsonDecimal128(new Decimal128(long.MaxValue))), false);
        Assert.AreEqual(long.MinValue, value.High); Assert.AreEqual(long.MaxValue, value.Low); Assert.AreEqual(2L, value.Count);
    }

    [DataTestMethod]
    [DataRow("1.000000000000000000000000000000001")]
    [DataRow("9223372036854775807.0000000001")]
    [DataRow("NaN")] [DataRow("Infinity")] [DataRow("-Infinity")]
    public void FractionalOrNonFiniteDecimal128CannotRoundIntoAnInteger(string text)
        => Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(
            Packet(new BsonDecimal128(Decimal128.Parse(text)), new BsonDecimal128(Decimal128.Zero)), false));

    [TestMethod]
    public void Binary64AndExtraFieldsCannotImpersonateANumericPacket()
    {
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(
            Packet(new BsonDouble(1), new BsonDecimal128(Decimal128.Zero)), false));
        var extra = Packet(new BsonDecimal128(Decimal128.Zero), new BsonDecimal128(Decimal128.Zero)); extra.Add("extra", 0);
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(extra, false));
    }

    [TestMethod]
    public void InvalidParticipationAndWorkOverflowNeverPublish()
    {
        var invalid = Packet(new BsonDecimal128(Decimal128.Zero), new BsonDecimal128(Decimal128.Zero)); invalid["invalid"] = 1L;
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlNumeric(invalid, false));
        invalid["invalid"] = 0L; invalid["inputs"] = (long)int.MaxValue + 1;
        Assert.ThrowsException<MongoCqlLimitException>(() => HolonRepository.ReadNativeCqlNumeric(invalid, false));
    }
}
