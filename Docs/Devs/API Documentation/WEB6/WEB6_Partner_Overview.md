# WEB6 — AI Abstraction Layer Overview
### For Partners & Integrators

---

## What Is WEB6?

WEB6 is OASIS's unified AI gateway — a single API key that gives you access to **98 AI providers and hundreds of models**, with intelligent routing, multi-agent reasoning, and privacy-first options built in.

Think of it like OpenRouter, but with three key differences:
1. **You own your infrastructure** — no third-party middleman, no data leaving your control
2. **It's not just a router** — it actively makes AI smarter through coordination layers (FAHRN, Holonic BRAID, semantic caching, avatar memory)
3. **Uncensored models are first-class** — Venice.ai and OrcaRouter are natively integrated as selectable defaults

---

## How It Works

```
Your App → WEB6 API Key → WEB6 Gateway → Provider (Venice / OrcaRouter / OpenAI / etc.)
```

You call one endpoint. WEB6 routes to whichever provider and model you select (or auto-selects the best one). The underlying provider charges at their standard rate — **WEB6 adds zero markup.**

This is the same model as OpenRouter, except:
- The gateway runs on OASIS infrastructure (your data, your control)
- The reasoning layer adds genuine intelligence, not just routing
- Uncensored providers (Venice.ai, OrcaRouter) are fully supported and can be set as default

---

## Uncensored & Privacy-First Providers

These are now live in WEB6:

### Venice.ai
**Privacy-preserving** (zero logging, no data retention) and **uncensored** — models respond without mainstream AI guardrails.

| Model | Context | Input $/M | Output $/M |
|---|---|---|---|
| `llama-3.3-70b` | 131K | $0.70 | $2.80 |
| `dolphin-2.9.2-qwen2-72b` | 32K | $0.70 | $2.80 |
| `qwen-2.5-qwq-32b` (reasoning) | 32K | $0.50 | $2.00 |

### OrcaRouter.ai
**Uncensored/jailbroken models**, privacy-focused gateway — no refusals, no mainstream restrictions.

| Model | Context | Input $/M | Output $/M |
|---|---|---|---|
| `meta-llama/llama-3.3-70b-instruct` | 131K | $0.59 | $0.79 |

> To make Venice or OrcaRouter Leela's default, one config change is all that's needed. Every request then routes there unless overridden per-call.

> **Verify before committing to tier pricing:** these figures are WEB6's current catalogue values. Confirm against your Venice/OrcaRouter dashboards once the API keys are live, since both providers adjust rates periodically.

---

## Model Access & Costs

WEB6 passes through provider costs at **exactly** what they charge — no markup. You pay only what the underlying model costs.

| Tier | Model | Input $/M | Output $/M |
|---|---|---|---|
| **Free (local)** | Ollama / vLLM / Jan / GPT4All | **$0** | **$0** |
| **Uncensored** | OrcaRouter llama-3.3-70b | $0.59 | $0.79 |
| **Uncensored** | Venice qwen-2.5-qwq-32b | $0.50 | $2.00 |
| **Uncensored** | Venice llama-3.3-70b | $0.70 | $2.80 |
| **Low cost** | DeepSeek Chat | $0.14 | $0.28 |
| **Low cost** | GPT-4o mini | $0.15 | $0.60 |
| **Low cost** | Groq llama-3.3-70b | $0.59 | $0.79 |
| **Mid tier** | Gemini 2.5 Pro | $1.25 | $10.00 |
| **Mid tier** | Mistral Large | $2.00 | $6.00 |
| **Mid tier** | GPT-4o | $2.50 | $10.00 |
| **Premium** | Claude Sonnet | $3.00 | $15.00 |
| **Premium** | Grok-3 | $3.00 | $15.00 |
| **Premium** | Claude Opus | $15.00 | $75.00 |

**Live pricing:** `GET /v1/models` returns every model with `inputPerMillionUSD`, `outputPerMillionUSD`, context window, and required plan — the same shape OpenRouter publishes, always current. Filter by tier with `?plan=Bronze`.

> **For Leela:** OrcaRouter's llama-3.3-70b at **$0.59 in / $0.79 out per million** is the cheapest uncensored option. Venice costs slightly more on output but adds a strict zero-logging guarantee. Both are a fraction of Claude Opus ($15/$75).

