# WEB6 — OASIS AI Abstraction & Orchestration Layer

WEB6 is the unified AI layer of the [OASIS Omniverse](https://oasisomniverse.one) stack. It sits on top of WEB4 (identity, karma, COSMIC ORM, NFTs) and WEB5 (STAR ODK, holon graph, STARNET), providing a single API surface for AI completions, multi-agent orchestration, semantic memory, and IDE tooling — regardless of which underlying AI provider, blockchain, or storage layer you're using.

---

## Projects in this folder

| Project | NuGet | Description |
|---|---|---|
| [`NextGenSoftware.OASIS.Web6.Core`](NextGenSoftware.OASIS.Web6.Core/) | `NextGenSoftware.OASIS.Web6.Core` | Business logic layer — all managers, models, enums |
| [`NextGenSoftware.OASIS.Web6.WebAPI`](NextGenSoftware.OASIS.Web6.WebAPI/) | `NextGenSoftware.OASIS.Web6.WebAPI` | ASP.NET Core REST API — all controllers |
| [`NextGenSoftware.OASIS.MCP.Server`](NextGenSoftware.OASIS.MCP.Server/) | `NextGenSoftware.OASIS.MCP.Server` · `@oasisomniverse/mcp-server` | MCP Server — 60+ tools for IDEs |
| [`npm/`](npm/) | — | npm shim package source (`@oasisomniverse/mcp-server`) |

---

## Key capabilities

### AI Provider Abstraction (`AIProviderManager`)
Single `POST /v1/complete` endpoint routes to OpenAI, Anthropic, Gemini, Groq, Mistral, Cohere, xAI, DeepSeek, Ollama, OpenSERV, and 8 more — normalised request/response shape across all of them. Automatic failover, cost/latency/quality-based routing.

### FAHRN — Fractal Adaptive Holonic Reasoning Network (`FAHRNManager`)
Multi-agent meta-orchestrator. Routes problems to a network of specialised AI agents using composite scoring (speed, cost, quality, karma). Five dispatch modes: Serial, Parallel, Debate, Voting, Decomposed. Built-in loop detection and agent budget guard (auto-stop at token/cost threshold).

### Holonic BRAID (`HolonicBraidManager`)
Shared reasoning-graph memory stored as holons in COSMIC ORM. Agents contribute to and reuse Mermaid execution plans across sessions and avatars, improving over time via EMA-based accuracy scoring.

### Per-Avatar Key Vault + Cost Metering (`KeyVaultManager`, `UsageMeteringManager`)
Encrypted per-avatar provider API keys stored in holon MetaData. Monthly USD budget and daily token quota enforcement with 429 + Retry-After on breach.

### Semantic Cache (`SemanticCacheManager`)
In-memory cosine-similarity cache. Embeds the last user message, returns cached responses on semantic match (default threshold 0.95), LRU eviction.

### Holonic Memory (`HolonicMemoryManager`)
Fractal memory layers (Session → Task → Project → Avatar → Global) stored as holons. Membrane rules control propagation between layers.

### MCP Server (`NextGenSoftware.OASIS.MCP.Server`)
Exposes the full WEB4–WEB10 stack as MCP tools directly inside Cursor, VS Code, and Claude Desktop. See [MCP Server README](NextGenSoftware.OASIS.MCP.Server/README.md).

---

## REST API quick reference

```
POST   /v1/complete                  Unified AI completion (all providers)
POST   /v1/complete/stream           SSE streaming completion
POST   /v1/complete/tool-result      Submit tool call result, continue loop
GET    /v1/embeddings                Generate embeddings
POST   /v1/fahrn/solve               FAHRN multi-agent solve
GET    /v1/fahrn/budget-estimate     Dry-run cost estimate
POST   /v1/holonic-braid/graphs      Store reasoning graph
GET    /v1/holonic-braid/graphs      List reasoning graphs
POST   /v1/holonic-memory/holons     Create memory holon
GET    /v1/holonic-memory/holons/{id}/memory   Read memories
POST   /v1/keys                      Save encrypted provider API key
GET    /v1/keys                      List stored provider keys
DELETE /v1/keys/{provider}           Delete provider key
GET    /v1/usage                     Per-avatar token/cost usage summary
GET    /v1/providers/status          Live health/latency per provider
GET    /v1/context/avatar/{id}       Rich avatar context (karma + quests)
GET    /v1/health                    API health check
```

Full Swagger UI: `https://api.web6.oasisomniverse.one/swagger`

---

## Install

### MCP Server (for IDE use)

```bash
# npm (no .NET SDK required)
npm install -g @oasisomniverse/mcp-server

# NuGet dotnet tool
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
| `publish-mcp.yml` | MCP Server only: platform binaries → GitHub release → NuGet → npm |

To publish the MCP Server: trigger `publish-mcp.yml` from GitHub Actions with a version number. Requires `NPM_TOKEN` secret set in repo settings.

---

## Links

- [oasisomniverse.one](https://oasisomniverse.one)
- [oasisweb4.com/products/mcp](https://oasisweb4.com/products/mcp) — MCP product page & pricing
- [WEB6 Action Plan](WEB6-ACTION-PLAN.md) — full implementation roadmap
- [OASIS GitHub](https://github.com/NextGenSoftwareUK/OASIS)
