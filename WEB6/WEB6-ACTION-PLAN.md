# WEB6 — Full Action Plan
## AI Abstraction & Aggregation Layer: Complete Integration Roadmap

_Generated: 2026-07-10_

---

## Appendix A — Original GPT Conversation (Reference)

_The conversation below was the starting point for this action plan. It covers the current AI protocol landscape, GPT's analysis of the WEB6 site and whitepaper, and the framing of OASIS as a universal interoperability layer._

---

**User:** What are all the current AI protocols, standards, APIs etc?

**GPT:** There are now quite a few, and the ecosystem is finally starting to converge around a handful of major protocols. I would group them like this:

| Category | Examples | Purpose |
|---|---|---|
| AI Model APIs | OpenAI API, Anthropic API, Google Gemini API, xAI API, Mistral API, Cohere API | Access AI models |
| Agent Protocols | MCP, A2A, ACP, ANP | Agents communicating with tools and each other |
| Context Standards | MCP, OpenAPI, JSON Schema | Tool descriptions and structured context |
| Memory Standards | Mem0, Zep, Letta Memory, LangMem | Persistent AI memory |
| Multi-Agent Frameworks | LangGraph, AutoGen, CrewAI, Semantic Kernel | Building agent systems |
| Workflow Standards | Temporal, Prefect, Airflow, n8n | AI orchestration |
| Vector Standards | pgvector, Milvus, Qdrant, Pinecone APIs | Semantic search |
| Identity | OAuth2, OpenID Connect, DID, Verifiable Credentials | Authentication and decentralized identity |
| AI Safety | Model Context Protocol permissions, Guardrails, Llama Guard | Secure AI execution |
| Data Formats | JSON, JSON Schema, OpenAPI, Protocol Buffers, Arrow | Structured data exchange |

**The biggest AI protocols today**

**MCP (Model Context Protocol)**

Probably the biggest development of 2025. Created by Anthropic and now supported by OpenAI, Google, Microsoft and many others. It standardizes how AI models connect to databases, files, GitHub, Slack, APIs, tools, and local applications. Think of it as USB-C for AI.

**A2A (Agent-to-Agent)**

Developed by Google. Allows completely different AI agents to discover and communicate with one another. Example: Sales Agent, Finance Agent, Coding Agent, Customer Support Agent — all can negotiate and exchange tasks.

**ACP (Agent Communication Protocol)**

Originally from BeeAI/IBM. Designed for interoperability between autonomous agents.

**ANP (Agent Network Protocol)**

One of the newer emerging protocols. Focuses on decentralized networks of AI agents rather than simple pairwise communication.

**AI APIs**

The major model providers include: OpenAI, Anthropic, Google Gemini, xAI (Grok), Mistral, Cohere, DeepSeek, Moonshot AI, Together AI, Fireworks AI, Groq, Cerebras, Ollama (local), Hugging Face Inference API.

**Agent Frameworks**

These help developers build AI systems: LangChain, LangGraph, AutoGen, CrewAI, Semantic Kernel, LlamaIndex, OpenAI Agents SDK, Mastra, Letta, SmolAgents.

**Memory systems**

Persistent memory is becoming essential. Examples: Mem0, Zep, Letta Memory, LangMem, Graphiti, Redis Vector Memory.

**Vector databases**

Used for Retrieval-Augmented Generation (RAG): Pinecone, Weaviate, Milvus, Qdrant, Chroma, pgvector, LanceDB, FAISS.

**Identity**

For AI acting on behalf of users: OAuth2, OpenID Connect, DID (Decentralized Identifiers), Verifiable Credentials, Passkeys, OAuth Device Flow.

**Tool description standards**

OpenAPI, JSON Schema, AsyncAPI, GraphQL, MCP Tool Schema.

**Orchestration**

Temporal, Prefect, Airflow, n8n, LangGraph, Dagster.

**Messaging/Event Protocols**

HTTP, WebSockets, Server-Sent Events (SSE), gRPC, MQTT, AMQP, Kafka.

**Decentralized AI**

This area is growing rapidly: Bittensor, Akash, Gensyn, Prime Intellect, Ritual, Nous Research, GaiaNet. These aim to decentralize AI compute, model training, or inference.

**Where OASIS could fit**

From everything you've shared about the OASIS Omniverse architecture, you're already thinking beyond a single protocol. OASIS could act as an abstraction layer that unifies many of these standards:

- MCP for AI-to-tool communication.
- A2A/ACP/ANP for agent-to-agent collaboration.
- OpenAPI, GraphQL, and AsyncAPI for existing Web2/Web3 services.
- OAuth2, DID, and Verifiable Credentials for identity.
- HTTP, WebSockets, gRPC, MQTT, and Kafka for transport.
- Your Hyperdrive concept could abstract storage and synchronization across local devices, cloud services, databases, blockchains, and decentralized networks.
- Your STAR API could provide higher-level concepts like quests, missions, karma, worlds, digital assets, and AI agents while hiding the underlying protocol differences.

That would make OASIS less of a competitor to MCP or A2A and more of a universal interoperability layer — similar to how Hyperdrive abstracts storage, but for AI, services, and decentralized infrastructure as a whole.

Overall, I'd say there are 40–60 significant AI standards, protocols, APIs, and frameworks today, but only a handful — especially MCP, A2A, OpenAPI, OAuth2/OpenID Connect, and JSON Schema — are emerging as the foundational building blocks that much of the ecosystem is converging on.

---

**User:** Yes that's the idea, you see WEB6 yet? web6.oasisomniverse.one

**GPT:** I had a look, and I actually really like where you're taking it.

The thing that stood out most to me is that Web6 isn't presented as just "AI". It's presented as an AI abstraction and aggregation layer, which is much more interesting and, in my opinion, much more defensible.

A few parts I thought were particularly strong:

- One API to every AI (OpenAI, Claude, Gemini, Ollama, etc.). That's immediately understandable.
- Holonic Braid as collective, hierarchical memory. That's a genuinely distinctive idea if you can execute it well.
- Meta-Orchestration Reasoning Network with routing between multiple models based on capability, speed, and fallback. That aligns with where the industry is heading (model routers, mixtures of experts, agent swarms).
- Positioning MCP as just one of many protocols that OASIS supports rather than the protocol itself.

The one thing I'd probably push even further is the protocol story.

Right now the page says things like:

> "One API to route, orchestrate and normalise across every AI model, agent and protocol."

I think that's true, but it doesn't yet communicate the full scope. I'd lean into something like:

> WEB6 is to AI what WEB4 is to data.
> Web4 abstracts databases, clouds, blockchains and storage.
> Web6 abstracts models, agents, memories, tools, protocols and reasoning.

That parallel is really elegant because people already understand your Web4 abstraction layer.

I'd also make it explicit that Web6 can sit on top of virtually every emerging AI standard:

- MCP, A2A, ACP, ANP
- OpenAPI, GraphQL, JSON Schema
- OAuth, OpenID Connect
- Vector databases, Memory providers, Agent frameworks

That reinforces the message that OASIS doesn't replace these standards — it unifies them.

The other idea that immediately came to mind is something you briefly mentioned in previous chats: if HyperDrive is your universal routing engine for data, Web6 could have an equivalent routing engine for intelligence. Rather than simply routing API calls, it could dynamically choose the best model, the best agent, the best protocol, the best memory, the best tool, and the best execution strategy (parallel, serial, debate, decomposition, voting, etc.)

That would make Web6 feel like a true "AI operating system" rather than just another AI gateway.

I actually think this is one of the strongest conceptual parts of the OASIS stack. Web4 solves interoperability for data; Web6 could solve interoperability for intelligence. If you can demonstrate that with working examples, it's a much more compelling story than simply claiming "we support lots of AI models."

---

**User:** Yes it's moving that way... you see FAHRN and the Whitepaper?

**GPT:** Yes, I found it, and I think FAHRN is one of the more interesting concepts you've introduced. From what I saw, it's not simply another agent framework — it appears to be the layer that coordinates distributed reasoning, memory, and execution across the Web6 architecture, alongside the Holonic BRAID shared memory concept.

The overall progression is becoming much clearer to me:

- Web4 → Universal data abstraction (HyperDrive, Cosmic ORM, identity, storage).
- Web5 → Applications, games, STARNET, Smart Bricks, quests, assets, worlds.
- Web6 → AI abstraction, orchestration, protocols, models, agents, and collective intelligence.
- FAHRN → The reasoning/network layer that coordinates autonomous AI rather than just calling models.

One thing I especially like is that you're not positioning Web6 as "our own LLM." Instead, it looks like an operating layer above the AI ecosystem. If executed well, that's a much broader vision because new models and protocols can be added underneath without changing developer code.

One suggestion I'd make for the whitepaper is to sharpen the distinction between three layers:

- **Model Layer** — GPT, Claude, Gemini, local models, etc.
- **Agent Layer** — MCP, A2A, tools, memories, workflows, protocols.
- **Intelligence Layer (FAHRN)** — reasoning, planning, routing, negotiation, shared memory, learning, and coordination.

That separation would help readers immediately understand why FAHRN exists instead of assuming it's just another agent framework.

I also think there's an opportunity to make a stronger claim about interoperability. Today, most platforms are converging on one standard (such as MCP). Your architecture appears to be aiming for protocol neutrality — supporting MCP, A2A, OpenAPI, GraphQL, OAuth, and future protocols simultaneously through a single abstraction layer. That's a stronger long-term position than depending on any one emerging standard.

Overall, the vision feels much more coherent now than it did a few months ago. The "Web4 = data abstraction" and "Web6 = intelligence abstraction" parallel gives people a mental model they can grasp quickly, while FAHRN provides the engine that ties the AI side together. I think that's one of the concepts worth emphasizing most in the whitepaper.

---

## 1. Current Stack Map

Every layer has its own WebAPI and its own set of MCP tools. Nothing is missing at the individual-layer level — the gap is in cross-layer integration and the unified entry points.

| Layer | WebAPI Project | MCP Tools | Key Surface |
|---|---|---|---|
| **Web4** | `ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI` | `Web4Tools.cs`, `Web4Web5Tools.cs` | Avatar, Karma, Wallet, NFT, Holon CRUD, Map, HyperDrive, Search, SCMS, Bridge (~50 controllers) |
| **Web5** | `STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI` | `Web5Tools.cs`, `Web4Web5Tools.cs` | Quests, Missions, OAPPs, CelestialBodies, STARNET, GeoNFTs, Templates, Runtimes, Games (~30 controllers) |
| **Web6** | `WEB6/NextGenSoftware.OASIS.Web6.WebAPI` | `Web6Tools.cs` | AI completion (15+ providers), FAHRN dispatch, Holonic BRAID, fractal memory hierarchy, orchestrator adapters (MCP/A2A/LangChain/AutoGen/CrewAI/SemanticKernel) |
| **Web7** | `WEB7/NextGenSoftware.OASIS.Web7.WebAPI` | `Web7Tools.cs` | Symbiosis sessions, bio-signal DSP (EEG/HRV/GSR), collective consciousness spaces |
| **Web8** | `WEB8/NextGenSoftware.OASIS.Web8.WebAPI` | `Web8Tools.cs` | Galactic mesh nodes, Dijkstra shortest-path routing, self-healing relay, protocol bridge |
| **Web9** | `WEB9/NextGenSoftware.OASIS.Web9.WebAPI` | `Web9Tools.cs` | Singularity aggregation — live unified health across Web4–Web8 |
| **Web10** | `WEB10/NextGenSoftware.OASIS.Web10.WebAPI` | `Web10Tools.cs` | The Source — root OASIS identity + Web9 unified status |
| **MCP** | `C:/Source/MCP/NextGenSoftware.OASIS.MCP.Server` | All of the above | stdio MCP server wrapping Web4–Web10 in-process |

### What is already fully built and working

