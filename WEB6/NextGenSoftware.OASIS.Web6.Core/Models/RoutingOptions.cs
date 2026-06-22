namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// Routing preferences for a WEB6 completion request.
    /// </summary>
    public class RoutingOptions
    {
        /// <summary>"cost", "latency" or "quality".</summary>
        public string Priority { get; set; } = "cost";

        /// <summary>Whether to automatically fail over to the next-best provider if the chosen one errors/times out.</summary>
        public bool Fallback { get; set; } = true;
    }
}
