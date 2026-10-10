using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS;
using NextGenSoftware.OASIS.Common;
using Xunit;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

public sealed class OASISEdgeRuntimeTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"oasis-edge-runtime-{Guid.NewGuid():N}.db");
    private readonly Guid _deviceId = Guid.NewGuid();
    private readonly Guid _avatarId = Guid.NewGuid();

    [Fact]
    public void PayloadSerializerStreamsOpaqueJsonWithoutChangingItsValues()
    {
        var serializer = new EdgePayloadSerializer();
        using var document = JsonDocument.Parse("{\"message\":\"quote: \\\" and newline\\n\",\"count\":2,\"enabled\":true}");
        var token = document.RootElement.Clone();

        var json = serializer.Serialize(token);
        var restored = serializer.Deserialize<JsonElement>(json);

        restored.GetProperty("message").GetString().Should().Be("quote: \" and newline\n");
        restored.GetProperty("count").GetInt32().Should().Be(2);
        restored.GetProperty("enabled").GetBoolean().Should().BeTrue();
        json.Should().NotContain("\r").And.NotContain("\n");
    }

    [Fact]
    public async Task OfflineMutationSucceedsWithoutCallingTransport()
    {
        var transport = new RecordingTransport();
        using var runtime = CreateRuntime(transport);
        var saved = await runtime.SaveLocalAsync(NewMutation(), default);
        saved.IsError.Should().BeFalse();
        runtime.Status.Connectivity.Should().Be(EdgeConnectivityState.Offline);
        runtime.Status.PendingOperationCount.Should().Be(1);
        transport.CallCount.Should().Be(0);
        (await runtime.LocalStore.ReadPendingOperationsAsync(10, default)).Result.Should().ContainSingle();
    }

    [Fact]
    public async Task TypedRepositoryEnumeratesSynchronizedEntitiesForOfflineDomainViews()
    {
        using var runtime = CreateRuntime(new RecordingTransport());
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        await runtime.LocalStore.CommitExchangeAsync(new SyncCommit
        {
            CommittedUtc = DateTime.UtcNow, NextPullCheckpoint = "offline-view",
            RemoteChanges = new[]
            {
                new SyncRemoteChange
                {
                    ChangeId = "one", EntityId = first, EntityType = HyperDriveEntityTypes.InventoryItem,
                    Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(),
                    PayloadJson = "{\"Name\":\"Anorak\",\"Progress\":1}", ChangedUtc = DateTime.UtcNow
                },
                new SyncRemoteChange
                {
                    ChangeId = "two", EntityId = second, EntityType = HyperDriveEntityTypes.InventoryItem,
                    Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(),
                    PayloadJson = "{\"Name\":\"Tree\",\"Progress\":2}", ChangedUtc = DateTime.UtcNow.AddMilliseconds(1)
                }
            }
        }, default);

        var loaded = await runtime.Entities.LoadAllAsync<TestEntity>(HyperDriveEntityTypes.InventoryItem);

        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.Select(x => x.Entity.Name).Should().Equal("Anorak", "Tree");
    }

    [Fact]
    public async Task CorruptLocalProjectionReturnsRecoverableStructuredErrorWithoutThrowing()
    {
        using var runtime = CreateRuntime(new RecordingTransport());
        var entityId = Guid.NewGuid();
        var committed = await runtime.LocalStore.CommitExchangeAsync(new SyncCommit
        {
            CommittedUtc = DateTime.UtcNow,
            NextPullCheckpoint = "corrupt-payload",
            RemoteChanges = new[]
            {
                new SyncRemoteChange
                {
                    ChangeId = "corrupt", EntityId = entityId, EntityType = "test-profile",
                    Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(),
                    PayloadJson = "{not valid json", ChangedUtc = DateTime.UtcNow
                }
            }
        }, default);
        committed.IsError.Should().BeFalse(committed.Message);

        Func<Task> load = async () =>
        {
            var result = await runtime.Entities.LoadAsync<TestEntity>("test-profile", entityId);
            result.IsError.Should().BeTrue();
            result.ErrorCode.Should().Be("EDGE_ENTITY_DESERIALIZATION_FAILED");
            result.Message.Should().Contain("could not be deserialized");
        };

        await load.Should().NotThrowAsync();
        runtime.Status.Connectivity.Should().Be(EdgeConnectivityState.Offline,
            "a corrupt local projection is a data error, not a reason to crash or fabricate connectivity");
    }

    [Fact]
    public async Task ConnectivityRecoveryDrainsBoundedOutbox()
    {
        var transport = new RecordingTransport();
        using var runtime = CreateRuntime(transport, maximumOperations: 2);
        for (int i = 0; i < 5; i++) await runtime.SaveLocalAsync(NewMutation(), default);

        var result = await runtime.SetConnectivityAsync(true, default);

        result.IsError.Should().BeFalse();
        result.Result.SentOperationCount.Should().Be(5);
        transport.CallCount.Should().Be(3);
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Synchronized);
        runtime.Status.PendingOperationCount.Should().Be(0);
        (await runtime.LocalStore.ReadPendingOperationsAsync(10, default)).Result.Should().BeEmpty();
    }

    [Fact]
    public async Task AcceptedCommandKeepsPullingUntilDurableOutcomeArrives()
    {
        var operationId = Guid.NewGuid();
        var transport = new DelayedCommandOutcomeTransport(operationId);
        using var runtime = CreateRuntime(transport);
        await runtime.SaveLocalAsync(new EdgeLocalMutation
        {
            OperationId = operationId, DeviceId = _deviceId, AvatarId = _avatarId,
            EntityId = Guid.NewGuid(), EntityType = HyperDriveEntityTypes.InventoryItem,
            Kind = SyncOperationKind.Command, LocalVersionId = Guid.NewGuid(),
            PayloadJson = "{}", CreatedUtc = DateTime.UtcNow
        }, default);

        var synchronized = await runtime.SetConnectivityAsync(true, default);

        synchronized.IsError.Should().BeFalse(synchronized.Message);
        transport.CallCount.Should().Be(3);
        runtime.Status.PendingOperationCount.Should().Be(0);
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Synchronized);
    }

    [Fact]
    public async Task HostedConnectivityIsNotReportedOnlineUntilExchangeSucceeds()
    {
        var transport = new ControlledTransport();
        using var runtime = CreateRuntime(transport);

        var connecting = runtime.SetConnectivityAsync(true, default);
        await transport.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        runtime.Status.Connectivity.Should().Be(EdgeConnectivityState.Connecting);
        transport.Release.TrySetResult(true);
        var connected = await connecting;

        connected.IsError.Should().BeFalse();
        runtime.Status.Connectivity.Should().Be(EdgeConnectivityState.Online);
    }

    [Fact]
    public async Task OfflineSignalDoesNotAttemptNetworkAndPublishesPendingStatus()
    {
        var transport = new RecordingTransport();
        using var runtime = CreateRuntime(transport);
        await runtime.SaveLocalAsync(NewMutation(), default);
        var result = await runtime.SetConnectivityAsync(false, default);
        result.IsError.Should().BeFalse();
        transport.CallCount.Should().Be(0);
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Pending);
        runtime.Status.LastMessage.Should().Contain("queued");
    }

    [Fact]
    public async Task BindOnetIdentitySignsAvatarDeviceNodeProofAndCallsHostedBindingTransport()
    {
        var transport = new RecordingTransport();
        using var identity = new TestNodeIdentity();
        using var runtime = CreateRuntime(transport);

        var result = await runtime.BindOnetIdentityAsync(identity);

        result.IsError.Should().BeFalse(result.Message);
        transport.BindingRequest.Should().NotBeNull();
        transport.BindingRequest!.DeviceId.Should().Be(_deviceId);
        transport.BindingRequest.NodeId.Should().Be(identity.NodeId);
        HyperDrivePeerBindingProof.Verify(_avatarId, _deviceId, identity.NodeId,
            Convert.FromBase64String(identity.PublicKey), Convert.FromBase64String(transport.BindingRequest.Signature))
            .Should().BeTrue();
    }

    [Fact]
    public async Task BindOnetIdentityRejectsNodeIdThatDoesNotMatchPublicKeyBeforeNetworkCall()
    {
        var transport = new RecordingTransport();
        using var identity = new TestNodeIdentity(new string('0', 64));
        using var runtime = CreateRuntime(transport);

        var result = await runtime.BindOnetIdentityAsync(identity);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("EDGE_NODE_ID_MISMATCH");
        transport.BindingRequest.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticatedHostedGrantIsValidatedBeforeSecureDeviceStorage()
    {
        var store = new RuntimeSecureStore();
        var transport = new GrantTransport(_avatarId, _deviceId);
        using var runtime = new OASISEdgeRuntime(new EdgeRuntimeOptions
        {
            DeviceId = _deviceId, AvatarId = _avatarId, DatabasePath = _path
        }, transport, store, new AcceptGrantValidator());

        var result = await runtime.AcquireOfflineSessionAsync(new[] { "world.read" }, 30);

        result.IsError.Should().BeFalse(result.Message);
        transport.Request.Should().NotBeNull();
        transport.Request!.DeviceId.Should().Be(_deviceId);
        store.Grant.Should().BeSameAs(result.Result);
        transport.AttachedGrant.Should().BeSameAs(result.Result);
    }

    [Fact]
    public async Task ResumedSecureGrantIsAttachedToTransportForAutomaticRecovery()
    {
        var store = new RuntimeSecureStore();
        var transport = new GrantTransport(_avatarId, _deviceId);
        using var runtime = new OASISEdgeRuntime(new EdgeRuntimeOptions
        { DeviceId = _deviceId, AvatarId = _avatarId, DatabasePath = _path },
            transport, store, new AcceptGrantValidator());
        await runtime.AcquireOfflineSessionAsync(new[] { "hyperdrive.sync" }, 30);
        transport.AttachedGrant = null;

        var resumed = await runtime.ResumeOfflineSessionAsync(new[] { "hyperdrive.sync" });

        resumed.IsError.Should().BeFalse(resumed.Message);
        transport.AttachedGrant.Should().BeSameAs(store.Grant);
    }

    [Fact]
    public async Task UnresolvedConflictKeepsRuntimePendingAcrossLaterSuccessfulSyncs()
    {
        var transport = new ConflictTransport();
        using var runtime = CreateRuntime(transport);
        await runtime.SaveLocalAsync(NewMutation(), default);

        (await runtime.SetConnectivityAsync(true, default)).IsError.Should().BeFalse();
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Pending);
        (await runtime.SynchronizeAsync(default)).IsError.Should().BeFalse();
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Pending);
        (await runtime.ReadUnresolvedConflictsAsync(default)).Result.Should().ContainSingle();
    }

    [Fact]
    public async Task ReconnectReconcilesIndependentLocalAndHostedChangesThroughDurableManualMerge()
    {
        var entityId = Guid.NewGuid();
        var initialVersion = Guid.NewGuid();
        var hostedVersion = Guid.NewGuid();
        var localVersion = Guid.NewGuid();
        var mergedVersion = Guid.NewGuid();
        var localOperationId = Guid.NewGuid();
        var mergeOperationId = Guid.NewGuid();
        var transport = new DivergentEntityTransport(entityId, initialVersion, hostedVersion,
            localOperationId, mergeOperationId, mergedVersion);
        using var runtime = CreateRuntime(transport);

        var bootstrap = await runtime.SetConnectivityAsync(true, default);
        bootstrap.IsError.Should().BeFalse(bootstrap.Message);
        var initial = await runtime.Entities.LoadAsync<TestEntity>("test-profile", entityId);
        initial.Result.VersionId.Should().Be(initialVersion);
        initial.Result.Entity.Name.Should().Be("shared");

        (await runtime.SetConnectivityAsync(false, default)).IsError.Should().BeFalse();
        var offlineWrite = await runtime.Entities.SaveAsync(new EdgeEntityWriteRequest<TestEntity>
        {
            OperationId = localOperationId,
            EntityId = entityId,
            EntityType = "test-profile",
            BaseVersionId = initialVersion,
            VersionId = localVersion,
            Entity = new TestEntity { Name = "local edit", Progress = 2 },
            CreatedUtc = DateTime.UtcNow
        });
        offlineWrite.IsError.Should().BeFalse(offlineWrite.Message);
        transport.ApplyIndependentHostedEdit();

        var reconnect = await runtime.SetConnectivityAsync(true, default);
        reconnect.IsError.Should().BeFalse(reconnect.Message);
        reconnect.Result.ConflictCount.Should().Be(1);
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Pending);
        runtime.Status.UnresolvedConflictCount.Should().Be(1);
        runtime.Status.LastConflict.Should().NotBeNull();
        runtime.Status.LastConflict.OperationId.Should().Be(localOperationId);
        runtime.Status.LastConflict.EntityId.Should().Be(entityId);
        runtime.Status.LastConflict.ServerVersionId.Should().Be(hostedVersion);
        runtime.Status.LastConflict.LocalVersionId.Should().Be(localVersion);
        var conflict = (await runtime.ReadUnresolvedConflictsAsync(default)).Result.Should().ContainSingle().Subject;
        conflict.OperationId.Should().Be(localOperationId);
        conflict.ServerVersionId.Should().Be(hostedVersion);
        conflict.LocalVersionId.Should().Be(localVersion);
        conflict.LocalPayloadJson.Should().Contain("local edit");
        var hosted = await runtime.Entities.LoadAsync<TestEntity>("test-profile", entityId);
        hosted.Result.VersionId.Should().Be(hostedVersion);
        hosted.Result.Entity.Name.Should().Be("hosted edit");

        var resolution = await runtime.ResolveConflictAsync(new EdgeConflictResolution
        {
            OperationId = localOperationId,
            Kind = EdgeConflictResolutionKind.ManualMerge,
            ResolutionOperationId = mergeOperationId,
            ResolutionVersionId = mergedVersion,
            MergedPayloadJson = "{\"Name\":\"merged edit\",\"Progress\":3}"
        }, default);
        resolution.IsError.Should().BeFalse(resolution.Message);
        resolution.Result.BaseVersionId.Should().Be(hostedVersion);
        resolution.Result.VersionId.Should().Be(mergedVersion);
        runtime.Status.UnresolvedConflictCount.Should().Be(0);
        runtime.Status.LastConflict.Should().BeNull();

        var synchronized = await runtime.SynchronizeAsync(default);
        synchronized.IsError.Should().BeFalse(synchronized.Message);
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Synchronized);
        runtime.Status.PendingOperationCount.Should().Be(0);
        (await runtime.ReadUnresolvedConflictsAsync(default)).Result.Should().BeEmpty();
        var merged = await runtime.Entities.LoadAsync<TestEntity>("test-profile", entityId);
        merged.Result.VersionId.Should().Be(mergedVersion);
        merged.Result.Entity.Name.Should().Be("merged edit");
        transport.AcceptedMergeBaseVersion.Should().Be(hostedVersion);
    }

    [Fact]
    public async Task HostedNetworkFailureSwitchesBackToExplicitLocalPendingMode()
    {
        using var runtime = CreateRuntime(new OfflineTransport());
        await runtime.SaveLocalAsync(NewMutation(), default);

        var sync = await runtime.SetConnectivityAsync(true, default);

        sync.IsError.Should().BeTrue();
        sync.ErrorCode.Should().Be("HYPERDRIVE_NETWORK_UNAVAILABLE");
        runtime.Status.Connectivity.Should().Be(EdgeConnectivityState.Offline);
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Pending);
        runtime.Status.LastMessage.Should().Contain("Local mode remains active");
        runtime.Status.PendingOperationCount.Should().Be(1);
        (await runtime.LocalStore.ReadPendingOperationsAsync(10, default)).Result.Should().ContainSingle();
    }

    [Theory]
    [InlineData("HYPERDRIVE_NETWORK_TIMEOUT")]
    [InlineData("HYPERDRIVE_REMOTE_UNAVAILABLE")]
    public async Task TimeoutAndGatewayOutageAlsoSwitchToDurableOfflineMode(string errorCode)
    {
        using var runtime = CreateRuntime(new OfflineTransport(errorCode));
        await runtime.SaveLocalAsync(NewMutation(), default);

        var sync = await runtime.SetConnectivityAsync(true, default);

        sync.ErrorCode.Should().Be(errorCode);
        runtime.Status.Connectivity.Should().Be(EdgeConnectivityState.Offline);
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Pending);
        runtime.Status.PendingOperationCount.Should().Be(1);
    }

    [Fact]
    public async Task TypedEntitySaveAndLoadUseTheAtomicDurableOutboxBoundary()
    {
        using var runtime = CreateRuntime(new RecordingTransport());
        var entityId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        var saved = await runtime.Entities.SaveAsync(new EdgeEntityWriteRequest<TestEntity>
        {
            OperationId = Guid.NewGuid(), EntityId = entityId, EntityType = "quest-progress",
            VersionId = versionId, Entity = new TestEntity { Name = "Anoraks", Progress = 3 }
        });
        var loaded = await runtime.Entities.LoadAsync<TestEntity>("quest-progress", entityId);

        saved.IsError.Should().BeFalse(saved.Message);
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result!.VersionId.Should().Be(versionId);
        loaded.Result.IsDeleted.Should().BeFalse();
        loaded.Result.Entity.Name.Should().Be("Anoraks");
        loaded.Result.Entity.Progress.Should().Be(3);
        runtime.Status.PendingOperationCount.Should().Be(1);
        (await runtime.LocalStore.ReadPendingOperationsAsync(10, default)).Result.Should().ContainSingle();
    }

    [Fact]
    public async Task TypedEntityDeletePersistsAnObservableTombstoneAndOutboxOperation()
    {
        using var runtime = CreateRuntime(new RecordingTransport());
        var entityId = Guid.NewGuid();
        var firstVersion = Guid.NewGuid();
        await runtime.Entities.SaveAsync(new EdgeEntityWriteRequest<TestEntity>
        {
            OperationId = Guid.NewGuid(), EntityId = entityId, EntityType = "inventory",
            VersionId = firstVersion, Entity = new TestEntity { Name = "Tree", Progress = 1 }
        });

        var deleted = await runtime.Entities.DeleteAsync(new EdgeEntityDeleteRequest
        {
            OperationId = Guid.NewGuid(), EntityId = entityId, EntityType = "inventory",
            BaseVersionId = firstVersion, VersionId = Guid.NewGuid()
        });
        var loaded = await runtime.Entities.LoadAsync<TestEntity>("inventory", entityId);

        deleted.IsError.Should().BeFalse(deleted.Message);
        loaded.Result!.IsDeleted.Should().BeTrue();
        loaded.Result.Entity.Should().BeNull();
        runtime.Status.PendingOperationCount.Should().Be(2);
    }

    [Fact]
    public async Task TypedEntityRemainsReadableAfterRuntimeRestart()
    {
        var entityId = Guid.NewGuid();
        using (var first = CreateRuntime(new RecordingTransport()))
        {
            (await first.Entities.SaveAsync(new EdgeEntityWriteRequest<TestEntity>
            {
                OperationId = Guid.NewGuid(), EntityId = entityId, EntityType = "avatar-state",
                VersionId = Guid.NewGuid(), Entity = new TestEntity { Name = "Offline", Progress = 7 }
            })).IsError.Should().BeFalse();
        }

        using var restarted = CreateRuntime(new RecordingTransport());
        var loaded = await restarted.Entities.LoadAsync<TestEntity>("avatar-state", entityId);
        loaded.Result!.Entity.Name.Should().Be("Offline");
        loaded.Result.Entity.Progress.Should().Be(7);
        (await restarted.LocalStore.ReadPendingOperationsAsync(10, default)).Result.Should().ContainSingle();
    }

    [Fact]
    public async Task TypedEntityWriteRejectsMissingStableOperationIdBeforePersistence()
    {
        using var runtime = CreateRuntime(new RecordingTransport());
        var result = await runtime.Entities.SaveAsync(new EdgeEntityWriteRequest<TestEntity>
        {
            EntityId = Guid.NewGuid(), EntityType = "holon", VersionId = Guid.NewGuid(),
            Entity = new TestEntity { Name = "invalid" }
        });
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("EDGE_ENTITY_WRITE_INVALID");
        runtime.Status.PendingOperationCount.Should().Be(0);
    }

    [Fact]
    public async Task OfflineCommandQueuesIntentWithoutSpeculativelyMutatingEntityState()
    {
        using var runtime = CreateRuntime(new RecordingTransport());
        Guid operationId = Guid.NewGuid();
        Guid questId = Guid.NewGuid();
        var queued = await runtime.Entities.QueueQuestProgressAsync(operationId, questId,
            new HyperDriveQuestProgressCommand { GameSource = "ODOOM", MonstersKilledDelta = 1 });

        queued.IsError.Should().BeFalse(queued.Message);
        queued.Result.Kind.Should().Be(SyncOperationKind.Command);
        (await runtime.Entities.LoadAsync<TestEntity>(HyperDriveEntityTypes.QuestProgress, questId))
            .Result.Should().BeNull("commands are intents until the hosted domain manager confirms them");
        (await runtime.LocalStore.ReadPendingOperationsAsync(10, default)).Result
            .Should().ContainSingle(x => x.OperationId == operationId && x.Kind == SyncOperationKind.Command);
    }

    [Theory]
    [InlineData("quest", HyperDriveEntityTypes.QuestProgress)]
    [InlineData("inventory", HyperDriveEntityTypes.InventoryItem)]
    [InlineData("geonft", HyperDriveEntityTypes.GeoNftCollection)]
    [InlineData("geohotspot", HyperDriveEntityTypes.GeoHotSpot)]
    public async Task OurWorldOfflineCommandsSurviveProcessRestartAsOneDurableIntent(
        string commandKind, string expectedEntityType)
    {
        Guid operationId = Guid.NewGuid();
        Guid entityId = Guid.NewGuid();
        using (var first = CreateRuntime(new RecordingTransport()))
        {
            var queued = commandKind switch
            {
                "quest" => await first.Entities.QueueQuestProgressAsync(operationId, entityId,
                    new HyperDriveQuestProgressCommand { GameSource = "Our World", XpEarnedDelta = 5 }),
                "inventory" => await first.Entities.QueueInventoryGrantAsync(operationId, entityId,
                    new HyperDriveInventoryGrantCommand { Name = "Offline reward", GameSource = "Our World" }),
                "geonft" => await first.Entities.QueueGeoNftCollectionAsync(operationId, entityId,
                    new HyperDriveGeoNftCollectionCommand { GameSource = "Our World", Quantity = 1 }),
                "geohotspot" => await first.Entities.QueueGeoHotSpotTriggerAsync(operationId, entityId,
                    new HyperDriveGeoHotSpotTriggerCommand
                    {
                        GameSource = "Our World", TriggerType = 0, ObservedAtUtc = DateTime.UtcNow
                    }),
                _ => throw new InvalidOperationException($"Unknown command kind '{commandKind}'.")
            };

            queued.IsError.Should().BeFalse(queued.Message);
            queued.Result.OperationId.Should().Be(operationId);
            queued.Result.EntityType.Should().Be(expectedEntityType);
            queued.Result.Kind.Should().Be(SyncOperationKind.Command);
        }

        using var restarted = CreateRuntime(new RecordingTransport());
        var initializedOffline = await restarted.SetConnectivityAsync(false, default);
        initializedOffline.IsError.Should().BeFalse(initializedOffline.Message);
        var pending = await restarted.LocalStore.ReadPendingOperationsAsync(10, default);
        pending.IsError.Should().BeFalse(pending.Message);
        pending.Result.Should().ContainSingle(x =>
            x.OperationId == operationId &&
            x.EntityId == entityId &&
            x.EntityType == expectedEntityType &&
            x.Kind == SyncOperationKind.Command);
        restarted.Status.PendingOperationCount.Should().Be(1);
    }

    [Fact]
    public async Task GeoHotSpotTriggerQueuesStableEvidenceWithoutSpeculativeEffects()
    {
        using var runtime = CreateRuntime(new RecordingTransport());
        Guid operationId = Guid.NewGuid();
        Guid hotSpotId = Guid.NewGuid();
        DateTime observedAtUtc = DateTime.UtcNow;

        var queued = await runtime.Entities.QueueGeoHotSpotTriggerAsync(operationId, hotSpotId,
            new HyperDriveGeoHotSpotTriggerCommand
            {
                TriggerType = 0, ObservedAtUtc = observedAtUtc, Latitude = 50.1,
                Longitude = -1.2, AccuracyMetres = 4, GameSource = "Our World"
            });

        queued.IsError.Should().BeFalse(queued.Message);
        queued.Result.Kind.Should().Be(SyncOperationKind.Command);
        queued.Result.EntityType.Should().Be(HyperDriveEntityTypes.GeoHotSpot);
        var payload = HyperDriveJson.Deserialize<HyperDriveGeoHotSpotTriggerCommand>(queued.Result.PayloadJson);
        payload.ObservedAtUtc.Should().Be(observedAtUtc);
        payload.GameSource.Should().Be("Our World");
        (await runtime.Entities.LoadAsync<TestEntity>(HyperDriveEntityTypes.GeoHotSpot, hotSpotId))
            .Result.Should().BeNull("trigger effects remain authoritative on the hosted ONODE");
    }

    [Fact]
    public async Task HostedCommandOutcomeBecomesReadableThroughTypedEdgeApi()
    {
        using var runtime = CreateRuntime(new RecordingTransport());
        Guid operationId = Guid.NewGuid();
        var outcome = new HyperDriveCommandOutcome
        {
            OperationId = operationId, Succeeded = true, Code = "COMMAND_COMPLETED",
            Message = "done", ResultJson = "{\"percent\":100}", CompletedUtc = DateTime.UtcNow
        };
        var serialized = HyperDriveJson.Serialize(outcome);
        var committed = await runtime.LocalStore.CommitExchangeAsync(new SyncCommit
        {
            RemoteChanges = new[]
            {
                new SyncRemoteChange
                {
                    ChangeId = "command:" + operationId.ToString("D"), EntityId = operationId,
                    EntityType = HyperDriveEntityTypes.CommandResult, Kind = SyncOperationKind.Upsert,
                    VersionId = Guid.NewGuid(), PayloadJson = serialized, ChangedUtc = outcome.CompletedUtc
                }
            },
            OperationResults = Array.Empty<SyncOperationResult>(), NextPullCheckpoint = "1",
            CommittedUtc = DateTime.UtcNow
        }, default);

        committed.IsError.Should().BeFalse(committed.Message);
        var loaded = await runtime.Entities.LoadCommandOutcomeAsync(operationId);
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.Entity.Succeeded.Should().BeTrue();
        loaded.Result.Entity.Code.Should().Be("COMMAND_COMPLETED");
    }

    [Fact]
    public async Task ConnectivityMonitorAutomaticallySynchronizesWhenPlatformComesOnline()
    {
        var transport = new RecordingTransport();
        using var runtime = CreateRuntime(transport);
        var monitor = new TestConnectivityMonitor(false);
        await runtime.SaveLocalAsync(NewMutation());
        (await runtime.StartConnectivityMonitoringAsync(monitor)).IsError.Should().BeFalse();

        var synchronized = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.StatusChanged += (_, status) =>
        {
            if (status.Synchronization == EdgeSynchronizationState.Synchronized)
                synchronized.TrySetResult(true);
        };
        monitor.SetOnline(true);

        await synchronized.Task.WaitAsync(TimeSpan.FromSeconds(5));
        transport.CallCount.Should().Be(1);
        runtime.Status.PendingOperationCount.Should().Be(0);
    }

    [Fact]
    public async Task StoppedConnectivityMonitorCannotTriggerNetworkWork()
    {
        var transport = new RecordingTransport();
        using var runtime = CreateRuntime(transport);
        var monitor = new TestConnectivityMonitor(false);
        await runtime.SaveLocalAsync(NewMutation());
        await runtime.StartConnectivityMonitoringAsync(monitor);
        runtime.StopConnectivityMonitoring();

        monitor.SetOnline(true);
        await Task.Delay(100);

        transport.CallCount.Should().Be(0);
        runtime.Status.PendingOperationCount.Should().Be(1);
    }

    [Fact]
    public async Task HostedServiceRecoveryAutomaticallyRetriesWhilePlatformRemainsOnline()
    {
        var transport = new RecoveringTransport();
        using var runtime = new OASISEdgeRuntime(new EdgeRuntimeOptions
        {
            DeviceId = _deviceId,
            AvatarId = _avatarId,
            DatabasePath = _path,
            MaximumRemoteChangesPerExchange = 10,
            HostedServiceRecoveryInterval = TimeSpan.FromMilliseconds(20)
        }, transport);
        var monitor = new TestConnectivityMonitor(true);
        var synchronized = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.StatusChanged += (_, status) =>
        {
            if (status.Synchronization == EdgeSynchronizationState.Synchronized)
                synchronized.TrySetResult(true);
        };
        await runtime.SaveLocalAsync(NewMutation());

        var initial = await runtime.StartConnectivityMonitoringAsync(monitor);

        initial.IsError.Should().BeTrue();
        initial.ErrorCode.Should().Be("HYPERDRIVE_REMOTE_UNAVAILABLE");
        runtime.Status.Connectivity.Should().Be(EdgeConnectivityState.Offline);
        await synchronized.Task.WaitAsync(TimeSpan.FromSeconds(5));
        transport.CallCount.Should().Be(2);
        runtime.Status.Connectivity.Should().Be(EdgeConnectivityState.Online);
        runtime.Status.PendingOperationCount.Should().Be(0);
    }

    [Fact]
    public async Task SuspendQueuesLocalWorkAndResumeDrainsItWhenPlatformIsOnline()
    {
        var transport = new RecordingTransport();
        using var runtime = CreateRuntime(transport);
        var monitor = new TestConnectivityMonitor(false);
        await runtime.StartConnectivityMonitoringAsync(monitor);
        await runtime.SuspendAsync();
        await runtime.SaveLocalAsync(NewMutation());

        monitor.SetOnline(true);
        await Task.Delay(100);

        transport.CallCount.Should().Be(0);
        runtime.Status.Connectivity.Should().Be(EdgeConnectivityState.Offline);
        runtime.Status.PendingOperationCount.Should().Be(1);
        var resumed = await runtime.ResumeAsync();
        resumed.IsError.Should().BeFalse(resumed.Message);
        transport.CallCount.Should().Be(1);
        runtime.Status.Connectivity.Should().Be(EdgeConnectivityState.Online);
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Synchronized);
        runtime.Status.PendingOperationCount.Should().Be(0);
    }

    [Fact]
    public async Task OfflineTransitionDuringExchangeCannotBeOverwrittenByStaleSuccess()
    {
        var transport = new ControlledTransport();
        using var runtime = CreateRuntime(transport);
        await runtime.SaveLocalAsync(NewMutation());

        var online = runtime.SetConnectivityAsync(true);
        await transport.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await runtime.SetConnectivityAsync(false);
        transport.Release.TrySetResult(true);
        await online;

        runtime.Status.Connectivity.Should().Be(EdgeConnectivityState.Offline);
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Pending);
        runtime.Status.LastMessage.Should().Contain("Connectivity changed");
    }

    [Fact]
    public async Task LocalWriteDuringExchangeIsDrainedBeforeSynchronizedStatusIsPublished()
    {
        var transport = new ControlledTransport();
        using var runtime = CreateRuntime(transport);
        await runtime.SaveLocalAsync(NewMutation());

        var synchronization = runtime.SetConnectivityAsync(true);
        await transport.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await runtime.SaveLocalAsync(NewMutation());
        transport.Release.TrySetResult(true);
        var result = await synchronization;

        result.IsError.Should().BeFalse(result.Message);
        transport.CallCount.Should().Be(2);
        result.Result.SentOperationCount.Should().Be(2);
        runtime.Status.PendingOperationCount.Should().Be(0);
        runtime.Status.Synchronization.Should().Be(EdgeSynchronizationState.Synchronized);
    }

    [Fact]
    public async Task AsyncDisposalCancelsInFlightExchangeBeforeClosingRuntimeState()
    {
        var transport = new ControlledTransport();
        var runtime = CreateRuntime(transport);
        await runtime.SaveLocalAsync(NewMutation());
        var synchronization = runtime.SetConnectivityAsync(true);
        await transport.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var disposal = runtime.DisposeAsync().AsTask();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => synchronization);
        await disposal.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => runtime.SynchronizeAsync());
    }

    [Fact]
    public async Task LocalProjectionQueuedBeforeTargetConnectsAndDrainsAfterAttach()
    {
        var options = new EdgeRuntimeOptions
        {
            DeviceId = _deviceId, AvatarId = _avatarId, DatabasePath = _path,
            LocalReplicationTargetIds = new[] { RecordingLocalTarget.Id },
            PayloadSerializer = new EdgePayloadSerializer(EdgeTestJsonContext.Default)
        };
        await using var runtime = new OASISEdgeRuntime(options, new RecordingTransport());

        var saved = await runtime.SaveLocalAsync(NewMutation(), default);
        saved.IsError.Should().BeFalse(saved.Message);
        (await runtime.LocalStore.GetPendingLocalReplicationCountAsync(RecordingLocalTarget.Id, default))
            .Result.Should().Be(1);

        var target = new RecordingLocalTarget();
        runtime.AttachLocalReplicationTarget(target);
        await target.Applied.Task.WaitAsync(TimeSpan.FromSeconds(5));
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if ((await runtime.LocalStore.GetPendingLocalReplicationCountAsync(RecordingLocalTarget.Id, default)).Result == 0)
                break;
            await Task.Delay(10);
        }

        target.Operations.Should().ContainSingle(x => x.OperationId == saved.Result.OperationId);
        (await runtime.LocalStore.GetPendingLocalReplicationCountAsync(RecordingLocalTarget.Id, default))
            .Result.Should().Be(0);
    }

    [Fact]
    public async Task AvatarPreferencesCommandSurvivesRestartAsTypedDurableIntent()
    {
        Guid operationId = Guid.NewGuid();
        using (var first = CreateRuntime(new RecordingTransport()))
        {
            var queued = await first.Entities.QueueCommandAsync(new EdgeCommandRequest<HyperDriveAvatarPreferences>
            {
                OperationId = operationId, EntityId = _avatarId,
                EntityType = HyperDriveEntityTypes.AvatarPreferences, VersionId = Guid.NewGuid(),
                Payload = new HyperDriveAvatarPreferences
                {
                    MasterVolume = 0.35f, GraphicsPreset = "Ultra",
                    ViewPresets = new[] { new HyperDriveViewPreset { Name = "Geo quests", Tab = "quests" } }
                }
            });
            queued.IsError.Should().BeFalse(queued.Message);
        }

        using var restarted = CreateRuntime(new RecordingTransport());
        var pending = (await restarted.LocalStore.ReadPendingOperationsAsync(10, default)).Result
            .Should().ContainSingle(x => x.OperationId == operationId).Subject;
        var preferences = HyperDriveJson.Deserialize<HyperDriveAvatarPreferences>(pending.PayloadJson);
        preferences.MasterVolume.Should().Be(0.35f);
        preferences.GraphicsPreset.Should().Be("Ultra");
        preferences.ViewPresets.Should().ContainSingle(x => x.Name == "Geo quests");
        (await restarted.Entities.LoadAsync<HyperDriveAvatarPreferences>(
            HyperDriveEntityTypes.AvatarPreferences, _avatarId)).Result.Should().BeNull(
                "the command is an intent until the hosted ONODE confirms the projection");
    }

    [Fact]
    public void UnconfiguredLocalTargetIsRejectedBeforeItCanCreateAnUndurablePath()
    {
        using var runtime = CreateRuntime(new RecordingTransport());

        var attach = () => runtime.AttachLocalReplicationTarget(new RecordingLocalTarget());

        attach.Should().Throw<InvalidOperationException>().WithMessage("*not configured before the durable store opened*");
    }

    private OASISEdgeRuntime CreateRuntime(IHyperDriveSyncTransport transport, int maximumOperations = 100) => new(
        new EdgeRuntimeOptions { DeviceId = _deviceId, AvatarId = _avatarId, DatabasePath = _path,
            MaximumOperationsPerExchange = maximumOperations, MaximumRemoteChangesPerExchange = 10,
            PayloadSerializer = new EdgePayloadSerializer(EdgeTestJsonContext.Default) }, transport);

    private EdgeLocalMutation NewMutation() => new()
    {
        OperationId = Guid.NewGuid(), DeviceId = _deviceId, AvatarId = _avatarId,
        EntityId = Guid.NewGuid(), EntityType = "holon", Kind = SyncOperationKind.Upsert,
        LocalVersionId = Guid.NewGuid(), PayloadJson = "{}", CreatedUtc = DateTime.UtcNow
    };

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        { var file = _path + suffix; if (File.Exists(file)) File.Delete(file); }
    }

    private sealed class RecordingTransport : IHyperDriveSyncTransport, IHyperDrivePeerBindingTransport
    {
        public int CallCount { get; private set; }
        public BindHyperDrivePeerRequest? BindingRequest { get; private set; }
        public Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new OASISResult<SyncExchangeResponse>(new SyncExchangeResponse
            {
                OperationResults = request.Operations.Select(x => new SyncOperationResult
                { OperationId = x.OperationId, Disposition = SyncOperationDisposition.Accepted, ResultVersionId = x.VersionId }).ToArray(),
                NextPullCheckpoint = CallCount.ToString(), RemoteChanges = Array.Empty<SyncRemoteChange>()
            }));
        }

        public Task<OASISResult<bool>> BindPeerAsync(BindHyperDrivePeerRequest request, CancellationToken cancellationToken)
        {
            BindingRequest = request;
            return Task.FromResult(new OASISResult<bool> { Result = true });
        }
    }

    private sealed class ConflictTransport : IHyperDriveSyncTransport
    {
        public Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request,
            CancellationToken cancellationToken) => Task.FromResult(new OASISResult<SyncExchangeResponse>(new SyncExchangeResponse
        {
            OperationResults = request.Operations.Select(x => new SyncOperationResult
            {
                OperationId = x.OperationId, Disposition = SyncOperationDisposition.Conflict,
                ResultVersionId = Guid.NewGuid(), Code = "BASE_VERSION_CONFLICT"
            }).ToArray(),
            NextPullCheckpoint = "conflict-checkpoint",
            RemoteChanges = Array.Empty<SyncRemoteChange>()
        }));
    }

    private sealed class DivergentEntityTransport : IHyperDriveSyncTransport
    {
        private readonly Guid _entityId;
        private readonly Guid _initialVersion;
        private readonly Guid _hostedVersion;
        private readonly Guid _localOperationId;
        private readonly Guid _mergeOperationId;
        private readonly Guid _mergedVersion;
        private bool _hostedEditApplied;
        private int _callCount;

        public DivergentEntityTransport(Guid entityId, Guid initialVersion, Guid hostedVersion,
            Guid localOperationId, Guid mergeOperationId, Guid mergedVersion)
        {
            _entityId = entityId;
            _initialVersion = initialVersion;
            _hostedVersion = hostedVersion;
            _localOperationId = localOperationId;
            _mergeOperationId = mergeOperationId;
            _mergedVersion = mergedVersion;
        }

        public Guid AcceptedMergeBaseVersion { get; private set; }
        public void ApplyIndependentHostedEdit() => _hostedEditApplied = true;

        public Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request,
            CancellationToken cancellationToken)
        {
            _callCount++;
            if (_callCount == 1)
                return Response(Array.Empty<SyncOperationResult>(), new SyncRemoteChange
                {
                    ChangeId = "initial", EntityId = _entityId, EntityType = "test-profile",
                    Kind = SyncOperationKind.Upsert, VersionId = _initialVersion,
                    PayloadJson = "{\"Name\":\"shared\",\"Progress\":1}", ChangedUtc = DateTime.UtcNow
                });

            if (_hostedEditApplied && request.Operations.Any(x => x.OperationId == _localOperationId))
                return Response(new[]
                {
                    new SyncOperationResult
                    {
                        OperationId = _localOperationId, Disposition = SyncOperationDisposition.Conflict,
                        ResultVersionId = _hostedVersion, Code = "BASE_VERSION_CONFLICT",
                        Message = "The hosted entity changed while this device was offline."
                    }
                }, new SyncRemoteChange
                {
                    ChangeId = "hosted-edit", EntityId = _entityId, EntityType = "test-profile",
                    Kind = SyncOperationKind.Upsert, PreviousVersionId = _initialVersion,
                    VersionId = _hostedVersion,
                    PayloadJson = "{\"Name\":\"hosted edit\",\"Progress\":2}", ChangedUtc = DateTime.UtcNow
                });

            var merge = request.Operations.Single(x => x.OperationId == _mergeOperationId);
            merge.VersionId.Should().Be(_mergedVersion);
            AcceptedMergeBaseVersion = merge.BaseVersionId;
            return Response(new[]
            {
                new SyncOperationResult
                {
                    OperationId = merge.OperationId, Disposition = SyncOperationDisposition.Accepted,
                    ResultVersionId = merge.VersionId
                }
            });
        }

        private Task<OASISResult<SyncExchangeResponse>> Response(
            IReadOnlyList<SyncOperationResult> operationResults, params SyncRemoteChange[] changes) =>
            Task.FromResult(new OASISResult<SyncExchangeResponse>(new SyncExchangeResponse
            {
                OperationResults = operationResults,
                RemoteChanges = changes,
                NextPullCheckpoint = _callCount.ToString()
            }));
    }

    private sealed class RecordingLocalTarget : IHyperDriveLocalReplicationTarget
    {
        public const string Id = "HoloOASIS.Edge";
        public string LocalReplicationTargetId => Id;
        public List<SyncOperation> Operations { get; } = new();
        public TaskCompletionSource<bool> Applied { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<OASISResult<bool>> ApplyLocalMutationAsync(SyncOperation operation,
            CancellationToken cancellationToken)
        {
            Operations.Add(operation);
            Applied.TrySetResult(true);
            return Task.FromResult(new OASISResult<bool>(true));
        }
    }

    private sealed class DelayedCommandOutcomeTransport : IHyperDriveSyncTransport
    {
        private readonly Guid _operationId;
        public DelayedCommandOutcomeTransport(Guid operationId) => _operationId = operationId;
        public int CallCount { get; private set; }
        public Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            var changes = CallCount < 3 ? Array.Empty<SyncRemoteChange>() : new[]
            {
                new SyncRemoteChange
                {
                    ChangeId = "outcome", EntityId = _operationId,
                    EntityType = HyperDriveEntityTypes.CommandResult, Kind = SyncOperationKind.Upsert,
                    VersionId = Guid.NewGuid(), PayloadJson = "{\"succeeded\":true}",
                    ChangedUtc = DateTime.UtcNow
                }
            };
            return Task.FromResult(new OASISResult<SyncExchangeResponse>(new SyncExchangeResponse
            {
                OperationResults = request.Operations.Select(x => new SyncOperationResult
                {
                    OperationId = x.OperationId, Disposition = SyncOperationDisposition.Accepted,
                    ResultVersionId = x.VersionId
                }).ToArray(),
                RemoteChanges = changes, NextPullCheckpoint = CallCount.ToString()
            }));
        }
    }

    private sealed class GrantTransport : IHyperDriveSyncTransport, IHyperDriveOfflineSessionGrantTransport,
        IHyperDriveOfflineSessionAwareTransport
    {
        private readonly Guid _avatarId;
        private readonly Guid _deviceId;
        public IssueHyperDriveOfflineSessionGrantRequest? Request { get; private set; }
        public HyperDriveOfflineSessionGrant? AttachedGrant { get; set; }
        public GrantTransport(Guid avatarId, Guid deviceId) { _avatarId = avatarId; _deviceId = deviceId; }
        public Task<OASISResult<HyperDriveOfflineSessionGrant>> IssueOfflineSessionGrantAsync(
            IssueHyperDriveOfflineSessionGrantRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new OASISResult<HyperDriveOfflineSessionGrant>(new HyperDriveOfflineSessionGrant
            {
                GrantId = "host-grant", AvatarId = _avatarId, DeviceId = _deviceId,
                IssuedUtc = DateTime.UtcNow.AddMinutes(-1), ExpiresUtc = DateTime.UtcNow.AddMinutes(29),
                Scopes = request.RequestedScopes, Signature = "host-signature"
            }));
        }
        public Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request,
            CancellationToken cancellationToken) => Task.FromResult(new OASISResult<SyncExchangeResponse>(new SyncExchangeResponse()));
        public void SetOfflineSessionGrant(HyperDriveOfflineSessionGrant grant) => AttachedGrant = grant;
    }

    private sealed class AcceptGrantValidator : IEdgeOfflineGrantValidator
    {
        public Task<OASISResult<bool>> ValidateAsync(HyperDriveOfflineSessionGrant grant,
            CancellationToken cancellationToken) => Task.FromResult(new OASISResult<bool>(true));
    }

    private sealed class RuntimeSecureStore : IEdgeSecureSessionStore
    {
        public HyperDriveOfflineSessionGrant? Grant { get; private set; }
        public Task<OASISResult<bool>> SaveAsync(HyperDriveOfflineSessionGrant grant, CancellationToken cancellationToken)
        { Grant = grant; return Task.FromResult(new OASISResult<bool>(true)); }
        public Task<OASISResult<HyperDriveOfflineSessionGrant>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new OASISResult<HyperDriveOfflineSessionGrant>(Grant!));
        public Task<OASISResult<bool>> DeleteAsync(CancellationToken cancellationToken)
        { Grant = null; return Task.FromResult(new OASISResult<bool>(true)); }
    }

    private sealed class OfflineTransport : IHyperDriveSyncTransport
    {
        private readonly string _errorCode;
        public OfflineTransport(string errorCode = "HYPERDRIVE_NETWORK_UNAVAILABLE") => _errorCode = errorCode;
        public Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request,
            CancellationToken cancellationToken) => Task.FromResult(new OASISResult<SyncExchangeResponse>
        {
            IsError = true, ErrorCount = 1, ErrorCode = _errorCode,
            Message = "Hosted OASIS is unreachable."
        });
    }

    private sealed class RecoveringTransport : IHyperDriveSyncTransport
    {
        public int CallCount { get; private set; }
        public Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (CallCount == 1)
                return Task.FromResult(new OASISResult<SyncExchangeResponse>
                {
                    IsError = true, ErrorCount = 1, ErrorCode = "HYPERDRIVE_REMOTE_UNAVAILABLE",
                    Message = "Hosted OASIS is temporarily unavailable."
                });
            return Task.FromResult(new OASISResult<SyncExchangeResponse>(new SyncExchangeResponse
            {
                OperationResults = request.Operations.Select(x => new SyncOperationResult
                {
                    OperationId = x.OperationId, Disposition = SyncOperationDisposition.Accepted,
                    ResultVersionId = x.VersionId
                }).ToArray(),
                NextPullCheckpoint = "recovered", RemoteChanges = Array.Empty<SyncRemoteChange>()
            }));
        }
    }

    private sealed class TestNodeIdentity : IEdgeNodeIdentity, IDisposable
    {
        private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        public TestNodeIdentity(string? forcedNodeId = null)
        {
            PublicKey = Convert.ToBase64String(_key.ExportSubjectPublicKeyInfo());
            NodeId = forcedNodeId ?? HyperDrivePeerBindingProof.DeriveNodeId(Convert.FromBase64String(PublicKey));
        }
        public string NodeId { get; }
        public string PublicKey { get; }
        public Task<OASISResult<string>> SignAsync(string message, CancellationToken cancellationToken) =>
            Task.FromResult(new OASISResult<string>
            {
                Result = Convert.ToBase64String(_key.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256))
            });
        public void Dispose() => _key.Dispose();
    }

    internal sealed class TestEntity
    {
        public string? Name { get; set; }
        public int Progress { get; set; }
    }

    private sealed class TestConnectivityMonitor : IEdgeConnectivityMonitor
    {
        public TestConnectivityMonitor(bool isOnline) { IsOnline = isOnline; }
        public bool IsOnline { get; private set; }
        public event EventHandler<EdgeConnectivityChangedEventArgs>? ConnectivityChanged;
        public void SetOnline(bool isOnline)
        {
            IsOnline = isOnline;
            ConnectivityChanged?.Invoke(this, new EdgeConnectivityChangedEventArgs(isOnline));
        }
    }

    private sealed class ControlledTransport : IHyperDriveSyncTransport
    {
        public int CallCount { get; private set; }
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Started.TrySetResult(true);
            await Release.Task.WaitAsync(cancellationToken);
            return new OASISResult<SyncExchangeResponse>(new SyncExchangeResponse
            {
                OperationResults = request.Operations.Select(x => new SyncOperationResult
                { OperationId = x.OperationId, Disposition = SyncOperationDisposition.Accepted, ResultVersionId = x.VersionId }).ToArray(),
                NextPullCheckpoint = "controlled", RemoteChanges = Array.Empty<SyncRemoteChange>()
            });
        }
    }
}
