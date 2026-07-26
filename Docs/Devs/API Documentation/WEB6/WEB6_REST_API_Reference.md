# WEB6 REST API Reference

**Base URL:** `https://api.web6.oasisomniverse.one`  
**Swagger UI:** `https://api.web6.oasisomniverse.one/swagger`  
**Version:** 2.0.0  
**Framework:** ASP.NET Core on .NET 10

> **Note — endpoints not shown in Swagger UI:**  
> Six endpoints are intentionally hidden from the Swagger API explorer because they use streaming transports (SSE / WebSocket) or are auto-discovered by MCP/A2A clients rather than called directly by users. They work exactly as documented below; they just don't appear in the interactive UI.  
>
> | Endpoint | Reason hidden |
> |---|---|
> | `POST /v1/complete/stream` | SSE — returns `text/event-stream`, not a JSON response body |
> | `GET /a2a/tasks/{id}/events` | SSE — real-time task state stream |
> | `GET /v1/telemetry/stream` | SSE — real-time telemetry event stream |
> | `GET /ws/session` | WebSocket upgrade — bidirectional agent session |
> | `GET /.well-known/mcp.json` | MCP discovery document — consumed by MCP clients automatically |
> | `GET /.well-known/agent.json` | A2A agent card — consumed by A2A peer agents automatically |
>
> All other endpoints are visible and testable at the Swagger UI link above.

---

## Authentication

All write endpoints require a JWT bearer token. Obtain a token via the WEB4 avatar authentication endpoint or the DID auth endpoint.

```
Authorization: Bearer <jwt-token>
```

Read endpoints (GET) are open by default; write endpoints return `401 Unauthorized` without a valid token.

---

## Common response envelope

All endpoints return an `OASISResult<T>` envelope:

```json
{
  "result": { ... },
  "isError": false,
  "message": "",
  "detailedMessage": "",
  "warningCount": 0,
  "warnings": []
}
```

On error, `isError` is `true` and `message` describes the problem. HTTP status is `400 Bad Request` for client errors, `500 Internal Server Error` for unhandled exceptions.

---

## AI Completion

### POST `/v1/complete`

Unified AI completion. Routes to whichever provider/model best fits based on `provider` and `routing` settings.

**Request body:**
```json
{
  "provider": "auto",
  "model": "auto",
  "messages": [
    { "role": "system", "content": "You are a helpful assistant." },
    { "role": "user",   "content": "Explain quantum entanglement." }
  ],
  "maxTokens": 1024,
  "temperature": 0.7,
  "tools": [],
  "routing": {
    "priority": "quality",
    "fallbackProviders": ["anthropic", "openai"]
  },
  "avatarId": "00000000-0000-0000-0000-000000000000",
  "externalMemoryProviders": ["mem0", "zep"]
}
```

**`provider` values:** `auto`, `openai`, `anthropic`, `gemini`, `groq`, `mistral`, `cohere`, `xai`, `deepseek`, `ollama`, `openserv`, `azureopenai`, `awsbedrock`, `huggingface`

**`routing.priority` values:** `quality`, `latency`, `cost`, `balanced`

**Response:**
```json
{
  "result": {
    "content": "Quantum entanglement is...",
    "provider": "anthropic",
    "model": "claude-sonnet-5",
    "promptTokens": 42,
    "completionTokens": 318,
    "totalTokens": 360,
    "costUsd": 0.0012,
    "toolCalls": []
  }
}
```

---

### POST `/v1/complete/stream`

Streaming SSE completion. Same request body as `/v1/complete`. Returns `text/event-stream`.

Each SSE event:
```
data: {"delta":"quantum ","provider":"openai","model":"gpt-4o"}
```

Final event:
```
data: {"done":true,"totalTokens":360,"costUsd":0.0012}
```

---

### POST `/v1/complete/tool-result`

Feed a tool call result back into an active agent loop.

**Request body:**
```json
{
  "sessionId": "sess-abc123",
  "toolCallId": "call_xyz",
  "result": "{ \"temperature\": 22.5 }"
}
```

Returns the next completion response from the model after processing the tool result.

---

### POST `/v1/embed`

Generate float embeddings for one or more texts.

**Request body:**
```json
{
  "texts": ["The quick brown fox", "Another text to embed"],
  "provider": "auto",
  "model": "auto"
}
```

**`provider` values:** `auto`, `openai` (text-embedding-3-small), `cohere` (embed-english-v3.0), `huggingface` (all-MiniLM-L6-v2)

**Response:**
```json
{
  "result": {
    "provider": "openai",
    "model": "text-embedding-3-small",
    "embeddings": [[0.012, -0.034, ...], [0.056, 0.078, ...]],
    "totalTokens": 14
  }
}
```

