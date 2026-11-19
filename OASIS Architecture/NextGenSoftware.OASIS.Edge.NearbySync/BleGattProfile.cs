using System;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// The OASIS nearby-sync GATT profile. Every device is both peripheral (advertises the service, accepts
    /// writes on <see cref="RxCharacteristic"/>) and central (scans for the service, connects, writes to the
    /// remote's RX and subscribes to its <see cref="TxCharacteristic"/> notifications).
    /// Request the largest MTU the platform allows (Android requestMtu(247+), iOS negotiates automatically);
    /// <see cref="BleNearbyLink"/> chunks to whatever was negotiated.
    /// </summary>
    public static class BleGattProfile
    {
        public static readonly Guid Service = new Guid("0a51500b-1e5e-4f0d-9a3b-0a5150e00001");
        /// <summary>Central writes frames here (write-without-response).</summary>
        public static readonly Guid RxCharacteristic = new Guid("0a51500b-1e5e-4f0d-9a3b-0a5150e00002");
        /// <summary>Peripheral notifies frames from here.</summary>
        public static readonly Guid TxCharacteristic = new Guid("0a51500b-1e5e-4f0d-9a3b-0a5150e00003");

        public const int AttOverheadBytes = 3;
        public const int DefaultMtu = 23;
        public const int PreferredMtu = 247;
    }
}
