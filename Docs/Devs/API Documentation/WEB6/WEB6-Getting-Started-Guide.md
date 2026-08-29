# OASIS WEB6 — Getting Started Guide

**Hosted API:** `https://api.web6.oasisomniverse.one` (free tier, no credit card required)
**npm:** `@oasisomniverse/web6-api` | **Python:** `oasis-web6`
**Docs site:** https://web6.oasisomniverse.one

---

## What is WEB6?

WEB6 is a unified AI abstraction and orchestration layer. Instead of integrating 97 AI providers separately, you call one endpoint and WEB6 routes to the right model automatically — by cost, quality or latency — with automatic failover if a provider goes down.

On top of routing, WEB6 adds:

- **FAHRN** — a self-improving multi-agent reasoning network that routes sub-tasks to the best-scoring agent, learns from every outcome, and handles serial, parallel or decomposed dispatch modes
- **Holonic BRAID** — a shared fractal memory graph that stores reasoning plans as holons, re-using them across sessions for up to 74× performance-per-dollar gains
- **Holonic Memory** — a fractal hierarchy (session → agent → user → group → city → Earth) that compounds intelligence across every interaction
- **Semantic caching** — returns cached results for 95%+ similar prompts at zero provider cost
- **MCP server** — every FAHRN agent, memory provider and protocol adapter exposed as an MCP tool, auto-discovered by Claude Code, Cursor and any MCP-compatible host
- **17 orchestrator protocols** — MCP, A2A, ACP, ANP, LangGraph, OpenAI Agents SDK / Swarm, Nostr NIP-90, LangChain, AutoGen, CrewAI, SemanticKernel, gRPC, GraphQL, Kafka, AMQP, MQTT, Webhook — all via `POST /v1/orchestrators/invoke`

---

## 1. Quick Start — Hosted API

The hosted API at `https://api.web6.oasisomniverse.one` is **free** (free plan, ~100 req/min). No sign-up is required to evaluate; create an OASIS avatar to get a persistent key and track usage.

### REST

```bash
curl -X POST https://api.web6.oasisomniverse.one/v1/complete \
  -H "Authorization: Bearer <your-oasis-avatar-key>" \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "auto",
    "model": "auto",
    "messages": [{ "role": "user", "content": "Summarise this document in 3 bullets." }],
    "routing": { "priority": "cost", "fallback": true }
  }'
```

**Routing priority options:** `cost` | `latency` | `quality`

Pin a specific provider when you need it:

```bash
-d '{ "provider": "anthropic", "model": "claude-sonnet-4-6", ... }'
```

### JavaScript / Node.js

```bash
npm install @oasisomniverse/web6-api
```

```js
import { Web6Client } from '@oasisomniverse/web6-api';

const web6 = new Web6Client({ baseUrl: 'https://api.web6.oasisomniverse.one' });
web6.setToken('<your-oasis-jwt>');

const { result } = await web6.completion.complete({
  Provider: 'auto',
  Model: 'auto',
  Messages: [{ Role: 'user', Content: 'Hello from the OASIS' }],
  Routing: { Priority: 'cost', Fallback: true },
});

console.log(result.Content);
```

### Python

```bash
pip install oasis-web6
```

```python
from oasis_web6 import Web6Client

client = Web6Client(base_url="https://api.web6.oasisomniverse.one", token="<your-oasis-jwt>")

response = client.complete(
    provider="auto",
    model="auto",
    messages=[{"role": "user", "content": "Hello from the OASIS"}],
    routing={"priority": "cost", "fallback": True}
)

print(response["content"])
```

---

## 2. Supported Providers

Pass any of these as `"provider"` to pin a specific backend, or use `"auto"` to let WEB6 decide. Values are matched case-insensitively against the `AIProviderType` enum.

