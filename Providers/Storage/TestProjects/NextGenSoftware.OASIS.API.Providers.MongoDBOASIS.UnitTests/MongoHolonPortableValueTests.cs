using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Native = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoHolonPortableValueTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void MongoMappingAndNativeBsonRetainPortableCanonicalRecord(bool nullCollections)
    {
        var source = new Holon { Id = Guid.NewGuid(), Name = "owned mapping", CustomKey = "owned/custom",
            Version = 1, VersionId = Guid.NewGuid(), CreatedDate = DateTime.UtcNow.AddTicks(37),
            CreatedByAvatarId = Guid.NewGuid(), ParentHolonId2 = Guid.NewGuid(), ParentHolonId3 = Guid.NewGuid(),
            ParentHolonIds = nullCollections ? null : new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            ChildrenIds = nullCollections ? null : new List<Guid> { Guid.NewGuid() },
            ChildIdListCache = "owned child cache", AllChildIdListCache = "owned all cache",
            ProviderUniqueStorageKey = new() { [ProviderType.SQLLiteDBOASIS] = "owned source key" },
            PreviousVersionProviderUniqueStorageKey = new() { [ProviderType.SQLLiteDBOASIS] = "owned previous key" },
            MetaData = new() { ["integer"] = 9007199254740993L, ["nested"] = new Dictionary<string, object> { ["flag"] = true } } };
        CosmicConcurrency.SetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        var plan = CommittedHolonReplicaPlan.Create(new CommittedHolonReplicaRequest(Guid.NewGuid(), "owned source",
            ProviderType.SQLLiteDBOASIS, source.CreatedByAvatarId, ConditionalHolonOperation.Save, new[] { source }, saveChildren: false), ProviderType.MongoDBOASIS);
        var admitted = plan.Nodes.Single().Snapshot;
        var entity = DataHelper.ConvertOASISHolonToMongoEntity(admitted, false);
        entity.Id = ObjectId.GenerateNewId().ToString(); entity.ProviderRevisionId = Guid.NewGuid();
        var loadedEntity = BsonSerializer.Deserialize<Native>(entity.ToBson());
        var loaded = DataHelper.ConvertMongoEntityToOASISHolon(loadedEntity, false);
        plan.ValidateAcknowledgedRecord(loaded);
        Assert.AreEqual(source.CustomKey, ((Holon)loaded).CustomKey);
        Assert.AreEqual(source.CreatedDate.Ticks, loaded.CreatedDate.Ticks);
        entity.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "changed entity";
        entity.PreviousVersionProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "changed entity";
        Assert.AreEqual("owned source key", source.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]);
        Assert.AreEqual("owned previous key", source.PreviousVersionProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]);
        Assert.AreEqual("owned source key", admitted.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]);
        Assert.AreEqual("owned previous key", admitted.PreviousVersionProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]);
        if (!nullCollections)
        {
            entity.ParentHolonIds!.Clear(); entity.ChildrenIds!.Clear();
            Assert.AreEqual(2, admitted.ParentHolonIds!.Count); Assert.AreEqual(1, admitted.ChildrenIds!.Count);
            loaded.ParentHolonIds!.Clear(); ((Holon)loaded).ChildrenIds!.Clear();
            Assert.AreEqual(2, loadedEntity.ParentHolonIds!.Count); Assert.AreEqual(1, loadedEntity.ChildrenIds!.Count);
        }
    }
}
