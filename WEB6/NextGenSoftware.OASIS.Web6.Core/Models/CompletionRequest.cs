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

        /// <summary>
        /// Per-request FAHRN override. null = use OASIS_DNA.Web6.EnableFAHRN default. true/false = force on/off
        /// regardless of the DNA setting. When enabled, WEB6 runs a FAHRN reasoning-network dispatch before calling
        /// the provider and injects the Mermaid execution plan into the system context.
        /// </summary>
        public bool? UseFAHRN { get; set; }

        /// <summary>
        /// Per-request Holonic BRAID override. null = use OASIS_DNA.Web6.EnableHolonicBraid default.
        /// true/false = force on/off. When enabled, WEB6 fetches the shared reasoning graph for FahrnTaskType
        /// and injects it into the system context before calling the provider.
        /// </summary>
        public bool? UseHolonicBraid { get; set; }

        /// <summary>
        /// Task type used for FAHRN dispatch scoring and Holonic BRAID graph look-up (e.g. "general", "legal",
        /// "code", "mathematics", "trust-guidance"). Defaults to "general".
        /// </summary>
        public string FahrnTaskType { get; set; } = "general";
    }
}
