# WEB6 vs OpenRouter vs Hermes Agent

*Last updated: 2026-10-20. Competitor prices are their published list prices at the time of writing. Check their sites before quoting them to a customer.*

**In short:** OpenRouter gives you one API key for many models. Hermes Agent gives one person an agent that runs on their own machine. **WEB6 does both jobs and adds what neither has:** many users on one platform, memory that persists, reasoning that is reused instead of re-bought, real billing and spending caps, and messaging channels. It charges **no markup** on model, sandbox or speech costs.

---

## 1. What each one is

| | OpenRouter | Hermes Agent (Nous Research) | **WEB6 (OASIS)** |
|---|---|---|---|
| Category | Hosted model router | Open-source agent app you run yourself | **Hosted AI platform: model gateway, agent runtime, memory, reasoning network and billing** |
| Who it serves | One developer account and API key | One person on one machine | **Every end user, each with their own OASIS avatar, memory, budget and audit trail** |
| Where it runs | Their cloud | Your laptop or server | **Our cloud, or on your own servers through a self-hosted ONODE** |
| How you adopt it | Change the base URL | Install and adopt its runtime | **Change the base URL (OpenAI-compatible), or use the agent, memory and channel APIs** |

---

## 2. Feature comparison

### Models and routing

| | OpenRouter | Hermes | **WEB6** |
|---|---|---|---|
| Model providers behind one API | Many | Whatever you configure | **98 providers** |
| OpenAI-compatible `/v1/chat/completions` | Yes | No (it is a client) | **Yes: LangChain, AutoGen and CrewAI work by changing the base URL** |
| Automatic cheapest-model routing | Price and uptime routing | No | **Yes: cost-first routing that only picks models your plan allows** |
| Semantic cache (a repeated question costs nothing) | No | No | **Yes** |
| Reasoning reuse across sessions and users | No | No | **Holonic BRAID: proven reasoning plans are reused, so cheaper models can follow them (up to a 74× cost reduction measured on BRAID tasks)** |
| Multi-model orchestration | No | Parallel subagents | **FAHRN: serial, parallel, debate, voting and decomposed dispatch, with budget guards and loop detection** |

### Agent runtime

| | OpenRouter | Hermes | **WEB6** |
|---|---|---|---|
| Server-side agent loop | No | Yes, local | **`POST /v1/agent/run`: metered per step, with a step budget, and it can pause for your app's own tools** |
| Self-improving skills | No | Writes skills | **Drafts a skill after a task. Your 👍 keeps it, and SkillOpt then evolves it, keeping only edits that measurably score better** |
| Long-term memory | No | Local notes for one agent | **Holonic Memory per user, which the agent curates automatically, without duplicates. Adapters for Mem0, Zep, Letta, LangMem, Graphiti, Qdrant and Weaviate** |
| Document knowledge base | No | No | **Holonic Memory ingests, chunks, de-duplicates and searches a whole document set** |
| Code execution | No | Shell on the host machine | **Isolated sandboxes (E2B, Daytona, Judge0), billed per millisecond** |
| Tools | No | MCP client | **Any remote MCP server as agent tools, plus 508 MCP tools served by WEB6 itself** |
| Agent protocols | No | No | **A2A, ACP, ANP, LangGraph, OpenAI Agents, gRPC, GraphQL, webhooks** |

### Reaching users