- `AIProviderManager` — real HTTP calls to OpenAI, Anthropic, Gemini, Groq, Mistral, Cohere, xAI, DeepSeek, HuggingFace, AzureOpenAI, AWSBedrock, Ollama, OpenServ, StabilityAI
- `FAHRNManager` — composite scoring, EMA continuous learning, Serial/Parallel/Decomposed dispatch, loop detection, BRAID graph reuse
- `HolonicBraidManager` — shared reasoning-graph library with conflict resolution, accuracy tracking, EMA regeneration threshold, cross-chain persistence
- `HolonicMemoryManager` — fractal hierarchy Session→Agent→User→Group→Neighbourhood→District→City→County→Country→Continent→Earth with membrane propagation
- `OrchestratorManager` — full MCP Streamable HTTP handshake (initialize → notifications/initialized → tools/call), A2A/LangChain/AutoGen/CrewAI/SemanticKernel webhook adapters
- All Web4–Web10 WebAPI layers with their own controllers
- MCP server with in-process tools for all layers

### What is missing (the actual gaps)

1. HTTP MCP transport (MCP server is stdio-only; no `/mcp` endpoint on Web6 WebAPI)
2. `web6_fahrn_solve` hero endpoint (full pipeline in one call — not in WebAPI or MCP)
3. Cross-layer integration in Web6 (Web4/5 avatar context, Web7 symbiosis hints, Web8 mesh routing, Web9 health gating)
4. `web7/8/9/10_request` HTTP passthrough MCP tools (same pattern as existing `web4_request`/`web5_request`)
5. A2A proper protocol (current implementation is a webhook shim; needs Agent Card + task endpoints)
6. Streaming SSE on `/v1/complete`
7. Embeddings endpoint (`/v1/embed`)
8. Tool/function calling in `CompletionRequest`
9. More AI providers (Cerebras, Together AI, Fireworks AI, Perplexity, Moonshot AI, etc.)
10. `FAHRN Debate` and `Voting` dispatch modes
11. Automatic task classification (currently caller must supply `taskType` manually)
12. Per-avatar API key management, cost metering, usage quotas
13. Semantic caching layer

---

## 2. Priority-Ordered Action Plan

---

### PRIORITY 1 — HTTP MCP Transport

**The single highest-leverage change. Turns the entire OASIS stack into a universal AI connector reachable by any MCP-compatible client anywhere.**

#### Why it matters

The current MCP server uses `.WithStdioServerTransport()`. This works for Claude Desktop and Cursor running locally, but blocks:
- Claude.ai connectors (require HTTP)
- OpenAI GPT Actions (require HTTP)
- Any cloud-hosted AI agent or automation
- The Web6 API calling back into OASIS tools during FAHRN dispatch
- Browser-based or mobile clients

#### Two complementary options — implement both

**Option A: Upgrade the standalone MCP server to HTTP**

File: `C:/Source/MCP/NextGenSoftware.OASIS.MCP.Server/Program.cs`

Convert to a full ASP.NET host and add `.WithHttpServerTransport()` alongside the existing stdio transport. Deploy at `mcp.oasisomniverse.one`. Serves claude.ai connectors and any client that needs a dedicated MCP endpoint without coupling to the Web6 API lifecycle.

```csharp
// Program.cs changes:
// 1. Replace Host.CreateApplicationBuilder with WebApplication.CreateBuilder
// 2. Add: builder.Services.AddMcpServer(...).WithHttpServerTransport().WithToolsFromAssembly();
// 3. Also keep: .WithStdioServerTransport() so existing Claude Desktop / Cursor configs keep working
// 4. app.MapMcp("/mcp");
```

**Option B: Embed MCP inside the Web6 WebAPI**

File: `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Program.cs`

Add `ModelContextProtocol.Server` NuGet to `NextGenSoftware.OASIS.Web6.WebAPI.csproj`. Register all tool type assemblies and mount the HTTP transport at `/mcp`. This is the preferred long-term home because:
- Single deployment, single OASIS boot, single JWT auth flow
- The Web6 API already has all managers in memory — no inter-process call overhead
- Callers get MCP and REST from the same base URL (`api.web6.oasisomniverse.one`)

```csharp
// In Program.cs, in the services section:
builder.Services
    .AddMcpServer(options => {
        options.ServerInfo = new Implementation { Name = "oasis-web4-to-web10-mcp", Version = "1.0.0" };
    })
    .WithHttpServerTransport()
    .WithToolsFromAssembly(typeof(NextGenSoftware.OASIS.MCP.Server.Tools.Web6Tools).Assembly)
    .WithToolsFromAssembly(typeof(NextGenSoftware.OASIS.MCP.Server.Tools.Web4Tools).Assembly);
    // ... other tool assemblies

// After app.Build():
app.MapMcp("/mcp");
```

#### MCP discovery document

Add `GET /.well-known/mcp.json` — how Claude.ai and other MCP clients auto-discover available tools:

```json
{
  "schema_version": "v1",
  "name_for_human": "OASIS WEB4–WEB10",
  "name_for_model": "oasis",
  "description_for_human": "Universal AI abstraction layer — Web4 identity/data, Web5 apps/quests, Web6 AI/FAHRN, Web7 symbiosis, Web8 mesh, Web9 singularity, Web10 source.",
  "description_for_model": "Access all OASIS capabilities: avatar/karma (web4), quests/missions/OAPPs (web5), AI completion/FAHRN dispatch/holonic-braid (web6), bio-signal symbiosis (web7), galactic mesh routing (web8), unified status (web9), root identity (web10).",
  "auth": { "type": "bearer" },
  "api": { "type": "mcp", "url": "https://api.web6.oasisomniverse.one/mcp" }
}
```

Also add `GET /.well-known/agent.json` (Google A2A agent card — see Priority 4).

---

### PRIORITY 2 — `web6_fahrn_solve` — The Hero Endpoint

**The single entry point that demonstrates the full WEB6 vision in one call. Currently absent from both the WebAPI and the MCP server.**

#### REST endpoint

`POST /v1/fahrn/solve`

Request:
```json
{
  "problem": "Explain the trade-offs between serial and parallel agent dispatch in multi-agent reasoning systems.",
  "avatarId": "optional-guid",
  "taskType": "auto",
  "returnReasoning": true,
  "injectAvatarContext": true,
  "scoringWeights": { "categoryWeight": 0.5, "speedWeight": 0.2, "costWeight": 0.3 }
}
```

Response:
```json
{
  "answer": "...",
  "reasoningTrace": "...",
  "mermaidPlan": "graph TD\n...",
  "taskTypeClassified": "reasoning",
  "modeUsed": "Parallel",
  "agentsUsed": ["openserv-claude-opus-4.6", "openserv-o3"],
  "braidGraphId": "guid",
  "braidGraphWasReused": true,
  "avatarContextInjected": true,
  "totalLatencyMs": 1842,
  "totalTokensUsed": 2310,
  "providers": ["Anthropic", "OpenAI"]
}
```

#### Pipeline inside `FahrnSolveManager.SolveAsync()` (new manager, or extend FAHRNManager)

1. **Classify task type** — if `taskType = "auto"`, call a small fast model (Groq Llama) with: _"Classify this problem into exactly one of: code / reasoning / writing / mathematics / legal / architecture / real-time / general. Reply with only the category word."_ — use the result for all subsequent scoring
2. **Avatar context** — if `avatarId` set and `injectAvatarContext = true`, call Web4 (karma, avatar profile) and Web5 (active quests, missions) in parallel via `HttpClient`; assemble `[OASIS Avatar Context]` block
3. **Web9 health gate** — call `SingularityAggregationManager.GetUnifiedStatusAsync()`; if Web4 storage is degraded, disable BRAID persistence; if a specific provider is flagged unhealthy, set its composite score to 0 before dispatch
4. **FAHRN dispatch** — `FAHRNManager.DispatchAsync()` with enriched request (classified task type, avatar context already injected into problem string)
5. **BRAID graph injection** — if an existing graph was found and reused, note `braidGraphWasReused = true` and the graph's `TimesReused` count
6. **EMA score update** — already happens inside `FAHRNManager`
7. **Session memory recording** — already happens inside `FAHRNManager`
8. **Assemble and return** — extract the final answer from the winning agent's completion response; include full trace if `returnReasoning = true`

#### MCP tool

Add to `Web6Tools.cs`:

```csharp
[McpServerTool(Name = "web6_fahrn_solve")]
[Description("WEB6 FAHRN: the hero endpoint — takes a natural-language problem and runs the full pipeline: auto-classify task type, inject avatar context (Web4+Web5), gate on Web9 health, FAHRN dispatch (Serial/Parallel/Decomposed), inject Holonic BRAID graph if one exists, loop-detect, EMA-update agent scores, record session memory — returning the answer, reasoning trace, Mermaid plan, and full telemetry in one call.")]
public static async Task<string> FahrnSolve(FahrnSolveRequest request, string? avatarId = null)
{
    request.AvatarId = request.AvatarId == Guid.Empty ? ParseAvatarId(avatarId) : request.AvatarId;
    FahrnSolveManager manager = new FahrnSolveManager(request.AvatarId);
    var result = await manager.SolveAsync(request);
    return JsonSerializer.Serialize(result);
}
```

---

### PRIORITY 3 — Cross-Layer Integration in Web6

**Web6 is the AI layer — it must be able to reach Web4–Web10 to ground AI reasoning in real OASIS state.**

#### 3a. Avatar context injection in `/v1/complete`

Add to `CompletionRequest`:
```csharp
public bool? InjectAvatarContext { get; set; }  // null = use DNA default
```

In `CompletionController.Complete()`, before the provider call:

```csharp
if (AvatarId != Guid.Empty && (request.InjectAvatarContext ?? web6?.InjectAvatarContext ?? false))
{
    // Call Web4 + Web5 in parallel
    Task<string> karmaTask   = CallWeb4Async($"/api/karma/{AvatarId}");
    Task<string> questsTask  = CallWeb5Async($"/api/quests/avatar/{AvatarId}");
    await Task.WhenAll(karmaTask, questsTask);

    string contextBlock =
        $"[OASIS Avatar Context]\n" +
        $"Karma: {ParseKarma(karmaTask.Result)}\n" +
        $"Active Quests: {ParseQuests(questsTask.Result)}\n";

    InjectIntoSystemContext(request, contextBlock);
}
```

DNA config: `OASIS.Web6.InjectAvatarContext` (bool, default false).
Base URLs: `OASIS.Web6.Web4BaseUrl` / `OASIS.Web6.Web5BaseUrl` (env var overrides).

This grounds every AI response in the avatar's actual OASIS state — no other AI API does this.

#### 3b. Web9 health gating in FAHRN

In `FAHRNManager.DispatchAsync()`, add a pre-dispatch health check:

```csharp
// Before scoring agents:
Web9HealthSnapshot health = await GetWeb9HealthAsync();
if (health.Web4StorageDegraded)
    _skipBraidPersistence = true;

// When scoring:
double ComputeCompositeScore(ReasoningAgentMetadata agent, ...)
{
    if (health.DegradedProviders.Contains(agent.Provider.ToString()))
        return 0.0;  // exclude degraded providers entirely
    // ... existing scoring logic
}
```

`Web9HealthSnapshot` is populated by calling `SingularityAggregationManager.GetUnifiedStatusAsync()` (in-process) or `GET /v1/singularity/status` (if Web9 is a separate deployment). Cache the result for 30 seconds to avoid adding latency to every dispatch.

#### 3c. Web7 symbiosis hints in FAHRN dispatch

Add to `DispatchRequest`:
```csharp
public Guid? SymbiosisSessionId { get; set; }
```

In `FAHRNManager.DispatchAsync()`:

