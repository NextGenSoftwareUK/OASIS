
# WEB6 OASIS AI API RELEASE HISTORY

This needs to be updated whenever we do any work that will affect the WEB6 AI API.
This file is linked to the Swagger documentation for the WEB6 AI API.

----------------------------------------------------------------------------------------------------------------------------
## 1.0.0 (22/06/26)

Initial release of the WEB6 OASIS AI API — the unified AI abstraction and aggregation layer built on top of WEB4 (OASIS API/ONODE) and WEB5 (STAR ODK/STARNET).

- One API surface for every AI provider (OpenAI, Anthropic, Google Gemini, Mistral, Cohere, Ollama and more).
- Karma-gated AI access — avatar karma level controls which AI capabilities are available.
- JWT authentication via the existing OASIS avatar system.
- Basic provider routing and aggregation.
- Full Swagger/OpenAPI documentation.
- Initial MCP (Model Context Protocol) tool surface.

----------------------------------------------------------------------------------------------------------------------------
## 2.0.0 (11/07/26)

Major upgrade establishing WEB6 as the production-grade unified AI layer.

- **HTTP MCP transport**: the entire OASIS WEB4-WEB10 tool surface is now reachable by any MCP client (Claude.ai, OpenAI, Cursor, etc.) via a single HTTP MCP endpoint.
- **FAHRN** (Fractal Adaptive Holonic Reasoning Network): controller agent that coordinates multi-model reasoning chains across the full OASIS provider fleet, with adaptive routing and self-healing.
- **SSE streaming** (Server-Sent Events): real-time token-by-token streaming for all AI completions.
- **Holonic BRAID**: shared reasoning-graph memory — AI agents share context and reasoning history across sessions via OASIS holons.
- **SkillOpt**: self-evolving agent skills that improve over time based on karma-weighted feedback.
- **ML.NET in-process AutoML**: on-device machine learning without external dependencies.
- **Embeddings API**: vector embeddings for semantic search and RAG pipelines.
- **Avatar context**: all AI requests are scoped to the authenticated OASIS avatar — karma, history and preferences inform AI responses.
- **Debate/Voting dispatch**: route the same prompt to multiple models and aggregate responses via debate or voting strategies.
- **A2A** (Agent-to-Agent): OASIS agents can communicate and delegate tasks between each other via a standardised A2A protocol.
- **WebSocket telemetry**: real-time observability stream for AI request tracing and performance metrics.
- **DID/Verifiable Credentials**: AI access and data sharing gated by decentralised identity and W3C Verifiable Credentials.
- **ACP/ANP/gRPC/GraphQL multi-protocol orchestration**: AI requests can be dispatched via any protocol.
- **Self-registration and discovery**: provider self-registration and a /.well-known discovery endpoint so MCP clients can auto-configure.
- **Provider health monitor**: continuous background health-checks across all registered AI providers with automatic failover.
- **Full OpenTelemetry observability**: distributed tracing, metrics and logs for every AI call.
- **250+ MCP tools** across WEB4-WEB10 exposed via the HTTP MCP server.
- Leela AI added as a new WEB6 AI provider.
- Swagger 500 fixes: custom schema IDs, open-form JsonObject/JsonNode/JsonArray mappings, ResolveConflictingActions, excluded SSE/WebSocket/discovery routes from API explorer.
- Various bug fixes and performance improvements.
