# WEB6 metering and WEB4 subscription integration audit

Date: 2026-09-20

## Implementation status

The September 22 protocol revision extends the initial implementation with authenticated service ownership, a durable execution-start boundary, shared WEB5–WEB10 outboxes, immutable audit, expiry/recovery, provider receipt deduplication, administrative corrections, historical opening-balance migration and reconciliation. `authorize-request` is retired. The prior implementation did not provide all of these guarantees; its presence alone was not proof of production billing correctness.

The canonical operating guide, policy table, failure semantics, deployment variables, and verification commands are in [WEB4_SUBSCRIPTION_USAGE_LEDGER.md](WEB4_SUBSCRIPTION_USAGE_LEDGER.md).

The [operations runbook](WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md) distinguishes automated protocol tests from credential-dependent provider, Stripe and deployed-environment release gates. Missing provider receipt contracts must reject work before cost is incurred; unsupported adapters and unrun live tests must not be described as complete production coverage.

The sections below preserve the original September 20 findings and target design for audit history. References there to the "current" compatibility counter and old usage manager describe that historical snapshot, not the active protocol.

## Decision

WEB4 must own subscription entitlements, quota policy, usage authorization, durable usage events, aggregates, overages, and billing-facing totals for every OASIS API. WEB6 should retain only provider-specific measurement: extracting token counts and provider charges, estimating a charge when a provider supplies none, and reporting that measurement to WEB4.

The current WEB6 metering code must not be copied into WEB4 unchanged. It contains a useful cost catalogue and endpoint instrumentation, but its identity, concurrency, failure, and accounting behavior is unsuitable for authoritative billing.

## Current WEB4 model

WEB4 stores these per-avatar records:

- `subscription`: Stripe customer/subscription IDs, plan, status, pay-as-you-go flag, billing period.
- `subscription-usage`: one serialized `UsageRecord` at key `yyyy-MM`, containing request count, overage count, storage used, and last update.
- `subscription-orders`: paid invoice/order history.

`POST /api/subscription/authorize-request` now authenticates the avatar, accepts only WEB5-WEB10 as consumers, checks the authoritative subscription, applies the monthly request allowance, increments usage, and returns the plan and remaining allowance.

Monthly request allowances are currently Free 1,000, Bronze 10,000, Silver 100,000, Gold 1,000,000, and Enterprise unlimited. WEB4 uses `-1` for unlimited.

## Current WEB6 model

`UsageMeteringManager` writes additional values into the same per-avatar `subscription-usage` settings dictionary:

| Key | Meaning | Period |
|---|---|---|
| `yyyy-MM` | Serialized monthly request count | Month |
| `web6-calls-yyyy-MM-dd` | Successful metered calls | Day |
| `web6-spend-yyyy-MM` | Estimated/provider-observed spend in USD | Month |
| `web6-tokens-yyyy-MM-dd` | Prompt plus completion tokens | Day |

It also implements:

- daily call limits: Free 20, Bronze 100, Silver 500, Gold 2,000, Enterprise unlimited;
- karma multipliers: 1x, 1.5x, 2x, 3x, 5x, or 10x;
- optional global DNA limits for daily tokens and monthly USD spend;
- static token prices for a subset of provider/model pairs;
- unit prices for images, video, speech, embeddings, search, memory, and other endpoint tags;
- learned provider costs through `ObservedCostTracker`;
- quota warning webhooks at 80% and 90%;
- REST, GraphQL, gRPC, and MCP usage-summary surfaces.

Twenty-four controller-level `MeteredEndpoint` declarations cover unit-priced endpoint groups. Token-based recording is separately implemented in completion, OpenAI-compatible completion, streaming, and WebSocket paths.

## Findings

### Critical

1. **WEB6 and WEB4 count the same request in the same monthly bucket.** The WEB4 subscription middleware increments `yyyy-MM` before the WEB6 action. A successful WEB6 metered action then increments `yyyy-MM` again. A billable request can therefore consume two monthly requests.

2. **Metering identity can differ from subscription identity.** WEB4 derives the avatar from the validated bearer token. `Web6ControllerBase` and `MeteredEndpointAttribute` prefer caller-controlled `AvatarId` headers, query parameters, or request-body IDs. A caller can cause provider work, caches, and metering to be attributed to another avatar. All billing identity must come from the validated authentication context.

3. **API-key calls bypass WEB4 subscription authorization.** `SubscriptionMiddleware` delegates only requests carrying a bearer token. WEB6's global `WEB6_API_KEY` path can authenticate without a user-bound identity, so those calls have neither an authoritative subscriber nor WEB4 quota accounting.

