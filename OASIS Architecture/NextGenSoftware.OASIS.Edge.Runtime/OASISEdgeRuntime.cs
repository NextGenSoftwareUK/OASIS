using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    /// <summary>
    /// Mobile composition root. Local mutations never depend on connectivity; synchronization is
    /// explicit and connectivity-triggered, while transport authentication is configured by the host.
    /// </summary>
    public sealed class OASISEdgeRuntime : IDisposable, IAsyncDisposable
    {
        private readonly EdgeRuntimeOptions _options;
        private readonly HyperDriveSyncCoordinator _coordinator;
        private readonly IHyperDrivePeerBindingTransport _peerBindingTransport;
        private readonly IHyperDriveOfflineSessionGrantTransport _offlineGrantTransport;
        private readonly IHyperDriveOfflineSessionAwareTransport _offlineSessionAwareTransport;
        private readonly SemaphoreSlim _runGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _syncWake = new SemaphoreSlim(0, 1);
        private readonly CancellationTokenSource _lifetimeCancellation = new CancellationTokenSource();
        private readonly object _connectivityGate = new object();
        private IEdgeConnectivityMonitor _connectivityMonitor;
        private CancellationTokenSource _recoveryCancellation;
        private Task _recoveryTask;
        private long _connectivityRevision;
        private bool _suspended;
        private bool _disposed;

        public EdgeSQLiteSyncStateStore LocalStore { get; }
        public EdgeEntityRepository Entities { get; }
        public EdgeOfflineSessionManager Sessions { get; }
        public EdgeRuntimeStatus Status { get; } = new EdgeRuntimeStatus();
        public event EventHandler<EdgeRuntimeStatus> StatusChanged;

        public OASISEdgeRuntime(EdgeRuntimeOptions options, IHyperDriveSyncTransport transport,
            IEdgeSecureSessionStore secureSessionStore = null,
            IEdgeOfflineGrantValidator offlineGrantValidator = null,
            IHyperDriveClock clock = null,
            IHyperDrivePeerBindingTransport peerBindingTransport = null,
            IHyperDriveOfflineSessionGrantTransport offlineGrantTransport = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (options.DeviceId == Guid.Empty) throw new ArgumentException("A stable device id is required.", nameof(options));
            if (options.AvatarId == Guid.Empty) throw new ArgumentException("An authenticated avatar id is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.DatabasePath)) throw new ArgumentException("A database path is required.", nameof(options));
            if (options.MaximumOperationsPerExchange <= 0 || options.MaximumRemoteChangesPerExchange <= 0 || options.MaximumExchangesPerRun <= 0)
                throw new ArgumentException("Edge synchronization limits must be greater than zero.", nameof(options));
            if (options.HostedServiceRecoveryInterval <= TimeSpan.Zero)
                throw new ArgumentException("The hosted-service recovery interval must be greater than zero.", nameof(options));
            if (options.MaximumHostedServiceRecoveryInterval < options.HostedServiceRecoveryInterval)
                throw new ArgumentException("The maximum hosted-service recovery interval cannot be shorter than the initial interval.", nameof(options));

            LocalStore = new EdgeSQLiteSyncStateStore(options.DatabasePath);
            Entities = new EdgeEntityRepository(options.DeviceId, options.AvatarId, LocalStore,
                SaveLocalAsync, options.PayloadSerializer ?? new EdgePayloadSerializer());
            _coordinator = new HyperDriveSyncCoordinator(options.DeviceId, options.AvatarId, LocalStore,
                transport ?? throw new ArgumentNullException(nameof(transport)));
            _peerBindingTransport = peerBindingTransport ?? transport as IHyperDrivePeerBindingTransport;
            _offlineGrantTransport = offlineGrantTransport ?? transport as IHyperDriveOfflineSessionGrantTransport;
            _offlineSessionAwareTransport = transport as IHyperDriveOfflineSessionAwareTransport;
            if ((secureSessionStore == null) != (offlineGrantValidator == null))
                throw new ArgumentException("Secure session storage and offline grant validation must be configured together.");
            if (secureSessionStore != null)
                Sessions = new EdgeOfflineSessionManager(options.AvatarId, options.DeviceId, secureSessionStore,
                    offlineGrantValidator, clock ?? new SystemHyperDriveClock());
            Status.Connectivity = EdgeConnectivityState.Offline;
            Status.Synchronization = EdgeSynchronizationState.Idle;
        }

        public async Task<OASISResult<HyperDriveOfflineSessionGrant>> AcquireOfflineSessionAsync(
            IReadOnlyList<string> scopes, int lifetimeMinutes, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (Sessions == null)
                return Error<HyperDriveOfflineSessionGrant>("EDGE_OFFLINE_SESSION_NOT_CONFIGURED",
                    "Platform-secure session storage and a pinned host signing key are required.");
            if (_offlineGrantTransport == null)
                return Error<HyperDriveOfflineSessionGrant>("EDGE_OFFLINE_GRANT_TRANSPORT_UNAVAILABLE",
                    "The configured authenticated transport cannot request offline-session grants.");

            var issued = await _offlineGrantTransport.IssueOfflineSessionGrantAsync(
                new IssueHyperDriveOfflineSessionGrantRequest
                {
                    DeviceId = _options.DeviceId,
                    RequestedScopes = scopes ?? Array.Empty<string>(),
                    RequestedLifetimeMinutes = lifetimeMinutes
                }, cancellationToken).ConfigureAwait(false);
            if (issued == null || issued.IsError || issued.Result == null)
                return Error<HyperDriveOfflineSessionGrant>(issued?.ErrorCode ?? "EDGE_OFFLINE_GRANT_ISSUANCE_FAILED",
                    issued?.Message ?? "The hosted ONODE did not issue an offline-session grant.");

            var cached = await Sessions.CacheAuthenticatedSessionAsync(issued.Result, cancellationToken).ConfigureAwait(false);
            if (cached == null || cached.IsError || !cached.Result)
                return Error<HyperDriveOfflineSessionGrant>(cached?.ErrorCode ?? "EDGE_OFFLINE_GRANT_CACHE_FAILED",
                    cached?.Message ?? "The issued offline-session grant could not be secured on this device.");
            _offlineSessionAwareTransport?.SetOfflineSessionGrant(issued.Result);
            return issued;
        }

        public async Task<OASISResult<HyperDriveOfflineSessionGrant>> ResumeOfflineSessionAsync(
            IReadOnlyList<string> requiredScopes, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (Sessions == null)
                return Error<HyperDriveOfflineSessionGrant>("EDGE_OFFLINE_SESSION_NOT_CONFIGURED",
                    "Secure offline-session storage and validation are not configured.");
            var resumed = await Sessions.ResumeAsync(requiredScopes ?? Array.Empty<string>(), cancellationToken)
                .ConfigureAwait(false);
            if (resumed != null && !resumed.IsError && resumed.Result != null)
                _offlineSessionAwareTransport?.SetOfflineSessionGrant(resumed.Result);
            return resumed;
        }

        public async Task<OASISResult<bool>> BindOnetIdentityAsync(IEdgeNodeIdentity identity,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (_peerBindingTransport == null)
                return Error<bool>("EDGE_PEER_BINDING_TRANSPORT_UNAVAILABLE",
                    "The configured synchronization transport does not support ONET peer registration.");
            if (identity == null || string.IsNullOrWhiteSpace(identity.NodeId) || string.IsNullOrWhiteSpace(identity.PublicKey))
                return Error<bool>("EDGE_NODE_IDENTITY_INVALID", "A platform-secure ONET node identity is required.");

            byte[] publicKey;
            try { publicKey = Convert.FromBase64String(identity.PublicKey); }
            catch (FormatException) { return Error<bool>("EDGE_NODE_PUBLIC_KEY_INVALID", "The ONET public key is not valid base64."); }
            if (!string.Equals(HyperDrivePeerBindingProof.DeriveNodeId(publicKey), identity.NodeId, StringComparison.Ordinal))
                return Error<bool>("EDGE_NODE_ID_MISMATCH", "The ONET node id does not fingerprint its public key.");

            var proof = HyperDrivePeerBindingProof.BuildMessage(_options.AvatarId, _options.DeviceId, identity.NodeId);
            var signature = await identity.SignAsync(proof, cancellationToken).ConfigureAwait(false);
            if (signature == null || signature.IsError || string.IsNullOrWhiteSpace(signature.Result))
                return Error<bool>(signature?.ErrorCode ?? "EDGE_NODE_SIGNING_FAILED",
                    signature?.Message ?? "The platform identity did not return a binding signature.");

            return await _peerBindingTransport.BindPeerAsync(new BindHyperDrivePeerRequest
            {
                DeviceId = _options.DeviceId,
                NodeId = identity.NodeId,
                PublicKey = identity.PublicKey,
                Signature = signature.Result
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<OASISResult<SyncOperation>> SaveLocalAsync(EdgeLocalMutation mutation, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (mutation != null && (mutation.DeviceId != _options.DeviceId || mutation.AvatarId != _options.AvatarId))
                return Error<SyncOperation>("EDGE_MUTATION_SCOPE_MISMATCH", "The mutation does not belong to this runtime device and avatar.");
            var saved = await LocalStore.ApplyLocalMutationAsync(mutation, cancellationToken).ConfigureAwait(false);
            if (saved != null && !saved.IsError)
            {
                var pending = await RefreshPendingCountAsync(cancellationToken).ConfigureAwait(false);
                if (pending.IsError)
                {
                    Status.LastErrorCode = pending.ErrorCode;
                    Status.LastMessage = pending.Message;
                }
                Status.Synchronization = EdgeSynchronizationState.Pending;
                OnStatusChanged();
                // A committed online mutation wakes the one runtime sync loop. Hosts never own a
                // second replay worker, and offline writes do not turn into unbounded network retries.
                lock (_connectivityGate)
                    if (Status.Connectivity == EdgeConnectivityState.Online && _syncWake.CurrentCount == 0)
                        _syncWake.Release();
            }
            return saved;
        }

        public Task<OASISResult<IReadOnlyList<EdgeSyncConflict>>> ReadUnresolvedConflictsAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return LocalStore.ReadUnresolvedConflictsAsync(cancellationToken);
        }

        public Task<OASISResult<SyncOperation>> ResolveConflictAsync(EdgeConflictResolution resolution,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return LocalStore.ResolveConflictAsync(resolution, cancellationToken);
        }

        public async Task<OASISResult<SyncCycleResult>> StartConnectivityMonitoringAsync(
            IEdgeConnectivityMonitor monitor, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (monitor == null)
                return Error<SyncCycleResult>("EDGE_CONNECTIVITY_MONITOR_REQUIRED", "A platform connectivity monitor is required.");
            if (_connectivityMonitor != null && !ReferenceEquals(_connectivityMonitor, monitor))
                return Error<SyncCycleResult>("EDGE_CONNECTIVITY_MONITOR_ALREADY_STARTED",
                    "A different platform connectivity monitor is already attached.");

            if (_connectivityMonitor == null)
            {
                _connectivityMonitor = monitor;
                _connectivityMonitor.ConnectivityChanged += ConnectivityMonitorOnConnectivityChanged;
                StartHostedRecoveryLoop();
            }
            return await SetConnectivityAsync(monitor.IsOnline, cancellationToken).ConfigureAwait(false);
        }

        public void StopConnectivityMonitoring()
        {
            ThrowIfDisposed();
            if (_connectivityMonitor == null) return;
            _connectivityMonitor.ConnectivityChanged -= ConnectivityMonitorOnConnectivityChanged;
            _connectivityMonitor = null;
            StopHostedRecoveryLoopAsync().GetAwaiter().GetResult();
        }

        public async Task<OASISResult<SyncCycleResult>> SuspendAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            lock (_connectivityGate)
            {
                _suspended = true;
                _connectivityRevision++;
            }
            await StopHostedRecoveryLoopAsync().ConfigureAwait(false);
            return await SetConnectivityAsync(false, cancellationToken).ConfigureAwait(false);
        }

        public async Task<OASISResult<SyncCycleResult>> ResumeAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            lock (_connectivityGate) _suspended = false;
            if (_connectivityMonitor != null) StartHostedRecoveryLoop();
            return await SetConnectivityAsync(_connectivityMonitor?.IsOnline == true, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<OASISResult<SyncCycleResult>> SetConnectivityAsync(bool isOnline, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            long revision;
            bool suspendedOnline;
            lock (_connectivityGate)
            {
                suspendedOnline = _suspended && isOnline;
                if (suspendedOnline) isOnline = false;
                // A platform network signal means the host may be reachable; it is not proof that the
                // hosted OASIS accepted an exchange. Publish Connecting until the first exchange succeeds.
                if (!isOnline) Status.Connectivity = EdgeConnectivityState.Offline;
                else if (Status.Connectivity != EdgeConnectivityState.Online) Status.Connectivity = EdgeConnectivityState.Connecting;
                revision = ++_connectivityRevision;
            }
            if (!isOnline)
            {
                Status.Synchronization = EdgeSynchronizationState.Pending;
                Status.LastMessage = suspendedOnline
                    ? "Suspended. Local mode remains active and durable changes stay queued."
                    : "Offline. Local changes remain durable and queued.";
                OnStatusChanged();
                return new OASISResult<SyncCycleResult> { Result = new SyncCycleResult(), Message = Status.LastMessage };
            }
            OnStatusChanged();
            return await SynchronizeAsync(revision, cancellationToken).ConfigureAwait(false);
        }

        public async Task<OASISResult<SyncCycleResult>> SynchronizeAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            long revision;
            lock (_connectivityGate)
            {
                if (Status.Connectivity != EdgeConnectivityState.Online)
                    return Error<SyncCycleResult>("EDGE_OFFLINE", "Synchronization was not attempted because the runtime is offline.");
                revision = _connectivityRevision;
            }
            return await SynchronizeAsync(revision, cancellationToken).ConfigureAwait(false);
        }

        private async Task<OASISResult<SyncCycleResult>> SynchronizeAsync(long connectivityRevision,
            CancellationToken cancellationToken)
        {
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _lifetimeCancellation.Token);
            CancellationToken effectiveCancellation = linkedCancellation.Token;
            await _runGate.WaitAsync(effectiveCancellation).ConfigureAwait(false);
            try
            {
                if (!IsCurrentOnlineRevision(connectivityRevision))
                    return await ConnectivityChangedResultAsync(effectiveCancellation).ConfigureAwait(false);
                Status.Synchronization = EdgeSynchronizationState.Synchronizing;
                Status.LastErrorCode = null;
                OnStatusChanged();
                SyncCycleResult aggregate = new SyncCycleResult();
                for (int exchange = 0; exchange < _options.MaximumExchangesPerRun; exchange++)
                {
                    var cycle = await _coordinator.SynchronizeOnceAsync(_options.MaximumOperationsPerExchange,
                        _options.MaximumRemoteChangesPerExchange, effectiveCancellation).ConfigureAwait(false);
                    if (cycle.IsError || cycle.Result == null)
                    {
                        Status.LastErrorCode = cycle.ErrorCode ?? "EDGE_SYNC_FAILED";
                        if (IsConnectivityFailure(Status.LastErrorCode))
                        {
                            lock (_connectivityGate)
                            {
                                Status.Connectivity = EdgeConnectivityState.Offline;
                                _connectivityRevision++;
                            }
                            Status.Synchronization = EdgeSynchronizationState.Pending;
                            Status.LastMessage = "Hosted OASIS is unreachable. Local mode remains active and durable changes are queued for automatic synchronization after connectivity returns.";
                        }
                        else
                        {
                            Status.Synchronization = EdgeSynchronizationState.Error;
                            Status.LastMessage = cycle.Message;
                        }
                        OnStatusChanged();
                        return cycle;
                    }
                    lock (_connectivityGate)
                    {
                        if (_connectivityRevision == connectivityRevision &&
                            Status.Connectivity == EdgeConnectivityState.Connecting)
                            Status.Connectivity = EdgeConnectivityState.Online;
                    }
                    if (!IsCurrentOnlineRevision(connectivityRevision))
                        return await ConnectivityChangedResultAsync(effectiveCancellation).ConfigureAwait(false);
                    OnStatusChanged(); // The first successful exchange proves reconnection while sync is still active.
                    aggregate.SentOperationCount += cycle.Result.SentOperationCount;
                    aggregate.AcceptedOperationCount += cycle.Result.AcceptedOperationCount;
                    aggregate.ConflictCount += cycle.Result.ConflictCount;
                    aggregate.RejectedOperationCount += cycle.Result.RejectedOperationCount;
                    aggregate.AppliedRemoteChangeCount += cycle.Result.AppliedRemoteChangeCount;
                    aggregate.PullCheckpoint = cycle.Result.PullCheckpoint;
                    aggregate.HasMoreRemoteChanges = cycle.Result.HasMoreRemoteChanges;
                    bool operationBatchWasFull = cycle.Result.SentOperationCount == _options.MaximumOperationsPerExchange;
                    if (!cycle.Result.HasMoreRemoteChanges && !operationBatchWasFull)
                    {
                        var unresolved = await LocalStore.ReadUnresolvedConflictsAsync(effectiveCancellation).ConfigureAwait(false);
                        if (unresolved == null || unresolved.IsError || unresolved.Result == null)
                        {
                            Status.Synchronization = EdgeSynchronizationState.Error;
                            Status.LastErrorCode = unresolved?.ErrorCode ?? "EDGE_CONFLICT_STATE_FAILED";
                            Status.LastMessage = unresolved?.Message ?? "The durable conflict state could not be read.";
                            OnStatusChanged();
                            return Error<SyncCycleResult>(Status.LastErrorCode, Status.LastMessage);
                        }
                        if (!IsCurrentOnlineRevision(connectivityRevision))
                            return await ConnectivityChangedResultAsync(effectiveCancellation).ConfigureAwait(false);
                        var pending = await RefreshPendingCountAsync(effectiveCancellation).ConfigureAwait(false);
                        if (pending.IsError)
                            return Error<SyncCycleResult>(pending.ErrorCode, pending.Message);
                        var unsent = await LocalStore.GetPendingOperationCountAsync(effectiveCancellation).ConfigureAwait(false);
                        if (unsent.IsError) return Error<SyncCycleResult>(unsent.ErrorCode, unsent.Message);
                        if (unsent.Result > 0)
                            continue;
                        bool connectivityChangedAfterPendingCheck;
                        lock (_connectivityGate)
                        {
                            connectivityChangedAfterPendingCheck = Status.Connectivity != EdgeConnectivityState.Online ||
                                _connectivityRevision != connectivityRevision;
                            if (!connectivityChangedAfterPendingCheck)
                            {
                                Status.Synchronization = pending.Result == 0 && unresolved.Result.Count == 0 && cycle.Result.RejectedOperationCount == 0
                                    ? EdgeSynchronizationState.Synchronized : EdgeSynchronizationState.Pending;
                                Status.LastSuccessfulSyncUtc = DateTime.UtcNow;
                                Status.LastMessage = "Edge synchronization completed.";
                            }
                        }
                        if (connectivityChangedAfterPendingCheck)
                            return await ConnectivityChangedResultAsync(effectiveCancellation).ConfigureAwait(false);
                        OnStatusChanged();
                        return new OASISResult<SyncCycleResult>(aggregate) { Message = Status.LastMessage };
                    }
                }

                Status.Synchronization = EdgeSynchronizationState.Pending;
                Status.LastMessage = "The bounded synchronization run completed with more durable work pending.";
                var remaining = await RefreshPendingCountAsync(effectiveCancellation).ConfigureAwait(false);
                if (remaining.IsError)
                    return Error<SyncCycleResult>(remaining.ErrorCode, remaining.Message);
                OnStatusChanged();
                return new OASISResult<SyncCycleResult>(aggregate) { Message = Status.LastMessage };
            }
            finally { _runGate.Release(); }
        }

        public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            if (_connectivityMonitor != null)
                _connectivityMonitor.ConnectivityChanged -= ConnectivityMonitorOnConnectivityChanged;
            _connectivityMonitor = null;
            await StopHostedRecoveryLoopAsync().ConfigureAwait(false);
            _lifetimeCancellation.Cancel();
            await _runGate.WaitAsync().ConfigureAwait(false);
            try { LocalStore.Dispose(); }
            finally
            {
                _runGate.Release();
                _runGate.Dispose();
                _lifetimeCancellation.Dispose();
            }
        }

        private void OnStatusChanged() => StatusChanged?.Invoke(this, Status);
        private bool IsCurrentOnlineRevision(long revision)
        {
            lock (_connectivityGate)
                return (Status.Connectivity == EdgeConnectivityState.Connecting ||
                        Status.Connectivity == EdgeConnectivityState.Online) &&
                    _connectivityRevision == revision;
        }
        private async Task<OASISResult<SyncCycleResult>> ConnectivityChangedResultAsync(CancellationToken cancellationToken)
        {
            Status.Synchronization = EdgeSynchronizationState.Pending;
            Status.LastMessage = "Connectivity changed during synchronization. Durable work remains queued for the next online transition.";
            var pending = await RefreshPendingCountAsync(cancellationToken).ConfigureAwait(false);
            if (pending.IsError)
                return Error<SyncCycleResult>(pending.ErrorCode, pending.Message);
            OnStatusChanged();
            return new OASISResult<SyncCycleResult> { Result = new SyncCycleResult(), Message = Status.LastMessage };
        }
        private async void ConnectivityMonitorOnConnectivityChanged(object sender, EdgeConnectivityChangedEventArgs args)
        {
            if (_disposed || _suspended) return;
            try
            {
                await SetConnectivityAsync(args.IsOnline).ConfigureAwait(false);
            }
            catch (ObjectDisposedException) when (_disposed) { }
            catch (Exception ex)
            {
                Status.Synchronization = EdgeSynchronizationState.Error;
                Status.LastErrorCode = "EDGE_CONNECTIVITY_TRANSITION_FAILED";
                Status.LastMessage = $"The connectivity transition could not be processed: {ex.Message}";
                OnStatusChanged();
            }
        }
        private void StartHostedRecoveryLoop()
        {
            if (_suspended || _recoveryTask != null) return;
            _recoveryCancellation = new CancellationTokenSource();
            _recoveryTask = RunHostedServiceRecoveryLoopAsync(_recoveryCancellation.Token);
        }
        private async Task StopHostedRecoveryLoopAsync()
        {
            var cancellation = _recoveryCancellation;
            var task = _recoveryTask;
            if (cancellation == null && task == null) return;
            cancellation?.Cancel();
            if (task != null)
            {
                try { await task.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
            if (ReferenceEquals(_recoveryTask, task))
            {
                _recoveryTask = null;
                _recoveryCancellation = null;
            }
            cancellation?.Dispose();
        }
        private async Task RunHostedServiceRecoveryLoopAsync(CancellationToken cancellationToken)
        {
            TimeSpan delay = _options.HostedServiceRecoveryInterval;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _syncWake.WaitAsync(delay, cancellationToken).ConfigureAwait(false);
                    var monitor = _connectivityMonitor;
                    if (monitor == null || !monitor.IsOnline)
                    {
                        delay = _options.HostedServiceRecoveryInterval;
                        continue;
                    }
                    bool shouldProbe;
                    lock (_connectivityGate)
                        shouldProbe = Status.Connectivity == EdgeConnectivityState.Online ||
                            (Status.Connectivity == EdgeConnectivityState.Offline &&
                             Status.Synchronization == EdgeSynchronizationState.Pending);
                    if (!shouldProbe)
                    {
                        delay = _options.HostedServiceRecoveryInterval;
                        continue;
                    }
                    var recovery = await SetConnectivityAsync(true, cancellationToken).ConfigureAwait(false);
                    delay = recovery != null && recovery.IsError && IsConnectivityFailure(recovery.ErrorCode)
                        ? NextRecoveryDelay(delay, _options.MaximumHostedServiceRecoveryInterval)
                        : _options.HostedServiceRecoveryInterval;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (ObjectDisposedException) when (_disposed) { }
            catch (Exception ex)
            {
                if (_disposed) return;
                Status.Synchronization = EdgeSynchronizationState.Error;
                Status.LastErrorCode = "EDGE_HOSTED_RECOVERY_LOOP_FAILED";
                Status.LastMessage = $"The hosted-service recovery loop stopped: {ex.Message}";
                OnStatusChanged();
            }
        }
        private static TimeSpan NextRecoveryDelay(TimeSpan current, TimeSpan maximum)
        {
            double nextMilliseconds = Math.Min(current.TotalMilliseconds * 2d, maximum.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(nextMilliseconds);
        }
        private async Task<OASISResult<long>> RefreshPendingCountAsync(CancellationToken cancellationToken)
        {
            var pending = await LocalStore.GetUnsettledOperationCountAsync(cancellationToken).ConfigureAwait(false);
            if (pending == null)
                return new OASISResult<long> { IsError = true, ErrorCount = 1,
                    ErrorCode = "EDGE_PENDING_COUNT_FAILED", Message = "The pending synchronization count is unavailable." };
            if (pending.IsError) return pending;
            Status.PendingOperationCount = pending.Result;
            return pending;
        }
        private static bool IsConnectivityFailure(string errorCode) =>
            string.Equals(errorCode, "HYPERDRIVE_NETWORK_UNAVAILABLE", StringComparison.Ordinal) ||
            string.Equals(errorCode, "HYPERDRIVE_NETWORK_TIMEOUT", StringComparison.Ordinal) ||
            string.Equals(errorCode, "HYPERDRIVE_REMOTE_UNAVAILABLE", StringComparison.Ordinal) ||
            string.Equals(errorCode, "ONET_CHANNEL_UNAVAILABLE", StringComparison.Ordinal) ||
            string.Equals(errorCode, "ONET_REMOTE_UNAVAILABLE", StringComparison.Ordinal);
        private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(OASISEdgeRuntime)); }
        private static OASISResult<T> Error<T>(string code, string message) => new OASISResult<T>
        { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    }
}
