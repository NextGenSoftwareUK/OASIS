using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// The continuously-updated scoring metadata for a single reasoning agent registered with FAHRN
    /// (the Fractal Adaptive Holonic Reasoning Network controller). Persisted as a ReasoningAgent holon.
    /// </summary>
    public class ReasoningAgentMetadata
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string AgentName { get; set; }

        public AIProviderType Provider { get; set; }

        public string Model { get; set; }

        /// <summary>Per-category score (0-1), e.g. "mathematics", "legal", "architecture", "code", "writing", "real-time".</summary>
        public Dictionary<string, double> CategoryScores { get; set; } = new Dictionary<string, double>();

        /// <summary>0-1, higher is faster.</summary>
        public double SpeedScore { get; set; } = 0.5;

        /// <summary>0-1, higher is cheaper.</summary>
        public double CostScore { get; set; } = 0.5;

        /// <summary>0-1, higher is more stable / less prone to looping.</summary>
        public double LoopDetectionScore { get; set; } = 1.0;

        /// <summary>0-1, fraction of recent tasks this agent has failed/stalled on.</summary>
        public double FailureRate { get; set; }

        public int TasksCompleted { get; set; }

        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        public double GetCategoryScore(string category)
        {
            return CategoryScores != null && CategoryScores.TryGetValue(category, out var score) ? score : 0.5;
        }
    }
}
