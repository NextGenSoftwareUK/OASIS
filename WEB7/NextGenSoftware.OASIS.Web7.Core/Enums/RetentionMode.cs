namespace NextGenSoftware.OASIS.Web7.Core.Enums
{
    /// <summary>How long a symbiosis session's signal-derived data persists. Default is Ephemeral - reversible by design, no trace left behind.</summary>
    public enum RetentionMode
    {
        Ephemeral,
        SessionScoped,
        Permanent
    }
}
