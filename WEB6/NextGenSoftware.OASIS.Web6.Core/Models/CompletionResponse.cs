using System;

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
    }
}
