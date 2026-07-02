using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// A unified completion request - the single request shape WEB6 accepts for every AI provider.
    /// POST https://api.web6.oasisomniverse.one/v1/complete
    /// </summary>
    public class CompletionRequest
    {
        /// <summary>"auto", "openai", "anthropic", "gemini", "ollama", etc. "auto" lets WEB6 pick the best provider.</summary>
        public string Provider { get; set; } = "auto";

        /// <summary>"auto" or a specific model id (e.g. "gpt-4o", "claude-opus-4-8").</summary>
        public string Model { get; set; } = "auto";

        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        public RoutingOptions Routing { get; set; } = new RoutingOptions();

        /// <summary>Optional OASIS avatar id - when set, the request is grounded with WEB4 identity/karma and WEB5 holon/STARNET context.</summary>
        public Guid AvatarId { get; set; }

        public double? Temperature { get; set; }

        public int? MaxTokens { get; set; }
    }
}
