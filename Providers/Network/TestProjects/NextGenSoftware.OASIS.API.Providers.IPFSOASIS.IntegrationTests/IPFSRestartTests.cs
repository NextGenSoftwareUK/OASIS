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

    private static readonly Guid ConditionalOperationId = new("90644865-2be6-4044-bdfb-3e6a394f5b30");

    private static Holon CreateConditionalFixtureRecord() => new()
    {
        Id = new Guid("90644865-2be6-4044-bdfb-3e6a394f5b31"),
        Name = "conditional IPFS create",
        HolonType = HolonType.Holon
    };

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
        var conditionalVersionPath = dnaPath + ".conditional-version";
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
                var oversizedCatalogue = await provider.LoadLookupToJson();
                var alphabet = Enumerable.Range(32, 95).Select(value => (char)value)
                    .Where(value => value != (char)34 && value != (char)92).ToArray();
                var random = System.Security.Cryptography.RandomNumberGenerator.GetBytes(4_150_000);
                Assert.Single(oversizedCatalogue).Value.login = string.Create(random.Length, (random, alphabet),
                    static (characters, state) =>
                    {
                        for (var i = 0; i < characters.Length; i++)
                            characters[i] = state.alphabet[state.random[i] % state.alphabet.Length];
                    });
                var sizeError = await Assert.ThrowsAsync<InvalidDataException>(
                    () => provider.SaveLookupToFile(oversizedCatalogue));
                Assert.Contains("exceeds the native query and replica read limit", sizeError.Message);
                Assert.Equal(publishedDna, await File.ReadAllTextAsync(dnaPath));
                var conditionalRecord = CreateConditionalFixtureRecord();
                var condition = new ConditionalHolonWrite(ConditionalOperationId, RecordId, ProviderType.IPFSOASIS,
                    new[] { new HolonWriteCondition(conditionalRecord.Id, null) });
                var conditionalRequest = new ConditionalHolonWriteRequest(condition, ConditionalHolonOperation.Save,
                    new[] { conditionalRecord }, saveChildren: false);
                var conditional = await provider.ExecuteConditionalHolonWriteAsync(conditionalRequest);
                Assert.True(conditional.IsSaved && !conditional.IsError, conditional.Message);
                await File.WriteAllTextAsync(conditionalVersionPath, Assert.Single(conditional.Result).VersionId.ToString("D"));
                var conditionalPointer = await File.ReadAllTextAsync(dnaPath);
                var replayed = provider.ExecuteConditionalHolonWrite(conditionalRequest);
                Assert.True(replayed.IsSaved && !replayed.IsError, replayed.Message);
                Assert.Equal(Assert.Single(conditional.Result).VersionId, Assert.Single(replayed.Result).VersionId);
                Assert.Equal(conditionalPointer, await File.ReadAllTextAsync(dnaPath));
                var staleCondition = new ConditionalHolonWrite(Guid.NewGuid(), RecordId, ProviderType.IPFSOASIS,
                    new[] { new HolonWriteCondition(conditionalRecord.Id, Guid.NewGuid()) });
                var staleRequest = new ConditionalHolonWriteRequest(staleCondition, ConditionalHolonOperation.Save,
                    new[] { conditionalRecord }, saveChildren: false);
                var stale = await provider.ExecuteConditionalHolonWriteAsync(staleRequest);
                Assert.True(stale.IsError);
                Assert.Equal(CosmicConcurrencyException.Code, stale.ErrorCode);
                Assert.Equal(conditionalPointer, await File.ReadAllTextAsync(dnaPath));
                var priorRevision = CosmicConcurrency.GetProviderRevisionId(Assert.Single(conditional.Result), ProviderType.IPFSOASIS);
                Assert.NotNull(priorRevision);
                var ordinaryRecord = Assert.Single(conditional.Result);
                ordinaryRecord.Name = "ordinary edit before conditional retry";
                ordinaryRecord.PreviousVersionId = ordinaryRecord.VersionId;
                ordinaryRecord.VersionId = Guid.NewGuid();
                ordinaryRecord.Version++;
                var ordinary = await provider.SaveHolonAsync(ordinaryRecord, saveChildren: false);
                Assert.True(ordinary.IsSaved && !ordinary.IsError, ordinary.Message);
                var newRevision = CosmicConcurrency.GetProviderRevisionId(ordinary.Result, ProviderType.IPFSOASIS);
                Assert.NotNull(newRevision);
                Assert.NotEqual(priorRevision, newRevision);
                var loadedAfterEdit = await provider.LoadHolonAsync(conditionalRecord.Id, loadChildren: false);
                Assert.True(loadedAfterEdit.IsLoaded && !loadedAfterEdit.IsError, loadedAfterEdit.Message);
                Assert.Equal(newRevision, CosmicConcurrency.GetProviderRevisionId(loadedAfterEdit.Result, ProviderType.IPFSOASIS));
                var editedPointer = await File.ReadAllTextAsync(dnaPath);
                var interveningCondition = new ConditionalHolonWrite(Guid.NewGuid(), RecordId, ProviderType.IPFSOASIS,
                    new[] { new HolonWriteCondition(conditionalRecord.Id, priorRevision) });
                var interveningRequest = new ConditionalHolonWriteRequest(interveningCondition, ConditionalHolonOperation.Save,
                    new[] { conditionalRecord }, saveChildren: false);
                var intervening = await provider.ExecuteConditionalHolonWriteAsync(interveningRequest);
                Assert.Equal(CosmicConcurrencyException.Code, intervening.ErrorCode);
                Assert.Equal(editedPointer, await File.ReadAllTextAsync(dnaPath));
                await VerifyReplicaAsync(provider, identity.Result, dnaPath, replay: false);
                holon.Name = "after update";
                holon.Version = 2;
                saved = await provider.SaveHolonAsync(holon, saveChildren: false);
                Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
                Assert.True(File.Exists(dnaPath));
                await VerifyPendingOutboundAcrossRestartAsync(provider, identity.Result, dnaPath, publish: true);
            }
            else
            {
                var identity = await provider.CaptureStorageIdentityAsync();
                Assert.False(identity.IsError, identity.Message);
                Assert.Equal(await File.ReadAllTextAsync(identityPath), identity.Result.BindingFingerprint);
                var conditionalRecord = CreateConditionalFixtureRecord();
                var condition = new ConditionalHolonWrite(ConditionalOperationId, RecordId, ProviderType.IPFSOASIS,
                    new[] { new HolonWriteCondition(conditionalRecord.Id, null) });
                var request = new ConditionalHolonWriteRequest(condition, ConditionalHolonOperation.Save,
                    new[] { conditionalRecord }, saveChildren: false);
                var pointerBeforeReplay = await File.ReadAllTextAsync(dnaPath);
                var outcome = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(request, identity.Result);
                Assert.True(outcome.IsLoaded && !outcome.IsError, outcome.Message);
                Assert.Equal(ConditionalHolonReceiptState.Committed, outcome.Result.ReceiptState);
                Assert.False(string.IsNullOrWhiteSpace(outcome.Result.CommittedDeltaSummaryJson));
                var wrongSource = new CosmicStorageIdentity(identity.Result.Authority,
                    identity.Result.EndpointFingerprint, Guid.NewGuid());
                var mismatched = await provider.ReadBoundConditionalHolonWriteOutcomeAsync(request, wrongSource);
                Assert.True(mismatched.IsError);
                Assert.Equal("COSMIC_SOURCE_STORAGE_BINDING_MISMATCH", mismatched.ErrorCode);
                Assert.Null(mismatched.Result);
                Assert.Equal(pointerBeforeReplay, await File.ReadAllTextAsync(dnaPath));
                var replay = await provider.ExecuteConditionalHolonWriteAsync(request);
                Assert.True(replay.IsSaved && !replay.IsError, replay.Message);
                Assert.Equal(Guid.Parse(await File.ReadAllTextAsync(conditionalVersionPath)), Assert.Single(replay.Result).VersionId);
                var evidenceRequest = new ConditionalHolonReceiptEvidenceRequest(request, identity.Result,
                    identity.Result, new[] { conditionalRecord.Id });
                var evidence = await provider.ReadConditionalHolonReceiptEvidenceAsync(evidenceRequest);
                Assert.True(evidence.IsLoaded && !evidence.IsError, evidence.Message);
                var committed = Assert.Single(evidence.Result.ReceiptEvidence.Records);
                var replayedRecord = Assert.Single(replay.Result);
                Assert.Equal(replayedRecord.VersionId, committed.VersionId);
                Assert.Equal(CosmicConcurrency.GetProviderRevisionId(replayedRecord, ProviderType.IPFSOASIS),
                    committed.SourceProviderRevisionId);
                Assert.Equal(CommittedHolonReplicaPlan.PortableContentDigest(replayedRecord, ProviderType.IPFSOASIS),
                    committed.PortableContentDigest);
                Assert.Equal(pointerBeforeReplay, await File.ReadAllTextAsync(dnaPath));

                var beforeDelete = await provider.LoadHolonAsync(conditionalRecord.Id, loadChildren: false);
                Assert.True(beforeDelete.IsLoaded && !beforeDelete.IsError, beforeDelete.Message);
                var revision = CosmicConcurrency.GetProviderRevisionId(beforeDelete.Result, ProviderType.IPFSOASIS);
                Assert.NotNull(revision);
                var deleteCondition = new ConditionalHolonWrite(Guid.NewGuid(), RecordId, ProviderType.IPFSOASIS,
                    new[] { new HolonWriteCondition(conditionalRecord.Id, revision) });
                var deleteRequest = new ConditionalHolonWriteRequest(deleteCondition, ConditionalHolonOperation.HardDelete,
                    new[] { beforeDelete.Result }, saveChildren: false);
                var deleted = await provider.ExecuteConditionalHolonWriteAsync(deleteRequest);
                Assert.True(deleted.IsDeleted && !deleted.IsError, deleted.Message);
                Assert.DoesNotContain(await provider.LoadLookupToJson(), entry => entry.Value.Id == conditionalRecord.Id);
                var pointerAfterDelete = await File.ReadAllTextAsync(dnaPath);
                var deleteReplay = provider.ExecuteConditionalHolonWrite(deleteRequest);
                Assert.True(deleteReplay.IsDeleted && !deleteReplay.IsError, deleteReplay.Message);
                Assert.Equal(Assert.Single(deleted.Result).VersionId, Assert.Single(deleteReplay.Result).VersionId);
                Assert.Equal(pointerAfterDelete, await File.ReadAllTextAsync(dnaPath));
                await VerifyReplicaAsync(provider, identity.Result, dnaPath, replay: true);
                await VerifyPendingOutboundAcrossRestartAsync(provider, identity.Result, dnaPath, publish: false);
                await VerifyOutboundDeliveryAsync(provider, identity.Result, dnaPath);
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

    private static async Task VerifyPendingOutboundAcrossRestartAsync(IPFSOASIS provider, CosmicStorageIdentity source,
        string dnaPath, bool publish)
    {
        var databasePath = dnaPath + ".pending-target.sqlite";
        Assert.Equal(!publish, File.Exists(databasePath));
        using var sqlite = new SqliteProvider($"Data Source={databasePath};Pooling=False");
        var activated = await sqlite.ActivateProviderAsync();
        Assert.True(activated.Result && !activated.IsError, activated.Message);
        var captured = await sqlite.CaptureStorageIdentityAsync();
        Assert.False(captured.IsError, captured.Message);

        var operationId = new Guid("90644865-2be6-4044-bdfb-3e6a394f5b32");
        var recordId = new Guid("90644865-2be6-4044-bdfb-3e6a394f5b33");
        var expiry = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var policy = new HolonReplicationActorGrant(new[] { RecordId }, expiry);
        var route = new HolonReplicationRouteBinding(source, captured.Result, policy.BindingIdentity);
        using var secondSqlite = new SqliteProvider($"Data Source={databasePath}.second;Pooling=False");
        var secondActivation = await secondSqlite.ActivateProviderAsync();
        Assert.True(secondActivation.Result && !secondActivation.IsError, secondActivation.Message);
        var secondIdentity = await secondSqlite.CaptureStorageIdentityAsync();
        Assert.False(secondIdentity.IsError, secondIdentity.Message);
        var secondRoute = new HolonReplicationRouteBinding(source, secondIdentity.Result, policy.BindingIdentity);
        var routes = new[] { route, secondRoute };
        if (publish)
        {
            var enrollment = new HolonReplicationEnrollment(route.SourceId, routes.Select(binding => binding.TargetId), source, routes);
            var record = new Holon { Id = recordId, Name = "pending across restart", HolonType = HolonType.Holon };
            var write = new ConditionalHolonWrite(operationId, RecordId, source.Authority,
                new[] { new HolonWriteCondition(recordId, null) }, enrollment);
            var saved = await provider.ExecuteReplicatingHolonWriteAsync(
                new ConditionalHolonWriteRequest(write, ConditionalHolonOperation.Save, new[] { record }, saveChildren: false),
                enrollment);
            Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
        }

        var store = provider.GetBoundHolonReplicationOutbox(source);
        Assert.False(store.IsError, store.Message);
        var pending = await store.Result.PeekNextAsync(route.TargetId);
        Assert.False(pending.IsError, pending.Message);
        Assert.NotNull(pending.Result);
        Assert.Equal(operationId, pending.Result.OperationId);
        var inventory = await store.Result.ReadRouteHeadsAsync(pageSize: 1);
        Assert.False(inventory.IsError, inventory.Message);
        var head = Assert.Single(inventory.Result.Heads);
        var ordered = routes.Select(binding => binding.TargetId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        Assert.Equal(ordered[0], head.TargetId);
        Assert.Equal(operationId, head.OperationId);
        Assert.Equal(head.TargetId, inventory.Result.NextAfterTargetId);
        var continuation = await store.Result.ReadRouteHeadsAsync(pageSize: 1, afterTargetId: inventory.Result.NextAfterTargetId);
        Assert.False(continuation.IsError, continuation.Message);
        var secondHead = Assert.Single(continuation.Result.Heads);
        Assert.Equal(ordered[1], secondHead.TargetId);
        Assert.Equal(operationId, secondHead.OperationId);
        Assert.Null(continuation.Result.NextAfterTargetId);
        Assert.Equal(pending.Result.Sequence, (head.TargetId == route.TargetId ? head : secondHead).Sequence);

        var wrongSource = new CosmicStorageIdentity(source.Authority, source.EndpointFingerprint, Guid.NewGuid());
        var wrongStore = provider.GetBoundHolonReplicationOutbox(wrongSource);
        Assert.False(wrongStore.IsError, wrongStore.Message);
        var pointer = await File.ReadAllTextAsync(dnaPath);
        var rejectedInventory = await wrongStore.Result.ReadRouteHeadsAsync();
        Assert.Equal("COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH", rejectedInventory.ErrorCode);
        Assert.Null(rejectedInventory.Result);
        var rejectedClaim = await wrongStore.Result.ClaimNextAsync(route.TargetId, "wrong-source", TimeSpan.FromMinutes(1));
        Assert.Equal("COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH", rejectedClaim.ErrorCode);
        Assert.Null(rejectedClaim.Result);
        Assert.Equal(pointer, await File.ReadAllTextAsync(dnaPath));
        Assert.Equal(HolonDeliveryState.Pending, pending.Result.State);
        if (publish)
            return;

        var target = new CommittedHolonReplicaTarget(route.TargetId, sqlite, captured.Result.Authority, route.SourceId,
            source.Authority, policy, "ipfs-restarted-worker", Guid.NewGuid(), policy.BindingIdentity, captured.Result);
        var worker = new HolonOutboxDispatcher(store.Result, new[] { target }, "ipfs-restarted-worker");
        var delivery = await worker.DispatchOnceAsync(route.TargetId, maximumCount: 1);
        Assert.False(delivery.IsError, delivery.Message);
        Assert.Equal(1, delivery.Result.CompletedCount);
        var otherTarget = new CommittedHolonReplicaTarget(secondRoute.TargetId, secondSqlite, secondIdentity.Result.Authority,
            secondRoute.SourceId, source.Authority, policy, "ipfs-restarted-worker", Guid.NewGuid(), policy.BindingIdentity, secondIdentity.Result);
        var otherWorker = new HolonOutboxDispatcher(store.Result, new[] { otherTarget }, "ipfs-restarted-worker");
        var otherDelivery = await otherWorker.DispatchOnceAsync(secondRoute.TargetId, maximumCount: 1);
        Assert.False(otherDelivery.IsError, otherDelivery.Message);
        Assert.Equal(1, otherDelivery.Result.CompletedCount);

        var original = await provider.LoadHolonAsync(recordId, loadChildren: false);
        Assert.True(original.IsLoaded && !original.IsError, original.Message);
        var copied = await sqlite.LoadHolonAsync(recordId, loadChildren: false);
        Assert.True(copied.IsLoaded && !copied.IsError, copied.Message);
        Assert.Equal(original.Result.VersionId, copied.Result.VersionId);
        var outcome = await store.Result.InspectAsync(pending.Result.Sequence);
        Assert.False(outcome.IsError, outcome.Message);
        Assert.Equal(HolonDeliveryState.Completed, outcome.Result.State);
        var drained = await store.Result.ReadRouteHeadsAsync();
        Assert.False(drained.IsError, drained.Message);
        Assert.Empty(drained.Result.Heads);
    }

    private static async Task VerifyOutboundDeliveryAsync(IPFSOASIS provider, CosmicStorageIdentity source, string dnaPath)
    {
        using var sqlite = new SqliteProvider($"Data Source={dnaPath}.target.sqlite;Pooling=False");
        var activated = await sqlite.ActivateProviderAsync();
        Assert.True(activated.Result && !activated.IsError, activated.Message);
        var captured = await sqlite.CaptureStorageIdentityAsync();
        Assert.False(captured.IsError, captured.Message);
        var actor = Guid.NewGuid();
        var policy = new HolonReplicationActorGrant(new[] { actor }, DateTime.UtcNow.AddMinutes(10));
        var route = new HolonReplicationRouteBinding(source, captured.Result, policy.BindingIdentity);
        var enrollment = new HolonReplicationEnrollment(route.SourceId, new[] { route.TargetId }, source, new[] { route });
        var record = new Holon { Id = Guid.NewGuid(), Name = "outbound IPFS delivery", HolonType = HolonType.Holon };
        var write = new ConditionalHolonWrite(Guid.NewGuid(), actor, source.Authority,
            new[] { new HolonWriteCondition(record.Id, null) }, enrollment);
        var request = new ConditionalHolonWriteRequest(write, ConditionalHolonOperation.Save, new[] { record }, saveChildren: false);
        var saved = await provider.ExecuteReplicatingHolonWriteAsync(request, enrollment);
        Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
        var store = provider.GetBoundHolonReplicationOutbox(source);
        Assert.False(store.IsError, store.Message);
        var pending = await store.Result.PeekNextAsync(route.TargetId);
        Assert.False(pending.IsError, pending.Message);
        Assert.Equal(HolonDeliveryState.Pending, pending.Result.State);
        Assert.Equal(write.OperationId, pending.Result.OperationId);
        var claimed = await store.Result.ClaimNextAsync(route.TargetId, "interrupted-worker", TimeSpan.FromSeconds(15));
        Assert.False(claimed.IsError, claimed.Message);
        Assert.NotNull(claimed.Result);
        var secondStore = provider.GetBoundHolonReplicationOutbox(source);
        Assert.False(secondStore.IsError, secondStore.Message);
        var competing = await secondStore.Result.ClaimNextAsync(route.TargetId, "competing-worker", TimeSpan.FromMinutes(1));
        Assert.False(competing.IsError, competing.Message);
        Assert.Null(competing.Result);
        var wrongOwner = new HolonDeliveryClaim(claimed.Result.Sequence, claimed.Result.OperationId, route.TargetId,
            "competing-worker", claimed.Result.LeaseToken, claimed.Result.PayloadJson, claimed.Result.AttemptCount,
            claimed.Result.LeaseUntilUtc);
        var invalidCompletion = await secondStore.Result.FinishAsync(wrongOwner, HolonDeliveryOutcome.Completed);
        Assert.False(invalidCompletion.IsError, invalidCompletion.Message);
        Assert.False(invalidCompletion.Result);
        var remainingLease = claimed.Result.LeaseUntilUtc - DateTime.UtcNow;
        if (remainingLease > TimeSpan.Zero)
            await Task.Delay(remainingLease + TimeSpan.FromMilliseconds(100));
        var target = new CommittedHolonReplicaTarget(route.TargetId, sqlite, captured.Result.Authority, route.SourceId,
            source.Authority, policy, "ipfs-outbound-fixture", Guid.NewGuid(), policy.BindingIdentity, captured.Result);
        var worker = new HolonOutboxDispatcher(store.Result, new[] { target }, "ipfs-outbound-fixture");
        var delivery = await worker.DispatchOnceAsync(route.TargetId, maximumCount: 1);
        Assert.False(delivery.IsError, delivery.Message);
        Assert.Equal(1, delivery.Result.CompletedCount);
        var copied = await sqlite.LoadHolonAsync(record.Id, loadChildren: false);
        Assert.True(copied.IsLoaded && !copied.IsError, copied.Message);
        Assert.Equal(Assert.Single(saved.Result).VersionId, copied.Result.VersionId);
        var completed = await store.Result.InspectAsync(pending.Result.Sequence);
        Assert.False(completed.IsError, completed.Message);
        Assert.Equal(HolonDeliveryState.Completed, completed.Result.State);
        Assert.Equal(2, completed.Result.AttemptCount);
        var pointer = await File.ReadAllTextAsync(dnaPath);
        var replay = await provider.ExecuteReplicatingHolonWriteAsync(request, enrollment);
        Assert.True(replay.IsSaved && !replay.IsError, replay.Message);
        Assert.Equal(pointer, await File.ReadAllTextAsync(dnaPath));
        Assert.Null((await store.Result.PeekNextAsync(route.TargetId)).Result);

        var committed = Assert.Single(saved.Result);
        foreach (var operation in new[] { ConditionalHolonOperation.SoftDelete, ConditionalHolonOperation.HardDelete })
        {
            var revision = CosmicConcurrency.GetProviderRevisionId(committed, source.Authority);
            Assert.NotNull(revision);
            var delete = new ConditionalHolonWrite(Guid.NewGuid(), actor, source.Authority,
                new[] { new HolonWriteCondition(record.Id, revision) }, enrollment);
            var deleted = await provider.ExecuteReplicatingHolonWriteAsync(
                new ConditionalHolonWriteRequest(delete, operation, new[] { committed }, saveChildren: false), enrollment);
            Assert.True(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            committed = Assert.Single(deleted.Result);
            var replicated = await worker.DispatchOnceAsync(route.TargetId, maximumCount: 1);
            Assert.False(replicated.IsError, replicated.Message);
            Assert.Equal(1, replicated.Result.CompletedCount);
            var expected = new CommittedHolonReplicaRequest(delete.OperationId, route.SourceId, source.Authority,
                actor, operation, new[] { committed }, false, true, 0, new[] { route });
            var receipt = await sqlite.ReadCommittedHolonReplicaReceiptAsync(new(expected, captured.Result));
            Assert.True(receipt.IsLoaded && !receipt.IsError, receipt.Message);
            Assert.Equal(ConditionalHolonReceiptState.Committed, receipt.Result.ReceiptState);
        }

        var removed = await sqlite.LoadHolonAsync(record.Id, loadChildren: false);
        Assert.False(removed.IsLoaded);
        Assert.Null(removed.Result);
        Assert.DoesNotContain(await provider.LoadLookupToJson(), entry => entry.Value.Id == record.Id);

        var parent = new Holon { Id = Guid.NewGuid(), Name = "outbound parent", HolonType = HolonType.Holon };
        var child = new Holon { Id = Guid.NewGuid(), Name = "outbound child", HolonType = HolonType.Holon,
            ParentHolonId = parent.Id };
        parent.Children = new List<IHolon> { child };
        var graphWrite = new ConditionalHolonWrite(Guid.NewGuid(), actor, source.Authority,
            new[] { new HolonWriteCondition(parent.Id, null), new HolonWriteCondition(child.Id, null) }, enrollment);
        var graphSaved = await provider.ExecuteReplicatingHolonWriteAsync(
            new ConditionalHolonWriteRequest(graphWrite, ConditionalHolonOperation.Save, new[] { parent },
                saveChildren: true), enrollment);
        Assert.True(graphSaved.IsSaved && !graphSaved.IsError, graphSaved.Message);
        var graphDelivery = await worker.DispatchOnceAsync(route.TargetId, maximumCount: 1);
        Assert.False(graphDelivery.IsError, graphDelivery.Message);
        Assert.Equal(1, graphDelivery.Result.CompletedCount);

        var sourceParent = Assert.Single(graphSaved.Result);
        var sourceChild = Assert.Single(sourceParent.Children);
        var copiedParent = await sqlite.LoadHolonAsync(parent.Id, loadChildren: true);
        Assert.True(copiedParent.IsLoaded && !copiedParent.IsError, copiedParent.Message);
        Assert.Equal(sourceParent.VersionId, copiedParent.Result.VersionId);
        var copiedChild = Assert.Single(copiedParent.Result.Children);
        Assert.Equal(child.Id, copiedChild.Id);
        Assert.Equal(sourceChild.VersionId, copiedChild.VersionId);
        Assert.Equal(parent.Id, copiedChild.ParentHolonId);
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
