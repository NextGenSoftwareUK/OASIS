# WEB6 User Guide

This guide covers everything you need to get started with WEB6 — from installation to common AI and multi-agent workflows.

---

## What is WEB6?

WEB6 is the AI Abstraction & Orchestration Layer of the OASIS Omniverse. It gives you:

- **One endpoint for every AI provider** — send the same request to OpenAI, Anthropic, Gemini, Groq, Mistral, or 10+ more. WEB6 normalises the wire format so you never have to rewrite code when you switch providers.
- **FAHRN multi-agent orchestration** — automatically route problems to a network of specialised AI agents, running them in parallel, having them debate, vote, or decompose the problem into subtasks.
- **Holonic BRAID shared memory** — agents across sessions share a growing library of Mermaid reasoning graphs. Over time, the network gets better at your specific problem types.
- **Fractal holonic memory** — structured, hierarchical memory from session level all the way up to a shared planetary Earth holon, with consent-governed membrane rules controlling what propagates upward.
- **External memory** — plug in Mem0, Zep, Letta, LangMem, or Graphiti as memory backends. WEB6 searches them all and injects the relevant context into your prompts automatically.
- **111 MCP tools** — the entire WEB4–WEB10 stack is available directly in Cursor, VS Code, and Claude Desktop.

---

## Installation

### Option 1 — MCP Server (for IDE use)

```bash
# npm — recommended, no .NET SDK required
npm install -g @oasisomniverse/mcp-server

# NuGet dotnet tool
dotnet tool install -g NextGenSoftware.OASIS.MCP.Server
```

