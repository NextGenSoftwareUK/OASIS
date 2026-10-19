# WEB6 for Leela AI — Cutting Costs Without Changing Your Stack

This guide is written specifically for the Leela AI development team. It addresses two concrete cost problems you are currently facing and shows exactly how WEB6 can solve them, with minimal changes to your existing UI and agentic flows.

> **Comparing WEB6 with a model router (OpenRouter) or Hermes Agent?** See [Why WEB6 beats a model router or Hermes Agent for Leela](#why-web6-beats-a-model-router-or-hermes-agent-for-leela). In short, WEB6 is the only one of the three that cuts both your model bill and your document-storage bill, and it gives you a multi-user agent runtime, client chat channels and per-client billing in the same platform.

---

## Your Two Problems

### Problem 1 — Model Costs (Claude Sonnet via AWS Bedrock)

You are paying AWS Bedrock's mark-up on Claude Sonnet for every inference request. The cost has two components: the model token cost plus Bedrock's per-request overhead and data transfer fees. At scale, Bedrock adds meaningful overhead over direct provider pricing.

### Problem 2 — Document Storage Costs (Bedrock Knowledge Bases / S3)

To feed documents into your AI model, you are storing them in AWS (S3 + Bedrock Knowledge Bases), paying for:
- Raw S3 storage of every document
- Bedrock Knowledge Base indexing and query fees
- Vector embedding costs per document chunk
- Data transfer between Bedrock services

At scale, a large document corpus on Bedrock becomes one of the biggest line items on the bill.

WEB6 addresses both problems directly.

---

## Problem 1: Cutting Model Costs with WEB6

### Option A — Direct API Swap (5 minutes)

The simplest change: replace your Bedrock endpoint with the WEB6 endpoint. WEB6 routes to Claude Sonnet directly (or via OpenServ's SERV gateway) without Bedrock's overhead.

**Before (AWS Bedrock):**
```python
import boto3

client = boto3.client('bedrock-runtime', region_name='eu-west-1')
response = client.invoke_model(
    modelId='anthropic.claude-sonnet-4-6-v1:0',
    body=json.dumps({ "messages": [...] })
)
```

**After (WEB6 — same model, no Bedrock fee):**
```python
import httpx

response = httpx.post(
    "https://api.web6.oasisomniverse.one/v1/complete",
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"},
    json={
        "provider": "anthropic",
        "model": "claude-sonnet-4-6",
        "messages": [...],
        "routing": { "priority": "cost", "fallback": true }
    }
)
```

No SDK changes, no LangChain refactoring. Same Claude Sonnet responses, no Bedrock middleman.

**If you use LangChain, AutoGen or CrewAI today:**

WEB6 exposes a fully OpenAI-compatible `POST /v1/chat/completions` endpoint. Change only your base URL and API key — no other code changes needed.

```python
# LangChain
from langchain_openai import ChatOpenAI

llm = ChatOpenAI(
    openai_api_base="https://api.web6.oasisomniverse.one/v1",
    openai_api_key="<your-oasis-avatar-key>",
    model="claude-sonnet-4-6"   # or "auto" to let WEB6 pick cheapest
)

# AutoGen
import autogen

llm_config = {"config_list": [{
    "model": "claude-sonnet-4-6",
    "api_key": "<your-oasis-avatar-key>",
    "base_url": "https://api.web6.oasisomniverse.one/v1",
}]}

# CrewAI
from crewai import LLM
llm = LLM(
    model="openai/claude-sonnet-4-6",
    base_url="https://api.web6.oasisomniverse.one/v1",
    api_key="<your-oasis-avatar-key>"
)
```

### Option B — Holonic BRAID: 74× Cost Reduction on Repeated Reasoning

If your use case involves reasoning over similar types of queries repeatedly (which most AI therapy / conversation platforms do), BRAID gives you dramatic savings.

**How it works for Leela:**

1. First time a query type arrives (e.g. "user expressing anxiety about X"), the BRAID **Generator** uses a high-tier model to produce a structured Mermaid reasoning graph for that query category — once, stored as a holon.
2. Every future query of the same type, the BRAID **Solver** uses a cheap, fast model (e.g. GPT-5 Nano) to execute the pre-built graph against the specific instance.
3. The expensive Generator call is amortised across thousands of similar queries.

Result: **74× performance-per-dollar** on benchmark tasks (arXiv:2512.15959), with equal or better accuracy.

```python
response = httpx.post(
    "https://api.web6.oasisomniverse.one/v1/complete",
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"},
    json={
        "provider": "auto",
        "model": "auto",
        "messages": [
            { "role": "system", "content": "You are a Leela AI therapy assistant." },
            { "role": "user", "content": user_message }
        ],
        "UseHolonicBraid": True,  # enables Generator/Solver split
        "UseFAHRN": True,
        "routing": { "priority": "cost", "fallback": True }
    }
)
```

The first call in a new query category will be slightly slower (Generator runs). Every subsequent call of the same category returns at solver cost — which is an order of magnitude cheaper.

### Option C — Semantic Caching (Near-Zero Cost for Repeated Queries)

WEB6 has embedding-based semantic caching built in. If two requests are ≥95% similar in meaning, the cached response is returned instantly at **zero provider cost**.

For Leela, this is particularly valuable for:
- Opening/check-in messages ("How are you feeling today?")
- Standard therapeutic prompts that appear frequently
- Classification steps that run on the same input

Semantic caching is on by default on the hosted API — no configuration needed.

### Option D — FAHRN Cost-Optimal Routing

For tasks that don't need Claude specifically (e.g. intent classification, document summarisation, session tagging), FAHRN routes to the cheapest model that can do the job:

```python
response = httpx.post(
    "https://api.web6.oasisomniverse.one/v1/complete",
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"},
    json={
        "provider": "auto",
        "model": "auto",
        "messages": [{ "role": "user", "content": "Classify the emotional tone of: " + text }],
        "routing": { "priority": "cost", "fallback": True },
        "UseFAHRN": True
    }
)
```

FAHRN's composite scoring formula weighs cost heavily in `cost` priority mode. For a classification task, it will route to Groq (Llama 3, ultra-fast and cheap) or DeepSeek rather than Claude — saving 80%+ on those calls while reserving Claude for the tasks it's genuinely better at.

### Combined Savings Estimate

| Mechanism | Saving | When It Applies |
|-----------|--------|-----------------|
| Remove Bedrock overhead | ~15–20% | All requests |
| Semantic caching | 30–70% | Repeated/similar queries |
| BRAID Generator/Solver split | Up to 74× PPD | Repeated reasoning patterns |
| FAHRN cheap-model routing | 60–85% on eligible tasks | Classification, tagging, summarisation |

A typical Leela workload combining all four could see **60–80% total model cost reduction** without any loss in response quality for end users.

---

## Problem 2: Document Storage Costs with Holonic Memory

### Why Bedrock Knowledge Bases Are Expensive

AWS Bedrock Knowledge Bases store your documents in S3, re-embed them at indexing time, and charge per query against the vector index. You pay:
- Storage for every version of every document
- Embedding tokens when documents are ingested or updated
- Retrieval fees per query
- Data transfer between Lambda → Bedrock → S3

For a growing therapy platform with session notes, intake forms, research papers and protocol documents, this compounds quickly.

### How Holonic Memory Changes the Model

Instead of a flat vector store, Holonic Memory decomposes every document into a **fractal holon hierarchy**:

```
Document Corpus Holon
└── Document Holon (e.g. "CBT Protocol v3")
    ├── Section Holon ("Cognitive Restructuring")
    │   ├── Paragraph Holon
    │   └── Paragraph Holon
    └── Section Holon ("Behavioural Activation")
        ├── Paragraph Holon
        └── Paragraph Holon
```

**Why this saves money:**

1. **Semantic deduplication.** The ingest pipeline embeds every chunk and compares it against all existing items before storing. Content shared across documents (e.g. the same evidence summary cited in 20 protocols) is stored as **one holon** — subsequent uploads that hit the same chunk skip storage and return the existing fieldName. With a flat vector store, you pay for every duplicate chunk.

2. **Hierarchical retrieval.** A query about "CBT techniques" hits the Section Holon level — not every paragraph. Fewer embedding comparisons, lower retrieval cost.

3. **Configurable storage backends via COSMIC ORM.** Holons can be stored on MongoDB (cheap, fast), IPFS (free, decentralised), Solana (immutable record, low cost), or any of 222 providers. You choose the cheapest combination that meets your latency and durability requirements. You are not locked to S3.

4. **TTL retention policies.** Session holons can auto-expire after a set period. You only pay long-term storage for the content that actually needs it.

5. **Embedding-based retrieval.** Each memory item stores a pre-computed embedding vector alongside its raw content. Semantic search compares embeddings (cosine similarity) rather than scanning raw text, and only the top-K matching items' content is returned — limiting how much text is fed into the context window.

### Migrating Documents to Holonic Memory

The Holonic Memory API is under `/v1/holonic-memory/`. Documents and session notes are stored as **memory items** inside holons. The typical flow is: create a holon for the document corpus, then record each document chunk as a memory item in that holon.

#### Step 1 — Create a corpus holon

```python
# Create a holon to hold all Leela therapy protocol documents
response = httpx.post(
    "https://api.web6.oasisomniverse.one/v1/holonic-memory/holons",
    params={
        "level": "Agent",       # HolonicMemoryLevel enum
        "name": "Leela-Protocols",
        "parentHolonId": "00000000-0000-0000-0000-000000000000"  # attach to user/org holon
    },
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"}
)
corpus_holon_id = response.json()["result"]["id"]
```

#### Step 2 — Ingest documents (auto-chunked, semantically deduplicated)

Use the bulk document endpoint — pass the full document text and WEB6 handles chunking and deduplication automatically:

1. Splits the document with a sliding-window chunker (default 400 tokens/chunk, 50 token overlap)
2. Embeds each chunk using the configured embedding provider
3. Compares each chunk embedding against all existing items in the holon (cosine similarity ≥ 0.98 = duplicate)
4. Stores only genuinely new chunks; skips duplicates and records the existing fieldName instead

This means if 20 of your CBT protocols cite the same evidence paragraph, it is stored as **one holon** — not 20 copies.

```python
# Ingest a full document in one call — WEB6 chunks, embeds, deduplicates, stores
response = httpx.post(
    f"https://api.web6.oasisomniverse.one/v1/holonic-memory/holons/{corpus_holon_id}/documents",
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"},
    json={
        "content": full_document_text,          # the entire raw document
        "documentName": "cbt-protocol-v3",      # prefix used in chunk fieldNames
        "chunkTokens": 400,                     # ~1600 chars per chunk
        "overlapTokens": 50,                    # overlap to preserve boundary context
        "tags": ["protocol", "CBT", "cognitive-restructuring"],
        "retentionPolicy": "Persistent"
    }
)

ingest = response.json()["result"]
print(f"{ingest['storedChunks']} new chunks stored, "
      f"{ingest['deduplicatedChunks']} duplicates skipped, "
      f"{ingest['totalChunks']} total")
```

If you already have pre-chunked data, use `POST /v1/holonic-memory/holons/{id}/memory` directly with `fieldName` + `value` per chunk.

#### Step 3 — Semantic search across the corpus

```python
results = httpx.get(
    f"https://api.web6.oasisomniverse.one/v1/holonic-memory/holons/{corpus_holon_id}/memory/semantic-search",
    params={
        "q": "How should a therapist respond to a client expressing hopelessness?",
        "topK": 5
    },
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"}
)

# Inject top results into your LLM call as context — same as you do with Bedrock KB
context_chunks = [r["content"] for r in results.json()["result"]]
```

#### Step 4 — Store session notes as memory items

```python
# After a Leela session ends, create a session-level holon and record the transcript
session_response = httpx.post(
    "https://api.web6.oasisomniverse.one/v1/holonic-memory/holons",
    params={
        "level": "Session",
        "name": f"leela-session-{session_id}",
        "parentHolonId": user_agent_holon_id   # ties to user's agent holon
    },
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"}
)
session_holon_id = session_response.json()["result"]["id"]

httpx.post(
    f"https://api.web6.oasisomniverse.one/v1/holonic-memory/holons/{session_holon_id}/memory",
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"},
    json={
        "fieldName": "transcript",
        "value": session_transcript,
        "tags": ["session-note", "therapy"],
        "retentionPolicy": "TimeLimited",
        "expiresUtc": "2027-08-14T00:00:00Z"   # auto-expire after 1 year
    }
)
```

Future sessions can query past holons semantically — without paying per-query Bedrock fees.

### Storage Cost Comparison

| Item | AWS Bedrock KB | Holonic Memory (WEB6) |
|------|---------------|----------------------|
| Document storage | S3 pricing + indexing fee | MongoDB/IPFS — much cheaper |
| Per-query retrieval | Bedrock KB query fee | Included in WEB6 plan |
| Duplicate content | Stored N times | Stored once, linked N times |
| Session transcripts | S3 + manual indexing | Auto-holonised, semantic search included |
| Retention control | Manual S3 lifecycle rules | Built-in TTL per holon |
| Portability | AWS lock-in | 222 providers, switch anytime |

---

## Minimal Integration Path for the Leela UI

You do not need to rewrite your UI or agentic flow. The changes are all at the API call level.

### Phase 1 — Drop-in endpoint swap (~5 minutes)

Replace your Bedrock `invoke_model` calls with WEB6 `/v1/complete`. No frontend changes. Immediate Bedrock overhead savings.

### Phase 2 — Enable BRAID and caching (~5 minutes)

Add `"UseHolonicBraid": true` and `"UseFAHRN": true` to your completion requests — two JSON fields, nothing else changes. WEB6 handles the Generator/Solver split and caches reasoning plans automatically. No change to prompt structure or response parsing.

### Phase 3 — Migrate document corpus (hours, depending on corpus size)

Run a one-time script to ingest your existing Bedrock Knowledge Base documents into Holonic Memory using `POST /v1/holonic-memory/holons` + `POST /v1/holonic-memory/holons/{id}/memory`. Replace `retrieve_and_generate` calls with `GET /v1/holonic-memory/holons/{id}/memory/search` + `POST /v1/complete`.

### Phase 4 — Session memory (optional, ~30 minutes)

Create a Session-level holon per session via `POST /v1/holonic-memory/holons` and post transcripts as memory items to `POST /v1/holonic-memory/holons/{id}/memory`. Future sessions can query past context via the search endpoint.

---

## Using the Hosted API vs. Running Your Own ONODE

### Hosted (recommended to start)

`https://api.web6.oasisomniverse.one`

- Free ($0): 1,000 requests/month, 20 calls/day
- Bronze ($9), Silver ($29), Gold ($99): 10,000 / 100,000 / 1,000,000 requests/month with higher daily call, token and budget limits
- Enterprise (custom): unlimited, SLA, dedicated support
- Plans are enforced by WEB4 — see [Quotas & Tiers](WEB6_Quotas_and_Tiers.md)
- No infrastructure to manage
- Your data is governed by OASIS membrane rules — you control what is retained

### Self-hosted ONODE (for data sovereignty or enterprise scale)

If Leela needs data to remain entirely within your own infrastructure (e.g. GDPR, NHS data requirements):

```bash
git clone https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-HoloAGI
cd WEB6/NextGenSoftware.OASIS.Web6.WebAPI
# Edit OASIS_DNA.json — add your API keys, point storage at your MongoDB
dotnet run
```

Your ONODE runs at `http://localhost:5000` (or your internal URL). All data stays on your servers. You can configure it to use your existing MongoDB/PostgreSQL for holon storage and your own AI provider keys.

OASISDNA settings for a Leela ONODE:

```json
{
  "OASIS": {
    "Web6": {
      "DefaultProvider": "anthropic",
      "DefaultRoutingPriority": "cost",
      "EnableFAHRN": true,
      "EnableHolonicBraid": true,
      "FAHRN": {
        "DefaultDispatchMode": "Serial",
        "EMAAlpha": 0.2
      },
      "HolonicBraid": {
        "AutoPersistWinningPlan": true
      },
      "HolonicMemory": {
        "DefaultRetentionPolicy": "365d",
        "RecordDispatchOutcomes": true
      },
      "ApiKeys": {
        "Anthropic": "<your-anthropic-key>",
        "OpenAI": "<your-openai-key-for-fallback>"
      }
    }
  }
}
```

---

## Authentication

Get your OASIS avatar key:

1. Register at https://oportal.oasisomniverse.one (free)
2. Your JWT is returned on login — use it as `Bearer <token>` on every WEB6 request
3. For self-hosted ONODE: use the WEB4 avatar login endpoint (`POST /api/Avatar/Authenticate`) on your ONODE to obtain a JWT, then use that token against the WEB6 endpoints on the same host

The existing Leela API key (`sk_leela_...`) stored in the OASISDNA is already configured if you are running a WEB6 ONODE locally from the OASIS repo.

---

## Summary

| Goal | WEB6 Solution | Effort |
|------|--------------|--------|
| Remove Bedrock overhead on Sonnet | Change endpoint URL | 1 hour |
| Reduce cost on repeated reasoning | Enable Holonic BRAID | ~5 min |
| Near-zero cost on repeated queries | Semantic caching (on by default) | Zero |
| Cheap routing for non-Sonnet tasks | FAHRN cost mode | ~5 min |
| Replace Bedrock document storage | Holonic Memory ingestion | Hours (corpus-size dependent) |
| Full data sovereignty | Self-hosted ONODE | ~1–2 hours (Docker or binary) |
| Server-side agent with self-improving skills | `POST /v1/agent/run` + `/v1/agent/feedback` | ~1 hour |
| Clients chat with Leela on WhatsApp / Telegram / Slack / Discord | `/v1/channels/*` + a per-client WEB4 grant | ~1 hour per channel (bot setup) |
| Daily check-ins and reminders | `/v1/schedules` with channel delivery | ~15 min |

Start with Phase 1 (endpoint swap) this week to capture immediate savings, then evaluate BRAID and Holonic Memory based on your actual usage patterns.

---

## Why WEB6 beats a model router or Hermes Agent for Leela

You may be weighing two alternatives: a **model router** such as [OpenRouter](https://openrouter.ai), or an **agent framework** such as [Hermes Agent](https://github.com/NousResearch/hermes-agent) (Nous Research). Each solves one slice of Leela's problem. **WEB6 solves all of it in one platform**, and it's built for a multi-user product handling sensitive conversations, not a single developer's chatbot.

| | Model router (e.g. OpenRouter) | Hermes Agent | **WEB6** |
|---|---|---|---|
| What it is | Pay-per-token gateway to many models | Self-hosted, single-user agent app | **Full AI platform: gateway, agent runtime, memory, reasoning reuse, billing, identity** |
| Cuts Leela's Bedrock model bill | Partly: one API, but you still pay full token price for every call, plus the router's platform fee | No: it calls whichever model you configure, at full price, and adds its own memory-saving and skill-writing calls | **Yes: cost-first routing across 98 providers, semantic caching (repeat questions cost nothing), Holonic BRAID reasoning reuse, FAHRN cheap-model routing** |
| Replaces Bedrock Knowledge Bases / S3 | No | No: per-agent local SQLite recall, not a document corpus | **Yes: Holonic Memory ingests, chunks, deduplicates and semantically searches your whole corpus** |
| Multi-user (each client separate) | No: one account and key | No: designed for one person | **Yes: every client is a WEB4 avatar with their own memory, budgets, metering and audit trail** |
| Privacy controls for therapy data | None beyond the provider's | Memory sits in local files on whatever box runs it | **Per-avatar holons, membrane rules (nothing is shared upward unless allowed), TTL retention, self-hostable ONODE for full data sovereignty** |
| Agent loop | No | Yes | **Yes: `POST /v1/agent/run`, metered per step, with pause-for-client-tools** |
| Self-improving skills | No | Yes: writes skills from tasks | **Yes: SkillOpt evolves each agent's skill per task category from scored runs (`/v1/agent/feedback`), and only keeps edits that measurably improve results** |
| Long-term memory | No | One agent's own notes | **Holonic Memory hierarchy plus Mem0, Zep, Letta, LangMem, Graphiti, Qdrant and Weaviate adapters; the agent curates durable facts automatically** |
| Shared reasoning | No | No | **Holonic BRAID: proven reasoning plans are reused across sessions and users** |
| Multi-agent orchestration | No | Parallel subagents | **FAHRN: serial, parallel, debate, voting and decomposed dispatch with budget guards and loop detection** |
| WhatsApp, Telegram, Slack, Discord | No | Yes | **Yes: signed webhooks, one-time avatar linking, per-chat conversation memory, voice notes transcribed and answered, every reply billed to that client's own capped grant** |
| Scheduled / unattended runs | No | Built-in cron, unbilled and single-user | **Cron schedules per avatar, run exactly once across servers, each under a revocable, budget-capped grant, with results to a webhook or the client's chat** |
| Spending control | Account-level credit balance | None | **Per-user plans, daily and monthly limits, per-grant monthly and per-operation caps, reserve-then-settle accounting on every call** |
| Tools and interop | No | MCP client | **508 MCP tools at `/mcp`, plus A2A, ACP, ANP, LangGraph, OpenAI Agents, gRPC, GraphQL and webhooks** |
| Drop-in for existing code | OpenAI-compatible API | You adopt its runtime | **OpenAI-compatible `/v1/chat/completions`: LangChain, AutoGen and CrewAI work by changing the base URL** |

### What this means for Leela

1. **One platform instead of three.** A router would fix only model access, and Hermes only the agent loop. You'd still need a vector store, a multi-tenant identity layer, per-client billing and a privacy model. WEB6 ships all of them, already integrated.
2. **The cost savings compound.** The router saves on model access. WEB6 also gives you near-zero cost for repeated questions (semantic cache), cheaper reasoning on recurring patterns (BRAID), cheap models for easy steps (FAHRN), and it removes the Bedrock Knowledge Base bill entirely (Holonic Memory).
3. **Built for a therapy product.** Every client's conversations, memories and spend are isolated by avatar. Nothing propagates without a membrane rule, retention is enforced, and you can self-host the whole stack.
4. **Clients reach Leela where they already are.** WhatsApp, Telegram, Slack and Discord work today. Each chat remembers its thread, can draw on that client's own memory, and is billed to that client's capped grant, so one heavy user can never run up a surprise bill.
5. **It gets better on its own.** SkillOpt turns your clinicians' ratings of answers into better agent skills, and BRAID makes good reasoning reusable.
6. **No lock-in.** It's OpenAI-compatible, MCP-native, has 98 providers and is self-hostable. You can change models at any time without touching your code.

### Already using a router or Hermes? Keep it, and put WEB6 underneath

- **Router users:** point your OpenAI client at `https://api.web6.oasisomniverse.one/v1` instead. You keep one API for every model and gain caching, BRAID, FAHRN, memory and per-client billing.
- **Hermes users:** set WEB6 as Hermes's custom OpenAI-compatible endpoint (base URL above, API key = your OASIS bearer token) and add `https://api.web6.oasisomniverse.one/mcp` as an MCP server. Every Hermes call then gets WEB6's savings and Leela's Holonic Memory corpus. Keep sensitive client memory in WEB6 holons, not in Hermes's local files.

> Configuring Hermes against WEB6 authentication has not yet been tested end to end. Try it in a non-production environment first.

### Getting Leela's agent onto WEB6 today

| Step | Call |
|---|---|
| Run the agent for a client | `POST /v1/agent/run` with `task`, optional `memoryHolonId` (the client's memory) and `agentHolonId` (your Leela agent's skill) |
| Rate answers to improve the agent | `POST /v1/agent/feedback` with `runId` and a 0–1 score |
| Let a client chat on WhatsApp or Telegram | Client issues a WEB4 grant (`POST /api/subscription/usage/grants`, endpoint `CHANNEL /v1/channels message`), then `POST /v1/channels/link-codes` and sends `link CODE` in the chat |
| Give that chat long-term memory | `PUT /v1/channels/bindings/{id}/memory` |
| Daily check-ins | `POST /v1/schedules` with a cron expression, the task, a grant for `SCHEDULE /v1/schedules run`, and `delivery.kind = "channel"` to post into the client's chat |

**Voice notes.** Clients can send WhatsApp or Telegram voice notes. WEB6 downloads each one, measures its exact length (verifying every Ogg page checksum), transcribes it and answers, all in one billed operation on that client's grant. The reply begins with what WEB6 heard, so a mis-hearing is obvious. Your operator turns this on once by publishing your transcription rate (an `audio_ms` unit price) in the WEB6 pricing catalogue. Until then, voice notes get a polite "please send text" reply and nothing is charged.

---

## Monitoring Costs with Prometheus + Grafana

WEB6 exposes a Prometheus metrics endpoint at `GET /metrics`. Scrape it to build real-time cost dashboards in Grafana.

Key metrics for Leela cost tracking:

```promql
# USD spent per provider in the last hour
increase(web6_estimated_cost_usd_sum[1h])

# Cache hit rate (% of requests served at zero cost)
rate(web6_cache_hits_total[5m]) / rate(web6_completion_requests_total[5m])

# Token usage by provider
rate(web6_prompt_tokens_sum[5m])

# P95 latency
histogram_quantile(0.95, rate(web6_request_latency_milliseconds_bucket[5m]))
```

For distributed tracing (full request traces in Jaeger/Grafana Tempo):

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://your-collector:4317
```

---

**Contact / support:** https://web6.oasisomniverse.one | Discord | david@oasisomniverse.one
