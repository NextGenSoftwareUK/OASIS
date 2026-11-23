using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>The radio or network a link runs over.</summary>
    public enum NearbyLinkKind { Loopback, BluetoothLE, WifiDirect, MultipeerConnectivity, Lan }

    /// <summary>
    /// One physical transport to nearby devices. Platform code (Android BLE, iOS Core Bluetooth/Multipeer,
    /// Wi-Fi Direct, LAN) implements this; everything above it is shared and radio-agnostic.
    /// A link moves opaque frames of at most <see cref="MaximumFrameSize"/> bytes to a directly connected peer.
    /// </summary>
    public interface INearbyLink : IDisposable
    {
        NearbyLinkKind Kind { get; }

        /// <summary>Largest frame <see cref="SendAsync"/> accepts (for BLE: negotiated ATT MTU minus 3).</summary>
        int MaximumFrameSize { get; }

        /// <summary>Link-local ids of directly connected peers.</summary>
        IReadOnlyCollection<string> ConnectedPeerIds { get; }

        /// <summary>Raised with (link-local peer id, frame) for every frame received.</summary>
        event Action<string, byte[]> FrameReceived;

        Task SendAsync(string peerId, byte[] frame, CancellationToken cancellationToken);
    }
}