| | OpenRouter | Hermes | **WEB6** |
|---|---|---|---|
| WhatsApp, Telegram, Slack, Discord, Signal, Email | No | Yes, for one user | **Yes, for every user: signed webhooks, one-time account linking, per-chat memory, every reply billed to that user** |
| Voice notes | No | Yes | **Transcribed and answered in one billed operation, charged by measured audio length** |
| Scheduled runs | No | Local cron, not billed | **Per-user cron schedules that run exactly once across servers, each under a budget-capped grant. Results go to a webhook or the user's chat** |
| Terminal chat | No | `hermes` | **`star chat`, with memory, skills, sandbox and MCP tools** |
| Live web demo | Playground | No | **[WEB6 Sandbox](https://web6.oasisomniverse.one/web6-sandbox.html)** |

### Money, safety and privacy

| | OpenRouter | Hermes | **WEB6** |
|---|---|---|---|
| Per-user billing | No: one shared account | No | **Yes: each user has a plan, daily and monthly limits, and an audit trail** |
| Spending caps | Account credit balance | None | **Plan budgets, plus revocable grants with monthly and per-operation caps** |
| Cost accounting | Charged after the call | None | **Reserve, then settle: every call reserves its maximum cost in advance and settles to what was measured, so a budget is never overrun** |
| Delegating spend to an agent or bot | API key sharing | Your own keys | **WEB4 delegated grants: scoped to endpoints, capped and revocable at any time** |
| Privacy for sensitive data | Provider terms only | Local files | **Each user's data in their own holons, sharing rules that keep it private unless allowed, retention limits, and self-hosting for full data control** |
| Identity | API key | None | **An OASIS avatar per user, with single sign-on across WEB4–WEB10** |

---

## 3. Prices

### Platform fee

| | OpenRouter | Hermes | **WEB6** |
|---|---|---|---|
| Software | Free to sign up | Free (open source) | **Free plan available** |
| Platform fee | About 5.5% on credit purchases. Bring-your-own-key use is charged a fee after a free allowance | $0, but you pay for the server it runs on and for your own time running it | **Flat plans: Free $0 · Bronze $9/mo · Silver $29/mo · Gold $99/mo · Enterprise custom** |
| Markup on model tokens | None on list price, but the purchase fee applies to every dollar | None (your own keys) | **None: provider list price** |
| Markup on code sandboxes | n/a | n/a (no isolation) | **None (see below)** |
| Markup on speech-to-text | n/a | Your own keys | **None (see below)** |

At scale a percentage fee grows with every dollar you spend, while a WEB6 plan stays flat. For example, a team spending $2,000 a month on models pays OpenRouter about $110 a month in fees; on WEB6 Gold it pays $99, and that also includes memory, agents, channels and billing.

### WEB6 plans

| Plan | Price | Monthly requests | Daily calls | Daily tokens | Monthly AI budget |
|---|---|---:|---:|---:|---:|
| Free | $0 | 1,000 | 20 | 50,000 | $1 |
| Bronze | $9/mo | 10,000 | 100 | 250,000 | $10 |
| Silver | $29/mo | 100,000 | 500 | 1,000,000 | $50 |
| Gold | $99/mo | 1,000,000 | 2,000 | 5,000,000 | $250 |
| Enterprise | Custom | Unlimited | Unlimited | Unlimited | Unlimited |

Karma raises daily call limits by up to 5×. Full details are in [WEB6_Quotas_and_Tiers.md](WEB6_Quotas_and_Tiers.md).

### Usage-based prices (provider cost, no markup)

| Meter | Provider | Price | In practice |
|---|---|---|---|
| `sandbox_ms` | E2B (2 vCPU) | $0.000000028 per ms ($0.10/hour) | A 2-second code run costs about $0.00006 |
| `sandbox_ms` | Daytona | $0.000000028 per ms | Same as E2B |
| `sandbox_ms` | Judge0 (self-hosted) | $0.000000005 per ms | Covers hosting only |
| `audio_ms` | AssemblyAI nano | $0.000000033 per ms ($0.12/hour) | A 30-second voice note costs about $0.001 |
| `audio_ms` | Deepgram nova-3 | $0.000000072 per ms ($0.0043/minute) | A 30-second voice note costs about $0.002 |
| Tokens | 98 providers | Each provider's list price | The semantic cache and BRAID reuse cut this further |

Administrators publish these as catalogue unit prices with `Scripts/web6_publish_unit_prices.py`, which calls `POST /v1/admin/config/catalogue/publish`. A metered call for a provider with no published price is refused, never billed at a guessed rate.

### A worked example: 1,000 users chatting daily

Assume each user sends 10 messages a day, one in five runs a short code snippet, and one in ten is a 30-second voice note.

| | OpenRouter | Hermes | **WEB6** |
|---|---|---|---|
| Model tokens | List price + ~5.5% | List price | **List price, minus cache hits and BRAID reuse** |
| Memory per user | Build it yourself | One instance per user (1,000 servers) | **Included** |
| WhatsApp / Telegram | Build it yourself | One instance per user | **Included, billed per user** |
| Code runs (~2,000/day × 2 s) | Not available | Unisolated shell | **About $0.12/day** |
| Voice notes (~1,000/day × 30 s) | Not available | Your own speech keys | **About $1/day (AssemblyAI)** |
| Per-user billing and caps | Build it yourself | Not possible | **Included** |

---

## 4. Already using OpenRouter or Hermes?

You don't have to choose. Point OpenRouter-style code at WEB6's OpenAI-compatible endpoint, or set WEB6 as Hermes's model provider. Every call then gets WEB6's routing, cache, memory and per-user billing underneath. When you're ready, move the agent loop itself to `POST /v1/agent/run`.

## 5. Try it

- Live demo: https://web6.oasisomniverse.one/web6-sandbox.html
- REST reference: [WEB6_REST_API_Reference.md](WEB6_REST_API_Reference.md)
- Leela integration guide: [WEB6-Leela-AI-Integration-Guide.md](WEB6-Leela-AI-Integration-Guide.md)
- Getting started: [WEB6-Getting-Started-Guide.md](WEB6-Getting-Started-Guide.md)
