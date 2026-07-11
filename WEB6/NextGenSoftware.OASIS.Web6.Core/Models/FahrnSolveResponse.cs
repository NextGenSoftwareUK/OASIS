using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// Response from POST /v1/fahrn/solve — full pipeline result with telemetry.
    /// </summary>
    public class FahrnSolveResponse
    {
        public string Answer { get; set; }
        public string ReasoningTrace { get; set; }
        public string MermaidPlan { get; set; }
        public string TaskTypeClassified { get; set; }
        public DispatchMode ModeUsed { get; set; }
        public List<string> AgentsUsed { get; set; } = new List<string>();
        public Guid? BraidGraphId { get; set; }
        public bool BraidGraphWasReused { get; set; }
        public bool AvatarContextInjected { get; set; }
        public long TotalLatencyMs { get; set; }
        public int TotalTokensUsed { get; set; }
        public List<string> Providers { get; set; } = new List<string>();
    }
}