| Key | Provider | Notes |
|-----|----------|-------|
| `OpenAI` | OpenAI | GPT-5, GPT-4o, o3 |
| `Anthropic` | Anthropic | Claude Opus/Sonnet/Haiku |
| `Gemini` | Google | Gemini 2.5 Flash/Pro |
| `Groq` | Groq | Llama 3 — ultra-fast LPU inference |
| `Mistral` | Mistral | Mixtral, Large, Codestral |
| `Ollama` | Ollama | Any local model (set `OLLAMA_BASE_URL`) |
| `Cohere` | Cohere | Command R+ |
| `XAI` | xAI | Grok 3/Vision |
| `DeepSeek` | DeepSeek | R1, V3, Coder |
| `OpenServ` | OpenServ | One SERV key → GPT-5 · Claude · Gemini · Grok · Qwen · DeepSeek |
| `AWSBedrock` | AWS Bedrock | Titan, Nova, Jurassic |
| `AzureOpenAI` | Azure OpenAI | Enterprise GPT deployments |
| `HuggingFace` | HuggingFace | Open-source / fine-tuned models |
| `StabilityAI` | Stability AI | Image generation (SDXL) |
| `Cerebras` | Cerebras | ~3000 tok/s, fastest inference (llama-3.3-70b) |
| `TogetherAI` | Together AI | 100+ open models |
| `FireworksAI` | Fireworks AI | Fast open model inference |
| `MoonshotAI` | Moonshot (Kimi) | 128k context, strong on long docs |
| `Perplexity` | Perplexity | Web-grounded answers with citations |
| `LMStudio` | LM Studio | Local inference (set `LM_STUDIO_BASE_URL`) |
| `Bittensor` | Bittensor | Decentralised inference via Corcel |
| `GaiaNet` | GaiaNet | Community-run decentralised nodes |
| `LeelaAI` | Leela AI | Spiritual intelligence / karmic-pattern reasoning |
| `Replicate` | Replicate | Open-source model marketplace — image, audio, video, language |

---

## 3. FAHRN — Multi-Agent Reasoning

FAHRN (Fractal Adaptive Holonic Reasoning Network) is a self-optimising agent mesh. A controller agent classifies your problem, picks the best agents by composite score (accuracy × speed × cost × reliability), dispatches in the chosen mode, and returns a structured Mermaid execution plan alongside the answer.

### Single FAHRN call

```bash
POST https://api.web6.oasisomniverse.one/v1/fahrn/solve
Authorization: Bearer <your-oasis-avatar-key>

{
  "problem": "Plan a zero-downtime migration of a 50M row Postgres table",
  "taskType": "architecture",
  "mode": "Parallel"
}
```

**Dispatch modes:**

| Mode | Best for | Cost |
|------|----------|------|
| `Serial` | Routine tasks, cost-first | Lowest |
| `Parallel` | High-stakes accuracy | Higher (multiple agents) |
| `Decomposed` | Complex / multi-domain problems | Medium (parallel sub-tasks) |
| `Debate` | Adversarial validation of a decision | Medium |
| `Voting` | Consensus across independent agents | Medium |

### FAHRN via the JS client

Use the FAHRN hero endpoint directly when you want to control dispatch mode:

```js
const { result } = await web6.fahrn.solve({
  Problem: 'Your problem here',
  TaskType: 'general',
  Mode: 'Parallel',    // Serial | Parallel | Decomposed | Debate | Voting
});
```

Or enable FAHRN enrichment inside a normal completion call (always uses Serial mode):

```js
const { result } = await web6.completion.complete({
  Provider: 'auto',
  Model: 'auto',
  Messages: [{ Role: 'user', Content: 'Your problem here' }],
  UseFAHRN: true,
  FahrnTaskType: 'general',
});
```

---

## 4. Holonic BRAID — Shared Reasoning Graph

BRAID stores every reasoning plan as a holon in a shared graph library. The next agent facing the same task type retrieves the plan at zero generation cost — compounding gains globally.

Under the hood: a high-tier **Generator** model creates a Mermaid reasoning graph once per task type. A low-tier **Solver** model executes it per instance. Result: **74× performance-per-dollar** on GSM-Hard benchmarks, 94%→98% accuracy.

Enable BRAID in a request:

```js
const { result } = await web6.completion.complete({
  Provider: 'auto',
  Model: 'auto',
  Messages: [{ Role: 'user', Content: 'Classify intent of this customer message.' }],
  UseHolonicBraid: true,
  UseFAHRN: true,
});
```

Plans are automatically persisted to MongoDB (fast access), Solana (immutable provenance) and IPFS (decentralised permanence) via the COSMIC ORM.

---

## 5. Holonic Memory

