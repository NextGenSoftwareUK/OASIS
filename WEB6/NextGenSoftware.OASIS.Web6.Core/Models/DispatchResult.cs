using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// A single agent's contribution to a dispatch (its score at dispatch time, its Mermaid execution plan and its outcome).
    /// </summary>
    public class AgentExecutionPlan
    {
        public Guid AgentId { get; set; }
        public string AgentName { get; set; }
        public double CompositeScoreAtDispatch { get; set; }
        public string MermaidDiagram { get; set; }
        public bool Stalled { get; set; }
        public bool LoopDetected { get; set; }
        public long LatencyMs { get; set; }
    }

    /// <summary>
    /// The result of a FAHRN dispatch - the merged/selected execution plan plus every agent's contribution.
    /// </summary>
    public class DispatchResult
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();

        public DispatchMode ModeUsed { get; set; }

        public List<AgentExecutionPlan> AgentPlans { get; set; } = new List<AgentExecutionPlan>();

        /// <summary>Final assembled Mermaid plan - selected (serial), merged (parallel) or stitched (decomposed).</summary>
        public string FinalMermaidPlan { get; set; }

        public Guid WinningAgentId { get; set; }

        /// <summary>The id of the HolonicBraidGraph holon this plan was loaded from or saved as, for the given TaskType.</summary>
        public Guid HolonicBraidGraphId { get; set; }

        public long TotalLatencyMs { get; set; }
    }
}