4. **Authoritative usage writes are best-effort and silently discarded.** Metering runs in unawaited `Task.Run` calls and multiple empty `catch` blocks. Process termination, storage errors, and provider errors can lose usage while the paid provider call succeeds.

5. **The storage update is not atomic across instances.** Both systems read an entire settings dictionary, mutate it, and save it under a process-local semaphore. Two Railway replicas can read the same count and overwrite each other. This affects request counts, tokens, spend, and overages.

### High

6. **WEB6 still trusts JWT `plan` and `karma` claims for internal quotas.** WEB4 is now the plan authority, while JWT claims can be absent or stale for the token lifetime. The WEB4 authorization response already returns the authoritative plan; karma must also come from authoritative avatar state or the WEB4 decision.

7. **Clients can choose the limits shown by usage endpoints.** REST accepts `plan` and `karma` query parameters, and MCP accepts equivalent arguments. These values affect displayed limits and can misrepresent entitlement.

8. **Quota checks fail open.** Storage or parsing failures in `CheckQuotaAsync` return no violation. Paid provider work proceeds when quota state cannot be read.

9. **Authorization and provider-cost quota checks are separate races.** A request can pass the preflight check concurrently with many others and exceed daily calls, tokens, or budget before any background recording completes.

10. **WEB6 uses global DNA budgets instead of per-plan/per-account policy.** `DefaultMonthlyBudgetUSD` and `DefaultDailyTokenLimit` apply globally and are not part of the subscription record or Stripe product configuration.

### Medium

11. **Static pricing can drift.** Prices are compiled into WEB6. Learned costs help only when providers expose a usable charge, require five samples, and are persisted on a best-effort timer.

12. **Unknown endpoint tags receive a non-zero guessed cost.** For example, `translation` is metered but has no explicit unit-cost entry, so it receives the generic $0.001 fallback. Billing must not use an unspecified guess.

13. **The obsolete in-process rate-limit middleware remains in source.** It is no longer registered, but its JWT-plan and per-pod counter design can be accidentally restored.

14. **Usage semantics are inconsistent.** The WEB4 counter counts authorized HTTP requests. WEB6 mostly records successful provider calls. Cache hits and failed provider calls therefore have unclear billing behavior, and class-level attributes can meter management/read actions at the same price as generation actions.

15. **The current summary does not expose an audit trail.** It reports aggregates without immutable event IDs, endpoint, provider, model, tokens, estimated versus actual cost, outcome, or correlation ID.

## Target design

### Ownership

WEB4 owns:

- the subscription and Stripe lifecycle;
- plan capabilities and quota policy;
- authoritative karma value used by commercial policy;
- request authorization and reservation;
- idempotent usage settlement;
- durable usage events and atomic aggregates;
- pay-as-you-go and overage calculation;
- warning/limit notifications;
- user and administrative usage APIs.

WEB6 owns:

- endpoint classification;
- provider/model selection;
- prompt/completion token extraction;
- provider-reported charge extraction;
- provider-specific cost estimation when actual cost is unavailable;
- reporting the final measurement to WEB4.

### Canonical usage dimensions

Every operation needs an immutable `UsageEvent` with:

- `OperationId`: caller-generated UUID used as the idempotency key;
- authenticated `AvatarId`, assigned by WEB4 rather than accepted from the payload;
- `Service`: WEB5-WEB10;
- `Endpoint` and `MeterCategory`;
- `StartedUtc`, `CompletedUtc`, and outcome/status;
- request units;
- prompt, completion, and total tokens;
- media units, duration, characters, pixels, storage bytes, or other typed quantity;
- provider and model;
- `EstimatedCostUsd`, `ActualCostUsd`, and `CostSource` (`provider`, `catalogue`, or `none`);
- currency and pricing-catalogue version;
- correlation/trace ID and optional quest/application attribution.

Aggregates should be derived by avatar, service, category, and UTC period. Request count, tokens, units, estimated spend, actual spend, and overage must be separate fields.

### Two-phase API

The existing request endpoint should evolve into two explicit operations.

1. `POST /api/subscription/usage/authorize`
   - Authenticates the end user or a user-bound OASIS API key.
   - Accepts `operationId`, service, endpoint/category, requested units, and a conservative estimate when known.
   - Atomically evaluates subscription status, monthly requests, daily service quota, tokens/budget where estimable, and burst policy.
   - Creates an idempotent reservation and increments the request dimension once.
   - Returns the authoritative entitlement snapshot: plan, karma, limits, remaining values, and reservation ID.

2. `POST /api/subscription/usage/settle`
   - Requires the reservation/operation ID.
   - Records outcome, actual tokens/units, provider/model, and actual or estimated cost.
   - Is idempotent: repeated settlement returns the original result and never increments twice.
   - Converts reserved amounts to final amounts and applies overage policy.