---

### POST `/v1/images/generate` · GET `/v1/images/generate`

Generate an image.

**Request body (POST):**
```json
{
  "prompt": "A fractal holonic tree glowing with bioluminescence",
  "provider": "openai",
  "model": "gpt-image-1",
  "size": "1024x1024",
  "quality": "high"
}
```

**`provider` values:** `openai` (gpt-image-1 / dall-e-3), `stabilityai`

**Response:**
```json
{
  "result": {
    "url": "https://...",
    "base64": null,
    "provider": "openai",
    "revisedPrompt": "..."
  }
}
```

---

## FAHRN Multi-Agent Orchestration

### POST `/v1/fahrn/solve`

Hero endpoint. Runs the full FAHRN pipeline in a single call: auto-classifies the task, looks up the BRAID reasoning graph, dispatches to the best agents, scores and returns the result with a full reasoning trace.

**Request body:**
```json
{
  "problem": "Design a microservices architecture for a real-time trading platform.",
  "avatarId": "00000000-0000-0000-0000-000000000000",
  "mode": "auto",
  "maxTotalTokens": 8000,
  "maxCostUsd": 0.50,
  "budgetExceededBehaviour": "best_so_far",
  "externalMemoryProviders": ["mem0"]
}
```

**`mode` values:** `auto`, `serial`, `parallel`, `debate`, `voting`, `decomposed`

**Response:**
```json
{
  "result": {
    "answer": "Use an event-driven architecture with...",
    "taskType": "architecture",
    "dispatchMode": "debate",
    "agentsUsed": ["claude-sonnet-5", "gpt-4o", "gemini-2.0-flash"],
    "tokensUsedTotal": 4820,
    "costUsdTotal": 0.18,
    "budgetExceeded": false,
    "braidGraphReused": true,
    "reasoningTrace": "..."
  }
}
```

---

### GET `/v1/fahrn/budget-estimate`

Dry-run estimate of token count and USD cost for a given dispatch — no agent calls are made.

**Query params:** `taskType`, `mode`, `agentCount`

**Response:**
```json
{
  "result": {
    "estimatedTokens": 6000,
    "estimatedCostUsd": 0.22,
    "agentCount": 3,
    "mode": "debate"
  }
}
```

---

### GET `/v1/reasoning-network/agents`

List all registered FAHRN agents.

**Response:** array of agent objects with `id`, `name`, `provider`, `model`, `taskTypes`, `karmaScore`.

---

### POST `/v1/reasoning-network/agents`

Register a new FAHRN agent.

**Request body:**
```json
{
  "name": "CodeReviewer",
  "provider": "anthropic",
  "model": "claude-sonnet-5",
  "taskTypes": ["code"],
  "systemPrompt": "You are an expert code reviewer..."
}
```

---

### POST `/v1/reasoning-network/agents/seed-openserv`

Auto-populate the agent registry from the live OpenSERV SERV model catalogue. Idempotent.

---

### POST `/v1/reasoning-network/dispatch`

Dispatch a problem directly to the agent network with explicit control over mode and agents.

**Request body:**
```json
{
  "problem": "Write unit tests for this function: ...",
  "taskType": "code",
  "mode": "parallel",
  "eligibleAgentIds": ["agent-1", "agent-2"],
  "maxTotalTokens": 4000,
  "maxCostUsd": 0.20,
  "maxTokensPerAgent": 1500,
  "votingStrategy": "majority",
  "minVotingAgents": 3
}
```

---

### GET `/v1/reasoning-network/agents/{agentId}/skills/{category}`

Get the current SkillOpt skill document for an agent ��� the evolved natural-language procedure that guides its reasoning, produced by the SkillOpt self-improvement loop (Microsoft Research arXiv:2605.23904).

**`category` values:** `code`, `reasoning`, `writing`, `mathematics`, `legal`, `architecture`, `real-time`, `general`

**Response:**
```json
{
  "result": {
    "taskCategory": "code",
    "content": "When reviewing code: 1. Check for...",
    "selectionScore": 0.87,
    "epochsRun": 12,
    "rejectedEdits": 3
  }
}
```

---

### POST `/v1/reasoning-network/agents/{agentId}/skills/{category}/evolve`

Run a SkillOpt epoch — the optimiser model proposes bounded edits to the skill document, a validation gate evaluates them on held-out problems, and the edit is accepted only if the score improves.

---

## Holonic BRAID Reasoning Graphs

### GET `/v1/holonic-braid/graph/{taskType}`

