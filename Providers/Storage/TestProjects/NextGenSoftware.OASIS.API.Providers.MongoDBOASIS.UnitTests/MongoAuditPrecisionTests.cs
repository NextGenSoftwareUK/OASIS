using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Entity = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoAuditPrecisionTests
{
    private static Entity Row()
    {
        _ = new MongoDBOASIS("mongodb://127.0.0.1:27028/?serverSelectionTimeoutMS=100", "owned-audit-unit");
        return new Entity { HolonId = Guid.NewGuid() };
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void AuditDatesRetainTicksAndNativeDateQueriesRegardlessOfMemberOrder(bool reverse)
    {
        var row = Row();
        var instant = new DateTime(2026, 10, 19, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567);
        row.CreatedDate = instant; row.ModifiedDate = instant.AddTicks(11); row.DeletedDate = instant.AddTicks(22);
        var bson = row.ToBsonDocument();
        foreach (var field in new[] { "CreatedDate", "ModifiedDate", "DeletedDate" })
            Assert.AreEqual(BsonType.DateTime, bson[field].BsonType);
        if (reverse) bson = new BsonDocument(bson.Elements.Reverse());
        var loaded = BsonSerializer.Deserialize<Entity>(bson.ToBson());
        Assert.AreEqual(row.CreatedDate.Ticks, loaded.CreatedDate.Ticks);
        Assert.AreEqual(row.ModifiedDate.Ticks, loaded.ModifiedDate.Ticks);
        Assert.AreEqual(row.DeletedDate.Ticks, loaded.DeletedDate.Ticks);
        Assert.AreEqual(DateTimeKind.Utc, loaded.CreatedDate.Kind);
        loaded.CreatedDate = instant.AddTicks(111); loaded.ModifiedDate = instant.AddTicks(222);
        var rewritten = BsonSerializer.Deserialize<Entity>(loaded.ToBson());
        Assert.AreEqual(loaded.CreatedDate.Ticks, rewritten.CreatedDate.Ticks);
        Assert.AreEqual(loaded.ModifiedDate.Ticks, rewritten.ModifiedDate.Ticks);
    }

    [DataTestMethod]
    [DataRow("CreatedDate")] [DataRow("ModifiedDate")] [DataRow("DeletedDate")]
    public void LegacyMillisecondDatesRemainReadableWithoutInventingPrecision(string field)
    {
        var bson = Row().ToBsonDocument();
        var instant = new DateTime(2026, 10, 19, 12, 34, 56, DateTimeKind.Utc).AddMilliseconds(123);
        bson[field] = new BsonDateTime(instant);
        var loaded = BsonSerializer.Deserialize<Entity>(bson.ToBson());
        Assert.AreEqual(instant, (DateTime)typeof(Entity).GetProperty(field)!.GetValue(loaded)!);
        Assert.IsFalse(loaded.ToBsonDocument().Contains(field + "ExactUtcTicks"));
    }

    [DataTestMethod]
    [DataRow("CreatedDate", false)] [DataRow("ModifiedDate", false)] [DataRow("DeletedDate", false)]
    [DataRow("CreatedDate", true)] [DataRow("ModifiedDate", true)] [DataRow("DeletedDate", true)]
    public void InconsistentOrMissingDateProjectionIsRejected(string field, bool missing)
    {
        var bson = Row().ToBsonDocument();
        var instant = new DateTime(2026, 10, 19, 12, 34, 56, DateTimeKind.Utc);
        bson[field + "ExactUtcTicks"] = instant.AddTicks(1).Ticks;
        if (missing) bson.Remove(field);
        else bson[field] = new BsonDateTime(instant.AddSeconds(1));
        Assert.ThrowsException<FormatException>(() => BsonSerializer.Deserialize<Entity>(bson.ToBson()));
    }
}