An explicit cancellation/failure settlement releases unused reservations while retaining the request audit event. Provider calls must await settlement. If settlement fails after provider work has occurred, WEB6 must return a visible metering failure and place the durable operation in a retryable outbox; it must not silently claim success with unrecorded billable usage.

### Authentication

- Bearer-token requests use the avatar resolved from the validated token.
- OASIS API keys must be per-avatar records resolved by WEB4. The current global WEB6 service key cannot identify a subscriber and must not authorize billable public calls.
- Internal calls use a separate service credential plus a signed, validated delegation token identifying the avatar. A plain avatar ID header is insufficient.
- Request-body, query, and header avatar IDs may be accepted only as resource targets after authorization; they cannot select the billing principal.

### Persistence

The provider layer currently has no compare-and-swap or atomic increment contract. A process-local semaphore does not restore the invariant across replicas. Add a dedicated `ISubscriptionUsageRepository` contract with atomic, idempotent operations, then implement it on a transactional production store before calling the system authoritative.

Required repository operations:

- create reservation if `OperationId` does not exist;
- atomically test and increment aggregate limits;
- settle once with optimistic versioning or a transaction;
- retrieve event and aggregate history;
- expire abandoned reservations through a documented state transition.

MongoDB can implement this with unique operation IDs, transactions, and conditional `$inc`; PostgreSQL can use unique constraints and row-level transactions. The OASIS provider abstraction may wrap either implementation, but a whole-settings-dictionary read/modify/write must not remain the billing store.

### Pricing and policy

- Move the versioned commercial pricing catalogue and plan quotas to WEB4 configuration/storage.
- Keep WEB6 provider adapters responsible for extracting actual charges.
- WEB4 records the catalogue version used for any estimate.
- Unknown categories or models must return a configuration error for billable production traffic rather than a generic price guess.
- Decide and document cache-hit, failed-call, retry, streaming, batch, and fan-out billing rules before migration.
- Karma can adjust quotas only through policy returned by WEB4. It must never be read from caller input or a stale plan claim.

## Migration sequence

1. Define billing semantics for cache hits, failed calls, streaming, retries, batch/fan-out, unit quantities, and pay-as-you-go.
2. Add `UsageEvent`, entitlement, reservation, settlement, aggregate, and pricing-catalogue contracts to API Core.
3. Implement and test `ISubscriptionUsageRepository` with cross-instance atomicity and idempotency.
4. Add WEB4 authorize, settle, event-history, aggregate, and pricing administration endpoints.
5. Bind OASIS API keys to avatars and reject unbound billable API-key requests.
6. Change the shared WEB4 client to return the authoritative plan, karma, and multidimensional remaining quotas.
7. Replace WEB6 preflight checks with WEB4 authorization. Remove plan/karma query arguments and JWT-claim quota logic.
8. Replace WEB6 settings writes and background tasks with awaited WEB4 settlement calls using operation IDs.
9. Route REST, streaming, OpenAI compatibility, WebSocket, GraphQL, gRPC, and MCP through the same metering application service.
10. Migrate existing aggregates once, reconcile the double-counted monthly bucket, and switch readers in the same release. Do not dual-write indefinitely.
11. Remove `UsageMeteringManager` persistence/quota code and the obsolete `RateLimitHeaderMiddleware`. Retain provider measurement helpers in a renamed WEB6 component.
12. Move warnings to WEB4 and expose consistent usage headers and APIs from the authoritative aggregate.

## Required test matrix

- every plan, including Enterprise unlimited;
- each karma boundary and negative karma;
- bearer and user-bound API-key identity;
- spoofed avatar IDs rejected as billing principals;
- authorize replay and settlement replay;
- simultaneous requests across multiple service replicas;
- exact-limit, over-limit, pay-as-you-go, and period reset;
- provider success, provider failure, cache hit, timeout, and cancellation;
- non-streaming, streaming, WebSocket, REST, GraphQL, gRPC, and MCP;
- token, image, video, speech, embedding, storage, batch, and fan-out quantities;
- actual provider cost, catalogue estimate, missing price, and catalogue-version changes;
- reservation expiry and durable settlement retry;
- Stripe upgrade, downgrade, cancellation, past-due, renewal, and webhook replay;
- usage summary and event-history reconciliation against aggregate totals.

## Immediate safety work before broader rollout

Until the full integration is implemented, do not treat WEB6's `subscription-usage` spend/token values as billing-grade. The first implementation change should stop WEB6 from incrementing WEB4's monthly request bucket, bind metering identity to the authenticated avatar, and prevent unbound API-key calls from reaching billable providers. Those three changes prevent double charging and cross-avatar attribution while the transactional usage repository is built.
