using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// The thin platform shim a host implements per OS (Android BluetoothGatt/BluetoothGattServer, iOS Core
    /// Bluetooth, or a cross-platform BLE library). It only moves raw bytes over the
    /// <see cref="BleGattProfile"/> characteristics; all chunking, signing and routing live in shared code.
    /// </summary>
    public interface IBleGattPort
    {
        /// <summary>Currently negotiated ATT MTU (use <see cref="BleGattProfile.DefaultMtu"/> until negotiated).</summary>
        int NegotiatedMtu { get; }

        IReadOnlyCollection<string> ConnectedDeviceIds { get; }

        /// <summary>Raised when a connected device writes to RX or notifies on TX.</summary>
        event Action<string, byte[]> ValueReceived;

        /// <summary>Delivers a frame to a connected device (central: write RX; peripheral: notify TX).</summary>
        Task WriteAsync(string deviceId, byte[] value, CancellationToken cancellationToken);
    }
}