```csharp
if (request.SymbiosisSessionId.HasValue)
{
    IntentionState state = await GetSymbiosisIntentionStateAsync(request.SymbiosisSessionId.Value);

    // High cognitive load → prefer simpler/faster models (boost CostScore/SpeedScore weight)
    // High focus + low arousal → user in flow state, use aggressive parallel dispatch
    // Low focus → force Serial mode, inject more context
    if (state.CognitiveLoad > 0.8 && request.Mode == DispatchMode.Auto)
        request.Mode = DispatchMode.Serial;
    else if (state.Focus > 0.7 && state.Arousal < 0.4 && request.Mode == DispatchMode.Auto)
        request.Mode = DispatchMode.Parallel;
}
```

#### 3d. Web8 mesh routing as FAHRN transport

Add to `DispatchRequest`:
```csharp
public bool UseMeshRouting { get; set; } = false;
public Guid? SourceMeshNodeId { get; set; }
```

In `FAHRNManager.ExecuteAgentAsync()`:

```csharp
if (request.UseMeshRouting && request.SourceMeshNodeId.HasValue)
{
    // Route the completion request through the Web8 galactic mesh
    // instead of calling AIProviderManager directly.
    // Enables distributed FAHRN federations where agents live on different mesh nodes.
    MeshMessage msg = new MeshMessage
    {
        SourceNodeId = request.SourceMeshNodeId.Value,
        DestinationNodeId = agent.MeshNodeId,  // registered on the agent
        Payload = JsonSerializer.Serialize(completionRequest)
    };
    var meshResult = await _meshManager.SendMessageAsync(msg);
    // parse meshResult.Payload as CompletionResponse
}
```

---

### PRIORITY 4 — A2A Proper Protocol

**The current `OrchestratorManager` A2A implementation is a simple JSON webhook POST. Proper Google A2A requires an Agent Card + task lifecycle.**

#### 4a. OASIS as a discoverable A2A agent

Add to Web6 WebAPI:

`GET /.well-known/agent.json`

```json
{
  "name": "OASIS WEB6 FAHRN",
  "description": "Fractal Adaptive Holonic Reasoning Network — universal AI abstraction and aggregation layer (Web4–Web10)",
  "url": "https://api.web6.oasisomniverse.one",
  "version": "1.0.0",
  "documentationUrl": "https://web6.oasisomniverse.one",
  "capabilities": {
    "streaming": true,
    "pushNotifications": false,
    "stateTransitionHistory": false
  },
  "defaultInputModes": ["text/plain", "application/json"],
  "defaultOutputModes": ["text/plain", "application/json"],
  "skills": [
    {
      "id": "fahrn-solve",
      "name": "FAHRN Solve",
      "description": "Full pipeline: classify → avatar context → dispatch → BRAID → answer + reasoning trace",
      "inputModes": ["text/plain"],
      "outputModes": ["application/json"]
    },
    {
      "id": "ai-complete",
      "name": "AI Completion",
      "description": "Route chat completions across 15+ AI providers with automatic failover",
      "inputModes": ["application/json"],
      "outputModes": ["application/json"]
    },
    {
      "id": "holonic-braid",
      "name": "Holonic BRAID",
      "description": "Shared reasoning graph memory — lookup or create Mermaid execution graphs per task type",
      "inputModes": ["application/json"],
      "outputModes": ["application/json"]
    },
    {
      "id": "oasis-data",
      "name": "OASIS Data (Web4)",
      "description": "Avatar, karma, wallet, NFT, holon CRUD via COSMIC ORM across 40+ storage providers"
    }
  ]
}
```

#### 4b. A2A task endpoints

New `A2AController` in Web6 WebAPI:

```
POST /a2a/tasks/send       — receive a task from a peer A2A agent → route through FAHRN → return TaskResult
GET  /a2a/tasks/{id}       — task status
GET  /a2a/tasks/{id}/events — SSE stream of task status transitions (A2A streaming mode)
POST /a2a/tasks/{id}/cancel
```

#### 4c. Fix OrchestratorManager A2A invocation

Replace the generic webhook POST with proper A2A wire format:

```csharp
case OrchestratorProtocolType.A2A:
    // 1. GET {endpoint}/.well-known/agent.json  → discover agent card, validate skill
    // 2. POST {endpoint}/tasks/send  → { "id": uuid, "message": { "role": "user", "parts": [{"text": input}] } }
    // 3. Poll GET {endpoint}/tasks/{id} until state = "completed" | "failed"
    //    OR subscribe to SSE on GET {endpoint}/tasks/{id}/events
    return await InvokeA2AAsync(config, request);
```

---

### PRIORITY 5 — Streaming SSE on `/v1/complete`

**Every production AI app expects streaming. This is the biggest UX gap.**

#### Changes required

Add to `CompletionRequest`:
```csharp
public bool Stream { get; set; } = false;
```

Add to `CompletionController`:

```csharp
[HttpPost("complete/stream")]
public async Task CompleteStream([FromBody] CompletionRequest request)
{
    Response.ContentType = "text/event-stream";
    Response.Headers["Cache-Control"] = "no-cache";
    Response.Headers["X-Accel-Buffering"] = "no";

    // ... FAHRN + BRAID context injection (same as Complete) ...

    var manager = new AIProviderManager(request.AvatarId, OASISDNA);
    await foreach (var chunk in manager.CompleteStreamAsync(request))
    {
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n");
        await Response.Body.FlushAsync();
    }
    await Response.WriteAsync("data: [DONE]\n\n");
}
```

#### `AIProviderManager.CompleteStreamAsync()` — per-provider wiring

| Provider | Streaming approach |
|---|---|
| OpenAI / Groq / Mistral / xAI / DeepSeek / Ollama / OpenServ | Standard OpenAI SSE: `stream: true`, parse `data: {...}` lines, extract `choices[0].delta.content` |
| Anthropic | `stream: true`, parse `content_block_delta` events, extract `delta.text` |
| Gemini | `stream: true` on `streamGenerateContent`, parse SSE `data:` lines |
| Cohere | `stream: true`, parse `text-generation` events |
| AzureOpenAI | Same as OpenAI |
| HuggingFace | `stream: true` on the router endpoint |
| AWSBedrock | `ConverseStreamAsync()` — yields `ContentBlockDeltaEvent` |

Normalised chunk shape:
```csharp
public class CompletionChunk
{
    public string Delta { get; set; }
    public string Provider { get; set; }
    public string Model { get; set; }
    public bool Done { get; set; }
    public int? PromptTokens { get; set; }    // only on final chunk
    public int? CompletionTokens { get; set; } // only on final chunk
}
```

#### MCP streaming

The MCP C# SDK supports `IAsyncEnumerable<string>` return from tool methods — update `web6_complete` to yield chunks when `request.Stream = true`.

---

### PRIORITY 6 — `web7/8/9/10_request` HTTP Passthrough MCP Tools

**Gives MCP clients full coverage of every endpoint on each of the remaining WebAPIs — same pattern as the existing `web4_request` and `web5_request` tools in `Web4Web5Tools.cs`.**

Add to `Web4Web5Tools.cs` (or a new `Web7To10Tools.cs`):

```csharp
private static string Web7BaseUrl => Environment.GetEnvironmentVariable("WEB7_API_BASE_URL") ?? "https://api.web7.oasisomniverse.one";
private static string Web8BaseUrl => Environment.GetEnvironmentVariable("WEB8_API_BASE_URL") ?? "https://api.web8.oasisomniverse.one";
private static string Web9BaseUrl => Environment.GetEnvironmentVariable("WEB9_API_BASE_URL") ?? "https://api.web9.oasisomniverse.one";
private static string Web10BaseUrl => Environment.GetEnvironmentVariable("WEB10_API_BASE_URL") ?? "https://api.web10.oasisomniverse.one";

[McpServerTool(Name = "web7_request")]
[Description("WEB7 (Symbiosis): calls any endpoint of the full WEB7 REST API (symbiosis sessions, bio-signal submission, collective consciousness spaces — see https://api.web7.oasisomniverse.one/swagger). path starts with /v1/...")]
public static async Task<string> Web7Request(string httpMethod, string path, string? queryString = null, string? bodyJson = null, string? bearerToken = null)
    => await ForwardRequestAsync(Web7BaseUrl, httpMethod, path, queryString, bodyJson, bearerToken);

[McpServerTool(Name = "web8_request")]
[Description("WEB8 (Galactic Mesh): calls any endpoint of the full WEB8 REST API (node registration, heartbeats, Dijkstra routing, message relay, protocol bridge — see https://api.web8.oasisomniverse.one/swagger). path starts with /v1/...")]
public static async Task<string> Web8Request(string httpMethod, string path, string? queryString = null, string? bodyJson = null, string? bearerToken = null)
    => await ForwardRequestAsync(Web8BaseUrl, httpMethod, path, queryString, bodyJson, bearerToken);

[McpServerTool(Name = "web9_request")]
[Description("WEB9 (Singularity): calls any endpoint of the full WEB9 REST API (unified health aggregation across Web4-Web8 — see https://api.web9.oasisomniverse.one/swagger). path starts with /v1/...")]
public static async Task<string> Web9Request(string httpMethod, string path, string? queryString = null, string? bodyJson = null, string? bearerToken = null)
    => await ForwardRequestAsync(Web9BaseUrl, httpMethod, path, queryString, bodyJson, bearerToken);

[McpServerTool(Name = "web10_request")]
[Description("WEB10 (The Source): calls any endpoint of the full WEB10 REST API (root OASIS identity and Web9 unified status — see https://api.web10.oasisomniverse.one/swagger). path starts with /v1/...")]
public static async Task<string> Web10Request(string httpMethod, string path, string? queryString = null, string? bodyJson = null, string? bearerToken = null)
    => await ForwardRequestAsync(Web10BaseUrl, httpMethod, path, queryString, bodyJson, bearerToken);
```

---

### PRIORITY 7 — Embeddings Endpoint

**Needed for RAG, semantic BRAID search, and semantic caching.**

#### New `EmbeddingController` — `/v1/embed`

```
POST /v1/embed
{
  "texts": ["string1", "string2"],
  "provider": "openai" | "cohere" | "huggingface" | "auto",
  "model": "text-embedding-3-large"
}
→ { "embeddings": [[float,...], [float,...]], "provider", "model", "tokens" }
```

#### New `EmbeddingManager` (mirrors `AIProviderManager` pattern)

- **OpenAI**: `POST https://api.openai.com/v1/embeddings` — models: `text-embedding-3-large`, `text-embedding-3-small`
- **Cohere**: `POST https://api.cohere.com/v2/embed` — models: `embed-english-v3.0`, `embed-multilingual-v3.0`
- **HuggingFace**: `POST https://router.huggingface.co/v1/embeddings` — models: any sentence-transformers model

#### Semantic BRAID search

Once embeddings exist, add `HolonicBraidManager.SemanticSearchAsync(query, topK)` — embed the query, cosine-match against all stored graph `MermaidDiagram` embeddings, return the closest match even when `taskType` doesn't match exactly. This is more powerful than the current exact-string `taskType` lookup.

#### MCP tool

```csharp
[McpServerTool(Name = "web6_embed")]
[Description("WEB6: generates embeddings for one or more texts via the configured provider (OpenAI, Cohere, or HuggingFace). Returns float arrays suitable for semantic search, RAG pipelines, or cosine-similarity comparisons.")]
public static async Task<string> Embed(EmbeddingRequest request, string? avatarId = null)
```

---

### PRIORITY 8 — Tool / Function Calling

**Unlocks proper agentic workflows. Models can request tool execution; the platform runs the tool and feeds results back.**

#### Changes to `CompletionRequest`

```csharp
public List<ToolDefinition> Tools { get; set; }
// { "name": "search_oasis", "description": "...", "parameters": { JSON Schema } }

public string ToolChoice { get; set; } = "auto";
// "auto" | "none" | "required" | { "type": "function", "function": { "name": "..." } }
```

#### Changes to `CompletionResponse`

```csharp
public List<ToolCall> ToolCalls { get; set; }
// [{ "id": "call_abc", "type": "function", "function": { "name": "...", "arguments": "..." } }]
```

#### New endpoint: `POST /v1/complete/tool-result`