Memory persists in a fractal hierarchy:

```
Session → Agent → User → Group → Neighbourhood → City → Country → Earth
```

Higher holons accumulate shared intelligence from every session below them. Membrane rules govern what propagates and what stays private.

### Full endpoint reference

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/v1/holonic-memory/earth` | Get the planetary Earth holon |
| `POST` | `/v1/holonic-memory/holons` | Get or create a holon at a given level |
| `PUT` | `/v1/holonic-memory/holons/{id}/membrane-rule` | Set the membrane rule governing upward propagation |
| `POST` | `/v1/holonic-memory/holons/{id}/memory` | Store a single memory item |
| `POST` | `/v1/holonic-memory/holons/{id}/documents` | **Bulk ingest** — auto-chunk a document and store each chunk |
| `GET` | `/v1/holonic-memory/holons/{id}/memory/search` | Semantic search across memory items |
| `POST` | `/v1/holonic-memory/holons/{id}/propagate` | Propagate one hop up the hierarchy |
| `POST` | `/v1/holonic-memory/holons/{id}/propagate-up` | Propagate N hops up (pass `levels=2147483647` for Earth) |

### Bulk document ingest (RAG) with semantic deduplication

POST your full document text to `/documents`. WEB6 auto-chunks it with a sliding-window splitter, embeds each chunk, checks for near-identical content already in the holon (cosine similarity ≥ 0.98), and stores only genuinely new chunks. Duplicate chunks are skipped and the existing fieldName is returned instead — so 10 documents sharing a common paragraph store that paragraph exactly once.

```python
# Ingest a full document — WEB6 chunks, embeds, deduplicates, and stores
response = httpx.post(
    f"https://api.web6.oasisomniverse.one/v1/holonic-memory/holons/{corpus_holon_id}/documents",
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"},
    json={
        "content": full_document_text,
        "documentName": "cbt-protocol-v3",
        "chunkTokens": 400,        # ~1600 chars per chunk
        "overlapTokens": 50,       # ~200 char overlap at boundaries
        "tags": ["protocol", "CBT"],
        "retentionPolicy": "Persistent"
    }
)

result = response.json()["result"]
print(f"{result['storedChunks']} new, {result['deduplicatedChunks']} deduplicated of {result['totalChunks']} total")
# chunkFieldNames: ["doc-cbt-protocol-v3-chunk-001", ...] — includes existing fieldNames for deduped chunks
```

**Why this saves money over a flat vector store:**
- Shared content (e.g. the same evidence paragraph cited in 20 protocols) is stored once, not 20 times
- Embeddings are computed once per unique chunk, not per document
- Storage grows with unique information, not with document count

### Semantic search

```bash
GET https://api.web6.oasisomniverse.one/v1/holonic-memory/holons/{holonId}/memory/search?q=How+should+a+therapist+respond+to+hopelessness%3F&topK=5
Authorization: Bearer <your-oasis-avatar-key>
```

Returns the top-K most semantically similar chunks using cosine similarity over stored embedding vectors. Falls back to keyword overlap when embeddings have not been computed.

### Avatar context

```bash
# Retrieve avatar context (identity + karma + memory summary) for any system
GET https://api.web6.oasisomniverse.one/v1/context/avatar/{avatarId}
Authorization: Bearer <your-oasis-avatar-key>
```

External memory providers (Mem0, Zep, Letta, LangMem, Graphiti, Qdrant, Weaviate) plug in alongside Holonic Memory via the same interface.

---

## 6. Embeddings & Semantic Cache

The embeddings endpoint accepts a `texts` array (not a single `input` string):

```bash
POST https://api.web6.oasisomniverse.one/v1/embed
Authorization: Bearer <your-oasis-avatar-key>

{
  "texts": ["Your text here"],
  "provider": "auto",
  "model": "auto"
}
```

Providers: `"openai"`, `"cohere"`, `"huggingface"`, or `"auto"`.

Semantic caching is on by default. Requests with ≥95% cosine similarity to a cached prompt return instantly at **zero provider cost**. Especially effective for repeated document Q&A and classification pipelines.

---

## 7. Streaming (SSE)

All completion endpoints support streaming via Server-Sent Events, normalised across every provider:

```js
const stream = await web6.completion.stream({
  Provider: 'auto',
  Messages: [{ Role: 'user', Content: 'Write a long essay...' }],
});

