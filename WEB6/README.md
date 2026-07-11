# WEB6 — OASIS AI Abstraction & Orchestration Layer

WEB6 is the unified AI layer of the [OASIS Omniverse](https://oasisomniverse.one) stack. It sits on top of WEB4 (identity, karma, COSMIC ORM, NFTs) and WEB5 (STAR ODK, holon graph, STARNET), providing a single API surface for AI completions, multi-agent orchestration, semantic memory, and IDE tooling — regardless of which underlying AI provider, blockchain, or storage layer you're using.

**101 MCP tools · 56 REST endpoints · 15+ AI providers · .NET 10**

---

## Projects in this folder

| Project | NuGet / npm | Description |
|---|---|---|
| [`NextGenSoftware.OASIS.Web6.Core`](NextGenSoftware.OASIS.Web6.Core/) | `NextGenSoftware.OASIS.Web6.Core` | Business logic — all managers, models, enums |
| [`NextGenSoftware.OASIS.Web6.WebAPI`](NextGenSoftware.OASIS.Web6.WebAPI/) | `NextGenSoftware.OASIS.Web6.WebAPI` | ASP.NET Core REST API — all controllers |
| [`NextGenSoftware.OASIS.MCP.Server`](NextGenSoftware.OASIS.MCP.Server/) | `NextGenSoftware.OASIS.MCP.Server` · `@oasisomniverse/mcp-server` | MCP Server — 101 typed tools for IDEs |
| [`npm/`](npm/) | — | npm shim package source |

---

## Key capabilities

### AI Provider Abstraction (`AIProviderManager`)
Single `POST /v1/complete` endpoint routes to **15+ providers** — OpenAI, Anthropic, Gemini, Groq, Mistral, Cohere, xAI, DeepSeek, Ollama, OpenSERV, Azure OpenAI, AWS Bedrock, HuggingFace, and more. Normalised request/response across all of them. Automatic failover, cost/latency/quality routing, streaming SSE, tool/function calling.

### FAHRN — Fractal Adaptive Holonic Reasoning Network (`FAHRNManager`)
Multi-agent meta-orchestrator. Routes problems to a network of specialised AI agents using composite scoring (speed, cost, quality, karma). Five dispatch modes: **Serial, Parallel, Debate, Voting, Decomposed**. Built-in loop detection, agent budget guard (auto-stop at token/cost ceiling), SkillOpt self-improvement (Microsoft Research arXiv:2605.23904, +23.5% avg improvement), and ML.NET in-process task classification.

### Holonic BRAID (`HolonicBraidManager`)
Shared reasoning-graph memory stored as holons in COSMIC ORM. Agents contribute to and reuse Mermaid execution plans across sessions and avatars, improving over time via EMA accuracy scoring.

### Holonic Memory (`HolonicMemoryManager`)
Fractal memory hierarchy (Session → Agent → User → Group → Neighbourhood → District → City → County → Country → Continent → Earth). Membrane rules govern what propagates upward. Semantic search via embedding cosine similarity. TTL-based retention policies. External memory provider integration (Mem0, Zep, Letta, LangMem, Graphiti).

### Per-Avatar Key Vault + Cost Metering
Encrypted per-avatar provider API keys stored in holon MetaData. Monthly USD budget and daily token quota enforcement with 429 + Retry-After on breach. Full per-request usage tracking.

### Agent Protocols (MCP · A2A · ACP · ANP · GraphQL)
HTTP MCP transport at `/mcp`, A2A task lifecycle at `/a2a/*`, BeeAI ACP, DID-based ANP, GraphQL, Kafka, AMQP, MQTT — all routed through `OrchestratorManager`.

### Identity & Trust (DID / Verifiable Credentials)
W3C DID creation (did:key, deterministic from avatar GUID), Universal Resolver integration, Verifiable Credential issuance and verification (HMAC-SHA256 proof). JWT bearer auth on all write endpoints.

### Real-time Telemetry
In-process ring buffer (500 events), SSE stream at `GET /v1/telemetry/stream`, replay at `GET /v1/telemetry/history`. Every completion request publishes provider, model, tokens, cost, and latency.

### ML.NET In-Process Machine Learning
Zero-latency task classification, sentiment analysis, and loop anomaly scoring — no LLM call needed. Trains and improves from FAHRN dispatch history.

### MCP Server
Exposes the full WEB4–WEB10 stack as 101 typed named tools directly inside Cursor, VS Code, and Claude Desktop. See [MCP Server README](NextGenSoftware.OASIS.MCP.Server/README.md).

---

## REST API Reference

Full Swagger UI: `https://api.web6.oasisomniverse.one/swagger`
Full docs: [`Docs/Devs/API Documentation/WEB6/`](../Docs/Devs/API%20Documentation/WEB6/)

### AI Completion

| Method | Path | Description |
|---|---|---|
| `POST` | `/v1/complete` | Unified AI completion (15+ providers, auto routing, failover) |
| `POST` | `/v1/complete/stream` | Streaming SSE completion |
| `POST` | `/v1/complete/tool-result` | Feed tool call result back to continue agent loop |
| `POST` | `/v1/embed` | Text embeddings (OpenAI / Cohere / HuggingFace) |
| `GET` | `/v1/images/generate` | Image generation (StabilityAI, OpenAI) |
| `POST` | `/v1/images/generate` | Image generation |

### FAHRN Multi-Agent

| Method | Path | Description |
|---|---|---|
| `POST` | `/v1/fahrn/solve` | Hero endpoint — full pipeline in one call (classify → BRAID → dispatch → score) |
| `GET` | `/v1/fahrn/budget-estimate` | Dry-run cost/token estimate for a DispatchRequest |
| `GET` | `/v1/reasoning-network/agents` | List all registered FAHRN agents |
| `POST` | `/v1/reasoning-network/agents` | Register a new FAHRN agent |
| `POST` | `/v1/reasoning-network/agents/seed-openserv` | Auto-seed agents from OpenSERV model catalogue |
| `POST` | `/v1/reasoning-network/dispatch` | Dispatch a problem to the agent network |

### Holonic BRAID Graphs

| Method | Path | Description |
|---|---|---|
| `GET` | `/v1/holonic-braid/graph/{taskType}` | Get the shared reasoning graph for a task type |
| `POST` | `/v1/holonic-braid/graph/{taskType}` | Store a new reasoning graph |
| `POST` | `/v1/holonic-braid/graph/{id}/outcome` | Record solver outcome (updates EMA accuracy score) |

### Holonic Memory

| Method | Path | Description |
|---|---|---|
| `GET` | `/v1/holonic-memory/earth` | Get or create the planetary Earth holon |
| `POST` | `/v1/holonic-memory/holons` | Get or create a holon at a given level |
| `PUT` | `/v1/holonic-memory/holons/{id}/membrane-rule` | Set the membrane propagation rule |
| `POST` | `/v1/holonic-memory/holons/{id}/memory` | Record a memory item |
| `POST` | `/v1/holonic-memory/holons/{id}/propagate` | Propagate permitted items to parent (single hop) |
| `POST` | `/v1/holonic-memory/holons/{id}/propagate-up` | Multi-hop propagation (`?levels=N`, max = Earth) |
| `GET` | `/v1/holonic-memory/holons/{id}/memory/search` | Semantic search (`?q=...&topK=5&provider=auto`) |

### External Memory Providers

| Method | Path | Description |
|---|---|---|
| `GET` | `/v1/memory/external/providers` | List configured external memory providers |
| `POST` | `/v1/memory/external/search` | Semantic search across all providers (Mem0/Zep/Letta/LangMem/Graphiti) |
| `POST` | `/v1/memory/external/add` | Add a memory to a specific provider |
| `DELETE` | `/v1/memory/external/{provider}/{id}` | Delete a specific external memory |

### Orchestrators

| Method | Path | Description |
|---|---|---|
| `GET` | `/v1/orchestrators` | List registered orchestrator adapters |
| `POST` | `/v1/orchestrators` | Register an orchestrator adapter |
| `POST` | `/v1/orchestrators/invoke` | Invoke an adapter (MCP/A2A/ACP/ANP/GraphQL/Kafka/AMQP/MQTT) |

### A2A (Agent-to-Agent Protocol)

| Method | Path | Description |
|---|---|---|
| `POST` | `/a2a/tasks/send` | Receive an A2A task from a peer agent |
| `GET` | `/a2a/tasks/{id}` | Poll task status |
| `GET` | `/a2a/tasks/{id}/events` | SSE stream of task state changes |
| `POST` | `/a2a/tasks/{id}/cancel` | Cancel a running task |

### Identity & Auth

| Method | Path | Description |
|---|---|---|
| `POST` | `/v1/auth/did` | DID-based authentication |
| `GET` | `/v1/auth/did/{*did}` | Resolve a W3C DID |
| `POST` | `/v1/auth/vc` | Issue a Verifiable Credential |
| `POST` | `/v1/auth/vc/verify` | Verify a Verifiable Credential |

### Keys & Usage

| Method | Path | Description |
|---|---|---|
| `GET` | `/v1/keys` | List avatar's stored provider keys |
| `POST` | `/v1/keys` | Save an encrypted provider key |
| `DELETE` | `/v1/keys/{provider}` | Delete a provider key |
| `GET` | `/v1/usage` | Per-avatar token/cost usage summary |

### ML.NET

| Method | Path | Description |
|---|---|---|
| `GET` | `/v1/ml/models` | List in-process ML models and their status |
| `POST` | `/v1/ml/classify-task` | Classify problem text to FAHRN task category |
| `POST` | `/v1/ml/sentiment` | Sentiment analysis (Positive / Neutral / Negative) |
| `POST` | `/v1/ml/train` | Train task classifier from labelled samples |

### SkillOpt

| Method | Path | Description |
|---|---|---|
| `GET` | `/v1/reasoning-network/agents/{agentId}/skills/{category}` | Get current best skill document for an agent |
| `POST` | `/v1/reasoning-network/agents/{agentId}/skills/{category}/evolve` | Run a SkillOpt epoch to improve the skill |

### Avatar Context

| Method | Path | Description |
|---|---|---|
| `GET` | `/v1/context/avatar/{avatarId}` | Rich avatar context block (karma, quests, OAPPs, Web7/8 membership) |

### Telemetry & Health

| Method | Path | Description |
|---|---|---|
| `GET` | `/v1/telemetry/stream` | Real-time SSE stream of per-request telemetry events |
| `GET` | `/v1/telemetry/history` | Historical telemetry replay (`?limit=N`) |
| `GET` | `/v1/health` | API health check |
| `GET` | `/v1/providers/status` | Live health and latency per AI provider |

### WebSocket Sessions

| Method | Path | Description |
|---|---|---|
| `GET` | `/v1/ws/session` | Upgrade to WebSocket for a bidirectional agent session |

### MCP & Discovery

| Method | Path | Description |
|---|---|---|
| `GET` | `/mcp` | MCP SSE stream (HTTP MCP transport) |
| `POST` | `/mcp` | MCP JSON-RPC tool calls |
| `GET` | `/.well-known/mcp.json` | MCP discovery document |
| `GET` | `/.well-known/agent.json` | A2A agent card |

---

## Install

### MCP Server (for IDE use)

```bash
# npm — no .NET SDK required (recommended)
npm install -g @oasisomniverse/mcp-server

# NuGet dotnet tool — requires .NET 8+ SDK
dotnet tool install -g NextGenSoftware.OASIS.MCP.Server
```

### Web6 Core / WebAPI (for .NET projects)

```bash
dotnet add package NextGenSoftware.OASIS.Web6.Core
dotnet add package NextGenSoftware.OASIS.Web6.WebAPI
```

---

## Publishing

| Workflow | What it publishes |
|---|---|
| `publish-nuget.yml` | All OASIS NuGet packages including Web6.Core and Web6.WebAPI |
| `publish-mcp.yml` | MCP Server: platform binaries → GitHub release → NuGet → npm |

---

## Links

- [oasisomniverse.one](https://oasisomniverse.one) — main site
- [oasisweb4.com/products/mcp](https://oasisweb4.com/products/mcp) — MCP product page & pricing
- [WEB6 API Reference](../Docs/Devs/API%20Documentation/WEB6/WEB6_REST_API_Reference.md)
- [MCP Tool Reference](../Docs/Devs/API%20Documentation/WEB6/WEB6_MCP_Tool_Reference.md)
- [WEB6 User Guide](../Docs/Devs/API%20Documentation/WEB6/WEB6_User_Guide.md)
- [OASIS GitHub](https://github.com/NextGenSoftwareUK/OASIS)
