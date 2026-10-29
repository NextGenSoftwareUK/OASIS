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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;
using Mongo = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using Sqlite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class SqliteMongoMigrationAdoptionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task NativeMongoGraphAdoptionRejectsContentDriftAndRollsBackWithReceiptFailure(bool failReceipt, bool nativeLineage)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-CosmicMongoDestinationTests.ps1"))) root = root.Parent;
        Assert.NotNull(root);
        var folder = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent"); Directory.CreateDirectory(folder);
        var sourcePath = Path.Combine(folder, "sqlite-mongo-adoption.db");
        var journal = Path.Combine(folder, "sqlite-mongo-adoption.pending.json");
        var planFolder = Path.Combine(folder, "adoption-plans");
        foreach (var path in new[] { sourcePath, sourcePath + "-wal", sourcePath + "-shm", journal }) Assert.False(File.Exists(path), "Preserve original outcomes.");
        const string databaseName = "cosmic_sqlite_mongo_adoption_fixture";
        var client = new MongoClient(uri); using (var names = await client.ListDatabaseNamesAsync()) Assert.DoesNotContain(databaseName, await names.ToListAsync());
        var parent = new Holon { Id = Guid.NewGuid(), Name = "owned-mongo-adoption-parent" };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = parent.Id, Name = "owned-mongo-adoption-child" };
        parent.Children = new System.Collections.Generic.List<IHolon> { child };
        var ids = new[] { parent.Id, child.Id }; var actor = Guid.NewGuid(); var operation = Guid.NewGuid(); var migration = Guid.NewGuid();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { await JsonSerializer.SerializeAsync(file, new { sourcePath, databaseName, planFolder, actor, operation, migration, ids, failReceipt, nativeLineage }); file.Flush(true); }
        using (var source = new Sqlite($"Data Source={sourcePath};Pooling=False"))
        {
            var target = new Mongo(uri, databaseName);
            try
            {
                Assert.False((await source.ActivateProviderAsync()).IsError); Assert.False((await target.ActivateProviderAsync()).IsError);
                var sourceBinding = await source.CaptureStorageIdentityAsync(); Assert.False(sourceBinding.IsError);
                var targetBinding = await target.CaptureStorageIdentityAsync(); Assert.False(targetBinding.IsError);
                var enrollment = new HolonReplicationEnrollment(nativeLineage
                    ? NextGenSoftware.OASIS.API.Core.Configuration.HolonReplicationRouteBinding.NativeSourceId(sourceBinding.Result)
                    : "owned-sqlite-mongo-adoption", new[] { "target" });
                var original = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, actor, ProviderType.SQLLiteDBOASIS,
                    ids.Select(id => new HolonWriteCondition(id, null)), enrollment), ConditionalHolonOperation.Save, new[] { parent }, saveChildren: true);
                var saved = await source.ExecuteReplicatingHolonWriteAsync(original, enrollment); Assert.False(saved.IsError, saved.Message + saved.Exception);
                var transport = new CommittedHolonReplicaRequest(operation, enrollment.SourceId, ProviderType.SQLLiteDBOASIS,
                    actor, original.Operation, saved.Result, saveChildren: true);
                var applied = await target.ApplyBoundCommittedHolonReplicaAsync(transport, targetBinding.Result); Assert.False(applied.IsError, applied.Message + applied.Exception);
                var receiptRequest = new CommittedHolonReplicaReceiptReadRequest(transport, targetBinding.Result);
                var originalReceipt = await target.ReadCommittedHolonReplicaReceiptAsync(receiptRequest); Assert.False(originalReceipt.IsError);
                var outbox = source.GetBoundHolonReplicationOutbox(sourceBinding.Result); Assert.False(outbox.IsError);
                await CosmicNativeAcceptance.CosmicOperationInventoryProbe.CompleteAlreadyAppliedTargetAsync(outbox.Result, sourceBinding.Result, operation, "target");
                var evidence = new ConditionalHolonReceiptEvidenceRequest(original, sourceBinding.Result, targetBinding.Result, ids);
                var preview = await CosmicNativeAcceptance.CosmicLineageInventoryProbe.VerifyPreviewAsync(outbox.Result,
                    sourceBinding.Result, targetBinding.Result, enrollment.SourceId, operation, 1,
                    () => HolonReplicationReceiptPreflight.ReadAsync(source, target, evidence),
                    readReplicaReceipt: () => target.ReadCommittedHolonReplicaReceiptAsync(receiptRequest), originalWrite: original, replacementTarget: targetBinding.Result);
                var context = preview.AuthorizationContext(migration, actor, "owned-mongo-adoption-admin");
                var policy = new ScopedHolonReplicationMigrationPolicy(context.AuthorizationPolicyBinding,
                    new[] { new HolonReplicationMigrationGrant(context, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(5)) });
                var participants = new[] { new HolonMigrationBarrierParticipant(sourceBinding.Result, source), new HolonMigrationBarrierParticipant(targetBinding.Result, target) };
                var plan = HolonReplicationMigrationBarrierPlan.Create(preview, context);
                var held = await HolonReplicationMigrationBarrierCoordinator.AcquirePersistedAsync(plan, new(planFolder), policy, participants);
                Assert.False(held.IsError, held.Message + held.Exception);
                var delivery = await HolonMigrationDeliveryReconciliation.ReadAsync(plan, policy, participants, (IHolonReplicationLineageInventoryOutbox)outbox.Result);
                Assert.False(delivery.IsError, delivery.Message + delivery.Exception);
                var replacement = await HolonMigrationReplacementReview.ReadAsync(plan, policy, participants);
                Assert.False(replacement.IsError, replacement.Message + replacement.Exception); Assert.Equal(2, replacement.Result.Records.Count);
                var preparation = new HolonMigrationMutationPreparation(plan, delivery.Result, replacement.Result, "owned-mongo-adoption-mutation");
                var mutationPolicy = new ScopedHolonMigrationMutationPolicy(preparation.MutationPolicyBinding,
                    new[] { new HolonMigrationMutationGrant(preparation, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(5)) });
                var preparationStore = new FileHolonMigrationMutationPreparationStore(planFolder);
                Assert.False((await preparationStore.SaveAsync(preparation, plan, preparation.Fingerprint)).IsError);
                var started = await HolonMigrationMutationCoordinator.StartPersistedAsync(plan, preparation.Fingerprint, preparationStore, policy, mutationPolicy, participants);
                Assert.False(started.IsError, started.Message + started.Exception); Assert.True(started.Result.AllParticipantsStarted);
                var adoption = new HolonMigrationReplicaAdoptionPlan(plan, preparation);
                Assert.Equal(nativeLineage, adoption.OriginalSourceId == adoption.ReplacementSourceId);
                var owner = await target.ReadHolonMigrationFenceAsync(adoption.Fence); Assert.False(owner.IsError);
                Assert.True((await target.AdoptPreparedHolonReplicaAsync(adoption, Guid.NewGuid())).IsError);
                var database = client.GetDatabase(databaseName); var heads = database.GetCollection<BsonDocument>("CosmicReplicaHead");
                var current = database.GetCollection<BsonDocument>("Holon"); var receipts = database.GetCollection<BsonDocument>("CosmicMigrationReplicaAdoption");
                await current.UpdateOneAsync(new BsonDocument("HolonId", child.Id.ToString("D")), new BsonDocument("$set", new BsonDocument("Name", "owned-content-drift")));
                Assert.True((await target.AdoptPreparedHolonReplicaAsync(adoption, owner.Result.OwnerToken)).IsError);
                Assert.Equal(0, await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                await current.UpdateOneAsync(new BsonDocument("HolonId", child.Id.ToString("D")), new BsonDocument("$set", new BsonDocument("Name", child.Name)));
                if (failReceipt)
                {
                await database.RunCommandAsync<BsonDocument>(new BsonDocument { { "create", "CosmicMigrationReplicaAdoption" },
                    { "validator", new BsonDocument("$jsonSchema", new BsonDocument { { "bsonType", "object" }, { "required", new BsonArray { "ownedReceiptFailure" } } }) } });
                var failed = await target.AdoptPreparedHolonReplicaAsync(adoption, owner.Result.OwnerToken); Assert.True(failed.IsError);
                Assert.Equal(2, await heads.CountDocumentsAsync(new BsonDocument("SourceId", adoption.OriginalSourceId)));
                Assert.Equal(0, await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                await database.RunCommandAsync<BsonDocument>(new BsonDocument { { "collMod", "CosmicMigrationReplicaAdoption" }, { "validator", new BsonDocument() }, { "validationLevel", "off" } });
                }
                var adopted = await target.AdoptPreparedHolonReplicaAsync(adoption, owner.Result.OwnerToken);
                Assert.False(adopted.IsError, adopted.Message + adopted.Exception); Assert.True(adopted.IsSaved);
                Assert.Equal(adoption.Fingerprint, adopted.MetaData["COSMIC.MigrationAdoptionReceipt"]);
                Assert.Equal(2, await heads.CountDocumentsAsync(new BsonDocument("SourceId", adoption.ReplacementSourceId)));
                Assert.Equal(1, await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                Assert.False((await target.DeActivateProviderAsync()).IsError); Assert.False((await target.ActivateProviderAsync()).IsError);
                var repeated = await target.AdoptPreparedHolonReplicaAsync(adoption, owner.Result.OwnerToken);
                Assert.False(repeated.IsError, repeated.Message + repeated.Exception); Assert.False(repeated.IsSaved);
                var observed = await target.ReadHolonReplicaHeadsAsync(new(targetBinding.Result, ids)); Assert.False(observed.IsError);
                foreach (var row in adoption.Records)
                {
                    var head = observed.Result.Records.Single(r => r.HolonId == row.Expected.HolonId).Head;
                    Assert.Equal(row.Head.VersionId, head.VersionId); Assert.Equal(row.Head.ProviderRevisionId, head.ProviderRevisionId);
                    Assert.Equal(row.Head.PortableContentDigest, head.PortableContentDigest);
                }
                var history = await target.ReadCommittedHolonReplicaReceiptAsync(receiptRequest);
                Assert.False(history.IsError); Assert.Equal(originalReceipt.Result.AcknowledgementDigest, history.Result.AcknowledgementDigest);
            }
            finally { Assert.False((await target.DeActivateProviderAsync()).IsError); Assert.False((await source.DeActivateProviderAsync()).IsError); }
        }
        await client.DropDatabaseAsync(databaseName); using (var names = await client.ListDatabaseNamesAsync()) Assert.DoesNotContain(databaseName, await names.ToListAsync());
        foreach (var path in new[] { sourcePath, sourcePath + "-wal", sourcePath + "-shm" }) if (File.Exists(path)) File.Delete(path);
        foreach (var extension in new[] { ".barrier-plan.json", ".mutation-preparation.json" }) foreach (var suffix in new[] { "", ".pending", ".lock" })
        { var path = Path.Combine(planFolder, migration.ToString("D") + extension + suffix); if (File.Exists(path)) File.Delete(path); }
        File.Delete(journal);
    }
}
