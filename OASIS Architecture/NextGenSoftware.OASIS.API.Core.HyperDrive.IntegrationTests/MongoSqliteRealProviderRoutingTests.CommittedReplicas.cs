using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task CommittedReplicaCrossProviderPreservesActualGraphAndLineage(bool synchronous, bool mongoSource)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var workspace = new DirectoryInfo(AppContext.BaseDirectory);
        while (workspace != null && !File.Exists(Path.Combine(workspace.FullName, "Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1"))) workspace = workspace.Parent;
        Assert.NotNull(workspace);
        var directory = Path.GetFullPath(Path.Combine(workspace!.FullName, "TestResults/HyperDriveMongoSqliteCurrent")); Directory.CreateDirectory(directory);
        using var lease = File.Open(Path.Combine(directory, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var path = Path.GetFullPath(Path.Combine(directory, "replica-cross-provider-current.db"));
        const string databaseName = "cosmic_cross_provider_fixture";
        var client = new MongoClient(uri); using var cursor = await client.ListDatabaseNamesAsync();
        Assert.DoesNotContain(databaseName, await cursor.ToListAsync()); Assert.False(File.Exists(path));
        var database = client.GetDatabase(databaseName); await database.CreateCollectionAsync("CosmicOwnedFixture");
        var connection = $"Data Source={path};Pooling=False";
        using var sqlite = new SQLLiteDBOASIS(connection); using var sqliteRead = new SQLLiteDBOASIS(connection);
        var mongo = new MongoDBOASIS(uri, databaseName); var mongoRead = new MongoDBOASIS(uri, databaseName);
        IOASISStorageProvider source = mongoSource ? mongo : sqlite, target = mongoSource ? sqlite : mongo;
        IOASISStorageProvider sourceRead = mongoSource ? mongoRead : sqliteRead, targetRead = mongoSource ? sqliteRead : mongoRead;
        var sourceType = mongoSource ? ProviderType.MongoDBOASIS : ProviderType.SQLLiteDBOASIS;
        var targetType = mongoSource ? ProviderType.SQLLiteDBOASIS : ProviderType.MongoDBOASIS;
        var sourceId = "owned-cross-provider-" + sourceType; var actor = Guid.NewGuid(); var complete = false;
        static IEnumerable<IHolon> Nodes(IHolon root)
        { yield return root; foreach (var child in root.Children) foreach (var item in Nodes(child)) yield return item; }
        static IHolon Ack(OASISResult<IEnumerable<IHolon>> result)
        { Assert.False(result.IsError, result.Message + result.Exception); return Assert.Single(result.Result); }
        async Task<(IHolon Value, Guid Operation)> Commit(IHolon value, bool create = false,
            ConditionalHolonOperation operation = ConditionalHolonOperation.Save, bool children = false)
        {
            var identity = Guid.NewGuid();
            var conditions = (children ? Nodes(value) : new[] { value }).Select(item => new HolonWriteCondition(item.Id,
                create ? null : CosmicConcurrency.GetProviderRevisionId(item, sourceType)));
            var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(identity, actor, sourceType, conditions), operation, new[] { value }, saveChildren: children);
            var enrollment=new HolonReplicationEnrollment(sourceId,new[]{"owned-cross-target"});
            var result=synchronous ? ((IAtomicHolonReplicationStorageProvider)source).ExecuteReplicatingHolonWrite(request,enrollment)
                    :await ((IAtomicHolonReplicationStorageProvider)source).ExecuteReplicatingHolonWriteAsync(request,enrollment);
            return (Ack(result), identity);
        }
        CommittedHolonReplicaRequest Delivery((IHolon Value, Guid Operation) item, bool children = false,
            ConditionalHolonOperation operation = ConditionalHolonOperation.Save)
            => new(item.Operation, sourceId, sourceType, actor, operation, new[] { item.Value }, saveChildren: children);
        async Task<OASISResult<IEnumerable<IHolon>>> Apply(CommittedHolonReplicaRequest request)
        {
            var receiver = (ICommittedHolonReplicaStorageProvider)target;
            return synchronous ? receiver.ApplyCommittedHolonReplica(request) : await receiver.ApplyCommittedHolonReplicaAsync(request);
        }
        async Task Hosted(CommittedHolonReplicaRequest request)
        {
            var adapter = new CommittedHolonReplicaTarget("owned-cross-target", (ICommittedHolonReplicaStorageProvider)target,
                targetType, sourceId, sourceType);
            IHolonReplicationRecoveryOutbox store=mongoSource ? new MongoHolonReplicationOutbox(mongo.Database):new SqliteHolonReplicationOutbox(connection);
            var worker=new HolonOutboxDispatcher(store,new[]{adapter},"owned-native-worker");
            var delivered=await worker.DispatchOnceAsync("owned-cross-target",maximumCount:1);
            Assert.False(delivered.IsError,delivered.Message+delivered.Exception);Assert.Equal(1,delivered.Result.CompletedCount);
            if(mongoSource)
            {
                var obligation=await database.GetCollection<BsonDocument>("CosmicHolonOutbox")
                    .Find(new BsonDocument{{"OperationId",request.OperationId.ToString("D")},{"TargetId","owned-cross-target"}}).SingleAsync();
                var inspected=await store.InspectAsync(obligation["Sequence"].AsInt64);Assert.False(inspected.IsError,inspected.Message+inspected.Exception);
                Assert.Equal(HolonDeliveryState.Completed,inspected.Result.State);Assert.NotNull(inspected.Result.RecoveryEvidence);
                Assert.False(inspected.Result.RecoveryEvidence.RequiresNativeReconciliation);return;
            }
            using var inspect=new SqliteConnection(connection);inspect.Open();using var query=inspect.CreateCommand();
            query.CommandText="SELECT DeliveryState FROM CosmicHolonOutbox WHERE OperationId=$operation AND TargetId='owned-cross-target'";
            query.Parameters.AddWithValue("$operation",request.OperationId.ToString("D"));Assert.Equal("Completed",query.ExecuteScalar());
        }
        async Task<IHolon> Fresh(IOASISStorageProvider reader, Guid id, bool children = true)
        {
            var result = synchronous ? reader.LoadHolon(id, loadChildren: children) : await reader.LoadHolonAsync(id, loadChildren: children);
            Assert.False(result.IsError, result.Message + result.Exception); return result.Result;
        }
        void VerifyActual((IHolon Value, Guid Operation) item, IHolon read, ProviderType authority, bool children)
        {
            var intent = Delivery(item, children);
            var plan = CommittedHolonReplicaPlan.Create(intent, authority);
            foreach (var record in Nodes(read)) plan.ValidateAcknowledgedRecord(record);
        }
        try
        {
            Assert.False((await mongo.ActivateProviderAsync()).IsError); Assert.False((await sqlite.ActivateProviderAsync()).IsError);
            var date = new DateTime(1960, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234567);
            var child = new Holon { Id = Guid.NewGuid(), Name = "owned cross child", MetaData = new() { ["when"] = date.AddTicks(11) } };
            var input = new Holon { Id = Guid.NewGuid(), Name = "owned cross root", CustomKey = "owned/cross", ParentHolonId2 = Guid.NewGuid(),
                ParentHolonId3 = Guid.NewGuid(), ParentHolonIds = new List<Guid> { Guid.NewGuid() }, ChildrenIds = new List<Guid> { child.Id },
                ChildIdListCache = "owned child cache", AllChildIdListCache = "owned all cache", Children = new List<IHolon> { child },
                MetaData = new() { ["when"] = date, ["integer"] = 9007199254740993L, ["bytes"] = new byte[] { 0, 17, 255 },
                    ["tree"] = new JObject { ["when"] = date, ["flags"] = new JArray(true, false), ["nested"] = new JObject { ["value"] = "owned" } } },
                ProviderMetaData = new() { [ProviderType.Neo4jOASIS] = new() { ["origin"] = "portable" }, [targetType] = new() { ["target-spoof"] = "discard" } },
                ProviderUniqueStorageKey = new() { [targetType] = "owned invalid remote target key" } };
            var first = await Commit(input, create: true, children: true); var firstRequest = Delivery(first, children: true);
            Assert.False((await mongoRead.ActivateProviderAsync()).IsError); Assert.False((await sqliteRead.ActivateProviderAsync()).IsError);
            // Read through independent provider contexts before transporting any acknowledgement.
            VerifyActual(first, await Fresh(sourceRead, first.Value.Id), sourceType, true);
            var sourceSnapshot = StoredRecordJson.Serialize<IHolon>(first.Value);
            await Hosted(firstRequest);
            var copied = Ack(await Apply(firstRequest)); Assert.Equal(sourceSnapshot, StoredRecordJson.Serialize<IHolon>(first.Value));
            VerifyActual(first, copied, targetType, true); VerifyActual(first, await Fresh(targetRead, first.Value.Id), targetType, true);
            Assert.NotEqual(CosmicConcurrency.GetProviderRevisionId(first.Value, sourceType), CosmicConcurrency.GetProviderRevisionId(copied, targetType));
            Assert.False(copied.ProviderMetaData[targetType].ContainsKey("target-spoof"));
            Assert.Equal(first.Value.VersionId, copied.VersionId); Assert.Equal(actor, copied.CreatedByAvatarId);
            Assert.Equal(first.Value.CreatedDate, copied.CreatedDate); Assert.Null(copied.CreatedOASISType);
            Assert.Equal(first.Value.CreatedProviderType?.Value, copied.CreatedProviderType?.Value);
            var edit = StoredRecordJson.Project<Holon>(first.Value); edit.Name = "owned cross second";
            var second = await Commit(edit); var secondRequest = Delivery(second);
            edit = StoredRecordJson.Project<Holon>(second.Value); edit.Name = "owned cross third";
            var third = await Commit(edit); var thirdRequest = Delivery(third);
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", (await Apply(thirdRequest)).ErrorCode);
            await Hosted(secondRequest); Ack(await Apply(secondRequest));
            await Hosted(thirdRequest); Ack(await Apply(thirdRequest));
            VerifyActual(third, await Fresh(targetRead, first.Value.Id, false), targetType, false);
            Assert.Equal(first.Value.VersionId, Ack(await Apply(firstRequest)).VersionId);
            Assert.Equal(third.Value.VersionId, (await Fresh(targetRead, first.Value.Id, false)).VersionId);
            var sourceChild = await Fresh(sourceRead, child.Id, false); var targetChild = await Fresh(targetRead, child.Id, false);
            Assert.Equal(sourceChild.VersionId, targetChild.VersionId); Assert.Equal(sourceChild.CreatedDate, targetChild.CreatedDate);
            var soft = await Commit(third.Value, operation: ConditionalHolonOperation.SoftDelete);
            await Hosted(Delivery(soft, operation: ConditionalHolonOperation.SoftDelete));
            var softCopy = Ack(await Apply(Delivery(soft, operation: ConditionalHolonOperation.SoftDelete)));
            Assert.True(softCopy.IsDeleted); Assert.Equal(soft.Value.DeletedDate, softCopy.DeletedDate);
            var hard = await Commit(soft.Value, operation: ConditionalHolonOperation.HardDelete);
            var hardRequest = Delivery(hard, operation: ConditionalHolonOperation.HardDelete);
            await Hosted(hardRequest);
            var deleted = await Apply(hardRequest); Assert.False(deleted.IsError, deleted.Message + deleted.Exception); Assert.True(deleted.IsDeleted);
            Assert.Equal(hard.Value.VersionId, Ack(await Apply(hardRequest)).VersionId);
            Assert.Equal(first.Value.VersionId, Ack(await Apply(firstRequest)).VersionId);
            Assert.Null(await Fresh(targetRead, first.Value.Id, false));
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", (await Apply(new CommittedHolonReplicaRequest(Guid.NewGuid(), sourceId, sourceType, actor,
                ConditionalHolonOperation.Save, new[] { first.Value }, saveChildren: true))).ErrorCode);
            complete = true;
        }
        finally
        {
            await mongoRead.DeActivateProviderAsync(); await mongo.DeActivateProviderAsync();
            await sqliteRead.DeActivateProviderAsync(); await sqlite.DeActivateProviderAsync();
            sqliteRead.Dispose(); sqlite.Dispose();
            if (complete)
            {
                var cleanup = new[] { path, path + "-wal", path + "-shm" }.Select(Path.GetFullPath).ToArray();
                if (cleanup.Any(candidate => !string.Equals(Path.GetDirectoryName(candidate), directory, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Unexpected owned fixture cleanup path.");
                await client.DropDatabaseAsync(databaseName);
                foreach (var candidate in cleanup) if (File.Exists(candidate)) File.Delete(candidate);
            }
        }
    }
}
