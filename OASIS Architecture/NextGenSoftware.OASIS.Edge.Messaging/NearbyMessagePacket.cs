namespace NextGenSoftware.OASIS.Edge.Messaging
{
    public sealed class NearbyMessagePacket
    {
        public EdgeMessage Message { get; set; }
    }

    public sealed class NearbyMessageAck
    {
        public bool Accepted { get; set; }
        public string Reason { get; set; }
    }
}
