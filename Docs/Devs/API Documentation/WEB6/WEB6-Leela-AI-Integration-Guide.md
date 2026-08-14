# WEB6 for Leela AI — Cutting Costs Without Changing Your Stack

This guide is written specifically for the Leela AI development team. It addresses two concrete cost problems you are currently facing and shows exactly how WEB6 can solve them, with minimal changes to your existing UI and agentic flows.

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

**If you use LangChain today:**

```python
from langchain_openai import ChatOpenAI

# Replace your Bedrock LLM with this — same interface, no other code changes
llm = ChatOpenAI(
    openai_api_base="https://api.web6.oasisomniverse.one/v1",
    openai_api_key="<your-oasis-avatar-key>",
    model="claude-sonnet-4-6"   # or "auto" to let WEB6 pick
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

1. **Semantic deduplication.** Content shared across documents (e.g. the same evidence summary cited in 20 protocols) is stored as one holon, linked from many parents. You pay storage once. With a flat vector store, you pay for every duplicate chunk.

2. **Hierarchical retrieval.** A query about "CBT techniques" hits the Section Holon level — not every paragraph. Fewer embedding comparisons, lower retrieval cost.

3. **Configurable storage backends via COSMIC ORM.** Holons can be stored on MongoDB (cheap, fast), IPFS (free, decentralised), Solana (immutable record, low cost), or any of 40+ providers. You choose the cheapest combination that meets your latency and durability requirements. You are not locked to S3.

4. **TTL retention policies.** Session holons can auto-expire after a set period. You only pay long-term storage for the content that actually needs it.

5. **Compression at the holon level.** Holons store a summary embedding alongside the raw content. For RAG retrieval, you can search against the lightweight summary embedding first, then only fetch the full content of the top-K matches — drastically reducing the amount of raw text retrieved and fed into the context window.

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

#### Step 2 — Store document chunks as memory items

```python
# For each chunk of a document:
httpx.post(
    f"https://api.web6.oasisomniverse.one/v1/holonic-memory/holons/{corpus_holon_id}/memory",
    headers={"Authorization": "Bearer <your-oasis-avatar-key>"},
    json={
        "key": "cbt-protocol-v3-chunk-001",
        "content": chunk_text,
        "tags": ["protocol", "CBT", "cognitive-restructuring"],
        "ttlSeconds": 0   # 0 = permanent
    }
)
```

#### Step 3 — Semantic search across the corpus

```python
results = httpx.get(
    f"https://api.web6.oasisomniverse.one/v1/holonic-memory/holons/{corpus_holon_id}/memory/search",
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
        "key": "transcript",
        "content": session_transcript,
        "tags": ["session-note", "therapy"],
        "ttlSeconds": 31536000   # auto-expire after 1 year
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
| Portability | AWS lock-in | 40+ providers, switch anytime |

---

## Minimal Integration Path for the Leela UI

You do not need to rewrite your UI or agentic flow. The changes are all at the API call level.

### Phase 1 — Drop-in endpoint swap (1 day)

Replace your Bedrock `invoke_model` calls with WEB6 `/v1/complete`. No frontend changes. Immediate Bedrock overhead savings.

### Phase 2 — Enable BRAID and caching (1 day)

Add `"UseHolonicBraid": true` and `"UseFAHRN": true` to your completion requests. WEB6 handles the rest. No change to prompt structure or response parsing.

### Phase 3 — Migrate document corpus (1–2 days)

Run a one-time script to ingest your existing Bedrock Knowledge Base documents into Holonic Memory. Replace `retrieve_and_generate` calls with WEB6 `/v1/holons/search` + `/v1/complete`.

### Phase 4 — Session memory (optional, 1 day)

Post session transcripts to `/v1/holons/sessions` instead of S3. Future sessions automatically have access to past context.

---

## Using the Hosted API vs. Running Your Own ONODE

### Hosted (recommended to start)

`https://api.web6.oasisomniverse.one`

- Free plan: ~1,000 requests/month, basic routing, shared BRAID library
- Pro plan (coming soon): 100,000 requests/month, full FAHRN, full BRAID
- Enterprise plan (coming soon): unlimited, private BRAID namespace, SLA, dedicated support
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
2. Your avatar key is returned on login — use it as `Bearer <key>` on every request
3. For enterprise/self-hosted: generate keys via `POST /v1/auth/avatar` on your own ONODE

The existing Leela API key (`sk_leela_...`) stored in the OASISDNA is already configured if you are running a WEB6 ONODE locally from the OASIS repo.

---

## Summary

| Goal | WEB6 Solution | Effort |
|------|--------------|--------|
| Remove Bedrock overhead on Sonnet | Change endpoint URL | 1 hour |
| Reduce cost on repeated reasoning | Enable Holonic BRAID | 1 day |
| Near-zero cost on repeated queries | Semantic caching (on by default) | Zero |
| Cheap routing for non-Sonnet tasks | FAHRN cost mode | 1 day |
| Replace Bedrock document storage | Holonic Memory ingestion | 1–2 days |
| Full data sovereignty | Self-hosted ONODE | 1–2 days |

Start with Phase 1 (endpoint swap) this week to capture immediate savings, then evaluate BRAID and Holonic Memory based on your actual usage patterns.

**Contact / support:** https://web6.oasisomniverse.one | Discord | david@oasisomniverse.one
