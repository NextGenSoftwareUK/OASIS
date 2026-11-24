namespace NextGenSoftware.OASIS.Edge.Messaging
{
    /// <summary>Lifecycle of a message on this device. Values only advance, except Failed.</summary>
    public enum EdgeMessageState
    {
        /// <summary>Composed locally; not yet durably queued.</summary>
        Pending = 0,
        /// <summary>In the durable HyperDrive outbox; will reach the server when any transport syncs.</summary>
        Queued = 1,
        /// <summary>Server has accepted it (seen in the online sync stream).</summary>
        Sent = 2,
        /// <summary>The recipient device acknowledged it (nearby) or the server reports delivery.</summary>
        Delivered = 3,
        Read = 4,
        /// <summary>Incoming message stored on this device.</summary>
        Received = 5,
        /// <summary>The outbox refused it. It is not queued and will not be sent.</summary>
        Failed = 6
    }
}