Get the shared Mermaid reasoning graph for a task type, if one has been stored.

**Response:**
```json
{
  "result": {
    "id": "graph-abc",
    "taskType": "architecture",
    "mermaidDiagram": "graph TD\n  A[Understand requirements]...",
    "generatedByModel": "gpt-4o",
    "accuracyScore": 0.91,
    "version": 4,
    "solverCount": 23
  }
}
```

---

### POST `/v1/holonic-braid/graph/{taskType}`

Store a new reasoning graph for a task type.

**Request body:**
```json
{
  "mermaidDiagram": "graph TD\n  A[...]...",
  "generatedByModel": "claude-sonnet-5",
  "version": 1
}
```

---

### POST `/v1/holonic-braid/graph/{id}/outcome`

Record a solver outcome after using a graph. Updates the EMA accuracy score.

**Request body:**
```json
{
  "solverModel": "gpt-4o",
  "succeeded": true,
  "qualityScore": 0.88,
  "tokensUsed": 1240
}
```

---

## Holonic Memory

The fractal memory hierarchy runs from Session → Agent → User → Group → Neighbourhood → District → City → County → Country → Continent → Earth. Every level is a holon. Membrane rules control what propagates upward — the default is private.

### GET `/v1/holonic-memory/earth`

Get (or create on first call) the planetary Earth holon.

---

### POST `/v1/holonic-memory/holons`

Get or create a holon at a given level.

**Query params:** `level` (Session/Agent/User/Group/Neighbourhood/District/City/County/Country/Continent), `name`, `parentHolonId`

---

### PUT `/v1/holonic-memory/holons/{holonId}/membrane-rule`

Set the membrane propagation rule for a holon.

**Request body:**
```json
{
  "fieldsAllowedToPropagate": ["topic", "outcome"],
  "anonymisedAggregateOnly": false,
  "triggerCondition": "topic:professional"
}
```

---

### POST `/v1/holonic-memory/holons/{holonId}/memory`

Record a memory item at a holon.

**Request body:**
```json
{
  "fieldName": "topic",
  "value": "The user is learning about quantum computing.",
  "tags": ["topic:science", "topic:professional"],
  "retentionPolicy": "Persistent",
  "embedding": null
}
```

**`retentionPolicy` values:** `Ephemeral`, `Session`, `Persistent`, `TimeLimited`

For `TimeLimited`, also pass `"expiresUtc": "2026-12-31T00:00:00Z"`.

---

### POST `/v1/holonic-memory/holons/{childHolonId}/propagate`

Propagate permitted items from a child holon to its parent (single hop).

**Response:** integer — count of items propagated.

---

### POST `/v1/holonic-memory/holons/{childHolonId}/propagate-up`

Multi-hop upward propagation.

**Query params:** `levels` (integer, default 1; pass `2147483647` to propagate all the way to Earth)

**Response:** integer — total items propagated across all hops.

---

### GET `/v1/holonic-memory/holons/{holonId}/memory/search`

Semantic search over all memory items in a holon.

**Query params:**

| Param | Type | Default | Description |
|---|---|---|---|
| `q` | string | required | Search query |
| `topK` | int | 5 | Max results to return |
| `provider` | string | `auto` | Embedding provider (`auto`/`openai`/`cohere`/`huggingface`) |

Items with stored embeddings are ranked by cosine similarity. Items without embeddings fall back to keyword-overlap scoring.

**Response:**
```json
{
  "result": [
    {
      "item": {
        "fieldName": "topic",
        "value": "The user is learning about quantum computing.",
        "tags": ["topic:science"],
        "createdUtc": "2026-07-10T14:22:00Z",
        "retentionPolicy": "Persistent"
      },
      "score": 0.943
    }
  ]
}
```

---

## External Memory Providers

Integrates with Mem0, Zep, Letta, LangMem, and Graphiti. Providers are auto-registered on startup when their env vars are present (e.g. `MEM0_API_KEY`, `ZEP_API_KEY`).

### GET `/v1/memory/external/providers`

List all configured and active external memory providers.

**Response:**
```json
{
  "result": ["mem0", "zep", "letta"]
}
```

---

### POST `/v1/memory/external/search`

Semantic search across all (or selected) external providers. Results are merged and ranked by score.

**Request body:**
```json
{
  "avatarId": "00000000-0000-0000-0000-000000000000",
  "query": "quantum computing discussions",
  "providers": ["mem0", "zep"],
  "topK": 10
}
```

**Response:**
```json
{
  "result": [
    {
      "provider": "mem0",
      "id": "mem-123",
      "content": "Discussion about quantum entanglement...",
      "score": 0.91,
      "metadata": {}
    }
  ]
}
```

