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
| `POST /v1/chat/completions` | Filter enforces quota; controller records tokens on both the streaming and non-streaming branches (`RecordUsage = false` to avoid double counting) |
| `GET /v1/ws/session` | Quota re-checked and tokens recorded **per message**, not per connection — a long-lived session cannot outrun its limit. Filter is quota-only (`RecordUsage = false`) |

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

## 7. Cost Resolution

Costs resolve in three steps, so a stale price guide corrects itself from real usage:

```
1. Learned rate   (ObservedCostTracker — from what providers actually charged)
2. Published rate (per-provider, sourced from the provider's price list)
3. Fallback       (tag-level or per-provider default)
```

A learned rate is only used once it has **5 observations** for that key; until then the
published rate applies. Learned rates are system-wide and persist through the OASIS Data
API, so they survive restarts and reach other instances.

The table hangs off an operator avatar rather than an end user. Set `WEB6_SYSTEM_AVATAR_ID`
to choose which; otherwise a fixed namespace id is used. It is loaded once at startup —
that load is also what arms saving, so without it the tracker would keep everything in
memory only.

Inspect what has been learned: `GET /v1/admin/config/observed-costs`
Reset to published defaults: `DELETE /v1/admin/config/observed-costs`

### How learning works

Providers that report a charge — in a response header (`x-cost`, `x-total-cost`,
`openrouter-cost`, …) or a JSON field (`cost`, `usage.total_cost`, `credits_used`, …) —
feed an exponential moving average.

- **Token-billed calls:** the reported charge divided by tokens used gives a blended
  per-1k rate, recorded per (provider, model). Values above $1/1k are rejected as
  implausible. Coverage: the shared OpenAI-compatible path (all ~90 providers routed
  through it) plus the dedicated paths for Anthropic, Gemini, Cohere, Google Vertex,
  IBM WatsonX, Baidu ERNIE and HyperCLOVA X.
  Providers whose response carries no usage block — AWS Bedrock (SDK, not raw HTTP),
  Azure OpenAI, HuggingFace, Snowflake Cortex, AlephAlpha, Tencent Hunyuan, Spark —
  cannot yield a per-1k rate and always use their published or fallback rate.
- **Unit-priced calls:** the reported charge is recorded per (tag, provider) by the
  `MeteredEndpoint` filter. Controllers can also set `HttpContext.Items["Web6-Observed-Cost"]`
  explicitly. Values outside $0–$100 are rejected.

Providers that report nothing keep using the published rate indefinitely — that is the
common case, which is why the published tables still matter.

### Published unit rates (per call)

| Tag | Provider | USD | Source |
|---|---|---|---|
| video | Runway ML | 0.25 | Gen-3 Turbo, 5 credits/s @ $0.01, 5s clip |
| video | Kling AI | 0.31 | v2.5 Turbo, 5s |
| video | Hailuo | 0.30 | 5s equivalent |
| video | Luma AI | 0.60 | Ray 2 Flash 720p, 5s |
| images | Leonardo | 0.02 | per image |
| images | Stability AI | 0.03 | per image |
| images | Black Forest Labs | 0.04 | FLUX 1.1 [pro], 4 credits @ $0.01 |
| images | Ideogram | 0.08 | per image |
| speech | Deepgram | 0.0043 | Nova-3 batch STT, per minute |
| speech | AssemblyAI | 0.0062 | per minute |
| speech | PlayHT | 0.05 | per request |
| speech | ElevenLabs | 0.10 | TTS v2/v3, per 1k characters |

Video and speech price by usage (clip length, character count), so these represent a
typical call — exactly the case the learner corrects.

### Tag-level fallbacks

Used when no provider-specific rate applies. Mid-market figures covering several providers.

| Tag | USD | Tag | USD |
|---|---|---|---|
| video | 0.31 | search | 0.005 |
| fahrn / reasoning / a2a | 0.05 | rerank / extraction / prompt / code | 0.002 |
| images | 0.04 | classification / guardrails | 0.001 |
| speech | 0.05 | memory | 0.0005 |
| graphrag / finetuning | 0.02 | embeddings | 0.0001 |
| batch / documents | 0.010 | moderation | 0.0 |

### Token pricing coverage

`UsageMeteringManager._pricing` holds **46 model rates across 28 providers**, including
OpenAI, Anthropic, Gemini, Groq, Mistral, Cohere, xAI, DeepSeek, Cerebras, Together,
Perplexity, Venice, OrcaRouter, OpenRouter, DeepInfra, Fireworks, SambaNova, Hyperbolic,
AWS Bedrock, Azure OpenAI and Google Vertex, plus explicit $0 entries for every local
runtime.

**The remaining providers have no published rate** and fall back to a coarse per-provider
figure ($0.005/1k default, $0.010 Anthropic, $0.005 OpenAI, $0.002 Gemini, $0.001 Groq).
Those are the entries the learner is there to fix: any of them that reports a cost will
converge on its true rate within 5 calls. Ones that never report a cost keep the fallback
and should be filled in by hand as their price lists are checked.

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

1. **Providers without a published token rate** sit on the coarse per-provider fallback
   until the learner gathers 5 observations — and only ever learn if that provider reports
   a cost. Check `GET /v1/admin/config/observed-costs` after real traffic to see which
   have converged; fill the rest in by hand from their price lists.
2. **Video and speech unit rates represent a typical call** (5-second clip, 1k characters,
   1 minute of audio). Actual charges scale with usage, so these are starting points the
   learner refines.
3. **Free tier is 20 calls/day.** Endpoints that were previously unmetered now enforce
   this. Any client relying on them will start seeing 429s; partners intended to have free
   unlimited access need `Enterprise` plan claims.

*Generated from source. Re-verify with `GET /v1/models`, `GET /v1/providers` and
`GET /v1/usage` against a running instance.*
