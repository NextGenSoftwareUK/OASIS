using System;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// A single SSE chunk returned by POST /v1/complete/stream.
    /// </summary>
    public class CompletionChunk
    {
        public string Delta { get; set; }
        public string Provider { get; set; }
        public string Model { get; set; }
        public bool Done { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
    }
}
