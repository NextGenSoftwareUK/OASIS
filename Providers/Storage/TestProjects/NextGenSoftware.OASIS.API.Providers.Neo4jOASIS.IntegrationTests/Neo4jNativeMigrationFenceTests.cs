using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    // Delay the real native commit, preserving its transaction and database locks.
    public class MigrationCommitProbe : DispatchProxy
    {
        public object Inner = null!;
        public MigrationCommitControl Control = null!;
        public static T Wrap<T>(T inner, MigrationCommitControl control) where T : class
        {
            var wrapped = DispatchProxy.Create<T, MigrationCommitProbe>();
            ((MigrationCommitProbe)(object)wrapped).Inner = inner;
            ((MigrationCommitProbe)(object)wrapped).Control = control;
            return wrapped;
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == "CommitAsync" && Control.Enabled)
                return Commit();
            object? value;
            try { value = method.Invoke(Inner, args); }
            catch (TargetInvocationException e) when (e.InnerException != null)
            { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
            if (value is IAsyncSession session) return Wrap(session, Control);
            if (value is Task<IAsyncTransaction> transaction) return WrapTransaction(transaction);
            return value;
        }
        private async Task<IAsyncTransaction> WrapTransaction(Task<IAsyncTransaction> transaction)
            => Wrap(await transaction, Control);
        private async Task Commit()
        {
            Control.Entered.TrySetResult(true);
            await Control.Release.Task.WaitAsync(TimeSpan.FromSeconds(20));
            await ((IAsyncTransaction)Inner).CommitAsync();
        }
    }
    public sealed class MigrationCommitControl
    {
        public bool Enabled;
        public TaskCompletionSource<bool> Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeMigrationBarrierPreservesReceiptAndRejectsAffectedWrites(bool reactivate)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-ReusableNeo4jTests.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var journal = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/migration-fence.pending.json");
        Directory.CreateDirectory(Path.GetDirectoryName(journal)!);
        Assert.IsFalse(File.Exists(journal), "Reconcile retained outcomes before another fixture.");
        var commitControl = new MigrationCommitControl();
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        var other = new Provider(uri, "neo4j", "oasis-local-test-only");
        var actor = Guid.NewGuid(); var operation = Guid.NewGuid(); var unrelatedOperation = Guid.NewGuid();
        var record = new Holon { Id = Guid.NewGuid(), Name = "owned-neo-migration-barrier" };
        var unrelated = new Holon { Id = Guid.NewGuid(), Name = "owned-neo-unrelated" };
        var migration = Guid.NewGuid(); var competing = Guid.NewGuid();
        var phaseMigration = Guid.NewGuid(); var phaseRaceMigration = Guid.NewGuid();
        var phaseId = Guid.NewGuid(); var phaseReplicaId = Guid.NewGuid(); var phaseOtherId = Guid.NewGuid();
        var phaseSeedOp = Guid.NewGuid(); var phaseReplicaOp = Guid.NewGuid(); var phaseOtherOp = Guid.NewGuid();
        var target = "owned-fence-target-" + operation; var source = "owned-fence-source-" + operation;
        var blockedOperations = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file, new { actor, operation, unrelatedOperation, migration, competing, phaseMigration, phaseRaceMigration, phaseSeedOp, phaseReplicaOp, phaseOtherOp, source, target, blockedOperations, ids = new[] { record.Id, unrelated.Id, phaseId, phaseReplicaId, phaseOtherId } });
            file.Flush(true);
        }
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await other.ActivateProviderAsync()).IsError);
            var binding = await provider.CaptureStorageIdentityAsync(); Assert.IsFalse(binding.IsError, binding.Message);
            var enrollment = new HolonReplicationEnrollment(source, new[] { target }, binding.Result);
            var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, actor,
                ProviderType.Neo4jOASIS, new[] { new HolonWriteCondition(record.Id, null) }, enrollment),
                ConditionalHolonOperation.Save, new[] { record }, saveChildren: false);
            var saved = await provider.ExecuteReplicatingHolonWriteAsync(request, enrollment);
            Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            var fence = new HolonMigrationFenceRequest(migration, actor, binding.Result, new string('A', 64), new string('B', 64), new[] { record.Id });
            // Hold the actual acquisition before its native commit. Other drivers
            // must wait on its record lock, then observe the committed barrier.
            var driverField = typeof(Provider).GetField("_driver", BindingFlags.Instance | BindingFlags.NonPublic)!;
            driverField.SetValue(provider, MigrationCommitProbe.Wrap((IDriver)driverField.GetValue(provider)!, commitControl));
            commitControl.Enabled = true;
            var acquisition = provider.AcquireHolonMigrationFenceAsync(fence);
            await commitControl.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var racingOutbox = other.GetBoundHolonReplicationOutbox(binding.Result);
            Assert.IsFalse(racingOutbox.IsError, racingOutbox.Message);
            var racingUpdate = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(blockedOperations[0], actor,
                ProviderType.Neo4jOASIS, new[] { new HolonWriteCondition(record.Id,
                    CosmicConcurrency.GetProviderRevisionId(saved.Result.Single(), ProviderType.Neo4jOASIS)) }),
                ConditionalHolonOperation.Save, saved.Result, saveChildren: false);
            var racingReplay = reactivate ? other.ExecuteConditionalHolonWriteAsync(racingUpdate)
                : other.ExecuteReplicatingHolonWriteAsync(request, enrollment);
            var racingClaim = racingOutbox.Result.ClaimNextAsync(target, "owned-racing-worker", TimeSpan.FromSeconds(30));
            try
            {
                using var raceDriver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
                using var raceInspection = raceDriver.AsyncSession();
                var deadline = DateTime.UtcNow.AddSeconds(10);
                var blockedCount = 0;
                do
                {
                    var transactions = await (await raceInspection.RunAsync(
                        "SHOW TRANSACTIONS YIELD currentQuery,status WHERE currentQuery STARTS WITH 'MERGE (g:CosmicMigrationRecordGate' RETURN status")).ToListAsync();
                    blockedCount = transactions.Count(row => row["status"].As<string>().StartsWith("Blocked", StringComparison.OrdinalIgnoreCase));
                    if (blockedCount < 2) await Task.Delay(50);
                } while (blockedCount < 2 && !racingReplay.IsCompleted && !racingClaim.IsCompleted && DateTime.UtcNow < deadline);
                Assert.AreEqual(2, blockedCount, "Both actual native operations must block on the acquiring record gate.");
            }
            finally { commitControl.Enabled = false; commitControl.Release.TrySetResult(true); }
            var held = await acquisition;
            var racedWrite = await racingReplay;
            var racedClaim = await racingClaim;
            Assert.IsTrue(racedWrite.IsError); Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", racedWrite.ErrorCode);
            Assert.IsTrue(racedClaim.IsError); Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", racedClaim.ErrorCode);
            Assert.IsFalse(held.IsError, held.Message + held.Exception);
            Assert.AreEqual(HolonMigrationFenceState.Held, held.Result.State);
            var repeat = await other.AcquireHolonMigrationFenceAsync(fence);
            Assert.IsFalse(repeat.IsError, repeat.Message + repeat.Exception);
            Assert.AreEqual(held.Result.OwnerToken, repeat.Result.OwnerToken);
            if (reactivate)
            {
                Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
                Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            }
            var observed = await provider.ReadHolonMigrationFenceAsync(fence);
            Assert.IsFalse(observed.IsError, observed.Message + observed.Exception);
            Assert.AreEqual(held.Result.OwnerToken, observed.Result.OwnerToken);
            var replay = await other.ExecuteReplicatingHolonWriteAsync(request, enrollment);
            Assert.IsTrue(replay.IsError); Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", replay.ErrorCode);
            // New updates and both delete modes must use the same admission gate.
            var committed = saved.Result.Single();
            var kinds = new[] { ConditionalHolonOperation.Save, ConditionalHolonOperation.SoftDelete, ConditionalHolonOperation.HardDelete };
            for (var i = 0; i < kinds.Length; i++)
            {
                var blocked = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(blockedOperations[i], actor,
                    ProviderType.Neo4jOASIS, new[] { new HolonWriteCondition(record.Id,
                        CosmicConcurrency.GetProviderRevisionId(committed, ProviderType.Neo4jOASIS)) }),
                    kinds[i], new[] { committed }, saveChildren: false);
                var refused = await other.ExecuteConditionalHolonWriteAsync(blocked);
                Assert.IsTrue(refused.IsError); Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", refused.ErrorCode);
            }
            // Claim admission must roll back before touching attempt/lease state.
            using var observerDriver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
            using var observer = observerDriver.AsyncSession();
            async Task<string> QueueState()
            {
                var row = await (await observer.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$operation}) RETURN properties(o) AS values",
                    new { operation = operation.ToString("D") })).SingleAsync();
                return JsonSerializer.Serialize(row["values"]);
            }
            var beforeClaim = await QueueState();
            var outbox = other.GetBoundHolonReplicationOutbox(binding.Result);
            Assert.IsFalse(outbox.IsError, outbox.Message);
            var deniedClaim = await outbox.Result.ClaimNextAsync(target, "owned-fence-worker", TimeSpan.FromSeconds(30));
            Assert.IsTrue(deniedClaim.IsError); Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", deniedClaim.ErrorCode);
            Assert.AreEqual(beforeClaim, await QueueState(), "Rejected claims cannot consume an attempt or lease.");
            var historical = await other.ReadBoundConditionalHolonWriteOutcomeAsync(request, binding.Result);
            Assert.IsFalse(historical.IsError, historical.Message + historical.Exception);
            Assert.AreEqual(ConditionalHolonReceiptState.Committed, historical.Result.ReceiptState);
            var overlap = new HolonMigrationFenceRequest(competing, actor, binding.Result, new string('A', 64), new string('B', 64), new[] { record.Id });
            var rejected = await other.AcquireHolonMigrationFenceAsync(overlap);
            Assert.IsTrue(rejected.IsError); Assert.AreEqual("COSMIC_MIGRATION_FENCE_CONFLICT", rejected.ErrorCode);
            var unrelatedRequest = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(unrelatedOperation, actor,
                ProviderType.Neo4jOASIS, new[] { new HolonWriteCondition(unrelated.Id, null) }),
                ConditionalHolonOperation.Save, new[] { unrelated }, saveChildren: false);
            var independent = await other.ExecuteConditionalHolonWriteAsync(unrelatedRequest);
            Assert.IsFalse(independent.IsError, independent.Message + independent.Exception);
            var foreignCancel = await other.CancelHolonMigrationFenceAsync(fence, Guid.NewGuid());
            Assert.IsTrue(foreignCancel.IsError); Assert.AreEqual("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", foreignCancel.ErrorCode);
            var cancelled = await provider.CancelHolonMigrationFenceAsync(fence, held.Result.OwnerToken);
            Assert.IsFalse(cancelled.IsError, cancelled.Message + cancelled.Exception);
            Assert.AreEqual(HolonMigrationFenceState.Cancelled, cancelled.Result.State);
            var terminal = await other.AcquireHolonMigrationFenceAsync(fence);
            Assert.IsTrue(terminal.IsError); Assert.AreEqual("COSMIC_MIGRATION_FENCE_TERMINAL", terminal.ErrorCode);
            var resumed = await other.ExecuteReplicatingHolonWriteAsync(request, enrollment);
            Assert.IsFalse(resumed.IsError, resumed.Message + resumed.Exception);
            var admittedClaim = await outbox.Result.ClaimNextAsync(target, "owned-fence-worker", TimeSpan.FromSeconds(30));
            Assert.IsFalse(admittedClaim.IsError, admittedClaim.Message + admittedClaim.Exception);
            Assert.IsNotNull(admittedClaim.Result);
            // Durable native mutation marker: no migration business write is enabled.
            var phaseRoute = new HolonReplicationEnrollment("phase-source-" + phaseSeedOp, new[] { "phase-target-" + phaseSeedOp }, binding.Result);
            var phaseSeedRequest = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(phaseSeedOp, actor, ProviderType.Neo4jOASIS,
                new[] { new HolonWriteCondition(phaseId, null) }, phaseRoute), ConditionalHolonOperation.Save,
                new[] { new Holon { Id = phaseId, Name = "phase-protected" } }, saveChildren: false);
            var phaseSeed = await provider.ExecuteReplicatingHolonWriteAsync(phaseSeedRequest, phaseRoute); Assert.IsFalse(phaseSeed.IsError, phaseSeed.Message + phaseSeed.Exception);
            var incoming = new Holon { Id = phaseReplicaId, Name = "phase-incoming", Version = 1, VersionId = Guid.NewGuid(), CreatedByAvatarId = actor };
            CosmicConcurrency.SetProviderRevisionId(incoming, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            var incomingRequest = new CommittedHolonReplicaRequest(phaseReplicaOp, "phase-incoming-source", ProviderType.SQLLiteDBOASIS,
                actor, ConditionalHolonOperation.Save, new[] { incoming }, saveChildren: false);
            var received = await provider.ApplyBoundCommittedHolonReplicaAsync(incomingRequest, binding.Result); Assert.IsFalse(received.IsError, received.Message + received.Exception);
            var phase = new HolonMigrationFenceRequest(phaseMigration, actor, binding.Result, new string('A',64), new string('B',64), new[] { record.Id, phaseId, phaseReplicaId });
            var phaseHeld = await provider.AcquireHolonMigrationFenceAsync(phase); Assert.IsFalse(phaseHeld.IsError);
            var phaseOwner = phaseHeld.Result.OwnerToken;
            Assert.AreEqual("COSMIC_MIGRATION_FENCE_OWNER_MISMATCH", (await other.BeginHolonMigrationMutationAsync(new(phase, Guid.NewGuid(), new string('C',64)))).ErrorCode);
            var starts = await Task.WhenAll(provider.BeginHolonMigrationMutationAsync(new(phase, phaseOwner, new string('C',64))),
                other.BeginHolonMigrationMutationAsync(new(phase, phaseOwner, new string('D',64))));
            Assert.AreEqual(1, starts.Count(r => !r.IsError));
            var started = starts.Single(r => !r.IsError); Assert.IsTrue(started.IsSaved);
            Assert.AreEqual(HolonMigrationFenceState.MutationStarted, started.Result.State);
            Assert.AreEqual("COSMIC_MIGRATION_MUTATION_INTENT_REUSED", starts.Single(r => r.IsError).ErrorCode);
            var startRequest = new HolonMigrationMutationStartRequest(phase, phaseOwner, started.Result.MutationDigest);
            Assert.IsFalse((await provider.BeginHolonMigrationMutationAsync(startRequest)).IsSaved);
            var repeatedPhase = await other.AcquireHolonMigrationFenceAsync(phase); Assert.IsFalse(repeatedPhase.IsError);
            Assert.AreEqual(HolonMigrationFenceState.MutationStarted, repeatedPhase.Result.State); Assert.AreEqual(startRequest.MutationDigest, repeatedPhase.Result.MutationDigest);
            Assert.AreEqual("COSMIC_MIGRATION_MUTATION_ALREADY_STARTED", (await other.CancelHolonMigrationFenceAsync(phase, phaseOwner)).ErrorCode);
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            Assert.AreEqual("COSMIC_MIGRATION_FENCE_NOT_READY", (await provider.BeginHolonMigrationMutationAsync(startRequest)).ErrorCode);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var recoveredPhase = await provider.ReadHolonMigrationFenceAsync(phase); Assert.IsFalse(recoveredPhase.IsError);
            Assert.AreEqual(HolonMigrationFenceState.MutationStarted, recoveredPhase.Result.State); Assert.AreEqual(startRequest.MutationDigest, recoveredPhase.Result.MutationDigest);
            Assert.IsFalse((await provider.BeginHolonMigrationMutationAsync(startRequest)).IsSaved);
            foreach (var kind in new[] { ConditionalHolonOperation.Save, ConditionalHolonOperation.SoftDelete, ConditionalHolonOperation.HardDelete })
            {
                var blocked = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(blockedOperations[0], actor, ProviderType.Neo4jOASIS,
                    new[] { new HolonWriteCondition(record.Id, CosmicConcurrency.GetProviderRevisionId(saved.Result.Single(), ProviderType.Neo4jOASIS)) }),
                    kind, saved.Result, saveChildren: false);
                Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", (await other.ExecuteConditionalHolonWriteAsync(blocked)).ErrorCode);
            }
            Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", (await other.ApplyBoundCommittedHolonReplicaAsync(incomingRequest, binding.Result)).ErrorCode);
            var retainedIncoming = await provider.ReadCommittedHolonReplicaReceiptAsync(new(incomingRequest, binding.Result, HolonReplicaHeadReadConsistency.ReadCommittedValidated));
            Assert.IsFalse(retainedIncoming.IsError, retainedIncoming.Message + retainedIncoming.Exception);
            Assert.AreEqual(ConditionalHolonReceiptState.Committed, retainedIncoming.Result.ReceiptState);
            var phaseOutbox = provider.GetBoundHolonReplicationOutbox(binding.Result); Assert.IsFalse(phaseOutbox.IsError);
            Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", (await phaseOutbox.Result.ClaimNextAsync(phaseRoute.TargetIds.Single(), "phase-worker", TimeSpan.FromSeconds(30))).ErrorCode);
            var outside = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(phaseOtherOp, actor, ProviderType.Neo4jOASIS,
                new[] { new HolonWriteCondition(phaseOtherId, null) }), ConditionalHolonOperation.Save,
                new[] { new Holon { Id = phaseOtherId, Name = "phase-unrelated" } }, saveChildren: false);
            Assert.IsFalse((await other.ExecuteConditionalHolonWriteAsync(outside)).IsError);
            var racePhase = new HolonMigrationFenceRequest(phaseRaceMigration, actor, binding.Result, new string('A',64), new string('B',64), new[] { unrelated.Id });
            var raceHeld = await provider.AcquireHolonMigrationFenceAsync(racePhase); Assert.IsFalse(raceHeld.IsError);
            var raceOutcomes = await Task.WhenAll(provider.BeginHolonMigrationMutationAsync(new(racePhase, raceHeld.Result.OwnerToken, new string('E',64))),
                other.CancelHolonMigrationFenceAsync(racePhase, raceHeld.Result.OwnerToken));
            Assert.AreEqual(1, raceOutcomes.Count(r => !r.IsError));
            var raceState = await provider.ReadHolonMigrationFenceAsync(racePhase); Assert.IsFalse(raceState.IsError);
            Assert.IsTrue(raceState.Result.State is HolonMigrationFenceState.MutationStarted or HolonMigrationFenceState.Cancelled);
            // No delivery is sent: remove this exact known fixture claim with its owned queue below.
            // Delete only this journal's exact fixture identities. Global schema survives.
            using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
            using var session = driver.AsyncSession();
            var args = new { ids = new[] { record.Id, unrelated.Id, phaseId, phaseReplicaId, phaseOtherId }.Select(id => id.ToString("D")).ToArray(),
                operations = new[] { operation, unrelatedOperation, phaseSeedOp, phaseReplicaOp, phaseOtherOp }.Concat(blockedOperations).Select(id => id.ToString("D")).ToArray(),
                migrations = new[] { migration, competing, phaseMigration, phaseRaceMigration }.Select(id => id.ToString("D")).ToArray() };
            const string owned = "((n:Holon AND n.Id IN $ids) OR (n:CosmicReplicaHead AND n.HolonId IN $ids) OR (n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId IN $ids)) OR (n:CosmicMigrationRecordGate AND n.HolonId IN $ids) OR (n:CosmicMigrationFence AND n.MigrationId IN $migrations) OR ((n:CosmicWriteReceipt OR n:CosmicReplicaHead OR n:CosmicHolonOutbox) AND n.OperationId IN $operations)";
            await (await session.RunAsync("MATCH (n) WHERE " + owned + " DETACH DELETE n", args)).ConsumeAsync();
            var remaining = await (await session.RunAsync("MATCH (n) WHERE " + owned + " RETURN count(n) AS count", args)).SingleAsync();
            Assert.AreEqual(0L, remaining["count"].As<long>());
            File.Delete(journal);
        }
        finally
        {
            commitControl.Enabled = false; commitControl.Release.TrySetResult(true);
            Assert.IsFalse((await other.DeActivateProviderAsync()).IsError);
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
        }
    }
}