for await (const chunk of stream) {
  process.stdout.write(chunk.delta);
}
```

---

## 8. WebSocket Sessions

For persistent multi-turn agent sessions with server-side state, connect to `/v1/ws/session` as a WebSocket. The avatar identity is taken from your JWT token. The server maintains conversation history for the lifetime of the connection.

**Client → server messages:**
```json
{ "type": "message", "content": "Your user message here" }
{ "type": "tool_result", "toolCallId": "...", "result": "..." }
{ "type": "interrupt" }
{ "type": "ping" }
```

**Server → client messages:**
```json
{ "type": "session_started", "sessionId": "..." }
{ "type": "chunk", "delta": "token...", "provider": "anthropic", "model": "..." }
{ "type": "done", "totalTokens": 512, "latencyMs": 0 }
{ "type": "error", "message": "..." }
{ "type": "pong" }
```

**Example (Node.js):**
```js
const ws = new WebSocket('wss://api.web6.oasisomniverse.one/v1/ws/session', {
  headers: { Authorization: 'Bearer <your-oasis-avatar-key>' }
});

ws.on('open', () => {
  ws.send(JSON.stringify({ type: 'message', content: 'Hello' }));
});

ws.on('message', (data) => {
  const msg = JSON.parse(data);
  if (msg.type === 'chunk') process.stdout.write(msg.delta);
  if (msg.type === 'done') console.log('\n[done]');
});
```

---

## 9. MCP Integration

WEB6 self-registers as an MCP orchestrator on startup. Every FAHRN agent, memory provider and protocol adapter is exposed as an MCP tool — auto-discovered by Claude Code, Cursor, Continue and any MCP-compatible host.

**HTTP MCP transport (for cloud agents and claude.ai connectors):**

```
GET  https://api.web6.oasisomniverse.one/mcp        — SSE stream
POST https://api.web6.oasisomniverse.one/mcp        — JSON-RPC tool calls
GET  https://api.web6.oasisomniverse.one/.well-known/mcp.json   — auto-discovery
GET  https://api.web6.oasisomniverse.one/.well-known/agent.json — A2A agent card
```

Point your MCP client at the discovery URL and it configures itself automatically.

**Claude Code config:**

```json
{
  "mcpServers": {
    "oasis-web6": {
      "url": "https://api.web6.oasisomniverse.one/mcp",
      "headers": { "Authorization": "Bearer <your-oasis-avatar-key>" }
    }
  }
}
```

---

## 10. Tool / Function Calling

Define tools once — WEB6 translates the schema to each provider's native format:

```js
const { result } = await web6.completion.complete({
  Provider: 'auto',
  Messages: [{ Role: 'user', Content: 'What is the weather in London?' }],
  Tools: [{
    Name: 'get_weather',
    Description: 'Returns current weather for a city',
    Parameters: {
      type: 'object',
      properties: { city: { type: 'string' } },
      required: ['city']
    }
  }]
});
```

---

## 11. Plugging into Existing Agentic Flows

WEB6 exposes **two completion endpoints**:

| Endpoint | Format | Use when |
|----------|--------|----------|
| `POST /v1/complete` | WEB6 native | New integrations, full feature access |
| `POST /v1/chat/completions` | OpenAI-compatible | Drop-in swap for any existing OpenAI-based code |

The OpenAI-compatible shim accepts the standard OpenAI request body and returns the standard OpenAI response envelope. Change only your base URL and API key — nothing else in your code needs to change.

### LangChain (Python)

```python
from langchain_openai import ChatOpenAI

llm = ChatOpenAI(
    openai_api_base="https://api.web6.oasisomniverse.one/v1",
    openai_api_key="<your-oasis-avatar-key>",
    model="claude-sonnet-4-6"   # or "auto" to let WEB6 pick cheapest available
)

response = llm.invoke("Summarise the key points of the attached document.")
print(response.content)
```

### LangChain (JS)

```js
import { ChatOpenAI } from '@langchain/openai';

const llm = new ChatOpenAI({
  configuration: { baseURL: 'https://api.web6.oasisomniverse.one/v1' },
  apiKey: '<your-oasis-avatar-key>',
  model: 'claude-sonnet-4-6',
});
```

### AutoGen (Python)

```python
import autogen

