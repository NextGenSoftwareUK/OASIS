using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using Sqlite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public partial class MongoStorageIdentityTests
{
    // Suppress one acknowledgement after the real native acquisition commits.
    // No network fault or server crash is claimed by this controlled boundary.
    private sealed class LostBarrierAcknowledgement(IHolonMigrationFenceStorageProvider native) : IHolonMigrationFenceStorageProvider
    {
        public async Task<OASISResult<HolonMigrationFenceObservation>> AcquireHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken token = default)
        {
            var result = await native.AcquireHolonMigrationFenceAsync(request, token);
            Assert.False(result.IsError, result.Message + result.Exception);
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "OWNED_NATIVE_ACKNOWLEDGEMENT_LOST",
                Exception = new IOException("controlled acknowledgement loss after native commit") };
        }
        public Task<OASISResult<HolonMigrationFenceObservation>> ReadHolonMigrationFenceAsync(HolonMigrationFenceRequest request, CancellationToken token = default)
            => native.ReadHolonMigrationFenceAsync(request, token);
        public Task<OASISResult<HolonMigrationFenceObservation>> CancelHolonMigrationFenceAsync(HolonMigrationFenceRequest request, Guid owner, CancellationToken token = default)
            => throw new InvalidOperationException("Coordinator must preserve unknown acquisitions.");
    }

    private static async Task VerifyNativeThreeStoreBarrierRecovery(Provider source, Provider target,
        CosmicStorageIdentity sourceBinding, CosmicStorageIdentity targetBinding, IHolonReplicationRouteInventoryOutbox outbox,
        ConditionalHolonWriteRequest original, HolonReplicationEnrollment enrollment, IHolon acknowledged,
        Func<Task<OASISResult<HolonReplicationReceiptPreflightReport>>> readReceipt, bool foreignReplacement)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Run-CosmicMongoDestinationTests.ps1"))) root = root.Parent;
        Assert.NotNull(root);
        var folder = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent");
        var database = Path.Combine(folder, "mongo-barrier-target.db");
        var journal = Path.Combine(folder, "mongo-barrier-coordinator.pending.json");
        var planFolder = Path.Combine(folder, "barrier-plans");
        Assert.False(File.Exists(database)); Assert.False(File.Exists(journal));
        var migration = Guid.NewGuid(); var replacementSeedOperation = Guid.NewGuid(); var blockedReplacementOperation = Guid.NewGuid(); var actor = original.Write.ActorId;
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file, new { migration, replacementSeedOperation, foreignReplacement, blockedReplacementOperation, actor, database, planFolder, operation = original.Write.OperationId,
                holon = acknowledged.Id, sourceBinding, targetBinding }); file.Flush(true);
        }
        using (var replacement = new Sqlite($"Data Source={database};Pooling=False"))
        {
            Assert.False((await replacement.ActivateProviderAsync()).IsError);
            var replacementBinding = await replacement.CaptureStorageIdentityAsync(); Assert.False(replacementBinding.IsError);
            if (foreignReplacement)
            {
                // Deliberate private fixture: same business data, foreign lineage.
                // Matching content must never become permission to adopt this head.
                var seeded = await replacement.ApplyBoundCommittedHolonReplicaAsync(new(replacementSeedOperation,
                    "owned-foreign-replacement-" + migration, ProviderType.MongoDBOASIS, actor, original.Operation,
                    new[] { acknowledged }, saveChildren: false), replacementBinding.Result);
                Assert.False(seeded.IsError, seeded.Message + seeded.Exception);
            }
            var transport = new CommittedHolonReplicaRequest(original.Write.OperationId, enrollment.SourceId,
                ProviderType.MongoDBOASIS, actor, original.Operation, new[] { acknowledged }, saveChildren: false);
            var receiptRequest = new CommittedHolonReplicaReceiptReadRequest(transport, targetBinding);
            var originalReceipt = await target.ReadCommittedHolonReplicaReceiptAsync(receiptRequest); Assert.False(originalReceipt.IsError);
            var preview = await CosmicNativeAcceptance.CosmicLineageInventoryProbe.VerifyPreviewAsync(outbox,
                sourceBinding, targetBinding, enrollment.SourceId, original.Write.OperationId, enrollment.TargetIds.Count,
                readReceipt, readReplicaReceipt: () => target.ReadCommittedHolonReplicaReceiptAsync(receiptRequest),
                originalWrite: original, replacementTarget: replacementBinding.Result);
            var context = preview.AuthorizationContext(migration, actor, "owned-three-store-admin");
            var policy = new ScopedHolonReplicationMigrationPolicy(context.AuthorizationPolicyBinding,
                new[] { new HolonReplicationMigrationGrant(context, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(5)) });
            var participants = new[] { new HolonMigrationBarrierParticipant(sourceBinding, source),
                new HolonMigrationBarrierParticipant(targetBinding, target), new HolonMigrationBarrierParticipant(replacementBinding.Result, replacement) }
                .OrderBy(p => p.Storage.BindingFingerprint, StringComparer.Ordinal).ToArray();
            participants[2] = participants[2] with { Provider = new LostBarrierAcknowledgement(participants[2].Provider) };
            var planStore = new FileHolonMigrationBarrierPlanStore(planFolder);
            var plan = HolonReplicationMigrationBarrierPlan.Create(preview, context);
            var partial = await HolonReplicationMigrationBarrierCoordinator.AcquirePersistedAsync(plan, planStore, policy, participants);
            Assert.True(partial.IsError); Assert.Equal("OWNED_NATIVE_ACKNOWLEDGEMENT_LOST", partial.ErrorCode);
            Assert.Equal(new[] { HolonMigrationBarrierObservationState.Held, HolonMigrationBarrierObservationState.Held,
                HolonMigrationBarrierObservationState.Unknown }, partial.Result.Stores.Select(s => s.State));
            // All three receipts survive reconstruction of their native owners.
            var reopenedSource = new Provider(Uri, First); var reopenedTarget = new Provider(Uri, Second);
            using var reopenedReplacement = new Sqlite($"Data Source={database};Pooling=False");
            try
            {
                Assert.False((await reopenedSource.ActivateProviderAsync()).IsError);
                Assert.False((await reopenedTarget.ActivateProviderAsync()).IsError);
                Assert.False((await reopenedReplacement.ActivateProviderAsync()).IsError);
                var recoveredParticipants = new[] { new HolonMigrationBarrierParticipant(sourceBinding, reopenedSource),
                    new HolonMigrationBarrierParticipant(targetBinding, reopenedTarget), new HolonMigrationBarrierParticipant(replacementBinding.Result, reopenedReplacement) };
                var recovered = await HolonReplicationMigrationBarrierCoordinator.ResumePersistedAsync(context,
                    new FileHolonMigrationBarrierPlanStore(planFolder), policy, recoveredParticipants);
                Assert.False(recovered.IsError, recovered.Message + recovered.Exception); Assert.True(recovered.Result.AllBarriersHeld);
                Assert.Equal(3, recovered.Result.Stores.Count); Assert.False(recovered.IsSaved);
                var deniedSource = await reopenedSource.ExecuteReplicatingHolonWriteAsync(original, enrollment);
                Assert.True(deniedSource.IsError); Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", deniedSource.ErrorCode);
                var deniedTarget = await reopenedTarget.ApplyBoundCommittedHolonReplicaAsync(transport, targetBinding);
                Assert.True(deniedTarget.IsError); Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", deniedTarget.ErrorCode);
                var deniedReplacement = await reopenedReplacement.ExecuteConditionalHolonWriteAsync(new(
                    new ConditionalHolonWrite(blockedReplacementOperation, actor, ProviderType.SQLLiteDBOASIS,
                        new[] { new HolonWriteCondition(acknowledged.Id, null) }), ConditionalHolonOperation.Save,
                    new[] { acknowledged }, saveChildren: false));
                Assert.True(deniedReplacement.IsError); Assert.Equal("COSMIC_MIGRATION_RECORD_FENCED", deniedReplacement.ErrorCode);
                var retained = await reopenedTarget.ReadCommittedHolonReplicaReceiptAsync(receiptRequest);
                Assert.False(retained.IsError); Assert.Equal(originalReceipt.Result.AcknowledgementDigest, retained.Result.AcknowledgementDigest);
                var deliveryReview = await HolonMigrationDeliveryReconciliation.ReadAsync(plan, policy,
                    recoveredParticipants, (IHolonReplicationLineageInventoryOutbox)outbox, pageSize: 1);
                Assert.False(deliveryReview.IsError, deliveryReview.Message + deliveryReview.Exception);
                Assert.False(deliveryReview.IsSaved); Assert.Equal(enrollment.TargetIds.Count, deliveryReview.Result.Obligations.Count);
                Assert.Equal(HolonMigrationDeliveryObservationKind.OriginalReceiverCommitted,
                    deliveryReview.Result.Obligations.Single(r => r.TargetId == "target").Kind);
                Assert.Equal(3, deliveryReview.Result.Obligations.Count(r => r.Kind == HolonMigrationDeliveryObservationKind.OtherAffectedRoute));
                var replacementReview = await HolonMigrationReplacementReview.ReadAsync(plan, policy, recoveredParticipants, pageSize: 1);
                Assert.False(replacementReview.IsError, replacementReview.Message + replacementReview.Exception); Assert.False(replacementReview.IsSaved);
                Assert.Equal(foreignReplacement ? HolonMigrationReplacementKind.ForeignLineage : HolonMigrationReplacementKind.EmptyAtObservation,
                    Assert.Single(replacementReview.Result.Records).Kind);
                // No migration data has been changed: explicit exact-owner pre-mutation cancellation is valid.
                foreach (var participant in recoveredParticipants)
                {
                    var retainedPlan = await new FileHolonMigrationBarrierPlanStore(planFolder).ReadAsync(context);
                    Assert.False(retainedPlan.IsError);
                    var scope = retainedPlan.Result.Scopes.Single(s => s.ExpectedStorage.BindingFingerprint == participant.Storage.BindingFingerprint);
                    var observation = await participant.Provider.ReadHolonMigrationFenceAsync(scope); Assert.False(observation.IsError);
                    var cancelled = await participant.Provider.CancelHolonMigrationFenceAsync(scope, observation.Result.OwnerToken);
                    Assert.False(cancelled.IsError, cancelled.Message + cancelled.Exception);
                    Assert.Equal(HolonMigrationFenceState.Cancelled, cancelled.Result.State);
                }
            }
            finally
            {
                Assert.False((await reopenedTarget.DeActivateProviderAsync()).IsError);
                Assert.False((await reopenedSource.DeActivateProviderAsync()).IsError);
            }
        }
        foreach (var path in new[] { database, database + "-wal", database + "-shm" }) if (File.Exists(path)) File.Delete(path);
        foreach (var suffix in new[] { "", ".pending", ".lock" })
        {
            var ownedPlanPath = Path.Combine(planFolder, migration.ToString("D") + ".barrier-plan.json" + suffix);
            if (File.Exists(ownedPlanPath)) File.Delete(ownedPlanPath);
        }
        File.Delete(journal);
        // RunOwned retains or drops both exact Mongo fixture databases after this returns.
    }
}
