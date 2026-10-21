using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using Native = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoEmbeddedAuditPrecisionTests
{
    private static readonly DateTime When = new DateTime(2026, 10, 19, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567);
    private static void Dates(IAuditBase value)
    { value.CreatedDate = When; value.ModifiedDate = When.AddTicks(11); value.DeletedDate = When.AddTicks(22); }

    [DataTestMethod]
    [DataRow(false, false)] [DataRow(false, true)] [DataRow(true, false)] [DataRow(true, true)]
    public void NestedCoreAndDerivedChildrenRetainAuditPrecisionDiscriminatorsAndNativeFilters(bool derived, bool reverse)
    {
        IHolon child = derived ? new InventoryItem { Id = Guid.NewGuid(), Quantity = 7 } : new Holon { Id = Guid.NewGuid() };
        var grand = new Holon { Id = Guid.NewGuid() }; Dates(child); Dates(grand); child.Children.Add(grand);
        var root = new Native { HolonId = Guid.NewGuid(), Children = new List<IHolon> { child } };
        var document = root.ToBsonDocument();
        var embedded = document["Children"][0].AsBsonDocument;
        Assert.AreEqual(BsonType.DateTime, embedded["CreatedDate"].BsonType);
        Assert.AreEqual(When.Ticks, embedded["CreatedDateExactUtcTicks"].AsInt64);
        if (reverse) document["Children"][0] = new BsonDocument(embedded.Elements.Reverse());
        var loaded = BsonSerializer.Deserialize<Native>(document.ToBson()).Children.Single();
        Assert.AreEqual(child.GetType(), loaded.GetType()); Assert.AreEqual(child.Id, loaded.Id);
        Assert.AreEqual(child.CreatedDate, loaded.CreatedDate); Assert.AreEqual(child.ModifiedDate, loaded.ModifiedDate);
        Assert.AreEqual(child.DeletedDate, loaded.DeletedDate); Assert.AreEqual(grand.CreatedDate, loaded.Children.Single().CreatedDate);
        if (derived) Assert.AreEqual(7, ((InventoryItem)loaded).Quantity);
        loaded.ModifiedDate = When.AddTicks(123);
        var rewritten = BsonSerializer.Deserialize<Native>(new Native { Children = new List<IHolon> { loaded } }.ToBson());
        Assert.AreEqual(loaded.ModifiedDate, rewritten.Children.Single().ModifiedDate);
        // Query/member translation is still delegated to the same class map.
        var rendered = Builders<Holon>.Filter.Gt(value => value.CreatedDate, When).Render(
            new RenderArgs<Holon>(BsonSerializer.LookupSerializer<Holon>(), BsonSerializer.SerializerRegistry));
        Assert.AreEqual(BsonType.DateTime, rendered["CreatedDate"]["$gt"].BsonType);
    }

    [DataTestMethod]
    [DataRow("CreatedDate", 0)] [DataRow("ModifiedDate", 0)] [DataRow("DeletedDate", 0)]
    [DataRow("CreatedDate", 1)] [DataRow("ModifiedDate", 1)] [DataRow("DeletedDate", 1)]
    [DataRow("CreatedDate", 2)] [DataRow("ModifiedDate", 2)] [DataRow("DeletedDate", 2)]
    public void CorruptEmbeddedAuditRepresentationCannotHydrate(string field, int fault)
    {
        var row = new Holon { Id = Guid.NewGuid() }; Dates(row); var document = row.ToBsonDocument();
        if (fault == 0) document.Remove(field);
        else if (fault == 1) document[field] = new BsonDateTime(When.AddSeconds(1));
        else document[field + "ExactUtcTicks"] = When.Ticks.ToString();
        Assert.ThrowsException<FormatException>(() => BsonSerializer.Deserialize<Holon>(document.ToBson()));
    }

    [DataTestMethod]
    [DataRow("CreatedDate")] [DataRow("ModifiedDate")] [DataRow("DeletedDate")]
    public void LegacyEmbeddedDatesRemainReadableAtTheirStoredPrecision(string field)
    {
        var row = new Holon { Id = Guid.NewGuid() }; var document = row.ToBsonDocument();
        var projected = new BsonDateTime(When); document[field] = projected;
        var loaded = BsonSerializer.Deserialize<Holon>(document.ToBson());
        Assert.AreEqual(projected.ToUniversalTime(), (DateTime)typeof(Holon).GetProperty(field)!.GetValue(loaded)!);
        Assert.IsFalse(loaded.ToBsonDocument().Contains(field + "ExactUtcTicks"));
    }
}