llm_config = {
    "config_list": [{
        "model": "claude-sonnet-4-6",
        "api_key": "<your-oasis-avatar-key>",
        "base_url": "https://api.web6.oasisomniverse.one/v1",
    }]
}

assistant = autogen.AssistantAgent("assistant", llm_config=llm_config)
user_proxy = autogen.UserProxyAgent("user_proxy", human_input_mode="NEVER")

user_proxy.initiate_chat(assistant, message="Your task here")
```

### CrewAI

```python
from crewai import LLM, Agent, Task, Crew

llm = LLM(
    model="openai/claude-sonnet-4-6",
    base_url="https://api.web6.oasisomniverse.one/v1",
    api_key="<your-oasis-avatar-key>"
)

researcher = Agent(role="Researcher", goal="Find the answer", llm=llm)
task = Task(description="Research and summarise topic X", agent=researcher)
Crew(agents=[researcher], tasks=[task]).kickoff()
```

### Semantic Kernel (C#)

```csharp
using Microsoft.SemanticKernel;

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(
        modelId:  "claude-sonnet-4-6",
        apiKey:   "<your-oasis-avatar-key>",
        endpoint: new Uri("https://api.web6.oasisomniverse.one/v1")
    )
    .Build();

var result = await kernel.InvokePromptAsync("Your prompt here");
Console.WriteLine(result);
```

### Direct REST (any language)

```python
import httpx

response = httpx.post(
    "https://api.web6.oasisomniverse.one/v1/complete",
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"},
    json={
        "provider": "auto",
        "model": "auto",
        "messages": [{"role": "user", "content": "Your prompt here"}],
        "routing": {"priority": "cost", "fallback": True}
    }
)
print(response.json()["result"]["content"])
```

### npm client

```js
import { Web6Client } from '@oasisomniverse/web6-api';

const web6 = new Web6Client({ baseUrl: 'https://api.web6.oasisomniverse.one' });
web6.setToken('<your-oasis-jwt>');

const { result } = await web6.completion.complete({
  Provider: 'auto',
  Model: 'auto',
  Messages: [{ Role: 'user', Content: 'Your prompt here' }],
  Routing: { Priority: 'cost', Fallback: true },
});
```

### Python package

```python
from oasis_web6 import Web6Client

