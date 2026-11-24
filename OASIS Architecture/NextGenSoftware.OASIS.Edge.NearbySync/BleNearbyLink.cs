using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>Adapts a platform <see cref="IBleGattPort"/> to a nearby link with MTU-aware frame sizing.</summary>
    public sealed class BleNearbyLink : INearbyLink
    {
        private readonly IBleGattPort _port;

        public BleNearbyLink(IBleGattPort port)
        {
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _port.ValueReceived += OnValue;
        }

        public NearbyLinkKind Kind => NearbyLinkKind.BluetoothLE;

        /// <summary>
        /// Negotiated MTU minus ATT overhead (must be at least the chunker minimum; if a platform cannot negotiate
        /// above 23 the minimum is met but sync is very slow, so request a larger MTU).
        /// </summary>
        public int MaximumFrameSize => _port.NegotiatedMtu - BleGattProfile.AttOverheadBytes;

        public IReadOnlyCollection<string> ConnectedPeerIds => _port.ConnectedDeviceIds;
        public event Action<string, byte[]> FrameReceived;

        private void OnValue(string deviceId, byte[] value) => FrameReceived?.Invoke(deviceId, value);

        public Task SendAsync(string peerId, byte[] frame, CancellationToken cancellationToken)
        {
            if (frame.Length > MaximumFrameSize)
                throw new ArgumentException($"Frame exceeds the negotiated BLE limit of {MaximumFrameSize} bytes.", nameof(frame));
            return _port.WriteAsync(peerId, frame, cancellationToken);
        }

        public void Dispose() => _port.ValueReceived -= OnValue;
    }
}
