using System;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    public enum EdgeRuntimeNotificationKind
    {
        WorkingOffline = 0,
        BackOnlineSynchronizing = 1,
        SynchronizationComplete = 2
    }

    public sealed class EdgeRuntimeNotification
    {
        public EdgeRuntimeNotificationKind Kind { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Converts detailed runtime status changes into stable, product-facing transition events.
    /// Hosts remain responsible only for presenting these events on their UI thread.
    /// </summary>
    public sealed class EdgeRuntimeNotificationStateMachine
    {
        private readonly object _gate = new object();
        private EdgeConnectivityState? _lastConnectivity;
        private EdgeSynchronizationState? _lastSynchronization;
        private bool _hasBeenOffline;
        private bool _reconnectSyncPending;

        public EdgeRuntimeNotification Observe(EdgeRuntimeStatus status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            return Observe(status.Connectivity, status.Synchronization, status.PendingOperationCount);
        }

        public EdgeRuntimeNotification Observe(EdgeConnectivityState connectivity,
            EdgeSynchronizationState synchronization, long pendingOperationCount)
        {
            lock (_gate)
            {
                EdgeRuntimeNotification notification = null;
                if (connectivity == EdgeConnectivityState.Offline && !_hasBeenOffline)
                {
                    _hasBeenOffline = true;
                    _reconnectSyncPending = false;
                    notification = Create(EdgeRuntimeNotificationKind.WorkingOffline,
                        "Working offline - changes will sync automatically.");
                }
                else if (_hasBeenOffline && connectivity == EdgeConnectivityState.Online &&
                         synchronization == EdgeSynchronizationState.Synchronizing &&
                         (_lastConnectivity != EdgeConnectivityState.Online ||
                          _lastSynchronization != EdgeSynchronizationState.Synchronizing))
                {
                    _reconnectSyncPending = true;
                    _hasBeenOffline = false;
                    notification = Create(EdgeRuntimeNotificationKind.BackOnlineSynchronizing,
                        "Back online - synchronizing changes.");
                }
                else if (_reconnectSyncPending && connectivity == EdgeConnectivityState.Online &&
                         synchronization == EdgeSynchronizationState.Synchronized &&
                         pendingOperationCount == 0)
                {
                    _reconnectSyncPending = false;
                    notification = Create(EdgeRuntimeNotificationKind.SynchronizationComplete,
                        "Synchronization complete.");
                }

                _lastConnectivity = connectivity;
                _lastSynchronization = synchronization;
                return notification;
            }
        }

        private static EdgeRuntimeNotification Create(EdgeRuntimeNotificationKind kind, string message) =>
            new EdgeRuntimeNotification { Kind = kind, Message = message };
    }
}