Add to your IDE config (`~/.cursor/mcp.json`, `.vscode/mcp.json`, or `claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "oasis": {
      "command": "oasis-mcp",
      "env": {
        "OASIS_API_URL": "https://api.web4.oasisomniverse.one",
        "OPENAI_API_KEY": "sk-...",
        "ANTHROPIC_API_KEY": "sk-ant-..."
      }
    }
  }
}
```

Restart your IDE. The 111 OASIS tools now appear in the tool list.

---

### Option 2 — REST API (self-hosted)

**Requirements:** .NET 10 SDK

```bash
git clone https://github.com/NextGenSoftwareUK/OASIS.git
cd OASIS/WEB6/NextGenSoftware.OASIS.Web6.WebAPI
dotnet run
```

The API starts on `http://localhost:5000`. Swagger UI is at `http://localhost:5000/swagger`.

> **Note:** SSE streaming endpoints (`/v1/complete/stream`, `/v1/telemetry/stream`, `/a2a/tasks/{id}/events`), the WebSocket session (`/ws/session`), and the auto-discovery documents (`/.well-known/mcp.json`, `/.well-known/agent.json`) are intentionally hidden from Swagger UI because they use streaming transports that the interactive explorer cannot represent. Use `curl` or a WebSocket client to call them directly — they are fully documented in the [REST API Reference](WEB6_REST_API_Reference.md).

---

### Option 3 — NuGet packages (embed in your .NET app)

```bash
dotnet add package NextGenSoftware.OASIS.Web6.Core
dotnet add package NextGenSoftware.OASIS.Web6.WebAPI
```

---

## Environment Variables

At minimum, set one AI provider key. WEB6 `auto` routing picks the best available provider based on what keys are configured.

```bash
# AI providers (set at least one)
OPENAI_API_KEY=sk-...
ANTHROPIC_API_KEY=sk-ant-...
GEMINI_API_KEY=...
GROQ_API_KEY=...
MISTRAL_API_KEY=...

# External memory (optional — enables automatic context injection)
MEM0_API_KEY=...
ZEP_API_KEY=...
LETTA_BASE_URL=http://localhost:8283

# OASIS platform
OASIS_API_URL=https://api.web4.oasisomniverse.one
```

---

## Common Workflows

### 1. Simple AI completion

The quickest way to make an AI call that works across any provider:

**REST:**
```bash
curl -X POST https://api.web6.oasisomniverse.one/v1/complete \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "auto",
    "messages": [
      { "role": "user", "content": "Explain the OASIS Omniverse in one paragraph." }
    ]
  }'
```

**MCP (in your IDE):**
```
Use the web6_complete tool with provider="auto" and a user message.
```

The response always has the same shape regardless of which provider handled it:

```json
{
  "result": {
    "content": "The OASIS Omniverse is...",
    "provider": "anthropic",
    "model": "claude-sonnet-5",
    "totalTokens": 280,
    "costUsd": 0.0009
  }
}
```

---

### 2. Streaming completion

```bash
curl -X POST https://api.web6.oasisomniverse.one/v1/complete/stream \
  -H "Content-Type: application/json" \
  --no-buffer \
  -d '{ "provider": "openai", "messages": [{ "role": "user", "content": "Tell me a story." }] }'
```

Each `data:` event contains a `delta` token. The final event has `"done": true`.

---

### 3. Tool/function calling

Submit a completion with tools defined:

```json
{
  "provider": "anthropic",
  "messages": [{ "role": "user", "content": "What is the weather in London?" }],
  "tools": [
    {
      "name": "get_weather",
      "description": "Get current weather for a city",
      "parameters": {
        "type": "object",
        "properties": { "city": { "type": "string" } },
        "required": ["city"]
      }
    }
  ]
}
```

When the model calls a tool, the response has `toolCalls` populated. Feed the result back:

```bash
curl -X POST /v1/complete/tool-result \
  -d '{
    "sessionId": "sess-abc",
    "toolCallId": "call_xyz",
    "result": "{ \"temperature\": 18, \"condition\": \"cloudy\" }"
  }'
```

---

### 4. FAHRN multi-agent solve (recommended for complex problems)

Use `web6_fahrn_solve` (or `POST /v1/fahrn/solve`) for any problem that benefits from multiple AI models reasoning together:

```json
{
  "problem": "Design a distributed event sourcing system that handles 1M events/sec with sub-10ms read latency.",
  "mode": "debate",
  "maxCostUsd": 0.50
}
```

FAHRN automatically:
1. Classifies the problem as `architecture`
2. Looks up the cached BRAID reasoning graph for `architecture` problems
3. Dispatches to the 3 best-scoring architecture agents
4. Runs them in Debate mode (each agent sees and critiques the others' answers)
5. Returns the best answer with a full reasoning trace

**Set a budget** with `maxCostUsd` or `maxTotalTokens` to prevent runaway spending. FAHRN stops launching new agents the moment the threshold is crossed and returns the best result seen so far (`budgetExceededBehaviour: "best_so_far"`).

---

### 5. Storing and searching memory

Record something at a session holon:

```bash
# First, get your avatar's user holon (or create it)
curl -X POST /v1/holonic-memory/holons \
  -d '{ "level": "User", "name": "david-user", "parentHolonId": "..." }'

# Record a memory item
curl -X POST /v1/holonic-memory/holons/{holonId}/memory \
  -d '{
    "fieldName": "preference",
    "value": "User prefers TypeScript over JavaScript for new projects.",
    "tags": ["preference:language"],
    "retentionPolicy": "Persistent"
  }'
```

Retrieve relevant memories semantically:

```bash
curl "/v1/holonic-memory/holons/{holonId}/memory/search?q=programming+language+preferences&topK=3"
```

Returns the top-3 memory items most semantically similar to the query, using cosine similarity if embeddings are stored or keyword overlap otherwise.

---

### 6. Using external memory providers

Configure `MEM0_API_KEY` (and/or `ZEP_API_KEY`, `LETTA_BASE_URL`, etc.) and WEB6 auto-registers them on startup.

Search across all providers:

```bash
curl -X POST /v1/memory/external/search \
  -d '{
    "avatarId": "...",
    "query": "previous discussions about React architecture",
    "topK": 5
  }'
```

Or inject external memory automatically into completions by passing `externalMemoryProviders`:

```json
{
  "provider": "auto",
  "messages": [{ "role": "user", "content": "How should I structure my React app?" }],
  "externalMemoryProviders": ["mem0", "zep"],
  "avatarId": "..."
}
```

WEB6 searches the named providers for context relevant to the user's message and prepends a `[External Memory]` block to the system prompt before sending to the AI.

---

### 7. WebSocket bidirectional sessions

For interactive conversations without round-tripping full history each time:

```javascript
const ws = new WebSocket('wss://api.web6.oasisomniverse.one/v1/ws/session');

ws.onmessage = (event) => {
  const msg = JSON.parse(event.data);
  if (msg.type === 'chunk') process.stdout.write(msg.delta);
  if (msg.type === 'done') console.log('\nTotal tokens:', msg.totalTokens);
  if (msg.type === 'tool_call') {
    // Execute the tool locally and feed the result back
    const result = executeLocalTool(msg.name, msg.arguments);
    ws.send(JSON.stringify({ type: 'tool_result', toolCallId: msg.toolCallId, result }));
  }
};

ws.onopen = () => {
  ws.send(JSON.stringify({ type: 'message', content: 'What is quantum entanglement?' }));
};
```

---

### 8. Monitoring with the telemetry stream

```bash
curl -N https://api.web6.oasisomniverse.one/v1/telemetry/stream
```

Every time a completion happens anywhere in the system, you see a structured event:

```
data: {"provider":"groq","model":"llama-3.3-70b-versatile","promptTokens":38,"completionTokens":210,"totalTokens":248,"costUsd":0.0001,"latencyMs":420}
```

Use this to monitor costs, catch slow providers, or trigger alerts.

---

### 9. DID-based authentication

Authenticate without a password using a W3C Decentralised Identifier tied to your OASIS avatar:

```bash
# Get (or create) your DID
curl -X POST /v1/auth/did \
  -d '{ "avatarId": "your-avatar-guid" }'

# Response includes your did:key identifier
# { "result": { "did": "did:key:z6Mk...", "document": {...} } }

# Resolve any DID
curl /v1/auth/did/did:key:z6Mk...
```

---

### 10. Agent protocols — connecting external agents

Register an external agent (LangChain, AutoGen, CrewAI, etc.) as an orchestrator adapter:

```bash
curl -X POST /v1/orchestrators \
  -d '{
    "name": "MyLangChainAgent",
    "protocol": "A2A",
    "endpointUrl": "https://my-agent.example.com",
    "agentId": "agent-langchain-01"
  }'
```

Invoke it:

```bash
curl -X POST /v1/orchestrators/invoke \
  -d '{
    "adapterId": "adapter-id-from-above",
    "problem": "Summarise the latest quarterly report.",
    "avatarId": "..."
  }'
```

WEB6 translates to A2A wire format, polls the task to completion, and returns the result in the standard OASIS envelope.

---

## Budget Management

Every FAHRN dispatch and solve request accepts budget parameters:

| Parameter | Type | Description |
|---|---|---|
| `maxTotalTokens` | int | Hard token ceiling across all agent calls |
| `maxCostUsd` | decimal | Hard USD cost ceiling |
| `maxTokensPerAgent` | int | Per-agent token limit |
| `budgetExceededBehaviour` | string | `stop` (default) or `best_so_far` |

When the budget is hit, FAHRN stops launching new agents immediately and returns what it has so far with `budgetExceeded: true` in the response.

---

## SkillOpt — Self-Improving Agents

Each FAHRN agent has a _skill document_ for each task category — a natural language procedure that guides its reasoning. SkillOpt (based on Microsoft Research arXiv:2605.23904) improves these documents over time:

1. An optimiser model proposes bounded edits to the skill document
2. WEB6 evaluates the edit on held-out problems using word-overlap scoring
3. The edit is accepted only if the score improves
4. Over time this compounds to +23.5% average improvement

To manually trigger an improvement epoch:

```bash
curl -X POST /v1/reasoning-network/agents/{agentId}/skills/code/evolve
```

Or use the MCP tool `web6_fahrn_evolve_agent_skill` from your IDE.

---

## ML.NET In-Process Classification

WEB6 ships a zero-latency ML.NET task classifier that runs in-process before every FAHRN dispatch. No API call, no network round-trip — the problem is classified in microseconds.

Check what it classifies your text as:

```bash
curl -X POST /v1/ml/classify-task \
  -d '{ "text": "Write a recursive Fibonacci function in Python." }'
# { "result": { "taskType": "code" } }
```

The classifier starts as a keyword-heuristic fallback and upgrades to a trained SDCA model once you feed it labelled training data:

```bash
curl -X POST /v1/ml/train \
  -d '{
    "samples": [
      { "problem": "Debug the null pointer exception", "taskType": "code" },
      { "problem": "Prove Fermat'\''s last theorem", "taskType": "mathematics" }
    ]
  }'
```

---

## Per-Avatar Key Vault

Store your AI provider keys in the encrypted OASIS key vault so they travel with your avatar rather than being hard-coded:

```bash
# Store a key
curl -X POST /v1/keys \
  -H "Authorization: Bearer <jwt>" \
  -d '{ "provider": "openai", "apiKey": "sk-..." }'

# WEB6 will use it automatically for any completion by this avatar
curl -X POST /v1/complete \
  -H "Authorization: Bearer <jwt>" \
  -d '{ "provider": "openai", "messages": [...], "avatarId": "..." }'
```

---

## Troubleshooting

**"No provider has a key configured"**
Set at least one AI provider key in your environment (e.g. `OPENAI_API_KEY`) or store one in the key vault.

**429 Too Many Requests**
You've hit the per-avatar token or cost quota. Check the `Retry-After` header and either wait or increase the quota via usage settings.

**Provider returns an error but auto-routing is on**
WEB6 tries fallback providers automatically. If all fail, check `GET /v1/providers/status` for a live health report.

**Holonic memory search returns low-quality results**
The items likely don't have stored embeddings yet. When recording memory items, also call `POST /v1/embed` on the item's value and store the result in the `embedding` field. Future searches will use cosine similarity instead of keyword overlap.

**WebSocket disconnects immediately**
Ensure the server has `app.UseWebSockets()` enabled (it does by default). Check that your reverse proxy (nginx/caddy) passes `Upgrade: websocket` headers through.

---

## API Quick Reference

| Task | Endpoint / Tool |
|---|---|
| AI completion | `POST /v1/complete` · `web6_complete` |
| Streaming | `POST /v1/complete/stream` |
| Multi-agent solve | `POST /v1/fahrn/solve` · `web6_fahrn_solve` |
| Embeddings | `POST /v1/embed` · `web6_embed` |
| Memory search | `GET /v1/holonic-memory/holons/{id}/memory/search` · `web6_memory_search` |
| External memory | `POST /v1/memory/external/search` · `web6_memory_external_search` |
| Provider health | `GET /v1/providers/status` |
| Telemetry | `GET /v1/telemetry/stream` |
| DID auth | `POST /v1/auth/did` |
| Cost estimate | `GET /v1/fahrn/budget-estimate` |

---

## See also

- [WEB6 REST API Reference](WEB6_REST_API_Reference.md) — full endpoint docs with request/response shapes
- [WEB6 MCP Tool Reference](WEB6_MCP_Tool_Reference.md) — all 111 tool parameters and returns
- [WEB6 GitHub README](../../../../WEB6/README.md)
- [MCP Server README](../../../../WEB6/NextGenSoftware.OASIS.MCP.Server/README.md)
- [WEB4 API Docs](../WEB4%20OASIS%20API/README.md)
