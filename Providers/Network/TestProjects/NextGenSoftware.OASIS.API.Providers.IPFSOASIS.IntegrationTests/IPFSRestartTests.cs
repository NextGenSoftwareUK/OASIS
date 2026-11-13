using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IntegrationTests;

public sealed class IPFSRestartTests
{
    private static readonly Guid RecordId = new("90644865-2be6-4044-bdfb-3e6a394f5b22");

    [Fact]
    public async Task PersistsCurrentAndHistoricalHolonAcrossDaemonRestart()
    {
        var endpoint = Environment.GetEnvironmentVariable("IPFSOASIS_TEST_API");
        var dnaPath = Environment.GetEnvironmentVariable("IPFSOASIS_RESTART_DNA");
        var phase = Environment.GetEnvironmentVariable("IPFSOASIS_RESTART_PHASE");
        Assert.False(string.IsNullOrWhiteSpace(endpoint));
        Assert.False(string.IsNullOrWhiteSpace(dnaPath));
        Assert.True(phase is "write" or "read", "Select the disposable Kubo restart fixture phase.");

        var identityPath = dnaPath + ".identity";
        OASISDNA dna;
        if (phase == "write")
        {
            Assert.False(File.Exists(dnaPath));
            dna = new OASISDNA();
            dna.OASIS.StorageProviders = new StorageProviderSettings
            {
                IPFSOASIS = new IPFSOASISSettings { ConnectionString = endpoint }
            };
        }
        else
        {
            Assert.True(File.Exists(dnaPath));
            var loaded = OASISDNAManager.LoadDNA(dnaPath);
            Assert.False(loaded.IsError, loaded.Message);
            dna = Assert.IsType<OASISDNA>(loaded.Result);
            Assert.Equal(endpoint, dna.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
            Assert.False(string.IsNullOrWhiteSpace(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress));
        }

        var provider = new IPFSOASIS(dna, dnaPath);
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.True(active.Result && !active.IsError, active.Message);
            if (phase == "write")
            {
                var unbound = await provider.CaptureStorageIdentityAsync();
                Assert.True(unbound.IsError);
                Assert.Null(unbound.Result);
                Assert.False(File.Exists(dnaPath));

                var expected = new CosmicStorageIdentity(ProviderType.IPFSOASIS, new string('A', 64), Guid.NewGuid());
                var original = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(), HolonType = HolonType.Holon };
                CosmicConcurrency.SetProviderRevisionId(original, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
                var delivery = new CommittedHolonReplicaRequest(Guid.NewGuid(), "unpublished-store-source",
                    ProviderType.SQLLiteDBOASIS, RecordId, ConditionalHolonOperation.Save, new[] { original }, saveChildren: false);
                var unavailable = await provider.ApplyBoundCommittedHolonReplicaAsync(delivery, expected);
                Assert.True(unavailable.IsError);
                Assert.Equal("COSMIC_REPLICA_STORAGE_BINDING_INVALID", unavailable.ErrorCode);
                Assert.False(File.Exists(dnaPath));

                var holon = new Holon { Id = RecordId, Name = "before update", Version = 1, HolonType = HolonType.Holon };
                var saved = await provider.SaveHolonAsync(holon, saveChildren: false);
                Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
                var identity = await provider.CaptureStorageIdentityAsync();
                Assert.False(identity.IsError, identity.Message);
                await File.WriteAllTextAsync(identityPath, identity.Result.BindingFingerprint);
                var publishedDna = await File.ReadAllTextAsync(dnaPath);
                var invalidCatalogue = await provider.LoadLookupToJson();
                invalidCatalogue.Add("invalid-cid", new HolonResume());
                await Assert.ThrowsAsync<InvalidDataException>(() => provider.SaveLookupToFile(invalidCatalogue));
                Assert.Equal(publishedDna, await File.ReadAllTextAsync(dnaPath));
                await VerifyReplicaAsync(provider, identity.Result, dnaPath, replay: false);
                holon.Name = "after update";
                holon.Version = 2;
                saved = await provider.SaveHolonAsync(holon, saveChildren: false);
                Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
                Assert.True(File.Exists(dnaPath));
            }
            else
            {
                var identity = await provider.CaptureStorageIdentityAsync();
                Assert.False(identity.IsError, identity.Message);
                Assert.Equal(await File.ReadAllTextAsync(identityPath), identity.Result.BindingFingerprint);
                await VerifyReplicaAsync(provider, identity.Result, dnaPath, replay: true);
                var current = await provider.LoadHolonAsync(RecordId, loadChildren: false);
                Assert.True(current.IsLoaded && !current.IsError, current.Message);
                Assert.Equal("after update", current.Result.Name);
                Assert.Equal(2, current.Result.Version);
                var history = await provider.LoadHolonAsync(RecordId, loadChildren: false, version: 1);
                Assert.True(history.IsLoaded && !history.IsError, history.Message);
                Assert.Equal("before update", history.Result.Name);
                Assert.Equal(1, history.Result.Version);
            }
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    private static async Task VerifyReplicaAsync(IPFSOASIS provider, CosmicStorageIdentity identity, string dnaPath, bool replay)
    {
        var replica = new Holon
        {
            Id = new Guid("90644865-2be6-4044-bdfb-3e6a394f5b23"),
            VersionId = new Guid("90644865-2be6-4044-bdfb-3e6a394f5b24"),
            Name = "replicated from SQLite",
            Version = 1,
            HolonType = HolonType.Holon
        };
        CosmicConcurrency.SetProviderRevisionId(replica, ProviderType.SQLLiteDBOASIS,
            new Guid("90644865-2be6-4044-bdfb-3e6a394f5b25"));
        var request = new CommittedHolonReplicaRequest(
            new Guid("90644865-2be6-4044-bdfb-3e6a394f5b26"), "restart-sqlite-source",
            ProviderType.SQLLiteDBOASIS, RecordId, ConditionalHolonOperation.Save, new[] { replica }, saveChildren: false);
        var persistedBefore = await File.ReadAllTextAsync(dnaPath);
        if (!replay)
        {
            using var lease = new FileStream(dnaPath + ".ipfs.lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => provider.ApplyBoundCommittedHolonReplicaAsync(request, identity, cancellation.Token));
            Assert.Equal(persistedBefore, await File.ReadAllTextAsync(dnaPath));
        }
        using var cancelledCapture = new CancellationTokenSource();
        cancelledCapture.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.CaptureStorageIdentityAsync(cancelledCapture.Token));
        Assert.Equal(persistedBefore, await File.ReadAllTextAsync(dnaPath));
        var wrongStore = new CosmicStorageIdentity(identity.Authority, identity.EndpointFingerprint, Guid.NewGuid());
        var rejected = await provider.ApplyBoundCommittedHolonReplicaAsync(request, wrongStore);
        Assert.True(rejected.IsError);
        Assert.Equal("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", rejected.ErrorCode);
        Assert.Equal(persistedBefore, await File.ReadAllTextAsync(dnaPath));

        var observation = new CommittedHolonReplicaReceiptReadRequest(request, identity);
        var receipt = await provider.ReadCommittedHolonReplicaReceiptAsync(observation);
        Assert.True(receipt.IsLoaded && !receipt.IsError, receipt.Message);
        Assert.Equal(replay ? ConditionalHolonReceiptState.Committed : ConditionalHolonReceiptState.Absent,
            receipt.Result.ReceiptState);
        Assert.Equal(persistedBefore, await File.ReadAllTextAsync(dnaPath));

        var before = await provider.LoadHolonAsync(replica.Id, loadChildren: false);
        if (replay)
            Assert.True(before.IsLoaded && !before.IsError, before.Message);
        var applied = await provider.ApplyBoundCommittedHolonReplicaAsync(request, identity);
        Assert.True(applied.IsSaved && !applied.IsError, applied.Message);
        var committed = await provider.ReadCommittedHolonReplicaReceiptAsync(observation);
        Assert.True(committed.IsLoaded && !committed.IsError, committed.Message);
        Assert.Equal(ConditionalHolonReceiptState.Committed, committed.Result.ReceiptState);
        Assert.NotNull(committed.Result.AcknowledgementDigest);
        var acknowledgement = Assert.Single(applied.Result);
        var current = await provider.LoadHolonAsync(replica.Id, loadChildren: false);
        Assert.True(current.IsLoaded && !current.IsError, current.Message);
        Assert.Equal(replica.VersionId, current.Result.VersionId);
        var revision = CosmicConcurrency.GetProviderRevisionId(acknowledgement, ProviderType.IPFSOASIS);
        Assert.NotNull(revision);
        Assert.Equal(revision, CosmicConcurrency.GetProviderRevisionId(current.Result, ProviderType.IPFSOASIS));
        if (replay)
        {
            Assert.Equal(receipt.Result.AcknowledgementDigest, committed.Result.AcknowledgementDigest);
            Assert.Equal(revision, CosmicConcurrency.GetProviderRevisionId(before.Result, ProviderType.IPFSOASIS));
            Assert.Equal(persistedBefore, await File.ReadAllTextAsync(dnaPath));

            var originalName = replica.Name;
            replica.Name = "different intent under the same operation";
            var conflicting = await provider.ApplyBoundCommittedHolonReplicaAsync(request, identity);
            Assert.True(conflicting.IsError);
            Assert.Equal("COSMIC_REPLICA_ACKNOWLEDGEMENT_INVALID", conflicting.ErrorCode);
            var conflictingReceipt = await provider.ReadCommittedHolonReplicaReceiptAsync(observation);
            Assert.True(conflictingReceipt.IsError);
            Assert.Equal(persistedBefore, await File.ReadAllTextAsync(dnaPath));
            replica.Name = originalName;

            await VerifyReplicaSuccessorAsync(provider, identity, replica, dnaPath);
            var advancedPointer = await File.ReadAllTextAsync(dnaPath);
            var historicalReplay = await provider.ApplyBoundCommittedHolonReplicaAsync(request, identity);
            Assert.True(historicalReplay.IsSaved && !historicalReplay.IsError, historicalReplay.Message);
            Assert.Equal(replica.VersionId, Assert.Single(historicalReplay.Result).VersionId);
            Assert.Equal(advancedPointer, await File.ReadAllTextAsync(dnaPath));
            await VerifyHostedReplicaTargetAsync(provider, identity, dnaPath);
            var successorRead = await provider.LoadHolonAsync(replica.Id, loadChildren: false);
            Assert.True(successorRead.IsLoaded && !successorRead.IsError, successorRead.Message);
            var previousVersion = successorRead.Result.VersionId;
            successorRead.Result.Name = "local divergence";
            var localWrite = await provider.SaveHolonAsync(successorRead.Result, saveChildren: false);
            Assert.True(localWrite.IsSaved && !localWrite.IsError, localWrite.Message);
            var divergentPointer = await File.ReadAllTextAsync(dnaPath);
            var next = new Holon { Id = replica.Id, Version = 3, VersionId = Guid.NewGuid(),
                PreviousVersionId = previousVersion, HolonType = HolonType.Holon };
            CosmicConcurrency.SetProviderRevisionId(next, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            var nextRequest = new CommittedHolonReplicaRequest(Guid.NewGuid(), "restart-sqlite-source",
                ProviderType.SQLLiteDBOASIS, RecordId, ConditionalHolonOperation.Save, new[] { next }, saveChildren: false);
            var conflict = await provider.ApplyBoundCommittedHolonReplicaAsync(nextRequest, identity);
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", conflict.ErrorCode);
            Assert.Equal(divergentPointer, await File.ReadAllTextAsync(dnaPath));
        }
    }

    private static async Task VerifyReplicaSuccessorAsync(IPFSOASIS provider, CosmicStorageIdentity identity,
        Holon original, string dnaPath)
    {
        var successor = new Holon
        {
            Id = original.Id,
            Version = 2,
            VersionId = Guid.NewGuid(),
            PreviousVersionId = Guid.NewGuid(),
            Name = "updated replica",
            HolonType = HolonType.Holon
        };
        CosmicConcurrency.SetProviderRevisionId(successor, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        var operationId = Guid.NewGuid();
        CommittedHolonReplicaRequest Request() => new(operationId, "restart-sqlite-source",
            ProviderType.SQLLiteDBOASIS, RecordId, ConditionalHolonOperation.Save, new[] { successor }, saveChildren: false);

        var persistedBefore = await File.ReadAllTextAsync(dnaPath);
        var rejected = await provider.ApplyBoundCommittedHolonReplicaAsync(Request(), identity);
        Assert.True(rejected.IsError);
        Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", rejected.ErrorCode);
        Assert.Equal(persistedBefore, await File.ReadAllTextAsync(dnaPath));

        successor.PreviousVersionId = original.VersionId;
        var accepted = await provider.ApplyBoundCommittedHolonReplicaAsync(Request(), identity);
        Assert.True(accepted.IsSaved && !accepted.IsError, accepted.Message);
        var current = await provider.LoadHolonAsync(original.Id, loadChildren: false);
        Assert.True(current.IsLoaded && !current.IsError, current.Message);
        Assert.Equal(successor.VersionId, current.Result.VersionId);
        var currentPointer = await File.ReadAllTextAsync(dnaPath);
        var audit = await provider.ReadHolonReplicaHeadsAsync(new(identity, new[] { original.Id }));
        Assert.True(audit.IsLoaded && !audit.IsError, audit.Message);
        var head = Assert.Single(audit.Result.Records);
        Assert.True(head.HasCurrentRecord);
        Assert.False(head.Head.HardDeleted);
        Assert.Equal(successor.VersionId, head.Head.VersionId);
        Assert.Equal(original.VersionId, head.Head.PreviousVersionId);
        Assert.Equal(CosmicConcurrency.GetProviderRevisionId(current.Result, ProviderType.IPFSOASIS), head.Head.ProviderRevisionId);
        Assert.Equal(currentPointer, await File.ReadAllTextAsync(dnaPath));
        var prior = await provider.LoadHolonAsync(original.Id, loadChildren: false, version: 1);
        Assert.True(prior.IsLoaded && !prior.IsError, prior.Message);
        Assert.Equal(original.VersionId, prior.Result.VersionId);
    }

    private static async Task VerifyHostedReplicaTargetAsync(IPFSOASIS provider, CosmicStorageIdentity identity, string dnaPath)
    {
        var actor = Guid.NewGuid();
        var policy = new HolonReplicationActorGrant(new[] { actor }, DateTime.UtcNow.AddMinutes(10));
        var databasePath = dnaPath + ".source.sqlite";
        Assert.False(File.Exists(databasePath));
        var connection = $"Data Source={databasePath};Pooling=False";
        var replica = new Holon
        {
            Id = Guid.NewGuid(),
            Name = "hosted replica target",
            HolonType = HolonType.Holon
        };
        var child = new Holon { Id = Guid.NewGuid(), Name = "replicated child", HolonType = HolonType.Holon,
            ParentHolonId = replica.Id };
        replica.Children = new List<IHolon> { child };
        HolonReplicationRouteBinding route;
        IHolon committed;
        using (var sqlite = new SqliteProvider(connection))
        {
            var activated = await sqlite.ActivateProviderAsync();
            Assert.True(activated.Result && !activated.IsError, activated.Message);
            var captured = await sqlite.CaptureStorageIdentityAsync();
            Assert.False(captured.IsError, captured.Message);
            var sourceStorage = captured.Result;
            route = new HolonReplicationRouteBinding(sourceStorage, identity, policy.BindingIdentity);
            var enrollment = new HolonReplicationEnrollment(route.SourceId, new[] { route.TargetId }, sourceStorage, new[] { route });
            var write = new ConditionalHolonWrite(Guid.NewGuid(), actor, sourceStorage.Authority,
                new[] { new HolonWriteCondition(replica.Id, null), new HolonWriteCondition(child.Id, null) });
            var saved = await sqlite.ExecuteReplicatingHolonWriteAsync(
                new ConditionalHolonWriteRequest(write, ConditionalHolonOperation.Save, new[] { replica }, saveChildren: true), enrollment);
            Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
            committed = Assert.Single(saved.Result);
        }

        var target = new CommittedHolonReplicaTarget(route.TargetId, provider, identity.Authority, route.SourceId,
            route.SourceStorage.Authority, policy, "ipfs-restart-fixture", Guid.NewGuid(), policy.BindingIdentity, identity);
        var worker = new HolonOutboxDispatcher(new SqliteHolonReplicationOutbox(connection),
            new[] { target }, "ipfs-restart-fixture");
        var result = await worker.DispatchOnceAsync(route.TargetId, maximumCount: 1);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(1, result.Result.CompletedCount);
        var copied = await provider.LoadHolonAsync(replica.Id, loadChildren: true, loadChildrenFromProvider: true);
        Assert.True(copied.IsLoaded && !copied.IsError, copied.Message);
        Assert.Equal(committed.VersionId, copied.Result.VersionId);
        var copiedChild = Assert.Single(copied.Result.Children);
        Assert.Equal(child.Id, copiedChild.Id);
        Assert.Equal(replica.Id, copiedChild.ParentHolonId);
        Assert.Equal(Assert.Single(committed.Children).VersionId, copiedChild.VersionId);

        var persistedAfterDelivery = await File.ReadAllTextAsync(dnaPath);
        var resumedWorker = new HolonOutboxDispatcher(new SqliteHolonReplicationOutbox(connection),
            new[] { target }, "ipfs-restart-fixture-resumed");
        var resumed = await resumedWorker.DispatchOnceAsync(route.TargetId, maximumCount: 1);
        Assert.False(resumed.IsError, resumed.Message);
        Assert.Equal(0, resumed.Result.CompletedCount);
        Assert.Equal(persistedAfterDelivery, await File.ReadAllTextAsync(dnaPath));

        foreach (var operation in new[] { ConditionalHolonOperation.SoftDelete, ConditionalHolonOperation.HardDelete })
        {
            using var sqlite = new SqliteProvider(connection);
            var activated = await sqlite.ActivateProviderAsync();
            Assert.True(activated.Result && !activated.IsError, activated.Message);
            var expectedRevision = CosmicConcurrency.GetProviderRevisionId(committed, route.SourceStorage.Authority);
            Assert.NotNull(expectedRevision);
            var write = new ConditionalHolonWrite(Guid.NewGuid(), actor, route.SourceStorage.Authority,
                new[] { new HolonWriteCondition(replica.Id, expectedRevision) });
            var enrollment = new HolonReplicationEnrollment(route.SourceId, new[] { route.TargetId }, route.SourceStorage,
                new[] { route });
            var deleted = await sqlite.ExecuteReplicatingHolonWriteAsync(
                new ConditionalHolonWriteRequest(write, operation, new[] { committed }, saveChildren: false), enrollment);
            Assert.False(deleted.IsError, deleted.Message);
            committed = Assert.Single(deleted.Result);
            Assert.True(committed.IsDeleted);
            var delivery = await resumedWorker.DispatchOnceAsync(route.TargetId, maximumCount: 1);
            Assert.False(delivery.IsError, delivery.Message);
            Assert.Equal(1, delivery.Result.CompletedCount);
            var request = new CommittedHolonReplicaRequest(write.OperationId, route.SourceId, route.SourceStorage.Authority,
                actor, operation, new[] { committed }, false, true, 0, new[] { route });
            var receipt = await provider.ReadCommittedHolonReplicaReceiptAsync(new(request, identity));
            Assert.True(receipt.IsLoaded && !receipt.IsError, receipt.Message);
            Assert.Equal(ConditionalHolonReceiptState.Committed, receipt.Result.ReceiptState);
            var beforeReplay = await File.ReadAllTextAsync(dnaPath);
            var replay = await provider.ApplyBoundCommittedHolonReplicaAsync(request, identity);
            Assert.True(replay.IsDeleted && !replay.IsError, replay.Message);
            Assert.Equal(beforeReplay, await File.ReadAllTextAsync(dnaPath));
        }
        var removed = await provider.LoadHolonAsync(replica.Id, loadChildren: false);
        Assert.False(removed.IsLoaded);
        Assert.Null(removed.Result);
        var beforeAudit = await File.ReadAllTextAsync(dnaPath);
        var missingId = Guid.NewGuid();
        var audit = await provider.ReadHolonReplicaHeadsAsync(new(identity, new[] { replica.Id, missingId }));
        Assert.True(audit.IsLoaded && !audit.IsError, audit.Message);
        Assert.Equal(HolonReplicaHeadReadConsistency.NativeSnapshot, audit.Result.Consistency);
        Assert.Equal(identity.BindingFingerprint, audit.Result.StorageFingerprint);
        var deletedHead = audit.Result.Records.Single(row => row.HolonId == replica.Id);
        Assert.False(deletedHead.HasCurrentRecord);
        Assert.True(deletedHead.Head.HardDeleted);
        Assert.Equal(committed.VersionId, deletedHead.Head.VersionId);
        var missing = audit.Result.Records.Single(row => row.HolonId == missingId);
        Assert.False(missing.HasCurrentRecord);
        Assert.Null(missing.Head);
        Assert.Equal(beforeAudit, await File.ReadAllTextAsync(dnaPath));
    }
}
