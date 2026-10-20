using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NativeHolon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoEmbeddedGraphRestartTests
{
    [TestMethod]
    public void StoredInterfaceDiscriminatorCanBeReadWithoutSerializingAnInstanceFirst()
    {
        var id = Guid.NewGuid(); var child = Guid.NewGuid(); var grand = Guid.NewGuid();
        var document = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "HolonId", id.ToString("D") },
            { "Children", new BsonArray { new BsonDocument { { "_t", "Holon" }, { "_id", child.ToString("D") },
                { "Name", "stored child" }, { "Children", new BsonArray { new BsonDocument {
                    { "_t", "Holon" }, { "_id", grand.ToString("D") }, { "Name", "stored grandchild" } } } } } } } };
        var read = BsonSerializer.Deserialize<NativeHolon>(document);
        Assert.AreEqual(id, read.HolonId); Assert.AreEqual(child, read.Children.Single().Id);
        Assert.AreEqual(grand, read.Children.Single().Children.Single().Id);
    }

    [TestMethod]
    public void EmbeddedChildSnapshotsRetainNestedChildrenWithoutParentNavigation()
    {
        var rootId = Guid.NewGuid(); var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = rootId };
        var grand = new Holon { Id = Guid.NewGuid(), ParentHolonId = child.Id };
        child.Children.Add(grand); grand.ParentHolon = child;
        var root = new NativeHolon { Id = ObjectId.GenerateNewId().ToString(), HolonId = rootId,
            Children = new List<IHolon> { child } };
        var document = root.ToBsonDocument();
        var storedChild = document["Children"].AsBsonArray.Single().AsBsonDocument;
        Assert.IsFalse(storedChild.Contains("ParentHolon")); Assert.AreEqual(1, storedChild["Children"].AsBsonArray.Count);
        var restored = BsonSerializer.Deserialize<NativeHolon>(document);
        Assert.AreEqual(grand.Id, restored.Children.Single().Children.Single().Id);
    }
    [TestMethod]
    public void OwnedInventoryExcludesNavigationWhileTheSameTypeCanBeAnEmbeddedGraph()
    {
        var item = new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem { Id = Guid.NewGuid(), Name = "owned item", Quantity = 4 };
        var child = new Holon { Id = Guid.NewGuid(), Name = "graph child" };
        item.Children.Add(child);
        var detail = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail {
            Id = ObjectId.GenerateNewId().ToString(), HolonId = Guid.NewGuid(),
            Inventory = new List<NextGenSoftware.OASIS.API.Core.Objects.InventoryItem> { item } };
        var document = detail.ToBsonDocument();
        Assert.IsFalse(document["Inventory"].AsBsonArray.Single().AsBsonDocument.Contains("Children"));
        var restored = BsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>(document);
        Assert.AreEqual(item.Id, restored.Inventory.Single().Id); Assert.AreEqual(4, restored.Inventory.Single().Quantity);
        Assert.AreEqual(0, restored.Inventory.Single().Children.Count);
        var graph = new NativeHolon { Id = ObjectId.GenerateNewId().ToString(), HolonId = Guid.NewGuid(), Children = new List<IHolon> { item } };
        var graphRead = BsonSerializer.Deserialize<NativeHolon>(graph.ToBsonDocument());
        Assert.AreEqual(child.Id, graphRead.Children.Single().Children.Single().Id);
        Assert.AreSame(child, item.Children.Single());
        var filter = MongoDB.Driver.Builders<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>.Filter
            .ElemMatch(row => row.Inventory, owned => owned.Quantity > 2);
        var rendered = filter.Render(new MongoDB.Driver.RenderArgs<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>(
            BsonSerializer.LookupSerializer<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>(), BsonSerializer.SerializerRegistry));
        Assert.AreEqual(2, rendered["Inventory"]["$elemMatch"]["Quantity"]["$gt"].AsInt32);
    }
    [TestMethod]
    public void PhysicalRevisionOwnsMappedObservationAndDoesNotAliasNativeMetadata()
    {
        var token = Guid.NewGuid(); var spoofed = Guid.NewGuid();
        var native = new NativeHolon { Id = ObjectId.GenerateNewId().ToString(), HolonId = Guid.NewGuid(),
            ProviderRevisionId = token, VersionId = Guid.NewGuid(), ProviderMetaData = new() {
                [NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS] = new() {
                    [CosmicConcurrency.ProviderRevisionMetadataKey] = spoofed.ToString("D") } } };
        var mapped = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper.ConvertMongoEntityToOASISHolon(native, false);
        Assert.AreEqual(token, CosmicConcurrency.GetProviderRevisionId(mapped, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS));
        Assert.AreEqual(native.VersionId, mapped.VersionId);
        Assert.AreEqual(spoofed.ToString("D"), native.ProviderMetaData[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS][CosmicConcurrency.ProviderRevisionMetadataKey]);
        native.ProviderRevisionId = Guid.Empty;
        var legacy = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper.ConvertMongoEntityToOASISHolon(native, false);
        Assert.AreEqual(Guid.Empty, CosmicConcurrency.GetProviderRevisionId(legacy, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS));
        var write = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper.ConvertOASISHolonToMongoEntity(mapped, false);
        write.ProviderMetaData[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS][CosmicConcurrency.ProviderRevisionMetadataKey] = "changed native preparation";
        Assert.AreEqual(token, CosmicConcurrency.GetProviderRevisionId(mapped, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS));
    }
}
