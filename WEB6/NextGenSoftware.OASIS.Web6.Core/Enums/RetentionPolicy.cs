namespace NextGenSoftware.OASIS.Web6.Core.Enums
{
    /// <summary>How long a piece of memory is retained at a given holon before being discarded.</summary>
    public enum RetentionPolicy
    {
        /// <summary>Deleted when its session holon ends.</summary>
        Ephemeral,

        /// <summary>Lives for the duration of the associated session. Alias for Ephemeral in practice.</summary>
        Session,

        /// <summary>Never expires. The default retention level.</summary>
        Persistent,

        /// <summary>Expires at the absolute UTC time set in HolonicMemoryItem.ExpiresUtc.</summary>
        TimeLimited,

        // Legacy names kept for backwards compatibility
        SessionScoped = Session,
        Permanent = Persistent
    }
}
