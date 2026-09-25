using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.ONET.Runtime;
using NextGenSoftware.OASIS.Edge.Runtime;
using NextGenSoftware.OASIS.ONET;

namespace NextGenSoftware.OASIS.API.Native.EndPoint.Edge
{
    /// <summary>
    /// Native/Unity facade for the Edge ONODE composition. The caller supplies the platform-secure
    /// identity, authenticated ONET channel and HTTPS bootstrap transports; no Full ONODE is loaded.
    /// </summary>
    public sealed class OASISEdgeOnetAPI : IDisposable
    {
        public OASISEdgeOnetRuntime OnetRuntime { get; }
        public OASISEdgeRuntime Runtime => OnetRuntime.EdgeRuntime;
        public EdgeSQLiteSyncStateStore LocalStorage => Runtime.LocalStore;
        public EdgeEntityRepository Entities => Runtime.Entities;
        public EdgeOfflineSessionManager Sessions => Runtime.Sessions;
        public EdgeRuntimeStatus Status => Runtime.Status;
        public string LastCapabilityAdvertisementError => OnetRuntime.LastCapabilityAdvertisementError;
        public string LastCapabilityAdvertisementWarning => OnetRuntime.LastCapabilityAdvertisementWarning;

        public OASISEdgeOnetAPI(EdgeRuntimeOptions options, IONETApplicationMessageChannel channel,
            string hostedNodeId, IHyperDrivePeerBindingTransport peerBindingTransport,
            IHyperDriveOfflineSessionGrantTransport offlineGrantTransport = null,
            IEdgeSecureSessionStore secureSessionStore = null,
            IEdgeOfflineGrantValidator offlineGrantValidator = null,
            IHyperDriveClock clock = null,
            IEnumerable<ONETProviderCapability> eligibleProviderCapabilities = null,
            IEnumerable<string> capabilityRegistryNodeIds = null,
            int? capabilityRegistryQuorum = null)
        {
            OnetRuntime = new OASISEdgeOnetRuntime(options, channel, hostedNodeId, peerBindingTransport,
                offlineGrantTransport, secureSessionStore, offlineGrantValidator, clock,
                eligibleProviderCapabilities, capabilityRegistryNodeIds: capabilityRegistryNodeIds,
                capabilityRegistryQuorum: capabilityRegistryQuorum);
        }

        public Task<OASISResult<SyncCycleResult>> StartAsync(IEdgeNodeIdentity identity,
            IEdgeConnectivityMonitor connectivityMonitor, CancellationToken cancellationToken = default) =>
            OnetRuntime.StartAsync(identity, connectivityMonitor, cancellationToken);

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

        public Task<OASISResult<HyperDriveOfflineSessionGrant>> AcquireOfflineSessionAsync(
            IReadOnlyList<string> scopes, int lifetimeMinutes, CancellationToken cancellationToken = default) =>
            Runtime.AcquireOfflineSessionAsync(scopes, lifetimeMinutes, cancellationToken);

        public Task<OASISResult<HyperDriveOfflineSessionGrant>> ResumeOfflineSessionAsync(
            IReadOnlyList<string> requiredScopes, CancellationToken cancellationToken = default) =>
            Runtime.ResumeOfflineSessionAsync(requiredScopes, cancellationToken);

        public Task<OASISResult<IReadOnlyList<EdgeSyncConflict>>> ReadUnresolvedConflictsAsync(
            CancellationToken cancellationToken = default) => Runtime.ReadUnresolvedConflictsAsync(cancellationToken);

        public Task<OASISResult<SyncOperation>> ResolveConflictAsync(EdgeConflictResolution resolution,
            CancellationToken cancellationToken = default) => Runtime.ResolveConflictAsync(resolution, cancellationToken);

        public Task<OASISResult<SyncCycleResult>> SynchronizeAsync(CancellationToken cancellationToken = default) =>
            Runtime.SynchronizeAsync(cancellationToken);

        public void Dispose() => OnetRuntime.Dispose();
    }
}
