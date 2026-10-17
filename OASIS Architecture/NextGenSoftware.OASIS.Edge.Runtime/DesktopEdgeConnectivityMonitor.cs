using System;
using System.Net.NetworkInformation;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    /// <summary>
    /// Desktop connectivity signal for NativeAOT and managed hosts. The signal only reports whether
    /// the operating system has an available network; hosted request results remain authoritative.
    /// </summary>
    public sealed class DesktopEdgeConnectivityMonitor : IEdgeConnectivityMonitor, IDisposable
    {
        private bool _disposed;
        private bool _isOnline;

        public DesktopEdgeConnectivityMonitor()
        {
            _isOnline = NetworkInterface.GetIsNetworkAvailable();
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        public bool IsOnline => _isOnline;
        public event EventHandler<EdgeConnectivityChangedEventArgs> ConnectivityChanged;

        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs args)
        {
            if (_disposed || args.IsAvailable == _isOnline) return;
            _isOnline = args.IsAvailable;
            ConnectivityChanged?.Invoke(this, new EdgeConnectivityChangedEventArgs(_isOnline));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        }
    }
}