```json
{
  "messages": [...previous messages...],
  "toolCallId": "call_abc",
  "toolName": "search_oasis",
  "result": "{ ... tool output ... }"
}
```
This appends a `tool` role message and continues the completion loop.

#### Per-provider wiring

| Provider | Wire format |
|---|---|
| OpenAI / Groq / Mistral / xAI / Ollama / OpenServ | `"tools": [...]` in request body — standard OpenAI function-calling format |
| Anthropic | `"tools": [{"name":..., "description":..., "input_schema":...}]` — different schema, normalise |
| Gemini | `"tools": [{"functionDeclarations":[...]}]` — normalise |
| Others | Shim: inject tool descriptions into system prompt if provider doesn't support native tool calls |

#### Built-in first-party tools (register these automatically)

These are available to any model without the caller having to define them:

| Tool name | Backed by | Description |
|---|---|---|
| `oasis_avatar_get` | Web4 `AvatarManager` | Load avatar by id |
| `oasis_karma_get` | Web4 `KarmaManager` | Get avatar karma total |
| `oasis_holon_search` | Web4 `HolonManager` | Search holons by text |
| `oasis_quest_get` | Web5 `QuestManager` | Load quests for avatar |
| `oasis_memory_read` | `HolonicMemoryManager` | Read memories from a holonic holon |
| `oasis_memory_write` | `HolonicMemoryManager` | Write a memory to a holonic holon |
| `oasis_braid_graph_get` | `HolonicBraidManager` | Get the shared reasoning graph for a task type |
| `web_search` | Brave/Serper/SerpAPI | Live web search |
| `code_execute` | Sandboxed runner | Execute Python/JS snippet, return output |

---

### PRIORITY 9 — Additional AI Providers

**Most of these are OpenAI-compatible — each is a ~5-line addition to `GetOpenAICompatibleEndpoint()` and a new `AIProviderType` enum value.**

| Provider | Env var | Base URL | Default model | Notes |
|---|---|---|---|---|
| Cerebras | `CEREBRAS_API_KEY` | `https://api.cerebras.ai/v1/chat/completions` | `llama-3.3-70b` | ~3000 tok/s — fastest inference available |
| Together AI | `TOGETHER_API_KEY` | `https://api.together.xyz/v1/chat/completions` | `meta-llama/Llama-3.3-70B-Instruct-Turbo` | 100+ open models |
| Fireworks AI | `FIREWORKS_API_KEY` | `https://api.fireworks.ai/inference/v1/chat/completions` | `accounts/fireworks/models/llama-v3p3-70b-instruct` | Fast open model inference |
| Moonshot AI (Kimi) | `MOONSHOT_API_KEY` | `https://api.moonshot.cn/v1/chat/completions` | `moonshot-v1-128k` | 128k context, strong for long docs |
| Perplexity | `PERPLEXITY_API_KEY` | `https://api.perplexity.ai/chat/completions` | `sonar-pro` | Web-grounded answers with citations |
| Novita AI | `NOVITA_API_KEY` | `https://api.novita.ai/v3/openai/chat/completions` | `meta-llama/llama-3.3-70b-instruct` | Cheap open model inference |
| Vertex AI | `GOOGLE_VERTEX_API_KEY` | Project-scoped endpoint | `gemini-2.5-pro` | Google enterprise (different to Gemini direct) |
| LM Studio | `LM_STUDIO_BASE_URL` | `http://localhost:1234/v1/chat/completions` | model-dependent | Local — complements Ollama |

Also seed FAHRN `OpenServSeedAgents` entries for each new provider so the reasoning network can route to them automatically.

#### FAHRN seed score estimates for new providers

```csharp
new ReasoningAgentMetadata { AgentName = "cerebras-llama-70b", Model = "llama-3.3-70b", SpeedScore = 0.97, CostScore = 0.80,
    CategoryScores = new Dictionary<string, double> { ["real-time"] = 0.90, ["code"] = 0.72, ["writing"] = 0.70 } },
new ReasoningAgentMetadata { AgentName = "together-llama-70b", Model = "meta-llama/Llama-3.3-70B-Instruct-Turbo", SpeedScore = 0.85, CostScore = 0.78,
    CategoryScores = new Dictionary<string, double> { ["code"] = 0.72, ["writing"] = 0.70, ["reasoning"] = 0.68 } },
new ReasoningAgentMetadata { AgentName = "perplexity-sonar-pro", Model = "sonar-pro", SpeedScore = 0.70, CostScore = 0.55,
    CategoryScores = new Dictionary<string, double> { ["real-time"] = 0.95, ["writing"] = 0.75, ["reasoning"] = 0.72 } },
new ReasoningAgentMetadata { AgentName = "moonshot-kimi-128k", Model = "moonshot-v1-128k", SpeedScore = 0.55, CostScore = 0.50,
    CategoryScores = new Dictionary<string, double> { ["writing"] = 0.82, ["legal"] = 0.78, ["architecture"] = 0.75 } },
```

---

### PRIORITY 10 — FAHRN Debate and Voting Dispatch Modes

**Completes the reasoning network's capability portfolio and fully justifies the "meta-orchestration" claim.**

#### Add to `DispatchMode` enum

```csharp
public enum DispatchMode
{
    Serial,       // cost-optimised: try best agent, fallback on failure
    Parallel,     // accuracy-optimised: all agents run simultaneously, best wins
    Decomposed,   // complex problems: slice into sub-problems, each agent owns a slice
    Debate,       // NEW: adversarial — agent A proposes, agent B critiques, agent C judges
    Voting,       // NEW: democratic — N agents answer independently, majority/weighted vote wins
    Auto          // NEW: FAHRN selects the mode based on task type and agent count
}
```

#### Debate mode implementation

In `FAHRNManager.DispatchDebateAsync()`:

1. **Proposer** (rank 1 agent): `"Produce the best answer to: {problem}"`
2. **Critic** (rank 2 agent): `"Critique this answer and identify flaws: {proposer_answer}"`
3. **Judge** (rank 3 agent, or rank 1 again if only 2 agents): `"Given the original problem, this proposed answer, and this critique, produce the final best answer synthesising both perspectives."`
4. Final answer = judge output; all three plans recorded in `AgentPlans`

Ideal for: high-stakes decisions, complex reasoning, legal analysis, architecture review.

#### Voting mode implementation

In `FAHRNManager.DispatchVotingAsync()`:

1. All N eligible agents answer independently in parallel (`Task.WhenAll`)
2. **Majority vote**: group identical/near-identical answers; largest group wins
3. **Weighted vote**: weight each agent's answer by its composite score; sum weights per answer cluster; highest-weight cluster wins
4. `VotingStrategy` enum: `Majority` | `Weighted` | `Unanimous` (only return if all agree)
5. Add `DispatchRequest.VotingStrategy` and `DispatchRequest.MinVotingAgents` (default 3)

Ideal for: factual Q&A, code review verdicts, classification decisions.

#### Auto mode

When `DispatchMode.Auto` is set, FAHRN selects the mode based on:

| Condition | Selected mode |
|---|---|
| `taskType` = "real-time" OR only 1 agent available | Serial |
| `taskType` = "reasoning" OR "mathematics" AND ≥3 agents | Parallel |
| Problem length > 500 words OR `taskType` = "architecture" | Decomposed |
| `taskType` = "legal" OR "code" (review) AND ≥3 agents | Debate |
| `taskType` = "general" AND ≥3 agents | Voting |
| Otherwise | Serial |

---

### PRIORITY 11 — Automatic Task Classification

**Currently callers must supply `taskType` manually. FAHRN should classify it automatically.**

Add `FahrnTaskClassifier` (lightweight, calls cheapest/fastest available model):

```csharp
public async Task<string> ClassifyTaskTypeAsync(string problem)
{
    CompletionRequest request = new CompletionRequest
    {
        Provider = "auto",
        Routing = new RoutingOptions { Priority = "latency" },  // fastest model wins
        MaxTokens = 10,
        Messages = new List<ChatMessage>
        {
            new ChatMessage { Role = "system", Content =
                "Classify the following problem into exactly one category. " +
                "Reply with only the category word and nothing else. " +
                "Categories: code, reasoning, writing, mathematics, legal, architecture, real-time, general" },
            new ChatMessage { Role = "user", Content = problem }
        }
    };

    OASISResult<CompletionResponse> result = await _providerManager.CompleteAsync(request);
    string raw = result.Result?.Content?.Trim().ToLowerInvariant() ?? "general";

    return _validCategories.Contains(raw) ? raw : "general";
}
```

Wire into `FahrnSolveManager` (Priority 2) and optionally into `FAHRNManager.DispatchAsync()` when `request.TaskType = "auto"`.

---

### PRIORITY 12 — Per-Avatar API Key Management, Cost Metering & Usage Quotas

**Required for production multi-tenant use and monetisation.**

#### Key management

New `KeyVaultManager`:
- Stores provider API keys encrypted as holon metadata in COSMIC ORM (never in env vars alone)
- `SaveProviderKeyAsync(avatarId, provider, encryptedKey)`
- `LoadProviderKeyAsync(avatarId, provider)` → decrypt on retrieval
- `AIProviderManager.LoadApiKeysFromEnvironment()` gains a fallback: if env var not set → try KeyVaultManager for this avatar's stored key

New controller: `POST /v1/keys`, `GET /v1/keys`, `DELETE /v1/keys/{provider}` (per-avatar)

#### Cost metering

Add cost-per-token tables to `AIProviderManager` (update these periodically):

```csharp
private static readonly Dictionary<(AIProviderType provider, string model), (double inputPer1k, double outputPer1k)> _pricing = new()
{
    [(AIProviderType.OpenAI, "gpt-4o")]              = (0.0025, 0.010),
    [(AIProviderType.Anthropic, "claude-opus-4.6")]  = (0.015,  0.075),
    [(AIProviderType.Gemini, "gemini-2.5-pro")]      = (0.00125,0.010),
    [(AIProviderType.Groq, "llama-3.3-70b-versatile")]= (0.00059,0.00079),
    // ...
};
```

Add to `CompletionResponse`:
```csharp
public double EstimatedCostUSD { get; set; }
```

Accumulate per-avatar spend in a holon (`AvatarSpendHolon`). Expose via `GET /v1/usage`.

#### Quotas

Add to `OASIS_DNA.json → OASIS.Web6`:
```json
"DefaultMonthlyBudgetUSD": 10.0,
"DefaultDailyTokenLimit": 500000
```

In `CompletionController`, pre-flight: load avatar's current-month spend; if over budget → `429 Too Many Requests` with `Retry-After` header.

---

### PRIORITY 13 — Semantic Caching

**Reduces costs and latency for repeated/similar prompts.**

Add `SemanticCacheManager`:

1. On each completion request: embed the last user message (`EmbeddingManager.EmbedAsync()`)
2. Cosine-compare against cached embeddings (stored as holon metadata with TTL)
3. If similarity > threshold (configurable, default 0.95): return cached `CompletionResponse` immediately
4. On cache miss: proceed normally, store embedding + response with `CacheTtlSeconds` TTL

Add to `CompletionRequest`:
```csharp
public int? CacheTtlSeconds { get; set; }   // null = use DNA default; 0 = disable cache
public double? CacheSimilarityThreshold { get; set; }  // null = use DNA default (0.95)
```

DNA config: `OASIS.Web6.Cache.DefaultTtlSeconds` (default 3600), `OASIS.Web6.Cache.SimilarityThreshold` (default 0.95).

---

### PRIORITY 14 — Decentralised AI Providers

**Completes the vision of routing across centralised, decentralised, and local inference.**

#### Bittensor

- `AIProviderType.Bittensor`
- Env var: `BITTENSOR_API_KEY`, `BITTENSOR_SUBNET_UID`
- Route via Corcel API (Bittensor gateway) or direct subnet endpoint
- Best for: privacy-sensitive requests, censorship resistance, community-owned inference
- Add `ReasoningAgentMetadata` attribute: `IsDecentralised = true` — useful for privacy-conscious FAHRN dispatch

