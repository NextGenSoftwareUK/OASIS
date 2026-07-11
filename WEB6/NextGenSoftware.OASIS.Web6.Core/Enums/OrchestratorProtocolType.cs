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
        Webhook,

        // ── Priority 21 — Additional Protocol Adapters ──────────────────────────────

        /// <summary>ACP (Agent Communication Protocol — BeeAI/IBM). Async run lifecycle: POST runs → poll/SSE.</summary>
        ACP,

        /// <summary>ANP (Agent Network Protocol). Decentralised agent discovery via DID Documents + message POST.</summary>
        ANP,

        /// <summary>gRPC — high-performance agent-to-agent calls using Protocol Buffers.</summary>
        GRPC,

        /// <summary>GraphQL — query/mutation-based agent invocation.</summary>
        GraphQL,

        /// <summary>Kafka event streaming — fire-and-forget or request/reply via topic pairs.</summary>
        Kafka,

        /// <summary>AMQP (RabbitMQ etc.) — durable message queue invocation.</summary>
        AMQP,

        /// <summary>MQTT — lightweight pub/sub for IoT agents and low-bandwidth environments.</summary>
        MQTT
    }
}
