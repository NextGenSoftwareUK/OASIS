using System;
using NextGenSoftware.OASIS.Edge.Runtime;
using UnityEngine;

namespace NextGenSoftware.OASIS.Edge.Unity
{
    /// <summary>Unity reachability signal; hosted request failures remain authoritative in Edge Runtime.</summary>
    public sealed class UnityEdgeConnectivityMonitor : IEdgeConnectivityMonitor
    {
        private bool _isOnline;
        public bool IsOnline => _isOnline;
        public event EventHandler<EdgeConnectivityChangedEventArgs> ConnectivityChanged;

        public UnityEdgeConnectivityMonitor()
        {
            _isOnline = Application.internetReachability != NetworkReachability.NotReachable;
        }

        public void Poll()
        {
            bool online = Application.internetReachability != NetworkReachability.NotReachable;
            if (online == _isOnline) return;
            _isOnline = online;
            ConnectivityChanged?.Invoke(this, new EdgeConnectivityChangedEventArgs(online));
        }
    }
}
