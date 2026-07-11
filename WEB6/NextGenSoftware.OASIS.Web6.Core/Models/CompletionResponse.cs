using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// The unified completion response returned for every AI provider WEB6 routes to.
    /// </summary>
    public class CompletionResponse
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Provider { get; set; }

        public string Model { get; set; }

        public string Content { get; set; }

        public int PromptTokens { get; set; }

        public int CompletionTokens { get; set; }

        public long LatencyMs { get; set; }

        /// <summary>True if WEB6 had to fail over to a different provider/model than requested.</summary>
        public bool FailedOver { get; set; }

        /// <summary>Estimated cost in USD based on token usage and provider pricing table.</summary>
        public double EstimatedCostUSD { get; set; }

        /// <summary>True if this response was served from the semantic cache (no provider call was made).</summary>
        public bool FromCache { get; set; }

        /// <summary>Why the model stopped. "stop" | "tool_calls" | "length" | "content_filter".</summary>
        public string FinishReason { get; set; }

        /// <summary>Tool calls the model wants to execute. Populated when FinishReason="tool_calls".</summary>
        public List<ToolCall> ToolCalls { get; set; }
    }
}