#### Akash / self-hosted OpenAI-compatible

- Any model deployed on Akash exposes an OpenAI-compatible endpoint
- Register as a custom provider: `AIProviderType.Custom` with configurable `BaseUrl`
- Add `OASIS_DNA.json → OASIS.Web6.CustomProviders[]` array for operator-configured endpoints

#### GaiaNet

- `AIProviderType.GaiaNet`
- OpenAI-compatible endpoint; community-run decentralised model nodes
- Env var: `GAIANET_NODE_URL`

#### Replicate

- `AIProviderType.Replicate`
- Env var: `REPLICATE_API_KEY`
- REST: `POST https://api.replicate.com/v1/models/{owner}/{model}/predictions`
- Hundreds of community open-source models (image, text, audio, video); not OpenAI-compatible — requires a dedicated adapter
- Best for: niche open-source models not available elsewhere

#### Gensyn / Ritual / Prime Intellect

- Register as `AIProviderType.Custom` endpoints once their inference APIs stabilise
- Monitor for OpenAI-compatible gateways — most decentralised compute networks are converging on this wire format

---

### PRIORITY 15 — External Memory Providers

**Makes Holonic BRAID interoperable with the broader memory ecosystem. Developers who already use Mem0, Zep, or LangMem can plug them in as additional memory layers alongside or beneath the fractal holon hierarchy.**

#### New `MemoryProviderManager` with abstract interface

```csharp
public interface IExternalMemoryProvider
{
    string Name { get; }
    Task<List<MemoryEntry>> SearchAsync(Guid avatarId, string query, int topK = 5);
    Task AddAsync(Guid avatarId, string content, Dictionary<string, string> metadata = null);
    Task DeleteAsync(Guid avatarId, string memoryId);
}
```

#### Adapters

| Provider | Notes |
|---|---|
| **Mem0** | REST API (`https://api.mem0.ai/v1/memories`). Auto-manages per-user/per-session/per-agent memories. `POST /memories`, `GET /memories/search`. Env var: `MEM0_API_KEY` |
| **Zep** | Graph-based long-term memory. REST API (`https://api.getzep.com`). Strong for dialog history + entity extraction. Env var: `ZEP_API_KEY` |
| **Letta (MemGPT)** | Stateful agent memory with self-editing context windows. REST API or in-process. Env var: `LETTA_BASE_URL`, `LETTA_API_KEY` |
| **LangMem** | LangGraph-native memory provider. Can be embedded in-process or called via LangGraph Cloud REST. Env var: `LANGMEM_API_KEY` |
| **Graphiti** | Temporal knowledge graph memory (from Zep team). `POST /episodes`, `GET /search`. Env var: `GRAPHITI_BASE_URL` |
| **Redis Vector Memory** | Fast in-process memory for high-throughput agents. Uses Redis Stack + RediSearch. `REDIS_URL` env var. |

#### Integration with FAHRN and completions

- `CompletionRequest.ExternalMemoryProviders[]` — list of provider names to query for context before the provider call
- `FAHRNManager.DispatchAsync()` — before dispatch, search each configured external memory provider and inject top-K results into the system message alongside Holonic BRAID context
- `MemoryProviderManager` registered in DI; providers activated via `OASIS_DNA.json → OASIS.Web6.Memory.Providers[]`

#### New endpoints

```
GET  /v1/memory/external/providers         List configured external memory providers  [NEW]
POST /v1/memory/external/search            Search across all configured providers  [NEW]
POST /v1/memory/external/add               Add a memory to the configured provider  [NEW]
DELETE /v1/memory/external/{provider}/{id} Delete a specific memory  [NEW]
```

#### MCP tools

```csharp
[McpServerTool(Name = "web6_memory_external_search")]
[Description("WEB6: searches configured external memory providers (Mem0, Zep, Letta, LangMem, etc.) for memories relevant to a query, returning top-K results ranked by relevance.")]

[McpServerTool(Name = "web6_memory_external_add")]
[Description("WEB6: adds a memory to the configured external memory provider for the given avatar.")]
```

---

### PRIORITY 16 — Holonic Memory Improvements

**Three concrete improvements to the existing `HolonicMemoryManager` that close gaps identified during planning.**

#### 16a. Multi-hop propagation: `PropagateUpAsync(childHolonId, levels)`

Currently `PropagateAsync` propagates exactly one hop (child → parent). Add:

```csharp
/// <summary>Propagates permitted memory up the fractal hierarchy for the given number of hops (levels). Pass int.MaxValue to propagate all the way to Earth.</summary>
public async Task<OASISResult<int>> PropagateUpAsync(Guid childHolonId, int levels = 1)
{
    int totalPropagated = 0;
    Guid currentId = childHolonId;

    for (int i = 0; i < levels; i++)
    {
        OASISResult<int> step = await PropagateAsync(currentId);
        if (step.IsError || step.Result == 0) break;
        totalPropagated += step.Result;
        // load parent id to continue upward
        OASISResult<IHolon> holon = await Data.LoadHolonAsync(currentId, false);
        if (holon.IsError || holon.Result?.ParentHolonId == Guid.Empty) break;
        currentId = holon.Result.ParentHolonId;
    }

    return new OASISResult<int> { Result = totalPropagated };
}
```

New endpoint: `POST /v1/holonic-memory/holons/{childHolonId}/propagate-up?levels={n}`
New MCP tool: `web6_memory_propagate_up`

#### 16b. Semantic memory search: `QueryMemoryAsync(holonId, semanticQuery, topK)`

Once embeddings exist (Priority 7), add semantic search within a holon's memory items:

```csharp
public async Task<OASISResult<List<HolonicMemoryItem>>> QueryMemoryAsync(Guid holonId, string semanticQuery, int topK = 5)
{
    // Load all memory items on the holon
    // Embed the query via EmbeddingManager
    // Embed each memory item's Value field (or use cached embeddings)
    // Cosine-rank, return top-K
}
```

New endpoint: `GET /v1/holonic-memory/holons/{holonId}/memory/search?q={query}&topK={n}`
New MCP tool: `web6_memory_search`

#### 16c. TTL enforcement for `RetentionPolicy`

The `RetentionPolicy` enum already exists in `WEB6/NextGenSoftware.OASIS.Web6.Core/Enums/RetentionPolicy.cs` but is not enforced. Wire it:

- `RetentionPolicy.Ephemeral` → memory item deleted when its session holon ends (wire into `SymbiosisSessionManager.EndSessionAsync` to also call `HolonicMemoryManager.DeleteEphemeralMemoriesAsync`)
- `RetentionPolicy.Session` → TTL = session lifetime
- `RetentionPolicy.Persistent` → no TTL (current default behaviour)
- `RetentionPolicy.TimeLimited` → `HolonicMemoryItem.ExpiresUtc` field; background job or on-read check deletes expired items

Add `HolonicMemoryItem.ExpiresUtc` and `HolonicMemoryItem.RetentionPolicy` fields. Add `HolonicMemoryManager.PurgeExpiredAsync()` — called on startup and periodically.

---

### PRIORITY 17 — Enhanced Loop Detection in FAHRN

**The current loop detection only catches degenerate line-repetition. Four additional mechanisms make it robust.**

Current detection (in `FAHRNManager.DetectLoop()`): flags when the last 6 lines of a Mermaid diagram are all identical. This is too narrow — it misses semantic loops, budget overruns, circular DAGs, and contradictory outputs.

#### 17a. Output hashing (rolling window)

```csharp
// Maintain a rolling hash set of the last N outputs across the serial dispatch chain:
private readonly HashSet<string> _outputHashes = new HashSet<string>();

bool IsOutputDuplicate(string content)
{
    // SHA256 hash of normalised content (trim whitespace, lowercase)
    string hash = ComputeHash(content.Trim().ToLowerInvariant());
    return !_outputHashes.Add(hash);  // Add returns false if already present
}
```

If `IsOutputDuplicate` returns true, set `plan.LoopDetected = true` immediately — the agent is cycling.

#### 17b. Token budget monitoring

Add to `DispatchRequest`:
```csharp
public int? MaxTotalTokens { get; set; }  // hard stop for the entire dispatch
```

In `FAHRNManager.DispatchAsync()`:
```csharp
int totalTokensUsed = 0;
// After each ExecuteAgentAsync:
totalTokensUsed += plan.TokensUsed;
if (request.MaxTotalTokens.HasValue && totalTokensUsed >= request.MaxTotalTokens.Value)
{
    dispatchResult.BudgetExceeded = true;
    break;  // stop dispatch even if more agents remain
}
```

#### 17c. DAG cycle detection in Mermaid

Parse `A --> B` edges from the Mermaid diagram and run a DFS cycle check:

```csharp
bool HasCyclicDependency(string mermaidDiagram)
{
    // Extract edges: regex @"(\w+)\s*--[>-]+\s*(\w+)"
    // Build adjacency list
    // DFS with visited + recursion-stack sets
    // Return true if any back-edge found
}
```

A Mermaid `graph TD` with a back-edge (`A --> B --> A`) indicates the agent produced a circular plan — flag as `LoopDetected = true`.

#### 17d. Contradiction detection (lightweight NLI)

For high-stakes dispatch modes (Debate, Voting), run a lightweight NLI (Natural Language Inference) check between adjacent agent outputs:

```csharp
// Call a small fast model:
// System: "Does statement B contradict statement A? Reply with only: ENTAIL, NEUTRAL, or CONTRADICT."
// If CONTRADICT → flag the second agent's plan as potentially looped / contradictory
```

This is optional (off by default, enabled via `DispatchRequest.EnableContradictionDetection = true`) because it adds a model call per agent pair. Best for Debate mode where catching contradictions is the point.

---

### PRIORITY 18 — WebSocket Bidirectional Agent Sessions

**SSE (Priority 5) covers server→client streaming. WebSocket adds client→server interactivity: mid-session tool results, user interruptions, multi-turn agent loops with persistent server-side state.**

#### New endpoint: `GET /v1/ws/session` (WebSocket upgrade)

Once connected, the session carries:
- Avatar context (loaded once from Web4/Web5 on connect, cached for the session)
- Conversation history (maintained server-side in Holonic Memory at Session level)
- Active tool calls (pending results tracked server-side)
- FAHRN dispatch state (which agents are mid-execution)

#### Message protocol (JSON over WebSocket)

Client → Server:
```json
{ "type": "message",      "content": "...", "role": "user" }
{ "type": "tool_result",  "toolCallId": "call_abc", "result": "..." }
{ "type": "interrupt" }   // cancel current FAHRN dispatch
{ "type": "ping" }
```

Server → Client:
```json
{ "type": "chunk",        "delta": "...", "provider": "Anthropic", "model": "..." }
{ "type": "tool_call",    "id": "call_abc", "name": "oasis_karma_get", "arguments": "{...}" }
{ "type": "done",         "totalTokens": 1842, "latencyMs": 1203 }
{ "type": "error",        "message": "..." }
{ "type": "pong" }
```

#### Implementation

- `WebSocketSessionManager` — manages active sessions keyed by `sessionId`
- Sessions stored as Holonic Memory Session holons (reuses existing `HolonicMemoryManager`)
- `CompletionController` gets a new `[HttpGet("ws/session")]` action with `HttpContext.WebSockets.AcceptWebSocketAsync()`
- Tool call loop: server sends `tool_call`, waits for `tool_result`, then continues completion

---

### PRIORITY 19 — Observability, Telemetry & SDK Generation

**Three distinct improvements to developer experience and operational visibility.**

#### 19a. `/v1/telemetry` — real-time per-request trace stream

```
GET /v1/telemetry/stream   (SSE)
```

