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

        /// <summary>Hard token budget for the entire dispatch run. Dispatch stops early if exceeded.</summary>
        public int? MaxTotalTokens { get; set; }

        /// <summary>Voting strategy for Voting mode: "majority" (default), "weighted", or "unanimous".</summary>
        public string VotingStrategy { get; set; } = "weighted";

        /// <summary>Minimum number of agents required for Voting mode (default 3).</summary>
        public int MinVotingAgents { get; set; } = 3;

        /// <summary>Optional per-request override of FAHRN composite score weights.</summary>
        public ScoringWeights ScoringWeights { get; set; }

        /// <summary>Hard USD cost ceiling for the entire dispatch run. Checked between agent calls; stops early if exceeded.</summary>
        public decimal? MaxCostUsd { get; set; }

        /// <summary>Hard token ceiling per individual agent call. Agent calls whose token count exceeds this are skipped.</summary>
        public int? MaxTokensPerAgent { get; set; }

        /// <summary>"stop" (default — return partial results) or "best_so_far" (return best answer seen before budget hit).</summary>
        public string BudgetExceededBehaviour { get; set; } = "stop";

        // ── Priority 3c — Web7 Symbiosis hints ──────────────────────────────────────────────────────
        /// <summary>When set, FAHRN reads the user's real-time bio-signal state (cognitive load, focus, arousal)
        /// from the active Symbiosis session and adjusts dispatch mode accordingly.</summary>
        public Guid? SymbiosisSessionId { get; set; }

        // ── Priority 3d — Web8 mesh routing ─────────────────────────────────────────────────────────
        /// <summary>When true, completion requests are routed through the Web8 galactic mesh rather than
        /// calling AIProviderManager directly. Requires SourceMeshNodeId to be set.</summary>
        public bool UseMeshRouting { get; set; } = false;

        /// <summary>The mesh node that initiates the routed completion request.</summary>
        public Guid? SourceMeshNodeId { get; set; }

        // ── Priority 17d — Contradiction detection ───────────────────────────────────────────────────
        /// <summary>When true, FAHRN runs a lightweight NLI check between adjacent agent outputs in Debate/Voting
        /// modes and flags contradictions. Adds one model call per agent pair. Default false.</summary>
        public bool EnableContradictionDetection { get; set; } = false;
    }
}
