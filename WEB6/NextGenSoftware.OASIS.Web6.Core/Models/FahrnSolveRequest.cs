using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// Request for the FAHRN hero endpoint POST /v1/fahrn/solve — full pipeline in one call.
    /// </summary>
    public class FahrnSolveRequest
    {
        public string Problem { get; set; }

        /// <summary>"auto" = let FAHRN classify; otherwise supply e.g. "code", "mathematics", "legal".</summary>
        public string TaskType { get; set; } = "auto";

        public Guid AvatarId { get; set; }

        /// <summary>When true, Web4 karma + Web5 quests are injected into the problem as avatar context.</summary>
        public bool InjectAvatarContext { get; set; } = true;

        /// <summary>When true, the full reasoning trace and Mermaid plan are returned in the response.</summary>
        public bool ReturnReasoning { get; set; } = true;

        public ScoringWeights ScoringWeights { get; set; }

        public DispatchMode Mode { get; set; } = DispatchMode.Auto;

        public int?     MaxTotalTokens         { get; set; }
        public decimal? MaxCostUsd             { get; set; }
        public int?     MaxTokensPerAgent      { get; set; }
        public string   BudgetExceededBehaviour { get; set; } = "stop";
    }
}
