using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// A registered external agent/orchestrator endpoint (an MCP server, an A2A agent, a LangChain/AutoGen/CrewAI/
    /// Semantic Kernel deployment, or any generic webhook) that WEB6 will normalise traffic to/from.
    /// </summary>
    public class OrchestratorAdapterConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; }

        public OrchestratorProtocolType Protocol { get; set; }

        /// <summary>The base URL of the external orchestrator/agent endpoint.</summary>
        public string EndpointUrl { get; set; }

        /// <summary>Optional bearer token / API key for the external endpoint.</summary>
        public string AuthToken { get; set; }

        /// <summary>Arbitrary extra headers/config the specific protocol adapter needs (e.g. MCP tool name).</summary>
        public Dictionary<string, string> ExtraConfig { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>A normalised request sent through an orchestrator adapter - the same CompletionRequest shape used by every other AI provider.</summary>
    public class OrchestratorInvokeRequest
    {
        public Guid AdapterId { get; set; }

        public string Input { get; set; }

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>A normalised response from an orchestrator adapter, regardless of the underlying protocol's native wire format.</summary>
    public class OrchestratorInvokeResponse
    {
        public string AdapterName { get; set; }

        public OrchestratorProtocolType Protocol { get; set; }

        public string Output { get; set; }

        public long LatencyMs { get; set; }
    }
}
