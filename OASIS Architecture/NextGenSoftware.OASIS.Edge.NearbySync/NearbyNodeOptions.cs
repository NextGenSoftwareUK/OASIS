using System;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    public sealed class NearbyNodeOptions
    {
        /// <summary>Maximum relay hops for a message. Bounds mesh flooding.</summary>
        public byte MaximumHops { get; set; } = 4;
        public TimeSpan DefaultRequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int DedupeCapacity { get; set; } = 4096;
        /// <summary>Relay messages addressed to other nodes. Disable on battery-constrained devices.</summary>
        public bool RelayForOthers { get; set; } = true;
    }
}
