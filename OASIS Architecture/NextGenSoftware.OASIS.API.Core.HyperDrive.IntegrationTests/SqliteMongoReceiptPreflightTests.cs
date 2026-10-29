using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class SqliteMongoReceiptPreflightTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeSqliteMongoPreflightDetectsIndependentTargetEditsAndHardDelete(bool editTarget)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.NotNull(root);
        var directory = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent"); Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(directory, "sqlite-mongo-preflight.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var sourcePath = Path.Combine(directory, "sqlite-mongo-preflight.db");
        var journal = Path.Combine(directory, "sqlite-mongo-preflight.pending.json");
        foreach (var path in new[] { sourcePath, sourcePath + "-wal", sourcePath + "-shm", journal }) Assert.False(File.Exists(path), "Preserve unfinished original outcomes.");
        const string databaseName = "cosmic_sqlite_mongo_preflight_fixture";
        var client = new MongoClient(uri);
        using (var names = await client.ListDatabaseNamesAsync()) Assert.DoesNotContain(databaseName, await names.ToListAsync());
        var input = new Holon { Id = Guid.NewGuid(), Name = "source-preflight-value" };
        var actor = Guid.NewGuid(); var createOperation = Guid.NewGuid(); var deleteOperation = Guid.NewGuid();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file, new { sourcePath, databaseName, input.Id, actor, createOperation, deleteOperation, editTarget });
            file.Flush(true);
        }
        var source = new SqliteProvider($"Data Source={sourcePath};Pooling=False");
        var target = new MongoProvider(uri, databaseName); var complete = false;
        try
        {
            Assert.False((await source.ActivateProviderAsync()).IsError);
            Assert.False((await target.ActivateProviderAsync()).IsError);
            var sourceBinding = await source.CaptureStorageIdentityAsync(); Assert.False(sourceBinding.IsError);
            var targetBinding = await target.CaptureStorageIdentityAsync(); Assert.False(targetBinding.IsError);
            var enrollment = new HolonReplicationEnrollment("owned-sqlite-mongo-preflight", new[] { "target", "target-b", "\uE000", "\U0001F600" });
            var original = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(createOperation, actor,
                ProviderType.SQLLiteDBOASIS, new[] { new HolonWriteCondition(input.Id, null) }, enrollment),
                ConditionalHolonOperation.Save, new[] { input }, saveChildren: false);
            var saved = await source.ExecuteReplicatingHolonWriteAsync(original, enrollment); Assert.False(saved.IsError, saved.Message + saved.Exception);
            var acknowledged = saved.Result.Single();
            ConditionalHolonReceiptEvidenceRequest Evidence(ConditionalHolonWriteRequest request)
                => new(request, sourceBinding.Result, targetBinding.Result, new[] { input.Id });
            var missing = await HolonReplicationReceiptPreflight.ReadAsync(source, target, Evidence(original));
            Assert.False(missing.IsError, missing.Message + missing.Exception);
            Assert.Equal(HolonReplicationReceiptComparisonKind.MissingReplicaHead, missing.Result.Records[0].Kind);
            CommittedHolonReplicaRequest originalReplica = null!;
            string originalAcknowledgement = null!;
            async Task Apply(ConditionalHolonWriteRequest request, IHolon row)
            {
                var transport = new CommittedHolonReplicaRequest(request.Write.OperationId,
                    enrollment.SourceId, ProviderType.SQLLiteDBOASIS, actor, request.Operation, new[] { row }, saveChildren: false);
                await CosmicNativeAcceptance.CosmicReplicaReceiptProbe.ObserveAsync(target, transport, targetBinding.Result, ConditionalHolonReceiptState.Absent);
                var applied = await target.ApplyBoundCommittedHolonReplicaAsync(transport, targetBinding.Result);
                Assert.False(applied.IsError, applied.Message + applied.Exception);
                var digest = await CosmicNativeAcceptance.CosmicReplicaReceiptProbe.ObserveAsync(target, transport, targetBinding.Result, ConditionalHolonReceiptState.Committed);
                if (request.Write.OperationId == createOperation) { originalReplica = transport; originalAcknowledgement = digest; }
            }
            await Apply(original, acknowledged);
            var boundSource = source.GetBoundHolonReplicationOutbox(sourceBinding.Result);
            Assert.False(boundSource.IsError, boundSource.Message + boundSource.Exception);
            await CosmicNativeAcceptance.CosmicOperationInventoryProbe.CompleteAlreadyAppliedTargetAsync(
                boundSource.Result, sourceBinding.Result, createOperation, "target");
            await CosmicNativeAcceptance.CosmicOperationInventoryProbe.VerifyAsync(boundSource.Result, sourceBinding.Result,
                createOperation, enrollment.SourceId, actor, enrollment.TargetIds, requireCompleted: true, migrationSource: source);

            var matched = await HolonReplicationReceiptPreflight.ReadAsync(source, target, Evidence(original));
            Assert.False(matched.IsError, matched.Message + matched.Exception);
            Assert.Equal(HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt, matched.Result.Records[0].Kind);
            await CosmicNativeAcceptance.CosmicLineageInventoryProbe.VerifyPreviewAsync(boundSource.Result,
                sourceBinding.Result, targetBinding.Result, enrollment.SourceId, createOperation, enrollment.TargetIds.Count,
                () => HolonReplicationReceiptPreflight.ReadAsync(source, target, Evidence(original)));
            if (editTarget)
            {
                var collection = client.GetDatabase(databaseName).GetCollection<BsonDocument>("Holon");
                var before = await collection.Find(new BsonDocument("HolonId", input.Id.ToString("D"))).SingleAsync();
                var changed = await collection.UpdateOneAsync(new BsonDocument("HolonId", input.Id.ToString("D")),
                    new BsonDocument("$set", new BsonDocument("Name", "out-of-band-change")));
                Assert.Equal(1L, changed.ModifiedCount);
                var after = await collection.Find(new BsonDocument("HolonId", input.Id.ToString("D"))).SingleAsync();
                Assert.Equal(before["ProviderRevisionId"], after["ProviderRevisionId"]);
                Assert.NotEqual(before["Name"], after["Name"]);
                var divergence = await HolonReplicationReceiptPreflight.ReadAsync(source, target, Evidence(original));
                Assert.True(divergence.IsError, "Changed current content must invalidate protected target evidence.");
                Assert.Null(divergence.Result);
                Assert.Equal("COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED", divergence.ErrorCode);
                Assert.Equal(originalAcknowledgement, await CosmicNativeAcceptance.CosmicReplicaReceiptProbe.ObserveAsync(
                    target, originalReplica, targetBinding.Result, ConditionalHolonReceiptState.Committed));
                var retained = await collection.Find(new BsonDocument("HolonId", input.Id.ToString("D"))).SingleAsync();
                Assert.Equal(after.ToJson(), retained.ToJson()); // Evidence reads must not repair the divergent record.
            }
            else
            {
                var deletion = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(deleteOperation, actor,
                    ProviderType.SQLLiteDBOASIS, new[] { new HolonWriteCondition(input.Id,
                        CosmicConcurrency.GetProviderRevisionId(acknowledged, ProviderType.SQLLiteDBOASIS)) }, enrollment),
                    ConditionalHolonOperation.HardDelete, new[] { StoredRecordJson.Project<Holon>(acknowledged) }, saveChildren: false);
                NextGenSoftware.OASIS.Common.OASISResult<System.Collections.Generic.IEnumerable<IHolon>> deleted = null!;
                await CosmicNativeAcceptance.CosmicLineageInventoryProbe.VerifyWindowAsync(boundSource.Result, sourceBinding.Result,
                    enrollment.SourceId, createOperation, enrollment.TargetIds.Count, deleteOperation, async () =>
                    {
                        deleted = await source.ExecuteReplicatingHolonWriteAsync(deletion, enrollment);
                        Assert.False(deleted.IsError, deleted.Message + deleted.Exception);
                    });
                await Apply(deletion, deleted.Result.Single());
                await CosmicNativeAcceptance.CosmicOperationInventoryProbe.VerifyAsync(boundSource.Result, sourceBinding.Result,
                    createOperation, enrollment.SourceId, actor, enrollment.TargetIds, requireCompleted: true, migrationSource: source);

                var tombstone = await HolonReplicationReceiptPreflight.ReadAsync(source, target, Evidence(deletion));
                Assert.False(tombstone.IsError, tombstone.Message + tombstone.Exception);
                Assert.Equal(HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt, tombstone.Result.Records[0].Kind);
                Assert.True(tombstone.Result.Records[0].Original.HardDeleted); Assert.False(tombstone.Result.Records[0].Target.HasCurrentRecord);
            }
            complete = true;
        }
        finally
        {
            Assert.False((await target.DeActivateProviderAsync()).IsError); source.Dispose();
            if (complete)
            {
                await client.DropDatabaseAsync(databaseName);
                using var names = await client.ListDatabaseNamesAsync(); Assert.DoesNotContain(databaseName, await names.ToListAsync());
                foreach (var path in new[] { sourcePath, sourcePath + "-wal", sourcePath + "-shm" }) if (File.Exists(path)) File.Delete(path);
                File.Delete(journal);
            }
        }
    }
}
