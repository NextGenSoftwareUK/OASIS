using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlSinglePacketTests
{
    [TestMethod]
    public void SelectedSingleRequiresExactWidenedFiniteBinary32()
    {
        var columns = CosmicQuery<Holon>.Create()
            .Select(h => CosmicQueryMetadata.Value<float>(h, "reading")).Result.Columns;

        foreach (var number in new[] { 0f, -0f, float.Epsilon, -float.MaxValue, float.MaxValue, 0.1f })
        {
            var packet = Packet(new BsonDouble(number));
            var row = HolonRepository.ReadNativeCqlProjectionRow(packet, columns);
            Assert.IsInstanceOfType<float>(row.Values[0]);
            Assert.AreEqual(number, (float)row.Values[0]);
        }

        // The detached packet must already be normalized. Rounding here would
        // conceal a broken translator or a forged provider result.
        foreach (var value in new BsonValue[] { new BsonDouble(0.1d), new BsonDouble(double.Epsilon),
            new BsonDouble(double.MaxValue), new BsonDouble(double.NaN),
            new BsonDouble(double.PositiveInfinity), new BsonInt32(1),
            new BsonDecimal128(1m), new BsonString("1") })
            Assert.ThrowsException<InvalidDataException>(() =>
                HolonRepository.ReadNativeCqlProjectionRow(Packet(value), columns));
    }

    private static BsonDocument Packet(BsonValue value)
        => new("cells", new BsonArray { new BsonDocument { { "s", 2 }, { "v", value } } });
}
