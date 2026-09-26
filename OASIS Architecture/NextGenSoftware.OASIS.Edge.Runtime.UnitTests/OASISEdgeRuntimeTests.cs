using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS;
using NextGenSoftware.OASIS.Common;
using Xunit;
using System.Security.Cryptography;
using System.Text;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

public sealed class OASISEdgeRuntimeTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"oasis-edge-runtime-{Guid.NewGuid():N}.db");
    private readonly Guid _deviceId = Guid.NewGuid();
    private readonly Guid _avatarId = Guid.NewGuid();

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
        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(outcome);
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
