# WEB6 — Plans, Quotas & Metering

*Last verified against source: 2026-09-28.*

Every WEB6 plan, limit and billing rule described here is enforced by **WEB4**, which is the single subscription and usage authority for WEB5–WEB10. WEB6 does not keep its own counters. It reserves each billable operation with WEB4, runs the provider call, measures what was used, and settles that usage back to WEB4's transactional ledger.

For the full protocol see [WEB4 Subscription Usage Ledger](../../WEB4_SUBSCRIPTION_USAGE_LEDGER.md) and the [operations runbook](../../WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md).

---

## 1. Plans

| Plan | Price | Monthly requests | Storage | Support | Intended for |
|---|---|---:|---|---|---|
| Free | $0 | 1,000 | — | Community | Evaluation |
| Bronze | $9/mo | 10,000 | 1 GB | Community | Developer entry tier |
| Silver | $29/mo | 100,000 | 10 GB | Email | Individual developers, small projects |
| Gold | $99/mo | 1,000,000 | 100 GB | Priority + SLA | Teams and production applications |
| Enterprise | Custom (contact sales) | Unlimited | Unlimited | Dedicated + SLA/SSO | Unlimited usage, custom contracts |

Plans are listed by `GET /api/subscription/plans` on WEB4 and purchased through Stripe checkout. Your plan is resolved by WEB4 from your authenticated avatar. Plan or karma values in a JWT are never trusted, and the request body cannot choose the billing identity.

---

## 2. Usage limits

WEB4 checks these before any provider work starts. If a limit is hit, the call is rejected and nothing is executed.

| Plan | Monthly requests | Daily calls (before karma) | Daily tokens | Monthly AI budget (USD) |
|---|---:|---:|---:|---:|
| Free | 1,000 | 20 | 50,000 | $1 |
| Bronze | 10,000 | 100 | 250,000 | $10 |
| Silver | 100,000 | 500 | 1,000,000 | $50 |
| Gold | 1,000,000 | 2,000 | 5,000,000 | $250 |
| Enterprise | Unlimited | Unlimited | Unlimited | Unlimited |

- Daily counters reset at 00:00 UTC. Monthly counters follow the UTC calendar month.
- Amounts already reserved by in-flight calls count against your limits until they settle.

### Karma multiplier (daily calls only)

Karma raises the **daily call** limit within your plan. It does not change monthly requests, daily tokens, budget or model access. WEB4 reads your karma itself.

| Karma | Multiplier |
|---|---|
| 0 – 499 | 1.0× |
| 500 – 999 | 1.5× |
| 1,000 – 4,999 | 2.0× |
| 5,000 – 19,999 | 3.0× |
| 20,000 – 99,999 | 5.0× |
| 100,000+ | 10.0× |

| Plan | 0 karma | 1k karma | 20k karma | 100k karma |
|---|---:|---:|---:|---:|
| Free | 20 | 40 | 100 | 200 |
| Bronze | 100 | 200 | 500 | 1,000 |
| Silver | 500 | 1,000 | 2,500 | 5,000 |
| Gold | 2,000 | 4,000 | 10,000 | 20,000 |
| Enterprise | ∞ | ∞ | ∞ | ∞ |

### Limit errors

Exceeding a limit returns HTTP `429` with one of these codes:

`MONTHLY_REQUEST_LIMIT_EXCEEDED` · `DAILY_CALL_LIMIT_EXCEEDED` · `DAILY_TOKEN_LIMIT_EXCEEDED` · `MONTHLY_BUDGET_EXCEEDED`

Missing or inactive subscriptions return `SUBSCRIPTION_REQUIRED` or `INACTIVE_SUBSCRIPTION`.

---

## 3. Calling the API

Billable calls require:

- a valid bearer token, and
- an `Idempotency-Key` header. Reuse the same key when retrying so a retry is never charged twice. A key cannot be reused with a changed request. Use a new key for new work.

Each billable response includes an **operation ID** you can use to look up its usage record.

If WEB4 is briefly unavailable after your provider call has already run, you receive HTTP `503` `SETTLEMENT_PENDING` with the operation ID. The usage is stored durably and settled automatically. Do not resubmit the call with a new key.

