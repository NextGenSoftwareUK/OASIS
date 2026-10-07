using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    public sealed class EdgeRuntimeOptions
    {
        public Guid DeviceId { get; set; }
        public Guid AvatarId { get; set; }
        public string DatabasePath { get; set; }
        /// <summary>Supply generated application metadata for additional typed entities; built-in HyperDrive and JSON projections require no configuration.</summary>
        public IEdgePayloadSerializer PayloadSerializer { get; set; }
        public int MaximumOperationsPerExchange { get; set; } = 100;
        public int MaximumRemoteChangesPerExchange { get; set; } = 100;
        public int MaximumExchangesPerRun { get; set; } = 20;
        /// <summary>Delay between pulls while accepted commands await their durable hosted outcomes.</summary>
        public TimeSpan CommandOutcomePollInterval { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan HostedServiceRecoveryInterval { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan MaximumHostedServiceRecoveryInterval { get; set; } = TimeSpan.FromMinutes(5);
        /// <summary>
        /// Durable device-local projection queues created atomically with each non-command mutation.
        /// Configure ids even when the target connects later so offline startup cannot lose projection work.
        /// </summary>
        public IReadOnlyList<string> LocalReplicationTargetIds { get; set; } = Array.Empty<string>();
        public int MaximumLocalReplicationsPerRun { get; set; } = 100;
    }

    public enum EdgeConnectivityState { Offline, Connecting, Online }
    public enum EdgeSynchronizationState { Idle, Synchronizing, Synchronized, Pending, Error }

    public sealed class EdgeRuntimeStatus
    {
        public EdgeConnectivityState Connectivity { get; internal set; }
        public EdgeSynchronizationState Synchronization { get; internal set; }
        public DateTime? LastSuccessfulSyncUtc { get; internal set; }
        public string LastErrorCode { get; internal set; }
        public string LastMessage { get; internal set; }
        public long PendingOperationCount { get; internal set; }
        public long PendingLocalReplicationCount { get; internal set; }
        public string LastLocalReplicationErrorCode { get; internal set; }
        public int UnresolvedConflictCount { get; internal set; }
        public EdgeConflictDiagnostic LastConflict { get; internal set; }
    }

    public sealed class EdgeConflictDiagnostic
    {
        public Guid OperationId { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; }
        public Guid ServerVersionId { get; set; }
        public Guid LocalVersionId { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public DateTime RecordedUtc { get; set; }
    }
}