Each AI request emits a structured trace event:
```json
{
  "requestId": "uuid",
  "timestamp": "2026-07-10T12:00:00Z",
  "provider": "Anthropic",
  "model": "claude-opus-4.6",
  "latencyMs": 843,
  "promptTokens": 512,
  "completionTokens": 1024,
  "estimatedCostUSD": 0.0089,
  "fahrnMode": "Parallel",
  "braidGraphReused": true,
  "braidGraphId": "uuid",
  "agentsScored": 5,
  "winningAgent": "openserv-claude-opus-4.6",
  "avatarContextInjected": true,
  "cacheHit": false,
  "loopDetected": false
}
```

Clients subscribe to this SSE stream to power real-time dashboards. Store last N events per avatar in Holonic Memory for historical replay via `GET /v1/telemetry/history`.

#### 19b. `/v1/providers/status` — live provider health

```
GET /v1/providers/status
```

Response:
```json
[
  { "provider": "OpenAI",    "healthy": true,  "latencyMs": 312, "lastCheckedUtc": "..." },
  { "provider": "Anthropic", "healthy": true,  "latencyMs": 498, "lastCheckedUtc": "..." },
  { "provider": "Groq",      "healthy": false, "latencyMs": null, "error": "timeout", "lastCheckedUtc": "..." }
]
```

Background `ProviderHealthMonitor` pings each provider every 60 seconds with a minimal completion request (e.g. `"Reply with OK"`). Results cached and exposed here. FAHRN's Web9 health gate (Priority 3b) consumes this directly.

#### 19c. OpenAPI spec improvements and SDK generation

- Add `[SwaggerOperation(Summary = "...", Description = "...")]` and request/response examples to every controller action
- Publish the spec at `GET /openapi.json` (alongside Swagger UI)
- Add a CI step to auto-generate SDKs from the published spec:
  - **npm**: `@oasisomniverse/web6-api` — TypeScript/JavaScript client, types inferred from the spec
  - **Python**: `oasis-web6` — async client via `openapi-python-client` or `fern`
  - **C#**: `NextGenSoftware.OASIS.Web6.Client` — already trivially available since the models live in `Web6.Core`, but a standalone NuGet for external callers is worthwhile
- Target: developers can `npm install @oasisomniverse/web6-api` and call `web6.fahrn.solve(...)` without ever reading the Swagger docs

---

### PRIORITY 20 — DID / Verifiable Credentials & Identity Layer

**Completes the identity story. OASIS avatars are already on-chain identities — this extends them to the emerging W3C decentralised identity standard, enabling AI to act verifiably on behalf of users.**

#### 20a. DID resolution and avatar mapping

Add `DidManager`:
- `ResolveDidAsync(did)` — resolve a W3C DID (e.g. `did:key:...`, `did:web:...`, `did:ethr:...`) to an OASIS avatar via the DID Document's `service` endpoint or via a registered mapping holon
- `RegisterAvatarDidAsync(avatarId, did)` — stores the DID↔avatar mapping as a holon
- `GET /v1/identity/did/{did}` — public resolution endpoint

#### 20b. DID-based authentication

New auth flow alongside existing JWT:
- `POST /v1/auth/did` — client presents a DID + a signed challenge (DID Auth); server verifies the signature against the DID Document's `verificationMethod`, issues an OASIS JWT scoped to the resolved avatar
- Enables walletless auth: users authenticate with their existing DID (e.g. their ENS name, their Ceramic DID, their `did:key`) without creating a separate OASIS account

#### 20c. Verifiable Credentials (VC) for capability grants

```csharp
// A VC issued by avatar X grants AI agent Y permission to perform specific actions:
{
  "@context": ["https://www.w3.org/2018/credentials/v1"],
  "type": ["VerifiableCredential", "OASISCapabilityGrant"],
  "issuer": "did:ethr:0xAvatarX",
  "credentialSubject": {
    "id": "did:web:api.web6.oasisomniverse.one/agents/fahrn",
    "capabilities": ["web4:karma:read", "web5:quests:complete", "web6:complete"]
  }
}
```

- `POST /v1/auth/vc` — present a VC; server verifies issuer signature, extracts granted capabilities, issues a scoped JWT limited to those capabilities
- `CompletionRequest.ActingAsAvatarId` — AI agent acts on behalf of this avatar; request must carry a VC proving the grant
- Enables: "Avatar X authorises Claude to complete quests on my behalf" — verifiable, revocable, on-chain provenance

#### 20d. `OASIS_DNA.json` config

```json
"OASIS": {
  "Web6": {
    "Identity": {
      "EnableDIDAuth": true,
      "EnableVerifiableCredentials": true,
      "TrustedDIDMethods": ["did:key", "did:web", "did:ethr", "did:ion"],
      "VCSignatureAlgorithm": "Ed25519Signature2020"
    }
  }
}
```

---

### PRIORITY 21 — ACP, ANP & Additional Protocol Adapters

**Completes the protocol neutrality story. OASIS should support every major emerging agent protocol, not just MCP and A2A.**

#### 21a. ACP (Agent Communication Protocol — BeeAI/IBM)

Add `OrchestratorProtocolType.ACP`.

Wire `OrchestratorManager.InvokeAcpAsync()`:
- `POST {endpoint}/agents/{agentId}/runs` — create a run (ACP spec)
- `GET {endpoint}/agents/{agentId}/runs/{runId}` — poll status
- `GET {endpoint}/agents/{agentId}/runs/{runId}/events` — SSE stream of run events
- Parse ACP's `RunStatus` → normalise to `OrchestratorInvokeResponse`

ACP is designed for autonomous agents that run asynchronously — the invoke call creates a run and returns immediately; results come via SSE or polling. This is structurally different from MCP (synchronous tools/call) and A2A (task lifecycle).

#### 21b. ANP (Agent Network Protocol)

Add `OrchestratorProtocolType.ANP`.

ANP focuses on decentralised agent discovery rather than pairwise invocation:
- `GET {did_document_url}` — resolve the agent's DID Document to find its ANP service endpoint
- `POST {anp_endpoint}/messages` — send a message to the agent
- Agents publish their capabilities via DID Documents rather than a central registry

Wire `OrchestratorManager.InvokeAnpAsync()`:
- Resolve `config.EndpointUrl` as a DID → extract `service` entries with `type: "ANPAgent"`
- `POST` to the resolved service endpoint with the ANP message envelope
- ANP is closely related to Priority 20 (DID) — enable them together

#### 21c. gRPC adapter

Add `OrchestratorProtocolType.GRPC`.

```csharp
case OrchestratorProtocolType.GRPC:
    // config.ExtraConfig["protoDescriptor"] = base64-encoded .proto file descriptor
    // config.ExtraConfig["service"] = "mypackage.MyService"
    // config.ExtraConfig["method"] = "MyMethod"
    // Use Grpc.Net.Client + Google.Protobuf.WellKnownTypes.Struct for dynamic invocation
    return await InvokeGrpcAsync(config, request);
```

Best for: high-performance agent-to-agent calls, internal microservice agents, latency-sensitive routing.

#### 21d. AsyncAPI / event-driven adapters (Kafka, AMQP, MQTT)

Add `OrchestratorProtocolType.Kafka`, `.AMQP`, `.MQTT`.

These are fire-and-forget or pub/sub — the invocation model differs:
- `InvokeAsync` publishes a message to the topic/queue and returns immediately
- Optional: subscribe to a reply topic (`config.ExtraConfig["replyTopic"]`) and await the response with a timeout

Best for: event-driven agent pipelines, IoT agents (MQTT), high-throughput task queues (Kafka/AMQP).

#### 21e. GraphQL adapter

Add `OrchestratorProtocolType.GraphQL`.

```csharp
case OrchestratorProtocolType.GraphQL:
    // config.ExtraConfig["query"] = "mutation InvokeAgent($input: String!) { invoke(input: $input) { result } }"
    // POST {endpoint} with { "query": ..., "variables": { "input": request.Input, ...request.Parameters } }
    return await InvokeGraphQLAsync(config, request);
```

---

### PRIORITY 24 — SkillOpt: Self-Evolving Agent Skill Documents

