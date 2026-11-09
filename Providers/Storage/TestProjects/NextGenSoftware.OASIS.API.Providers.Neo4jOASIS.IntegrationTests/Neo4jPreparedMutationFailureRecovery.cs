using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Neo = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using Sqlite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
public class Neo4jPreparedMutationFailureRecovery
{
    [TestMethod]
    public async Task ResumeKnownNonemptyHeadPreparationFailureWithoutReplayingSourceWrite()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-ReusableNeo4jTests.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent");
        var journal = Path.Combine(state, "source-receipt.pending.json");
        var journalText = await File.ReadAllTextAsync(journal);
        using var originalJournal = JsonDocument.Parse(journalText); var j = originalJournal.RootElement;
        var operation = j.GetProperty("operation").GetGuid(); var id = j.GetProperty("id").GetGuid();
        Assert.AreEqual(Guid.Parse("61e8ac4c-679e-4370-b8b6-399cd6580a25"), operation);
        Assert.IsTrue(j.GetProperty("startMutation").GetBoolean());
        var migration = j.GetProperty("migration").GetGuid(); var actor = j.GetProperty("actor").GetGuid();
        var database = Path.Combine(state, "source-receipt-target.db");
        var planFolder = Path.Combine(state, "barrier-plans");
        Assert.AreEqual(Path.GetFullPath(database), Path.GetFullPath(j.GetProperty("targetPath").GetString()!));
        Assert.AreEqual(Path.GetFullPath(planFolder), Path.GetFullPath(j.GetProperty("planFolder").GetString()!));
        using var configuration = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "barrier-host-context.json")));
        var c = configuration.RootElement;
        CosmicStorageIdentity Storage(string key)
        {
            var s = c.GetProperty(key);
            return new((ProviderType)s.GetProperty("Authority").GetInt32(), s.GetProperty("EndpointFingerprint").GetString()!, s.GetProperty("StoreId").GetString()!);
        }
        var sourceStorage = Storage("SourceStorage"); var targetStorage = Storage("OriginalTargetStorage");
        Assert.AreEqual(ProviderType.Neo4jOASIS, sourceStorage.Authority);
        Assert.AreEqual(ProviderType.SQLLiteDBOASIS, targetStorage.Authority);
        var context = new HolonReplicationMigrationAuthorizationContext(migration, actor, sourceStorage, targetStorage,
            c.GetProperty("OriginalSourceId").GetString()!, c.GetProperty("OriginalTargetId").GetString()!,
            new HolonReplicationRouteBinding(sourceStorage, Storage("replacementTarget"), c.GetProperty("replacementPolicy").GetString()!),
            c.GetProperty("PreviewDigest").GetString()!, c.GetProperty("AuthorizationPolicyBinding").GetString()!);
        Assert.AreEqual(c.GetProperty("ContextDigest").GetString(), context.ContextDigest);
        // Explicit renewal for this exact known owned fixture, never a new scope/operator.
        var policy = new ScopedHolonReplicationMigrationPolicy(context.AuthorizationPolicyBinding,
            new[] { new HolonReplicationMigrationGrant(context, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(5)) });
        var persisted = await new FileHolonMigrationBarrierPlanStore(planFolder).ReadAsync(context);
        Assert.IsFalse(persisted.IsError, persisted.Message + persisted.Exception); var plan = persisted.Result;
        Assert.IsTrue(plan.Scopes.All(s => s.HolonIds.SequenceEqual(new[] { id })));
        var source = new Neo(uri, "neo4j", "oasis-local-test-only");
        using (var target = new Sqlite($"Data Source={database};Pooling=False"))
        {
            try
            {
                Assert.IsFalse((await source.ActivateProviderAsync()).IsError);
                Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
                var participants = new[] { new HolonMigrationBarrierParticipant(sourceStorage, source), new HolonMigrationBarrierParticipant(targetStorage, target) };
                foreach (var participant in participants)
                {
                    var scope = plan.Scopes.Single(s => s.ExpectedStorage.BindingFingerprint == participant.Storage.BindingFingerprint);
                    var original = await participant.Provider.ReadHolonMigrationFenceAsync(scope);
                    Assert.IsFalse(original.IsError, original.Message + original.Exception);
                    Assert.AreEqual(HolonMigrationFenceState.Held, original.Result.State);
                }
                var outbox = source.GetBoundHolonReplicationOutbox(sourceStorage); Assert.IsFalse(outbox.IsError);
                var inventory = (IHolonReplicationLineageInventoryOutbox)outbox.Result;
                var delivery = await HolonMigrationDeliveryReconciliation.ReadAsync(plan, policy, participants, inventory, pageSize: 1);
                Assert.IsFalse(delivery.IsError, delivery.Message + delivery.Exception);
                Assert.AreEqual(1, delivery.Result.Obligations.Count);
                Assert.AreEqual(operation, delivery.Result.Obligations.Single().OperationId);
                var replacement = await HolonMigrationReplacementReview.ReadAsync(plan, policy, participants, pageSize: 1);
                Assert.IsFalse(replacement.IsError, replacement.Message + replacement.Exception);
                Assert.AreEqual(HolonMigrationReplacementKind.MatchingOriginalLineage, replacement.Result.Records.Single().Kind);
                var preparation = new HolonMigrationMutationPreparation(plan, delivery.Result, replacement.Result, "owned-neo-mutation",
                    HolonReplicaHeadReadConsistency.ReadCommittedValidated, HolonReplicaHeadReadConsistency.NativeSnapshot);
                var mutationPolicy = new ScopedHolonMigrationMutationPolicy(preparation.MutationPolicyBinding,
                    new[] { new HolonMigrationMutationGrant(preparation, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(5)) });
                var preparationStore = new FileHolonMigrationMutationPreparationStore(planFolder);
                var saved = await preparationStore.SaveAsync(preparation, plan, preparation.Fingerprint);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                var started = await HolonMigrationMutationCoordinator.StartPersistedAsync(plan, preparation.Fingerprint,
                    preparationStore, policy, mutationPolicy, participants);
                Assert.IsFalse(started.IsError, started.Message + started.Exception);
                Assert.IsTrue(started.Result.AllParticipantsStarted); Assert.AreEqual(2, started.SavedCount);
                Assert.IsFalse((await source.DeActivateProviderAsync()).IsError);
                Assert.IsFalse((await target.DeActivateProviderAsync()).IsError);
                Assert.IsFalse((await source.ActivateProviderAsync()).IsError);
                Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
                var resumed = await HolonMigrationMutationCoordinator.StartPersistedAsync(plan, preparation.Fingerprint,
                    new FileHolonMigrationMutationPreparationStore(planFolder), policy, mutationPolicy, participants);
                Assert.IsFalse(resumed.IsError, resumed.Message + resumed.Exception);
                Assert.IsTrue(resumed.Result.AllParticipantsStarted); Assert.IsFalse(resumed.IsSaved);
                var retainedDelivery = await HolonMigrationDeliveryReconciliation.ReadAsync(plan, policy, participants,
                    (IHolonReplicationLineageInventoryOutbox)source.GetBoundHolonReplicationOutbox(sourceStorage).Result, pageSize: 1);
                Assert.IsFalse(retainedDelivery.IsError, retainedDelivery.Message + retainedDelivery.Exception);
                Assert.AreEqual(delivery.Result.ObservationDigest, retainedDelivery.Result.ObservationDigest);
                foreach (var participant in participants)
                {
                    var scope = plan.Scopes.Single(s => s.ExpectedStorage.BindingFingerprint == participant.Storage.BindingFingerprint);
                    var observed = await participant.Provider.ReadHolonMigrationFenceAsync(scope);
                    Assert.IsFalse(observed.IsError); Assert.AreEqual(preparation.Fingerprint, observed.Result.MutationDigest);
                    var cancelled = await participant.Provider.CancelHolonMigrationFenceAsync(scope, observed.Result.OwnerToken);
                    Assert.IsTrue(cancelled.IsError); Assert.AreEqual("COSMIC_MIGRATION_MUTATION_ALREADY_STARTED", cancelled.ErrorCode);
                }
                await File.WriteAllTextAsync(Path.Combine(root.FullName, "TestResults/Neo4jCurrent/prepared-mutation-reviewed-journal.json"), journalText);
            }
            finally { Assert.IsFalse((await source.DeActivateProviderAsync()).IsError); }
        }
        // Known owned marker-only fixture: no source replay or business cutover occurred.
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var session = driver.AsyncSession();
        var args = new { id = id.ToString("D"), operation = operation.ToString("D"), migration = migration.ToString("D") };
        var body = await (await session.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot", args)).SingleAsync();
        using var snapshot = JsonDocument.Parse(body["snapshot"].As<string>());
        Assert.AreEqual(1, snapshot.RootElement.GetProperty("Version").GetInt32());
        Assert.AreEqual("private-neo-original-receipt", snapshot.RootElement.GetProperty("Name").GetString());
        await (await session.RunAsync("MATCH (n) WHERE (n:Holon AND n.Id=$id) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId=$operation) OR (n:CosmicMigrationFence AND n.MigrationId=$migration) OR ((n:CosmicMigrationRecordGate OR n:CosmicReplicaHead) AND n.HolonId=$id) OR (n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId=$id) DETACH DELETE n", args)).ConsumeAsync();
        var remaining = await (await session.RunAsync("MATCH (n) WHERE (n:Holon AND n.Id=$id) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId=$operation) OR (n:CosmicMigrationFence AND n.MigrationId=$migration) OR (n:CosmicMigrationRecordGate AND n.HolonId=$id) RETURN count(n) AS count", args)).SingleAsync();
        Assert.AreEqual(0L, remaining["count"].As<long>());
        foreach (var path in new[] { database, database + "-wal", database + "-shm" }) if (File.Exists(path)) File.Delete(path);
        foreach (var extension in new[] { ".barrier-plan.json", ".mutation-preparation.json" })
            foreach (var suffix in new[] { "", ".pending", ".lock" })
            {
                var path = Path.Combine(planFolder, migration.ToString("D") + extension + suffix);
                if (File.Exists(path)) File.Delete(path);
            }
        foreach (var name in new[] { "barrier-host-context.json", "barrier-process-acquired.json", "barrier-process-resumed.json" }) File.Delete(Path.Combine(state, name));
        File.Delete(journal);
    }
}
