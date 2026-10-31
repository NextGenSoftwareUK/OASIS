using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlGroupedIdentityTests
{
    private static BsonDocument Packet(BsonValue identity, BsonValue value) => new()
    {
        { "_id", new BsonDocument("k0", identity) },
        { "k0", new BsonDocument { { "s", value.IsBsonNull ? 1 : 2 }, { "v", value } } },
        { "inputs", 1L }, { "work", 1L }, { "invalid", 0L }, { "resultMember", true }
    };
    private static CosmicGroupedRow ReadKey<T>(BsonDocument packet)
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => CosmicQueryMetadata.Value<T>(h, "key"))
            .Select(g => g.LongCount()).Result;
        return HolonRepository.ReadNativeCqlGrouped(packet, query);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void ComparableIdentityMustAgreeWithTypedKeyEvenOutsideResultPage(bool member)
    {
        var packet = Packet("tea", "coffee"); packet["resultMember"] = member;
        Assert.ThrowsException<InvalidDataException>(() => ReadKey<string>(packet));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void NullAndNonNullIdentityCannotContradictTheTypedKey(bool nullIdentity)
    {
        var packet = Packet(nullIdentity ? BsonNull.Value : new BsonString("coffee"),
            nullIdentity ? new BsonString("coffee") : BsonNull.Value);
        Assert.ThrowsException<InvalidDataException>(() => ReadKey<string>(packet));
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)]
    public void EveryCompositeIdentityComponentMustAgree(int changed)
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => new { h.Name, h.Version }).Select(g => g.LongCount()).Result;
        var packet = Packet("coffee", "coffee"); packet["_id"]["k1"] = 7;
        packet["k1"] = new BsonDocument { { "s", 2 }, { "v", 7 } };
        packet["_id"]["k" + changed] = changed == 0 ? new BsonString("tea") : new BsonInt32(8);
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlGrouped(packet, query));
    }

    [DataTestMethod]
    [DataRow(false, false)] [DataRow(false, true)] [DataRow(true, false)] [DataRow(true, true)]
    public void ExtremumComparableCannotDisagreeWithItsSelectedValue(bool maximum, bool member)
    {
        var grouped = CosmicQuery<Holon>.Create().GroupBy(h => h.Name);
        var query = maximum ? grouped.Select(g => g.Max(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Result
            : grouped.Select(g => g.Min(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Result;
        var packet = Packet("coffee", "coffee"); packet["resultMember"] = member;
        packet["a0present"] = 1L;
        packet["a0"] = new BsonDocument { { "k", 1L }, { "v", 2L } };
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlGrouped(packet, query));
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public void LogicalGuidIdentityAdmitsEverySupportedStoredRepresentation(int representation)
    {
        var id = Guid.Parse("a0000000-bbbb-cccc-dddd-eeeeeeeeeeee");
        BsonValue stored = representation == 0 ? new BsonString(id.ToString("D").ToUpperInvariant())
            : new BsonBinaryData(id, representation == 1 ? GuidRepresentation.Standard : GuidRepresentation.CSharpLegacy);
        var result = ReadKey<Guid>(Packet(id.ToString("N"), stored));
        Assert.AreEqual(id, result.Keys.Single());
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void NativeComparableGuidRequiresItsSingleNormalizedFormat(bool upper)
    {
        var id = Guid.Parse("a0000000-bbbb-cccc-dddd-eeeeeeeeeeee");
        var comparable = upper ? id.ToString("N").ToUpperInvariant() : id.ToString("D");
        Assert.ThrowsException<InvalidDataException>(() => ReadKey<Guid>(Packet(comparable, id.ToString("D"))));
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)]
    public void ComparableEqualityUsesLogicalDomainEquality(int domain)
    {
        if (domain == 0) Assert.AreEqual(1L, ReadKey<long>(Packet(new BsonInt32(1), new BsonInt64(1))).Keys.Single());
        if (domain == 1) Assert.AreEqual(1m, ReadKey<decimal>(Packet(new BsonDecimal128(new Decimal128(1m)),
            new BsonDecimal128(new Decimal128(1.00m)))).Keys.Single());
        if (domain == 2) Assert.AreEqual(0d, ReadKey<double>(Packet(new BsonDouble(0d), new BsonDouble(-0d))).Keys.Single());
        if (domain == 3) Assert.IsNull(ReadKey<long?>(Packet(BsonNull.Value, BsonNull.Value)).Keys.Single());
    }
}
