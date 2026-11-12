using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    /// <summary>Optional shaping of a holon audit trail (POST variant).</summary>
    public class HolonAuditTrailRequest
    {
        /// <summary>Display labels keyed by field or metadata key, e.g. {"goals":"Therapy goals"}.</summary>
        public Dictionary<string, string> FieldLabels { get; set; }
        /// <summary>Metadata keys to leave out of the comparison (bookkeeping).</summary>
        public List<string> SkipKeys { get; set; }
        /// <summary>Oldest version to include (inclusive). 0 = no lower bound.</summary>
        public int FromVersion { get; set; }
        /// <summary>Newest version to include (inclusive). 0 = no upper bound.</summary>
        public int ToVersion { get; set; }
        /// <summary>Page size (default 50, max 200).</summary>
        public int Limit { get; set; } = 50;
        /// <summary>Entries to skip, newest first.</summary>
        public int Offset { get; set; }
    }

    public class HolonAuditTrailResponse
    {
        public System.Guid HolonId { get; set; }
        /// <summary>Latest saved version number of the holon.</summary>
        public int LatestVersion { get; set; }
        /// <summary>Total entries available in the requested version range.</summary>
        public int Total { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
        /// <summary>Newest first.</summary>
        public List<HolonAuditEntry> Entries { get; set; } = new List<HolonAuditEntry>();
    }
}