client = Web6Client(base_url="https://api.web6.oasisomniverse.one", token="<your-oasis-jwt>")
response = client.complete(
    provider="auto",
    model="auto",
    messages=[{"role": "user", "content": "Your prompt here"}]
)
```

---

## 12. Spinning Up Your Own ONODE

If you need a private deployment (data sovereignty, custom models, on-premise), you can self-host the ONODE — the server that runs the WEB6 API.

### Prerequisites

- .NET 10 SDK
- Docker (optional)
- MongoDB (default storage) — or configure any of the 40+ COSMIC ORM providers

### Clone & Run

```bash
git clone https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-HoloAGI
cd WEB6/NextGenSoftware.OASIS.Web6.WebAPI
dotnet run
```

Or with Docker:

```bash
docker pull nextgensoftware/oasis-web6
docker run -p 8080:8080 -v ./OASIS_DNA.json:/app/OASIS_DNA.json nextgensoftware/oasis-web6
```

### OASISDNA — Web6 Settings

All Web6 behaviour is configured in `OASIS_DNA.json` under the `OASIS.Web6` key:

```json
{
  "OASIS": {
    "Web6": {

      "DefaultProvider": "Auto",
      "DefaultRoutingPriority": "cost",
      "DefaultRoutingFallbackEnabled": true,

      "PreferOpenServ": false,

      "EnableFAHRN": true,
      "EnableHolonicBraid": true,

      "FAHRN": {
        "EMAAlpha": 0.2,
        "DefaultDispatchMode": "Serial",
        "AutoSeedOpenServAgentsOnStartup": true,
        "MaxDecomposedSubProblems": 3
      },

      "HolonicBraid": {
        "AutoPersistWinningPlan": true
      },

      "HolonicMemory": {
        "DefaultRetentionPolicy": "Default",
        "RecordDispatchOutcomes": true
      },

      "OpenServ": {
        "BaseUrl": "https://inference-api.openserv.ai/v1/chat/completions",
        "DefaultModel": "gpt-5.4"
      },

      "ApiKeys": {
        "OpenAI":      "sk-...",
        "Anthropic":   "sk-ant-...",
        "Gemini":      "",
        "Groq":        "",
        "Mistral":     "",
        "Cohere":      "",
        "XAI":         "",
        "DeepSeek":    "",
        "HuggingFace": "",
        "AzureOpenAI": "",
        "StabilityAI": "",
        "OpenServ":    "serv_..."
      }
    }
  }
}
```

**Key settings explained:**

| Setting | Effect |
|---------|--------|
| `DefaultProvider` | `Auto` lets WEB6 pick; or pin to `openai`, `anthropic`, etc. |
| `DefaultRoutingPriority` | `cost` / `latency` / `quality` — the default weight for auto routing |
| `EnableFAHRN` | Enable the multi-agent reasoning network |
| `EnableHolonicBraid` | Enable shared reasoning graph library |
| `FAHRN.EMAAlpha` | Learning rate for composite score updates (0.1 = slow, 0.4 = fast) |
| `FAHRN.DefaultDispatchMode` | `Serial` (cheapest), `Parallel` (most accurate), `Decomposed` (complex tasks) |
| `HolonicBraid.AutoPersistWinningPlan` | Save winning Mermaid plans to the shared graph automatically |
| `HolonicMemory.RecordDispatchOutcomes` | Feed every outcome back into agent scoring |
| `PreferOpenServ` | Route through OpenServ's SERV gateway by default (one key, all models) |

The `StorageProviders` section (separate from `Web6`) controls which of the 40+ COSMIC ORM backends your ONODE uses for holon persistence, avatars and BRAID graphs.

---

## 13. Hosted API Plans

| Plan | Requests/month | FAHRN | Holonic BRAID | Price |
|------|----------------|-------|---------------|-------|
| Free | ~1,000 | Basic | Shared library read | Free |
| Pro | 100,000 | All modes | Full BRAID read/write | Coming soon |
| Enterprise | Unlimited | All modes + custom agents | Private BRAID namespace | Coming soon |
| ONODE (self-hosted) | Unlimited | Full | Local + global BRAID | See below |

Plans are tied to your OASIS avatar and governed by the karma system — higher karma unlocks better routing and more capable models.

---

## 14. Observability

WEB6 ships three complementary observability layers.

### Prometheus metrics — `GET /metrics`

Standard Prometheus scrape endpoint. Pull-based; configure your Prometheus, Grafana Agent, or Datadog Agent to scrape it.

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'web6'
    static_configs:
      - targets: ['api.web6.oasisomniverse.one']
    metrics_path: /metrics
    scheme: https
    bearer_token: <your-oasis-avatar-key>
```

Available metrics:

| Metric | Type | Labels | Description |
|--------|------|--------|-------------|
| `web6_completion_requests_total` | Counter | `provider`, `model`, `cached` | All completion requests |
| `web6_fahrn_dispatches_total` | Counter | `mode` | FAHRN reasoning dispatches |
| `web6_braid_graph_reuses_total` | Counter | — | Holonic BRAID plan reuses |
| `web6_cache_hits_total` | Counter | — | Semantic cache hits (zero cost) |
| `web6_prompt_tokens` | Histogram | `provider`, `model` | Prompt token distribution |
| `web6_completion_tokens` | Histogram | `provider`, `model` | Completion token distribution |
| `web6_request_latency_milliseconds` | Histogram | `provider`, `model` | End-to-end latency |
| `web6_estimated_cost_usd` | Histogram | `provider`, `model` | USD cost per request |
| `web6_errors_total` | Counter | `provider` | Request errors |

### OpenTelemetry distributed tracing

WEB6 exports OpenTelemetry spans via OTLP. Every HTTP request becomes a trace span with full request attributes. Compatible with **Jaeger**, **Grafana Tempo**, **Datadog**, **Honeycomb**, **Dynatrace**, **New Relic**.

