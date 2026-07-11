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

        /// <summary>
        /// Per-request OpenServ override. null = use OASIS_DNA.Web6.PreferOpenServ default. true = route through
        /// the OpenServ SERV gateway regardless of provider; false = call the provider directly even if PreferOpenServ
        /// is enabled in DNA. Only applies when CompletionRequest.Provider is "auto".
        /// </summary>
        public bool? UseOpenServ { get; set; }
    }
}