**Source:** Microsoft Research, arXiv:2605.23904 — [https://microsoft.github.io/SkillOpt/](https://microsoft.github.io/SkillOpt/)

**What it is:** SkillOpt treats the agent's natural-language *skill document* (a markdown procedure file) as the optimisation target rather than model weights. A frozen target model executes tasks guided by the current skill; a separate optimiser model proposes bounded textual edits; a held-out validation gate accepts a candidate only when performance improves. The exported artifact is a portable `best_skill.md`.

**Why it matters for FAHRN:** FAHRN already updates agent scores via EMA. SkillOpt adds a second, complementary learning axis — the *procedure itself* evolves, not just the score. An agent that scores well numerically but uses a suboptimal procedure for a task category now gets its procedure rewritten automatically. Demonstrated gains: **+23.5% avg** on GPT-5.5 across SearchQA, SpreadsheetBench, OfficeQA, DocVQA, LiveMath, ALFWorld.

#### New `SkillOptManager`

```csharp
public class SkillOptManager
{
    // Runs one SkillOpt epoch for a given agent + task category:
    // 1. Collect scored rollout trajectories from recent FAHRN dispatches
    // 2. Split into success and failure minibatches
    // 3. Call optimiser model: "Given these failures, propose bounded add/delete/replace edits to the skill"
    // 4. Apply edits under budget (textual learning rate = max N lines changed)
    // 5. Evaluate candidate skill on held-out task set via ExecuteWithSkillAsync()
    // 6. Accept candidate only if held-out selection score improves; otherwise store in rejected-edit buffer
    public async Task<OASISResult<SkillDocument>> RunEpochAsync(Guid agentHolonId, string taskCategory);

    // Execute a single FAHRN dispatch using the agent's current best skill injected into the system context
    public async Task<OASISResult<DispatchResult>> ExecuteWithSkillAsync(DispatchRequest request, SkillDocument skill);

    // Load/save the current best skill for this agent + category as a holon child of the agent holon
    public async Task<OASISResult<SkillDocument>> LoadSkillAsync(Guid agentHolonId, string taskCategory);
    public async Task<OASISResult<SkillDocument>> SaveSkillAsync(Guid agentHolonId, string taskCategory, SkillDocument skill);
}
```

#### `SkillDocument` holon

```csharp
public class SkillDocument : HolonBase
{
    public string TaskCategory { get; set; }        // "mathematics", "code", "legal", etc.
    public string Content { get; set; }             // Markdown — the actual skill text
    public double SelectionScore { get; set; }      // Best held-out score achieved
    public int EpochsRun { get; set; }
    public List<RejectedEdit> RejectedEdits { get; set; }  // Negative feedback buffer
    public string SlowUpdateContent { get; set; }   // Checkpoint for slow update mechanism
}
```

#### Integration with FAHRN

- `FAHRNManager.DispatchAsync()` — before dispatching, call `SkillOptManager.LoadSkillAsync(agentId, taskType)`. If a skill document exists, inject its content into the system message: `[AGENT SKILL FOR {taskCategory}]\n{skill.Content}`
- After dispatch completes: store the outcome (score, trajectory) as a rollout record on the agent holon
- Background job (or on-demand via API): `SkillOptManager.RunEpochAsync()` processes accumulated rollouts and attempts a skill evolution step
- `ReasoningAgent.SkillOptEnabled` flag — opt-in per agent (default true for agents with ≥20 rollout records in a category)

#### New endpoints

```
GET  /v1/reasoning-network/agents/{id}/skills              List skill documents per task category  [NEW]
GET  /v1/reasoning-network/agents/{id}/skills/{category}   Get current best skill for a category  [NEW]
POST /v1/reasoning-network/agents/{id}/skills/{category}/evolve   Trigger one SkillOpt epoch  [NEW]
```

#### New MCP tool

```csharp
[McpServerTool(Name = "web6_fahrn_get_agent_skill")]
[Description("WEB6: returns the current best_skill.md document for a FAHRN agent and task category — the evolved natural-language procedure that guides the agent's reasoning, produced by the SkillOpt self-improvement loop.")]
```

#### Skill transferability

Per the SkillOpt paper, the exported skill document transfers across:
- **Cross-model**: skill trained with GPT-5.4 transfers to GPT-5.4-nano (+15.2 pts)  
- **Cross-harness**: skill trained in Codex transfers to Claude Code (+31.8 pts)
- **Self-optimiser**: small model optimising its own skills still finds useful edits

This means a skill evolved by one FAHRN agent can seed the starting skill for a newly added agent in the same task category — dramatically accelerating onboarding of new providers into the reasoning network.

---

### PRIORITY 25 — ML.NET In-Process Machine Learning

**Source:** [https://dotnet.microsoft.com/en-us/apps/ai/ml-dotnet](https://dotnet.microsoft.com/en-us/apps/ai/ml-dotnet)

**What it is:** ML.NET is Microsoft's open-source, cross-platform ML framework for .NET — used in Power BI, Microsoft Defender, Outlook and Bing. Runs entirely in-process (C#/F#), supports AutoML, TensorFlow, ONNX, and achieves 95% accuracy on 9GB datasets.

**Why it matters for WEB6:** The entire OASIS stack is .NET. ML.NET enables WEB6 to run ML inference *inside the API process* — zero additional API calls, microsecond latency, no external dependency — for tasks that don't need a large LLM: classification, anomaly detection, sentiment analysis, recommendation.

#### Use cases within WEB6

| ML Task | Where Used | Model Type |
|---|---|---|
| **Task type classification** | `FahrnTaskClassifier` — classify incoming problem as mathematics/code/legal/etc. | Multi-class classification (AutoML) |
| **Loop anomaly scoring** | `FAHRNManager.DetectLoop()` — supplement hash/DAG checks with an ML anomaly score on token velocity and graph growth rate | Anomaly detection |
| **Sentiment analysis** | Avatar content moderation, OAPP review scoring, holonic memory tagging | Binary/multi-class classification |
| **Agent routing recommendation** | Recommend agent subset for a new task based on task embedding similarity to historical dispatches | Recommendation / collaborative filtering |
| **Cost anomaly detection** | Flag unusual spend spikes per avatar before they hit budget cap | Time-series anomaly detection |
| **Semantic similarity scoring** | Support `SemanticCacheManager` with a local cosine-similarity model as a fast pre-filter before embedding API call | Regression |

#### New `MLNetManager`

```csharp
public class MLNetManager
{
    private readonly MLContext _ctx = new MLContext();

    // Train or retrain a named model from OASIS usage holons
    public async Task<OASISResult<bool>> TrainAsync(string modelName, IEnumerable<IHolon> trainingData);

    // Load a serialised model from holon storage (COSMIC ORM)
    public async Task<OASISResult<ITransformer>> LoadModelAsync(string modelName);

    // Classify task type without an LLM call
    public string ClassifyTaskType(string problemText);

    // Score loop anomaly: returns 0.0 (normal) to 1.0 (certain loop)
    public double ScoreLoopAnomaly(LoopFeatureVector features);

    // Sentiment: returns Positive / Neutral / Negative
    public string AnalyseSentiment(string text);
}
```

#### AutoML integration

- `POST /v1/ml/train` — trigger AutoML training on a named scenario using holons as training data
- `GET /v1/ml/models` — list deployed in-process models and their accuracy metrics
- Models serialised as `.zip` and stored as holons via COSMIC ORM — survive restarts, replicate across deployments

#### `FahrnTaskClassifier` upgrade

Replace the current regex/keyword heuristic in `FahrnTaskClassifier.cs` with an ML.NET multi-class classification model trained on historical FAHRN dispatch outcomes:

```csharp
// Before (heuristic):
if (problem.Contains("code") || problem.Contains("function")) return "code";

// After (ML.NET):
string taskType = _mlNetManager.ClassifyTaskType(problem);
// Returns: "mathematics" | "code" | "legal" | "architecture" | "writing" | "real-time" | "general"
// Trained on accumulated FAHRN dispatch history stored as holons
```

#### NuGet packages required

```xml
<PackageReference Include="Microsoft.ML" Version="4.*" />
<PackageReference Include="Microsoft.ML.AutoML" Version="0.22.*" />
<PackageReference Include="Microsoft.ML.TensorFlow" Version="4.*" />  <!-- optional: ONNX/TF models -->
```

#### DNA config

```json
"OASIS": {
  "Web6": {
    "MLNet": {
      "Enabled": true,
      "AutoRetrainOnHolonThreshold": 1000,
      "Models": {
        "TaskClassifier": { "Enabled": true, "MinAccuracy": 0.85 },
        "LoopAnomaly":    { "Enabled": true },
        "Sentiment":      { "Enabled": true }
      }
    }
  }
}
```

---

### PRIORITY 22 — Self-Registration as Orchestrator at Startup

**A small but elegant addition: the Web6 API registers itself in its own FAHRN orchestrator registry on boot, so FAHRN agents can call back into any OASIS tool as a first-class orchestrator without any manual setup.**

In `Program.cs`, after OASIS boot and FAHRN auto-seed:

```csharp
// Auto-register this Web6 instance as an MCP orchestrator adapter in its own registry.
// Enables FAHRN agents to invoke any Web4-Web10 tool via web6_orchestrator_invoke
// without leaving the FAHRN dispatch pipeline.
if (dna?.OASIS?.Web6?.SelfRegisterAsOrchestrator ?? true)
{
    try
    {
        string selfUrl = Environment.GetEnvironmentVariable("WEB6_PUBLIC_URL")
            ?? "https://api.web6.oasisomniverse.one/mcp";

        OrchestratorManager orchManager = new OrchestratorManager(Guid.Empty, dna);
        OASISResult<List<OrchestratorAdapterConfig>> existing = await orchManager.GetAdaptersAsync();

        bool alreadyRegistered = existing.Result?.Any(a =>
            string.Equals(a.EndpointUrl, selfUrl, StringComparison.OrdinalIgnoreCase)) ?? false;

        if (!alreadyRegistered)
        {
            await orchManager.RegisterAdapterAsync(new OrchestratorAdapterConfig
            {
                Name        = "OASIS-WEB6-SELF",
                Protocol    = OrchestratorProtocolType.MCP,
                EndpointUrl = selfUrl,
                ExtraConfig = new Dictionary<string, string> { ["tool"] = "web6_fahrn_solve" }
            });
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError($"Warning: WEB6 self-registration as orchestrator failed: {ex.Message}", ex);
    }
}
```

DNA config: `OASIS.Web6.SelfRegisterAsOrchestrator` (bool, default true). Controlled so operators can disable it in environments where the public URL isn't resolvable.

---

### PRIORITY 23 — Karma-Gated AI Access & StarnetContextManager

**Two related WEB4/WEB5 integration items that make OASIS's identity layer a first-class routing signal.**

#### 23a. Karma-gated AI access

Avatar karma score influences what AI resources are available to them:

Add `KarmaGatingConfig` to `OASIS_DNA.json → OASIS.Web6`:
```json
"KarmaGating": {
  "Enabled": true,
  "Tiers": [
    { "MinKarma": 0,     "MaxKarma": 999,    "AllowedProviders": ["Groq", "DeepSeek", "Ollama"], "MonthlyBudgetUSD": 1.0  },
    { "MinKarma": 1000,  "MaxKarma": 9999,   "AllowedProviders": ["auto"],                        "MonthlyBudgetUSD": 5.0  },
    { "MinKarma": 10000, "MaxKarma": 999999, "AllowedProviders": ["auto"],                        "MonthlyBudgetUSD": 25.0 },
    { "MinKarma": 10000, "MaxKarma": null,   "AllowedProviders": ["auto"],                        "MonthlyBudgetUSD": 100.0, "PriorityRouting": true }
  ]
}
```

In `CompletionController.Complete()` pre-flight:
1. Load avatar karma via `KarmaManager` (Web4)
2. Look up karma tier
3. Filter `candidates` in `AIProviderManager.ResolveProviderCandidates()` to only allowed providers for this tier
4. Apply tier's monthly budget cap (feeds into Priority 12 quota system)
5. If `PriorityRouting = true` → set `request.Routing.Priority = "quality"` automatically

This creates a meaningful progression loop: earn karma in the OurWorld game → unlock better AI models → complete harder quests → earn more karma.

#### 23b. `StarnetContextManager` — standalone context endpoint

```
GET /v1/context/avatar/{avatarId}
```

Returns a rich, pre-assembled JSON block of the avatar's current OASIS state, callable by any external system (not just the completion pipeline):

```json
{
  "avatarId": "...",
  "displayName": "...",
  "karmaScore": 4821,
  "karmaLevel": "Adept",
  "activeQuests": [
    { "id": "...", "name": "The Holonic Path", "progress": "3/5 objectives complete" }
  ],
  "activeMissions": [...],
  "worldMemberships": ["OurWorld", "Thrive"],
  "recentHolons": [...],
  "walletSummary": { "totalBalanceUSD": 12.50, "providers": ["Solana", "Ethereum"] },
  "assembledAtUtc": "2026-07-10T12:00:00Z"
}
```

New `StarnetContextManager` assembles this by calling Web4 + Web5 managers in parallel. Used internally by `CompletionController` for avatar context injection (Priority 3a) but also exposed as a standalone endpoint so game clients, OAPPs, and third-party AI tools can consume the same context block.

MCP tool:
```csharp
[McpServerTool(Name = "web6_get_avatar_context")]
[Description("WEB6: assembles and returns a rich context block for an OASIS avatar — karma, active quests, missions, world memberships, wallet summary — assembled from Web4 and Web5 in parallel. Use this to ground AI prompts in the avatar's real OASIS state.")]
public static async Task<string> GetAvatarContext(string avatarId)
```

---

## 3. MCP Tool Inventory (Complete — Post Implementation)

After all priorities are implemented, the MCP server exposes:

### Web4 Tools (Avatar, Karma, Holon, NFT, Wallet)
- `web4_avatar_register`, `web4_avatar_authenticate`, `web4_avatar_load_by_id`, `web4_avatar_load_by_email`
- `web4_karma_get`, `web4_karma_add`, `web4_karma_deduct`, `web4_karma_get_history`, `web4_karma_transfer`, `web4_karma_get_stats`
- `web4_holon_load`, `web4_holon_save`, `web4_holon_delete`, `web4_holon_search`
- `web4_nft_load`, `web4_nft_load_all_for_avatar`
- `web4_wallet_get_total_balance`, `web4_wallet_load_provider_wallets`, `web4_wallet_create`
- `web4_request` (full Web4 REST API passthrough)

### Web5 Tools (Quests, Missions, OAPPs)
- `web5_quest_load_all_for_avatar`, `web5_quest_start`, `web5_quest_complete_objective`, `web5_quest_complete`
- `web5_mission_complete`, `web5_mission_get_leaderboard`, `web5_mission_get_rewards`, `web5_mission_get_stats`
- `web5_request` (full Web5 REST API passthrough)

### Web6 Tools (AI, FAHRN, BRAID, Memory, Orchestrators)
- `web6_complete` (15+ providers, auto routing, failover)
- `web6_complete_stream` _(new — streaming SSE)_
- `web6_generate_image`
- `web6_embed` _(new — embeddings)_
- `web6_fahrn_solve` _(new — hero endpoint, full pipeline)_
- `web6_fahrn_register_agent`, `web6_fahrn_get_agents`, `web6_fahrn_seed_openserv_agents`, `web6_fahrn_dispatch`
- `web6_braid_find_graph`, `web6_braid_save_graph`, `web6_braid_record_outcome`, `web6_braid_record_solver_outcome` _(new)_
- `web6_memory_get_earth_holon`, `web6_memory_get_or_create_holon`, `web6_memory_set_membrane_rule`, `web6_memory_record`, `web6_memory_propagate`
- `web6_memory_propagate_up` _(new — multi-hop upward propagation, Priority 16)_
- `web6_memory_search` _(new — semantic search within a holon, Priority 16)_
- `web6_memory_external_search` _(new — search Mem0/Zep/Letta/LangMem/Graphiti/Redis, Priority 15)_
- `web6_memory_external_add` _(new — add to external memory provider, Priority 15)_
- `web6_orchestrator_register`, `web6_orchestrator_list`, `web6_orchestrator_invoke`
- `web6_list_openserv_models`
- `web6_get_avatar_context` _(new — StarnetContextManager rich context block, Priority 23)_

### Web7 Tools (Symbiosis, Collective Consciousness)
- `web7_start_session`, `web7_submit_signals`, `web7_end_session`, `web7_get_session`
- `web7_create_space`, `web7_join_space`, `web7_get_aggregate_field`
- `web7_request` _(new — full Web7 REST API passthrough)_

### Web8 Tools (Galactic Mesh, Protocol Bridge)
- `web8_register_node`, `web8_get_nodes`, `web8_add_link`, `web8_heartbeat`, `web8_compute_route`, `web8_send_message`
- `web8_translate_inbound`, `web8_translate_outbound`
- `web8_request` _(new — full Web8 REST API passthrough)_

### Web9 Tools (Singularity)
- `web9_get_unified_status`
- `web9_request` _(new — full Web9 REST API passthrough)_

### Web10 Tools (The Source)
- `web10_get_source`
- `web10_request` _(new — full Web10 REST API passthrough)_

---

## 4. Web6 WebAPI Endpoint Inventory (Complete — Post Implementation)

```
POST   /v1/complete                          AI completion (15+ providers, FAHRN+BRAID optional)
POST   /v1/complete/stream                   Streaming SSE completion  [NEW]
POST   /v1/complete/tool-result              Feed tool call result back to continue agent loop  [NEW]
POST   /v1/fahrn/solve                       Hero endpoint: full pipeline in one call  [NEW]
POST   /v1/embed                             Text embeddings  [NEW]
GET    /v1/images/generate                   Image generation (StabilityAI, OpenAI)
POST   /v1/images/generate

GET    /v1/reasoning-network/agents          List FAHRN agents
POST   /v1/reasoning-network/agents          Register FAHRN agent
POST   /v1/reasoning-network/agents/seed-openserv
POST   /v1/reasoning-network/dispatch        FAHRN dispatch

GET    /v1/holonic-braid/graph/{taskType}    Get shared BRAID graph
POST   /v1/holonic-braid/graph/{taskType}    Save BRAID graph
POST   /v1/holonic-braid/graph/{id}/outcome  Record solver outcome  [NEW]

GET    /v1/holonic-memory/earth
POST   /v1/holonic-memory/holons
PUT    /v1/holonic-memory/holons/{id}/membrane-rule
POST   /v1/holonic-memory/holons/{id}/memory
POST   /v1/holonic-memory/holons/{id}/propagate

GET    /v1/orchestrators                     List orchestrator adapters
POST   /v1/orchestrators                     Register orchestrator adapter
POST   /v1/orchestrators/invoke              Invoke orchestrator adapter

GET    /v1/keys                              List avatar's stored provider keys  [NEW]
POST   /v1/keys                              Save encrypted provider key  [NEW]
DELETE /v1/keys/{provider}                   Delete provider key  [NEW]

GET    /v1/usage                             Per-avatar token/cost usage summary  [NEW]

GET    /v1/health                            Web6 API health
GET    /v1/providers/status                  Live health/latency per AI provider  [NEW]

GET    /mcp                                  MCP SSE stream  [NEW]
POST   /mcp                                  MCP JSON-RPC tool calls  [NEW]

GET    /a2a/tasks/{id}                       A2A task status  [NEW]
GET    /a2a/tasks/{id}/events                A2A task SSE stream  [NEW]
POST   /a2a/tasks/send                       Receive A2A task from peer agent  [NEW]
POST   /a2a/tasks/{id}/cancel                Cancel A2A task  [NEW]

GET    /.well-known/mcp.json                 MCP discovery document  [NEW]
GET    /.well-known/agent.json               A2A agent card  [NEW]

GET    /openserv/models                      OpenServ SERV catalog

GET    /v1/ws/session                        WebSocket bidirectional agent session  [NEW - Priority 18]

GET    /v1/telemetry/stream                  Real-time per-request SSE trace stream  [NEW - Priority 19]
GET    /v1/telemetry/history                 Historical telemetry replay  [NEW - Priority 19]

GET    /v1/identity/did/{did}                Resolve a W3C DID to OASIS avatar  [NEW - Priority 20]
POST   /v1/auth/did                          DID-based authentication  [NEW - Priority 20]
POST   /v1/auth/vc                           Verifiable Credential capability grant  [NEW - Priority 20]

GET    /v1/context/avatar/{avatarId}         StarnetContextManager rich avatar context block  [NEW - Priority 23]

GET    /v1/memory/external/providers         List configured external memory providers  [NEW - Priority 15]
POST   /v1/memory/external/search            Semantic search across all external providers  [NEW - Priority 15]
POST   /v1/memory/external/add               Add memory to configured external provider  [NEW - Priority 15]
DELETE /v1/memory/external/{provider}/{id}   Delete specific external memory  [NEW - Priority 15]

POST   /v1/holonic-memory/holons/{id}/propagate-up  Multi-hop upward propagation  [NEW - Priority 16]
GET    /v1/holonic-memory/holons/{id}/memory/search  Semantic search within holon  [NEW - Priority 16]
```

---

## 5. GPT Conversation — Key Takeaways & Alignment Notes

The GPT conversation confirms the vision is coherent and the positioning is correct. Key points that directly map to this action plan:

**"OASIS doesn't replace MCP or A2A — it unifies them"**
→ Already implemented in `OrchestratorManager`. Needs the A2A proper protocol fix (Priority 4) to be complete.

**"Web4 = data abstraction / Web6 = intelligence abstraction"**
→ The parallel is architecturally sound. The cross-layer integration (Priority 3) is what makes Web6 the intelligence layer that *knows about* the data layer, not just a dumb API gateway.

**"HyperDrive for data → FAHRN for intelligence"**
→ FAHRN is the equivalent routing engine for intelligence. The Debate/Voting modes (Priority 10) and Auto classification (Priority 11) are what make it feel like a genuine reasoning OS rather than a model router.

**"If you can demonstrate that with working examples, it's a much more compelling story"**
→ `web6_fahrn_solve` (Priority 2) is the working example. One call, natural language in, answer + full reasoning trace out, with BRAID graph reuse and EMA learning visible in the telemetry.

**"MCP for AI-to-tool, A2A for agent-to-agent, OpenAPI for Web2/3, OAuth2+DID for identity"**
→ MCP HTTP transport (Priority 1) + A2A proper protocol (Priority 4) + per-avatar key management (Priority 12) covers this exactly.

**"Decentralised AI: Bittensor, Akash, Gensyn, Ritual"**
→ Priority 14 — lower priority than integration, but genuinely differentiating long-term.

---

## 6. File Locations for All Changes

| Change | File(s) |
|---|---|
| HTTP MCP transport (Option A) | `C:/Source/MCP/NextGenSoftware.OASIS.MCP.Server/Program.cs` |
| HTTP MCP transport (Option B — preferred) | `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Program.cs` |
| `web6_fahrn_solve` MCP tool | `C:/Source/MCP/NextGenSoftware.OASIS.MCP.Server/Tools/Web6Tools.cs` |
| `web6_fahrn_solve` REST endpoint | New `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/FahrnSolveController.cs` |
| `FahrnSolveManager` | New `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/FahrnSolveManager.cs` |
| Avatar context injection | `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/CompletionController.cs` |
| Web9 health gating | `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/FAHRNManager.cs` |
| Web7 symbiosis hints | `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/FAHRNManager.cs` + `WEB6/NextGenSoftware.OASIS.Web6.Core/Models/DispatchRequest.cs` |
| Web8 mesh routing | `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/FAHRNManager.cs` |
| `web7/8/9/10_request` tools | `C:/Source/MCP/NextGenSoftware.OASIS.MCP.Server/Tools/Web4Web5Tools.cs` |
| A2A agent card + task endpoints | New `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/A2AController.cs` |
| A2A protocol fix | `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/OrchestratorManager.cs` |
| Streaming SSE | `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/CompletionController.cs` + `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/AIProviderManager.cs` |
| Embeddings | New `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/EmbeddingController.cs` + `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/EmbeddingManager.cs` |
| Tool/function calling | `WEB6/NextGenSoftware.OASIS.Web6.Core/Models/CompletionRequest.cs` + `CompletionResponse.cs` + `AIProviderManager.cs` |
| New providers | `WEB6/NextGenSoftware.OASIS.Web6.Core/Enums/AIProviderType.cs` + `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/AIProviderManager.cs` + `FAHRNManager.cs` (seed agents) |
| Debate + Voting modes | `WEB6/NextGenSoftware.OASIS.Web6.Core/Enums/DispatchMode.cs` + `FAHRNManager.cs` |
| Auto task classification | New `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/FahrnTaskClassifier.cs` |
| Key vault / cost metering | New `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/KeyVaultManager.cs` + `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/KeysController.cs` + `UsageController.cs` |
| Semantic caching | New `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/SemanticCacheManager.cs` |
| Decentralised AI providers | `WEB6/NextGenSoftware.OASIS.Web6.Core/Enums/AIProviderType.cs` + `AIProviderManager.cs` |
| Replicate provider | `WEB6/NextGenSoftware.OASIS.Web6.Core/Enums/AIProviderType.cs` + `AIProviderManager.cs` |
| Discovery documents | New `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/DiscoveryController.cs` |
| External memory providers (Mem0/Zep/Letta/LangMem/Graphiti/Redis) | New `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/MemoryProviderManager.cs` + `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/ExternalMemoryController.cs` + `C:/Source/MCP/NextGenSoftware.OASIS.MCP.Server/Tools/Web6Tools.cs` |
| Holonic Memory improvements (multi-hop propagate, semantic search, TTL) | `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/HolonicMemoryManager.cs` + `WEB6/NextGenSoftware.OASIS.Web6.Core/Models/HolonicMemoryItem.cs` + `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/HolonicMemoryController.cs` |
| Enhanced loop detection (hashing, budget, DAG, NLI) | `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/FAHRNManager.cs` + `WEB6/NextGenSoftware.OASIS.Web6.Core/Models/DispatchRequest.cs` |
| WebSocket bidirectional sessions | New `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/WebSocketSessionManager.cs` + `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/CompletionController.cs` |
| Telemetry SSE stream | New `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/TelemetryController.cs` + `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/TelemetryManager.cs` |
| Provider health monitor | New `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/ProviderHealthMonitor.cs` + `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/ProvidersController.cs` |
| OpenAPI SDK generation | CI pipeline (GitHub Actions / Azure DevOps) + `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/` (SwaggerOperation annotations) |
| DID / Verifiable Credentials | New `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/DidManager.cs` + `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/IdentityController.cs` + `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Middleware/JwtMiddleware.cs` |
| ACP adapter | `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/OrchestratorManager.cs` + `WEB6/NextGenSoftware.OASIS.Web6.Core/Enums/OrchestratorProtocolType.cs` |
| ANP adapter | `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/OrchestratorManager.cs` + `OrchestratorProtocolType.cs` (requires DidManager from Priority 20) |
| gRPC / AsyncAPI / GraphQL adapters | `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/OrchestratorManager.cs` + `OrchestratorProtocolType.cs` |
| Self-registration as orchestrator at startup | `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Program.cs` |
| Karma-gated AI access | `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/CompletionController.cs` + `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/AIProviderManager.cs` + `OASIS_DNA.json` |
| StarnetContextManager + standalone endpoint | New `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/StarnetContextManager.cs` + New `WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Controllers/ContextController.cs` + `C:/Source/MCP/NextGenSoftware.OASIS.MCP.Server/Tools/Web6Tools.cs` |