Set one environment variable on your ONODE to enable:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://your-collector:4317
```

No other configuration needed. The service name in traces is `oasis-web6`.

### Real-time event stream (SSE) — `GET /v1/telemetry/stream`

Server-Sent Events stream of per-request trace events for live dashboards and debugging:

```bash
# Real-time SSE stream of telemetry events
GET https://api.web6.oasisomniverse.one/v1/telemetry/stream
Authorization: Bearer <your-oasis-avatar-key>

# Last N events (default 50, max 500)
GET https://api.web6.oasisomniverse.one/v1/telemetry/history?limit=50
Authorization: Bearer <your-oasis-avatar-key>
```

Each event is a JSON object containing: `provider`, `model`, `latencyMs`, `promptTokens`, `completionTokens`, `estimatedCostUSD`, `fahrnMode`, `braidGraphReused`, `cacheHit`, `loopDetected`, `avatarId`, `timestamp`.

---

## 15. Authentication — Getting Your API Key

Every WEB6 request requires a Bearer token tied to your OASIS avatar.

1. Register at https://oportal.oasisomniverse.one (free — no credit card required)
2. Your JWT is returned on login — use it as `Authorization: Bearer <token>` on every request
3. For a self-hosted ONODE: use `POST /api/Avatar/Authenticate` on your ONODE to get a JWT, then use it against the WEB6 endpoints on the same host

```bash
# Register
curl -X POST https://api.web4.oasisomniverse.one/api/Avatar/Register \
  -H "Content-Type: application/json" \
  -d '{ "username": "yourname", "email": "you@example.com", "password": "..." }'

# Authenticate — returns jwtToken
curl -X POST https://api.web4.oasisomniverse.one/api/Avatar/Authenticate \
  -H "Content-Type: application/json" \
  -d '{ "username": "yourname", "password": "..." }'
```

Use the returned `jwtToken` as your Bearer token on all WEB6 calls.

---

## 16. Migrating from an Existing AI Setup

If you're already calling an AI provider directly (OpenAI, Anthropic, AWS Bedrock, Azure OpenAI, etc.), you don't need to rewrite your code. WEB6 is designed as a drop-in.

### Phase 1 — Endpoint swap (1–2 hours)

Replace your provider endpoint with WEB6's. If you use OpenAI's SDK or LangChain/AutoGen/CrewAI/Semantic Kernel, change only your base URL and API key — the OpenAI-compatible shim handles the rest.

```python
# Before — direct Anthropic
response = anthropic.messages.create(model="claude-sonnet-4-6", messages=[...])

# After — WEB6 (same model, adds BRAID + FAHRN + failover automatically)
response = httpx.post(
    "https://api.web6.oasisomniverse.one/v1/complete",
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"},
    json={"provider": "anthropic", "model": "claude-sonnet-4-6", "messages": [...]}
)
```

### Phase 2 — Enable BRAID and caching (~5 minutes)

Add `"UseHolonicBraid": true` and `"UseFAHRN": true` to your completion requests — two JSON fields, nothing else changes. WEB6 handles the Generator/Solver split and caches reasoning plans automatically.

### Phase 3 — Migrate document storage to Holonic Memory (hours, depending on corpus size)

Replace your vector store or knowledge base with Holonic Memory using `POST /v1/holonic-memory/holons/{id}/documents`. Replace retrieval calls with `GET /v1/holonic-memory/holons/{id}/memory/search`.

### Phase 4 — Session memory (optional, ~30 minutes)

Create a Session-level holon per conversation and post transcripts as memory items. Future sessions query past context via the search endpoint — no per-query retrieval fees.

---

## 17. Why WEB6 Reduces Costs

These savings are cumulative. A typical AI workload that applies all four mechanisms can achieve 60–80% total cost reduction without any loss in response quality.

| Mechanism | Typical Saving | When It Applies |
|-----------|---------------|-----------------|
| Remove provider middleman overhead | 15–20% | All requests (especially AWS Bedrock, Azure) |
| Semantic caching (95%+ similarity = free) | 30–70% | Repeated / similar queries |
| BRAID Generator/Solver split | Up to 74× PPD | Repeated reasoning patterns |
| FAHRN cheap-model routing | 60–85% on eligible tasks | Classification, tagging, summarisation |

FAHRN's composite scoring formula routes to the cheapest model that can do the job in `cost` priority mode — e.g. Groq (Llama 3) for intent classification instead of Claude, saving 80%+ on those calls while reserving Claude for tasks where it's genuinely better.

---

## 18. Holonic Memory vs Flat Vector Stores

If you're using a vector database (Pinecone, Weaviate, pgvector) or a hosted knowledge base (AWS Bedrock KB, Azure AI Search), Holonic Memory provides several structural advantages:

| Dimension | Flat vector store | Holonic Memory (WEB6) |
|-----------|------------------|----------------------|
| Duplicate content | Stored N times per document | Stored once, linked N times via semantic dedup |
| Retrieval unit | Raw embedding comparison | Hierarchical — query hits the right level, not every chunk |
| Storage backend | Locked to one vendor | 40+ providers via COSMIC ORM; switch without data migration |
| Retention control | Manual TTL / lifecycle rules | Built-in per-holon TTL (`SessionOnly`, `Persistent`, `Expiring`) |
| Session notes | Manual indexing | Auto-holonised, semantic search included |
| Portability | Vendor lock-in | Open, portable, self-hostable |

The deduplication benefit compounds with document count: 20 documents sharing a common paragraph store that paragraph as **one holon** — not 20 copies. Embeddings are computed once per unique chunk, not per document.

### Example: storing session transcripts with auto-expiry

```python
# Create a session holon (tied to a user's agent holon)
session = httpx.post(
    "https://api.web6.oasisomniverse.one/v1/holonic-memory/holons",
    params={"level": "Session", "name": f"session-{session_id}", "parentHolonId": user_holon_id},
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"}
).json()["result"]

