# WEB6 — Quotas, Tiers & Metering Reference

Authoritative reference for every limit, gate and cost the WEB6 API enforces.
All values below are read directly from source; the file/method for each is cited so
this document can be re-verified rather than trusted.

---

## 1. Subscription Plans

`NextGenSoftware.OASIS.Web6.Core/Enums/SubscriptionPlan.cs`

| Plan | Price | Intended for |
|---|---|---|
| Free | $0 | Evaluation — local/self-hosted models only |
| Bronze | $9/mo | Developer entry tier |
| Silver | $29/mo | Individual developers, small projects |
| Gold | $99/mo | Teams and production applications |
| Enterprise | Custom | Unlimited, SLA, priority routing |

Plan is read from the `plan` JWT claim set by `JwtMiddleware`. Absent or unparseable → `Free`.

---

## 2. Daily Call Quota

`UsageMeteringManager.GetPlanDailyCallLimit`

| Plan | Base calls/day |
|---|---|
| Free | 20 |
| Bronze | 100 |
| Silver | 500 |
| Gold | 2,000 |
| Enterprise | **Unlimited** (0 = no limit) |

### Karma multiplier

`UsageMeteringManager.GetKarmaMultiplier` — karma multiplies quota **within** the plan.
It never unlocks a higher model tier.

| Karma | Multiplier |
|---|---|
| 0 – 499 | 1.0× |
| 500 – 999 | 1.5× |
| 1,000 – 4,999 | 2.0× |
| 5,000 – 19,999 | 3.0× |
| 20,000 – 99,999 | 5.0× |
| 100,000+ | 10.0× |

### Effective daily limit

`GetEffectiveDailyCallLimit(plan, karma) = GetPlanDailyCallLimit(plan) × GetKarmaMultiplier(karma)`

| Plan | 0 karma | 1k karma | 20k karma | 100k karma |
|---|---|---|---|---|
| Free | 20 | 40 | 100 | 200 |
| Bronze | 100 | 200 | 500 | 1,000 |
| Silver | 500 | 1,000 | 2,500 | 5,000 |
| Gold | 2,000 | 4,000 | 10,000 | 20,000 |
| Enterprise | ∞ | ∞ | ∞ | ∞ |

Counter key: `web6-calls-yyyy-MM-dd`, stored per avatar in the `subscription-usage`
settings holon. Resets at **00:00 UTC**.

---

## 3. Per-Minute Burst Limit

`RateLimitHeaderMiddleware.GetPerMinuteLimit` — enforced **before** the request reaches
the controller. Breach returns `429` with `Retry-After: 60`.

| Plan | Requests/minute |
|---|---|
| Free | 10 |
| Bronze | 60 |
| Silver | 300 |
| Gold | 1,000 |
| Enterprise | Unlimited |

Karma does **not** affect the burst limit — only the daily quota.
Counters are per-process; see the caveat in section 9.

---

## 4. Additional Quota Dimensions (opt-in via OASIS_DNA.json)

Both default to `0` = disabled.

| Setting | Effect |
|---|---|
| `OASIS.Web6.DefaultMonthlyBudgetUSD` | Blocks once monthly spend exceeds this. Key `web6-spend-yyyy-MM` |
| `OASIS.Web6.DefaultDailyTokenLimit` | Blocks once daily tokens exceed this. Key `web6-tokens-yyyy-MM-dd` |

`CheckQuotaAsync` evaluates in order: monthly budget → daily tokens → daily calls.
First breach returns a human-readable reason and a `429`.

---

## 5. Model Access Gate

`KarmaGateManager.Evaluate` — enforced inside `AIProviderManager` on both the streaming
and non-streaming paths. Requesting a model above your tier **downgrades** rather than
rejects, so the call still succeeds.

| Plan | Models accessible |
|---|---|
| Free | Local/self-hosted only — Ollama, GPT4All, VLLM, Jan, Llamafile, GaiaNet, custom endpoints |
| Bronze | Cheap cloud — gpt-4o-mini, claude-haiku, gemini-flash, groq, mistral-small, deepseek-chat, cerebras, Venice, OrcaRouter |
| Silver | Mid-tier — gpt-4o, claude-sonnet, gemini-pro, mistral-large, command-r+, llama-3.1-70b, Bedrock, Azure OpenAI |
| Gold | Premium — gpt-5, o1, o3, claude-opus, gemini-ultra, grok-3 |
| Enterprise | All models + priority routing |

Downgrade fallbacks: Free → `llama3.2` (Ollama) · Bronze → `llama-3.1-8b-instant` · Silver → `gpt-4o-mini`

Tier lists are overridable at startup via `WEB6_BRONZE_MODELS`, `WEB6_SILVER_MODELS`,
`WEB6_GOLD_MODELS` (comma-separated substrings, appended to the defaults).

---

## 6. Metering Coverage

Quota and usage recording are applied by `MeteredEndpointAttribute`
(`WebAPI/Attributes/MeteredEndpointAttribute.cs`), an action filter that runs the quota
pre-flight before the action and records the call afterwards — **only on 2xx**.

### Token-accurate (billed on real token counts)

| Endpoint | Notes |
|---|---|
| `POST /v1/complete` | Inline quota + `RecordUsageAsync` |
| `POST /v1/complete/stream` | Same; tokens taken from the terminal SSE chunk |
| `POST /v1/chat/completions` | Filter enforces quota; controller records tokens (`RecordUsage = false` to avoid double counting) |

### Unit-priced (billed per call)

