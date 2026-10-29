using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeBoundOriginalReceiptPreservesHistoricalIntentAndQueue(bool bindOriginal)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-ReusableNeo4jTests.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var journal = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/source-receipt.pending.json");
        Directory.CreateDirectory(Path.GetDirectoryName(journal)!);
        Assert.IsFalse(File.Exists(journal), "Unfinished original outcomes require reconciliation.");
        var targetPath = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/source-receipt-target.db");
        Assert.IsFalse(File.Exists(targetPath), "Preserve any unfinished replica fixture.");
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = driver.AsyncSession();
        var input = new Holon { Id = Guid.NewGuid(), Name = "private-neo-original-receipt" };
        var operation = Guid.NewGuid(); var appendOperation = Guid.NewGuid(); var migration = Guid.NewGuid();
        var planFolder = Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent/barrier-plans");
        var actor = Guid.NewGuid();
        var arguments = new { id = input.Id.ToString("D"), operation = operation.ToString("D") };
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var captured = await provider.CaptureStorageIdentityAsync(); Assert.IsFalse(captured.IsError);
            var expected = captured.Result;
            var enrollment = new HolonReplicationEnrollment("owned-source-receipt-" + operation, new[] { "target", "target-b", "\uE000", "\U0001F600" }, bindOriginal ? expected : null);
            var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, actor,
                ProviderType.Neo4jOASIS, new[] { new HolonWriteCondition(input.Id, null) }, enrollment),
                ConditionalHolonOperation.Save, new[] { input }, saveChildren: false);
            using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, new { id = input.Id, operation, appendOperation, migration, planFolder, actor, source = expected, bindOriginal, targetPath });
                file.Flush(true);
            }
            var absent = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(request, expected);
            Assert.IsFalse(absent.IsError, absent.Message + absent.Exception);
            Assert.AreEqual(ConditionalHolonReceiptState.Absent, absent.Result.ReceiptState);
            var saved = await provider.ExecuteReplicatingHolonWriteAsync(request, enrollment);
            Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            async Task<string> Snapshot()
            {
                var rows = await (await inspection.RunAsync(
                    "MATCH (n) WHERE (n:Holon AND n.Id=$id) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId=$operation) RETURN labels(n) AS labels, properties(n) AS values ORDER BY labels(n)[0]", arguments)).ToListAsync();
                Assert.AreEqual(2 + enrollment.TargetIds.Count, rows.Count);
                return JsonSerializer.Serialize(rows.Select(row => new { labels = row["labels"], values = row["values"] }));
            }
            var before = await Snapshot();
            var verified = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(request, expected);
            Assert.IsFalse(verified.IsError, verified.Message + verified.Exception);
            Assert.AreEqual(ConditionalHolonReceiptState.Committed, verified.Result.ReceiptState);
            Assert.IsFalse(verified.IsSaved);
            var comparison = new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, new string('A', 64), Guid.NewGuid());
            var projected = await provider.ReadConditionalHolonReceiptEvidenceAsync(new(request, expected, comparison, new[] { input.Id }));
            Assert.IsFalse(projected.IsError, projected.Message + projected.Exception);
            Assert.AreEqual(input.Id, projected.Result.ReceiptEvidence.Records.Single().HolonId);
            Assert.AreEqual(1, projected.Result.ReceiptEvidence.OriginalSelectedRecordCount);
            Assert.AreEqual(comparison.BindingFingerprint, projected.Result.ReceiptEvidence.ComparisonTargetFingerprint);

            Assert.AreSame(bindOriginal ? expected : null, request.Write.ReplicationEnrollment.SourceStorageIdentity);
            Assert.IsFalse(JsonSerializer.Serialize(verified.Result).Contains(input.Name, StringComparison.Ordinal));
            var wrong = new CosmicStorageIdentity(expected.Authority, expected.EndpointFingerprint, Guid.NewGuid());
            var rejected = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(request, wrong);
            Assert.IsTrue(rejected.IsError); Assert.IsNull(rejected.Result);
            var foreign = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, Guid.NewGuid(),
                ProviderType.Neo4jOASIS, request.Write.Conditions, enrollment), request.Operation, request.Roots, saveChildren: false);
            var foreignOutcome = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(foreign, expected);
            Assert.IsTrue(foreignOutcome.IsError); Assert.IsNull(foreignOutcome.Result);
            Assert.AreEqual(before, await Snapshot(), "Evidence reads must not alter graph, receipt or pending queue.");
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var reconstructed = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(request, expected);
            Assert.IsFalse(reconstructed.IsError, reconstructed.Message + reconstructed.Exception);
            Assert.AreEqual(ConditionalHolonReceiptState.Committed, reconstructed.Result.ReceiptState);
            Assert.AreEqual(before, await Snapshot());
            // Actual Neo source -> independent SQLite receiver. Source receipt is
            // immutable; the target observation uses its native snapshot reader.
            using (var replica = new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={targetPath};Pooling=False"))
            {
                Assert.IsFalse((await replica.ActivateProviderAsync()).IsError);
                var targetBinding = await replica.CaptureStorageIdentityAsync(); Assert.IsFalse(targetBinding.IsError);
                var preflightRequest = new ConditionalHolonReceiptEvidenceRequest(request, expected, targetBinding.Result, new[] { input.Id });
                var missing = await HolonReplicationReceiptPreflight.ReadAsync(provider, replica, preflightRequest);
                Assert.IsFalse(missing.IsError, missing.Message + missing.Exception);
                Assert.AreEqual(HolonReplicationReceiptComparisonKind.MissingReplicaHead, missing.Result.Records[0].Kind);
                var originalReplica = new CommittedHolonReplicaRequest(operation, enrollment.SourceId,
                    ProviderType.Neo4jOASIS, actor, request.Operation, saved.Result, saveChildren: false);
                var receiptRequest = new CommittedHolonReplicaReceiptReadRequest(originalReplica, targetBinding.Result);
                var absentReceipt = await replica.ReadCommittedHolonReplicaReceiptAsync(receiptRequest);
                Assert.IsFalse(absentReceipt.IsError, absentReceipt.Message + absentReceipt.Exception);
                Assert.AreEqual(ConditionalHolonReceiptState.Absent, absentReceipt.Result.ReceiptState);
                var applied = await replica.ApplyBoundCommittedHolonReplicaAsync(originalReplica, targetBinding.Result);
                Assert.IsFalse(applied.IsError, applied.Message + applied.Exception);
                var historicalReceipt = await replica.ReadCommittedHolonReplicaReceiptAsync(receiptRequest);
                Assert.IsFalse(historicalReceipt.IsError, historicalReceipt.Message + historicalReceipt.Exception);
                Assert.AreEqual(ConditionalHolonReceiptState.Committed, historicalReceipt.Result.ReceiptState);
                var wrongBinding = new CosmicStorageIdentity(targetBinding.Result.Authority,
                    targetBinding.Result.EndpointFingerprint, Guid.NewGuid());
                var wrongReceipt = await replica.ReadCommittedHolonReplicaReceiptAsync(new(originalReplica, wrongBinding));
                Assert.IsTrue(wrongReceipt.IsError); Assert.IsNull(wrongReceipt.Result);
                Assert.AreEqual("COSMIC_REPLICA_RECEIPT_STORAGE_MISMATCH", wrongReceipt.ErrorCode);
                var boundSource = provider.GetBoundHolonReplicationOutbox(expected);
                Assert.IsFalse(boundSource.IsError, boundSource.Message + boundSource.Exception);
                await CosmicNativeAcceptance.CosmicOperationInventoryProbe.CompleteAlreadyAppliedTargetAsync(
                    boundSource.Result, expected, operation, "target", HolonReplicaHeadReadConsistency.ReadCommittedValidated);
                await CosmicNativeAcceptance.CosmicOperationInventoryProbe.VerifyAsync(boundSource.Result, expected,
                    operation, enrollment.SourceId, actor, enrollment.TargetIds, requireCompleted: true,
                    consistency: HolonReplicaHeadReadConsistency.ReadCommittedValidated, migrationSource: provider);

                var reviewedPreview = await CosmicNativeAcceptance.CosmicLineageInventoryProbe.VerifyPreviewAsync(boundSource.Result,
                    expected, targetBinding.Result, enrollment.SourceId, operation, enrollment.TargetIds.Count,
                    () => HolonReplicationReceiptPreflight.ReadAsync(provider, replica, preflightRequest),
                    HolonReplicaHeadReadConsistency.ReadCommittedValidated,
                    () => replica.ReadCommittedHolonReplicaReceiptAsync(receiptRequest), request);
                // Coordinate actual Neo source + SQLite receiver from native evidence.
                // This fixture performs no migration mutation, so explicit owner
                // cancellation below is still a pre-mutation operation.
                var context = reviewedPreview.AuthorizationContext(migration, actor, "owned-native-migration-admin");
                var policy = new ScopedHolonReplicationMigrationPolicy(context.AuthorizationPolicyBinding,
                    new[] { new HolonReplicationMigrationGrant(context, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(5)) });
                var participants = new[] { new HolonMigrationBarrierParticipant(expected, provider),
                    new HolonMigrationBarrierParticipant(targetBinding.Result, replica) };
                var durablePlan = HolonReplicationMigrationBarrierPlan.Create(reviewedPreview, context);
                var persisted = await new FileHolonMigrationBarrierPlanStore(planFolder).SaveAsync(durablePlan);
                Assert.IsFalse(persisted.IsError, persisted.Message + persisted.Exception);
                await NativeMigrationBarrierProcessTests.VerifyProcessRestartAsync(root.FullName, context);
                var held = await HolonReplicationMigrationBarrierCoordinator.ResumePersistedAsync(context,
                    new FileHolonMigrationBarrierPlanStore(planFolder), policy, participants);
                Assert.IsFalse(held.IsError, held.Message + held.Exception);
                Assert.IsTrue(held.Result.AllBarriersHeld); Assert.AreEqual(2, held.Result.Stores.Count);
                var barredSource = await provider.ExecuteReplicatingHolonWriteAsync(request, enrollment);
                Assert.IsTrue(barredSource.IsError); Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", barredSource.ErrorCode);
                var barredTarget = await replica.ApplyBoundCommittedHolonReplicaAsync(originalReplica, targetBinding.Result);
                Assert.IsTrue(barredTarget.IsError); Assert.AreEqual("COSMIC_MIGRATION_RECORD_FENCED", barredTarget.ErrorCode);
                var retained = await replica.ReadCommittedHolonReplicaReceiptAsync(receiptRequest);
                Assert.IsFalse(retained.IsError); Assert.AreEqual(historicalReceipt.Result.AcknowledgementDigest, retained.Result.AcknowledgementDigest);
                // Fresh native owners recover receipts without another acquire.
                var recoveredSource = new Provider(uri, "neo4j", "oasis-local-test-only");
                using (var recoveredTarget = new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={targetPath};Pooling=False"))
                {
                    try
                    {
                        Assert.IsFalse((await recoveredSource.ActivateProviderAsync()).IsError);
                        Assert.IsFalse((await recoveredTarget.ActivateProviderAsync()).IsError);
                        var resumed = await HolonReplicationMigrationBarrierCoordinator.ResumePersistedAsync(context,
                            new FileHolonMigrationBarrierPlanStore(planFolder), policy,
                            new[] { new HolonMigrationBarrierParticipant(expected, recoveredSource), new HolonMigrationBarrierParticipant(targetBinding.Result, recoveredTarget) });
                        Assert.IsFalse(resumed.IsError, resumed.Message + resumed.Exception); Assert.IsTrue(resumed.Result.AllBarriersHeld);
                        Assert.IsFalse(resumed.IsSaved, "Recovery must read original native receipts rather than acquire again.");
                        var deliveryReview = await HolonMigrationDeliveryReconciliation.ReadAsync(durablePlan, policy,
                            new[] { new HolonMigrationBarrierParticipant(expected, recoveredSource), new HolonMigrationBarrierParticipant(targetBinding.Result, recoveredTarget) },
                            (IHolonReplicationLineageInventoryOutbox)boundSource.Result, pageSize: 1);
                        Assert.IsFalse(deliveryReview.IsError, deliveryReview.Message + deliveryReview.Exception);
                        Assert.IsFalse(deliveryReview.IsSaved); Assert.AreEqual(enrollment.TargetIds.Count, deliveryReview.Result.Obligations.Count);
                        Assert.AreEqual(HolonMigrationDeliveryObservationKind.OriginalReceiverCommitted,
                            deliveryReview.Result.Obligations.Single(r => r.TargetId == "target").Kind);
                        Assert.AreEqual(3, deliveryReview.Result.Obligations.Count(r => r.Kind == HolonMigrationDeliveryObservationKind.OtherAffectedRoute));
                        foreach (var participant in new[] { new HolonMigrationBarrierParticipant(expected, recoveredSource), new HolonMigrationBarrierParticipant(targetBinding.Result, recoveredTarget) })
                        {
                            var retainedPlan = await new FileHolonMigrationBarrierPlanStore(planFolder).ReadAsync(context);
                            Assert.IsFalse(retainedPlan.IsError);
                            var scope = retainedPlan.Result.Scopes.Single(s => s.ExpectedStorage.BindingFingerprint == participant.Storage.BindingFingerprint);
                            var observation = await participant.Provider.ReadHolonMigrationFenceAsync(scope);
                            Assert.IsFalse(observation.IsError, observation.Message + observation.Exception);
                            var released = await participant.Provider.CancelHolonMigrationFenceAsync(scope, observation.Result.OwnerToken);
                            Assert.IsFalse(released.IsError, released.Message + released.Exception);
                            Assert.AreEqual(HolonMigrationFenceState.Cancelled, released.Result.State);
                        }
                    }
                    finally { Assert.IsFalse((await recoveredSource.DeActivateProviderAsync()).IsError); }
                }
                await CosmicNativeAcceptance.CosmicLineageInventoryProbe.VerifyWindowAsync(boundSource.Result, expected,
                    enrollment.SourceId, operation, enrollment.TargetIds.Count, appendOperation, async () =>
                    {
                        var prior = saved.Result.Single();
                        var edit = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(prior);
                        edit.Name = "later-lineage-operation";
                        var update = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(appendOperation, actor, ProviderType.Neo4jOASIS,
                            new[] { new HolonWriteCondition(input.Id, CosmicConcurrency.GetProviderRevisionId(prior, ProviderType.Neo4jOASIS)) }, enrollment),
                            ConditionalHolonOperation.Save, new[] { edit }, saveChildren: false);
                        var appended = await provider.ExecuteReplicatingHolonWriteAsync(update, enrollment);
                        Assert.IsFalse(appended.IsError, appended.Message + appended.Exception);
                    }, HolonReplicaHeadReadConsistency.ReadCommittedValidated);
                var matched = await HolonReplicationReceiptPreflight.ReadAsync(provider, replica, preflightRequest);
                Assert.IsFalse(matched.IsError, matched.Message + matched.Exception);
                Assert.AreEqual(HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt, matched.Result.Records[0].Kind);
                Assert.AreEqual(HolonReplicaHeadReadConsistency.NativeSnapshot, matched.Result.TargetConsistency);
                Assert.IsFalse(matched.IsSaved);
                Assert.AreEqual(matched.Result.ObservationDigest,
                    (await HolonReplicationReceiptPreflight.ReadAsync(provider, replica, preflightRequest)).Result.ObservationDigest);
                using var native = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={targetPath};Pooling=False");
                await native.OpenAsync();
                using var edit = native.CreateCommand();
                edit.CommandText = "UPDATE Holon SET Name=$name WHERE Id=$id";
                edit.Parameters.AddWithValue("$name", "independent-sqlite-edit");
                edit.Parameters.AddWithValue("$id", input.Id.ToString("D"));
                Assert.AreEqual(1, await edit.ExecuteNonQueryAsync());
                var changed = await HolonReplicationReceiptPreflight.ReadAsync(provider, replica, preflightRequest);
                Assert.IsTrue(changed.IsError, "An unchanged revision cannot certify changed current SQLite content.");
                Assert.IsNull(changed.Result);
                Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED", changed.ErrorCode);
                // Current content has diverged, but the original delivery remains
                // provable through its immutable receipt; never replay the write.
                var retainedReceipt = await replica.ReadCommittedHolonReplicaReceiptAsync(receiptRequest);
                Assert.IsFalse(retainedReceipt.IsError, retainedReceipt.Message + retainedReceipt.Exception);
                Assert.AreEqual(historicalReceipt.Result.AcknowledgementDigest, retainedReceipt.Result.AcknowledgementDigest);
                Assert.AreEqual(ConditionalHolonReceiptState.Committed, retainedReceipt.Result.ReceiptState);

            }
            foreach (var path in new[] { targetPath, targetPath + "-wal", targetPath + "-shm" })
                if (File.Exists(path)) File.Delete(path);
            await (await inspection.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h", arguments)).ConsumeAsync();
            await (await inspection.RunAsync("MATCH (n) WHERE (n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $operations DETACH DELETE n", new { operations = new[] { operation.ToString("D"), appendOperation.ToString("D") } })).ConsumeAsync();
            var remaining = await (await inspection.RunAsync("MATCH (n) WHERE (n:Holon AND n.Id=$id) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $operations) RETURN count(n) AS count", new { arguments.id, operations = new[] { operation.ToString("D"), appendOperation.ToString("D") } })).SingleAsync();
            Assert.AreEqual(0L, remaining["count"].As<long>());
            await (await inspection.RunAsync("MATCH (n) WHERE (n:CosmicMigrationFence AND n.MigrationId=$migration) OR (n:CosmicMigrationRecordGate AND n.HolonId=$id) DETACH DELETE n",
                new { migration = migration.ToString("D"), arguments.id })).ConsumeAsync();
            var fenceRemaining = await (await inspection.RunAsync("MATCH (n) WHERE (n:CosmicMigrationFence AND n.MigrationId=$migration) OR (n:CosmicMigrationRecordGate AND n.HolonId=$id) RETURN count(n) AS count",
                new { migration = migration.ToString("D"), arguments.id })).SingleAsync();
            Assert.AreEqual(0L, fenceRemaining["count"].As<long>());
            foreach (var suffix in new[] { "", ".pending", ".lock" })
            {
                var ownedPlanPath = Path.Combine(planFolder, migration.ToString("D") + ".barrier-plan.json" + suffix);
                if (File.Exists(ownedPlanPath)) File.Delete(ownedPlanPath);
            }
            foreach (var name in new[] { "barrier-host-context.json", "barrier-process-acquired.json", "barrier-process-resumed.json" })
                File.Delete(Path.Combine(root.FullName, "LocalState/COSMIC/Neo4jCurrent", name));
            File.Delete(journal);
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
    }
}