---

### POST `/v1/memory/external/add`

Add a memory to a specific external provider.

**Request body:**
```json
{
  "avatarId": "00000000-0000-0000-0000-000000000000",
  "provider": "mem0",
  "content": "User prefers concise code examples.",
  "metadata": { "source": "conversation" }
}
```

---

### DELETE `/v1/memory/external/{provider}/{id}`

Delete a specific memory from an external provider.

---

## Orchestrators

### GET `/v1/orchestrators`

List all registered orchestrator adapters.

---

### POST `/v1/orchestrators`

Register an orchestrator adapter.

**Request body:**
```json
{
  "name": "MyLangChainAgent",
  "protocol": "A2A",
  "endpointUrl": "https://my-langchain-agent.example.com",
  "agentId": "agent-langchain-01"
}
```

**`protocol` values:** `MCP`, `A2A`, `ACP`, `ANP`, `GRPC`, `GraphQL`, `Kafka`, `AMQP`, `MQTT`, `HTTP`

---

### POST `/v1/orchestrators/invoke`

Invoke a registered orchestrator adapter.

**Request body:**
```json
{
  "adapterId": "adapter-001",
  "problem": "Summarise this document: ...",
  "avatarId": "00000000-0000-0000-0000-000000000000"
}
```

---

## A2A (Agent-to-Agent Protocol)

### POST `/a2a/tasks/send`

Receive an A2A task from a peer agent. The task is processed asynchronously.

**Request body:**
```json
{
  "id": "task-xyz-001",
  "message": {
    "role": "user",
    "parts": [{ "type": "text", "text": "Solve this problem: ..." }]
  }
}
```

**Response:** task object with `id`, `status` (`working`), `sessionId`.

---

### GET `/a2a/tasks/{id}`

Poll the status of a task.

**`status` values:** `working`, `completed`, `failed`, `cancelled`

---

### GET `/a2a/tasks/{id}/events`

SSE stream of task state changes. Returns `text/event-stream`.

Each event:
```
data: {"id":"task-xyz-001","status":"completed","artifacts":[{"type":"text","content":"..."}]}
```

---

### POST `/a2a/tasks/{id}/cancel`

Cancel a running task.

---

## Identity & Auth (DID / Verifiable Credentials)

### POST `/v1/auth/did`

Authenticate using a W3C Decentralised Identifier.

**Request body:**
```json
{
  "avatarId": "00000000-0000-0000-0000-000000000000"
}
```

**Response:**
```json
{
  "result": {
    "did": "did:key:z6Mk...",
    "document": { "@context": [...], "id": "did:key:z6Mk...", "verificationMethod": [...] }
  }
}
```

---

### GET `/v1/auth/did/{*did}`

Resolve a W3C DID using the Universal Resolver.

---

### POST `/v1/auth/vc`

Issue a Verifiable Credential granting a capability to a subject.

**Request body:**
```json
{
  "subjectDid": "did:key:z6Mk...",
  "capability": "read:holonic-memory",
  "avatarId": "00000000-0000-0000-0000-000000000000"
}
```

---

### POST `/v1/auth/vc/verify`

Verify a Verifiable Credential.

**Request body:** the full VC object previously issued.

**Response:**
```json
{
  "result": { "valid": true }
}
```

---

## Keys & Usage

### GET `/v1/keys`

List the AI provider API keys stored for the authenticated avatar (key values are masked).

---

### POST `/v1/keys`

Encrypt and store a provider API key for the authenticated avatar.

**Request body:**
```json
{
  "provider": "openai",
  "apiKey": "sk-..."
}
```

---

### DELETE `/v1/keys/{provider}`

Delete the stored API key for a provider.

---

### GET `/v1/usage`

Per-avatar token and cost usage summary.

**Response:**
```json
{
  "result": {
    "avatarId": "...",
    "totalTokensThisMonth": 48000,
    "totalCostUsdThisMonth": 1.82,
    "byProvider": {
      "openai": { "tokens": 32000, "costUsd": 1.20 },
      "anthropic": { "tokens": 16000, "costUsd": 0.62 }
    }
  }
}
```

---

## ML.NET In-Process Machine Learning

### GET `/v1/ml/models`

List in-process ML models and their current status (`trained` or `heuristic_fallback`).

---

### POST `/v1/ml/classify-task`

Classify a problem string to a FAHRN task category. Uses the in-process ML.NET SDCA model if trained, otherwise falls back to keyword heuristics. Zero latency — no API call.

**Request body:**
```json
{ "text": "Write a recursive algorithm to traverse a binary tree." }
```