| Endpoint | Tag |
|---|---|
| Video | `video` |
| Images | `images` |
| Speech | `speech` |
| Batch | `batch` |
| Rerank | `rerank` |
| Embeddings | `embeddings` |
| Moderation | `moderation` |
| FAHRN Solve | `fahrn` |
| Reasoning Network | `reasoning` |
| GraphRAG | `graphrag` |
| A2A | `a2a` |
| Classification | `classification` |
| Extraction | `extraction` |
| Search | `search` |
| Documents | `documents` |
| Finetuning | `finetuning` |
| Guardrails | `guardrails` |
| Prompt | `prompt` |
| Memory / Holonic Memory | `memory` |
| Code | `code` |
| WebSocket session | `chat` |

**24 controllers metered.**

### Deliberately unmetered

| Endpoint | Why |
|---|---|
| `UsageController` | Reports usage — metering it would be circular |
| `MLNetController` | Local ML.NET, no provider call, zero marginal cost |
| `ContextController` | Reads OASIS-internal avatar context |
| `HolonicBraidController` | Reads/writes OASIS-held reasoning graphs |
| Health, Discovery, Admin, Did, Keys | Not AI calls |

---

## 7. Per-Call Unit Costs

`UsageMeteringManager.GetUnitCost` — USD charged against monthly spend for one call to a
unit-priced endpoint.

> **These figures are internal estimates, not confirmed provider rates.**
> They were set as conservative defaults and have not been calibrated against real
> invoices. They feed monthly-budget enforcement and cost reporting, so they must be
> replaced with measured values before being relied on for billing or tier pricing.
> See "Open items" below.

| Tag | USD/call | Basis |
|---|---|---|
| `video` | 0.50 | Runway Gen-3 / Luma / Kling |
| `fahrn`, `reasoning`, `a2a` | 0.05 | Multi-model fan-out |
| `images` | 0.04 | Flux Pro / Ideogram / Leonardo |
| `graphrag` | 0.02 | Multi-step retrieval + synthesis |
| `finetuning` | 0.02 | Job submission (training billed separately by provider) |
| `speech` | 0.015 | ElevenLabs TTS / Deepgram STT |
| `batch`, `documents` | 0.010 | |
| `search` | 0.005 | Web-grounded, per query |
| `rerank`, `extraction`, `prompt`, `code` | 0.002 | |
| `classification`, `guardrails`, *default* | 0.001 | |
| `memory` | 0.0005 | Embedding-backed recall |
| `embeddings` | 0.0001 | |
| `moderation` | 0.0 | Free on most providers |

Token-billed models use the per-model table in `UsageMeteringManager._pricing`
(35 entries) with a per-provider fallback of $0.005/1k for unlisted models.

---

## 8. Quota Alerts

`UsageMeteringManager.MaybeFireQuotaAlertAsync`

- Fires at **80%** and **90%** of the effective daily call limit
- POSTs to `WEB6_QUOTA_WEBHOOK_URL`
- Signed with HMAC-SHA256 over `{timestamp}.{body}` when `WEB6_QUOTA_WEBHOOK_SECRET` is set
  - Headers: `X-Web6-Signature: sha256=<hex>`, `X-Web6-Timestamp`
- Deduped per avatar + threshold + day

---

## 9. Response Headers

Added by `RateLimitHeaderMiddleware` on every non-health request:

| Header | Meaning |
|---|---|
| `X-RateLimit-Limit` | Effective daily limit (plan × karma) |
| `X-RateLimit-Remaining` | Calls left today |
| `X-RateLimit-Reset` | UTC epoch seconds of next reset (midnight UTC) |
| `X-RateLimit-Plan` | Plan label |
| `X-Karma-Multiplier` | Applied multiplier |
| `X-RateLimit-Limit-Min` / `X-RateLimit-Used-Min` | Per-minute burst |
| `X-RateLimit-Scope` | Always `instance` — see caveat below |

Completion endpoints also return `X-Cache` (HIT or MISS) and `X-Cache-Age`.

> **Horizontal-scale caveat:** rate-limit counters are in-process. With N instances a
> caller can reach up to N× the stated burst limit. The daily quota is authoritative
> because it reads from the shared Data API, but the header figures are per-pod
> approximations — hence `X-RateLimit-Scope: instance`.

---

## 10. Environment Variables

| Variable | Purpose |
|---|---|
| `WEB6_QUOTA_WEBHOOK_URL` | Quota alert destination |
| `WEB6_QUOTA_WEBHOOK_SECRET` | HMAC signing key for alerts |
| `WEB6_BRONZE_MODELS` / `WEB6_SILVER_MODELS` / `WEB6_GOLD_MODELS` | Extend model tier lists |
| `WEB6_ADMIN_API_KEY` | Admin endpoint access (`X-Web6-Admin-Key` header) |
| `WEB6_PROVIDER_STATUS_TTL_SECONDS` | Provider health cache TTL (default 60) |

---

## Open items

1. **Unit costs in section 7 are uncalibrated estimates.** They drive budget enforcement
   and cost reporting. Replace with measured per-call costs from real provider invoices.
2. **Venice.ai and OrcaRouter token rates** in `_pricing` are likewise unconfirmed —
   verify against their dashboards once API keys are live.
3. **Free tier is 20 calls/day.** Endpoints that were previously unmetered now enforce
   this. Any client relying on them will start seeing 429s; partners intended to have
   free unlimited access need `Enterprise` plan claims.

---

*Generated from source. Re-verify with `GET /v1/models`, `GET /v1/providers` and
`GET /v1/usage` against a running instance.*
