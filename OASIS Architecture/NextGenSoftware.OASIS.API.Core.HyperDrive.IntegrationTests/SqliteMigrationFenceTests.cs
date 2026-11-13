using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class SqliteMigrationFenceTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeRecordFenceBlocksV2WritesAndDeliveryAndSurvivesRestart(bool restart)
    {
        var root = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_MIGRATION_FENCE_ROOT")
            ?? throw new InvalidOperationException("Use the owned bounded fence runner."));
        Assert.True(File.Exists(Path.Combine(root, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")));
        var directory = Path.Combine(root, "LocalState/COSMIC/MigrationFenceCurrent"); Directory.CreateDirectory(directory);
        var database = Path.Combine(directory, "fence.db"); var journal = Path.Combine(directory, "pending.json");
        foreach (var path in new[] { database, database + "-wal", database + "-shm", journal }) Assert.False(File.Exists(path), "Preserve an earlier original fence outcome.");
        var actor = Guid.NewGuid(); var migration = Guid.NewGuid();
        var phaseMigration = Guid.NewGuid(); var phaseRaceMigration = Guid.NewGuid(); var phaseUnrelatedId = Guid.NewGuid();
        var phaseSeedOp = Guid.NewGuid(); var phaseRejectOp = Guid.NewGuid(); var phaseOtherOp = Guid.NewGuid(); var raceMigrations = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var sourceId = Guid.NewGuid(); var replicaId = Guid.NewGuid(); var otherId = Guid.NewGuid(); var extraId = Guid.NewGuid();
        var createOp = Guid.NewGuid(); var replicaOp = Guid.NewGuid(); var editOp = Guid.NewGuid(); var otherOp = Guid.NewGuid();
        File.WriteAllText(journal, JsonSerializer.Serialize(new { owner = "cosmic-native-fence", actor, migration, raceMigrations, phaseMigration, phaseRaceMigration,
            ids = new[] { sourceId, replicaId, otherId, extraId, phaseUnrelatedId }, operations = new[] { createOp, replicaOp, editOp, otherOp, phaseSeedOp, phaseRejectOp, phaseOtherOp } }));
        var connection = $"Data Source={database};Pooling=False";
        var provider = new SQLLiteDBOASIS(connection) { IsVersionControlEnabled = false };
        bool complete = false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var identity = await provider.CaptureStorageIdentityAsync(); Assert.False(identity.IsError);
            var enrollment = new HolonReplicationEnrollment("old-lineage", new[] { "target" });
            var initial = new Holon { Id = sourceId, Name = "source-original" };
            var create = new ConditionalHolonWriteRequest(new(createOp, actor, ProviderType.SQLLiteDBOASIS,
                new[] { new HolonWriteCondition(sourceId, null) }, enrollment), ConditionalHolonOperation.Save, new[] { initial }, saveChildren: false);
            var saved = await provider.ExecuteReplicatingHolonWriteAsync(create, enrollment); Assert.False(saved.IsError, saved.Message + saved.Exception);
            var source = saved.Result.Single();
            var incoming = new Holon { Id = replicaId, Name = "replica-original", Version = 1, VersionId = Guid.NewGuid() };
            CosmicConcurrency.SetProviderRevisionId(incoming, ProviderType.MongoDBOASIS, Guid.NewGuid());
            var replica = new CommittedHolonReplicaRequest(replicaOp, "replica-lineage", ProviderType.MongoDBOASIS,
                actor, ConditionalHolonOperation.Save, new[] { incoming }, saveChildren: false);
            Assert.False((await provider.ApplyBoundCommittedHolonReplicaAsync(replica, identity.Result)).IsError);
            var request = new HolonMigrationFenceRequest(migration, actor, identity.Result, new string('A',64), new string('B',64), new[] { sourceId, replicaId });
            // Separate provider instances and native connections race for the
            // same scope. Exactly one durable owner may win, not merely one
            // winner within a process-local lock.
            var competitor = new SQLLiteDBOASIS(connection) { IsVersionControlEnabled = false };
            try
            {
                Assert.False((await competitor.ActivateProviderAsync()).IsError);
                var race = raceMigrations.Select(id => new HolonMigrationFenceRequest(id, actor, identity.Result,
                    request.PreviewDigest, request.AuthorizationContextDigest, new[] { extraId })).ToArray();
                var outcomes = await Task.WhenAll(Task.Run(() => provider.AcquireHolonMigrationFenceAsync(race[0])),
                    Task.Run(() => competitor.AcquireHolonMigrationFenceAsync(race[1])));
                Assert.Equal(1, outcomes.Count(r => !r.IsError));
                Assert.Equal(1, outcomes.Count(r => r.ErrorCode == "COSMIC_MIGRATION_FENCE_CONFLICT"));
                var winner = outcomes[0].IsError ? 1 : 0; var loser = 1 - winner;
                Assert.Equal(HolonMigrationFenceState.Absent, (await provider.ReadHolonMigrationFenceAsync(race[loser])).Result.State);
                Assert.False((await provider.CancelHolonMigrationFenceAsync(race[winner], outcomes[winner].Result.OwnerToken)).IsError);
            }
            finally { Assert.False((await competitor.DeActivateProviderAsync()).IsError); }
            Assert.Equal(HolonMigrationFenceState.Absent, (await provider.ReadHolonMigrationFenceAsync(request)).Result.State);
            var held = await provider.AcquireHolonMigrationFenceAsync(request); Assert.False(held.IsError, held.Message + held.Exception);
            var owner = held.Result.OwnerToken;
            Assert.True(owner == (await provider.AcquireHolonMigrationFenceAsync(request)).Result.OwnerToken);
            var overlap = new HolonMigrationFenceRequest(Guid.NewGuid(), actor, identity.Result, request.PreviewDigest,
                request.AuthorizationContextDigest, new[] { sourceId, extraId });
            Assert.Equal("COSMIC_MIGRATION_FENCE_CONFLICT", (await provider.AcquireHolonMigrationFenceAsync(overlap)).ErrorCode);
            if (restart)
            {
                Assert.False((await provider.DeActivateProviderAsync()).IsError);
                Assert.Equal("COSMIC_MIGRATION_FENCE_NOT_READY", (await provider.ReadHolonMigrationFenceAsync(request)).ErrorCode);
                provider = new SQLLiteDBOASIS(connection) { IsVersionControlEnabled = false };
                Assert.False((await provider.ActivateProviderAsync()).IsError);
            }
            Assert.True(owner == (await provider.ReadHolonMigrationFenceAsync(request)).Result.OwnerToken);
            var edited = StoredRecordJson.Project<Holon>(source); edited.Name = "must-not-save";
            foreach (var lineage in new[] { "old-lineage", "new-lineage" })
            {
                var route = new HolonReplicationEnrollment(lineage, new[] { "target" });
                var write = new ConditionalHolonWriteRequest(new(editOp, actor, ProviderType.SQLLiteDBOASIS,
                    new[] { new HolonWriteCondition(sourceId, CosmicConcurrency.GetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS)) }, route),
                    ConditionalHolonOperation.Save, new[] { edited }, saveChildren: false);
                Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await provider.ExecuteReplicatingHolonWriteAsync(write, route)).ErrorCode);
            }
            foreach (var kind in new[] { ConditionalHolonOperation.SoftDelete, ConditionalHolonOperation.HardDelete })
            {
                var deletion = new ConditionalHolonWriteRequest(new(editOp, actor, ProviderType.SQLLiteDBOASIS,
                    new[] { new HolonWriteCondition(sourceId, CosmicConcurrency.GetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS)) }, enrollment),
                    kind, new[] { source }, saveChildren: false);
                Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await provider.ExecuteReplicatingHolonWriteAsync(deletion, enrollment)).ErrorCode);
            }
            Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await provider.ApplyBoundCommittedHolonReplicaAsync(replica, identity.Result)).ErrorCode);
            Assert.Equal(ConditionalHolonReceiptState.Committed, (await provider.ReadCommittedHolonReplicaReceiptAsync(new(replica, identity.Result))).Result.ReceiptState);
            var outbox = provider.GetBoundHolonReplicationOutbox(identity.Result); Assert.False(outbox.IsError);
            Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await outbox.Result.ClaimNextAsync("target", "fence-fixture", TimeSpan.FromMinutes(1))).ErrorCode);
            var unrelated = new ConditionalHolonWriteRequest(new(otherOp, actor, ProviderType.SQLLiteDBOASIS,
                new[] { new HolonWriteCondition(otherId, null) }), ConditionalHolonOperation.Save,
                new[] { new Holon { Id = otherId, Name = "unrelated-still-writable" } }, saveChildren: false);
            Assert.False((await provider.ExecuteConditionalHolonWriteAsync(unrelated)).IsError);
            Assert.Equal("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", (await provider.CancelHolonMigrationFenceAsync(request, Guid.NewGuid())).ErrorCode);
            var cancelled = await provider.CancelHolonMigrationFenceAsync(request, owner); Assert.False(cancelled.IsError, cancelled.Message + cancelled.Exception);
            Assert.Equal(HolonMigrationFenceState.Cancelled, (await provider.CancelHolonMigrationFenceAsync(request, owner)).Result.State);
            Assert.Equal("COSMIC_MIGRATION_FENCE_TERMINAL", (await provider.AcquireHolonMigrationFenceAsync(request)).ErrorCode);
            Assert.False((await provider.ApplyBoundCommittedHolonReplicaAsync(replica, identity.Result)).IsError);
            var claim = await outbox.Result.ClaimNextAsync("target", "fence-fixture", TimeSpan.FromMinutes(1));
            Assert.False(claim.IsError, claim.Message + claim.Exception); Assert.NotNull(claim.Result);
            // No remote delivery was attempted. Return this controlled fixture
            // claim as Retry; do not falsely acknowledge a replica as delivered.
            var released = await outbox.Result.FinishAsync(claim.Result, HolonDeliveryOutcome.Retry,
                "FIXTURE_NO_DELIVERY", nextAttemptUtc: DateTime.UtcNow.AddMinutes(1));
            Assert.False(released.IsError, released.Message + released.Exception); Assert.True(released.Result);
            // Native mutation-start is metadata only: no cutover executor exists.
            var phaseRoute = new HolonReplicationEnrollment("phase-source", new[] { "phase-target" }, identity.Result);
            var seedRequest = new ConditionalHolonWriteRequest(new(phaseSeedOp, actor, ProviderType.SQLLiteDBOASIS,
                new[] { new HolonWriteCondition(extraId, null) }, phaseRoute), ConditionalHolonOperation.Save,
                new[] { new Holon { Id = extraId, Name = "phase-protected" } }, saveChildren: false);
            var phaseSeed = await provider.ExecuteReplicatingHolonWriteAsync(seedRequest, phaseRoute); Assert.False(phaseSeed.IsError, phaseSeed.Message);
            var phaseFence = new HolonMigrationFenceRequest(phaseMigration, actor, identity.Result, new string('A',64), new string('B',64), new[] { extraId });
            var phaseHeld = await provider.AcquireHolonMigrationFenceAsync(phaseFence); Assert.False(phaseHeld.IsError);
            var phaseOwner = phaseHeld.Result.OwnerToken;
            // Recreate the exact old schema in this owned fixture while all
            // mutation digests are null, then activation must upgrade atomically.
            Assert.False((await provider.DeActivateProviderAsync()).IsError);
            using (var native = new Microsoft.Data.Sqlite.SqliteConnection(connection))
            {
                await native.OpenAsync(); using var check = native.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM CosmicMigrationFence WHERE MutationDigest IS NOT NULL";
                Assert.Equal(0L, (long)(await check.ExecuteScalarAsync())!);
                using var oldSchema = native.CreateCommand(); oldSchema.CommandText = "ALTER TABLE CosmicMigrationFence DROP COLUMN MutationDigest";
                await oldSchema.ExecuteNonQueryAsync();
            }
            provider = new SQLLiteDBOASIS(connection) { IsVersionControlEnabled = false };
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            Assert.Equal(phaseOwner, (await provider.ReadHolonMigrationFenceAsync(phaseFence)).Result.OwnerToken);
            Assert.Equal("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", (await provider.BeginHolonMigrationMutationAsync(new(phaseFence, Guid.NewGuid(), new string('C',64)))).ErrorCode);
            var startRequest = new HolonMigrationMutationStartRequest(phaseFence, phaseOwner, new string('C',64));
            var started = await provider.BeginHolonMigrationMutationAsync(startRequest); Assert.False(started.IsError, started.Message + started.Exception);
            Assert.True(started.IsSaved); Assert.Equal(HolonMigrationFenceState.MutationStarted, started.Result.State);
            Assert.Equal(startRequest.MutationDigest, started.Result.MutationDigest);
            Assert.False((await provider.BeginHolonMigrationMutationAsync(startRequest)).IsSaved);
            Assert.Equal("COSMIC_MIGRATION_MUTATION_INTENT_REUSED", (await provider.BeginHolonMigrationMutationAsync(new(phaseFence, phaseOwner, new string('D',64)))).ErrorCode);
            Assert.Equal("COSMIC_MIGRATION_MUTATION_ALREADY_STARTED", (await provider.CancelHolonMigrationFenceAsync(phaseFence, phaseOwner)).ErrorCode);
            // Native constraint also rejects an old-style cancellation statement.
            using (var native = new Microsoft.Data.Sqlite.SqliteConnection(connection))
            {
                await native.OpenAsync(); using var cancel = native.CreateCommand();
                cancel.CommandText = "UPDATE CosmicMigrationFence SET FenceState='Cancelled' WHERE MigrationId=$id";
                cancel.Parameters.AddWithValue("$id", phaseMigration.ToString("D"));
                await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => cancel.ExecuteNonQueryAsync());
                // Execute the actual migration Down operation: a downgrade must
                // not erase a durable started/uncertain intent.
                var builder = new Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                var migrationType = typeof(NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Migrations.AddCosmicMigrationMutationDigest);
                migrationType.GetMethod("Down", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .Invoke(Activator.CreateInstance(migrationType), new object[] { builder });
                using var downgrade = native.BeginTransaction(); using var down = native.CreateCommand(); down.Transaction = downgrade;
                down.CommandText = Assert.Single(builder.Operations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation>()).Sql;
                await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => down.ExecuteNonQueryAsync());
                downgrade.Rollback();
            }
            Assert.False((await provider.DeActivateProviderAsync()).IsError);
            provider = new SQLLiteDBOASIS(connection) { IsVersionControlEnabled = false }; Assert.False((await provider.ActivateProviderAsync()).IsError);
            var recoveredStart = await provider.ReadHolonMigrationFenceAsync(phaseFence); Assert.False(recoveredStart.IsError);
            Assert.Equal(HolonMigrationFenceState.MutationStarted, recoveredStart.Result.State); Assert.Equal(startRequest.MutationDigest, recoveredStart.Result.MutationDigest);
            Assert.False((await provider.BeginHolonMigrationMutationAsync(startRequest)).IsSaved);
            var phaseValue = phaseSeed.Result.Single();
            foreach (var kind in new[] { ConditionalHolonOperation.Save, ConditionalHolonOperation.SoftDelete, ConditionalHolonOperation.HardDelete })
            {
                var blocked = new ConditionalHolonWriteRequest(new(phaseRejectOp, actor, ProviderType.SQLLiteDBOASIS,
                    new[] { new HolonWriteCondition(extraId, CosmicConcurrency.GetProviderRevisionId(phaseValue, ProviderType.SQLLiteDBOASIS)) }),
                    kind, new[] { phaseValue }, saveChildren: false);
                Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await provider.ExecuteConditionalHolonWriteAsync(blocked)).ErrorCode);
            }
            var currentOutbox = provider.GetBoundHolonReplicationOutbox(identity.Result); Assert.False(currentOutbox.IsError);
            Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", (await currentOutbox.Result.ClaimNextAsync("phase-target", "phase-worker", TimeSpan.FromMinutes(1))).ErrorCode);
            Assert.Equal(ConditionalHolonReceiptState.Committed, (await provider.ReadBoundConditionalHolonWriteOutcomeAsync(seedRequest, identity.Result)).Result.ReceiptState);
            var phaseUnrelated = new ConditionalHolonWriteRequest(new(phaseOtherOp, actor, ProviderType.SQLLiteDBOASIS,
                new[] { new HolonWriteCondition(phaseUnrelatedId, null) }), ConditionalHolonOperation.Save,
                new[] { new Holon { Id = phaseUnrelatedId, Name = "phase-unrelated" } }, saveChildren: false);
            Assert.False((await provider.ExecuteConditionalHolonWriteAsync(phaseUnrelated)).IsError);
            var racing = new SQLLiteDBOASIS(connection) { IsVersionControlEnabled = false };
            try
            {
                Assert.False((await racing.ActivateProviderAsync()).IsError);
                var raceScope = new HolonMigrationFenceRequest(phaseRaceMigration, actor, identity.Result, new string('A',64), new string('B',64), new[] { phaseUnrelatedId });
                var raceHeld = await racing.AcquireHolonMigrationFenceAsync(raceScope); Assert.False(raceHeld.IsError);
                var raceResults = await Task.WhenAll(Task.Run(() => provider.BeginHolonMigrationMutationAsync(new(raceScope, raceHeld.Result.OwnerToken, new string('E',64)))),
                    Task.Run(() => racing.CancelHolonMigrationFenceAsync(raceScope, raceHeld.Result.OwnerToken)));
                Assert.Single(raceResults.Where(r => !r.IsError));
                var final = await provider.ReadHolonMigrationFenceAsync(raceScope); Assert.False(final.IsError);
                Assert.True(final.Result.State is HolonMigrationFenceState.MutationStarted or HolonMigrationFenceState.Cancelled);
            }
            finally { Assert.False((await racing.DeActivateProviderAsync()).IsError); }
            complete = true;
        }
        finally
        {
            Assert.False((await provider.DeActivateProviderAsync()).IsError);
            if (complete)
            {
                foreach (var path in new[] { database, database + "-wal", database + "-shm" })
                    if (File.Exists(path)) File.Delete(path);
                File.Delete(journal);
            }
        }
    }
}
