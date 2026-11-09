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
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public partial class MongoStorageIdentityTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeMongoRecordBarrierBlocksV2OperationsAndPreservesOwner(bool reactivate)
    {
        string extraJournal = null!;
        await RunOwned(async (client, source, other) =>
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
            Assert.NotNull(root);
            extraJournal = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent/mongo-fence.pending.json");
            Assert.False(File.Exists(extraJournal), "Preserve original fence outcome.");
            var actor = Guid.NewGuid(); var migration = Guid.NewGuid();
            var phaseMigration = Guid.NewGuid(); var phaseRaceMigration = Guid.NewGuid();
            var phaseOps = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }; var phaseUnrelatedId = Guid.NewGuid(); var races = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var ops = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            using (var file = new FileStream(extraJournal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await JsonSerializer.SerializeAsync(file, new { owner = "cosmic-mongo-fence", database = First, actor, migration, races, ids, phaseMigration, phaseRaceMigration, phaseOps, phaseUnrelatedId, operations = ops }); file.Flush(true); }
            var binding = await source.CaptureStorageIdentityAsync(); Assert.False(binding.IsError);
            var enrollment = new HolonReplicationEnrollment("old-lineage", new[] { "target" });
            var initial = new Holon { Id = ids[0], Name = "fenced-source" };
            var create = new ConditionalHolonWriteRequest(new(ops[0], actor, ProviderType.MongoDBOASIS,
                new[] { new HolonWriteCondition(ids[0], null) }, enrollment), ConditionalHolonOperation.Save, new[] { initial }, saveChildren: false);
            var saved = await source.ExecuteReplicatingHolonWriteAsync(create, enrollment); Assert.False(saved.IsError, saved.Message + saved.Exception);
            var original = saved.Result.Single();
            var incoming = new Holon { Id = ids[1], Name = "fenced-replica", CreatedByAvatarId = actor, Version = 1, VersionId = Guid.NewGuid() };
            CosmicConcurrency.SetProviderRevisionId(incoming, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            var replica = new CommittedHolonReplicaRequest(ops[1], "replica-lineage", ProviderType.SQLLiteDBOASIS,
                actor, ConditionalHolonOperation.Save, new[] { incoming }, saveChildren: false);
            Assert.False((await source.ApplyBoundCommittedHolonReplicaAsync(replica, binding.Result)).IsError);
            var scope = new HolonMigrationFenceRequest(migration, actor, binding.Result, new string('A',64), new string('B',64), ids.Take(2));
            var race = races.Select(id => new HolonMigrationFenceRequest(id, actor, binding.Result, scope.PreviewDigest, scope.AuthorizationContextDigest, new[] { ids[3] })).ToArray();
            var acquired = await Task.WhenAll(source.AcquireHolonMigrationFenceAsync(race[0]), other.AcquireHolonMigrationFenceAsync(race[1]));
            Assert.Equal(1, acquired.Count(r => !r.IsError));
            var winner = acquired[0].IsError ? 1 : 0; var loser = 1 - winner;
            Assert.Contains(acquired[loser].ErrorCode, new[] { "COSMIC_MIGRATION_FENCE_CONFLICT", "COSMIC_MIGRATION_FENCE_CONCURRENT_CHANGE" });
            Assert.Equal(HolonMigrationFenceState.Absent, (await source.ReadHolonMigrationFenceAsync(race[loser])).Result.State);
            Assert.False((await source.CancelHolonMigrationFenceAsync(race[winner], acquired[winner].Result.OwnerToken)).IsError);
            // Hold the actual record admission document in an independent native
            // transaction. Snapshot-only admission would incorrectly cross it.
            using (var writer = await client.StartSessionAsync())
            {
                writer.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority));
                try
                {
                    var gates = client.GetDatabase(First).GetCollection<BsonDocument>("CosmicMigrationRecordGate");
                    var touched = await gates.UpdateOneAsync(writer, new BsonDocument("_id", ids[0].ToString("D")),
                        new BsonDocument("$inc", new BsonDocument("Epoch", 1L)));
                    Assert.Equal(1L, touched.MatchedCount);
                    Assert.Equal("COSMIC_MIGRATION_FENCE_CONCURRENT_CHANGE", (await other.AcquireHolonMigrationFenceAsync(scope)).ErrorCode);
                }
                finally { await writer.AbortTransactionAsync(); }
            }
            Assert.Equal(HolonMigrationFenceState.Absent, (await source.ReadHolonMigrationFenceAsync(scope)).Result.State);
            var held = await source.AcquireHolonMigrationFenceAsync(scope); Assert.False(held.IsError, held.Message + held.Exception);
            var owner = held.Result.OwnerToken;
            Assert.True(owner == (await other.AcquireHolonMigrationFenceAsync(scope)).Result.OwnerToken);
            if (reactivate)
            {
                Assert.False((await source.DeActivateProviderAsync()).IsError);
                Assert.Equal("COSMIC_MIGRATION_FENCE_NOT_READY", (await source.ReadHolonMigrationFenceAsync(scope)).ErrorCode);
                Assert.False((await source.ActivateProviderAsync()).IsError);
            }
            Assert.True(owner == (await source.ReadHolonMigrationFenceAsync(scope)).Result.OwnerToken);
            foreach (var lineage in new[] { "old-lineage", "new-lineage" })
            foreach (var kind in new[] { ConditionalHolonOperation.Save, ConditionalHolonOperation.SoftDelete, ConditionalHolonOperation.HardDelete })
            {
                var route = new HolonReplicationEnrollment(lineage, new[] { "target" });
                var input = StoredRecordJson.Project<Holon>(original); input.Name = "must-not-save";
                var write = new ConditionalHolonWriteRequest(new(ops[2], actor, ProviderType.MongoDBOASIS,
                    new[] { new HolonWriteCondition(ids[0], CosmicConcurrency.GetProviderRevisionId(original, ProviderType.MongoDBOASIS)) }, route),
                    kind, new[] { input }, saveChildren: false);
                Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await other.ExecuteReplicatingHolonWriteAsync(write, route)).ErrorCode);
            }
            Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await source.ApplyBoundCommittedHolonReplicaAsync(replica, binding.Result)).ErrorCode);
            Assert.Equal(ConditionalHolonReceiptState.Committed, (await source.ReadCommittedHolonReplicaReceiptAsync(new(replica, binding.Result))).Result.ReceiptState);
            var outbox = source.GetBoundHolonReplicationOutbox(binding.Result); Assert.False(outbox.IsError);
            Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await outbox.Result.ClaimNextAsync("target", "mongo-fence-fixture", TimeSpan.FromMinutes(1))).ErrorCode);
            var unrelated = new ConditionalHolonWriteRequest(new(ops[3], actor, ProviderType.MongoDBOASIS,
                new[] { new HolonWriteCondition(ids[2], null) }), ConditionalHolonOperation.Save,
                new[] { new Holon { Id = ids[2], Name = "unrelated-writable" } }, saveChildren: false);
            Assert.False((await other.ExecuteConditionalHolonWriteAsync(unrelated)).IsError);
            Assert.Equal("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", (await source.CancelHolonMigrationFenceAsync(scope, Guid.NewGuid())).ErrorCode);
            Assert.False((await source.CancelHolonMigrationFenceAsync(scope, owner)).IsError);
            Assert.Equal(HolonMigrationFenceState.Cancelled, (await other.CancelHolonMigrationFenceAsync(scope, owner)).Result.State);
            Assert.Equal("COSMIC_MIGRATION_FENCE_TERMINAL", (await source.AcquireHolonMigrationFenceAsync(scope)).ErrorCode);
            Assert.False((await source.ApplyBoundCommittedHolonReplicaAsync(replica, binding.Result)).IsError);
            var claim = await outbox.Result.ClaimNextAsync("target", "mongo-fence-fixture", TimeSpan.FromMinutes(1));
            Assert.False(claim.IsError, claim.Message + claim.Exception); Assert.NotNull(claim.Result);
            // No remote delivery is attempted by this protocol fixture.
            var released = await outbox.Result.FinishAsync(claim.Result, HolonDeliveryOutcome.Retry, "FIXTURE_NO_DELIVERY", nextAttemptUtc: DateTime.UtcNow.AddMinutes(1));
            Assert.False(released.IsError, released.Message + released.Exception); Assert.True(released.Result);
            // Mutation phase remains metadata only; all original business and
            // delivery evidence stays protected, with no remote delivery sent.
            var phaseRoute = new HolonReplicationEnrollment("phase-lineage", new[] { "phase-target" }, binding.Result);
            var seed = new ConditionalHolonWriteRequest(new(phaseOps[0], actor, ProviderType.MongoDBOASIS,
                new[] { new HolonWriteCondition(ids[3], null) }, phaseRoute), ConditionalHolonOperation.Save,
                new[] { new Holon { Id = ids[3], Name = "phase-protected" } }, saveChildren: false);
            var seeded = await source.ExecuteReplicatingHolonWriteAsync(seed, phaseRoute); Assert.False(seeded.IsError, seeded.Message + seeded.Exception);
            var phase = new HolonMigrationFenceRequest(phaseMigration, actor, binding.Result, scope.PreviewDigest, scope.AuthorizationContextDigest,
                new[] { ids[0], ids[1], ids[3] });
            var phaseHeld = await source.AcquireHolonMigrationFenceAsync(phase); Assert.False(phaseHeld.IsError);
            var phaseOwner = phaseHeld.Result.OwnerToken;
            Assert.Equal("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", (await other.BeginHolonMigrationMutationAsync(new(phase, Guid.NewGuid(), new string('C',64)))).ErrorCode);
            var firstStart = new HolonMigrationMutationStartRequest(phase, phaseOwner, new string('C',64));
            using (var blocker = await client.StartSessionAsync())
            {
                blocker.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority));
                try
                {
                    var gate = client.GetDatabase(First).GetCollection<BsonDocument>("CosmicMigrationRecordGate");
                    Assert.Equal(1L, (await gate.UpdateOneAsync(blocker, new BsonDocument("_id", ids[0].ToString("D")),
                        new BsonDocument("$inc", new BsonDocument("Epoch", 1L)))).MatchedCount);
                    Assert.Equal("COSMIC_MIGRATION_FENCE_CONCURRENT_CHANGE", (await other.BeginHolonMigrationMutationAsync(firstStart)).ErrorCode);
                }
                finally { await blocker.AbortTransactionAsync(); }
            }
            Assert.Equal(HolonMigrationFenceState.Held, (await source.ReadHolonMigrationFenceAsync(phase)).Result.State);
            var starts = await Task.WhenAll(source.BeginHolonMigrationMutationAsync(firstStart),
                other.BeginHolonMigrationMutationAsync(new(phase, phaseOwner, new string('D',64))));
            Assert.Single(starts.Where(r => !r.IsError));
            var started = starts.Single(r => !r.IsError); Assert.True(started.IsSaved);
            Assert.Equal(HolonMigrationFenceState.MutationStarted, started.Result.State);
            Assert.Contains(starts.Single(r => r.IsError).ErrorCode, new[] { "COSMIC_MIGRATION_FENCE_CONCURRENT_CHANGE", "COSMIC_MIGRATION_MUTATION_INTENT_REUSED" });
            var committedStart = new HolonMigrationMutationStartRequest(phase, phaseOwner, started.Result.MutationDigest);
            Assert.False((await source.BeginHolonMigrationMutationAsync(committedStart)).IsSaved);
            var repeatedAcquire = await other.AcquireHolonMigrationFenceAsync(phase); Assert.False(repeatedAcquire.IsError);
            Assert.Equal(HolonMigrationFenceState.MutationStarted, repeatedAcquire.Result.State);
            Assert.Equal(committedStart.MutationDigest, repeatedAcquire.Result.MutationDigest); Assert.False(repeatedAcquire.IsSaved);
            var foreignScope = new HolonMigrationFenceRequest(phaseMigration, Guid.NewGuid(), binding.Result, phase.PreviewDigest,
                phase.AuthorizationContextDigest, phase.HolonIds);
            Assert.Equal("COSMIC_MIGRATION_FENCE_SCOPE_MISMATCH", (await source.BeginHolonMigrationMutationAsync(new(foreignScope, phaseOwner, committedStart.MutationDigest))).ErrorCode);
            var differentDigest = started.Result.MutationDigest == new string('C',64) ? new string('D',64) : new string('C',64);
            Assert.Equal("COSMIC_MIGRATION_MUTATION_INTENT_REUSED", (await source.BeginHolonMigrationMutationAsync(new(phase, phaseOwner, differentDigest))).ErrorCode);
            Assert.Equal("COSMIC_MIGRATION_MUTATION_ALREADY_STARTED", (await other.CancelHolonMigrationFenceAsync(phase, phaseOwner)).ErrorCode);
            Assert.False((await source.DeActivateProviderAsync()).IsError);
            Assert.Equal("COSMIC_MIGRATION_FENCE_NOT_READY", (await source.BeginHolonMigrationMutationAsync(committedStart)).ErrorCode);
            Assert.False((await source.ActivateProviderAsync()).IsError);
            var recoveredStart = await source.ReadHolonMigrationFenceAsync(phase); Assert.False(recoveredStart.IsError);
            Assert.Equal(HolonMigrationFenceState.MutationStarted, recoveredStart.Result.State);
            Assert.Equal(committedStart.MutationDigest, recoveredStart.Result.MutationDigest);
            Assert.False((await source.BeginHolonMigrationMutationAsync(committedStart)).IsSaved);
            foreach (var kind in new[] { ConditionalHolonOperation.Save, ConditionalHolonOperation.SoftDelete, ConditionalHolonOperation.HardDelete })
            {
                var row = StoredRecordJson.Project<Holon>(original); row.Name = "phase-must-reject";
                var blocked = new ConditionalHolonWriteRequest(new(phaseOps[1], actor, ProviderType.MongoDBOASIS,
                    new[] { new HolonWriteCondition(ids[0], CosmicConcurrency.GetProviderRevisionId(original, ProviderType.MongoDBOASIS)) }), kind, new[] { row }, saveChildren: false);
                Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await other.ExecuteConditionalHolonWriteAsync(blocked)).ErrorCode);
            }
            Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await source.ApplyBoundCommittedHolonReplicaAsync(replica, binding.Result)).ErrorCode);
            var phaseOutbox = source.GetBoundHolonReplicationOutbox(binding.Result); Assert.False(phaseOutbox.IsError);
            Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await phaseOutbox.Result.ClaimNextAsync("phase-target", "phase-worker", TimeSpan.FromMinutes(1))).ErrorCode);
            Assert.Equal(ConditionalHolonReceiptState.Committed, (await source.ReadCommittedHolonReplicaReceiptAsync(new(replica, binding.Result))).Result.ReceiptState);
            var outside = new ConditionalHolonWriteRequest(new(phaseOps[2], actor, ProviderType.MongoDBOASIS,
                new[] { new HolonWriteCondition(phaseUnrelatedId, null) }), ConditionalHolonOperation.Save,
                new[] { new Holon { Id = phaseUnrelatedId, Name = "phase-unrelated" } }, saveChildren: false);
            Assert.False((await other.ExecuteConditionalHolonWriteAsync(outside)).IsError);
            var racePhase = new HolonMigrationFenceRequest(phaseRaceMigration, actor, binding.Result,
                scope.PreviewDigest, scope.AuthorizationContextDigest, new[] { ids[2] });
            var raceHeld = await source.AcquireHolonMigrationFenceAsync(racePhase); Assert.False(raceHeld.IsError);
            var competing = await Task.WhenAll(source.BeginHolonMigrationMutationAsync(new(racePhase, raceHeld.Result.OwnerToken, new string('E',64))),
                other.CancelHolonMigrationFenceAsync(racePhase, raceHeld.Result.OwnerToken));
            Assert.Single(competing.Where(r => !r.IsError));
            var finalRace = await source.ReadHolonMigrationFenceAsync(racePhase); Assert.False(finalRace.IsError);
            Assert.True(finalRace.Result.State is HolonMigrationFenceState.MutationStarted or HolonMigrationFenceState.Cancelled);
            // A lost admission namespace/marker must not be silently recreated
            // in the presence of retained fence history, even after cancellation.
            await client.GetDatabase(First).DropCollectionAsync("CosmicMigrationRecordGate");
            Assert.True((await other.ExecuteConditionalHolonWriteAsync(unrelated)).IsError);
            Assert.Equal(ConditionalHolonReceiptState.Committed, (await source.ReadCommittedHolonReplicaReceiptAsync(new(replica, binding.Result))).Result.ReceiptState);
            Assert.False((await source.DeActivateProviderAsync()).IsError);
            Assert.True((await source.ActivateProviderAsync()).IsError);
            using var namespaces = await client.GetDatabase(First).ListCollectionsAsync(new ListCollectionsOptions {
                Filter = new BsonDocument("name", "CosmicMigrationRecordGate") });
            Assert.Empty(await namespaces.ToListAsync());
        });
        // The reusable owner already verified both private databases were dropped.
        Assert.NotNull(extraJournal); File.Delete(extraJournal);
    }
}