**Response:**
```json
{ "result": { "taskType": "code" } }
```

**`taskType` values:** `code`, `reasoning`, `writing`, `mathematics`, `legal`, `architecture`, `real-time`, `general`

---

### POST `/v1/ml/sentiment`

Sentiment analysis.

**Request body:**
```json
{ "text": "This solution is excellent and well-designed." }
```

**Response:**
```json
{ "result": { "sentiment": "Positive" } }
```

---

### POST `/v1/ml/train`

Train the task classifier from labelled samples. Requires at least 10 samples. The trained model is saved to disk and used immediately.

**Request body:**
```json
{
  "samples": [
    { "problem": "Debug this null pointer exception", "taskType": "code" },
    { "problem": "Prove the Pythagorean theorem", "taskType": "mathematics" }
  ]
}
```

---

## Avatar Context

### GET `/v1/context/avatar/{avatarId}`

Get a rich context block for an avatar — karma score, active quests, installed OAPPs, Web7 consciousness spaces, Web8 mesh nodes. Used by FAHRN to provide background context to agents.

---

## Telemetry

### GET `/v1/telemetry/stream`

Real-time SSE stream of per-request telemetry events. Returns `text/event-stream`. Ring buffer of 500 events; new subscribers receive events as they arrive.

Each event:
```
data: {"provider":"anthropic","model":"claude-sonnet-5","promptTokens":42,"completionTokens":318,"totalTokens":360,"costUsd":0.0012,"latencyMs":820,"timestampUtc":"2026-07-11T09:15:32Z"}
```

---

### GET `/v1/telemetry/history`

Historical telemetry replay from the in-process ring buffer.

**Query params:** `limit` (default 50, max 500)

---

## WebSocket Sessions

### GET `/v1/ws/session`

Upgrade to a WebSocket connection for a bidirectional agent session.

**Client → Server message types:**

| Type | Payload | Description |
|---|---|---|
| `message` | `{ "content": "..." }` | Send a user message, triggers a completion |
| `tool_result` | `{ "toolCallId": "...", "result": "..." }` | Feed a tool result back |
| `interrupt` | — | Interrupt the current generation |
| `ping` | — | Keepalive ping |

**Server → Client message types:**

| Type | Payload | Description |
|---|---|---|
| `session_started` | `{ "sessionId": "..." }` | Session initialised |
| `chunk` | `{ "delta": "...", "provider": "..." }` | Streaming token chunk |
| `tool_call` | `{ "toolCallId": "...", "name": "...", "arguments": {} }` | Model is calling a tool |
| `done` | `{ "totalTokens": N, "costUsd": N }` | Generation complete |
| `error` | `{ "message": "..." }` | Error occurred |
| `pong` | — | Keepalive response |

---

## Health & Providers

### GET `/v1/health`

API health check. Returns `{ "status": "ok" }`.

---

### GET `/v1/providers/status`

Live health and latency check for every configured AI provider. Makes a minimal test call to each.

**Response:**
```json
{
  "result": {
    "openai":    { "healthy": true,  "latencyMs": 210 },
    "anthropic": { "healthy": true,  "latencyMs": 340 },
    "groq":      { "healthy": true,  "latencyMs": 95  },
    "ollama":    { "healthy": false, "error": "Connection refused" }
  }
}
```

---

## MCP & Discovery

### GET `/mcp` · POST `/mcp`

HTTP MCP transport. `GET` establishes an SSE stream; `POST` accepts MCP JSON-RPC tool calls. Allows any HTTP-capable client to use the MCP server without the stdio transport.

---

### GET `/.well-known/mcp.json`

MCP discovery document — lists the MCP server name, version, and capabilities so MCP clients can auto-configure.

---

### GET `/.well-known/agent.json`

A2A agent card — describes this OASIS instance as an A2A-compatible agent (name, capabilities, task input/output schema, authentication requirements).

---

## Error Reference

| HTTP Status | Meaning |
|---|---|
| `400` | Bad request — missing or invalid parameters |
| `401` | Unauthenticated — JWT bearer token missing or expired |
| `403` | Forbidden — avatar does not have permission |
| `404` | Not found — holon, agent, or resource not found |
| `429` | Rate limited — token or cost quota exceeded; check `Retry-After` header |
| `500` | Internal server error — check telemetry stream for details |

---

## See also

- [WEB6 MCP Tool Reference](WEB6_MCP_Tool_Reference.md)
- [WEB6 User Guide](WEB6_User_Guide.md)
- [WEB4 API Reference](../WEB4%20OASIS%20API/WEB4_OASIS_API_Documentation.md)
