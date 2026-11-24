namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>Request channels multiplexed over one node. The first payload byte selects the handler.</summary>
    public static class NearbyChannels
    {
        public const byte HyperDriveSync = 1;
        public const byte Messaging = 2;
    }
}
