# OASIS WEB6 — Getting Started Guide

**Hosted API:** `https://api.web6.oasisomniverse.one` (free tier, no credit card required)
**npm:** `@oasisomniverse/web6-api` | **Python:** `oasis-web6`
**Docs site:** https://web6.oasisomniverse.one

---

## What is WEB6?

WEB6 is a unified AI abstraction and orchestration layer. Instead of integrating 15+ AI providers separately, you call one endpoint and WEB6 routes to the right model automatically — by cost, quality or latency — with automatic failover if a provider goes down.

On top of routing, WEB6 adds:

- **FAHRN** — a self-improving multi-agent reasoning network that routes sub-tasks to the best-scoring agent, learns from every outcome, and handles serial, parallel or decomposed dispatch modes
- **Holonic BRAID** — a shared fractal memory graph that stores reasoning plans as holons, re-using them across sessions for up to 74× performance-per-dollar gains
- **Holonic Memory** — a fractal hierarchy (session → agent → user → group → city → Earth) that compounds intelligence across every interaction
- **Semantic caching** — returns cached results for 95%+ similar prompts at zero provider cost
- **MCP server** — every FAHRN agent, memory provider and protocol adapter exposed as an MCP tool, auto-discovered by Claude Code, Cursor and any MCP-compatible host

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

Pass any of these as `"provider"` to pin a specific backend, or use `"auto"` to let WEB6 decide:

| Key | Provider | Models |
|-----|----------|--------|
| `openai` | OpenAI | GPT-5, GPT-4o, o3 |
| `anthropic` | Anthropic | Claude Opus/Sonnet/Haiku |
| `gemini` | Google | Gemini 2.5 Flash/Pro |
| `groq` | Groq | Llama 3 (ultra-fast LPU) |
| `mistral` | Mistral | Mixtral, Large, Codestral |
| `ollama` | Ollama | Any local model |
| `cohere` | Cohere | Command R+ |
| `xai` | xAI | Grok 3/Vision |
| `deepseek` | DeepSeek | R1, V3, Coder |
| `openserv` | OpenServ | GPT-5 · Claude · Gemini · Grok · Qwen · DeepSeek via one SERV key |
| `bedrock` | AWS Bedrock | Titan, Nova, Jurassic |
| `huggingface` | HuggingFace | Open-source / fine-tuned |
| `replicate` | Replicate | Image / Audio / Video |

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

```js
const { result } = await web6.completion.complete({
  Provider: 'auto',
  Model: 'auto',
  Messages: [{ Role: 'user', Content: 'Your problem here' }],
  UseFAHRN: true,
  FAHRNMode: 'Parallel',    // Serial | Parallel | Decomposed | Debate | Voting
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

```bash
# Retrieve avatar context (identity + karma + memory summary) for any system
GET https://api.web6.oasisomniverse.one/v1/context/avatar/{avatarId}
Authorization: Bearer <your-oasis-avatar-key>
```

External memory providers (Mem0, Zep, Letta, LangMem, Redis Vector) plug in alongside Holonic Memory via the same interface.

---

## 6. Embeddings & Semantic Cache

```bash
POST https://api.web6.oasisomniverse.one/v1/embeddings
Authorization: Bearer <your-oasis-avatar-key>

{
  "input": "Your text here",
  "model": "text-embedding-3-small"   // or auto
}
```

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

For persistent multi-turn agent sessions with server-side state:

```js
const session = await web6.sessions.connect({ avatarId: 'your-avatar-id' });

await session.send({ role: 'user', content: 'First message' });
const reply1 = await session.receive();

await session.send({ role: 'user', content: 'Follow-up question' });
const reply2 = await session.receive();

// State and memory survive reconnects — stored as a holon in BRAID
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

WEB6 is a drop-in replacement. Replace your direct provider call with the WEB6 endpoint and gain routing, failover, caching and FAHRN — no other changes needed.

### LangChain

```python
from langchain_openai import ChatOpenAI

llm = ChatOpenAI(
    openai_api_base="https://api.web6.oasisomniverse.one/v1",
    openai_api_key="<your-oasis-avatar-key>",
    model="auto"
)
```

### AutoGen

```python
config_list = [{
    "model": "auto",
    "api_key": "<your-oasis-avatar-key>",
    "base_url": "https://api.web6.oasisomniverse.one/v1"
}]
```

### CrewAI

```python
from crewai import LLM

llm = LLM(
    model="openai/auto",
    base_url="https://api.web6.oasisomniverse.one/v1",
    api_key="<your-oasis-avatar-key>"
)
```

### Semantic Kernel (C#)

```csharp
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(
        modelId: "auto",
        endpoint: new Uri("https://api.web6.oasisomniverse.one/v1"),
        apiKey: "<your-oasis-avatar-key>"
    )
    .Build();
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

Every request produces OpenTelemetry traces and Prometheus metrics:

- Provider chosen, model used, latency, token count, cost
- FAHRN dispatch path (which agents were called, their scores)
- BRAID hit/miss (was a cached reasoning plan reused?)
- Per-avatar monthly spend tracking

Real-time telemetry stream:

```bash
GET https://api.web6.oasisomniverse.one/v1/telemetry/stream
Authorization: Bearer <your-oasis-avatar-key>
```

---

## Further Reading

- Whitepaper: https://web6.oasisomniverse.one (READ THE WHITEPAPER button)
- API Reference: https://api.web6.oasisomniverse.one/swagger
- OASIS MCP Server: https://web6.oasisomniverse.one/#mcp
- GitHub: https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-HoloAGI
