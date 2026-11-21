using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class FirestoreSqliteOutageTests
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task SqliteOutageRoutesToFirestore(bool synchronous)
    {
        Assert.AreEqual("127.0.0.1:18180", Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        const string project = "demo-oasis-firestore-outage";
        var db = GoogleNativeFirestore.Database(project);
        var fixture = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_FIRESTORE_CI_FIXTURE")
            ?? throw new InvalidOperationException("An owned Firestore fixture directory is required."));
        Assert.IsTrue(Directory.Exists(fixture));
        var id = Guid.NewGuid();
        var path = Path.Combine(fixture, $"{id:N}.unavailable.db");
        Assert.IsFalse(File.Exists(path));
        using var unavailable = new SqliteProvider($"Data Source={path};Mode=ReadWrite;Pooling=False;Default Timeout=1");
        var firestore = new GoogleCloudOASIS(projectId: project, enableStorage: false, enableFirestore: true, enableBigQuery: false);
        var document = db.Collection("holons").Document(id.ToString());
        const string expected = "Firestore outage replica";
        try
        {
            var activated = await firestore.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message);
            var bindings = await Task.WhenAll(firestore.CaptureStorageIdentityAsync(),
                firestore.CaptureStorageIdentityAsync());
            foreach (var binding in bindings)
                Assert.IsFalse(binding.IsError, binding.Message);
            var identity = await db.Collection("cosmicStorageIdentity").Document("store").GetSnapshotAsync();
            Assert.AreEqual(identity.GetValue<string>("storeId"), bindings[0].Result.StoreId);
            Assert.AreEqual(bindings[0].Result.BindingFingerprint, bindings[1].Result.BindingFingerprint);
            var reopened = new GoogleCloudOASIS(projectId: project, enableStorage: false, enableFirestore: true, enableBigQuery: false);
            try
            {
                var reopenedActivation = await reopened.ActivateProviderAsync();
                Assert.IsFalse(reopenedActivation.IsError, reopenedActivation.Message);
                var reopenedBinding = await reopened.CaptureStorageIdentityAsync();
                Assert.IsFalse(reopenedBinding.IsError, reopenedBinding.Message);
                Assert.AreEqual(bindings[0].Result.BindingFingerprint, reopenedBinding.Result.BindingFingerprint);
            }
            finally
            {
                await reopened.DeActivateProviderAsync();
            }
            var otherStore = new GoogleCloudOASIS(projectId: project, enableStorage: false,
                enableFirestore: true, enableBigQuery: false, firestoreDatabaseId: "replica-binding-other");
            try
            {
                var otherActivation = await otherStore.ActivateProviderAsync();
                Assert.IsFalse(otherActivation.IsError, otherActivation.Message);
                var otherBinding = await otherStore.CaptureStorageIdentityAsync();
                Assert.IsFalse(otherBinding.IsError, otherBinding.Message);
                Assert.AreNotEqual(bindings[0].Result.BindingFingerprint, otherBinding.Result.BindingFingerprint);
                var source = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid() };
                CosmicConcurrency.SetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
                var original = new CommittedHolonReplicaRequest(Guid.NewGuid(), "owned-sqlite",
                    ProviderType.SQLLiteDBOASIS, Guid.NewGuid(), ConditionalHolonOperation.Save,
                    new[] { source }, saveChildren: false);
                var request = new CommittedHolonReplicaReceiptReadRequest(original, bindings[0].Result);
                var absent = await firestore.ReadCommittedHolonReplicaReceiptAsync(request);
                Assert.IsFalse(absent.IsError, absent.Message);
                Assert.AreEqual(ConditionalHolonReceiptState.Absent, absent.Result.ReceiptState);
                var mismatch = await otherStore.ReadCommittedHolonReplicaReceiptAsync(request);
                Assert.AreEqual("COSMIC_REPLICA_RECEIPT_STORAGE_MISMATCH", mismatch.ErrorCode);

                var receipt = db.Collection("cosmicReplicaReceipts").Document(original.OperationId.ToString("D"));
                var replicated = db.Collection("holons").Document(source.Id.ToString("D"));
                var head = db.Collection("cosmicReplicaHeads").Document(source.Id.ToString("D"));
                var successorId = Guid.NewGuid();
                var deleteId = Guid.NewGuid();
                var rejectedId = Guid.NewGuid();
                var receipts = receipt.Parent;
                try
                {
                    var rejectedReplica = await otherStore.ApplyBoundCommittedHolonReplicaAsync(original, bindings[0].Result);
                    Assert.AreEqual("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", rejectedReplica.ErrorCode);
                    var applied = synchronous ? firestore.ApplyBoundCommittedHolonReplica(original, bindings[0].Result)
                        : await firestore.ApplyBoundCommittedHolonReplicaAsync(original, bindings[0].Result);
                    Assert.IsFalse(applied.IsError, applied.Message);
                    Assert.AreEqual(source.VersionId, applied.Result.Single().VersionId);
                    var native = await replicated.GetSnapshotAsync();
                    Assert.IsTrue(native.Exists);
                    Assert.IsTrue((await head.GetSnapshotAsync()).Exists);
                    var replay = await firestore.ApplyBoundCommittedHolonReplicaAsync(original, bindings[0].Result);
                    Assert.IsFalse(replay.IsError, replay.Message);
                    Assert.AreEqual(native.UpdateTime, (await replicated.GetSnapshotAsync()).UpdateTime);
                    var committed = await firestore.ReadCommittedHolonReplicaReceiptAsync(request);
                    Assert.IsFalse(committed.IsError, committed.Message);
                    Assert.AreEqual(ConditionalHolonReceiptState.Committed, committed.Result.ReceiptState);
                    Assert.AreEqual(original.OperationId, committed.Result.OperationId);
                    Assert.IsFalse(string.IsNullOrEmpty(committed.Result.AcknowledgementDigest));

                    var successor = new Holon { Id = source.Id, Version = 2, VersionId = Guid.NewGuid(),
                        PreviousVersionId = source.VersionId, Name = "Replica successor", IsActive = true };
                    CosmicConcurrency.SetProviderRevisionId(successor, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
                    var wrongPredecessor = successor.PreviousVersionId;
                    successor.PreviousVersionId = Guid.NewGuid();
                    var invalidUpdate = new CommittedHolonReplicaRequest(rejectedId, original.SourceId, original.SourceAuthority,
                        original.ActorId, ConditionalHolonOperation.Save, new[] { successor }, saveChildren: false);
                    var rejectedSuccessor = synchronous ? firestore.ApplyBoundCommittedHolonReplica(invalidUpdate, bindings[0].Result)
                        : await firestore.ApplyBoundCommittedHolonReplicaAsync(invalidUpdate, bindings[0].Result);
                    Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT", rejectedSuccessor.ErrorCode, rejectedSuccessor.Message);
                    Assert.AreEqual(native.UpdateTime, (await replicated.GetSnapshotAsync()).UpdateTime);
                    Assert.IsFalse((await receipts.Document(rejectedId.ToString("D")).GetSnapshotAsync()).Exists);
                    successor.PreviousVersionId = wrongPredecessor;
                    var update = new CommittedHolonReplicaRequest(successorId, original.SourceId, original.SourceAuthority,
                        original.ActorId, ConditionalHolonOperation.Save, new[] { successor }, saveChildren: false);
                    var updated = synchronous ? firestore.ApplyBoundCommittedHolonReplica(update, bindings[0].Result)
                        : await firestore.ApplyBoundCommittedHolonReplicaAsync(update, bindings[0].Result);
                    Assert.IsFalse(updated.IsError, updated.Message);
                    var loaded = await firestore.LoadHolonAsync(source.Id, loadChildren: false);
                    Assert.AreEqual(successor.VersionId, loaded.Result?.VersionId);

                    var missingId = Guid.NewGuid();
                    var headRequest = new HolonReplicaHeadReadRequest(bindings[0].Result, new[] { source.Id, missingId });
                    var liveAudit = await firestore.ReadHolonReplicaHeadsAsync(headRequest);
                    Assert.IsFalse(liveAudit.IsError, liveAudit.Message);
                    Assert.AreEqual(bindings[0].Result.BindingFingerprint, liveAudit.Result.StorageFingerprint);
                    Assert.AreEqual(HolonReplicaHeadReadConsistency.NativeSnapshot, liveAudit.Result.Consistency);
                    Assert.IsTrue(liveAudit.Result.Records[0].HasCurrentRecord);
                    Assert.AreEqual(successor.VersionId, liveAudit.Result.Records[0].Head.VersionId);
                    Assert.AreEqual(missingId, liveAudit.Result.Records[1].HolonId);
                    Assert.IsFalse(liveAudit.Result.Records[1].HasCurrentRecord);
                    Assert.IsNull(liveAudit.Result.Records[1].Head);

                    var tombstone = new Holon { Id = source.Id, Version = 3, VersionId = Guid.NewGuid(),
                        PreviousVersionId = successor.VersionId, Name = successor.Name, IsActive = false,
                        DeletedDate = DateTime.UtcNow, DeletedByAvatarId = original.ActorId };
                    CosmicConcurrency.SetProviderRevisionId(tombstone, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
                    var deletion = new CommittedHolonReplicaRequest(deleteId, original.SourceId, original.SourceAuthority,
                        original.ActorId, ConditionalHolonOperation.HardDelete, new[] { tombstone }, saveChildren: false);
                    var deleted = synchronous ? firestore.ApplyBoundCommittedHolonReplica(deletion, bindings[0].Result)
                        : await firestore.ApplyBoundCommittedHolonReplicaAsync(deletion, bindings[0].Result);
                    Assert.IsTrue(!deleted.IsError && deleted.IsDeleted, deleted.Message);
                    Assert.IsFalse((await replicated.GetSnapshotAsync()).Exists);
                    Assert.IsTrue((await head.GetSnapshotAsync()).GetValue<bool>("hardDeleted"));
                    var nativeHead = await head.GetSnapshotAsync();
                    var deleteAudit = await firestore.ReadHolonReplicaHeadsAsync(headRequest);
                    Assert.IsFalse(deleteAudit.IsError, deleteAudit.Message);
                    Assert.IsFalse(deleteAudit.Result.Records[0].HasCurrentRecord);
                    Assert.IsTrue(deleteAudit.Result.Records[0].Head.HardDeleted);
                    Assert.AreEqual(tombstone.VersionId, deleteAudit.Result.Records[0].Head.VersionId);
                    Assert.AreEqual(nativeHead.UpdateTime, (await head.GetSnapshotAsync()).UpdateTime);
                    var historical = await firestore.ReadCommittedHolonReplicaReceiptAsync(request);
                    Assert.AreEqual(committed.Result.AcknowledgementDigest, historical.Result?.AcknowledgementDigest);
                    var deleteEvidence = await firestore.ReadCommittedHolonReplicaReceiptAsync(
                        new CommittedHolonReplicaReceiptReadRequest(deletion, bindings[0].Result));
                    Assert.IsFalse(deleteEvidence.IsError, deleteEvidence.Message);
                    Assert.AreEqual(ConditionalHolonReceiptState.Committed, deleteEvidence.Result.ReceiptState);

                    await receipt.UpdateAsync("fingerprint", new string('0', 64));
                    var tampered = await firestore.ReadCommittedHolonReplicaReceiptAsync(request);
                    Assert.AreEqual("COSMIC_REPLICA_RECEIPT_UNVERIFIED", tampered.ErrorCode);
                    Assert.IsNull(tampered.Result);
                }
                finally
                {
                    await receipt.DeleteAsync();
                    await receipts.Document(successorId.ToString("D")).DeleteAsync();
                    await receipts.Document(deleteId.ToString("D")).DeleteAsync();
                    await receipts.Document(rejectedId.ToString("D")).DeleteAsync();
                    await replicated.DeleteAsync();
                    await head.DeleteAsync();
                }
            }
            finally
            {
                await otherStore.DeActivateProviderAsync();
            }
            var avatarDocument = db.Collection("avatars").Document(id.ToString());
            await VerifySqliteDeliveryAsync(firestore, db, bindings[0].Result, fixture);
            await VerifyFirestoreDeliveryAsync(firestore, db, bindings[0].Result, fixture, synchronous);
            try
            {
                var avatar = await firestore.SaveAvatarAsync(new Avatar { Id = id, IsActive = true });
                Assert.IsFalse(avatar.IsError, avatar.Message);
                Assert.IsTrue((await avatarDocument.GetSnapshotAsync()).Exists);
            }
            finally
            {
                var storedAvatar = await avatarDocument.GetSnapshotAsync();
                if (storedAvatar.Exists)
                    await avatarDocument.DeleteAsync(Precondition.LastUpdated(storedAvatar.UpdateTime!.Value));
                Assert.IsFalse((await avatarDocument.GetSnapshotAsync()).Exists);
            }
            var saved = await firestore.SaveHolonAsync(new Holon { Id = id, Name = expected, IsActive = true }, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.IsTrue((await document.GetSnapshotAsync()).Exists);

            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = HyperDriveModes.V2,
                StorageProviders = new StorageProviderSettings(),
                SubscriptionConfig = new SubscriptionConfig { PlanType = "Gold" }
            } };
            var manager = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false };
            manager.RegisterProvider(unavailable);
            manager.RegisterProvider(firestore);
            var configured = manager.SetAndReplaceAutoFailOverListForProviders(new[]
            {
                new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),
                new EnumValue<ProviderType>(ProviderType.GoogleCloudOASIS)
            });
            Assert.IsFalse(configured.IsError, configured.Message);
            var router = new OASISHyperDrive(manager);
            StorageOperationRequest ReadSqlite() => new()
            {
                Operation = "LoadHolon", HolonId = id, LoadChildren = false,
                PreferredProvider = ProviderType.SQLLiteDBOASIS
            };
            var rejected = synchronous ? router.RouteRequest<IHolon>(ReadSqlite())
                : await router.RouteRequestAsync<IHolon>(ReadSqlite());
            Assert.IsTrue(rejected.IsError, rejected.Message);

            manager.IsAutoFailOverEnabled = true;
            var recovered = synchronous ? router.RouteRequest<IHolon>(ReadSqlite())
                : await router.RouteRequestAsync<IHolon>(ReadSqlite());
            Assert.IsFalse(recovered.IsError, recovered.Message);
            Assert.IsTrue(recovered.IsLoaded);
            Assert.AreEqual(id, recovered.Result.Id);
            Assert.AreEqual(expected, recovered.Result.Name);
            using var route = JsonDocument.Parse(recovered.MetaData["hyperDriveRoute"]);
            Assert.AreEqual(nameof(ProviderType.SQLLiteDBOASIS), route.RootElement.GetProperty("RequestedProvider").GetString());
            Assert.AreEqual(nameof(ProviderType.GoogleCloudOASIS), route.RootElement.GetProperty("ServingProvider").GetString());
            Assert.IsFalse(File.Exists(path));

            using var staleSqlite = new SqliteProvider($"Data Source={Path.Combine(fixture, $"{id:N}.db")};Pooling=False");
            var sqliteActivation = await staleSqlite.ActivateProviderAsync();
            Assert.IsTrue(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);
            var replica = await staleSqlite.SaveHolonAsync(new Holon { Id = id, Name = expected, IsActive = true }, saveChildren: false);
            Assert.IsFalse(replica.IsError, replica.Message);
            var snapshot = await document.GetSnapshotAsync();
            await document.DeleteAsync(Precondition.LastUpdated(snapshot.UpdateTime!.Value));
            Assert.IsFalse((await document.GetSnapshotAsync()).Exists);
            Assert.AreEqual(id, (await staleSqlite.LoadHolonAsync(id, loadChildren: false)).Result?.Id);

            var healthy = new ProviderManager(null, dna) { IsAutoFailOverEnabled = true };
            healthy.RegisterProvider(firestore);
            healthy.RegisterProvider(staleSqlite);
            Assert.IsFalse(healthy.SetAndReplaceAutoFailOverListForProviders(new[]
            {
                new EnumValue<ProviderType>(ProviderType.GoogleCloudOASIS),
                new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)
            }).IsError);
            var healthyRouter = new OASISHyperDrive(healthy);
            var readFirestore = new StorageOperationRequest
            {
                Operation = "LoadHolon", HolonId = id, LoadChildren = false,
                PreferredProvider = ProviderType.GoogleCloudOASIS
            };
            var missing = synchronous ? healthyRouter.RouteRequest<IHolon>(readFirestore)
                : await healthyRouter.RouteRequestAsync<IHolon>(readFirestore);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, missing.ErrorCode, missing.Message);
            Assert.IsNull(missing.Result);
            Assert.AreEqual(0L, healthy.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
        }
        finally
        {
            try
            {
                var snapshot = await document.GetSnapshotAsync();
                if (snapshot.Exists)
                    await document.DeleteAsync(Precondition.LastUpdated(snapshot.UpdateTime!.Value));
                Assert.IsFalse((await document.GetSnapshotAsync()).Exists);
            }
            finally
            {
                await firestore.DeActivateProviderAsync();
            }
        }
    }

    private static async Task VerifyFirestoreDeliveryAsync(GoogleCloudOASIS firestore, FirestoreDb db,
        CosmicStorageIdentity identity, string fixture, bool synchronous)
    {
        using var sqlite = new SqliteProvider($"Data Source={Path.Combine(fixture, $"{Guid.NewGuid():N}.firestore-source.db")};Pooling=False");
        var activation = await sqlite.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        var captured = await sqlite.CaptureStorageIdentityAsync();
        Assert.IsFalse(captured.IsError, captured.Message);

        var actor = Guid.NewGuid();
        var policy = new HolonReplicationActorGrant(new[] { actor }, DateTime.UtcNow.AddMinutes(10));
        var route = new HolonReplicationRouteBinding(identity, captured.Result, policy.BindingIdentity);
        var enrollment = new HolonReplicationEnrollment(route.SourceId, new[] { route.TargetId }, identity, new[] { route });
        var record = new Holon { Id = Guid.NewGuid(), Name = "Owned Firestore source fixture", IsActive = true };
        var write = new ConditionalHolonWrite(Guid.NewGuid(), actor, identity.Authority,
            new[] { new HolonWriteCondition(record.Id, null) });
        var request = new ConditionalHolonWriteRequest(write, ConditionalHolonOperation.Save, new[] { record }, saveChildren: false);
        var sourceRecord = db.Collection("holons").Document(record.Id.ToString("D"));
        var updateId = Guid.NewGuid();
        var deleteId = Guid.NewGuid();
        try
        {
            var opened = firestore.GetBoundHolonReplicationOutbox(identity);
            Assert.IsFalse(opened.IsError, opened.Message);
            var outbox = opened.Result;
            var wrongIdentity = new CosmicStorageIdentity(identity.Authority, identity.EndpointFingerprint, Guid.NewGuid());
            var wrongRoute = new HolonReplicationRouteBinding(wrongIdentity, captured.Result, policy.BindingIdentity);
            var wrongEnrollment = new HolonReplicationEnrollment(wrongRoute.SourceId, new[] { wrongRoute.TargetId },
                wrongIdentity, new[] { wrongRoute });
            var wrongWrite = await firestore.ExecuteReplicatingHolonWriteAsync(request, wrongEnrollment);
            Assert.AreEqual("COSMIC_SOURCE_STORAGE_BINDING_MISMATCH", wrongWrite.ErrorCode);
            Assert.IsFalse((await sourceRecord.GetSnapshotAsync()).Exists);
            var wrongOutbox = firestore.GetBoundHolonReplicationOutbox(wrongIdentity);
            Assert.IsFalse(wrongOutbox.IsError, wrongOutbox.Message);
            var wrongInventory = await wrongOutbox.Result.ReadRouteHeadsAsync();
            Assert.AreEqual("COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH", wrongInventory.ErrorCode);
            Assert.IsNull(wrongInventory.Result);

            var saved = synchronous ? firestore.ExecuteReplicatingHolonWrite(request, enrollment)
                : await firestore.ExecuteReplicatingHolonWriteAsync(request, enrollment);
            Assert.IsTrue(!saved.IsError && saved.IsSaved, saved.Message);
            var committed = saved.Result.Single();
            var page = await outbox.ReadRouteHeadsAsync();
            Assert.IsFalse(page.IsError, page.Message);
            var pending = page.Result.Heads.Single(head => head.TargetId == route.TargetId);
            Assert.AreEqual(write.OperationId, pending.OperationId);
            Assert.AreEqual(HolonDeliveryState.Pending, pending.State);
            var replay = await firestore.ExecuteReplicatingHolonWriteAsync(request, enrollment);
            Assert.IsFalse(replay.IsError, replay.Message);
            Assert.AreEqual(committed.VersionId, replay.Result.Single().VersionId);

            var wrongReplay = await firestore.ExecuteReplicatingHolonWriteAsync(request, wrongEnrollment);
            Assert.AreEqual("COSMIC_SOURCE_STORAGE_BINDING_MISMATCH", wrongReplay.ErrorCode);

            var expired = await outbox.ClaimNextAsync(route.TargetId, "expired-firestore-worker", TimeSpan.FromTicks(1));
            Assert.IsFalse(expired.IsError, expired.Message);
            Assert.AreEqual(pending.Sequence, expired.Result?.Sequence);
            var staleRenewal = await outbox.RenewAsync(expired.Result, TimeSpan.FromMinutes(1));
            Assert.IsFalse(staleRenewal.IsError, staleRenewal.Message);
            Assert.IsFalse(staleRenewal.Result);
            var staleCompletion = await outbox.FinishAsync(expired.Result, HolonDeliveryOutcome.Completed);
            Assert.IsFalse(staleCompletion.IsError, staleCompletion.Message);
            Assert.IsFalse(staleCompletion.Result);

            var target = new CommittedHolonReplicaTarget(route.TargetId, sqlite, captured.Result.Authority,
                route.SourceId, identity.Authority, policy, "firestore-source", Guid.NewGuid(), policy.BindingIdentity, captured.Result);
            var dispatcher = new HolonOutboxDispatcher(outbox, new[] { target }, "firestore-source");
            var delivered = await dispatcher.DispatchOnceAsync(route.TargetId, maximumCount: 2);
            Assert.IsFalse(delivered.IsError, delivered.Message);
            Assert.AreEqual(1, delivered.Result.CompletedCount);
            var loaded = await sqlite.LoadHolonAsync(record.Id, loadChildren: false);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(committed.VersionId, loaded.Result?.VersionId);
            var finished = await outbox.InspectAsync(pending.Sequence);
            Assert.IsFalse(finished.IsError, finished.Message);
            Assert.AreEqual(HolonDeliveryState.Completed, finished.Result.State);
            Assert.AreEqual(2, finished.Result.AttemptCount);
            var next = await outbox.PeekNextAsync(route.TargetId);
            Assert.IsFalse(next.IsError, next.Message);
            Assert.IsNull(next.Result);

            var update = new ConditionalHolonWrite(updateId, actor, identity.Authority,
                new[] { new HolonWriteCondition(record.Id, CosmicConcurrency.GetProviderRevisionId(committed, identity.Authority)) });
            var successor = new Holon { Id = record.Id, Name = "Owned Firestore successor", IsActive = true };
            var updated = await firestore.ExecuteReplicatingHolonWriteAsync(
                new ConditionalHolonWriteRequest(update, ConditionalHolonOperation.Save, new[] { successor }, saveChildren: false), enrollment);
            Assert.IsFalse(updated.IsError, updated.Message);
            var revision = updated.Result.Single();
            Assert.AreEqual(committed.VersionId, revision.PreviousVersionId);

            var delete = new ConditionalHolonWrite(deleteId, actor, identity.Authority,
                new[] { new HolonWriteCondition(record.Id, CosmicConcurrency.GetProviderRevisionId(revision, identity.Authority)) });
            var deleted = await firestore.ExecuteReplicatingHolonWriteAsync(
                new ConditionalHolonWriteRequest(delete, ConditionalHolonOperation.HardDelete, new[] { revision }, saveChildren: false), enrollment);
            Assert.IsTrue(!deleted.IsError && deleted.IsDeleted, deleted.Message);
            Assert.IsFalse((await sourceRecord.GetSnapshotAsync()).Exists);

            var updateDelivery = await dispatcher.DispatchOnceAsync(route.TargetId, maximumCount: 1);
            Assert.IsFalse(updateDelivery.IsError, updateDelivery.Message);
            Assert.AreEqual(1, updateDelivery.Result.CompletedCount);
            Assert.AreEqual(revision.VersionId, (await sqlite.LoadHolonAsync(record.Id, loadChildren: false)).Result?.VersionId);

            var deleteDelivery = await dispatcher.DispatchOnceAsync(route.TargetId, maximumCount: 1);
            Assert.IsFalse(deleteDelivery.IsError, deleteDelivery.Message);
            Assert.AreEqual(1, deleteDelivery.Result.CompletedCount);
            var heads = await sqlite.ReadHolonReplicaHeadsAsync(new HolonReplicaHeadReadRequest(captured.Result, new[] { record.Id }));
            Assert.IsFalse(heads.IsError, heads.Message);
            var tombstone = heads.Result.Records.Single();
            Assert.IsFalse(tombstone.HasCurrentRecord);
            Assert.IsTrue(tombstone.Head.HardDeleted);
            Assert.AreEqual(deleted.Result.Single().VersionId, tombstone.Head.VersionId);

        }
        finally
        {
            foreach (var operationId in new[] { write.OperationId, updateId, deleteId })
            {
                foreach (var delivery in (await db.Collection("cosmicOutboundDeliveries")
                    .WhereEqualTo("operationId", operationId.ToString("D")).GetSnapshotAsync()).Documents)
                    await delivery.Reference.DeleteAsync();
                var key = $"GoogleCloudOASIS:{operationId:N}";
                await db.Collection("cosmicWriteReceipts").Document(key).DeleteAsync();
                await db.Collection("cosmicOperationFences").Document(key).DeleteAsync();
            }
            await sourceRecord.DeleteAsync();
        }
    }

    private static async Task VerifySqliteDeliveryAsync(GoogleCloudOASIS firestore, FirestoreDb db,
        CosmicStorageIdentity identity, string fixture)
    {
        var actor = Guid.NewGuid();
        var policy = new HolonReplicationActorGrant(new[] { actor }, DateTime.UtcNow.AddMinutes(10));
        var connection = $"Data Source={Path.Combine(fixture, $"{Guid.NewGuid():N}.outbox.db")};Pooling=False";
        using var sqlite = new SqliteProvider(connection);
        var activated = await sqlite.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        var captured = await sqlite.CaptureStorageIdentityAsync();
        Assert.IsFalse(captured.IsError, captured.Message);
        var route = new HolonReplicationRouteBinding(captured.Result, identity, policy.BindingIdentity);
        var enrollment = new HolonReplicationEnrollment(route.SourceId, new[] { route.TargetId }, captured.Result, new[] { route });
        var record = new Holon { Id = Guid.NewGuid(), Name = "Owned Firestore dispatcher fixture" };
        var write = new ConditionalHolonWrite(Guid.NewGuid(), actor, captured.Result.Authority,
            new[] { new HolonWriteCondition(record.Id, null) });
        var document = db.Collection("holons").Document(record.Id.ToString("D"));
        var receipt = db.Collection("cosmicReplicaReceipts").Document(write.OperationId.ToString("D"));
        var head = db.Collection("cosmicReplicaHeads").Document(record.Id.ToString("D"));
        try
        {
            var saved = await sqlite.ExecuteReplicatingHolonWriteAsync(
                new ConditionalHolonWriteRequest(write, ConditionalHolonOperation.Save, new[] { record }, saveChildren: false), enrollment);
            Assert.IsTrue(!saved.IsError && saved.IsSaved, saved.Message);
            var committed = saved.Result.Single();
            var target = new CommittedHolonReplicaTarget(route.TargetId, firestore, identity.Authority, route.SourceId,
                route.SourceStorage.Authority, policy, "firestore-fixture", Guid.NewGuid(), policy.BindingIdentity, identity);
            var worker = new HolonOutboxDispatcher(new SqliteHolonReplicationOutbox(connection), new[] { target }, "firestore-fixture");
            var delivered = await worker.DispatchOnceAsync(route.TargetId, maximumCount: 1);
            Assert.IsFalse(delivered.IsError, delivered.Message);
            Assert.AreEqual(1, delivered.Result.CompletedCount);
            var native = await document.GetSnapshotAsync();
            Assert.IsTrue(native.Exists);
            var loaded = await firestore.LoadHolonAsync(record.Id, loadChildren: false);
            Assert.AreEqual(committed.VersionId, loaded.Result?.VersionId);
            var request = new CommittedHolonReplicaRequest(write.OperationId, route.SourceId, route.SourceStorage.Authority,
                actor, ConditionalHolonOperation.Save, new[] { committed }, false, true, 0, new[] { route });
            var evidence = await firestore.ReadCommittedHolonReplicaReceiptAsync(new(request, identity));
            Assert.IsFalse(evidence.IsError, evidence.Message);
            Assert.AreEqual(ConditionalHolonReceiptState.Committed, evidence.Result.ReceiptState);
            var restarted = new HolonOutboxDispatcher(new SqliteHolonReplicationOutbox(connection),
                new[] { target }, "firestore-fixture-restarted");
            var replay = await restarted.DispatchOnceAsync(route.TargetId, maximumCount: 1);
            Assert.IsFalse(replay.IsError, replay.Message);
            Assert.AreEqual(0, replay.Result.CompletedCount);
            Assert.AreEqual(native.UpdateTime, (await document.GetSnapshotAsync()).UpdateTime);
        }
        finally
        {
            await sqlite.DeActivateProviderAsync();
            await document.DeleteAsync();
            await receipt.DeleteAsync();
            await head.DeleteAsync();
        }
    }
}
