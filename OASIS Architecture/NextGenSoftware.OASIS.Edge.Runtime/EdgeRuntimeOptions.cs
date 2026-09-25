using System;

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
        public TimeSpan HostedServiceRecoveryInterval { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan MaximumHostedServiceRecoveryInterval { get; set; } = TimeSpan.FromMinutes(5);
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
    }
}
