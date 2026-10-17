using System;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    /// <summary>
    /// Platform adapter implemented by Unity, Android, iOS or desktop hosts. The runtime consumes
    /// authoritative connectivity transitions; it does not infer connectivity from failed writes.
    /// </summary>
    public interface IEdgeConnectivityMonitor
    {
        bool IsOnline { get; }
        event EventHandler<EdgeConnectivityChangedEventArgs> ConnectivityChanged;
    }

    public sealed class EdgeConnectivityChangedEventArgs : EventArgs
    {
        public EdgeConnectivityChangedEventArgs(bool isOnline) { IsOnline = isOnline; }
        public bool IsOnline { get; }
    }
}
