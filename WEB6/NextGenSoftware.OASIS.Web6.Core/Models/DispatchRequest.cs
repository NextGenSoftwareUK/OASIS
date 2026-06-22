using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// A request to the FAHRN controller agent to dispatch a problem to the reasoning network.
    /// </summary>
    public class DispatchRequest
    {
        public string Problem { get; set; }

        /// <summary>Task category used for composite scoring, e.g. "mathematics", "legal", "code", "writing", "real-time".</summary>
        public string TaskType { get; set; } = "general";

        public DispatchMode Mode { get; set; } = DispatchMode.Serial;

        /// <summary>If empty, every registered agent is eligible.</summary>
        public List<Guid> EligibleAgentIds { get; set; } = new List<Guid>();

        public Guid AvatarId { get; set; }
    }
}
