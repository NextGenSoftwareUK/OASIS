using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using Native = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicConditionalWriteTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCommittedReplicasBindSourceLineageGraphHeadAndReceipt(bool synchronous)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        using var lease = File.Open(Path.Combine(workspace, "TestResults/SubscriptionUsageCurrent/cosmic-conditional-environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        const string sourceName = "cosmic_committed_replica_source_fixture", targetName = "cosmic_committed_replica_target_fixture";
        var client = new MongoClient(uri); using var names = await client.ListDatabaseNamesAsync(); var existing = await names.ToListAsync();
        Assert.DoesNotContain(sourceName, existing); Assert.DoesNotContain(targetName, existing);
        var sourceDb = client.GetDatabase(sourceName); var targetDb = client.GetDatabase(targetName);
        await sourceDb.CreateCollectionAsync("CosmicOwnedFixture"); await targetDb.CreateCollectionAsync("CosmicOwnedFixture");
        var source = new Provider(uri, sourceName); var target = new Provider(uri, targetName); var other = new Provider(uri, targetName);
        var complete = false; var actor = Guid.NewGuid();
        var rows = targetDb.GetCollection<BsonDocument>("Holon"); var heads = targetDb.GetCollection<BsonDocument>("CosmicReplicaHead");
        var receipts = targetDb.GetCollection<BsonDocument>("CosmicWriteReceipt");
        Task<OASISResult<IEnumerable<IHolon>>> SourceWrite(IHolon value, Guid? expected, ConditionalHolonOperation operation = ConditionalHolonOperation.Save, bool children = false)
        {
            var conditions = HolonGraphWritePlan.Create(new[] { value }, children, true, 0, 0).Nodes
                .Select(node => new HolonWriteCondition(node.Snapshot.Id, node.Snapshot.Id == value.Id ? expected : (Guid?)null));
            var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.MongoDBOASIS, conditions), operation, new[] { value }, saveChildren: children);
            return synchronous ? Task.Run(() => source.ExecuteConditionalHolonWrite(request)) : source.ExecuteConditionalHolonWriteAsync(request);
        }
        Guid Token(IHolon value) => CosmicConcurrency.GetProviderRevisionId(value, ProviderType.MongoDBOASIS)!.Value;
        CommittedHolonReplicaRequest Request(IHolon value, ConditionalHolonOperation operation = ConditionalHolonOperation.Save, bool children = false,
            Guid? identity = null, string origin = "owned mongo source")
            => new(identity ?? Guid.NewGuid(), origin, ProviderType.MongoDBOASIS, actor, operation, new[] { value }, saveChildren: children);
        Task<OASISResult<IEnumerable<IHolon>>> Apply(Provider provider, CommittedHolonReplicaRequest request)
            => synchronous ? Task.Run(() => provider.ApplyCommittedHolonReplica(request)) : provider.ApplyCommittedHolonReplicaAsync(request);
        async Task<byte[]> Body(Guid id) => (await rows.Find(new BsonDocument("HolonId", id.ToString("D"))).SingleAsync()).ToBson();
        IHolon Ack(OASISResult<IEnumerable<IHolon>> result) { Assert.False(result.IsError, result.Message + result.Exception); return Assert.Single(result.Result); }
        try
        {
            Assert.False((await source.ActivateProviderAsync()).IsError); Assert.False((await target.ActivateProviderAsync()).IsError);
            Assert.False((await other.ActivateProviderAsync()).IsError);
            var capturedChild = new Holon { Id = Guid.NewGuid(), Name = "owned captured child" };
            var capturedRoot = new ReadOnceChildHolon { Id = Guid.NewGuid(), Name = "owned captured root", Children = new List<IHolon> { capturedChild } };
            var capturedSave = synchronous ? source.SaveHolon(capturedRoot, saveChildren: true)
                : await source.SaveHolonAsync(capturedRoot, saveChildren: true);
            Assert.False(capturedSave.IsError, capturedSave.Message + capturedSave.Exception);
            Assert.Equal(1, capturedRoot.Reads); Assert.Equal(capturedChild.Id, Assert.Single(capturedSave.Result.Children).Id);
            var child = new Holon { Id = Guid.NewGuid(), Name = "owned replica child", CustomKey = "owned/child" };
            var input = new Holon { Id = Guid.NewGuid(), Name = "owned replica root", CustomKey = "owned/root", ParentHolonId2 = Guid.NewGuid(),
                ParentHolonId3 = Guid.NewGuid(), ParentHolonIds = new List<Guid> { Guid.NewGuid() }, ChildrenIds = new List<Guid> { child.Id },
                Children = new List<IHolon> { child }, MetaData = new() { ["integer"] = 9007199254740993L },
                ProviderMetaData = new() { [ProviderType.SQLLiteDBOASIS] = new() { ["origin"] = "portable" }, [ProviderType.MongoDBOASIS] = new() { ["source-local"] = "not target" } } };
            var first = Ack(await SourceWrite(input, null, children: true)); var firstChild = Assert.Single(first.Children);
            var firstRequest = Request(first, children: true); var before = StoredRecordJson.Serialize<IHolon>(first);
            var copied = Ack(await Apply(target, firstRequest));
            Assert.Equal(before, StoredRecordJson.Serialize<IHolon>(first)); Assert.Equal(first.VersionId, copied.VersionId);
            Assert.NotEqual(Token(first), Token(copied)); Assert.NotEqual(first.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS], copied.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
            var plan = CommittedHolonReplicaPlan.Create(firstRequest, ProviderType.MongoDBOASIS);
            plan.ValidateAcknowledgedRecord(copied); plan.ValidateAcknowledgedRecord(Assert.Single(copied.Children));
            var nativeFirst = BsonSerializer.Deserialize<Native>(await Body(first.Id));
            Assert.Equal(first.CreatedDate.Ticks, nativeFirst.CreatedDate.Ticks);
            Assert.Equal(firstChild.CreatedDate.Ticks, Assert.Single(nativeFirst.Children).CreatedDate.Ticks);
            Assert.Equal(2, await heads.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(1, await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.False(copied.ProviderMetaData[ProviderType.MongoDBOASIS].ContainsKey("source-local"));
            Assert.Equal("portable", copied.ProviderMetaData[ProviderType.SQLLiteDBOASIS]["origin"]);
            await other.DeActivateProviderAsync(); other = new Provider(uri, targetName); Assert.False((await other.ActivateProviderAsync()).IsError);
            Assert.Equal(copied.VersionId, Ack(await Apply(other, firstRequest)).VersionId);
            var altered = StoredRecordJson.Project<Holon>(first); altered.Name = "owned altered intent";
            Assert.Equal("COSMIC_OPERATION_ID_REUSED", (await Apply(target, Request(altered, children: true, identity: firstRequest.OperationId))).ErrorCode);
            Assert.Equal("COSMIC_OPERATION_ID_REUSED", (await Apply(target, Request(first, children: true, identity: firstRequest.OperationId, origin: "owned other source"))).ErrorCode);

            var edit = StoredRecordJson.Project<Holon>(first); edit.Name = "owned source second";
            var second = Ack(await SourceWrite(edit, Token(first))); var secondRequest = Request(second);
            edit = StoredRecordJson.Project<Holon>(second); edit.Name = "owned source third";
            var third = Ack(await SourceWrite(edit, Token(second))); var thirdRequest = Request(third);
            var unchanged = await Body(first.Id);
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", (await Apply(target, thirdRequest)).ErrorCode); Assert.Equal(unchanged, await Body(first.Id));
            var workers = await Task.WhenAll(Apply(target, secondRequest), Apply(other, secondRequest));
            Assert.Contains(workers, result => !result.IsError);
            foreach (var loser in workers.Where(result => result.IsError)) Assert.Equal("COSMIC_CONCURRENT_WRITE", loser.ErrorCode);
            Assert.Equal(second.VersionId, Ack(await Apply(other, secondRequest)).VersionId);
            await VerifyCommittedReplicaUncertainCommit(uri, targetName, thirdRequest, synchronous);
            Assert.Equal(third.VersionId, Ack(await Apply(target, thirdRequest)).VersionId);
            nativeFirst = BsonSerializer.Deserialize<Native>(await Body(first.Id));
            Assert.Equal(firstChild.CreatedDate, Assert.Single(nativeFirst.Children).CreatedDate); // excluded frontier survives.

            // Authoritative server rejection of either auxiliary write rolls back graph + heads + receipt.
            foreach (var collection in new[] { "CosmicReplicaHead", "CosmicWriteReceipt" })
            {
                var extra = Ack(await SourceWrite(new Holon { Id = Guid.NewGuid(), Name = "owned rollback " + collection }, null));
                var attempt = Request(extra);
                await targetDb.RunCommandAsync<BsonDocument>(new BsonDocument { { "collMod", collection },
                    { "validator", new BsonDocument("$jsonSchema", new BsonDocument("required", new BsonArray { "OwnedImpossible" })) } });
                try
                {
                    var rejected = await Apply(target, attempt); Assert.True(rejected.IsError);
                    Assert.Equal(0, await rows.CountDocumentsAsync(new BsonDocument("HolonId", extra.Id.ToString("D"))));
                    Assert.Equal(0, await heads.CountDocumentsAsync(new BsonDocument("_id", extra.Id.ToString("D"))));
                    Assert.Equal(0, await receipts.CountDocumentsAsync(new BsonDocument("_id", attempt.OperationId.ToString("D"))));
                }
                finally { await targetDb.RunCommandAsync<BsonDocument>(new BsonDocument { { "collMod", collection }, { "validator", new BsonDocument() } }); }
                Assert.Equal(extra.VersionId, Ack(await Apply(target, attempt)).VersionId);
            }

            // Local raw mutation retains the logical ID but invalidates the target physical anchor.
            var localSource = Ack(await SourceWrite(new Holon { Id = Guid.NewGuid(), Name = "owned local conflict" }, null));
            var localCopy = Ack(await Apply(target, Request(localSource)));
            var localEdit = StoredRecordJson.Project<Holon>(localCopy); localEdit.Name = "owned independent local edit";
            var raw = await target.SaveHolonAsync(localEdit, saveChildren: false); Assert.False(raw.IsError, raw.Message + raw.Exception);
            var nextLocal = StoredRecordJson.Project<Holon>(localSource); nextLocal.Name = "owned source local second";
            var nextSource = Ack(await SourceWrite(nextLocal, Token(localSource)));
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", (await Apply(target, Request(nextSource))).ErrorCode);

            var storedHead = await heads.Find(new BsonDocument("_id", first.Id.ToString("D"))).SingleAsync();
            await heads.UpdateOneAsync(new BsonDocument("_id", first.Id.ToString("D")), new BsonDocument("$set", new BsonDocument("SnapshotDigest", "owned corrupt checksum")));
            var fourth = Ack(await SourceWrite(third, Token(third), ConditionalHolonOperation.SoftDelete)); var fourthRequest = Request(fourth, ConditionalHolonOperation.SoftDelete);
            Assert.Equal("COSMIC_REPLICA_HEAD_INVALID", (await Apply(target, fourthRequest)).ErrorCode);
            await heads.ReplaceOneAsync(new BsonDocument("_id", first.Id.ToString("D")), storedHead);
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", (await Apply(target, Request(fourth, ConditionalHolonOperation.SoftDelete, origin: "owned other source"))).ErrorCode);
            var soft = Ack(await Apply(target, fourthRequest)); Assert.True(soft.IsDeleted); Assert.Equal(fourth.DeletedDate, soft.DeletedDate);
            var fifth = Ack(await SourceWrite(fourth, Token(fourth), ConditionalHolonOperation.HardDelete)); var fifthRequest = Request(fifth, ConditionalHolonOperation.HardDelete);
            var hard = await Apply(target, fifthRequest); Assert.False(hard.IsError, hard.Message + hard.Exception); Assert.True(hard.IsDeleted);
            Assert.Equal(0, await rows.CountDocumentsAsync(new BsonDocument("HolonId", first.Id.ToString("D"))));
            Assert.True((await heads.Find(new BsonDocument("_id", first.Id.ToString("D"))).SingleAsync())["HardDeleted"].AsBoolean);
            Assert.Equal(fifth.VersionId, Ack(await Apply(other, fifthRequest)).VersionId);
            Assert.Equal(first.VersionId, Ack(await Apply(target, firstRequest)).VersionId);
            Assert.Equal(0, await rows.CountDocumentsAsync(new BsonDocument("HolonId", first.Id.ToString("D"))));
            Assert.Equal(1, await rows.CountDocumentsAsync(new BsonDocument("HolonId", firstChild.Id.ToString("D"))));
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", (await Apply(target, Request(first, children: true))).ErrorCode);
            complete = true;
        }
        finally
        {
            await source.DeActivateProviderAsync(); await target.DeActivateProviderAsync(); await other.DeActivateProviderAsync();
            if (complete) { await client.DropDatabaseAsync(sourceName); await client.DropDatabaseAsync(targetName); }
        }
    }
    private async Task VerifyCommittedReplicaUncertainCommit(string uri, string databaseName, CommittedHolonReplicaRequest request, bool synchronous)
    {
        const string app = "cosmic-committed-replica-original-commit";
        var settings = MongoClientSettings.FromConnectionString(uri + "&appName=" + app);
        var commits = 0; var aborts = 0; var writes = 0;
        settings.ClusterConfigurator = cluster => cluster.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(command => {
            if (command.CommandName == "commitTransaction") System.Threading.Interlocked.Increment(ref commits);
            if (command.CommandName == "abortTransaction") System.Threading.Interlocked.Increment(ref aborts);
            if (command.CommandName is "insert" or "update" or "delete") System.Threading.Interlocked.Increment(ref writes);
        });
        var monitored = new MongoClient(settings); var fault = new Provider(uri + "&appName=" + app, databaseName);
        Assert.False((await fault.ActivateProviderAsync()).IsError);
        fault.Database.MongoClient = monitored; fault.Database.MongoDB = monitored.GetDatabase(databaseName);
        var admin = new MongoClient(uri).GetDatabase("admin");
        try
        {
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "alwaysOn" },
                { "data", new BsonDocument { { "failCommands", new BsonArray { "commitTransaction" } }, { "appName", app },
                    { "writeConcernError", new BsonDocument { { "code", 64 }, { "errmsg", "owned committed replica post-commit acknowledgement fault" } } } } } });
            var failed = synchronous ? fault.ApplyCommittedHolonReplica(request) : await fault.ApplyCommittedHolonReplicaAsync(request);
            Assert.True(failed.IsError); Assert.Null(failed.Result); Assert.Equal("COSMIC_WRITE_RECONCILIATION_REQUIRED", failed.ErrorCode);
            var evidence = Assert.IsType<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoTransactionReconciliationException>(failed.Exception);
            var cause = Assert.IsType<MongoWriteConcernException>(evidence.InnerException);
            Assert.False(string.IsNullOrWhiteSpace(evidence.SessionId)); Assert.True(evidence.TransactionNumber > 0);
            Assert.Equal(0, aborts); Assert.True(commits >= 1); Assert.Equal(3, writes);
            var reply = cause.WriteConcernResult.Response; Assert.True(reply.Contains("operationTime"));
            var database = new MongoClient(uri).GetDatabase(databaseName);
            var observed = await database.RunCommandAsync<BsonDocument>(new BsonDocument {
                { "find", "CosmicWriteReceipt" }, { "filter", new BsonDocument("_id", request.OperationId.ToString("D")) },
                { "readConcern", new BsonDocument { { "level", "majority" }, { "afterClusterTime", reply["operationTime"] } } },
                { "maxTimeMS", 5000 }, { "singleBatch", true }, { "batchSize", 2 } });
            var receipt = Assert.Single(observed["cursor"]["firstBatch"].AsBsonArray).AsBsonDocument;
            var exact = Assert.Single(ConditionalHolonReceipt.Read(receipt["ResultJson"].AsString,
                CommittedHolonReplicaPlan.Create(request, ProviderType.MongoDBOASIS)));
            var head = await database.GetCollection<BsonDocument>("CosmicReplicaHead").WithReadConcern(ReadConcern.Majority)
                .Find(new BsonDocument("_id", exact.Id.ToString("D"))).SingleAsync();
            var row = await database.GetCollection<BsonDocument>("Holon").WithReadConcern(ReadConcern.Majority)
                .Find(new BsonDocument("HolonId", exact.Id.ToString("D"))).SingleAsync();
            Assert.Equal(exact.VersionId.ToString("D"), row["VersionId"].AsString);
            Assert.Equal(CosmicConcurrency.GetProviderRevisionId(exact, ProviderType.MongoDBOASIS)!.Value.ToString("D"), head["ProviderRevisionId"].AsString);
            output.WriteLine($"Replica original operation={request.OperationId:D}, source={request.SourceId}, session={evidence.SessionId}, transaction={evidence.TransactionNumber}; majority-visible graph/head/receipt proven before replay.");
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            var originalCommits = commits; var originalWrites = writes;
            var replay = synchronous ? fault.ApplyCommittedHolonReplica(request) : await fault.ApplyCommittedHolonReplicaAsync(request);
            Assert.False(replay.IsError, replay.Message + replay.Exception);
            Assert.Equal(exact.VersionId, Assert.Single(replay.Result).VersionId);
            Assert.Equal(originalCommits, commits); Assert.Equal(originalWrites, writes);
        }
        finally
        {
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            await fault.DeActivateProviderAsync();
        }
    }

    private sealed class ReadOnceChildHolon : Holon, IHolon
    {
        private IList<IHolon> selected = new List<IHolon>();
        [Newtonsoft.Json.JsonIgnore]
        public int Reads { get; private set; }
        public new IList<IHolon> Children
        {
            get => ++Reads == 1 ? selected : new List<IHolon>();
            set => selected = value;
        }
    }

}
