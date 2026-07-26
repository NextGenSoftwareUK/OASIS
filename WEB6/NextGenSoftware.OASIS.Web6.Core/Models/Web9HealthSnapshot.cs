using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// A point-in-time health snapshot from Web9 (Singularity Aggregation). Used by FAHRN to gate
    /// dispatch: degrade providers reported as unhealthy and optionally skip BRAID persistence when
    /// Web4 storage is reported as degraded.
    /// </summary>
    public class Web9HealthSnapshot
    {
        /// <summary>When true, Web4 storage is reported degraded — skip BRAID graph persistence for this dispatch.</summary>
        public bool Web4StorageDegraded { get; set; }

        /// <summary>AI provider names (matching AIProviderType.ToString()) that Web9 reports as unhealthy.
        /// Their composite score is set to 0 so FAHRN excludes them from dispatch ranking.</summary>
        public List<string> DegradedProviders { get; set; } = new List<string>();

        /// <summary>A healthy snapshot with no degraded providers — returned when Web9 is unreachable.</summary>
        public static Web9HealthSnapshot Healthy { get; } = new Web9HealthSnapshot();
    }
}