# Store transcript with 1-year TTL — auto-expires without manual cleanup
httpx.post(
    f"https://api.web6.oasisomniverse.one/v1/holonic-memory/holons/{session['id']}/memory",
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"},
    json={
        "fieldName": "transcript",
        "value": session_transcript,
        "tags": ["session-note"],
        "retentionPolicy": "Expiring",
        "expiresUtc": "2027-08-14T00:00:00Z"
    }
)
```

---

## 19. Hosted API vs Self-Hosted ONODE

### Hosted (recommended to start)

`https://api.web6.oasisomniverse.one`

- Free plan: ~1,000 requests/month, basic routing, shared BRAID library
- Pro plan (coming soon): 100,000 requests/month, full FAHRN, full BRAID
- Enterprise plan (coming soon): unlimited, private BRAID namespace, SLA, dedicated support
- No infrastructure to manage; your data governed by OASIS membrane rules

### Self-hosted ONODE (for data sovereignty or enterprise scale)

If you need data to remain within your own infrastructure (GDPR, healthcare, enterprise compliance):

```bash
git clone https://github.com/NextGenSoftwareUK/OASIS
cd OASIS/WEB6/NextGenSoftware.OASIS.Web6.WebAPI
# Edit OASIS_DNA.json — add your API keys, point storage at your MongoDB
dotnet run
```

Your ONODE runs at `http://localhost:5000`. All data stays on your servers. Configure your existing MongoDB/PostgreSQL for holon storage and your own AI provider keys.

See [Section 12](#12-spinning-up-your-own-onode) for the full ONODE configuration reference.

---

## Further Reading

| Resource | Description |
|----------|-------------|
| [WEB6 REST API Reference](WEB6_REST_API_Reference.md) | Full endpoint docs — request/response shapes, auth, gRPC and GraphQL |
| [WEB6 MCP Tool Reference](WEB6_MCP_Tool_Reference.md) | All 250 MCP tools — parameters and return values |
| [WEB6 User Guide](WEB6_User_Guide.md) | Common workflows, environment setup, recipes |
| [WEB6 for Leela AI](WEB6-Leela-AI-Integration-Guide.md) | Cost reduction playbook — Bedrock swap, BRAID/caching, holonic document storage |
| [Holonic Braid Whitepaper](https://web6.oasisomniverse.one/holonic-braid-whitepaper.html) | Deep technical detail on BRAID, FAHRN, holonic memory |
| [Swagger / Interactive API](https://api.web6.oasisomniverse.one/swagger) | Try every endpoint live |
| [GitHub](https://github.com/NextGenSoftwareUK/OASIS) | Source code, issues, discussions |
