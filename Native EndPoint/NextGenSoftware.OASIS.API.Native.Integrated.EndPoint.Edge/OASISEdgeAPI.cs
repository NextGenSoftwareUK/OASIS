using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime;

namespace NextGenSoftware.OASIS.API.Native.EndPoint.Edge
{
    /// <summary>In-process facade for constrained and Unity clients.</summary>
    public sealed class OASISEdgeAPI : IDisposable, IAsyncDisposable
    {
        public OASISEdgeRuntime Runtime { get; }
        public EdgeSQLiteSyncStateStore LocalStorage => Runtime.LocalStore;
        public EdgeEntityRepository Entities => Runtime.Entities;
        public EdgeOfflineSessionManager Sessions => Runtime.Sessions;
        public EdgeRuntimeStatus Status => Runtime.Status;
        public event EventHandler<EdgeRuntimeStatus> StatusChanged
        {
            add => Runtime.StatusChanged += value;
            remove => Runtime.StatusChanged -= value;
        }

        public OASISEdgeAPI(EdgeRuntimeOptions options, IHyperDriveSyncTransport transport)
        {
            Runtime = new OASISEdgeRuntime(options, transport);
        }

        /// <summary>
        /// Composes the Unity/mobile Edge runtime directly against the hosted ONODE synchronization API.
        /// The caller owns the HttpClient and must configure its BaseAddress and authentication handler.
        /// </summary>
        public OASISEdgeAPI(EdgeRuntimeOptions options, HttpClient authenticatedHttpClient)
            : this(options, new HttpHyperDriveSyncTransport(authenticatedHttpClient))
        {
        }

        public OASISEdgeAPI(EdgeRuntimeOptions options, IHyperDriveSyncTransport transport,
            IEdgeSecureSessionStore secureSessionStore, IEdgeOfflineGrantValidator offlineGrantValidator,
            IHyperDriveClock clock = null)
        {
            Runtime = new OASISEdgeRuntime(options, transport, secureSessionStore, offlineGrantValidator, clock);
        }

        /// <summary>
        /// Composes hosted synchronization and cryptographically validated offline-session support.
        /// The caller owns the HttpClient and platform-secure session store.
        /// </summary>
        public OASISEdgeAPI(EdgeRuntimeOptions options, HttpClient authenticatedHttpClient,
            IEdgeSecureSessionStore secureSessionStore, IEdgeOfflineGrantValidator offlineGrantValidator,
            IHyperDriveClock clock = null)
            : this(options, new HttpHyperDriveSyncTransport(authenticatedHttpClient), secureSessionStore,
                offlineGrantValidator, clock)
        {
        }

        public Task<OASISResult<SyncOperation>> SaveAsync(EdgeLocalMutation mutation,
            CancellationToken cancellationToken = default) => Runtime.SaveLocalAsync(mutation, cancellationToken);

        public Task<OASISResult<SyncOperation>> SaveEntityAsync<T>(EdgeEntityWriteRequest<T> request,
            CancellationToken cancellationToken = default) => Entities.SaveAsync(request, cancellationToken);

        public Task<OASISResult<EdgeEntityReadResult<T>>> LoadEntityAsync<T>(string entityType, Guid entityId,
            CancellationToken cancellationToken = default) => Entities.LoadAsync<T>(entityType, entityId, cancellationToken);

        public Task<OASISResult<IReadOnlyList<EdgeEntityReadResult<T>>>> LoadAllEntitiesAsync<T>(string entityType,
            bool includeDeleted = false, CancellationToken cancellationToken = default) =>
            Entities.LoadAllAsync<T>(entityType, includeDeleted, cancellationToken);

        public Task<OASISResult<SyncOperation>> DeleteEntityAsync(EdgeEntityDeleteRequest request,
            CancellationToken cancellationToken = default) => Entities.DeleteAsync(request, cancellationToken);

        public Task<OASISResult<SyncOperation>> QueueQuestProgressAsync(Guid operationId, Guid questId,
            HyperDriveQuestProgressCommand payload, CancellationToken cancellationToken = default) =>
            Entities.QueueQuestProgressAsync(operationId, questId, payload, cancellationToken);

        public Task<OASISResult<SyncOperation>> QueueInventoryGrantAsync(Guid operationId, Guid itemId,
            HyperDriveInventoryGrantCommand payload, CancellationToken cancellationToken = default) =>
            Entities.QueueInventoryGrantAsync(operationId, itemId, payload, cancellationToken);

        public Task<OASISResult<SyncOperation>> QueueGeoNftCollectionAsync(Guid operationId, Guid geoNftId,
            HyperDriveGeoNftCollectionCommand payload, CancellationToken cancellationToken = default) =>
            Entities.QueueGeoNftCollectionAsync(operationId, geoNftId, payload, cancellationToken);

        public Task<OASISResult<EdgeEntityReadResult<HyperDriveCommandOutcome>>> LoadCommandOutcomeAsync(
            Guid operationId, CancellationToken cancellationToken = default) =>
            Entities.LoadCommandOutcomeAsync(operationId, cancellationToken);

        public Task<OASISResult<SyncCycleResult>> SetConnectivityAsync(bool isOnline,
            CancellationToken cancellationToken = default) => Runtime.SetConnectivityAsync(isOnline, cancellationToken);

        public Task<OASISResult<SyncCycleResult>> StartConnectivityMonitoringAsync(IEdgeConnectivityMonitor monitor,
            CancellationToken cancellationToken = default) => Runtime.StartConnectivityMonitoringAsync(monitor, cancellationToken);

        public void StopConnectivityMonitoring() => Runtime.StopConnectivityMonitoring();

        public Task<OASISResult<SyncCycleResult>> SuspendAsync(
            CancellationToken cancellationToken = default) => Runtime.SuspendAsync(cancellationToken);

        public Task<OASISResult<SyncCycleResult>> ResumeAsync(
            CancellationToken cancellationToken = default) => Runtime.ResumeAsync(cancellationToken);

        public Task<OASISResult<SyncCycleResult>> SynchronizeAsync(
            CancellationToken cancellationToken = default) => Runtime.SynchronizeAsync(cancellationToken);

        public Task<OASISResult<HyperDriveOfflineSessionGrant>> AcquireOfflineSessionAsync(
            IReadOnlyList<string> scopes, int lifetimeMinutes, CancellationToken cancellationToken = default) =>
            Runtime.AcquireOfflineSessionAsync(scopes, lifetimeMinutes, cancellationToken);

        public Task<OASISResult<HyperDriveOfflineSessionGrant>> ResumeOfflineSessionAsync(
            IReadOnlyList<string> requiredScopes, CancellationToken cancellationToken = default) =>
            Runtime.ResumeOfflineSessionAsync(requiredScopes, cancellationToken);

        public Task<OASISResult<bool>> BindOnetIdentityAsync(IEdgeNodeIdentity identity,
            CancellationToken cancellationToken = default) =>
            Runtime.BindOnetIdentityAsync(identity, cancellationToken);

        public Task<OASISResult<IReadOnlyList<EdgeSyncConflict>>> ReadUnresolvedConflictsAsync(
            CancellationToken cancellationToken = default) => Runtime.ReadUnresolvedConflictsAsync(cancellationToken);

        public Task<OASISResult<SyncOperation>> ResolveConflictAsync(EdgeConflictResolution resolution,
            CancellationToken cancellationToken = default) => Runtime.ResolveConflictAsync(resolution, cancellationToken);

        public void Dispose() => Runtime.Dispose();
        public ValueTask DisposeAsync() => Runtime.DisposeAsync();
    }
}