Streaming (SSE) responses are currently delivered once settlement is acknowledged rather than incrementally over HTTP. WebSocket sessions (`/v1/ws/session`) reserve and settle per message, and each message needs its own idempotency key.

---

## 4. Model access by plan

Each model in the catalogue carries a minimum plan.

| Plan | Intended model access |
|---|---|
| Free | Local / self-hosted (Ollama, GPT4All, VLLM, Jan, Llamafile, GaiaNet, custom endpoints) |
| Bronze | Cheap cloud: gpt-4o-mini, claude-haiku, gemini-flash, groq, mistral-small, deepseek-chat, cerebras, Venice, OrcaRouter |
| Silver | Mid-tier: gpt-4o, claude-sonnet, gemini-pro, mistral-large, command-r+, llama-3.1-70b, Bedrock, Azure OpenAI |
| Gold | Premium: gpt-5, o1, o3, claude-opus, gemini-ultra, grok-3 |
| Enterprise | All models + priority routing |

`GET /v1/models?plan=<Plan>` lists the models reachable on a plan, with each model's required plan and its input/output price per million tokens.

> **Note:** the automatic downgrade rules (`KarmaGateManager`) are implemented and unit-tested, but as of 2026-09-28 they are **not wired into the request path**. Plan-based model gating is therefore advertised by the catalogue and not yet enforced at call time. Enforcement is bounded by the daily-token and monthly-budget limits above.

---

## 5. What is metered

Each billable endpoint is tagged with a meter category:

- **`ai.tokens`** — calls that reach an AI provider. WEB4 reserves a conservative upper bound of tokens and USD up front, then settles the measured tokens and cost. Unknown prices or unsupported measurement paths fail before any provider work happens.
- **`api.request`** — included requests (no provider cost), counted against monthly requests.

Failures and cancellations still settle any cost that was measured. An HTTP error does not mean the provider charged nothing.

### Metered endpoint families (24 controllers)

Completions (`/v1/complete`, `/v1/complete/stream`, `/v1/chat/completions`, `/v1/ws/session`), video, images, speech, batch, rerank, embeddings, moderation, FAHRN solve, reasoning network, GraphRAG, A2A, classification, extraction, search, documents, fine-tuning, guardrails, prompt, memory / holonic memory, code, translation, plus GraphQL, MCP and gRPC provider calls.

### Not metered

`/v1/usage`, health, discovery, swagger/openapi, `.well-known` and metrics endpoints. Anonymous endpoints are also exempt.

---

## 6. Pricing catalogue

Provider costs are calculated from the reviewed model catalogue, `web6-model-catalogue.json` (version `web6-model-catalogue-2026-09-22-v1`): **88 models across 26 providers**. The catalogue version is recorded on every operation, so historical usage can always be tied to the prices used.

Price lists for individual models are available at runtime from `GET /v1/models` (`inputPerMillionUSD`, `outputPerMillionUSD`, context window, required plan).

Amounts are stored as exact decimals and rounded once, at the invoice boundary.

Local / self-hosted models have zero marginal provider cost.

---

## 7. Checking your usage

- `GET /v1/usage` — WEB6 usage and quota summary for your avatar.
- `GET /api/subscription/usage` (WEB4) — authoritative plan, usage and billing-facing totals.

See the [WEB4 Subscription API](../WEB4%20OASIS%20API/Subscription-API.md) for the full contract.

---

## Retired behaviour

The following are no longer part of WEB6 and should not be relied on:

- WEB6-local quota counters, the `authorize-request` counter (returns `410`) and JWT plan/karma decisions
- Per-minute burst limits and the `X-RateLimit-*` / `X-Karma-Multiplier` response headers
- Quota alert webhooks (`WEB6_QUOTA_WEBHOOK_URL`)
- Learned rates as a billing input. The cost learner (`GET/DELETE /v1/admin/config/observed-costs`, admin only) still runs, but it only affects the advisory `estimatedCostUSD` shown on responses. Billed cost comes solely from the reviewed catalogue and measured provider receipts.

---

## Open items

1. Plan-based model gating is not yet enforced at call time (see section 4).
2. Provider measurements and reviewed price catalogues must be configured before paid execution is enabled in each environment.
3. Enterprise pricing and limits are set per contract.
