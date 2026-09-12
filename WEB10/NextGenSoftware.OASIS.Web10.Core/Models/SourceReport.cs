using System;
using NextGenSoftware.OASIS.Web9.Core.Models;

namespace NextGenSoftware.OASIS.Web10.Core.Models
{
    /// <summary>
    /// "WEB10 = WEB0" made literal: the foundational runtime/version identity every layer derives from (the Alpha),
    /// together with WEB9's live unified status across WEB4-WEB8 (the Omega) - returned as one report.
    /// </summary>
    public class SourceReport
    {
        public string OasisRuntimeVersion { get; set; }

        public string OasisApiVersion { get; set; }

        public string StarApiVersion { get; set; }

        /// <summary>The live aggregate status of every layer above the foundation, as reported by WEB9.</summary>
        public UnifiedStatusReport UnifiedStatus { get; set; }

        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    }
}
