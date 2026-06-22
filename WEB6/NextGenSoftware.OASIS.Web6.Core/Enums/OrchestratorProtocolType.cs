namespace NextGenSoftware.OASIS.Web6.Core.Enums
{
    /// <summary>
    /// The agent/orchestration protocols and frameworks WEB6 normalises over, so a caller can write once against
    /// the WEB6 protocol and reach every agent framework without learning each one's native wire format.
    /// </summary>
    public enum OrchestratorProtocolType
    {
        /// <summary>Model Context Protocol (Anthropic) - tool/resource invocation over JSON-RPC.</summary>
        MCP,

        /// <summary>Agent2Agent protocol - cross-vendor agent-to-agent task delegation.</summary>
        A2A,

        LangChain,
        AutoGen,
        CrewAI,
        SemanticKernel,

        /// <summary>A generic JSON-over-HTTP webhook for any orchestrator that doesn't have a dedicated adapter yet.</summary>
        Webhook
    }
}