---

## Why WEB6 Over OpenRouter?

| Feature | OpenRouter | WEB6 |
|---|---|---|
| Number of providers | 600+ | 98 (growing rapidly) |
| Single API key | ✅ | ✅ |
| Pick any model per request | ✅ | ✅ |
| Uncensored models | Some | ✅ Venice + OrcaRouter (default-able) |
| Multi-agent reasoning (FAHRN) | ❌ | ✅ |
| Holonic BRAID coordination | ❌ | ✅ |
| Semantic response caching | ❌ | ✅ (reduces costs over time) |
| Avatar memory injection | ❌ | ✅ |
| Data ownership | OpenRouter's servers | Your infrastructure |
| Middleman markup | Yes | **None** |
| Free local model tier | ❌ | ✅ ($0 cost) |
| Custom system prompt + knowledge base | Manual | ✅ Built-in avatar context layer |

### The Key Differentiator

OpenRouter routes requests to individual models. **WEB6 coordinates multiple AI agents working together** — FAHRN dispatches your query to a reasoning network, Holonic BRAID injects shared context across agents, and semantic caching means repeated or similar queries are answered instantly at zero cost. This is the difference between asking one person a question and convening a specialist team.

---

## What This Means for Leela

1. **Leela can be fully uncensored** — set Venice.ai or OrcaRouter as default; she will never refuse or hedge based on mainstream AI guardrails

2. **Leela's intelligence compounds** — FAHRN + Holonic BRAID means her responses draw on coordinated multi-agent reasoning, not a single model's output

3. **Costs stay low** — semantic caching means repeated conversations cost a fraction of normal; local models ($0) handle anything that doesn't need cloud inference

4. **You control the guardrails** — the system prompt and knowledge base define Leela's worldview; the uncensored model simply means she won't refuse to engage with it, not that she ignores it

5. **No vendor lock-in** — if a better uncensored model launches tomorrow, it can be added and set as default in minutes

---

## Subscription Tiers (OPORTAL)

| Plan | Monthly | Daily Call Limit | Model Access |
|---|---|---|---|
| Free | $0 | 100 | Local/self-hosted only ($0 cost) |
| Bronze | $9 | 500 | Cheap cloud: Venice, OrcaRouter, Groq, GPT-4o-mini |
| Silver | $29 | 2,000 | Mid-tier: GPT-4o, Claude Sonnet, Gemini Pro |
| Gold | $99 | 10,000 | Premium: GPT-5, o3, Claude Opus, Grok-3 |
| Enterprise | Custom | Unlimited | All models + priority routing + SLA |

> Karma multiplies your daily call limit — high karma users get more headroom within their plan tier.

---

## Getting Started

1. **Sign up** at OPORTAL → receive your WEB6 API key
2. **Set your provider** — default is `auto` (OASIS selects best available), or specify `VeniceAI` / `OrcaRouter` for uncensored
3. **Call the endpoint:**

```http
POST https://api.web6.oasisomniverse.one/v1/complete
Authorization: Bearer <your-web6-key>

{
  "provider": "VeniceAI",
  "model": "llama-3.3-70b",
  "messages": [
    { "role": "user", "content": "Hello Leela" }
  ]
}
```

4. **That's it.** Response is normalised — same shape regardless of which provider handled it.

### Discovering what's available

```bash
# Every model with live per-million pricing, context window and required plan
GET /v1/models

# Only models reachable on the Bronze plan
GET /v1/models?plan=Bronze

# All 98 providers with capabilities and tier
GET /v1/providers

# Live health probe of every provider
GET /v1/providers?live=true
```

These endpoints are the authoritative, always-current answer to "what models do you provide and what do they cost" — this document is a snapshot.

---

## Environment Variables (for self-hosted deployments)

```
VENICE_API_KEY=your-venice-key
ORCAROUTER_API_KEY=your-orca-key
WEB6_DEFAULT_PROVIDER=VeniceAI
WEB6_DEFAULT_MODEL=llama-3.3-70b
```

---

*WEB6 is part of the OASIS Omniverse ecosystem — oasisomniverse.one*
