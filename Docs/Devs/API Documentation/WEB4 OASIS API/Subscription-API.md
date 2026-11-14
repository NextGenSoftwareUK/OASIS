# Subscription API

WEB4 is the only subscription, quota, budget and usage-ledger authority for WEB5–WEB10. See the [protocol and sequence diagram](../../../WIP/Subscriptions/WEB4_SUBSCRIPTION_USAGE_LEDGER.md) and [deployment/recovery runbook](../../../WIP/Subscriptions/WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md).

## Usage protocol

All paths below are relative to `/api/subscription`. Public clients use the consuming service with a validated bearer and stable `Idempotency-Key`; only trusted consuming services call WEB4's mutating protocol directly.

| Method/path | Authentication | Result |
|---|---|---|
| POST usage/authorize | User bearer + service credential | Atomic reservation and authoritative owner/limits/status/expiry |
| POST usage/start | Owning service credential | Durable execution-start acknowledgement |
| POST usage/settle | Owning service credential | Atomic measured settlement; identical replay returns alreadySettled |
| GET usage/current | User bearer | Current authoritative month/day usage, reserved exposure and limits |
| GET usage/events?limit=100 | User bearer | Operation projections for this subscriber |
| GET usage/audit?limit=100 | User bearer | Append-only accounting history |
| POST authorize-request | — | 410 USAGE_PROTOCOL_REQUIRED; retired, never increments usage |

Service authentication uses `X-OASIS-Service: WEB6` and `X-OASIS-Service-Key: <deployment secret>`. Each service has a distinct key. WEB4 derives authorize's avatar from validated bearer identity and checks start/settle ownership against the stored reservation. User-supplied avatar, plan or karma cannot override that identity.

### Authorize

```json
{
  "operationId": "2f516ca8-ce84-4f20-a020-f35e36d9b485",
  "consumingService": "WEB6",
  "endpoint": "POST /v1/chat/completions",
  "meterCategory": "ai.tokens",
  "requestedUnits": 256,
  "estimatedCostUsd": 0.01,
  "requestFingerprint": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
}
```

This is a payload illustration, not a provider price quotation. The consumer computes a conservative upper bound from its reviewed catalogue and permitted provider inputs/output/fan-out. The response contains allowed, statusCode, code, operationId, userId, alreadyAuthorized, status, expiresAtUtc, planId, karma and remaining quotas. Replays with changed fingerprint or accounting fields return 409.

### Start

After persisting the unique execution claim, send:

```json
{
  "operationId": "2f516ca8-ce84-4f20-a020-f35e36d9b485",
  "userId": "THE_OWNER_RETURNED_BY_AUTHORIZE",
  "consumingService": "WEB6"
}
```

The response contains started, alreadyStarted, operationId and status. Do not execute the provider before started acknowledgement. Expired/unowned operations cannot start.

### Settle

Send operationId, userId, consumingService, outcome, provider, model, providerRequestId, promptTokens, completionTokens, units, estimatedCostUsd, **actualCostUsd**, costSource, pricingCatalogueVersion and providerReceipts. Each provider receipt contains provider/model, immutable providerRequestId, measured prompt/completion tokens/units, actualCostUsd, costSource and pricingCatalogueVersion. Receipt totals must exactly equal settlement totals, with no repeated provider receipt IDs.

Outcomes are succeeded, failed and cancelled. Costs already incurred remain billable on failure/cancellation. Missing actual measurement is an explicit error; an estimate alone cannot settle. The immutable payload is queued durably before synchronous delivery. WEB4 acknowledgement precedes terminal success. Authority outage returns visible SETTLEMENT_PENDING while the worker retries the same payload.

### Failure codes

| HTTP | Meaning |
|---|---|
| 400 | Missing/invalid key, UUID, fingerprint, dimensions, bounds or receipt evidence |
| 401 | Missing/invalid user or service authentication |
| 402 | Required subscription absent, inactive or expired |
| 403 | Administrative access denied |
| 404 | Unknown operation |
| 409 | Ownership/idempotency/receipt conflict or illegal state transition |
| 410 | Retired authorize-request compatibility counter |
| 429 | Authoritative quota/token/budget exhausted |
| 503 | Authority/outbox unavailable, settlement pending or unresolved measurement |

Idempotent repeats preserve original accounting. New client keys represent new work. Read the original operation/business result when retry returns an existing-operation conflict; do not change the key merely to bypass it.

## Administration and reconciliation

These routes require a validated administrator claim plus the WEB4 allowlist:

| Method/path | Purpose |
|---|---|
| POST usage/admin/corrections | Append correctionId, operationId, correctionKind, costDeltaUsd, unitsDelta, reason, evidenceReference; provider-measurement also requires provider/providerRequestId |
| GET usage/admin/migration/inventory | Read historical source digest and migration status |
| POST usage/admin/migration/opening-balance | Import reviewed signed opening-balance manifest |
| POST usage/admin/reconciliation/provider-receipts | Append independently obtained provider evidence |
| POST usage/admin/reconciliation/stripe-invoices | Fetch/validate a finalized Stripe usage-only invoice |
| POST usage/admin/reconciliation/run | Reconcile userId and UTC month |
| GET usage/admin/reconciliation/reports | Read persisted reports by userId/month |

Reports use OASISResult envelopes. A successful check can contain findings; inspect Balanced, Findings, provider/Stripe evidence completeness and unresolved operations. Reconciliation never silently changes ledger balances. Corrections append history; they do not rewrite original provider evidence.

Correction kinds are explicitly `billing-adjustment` or `provider-measurement`. The latter must name a receipt already belonging to that operation. Only its receipt-specific delta changes the expected provider amount during independent reconciliation; general billing adjustments change ledger and billing totals without claiming that a provider measurement changed.

### Plan grants and app accounts

Under `/api/subscription/admin/` (administrator claim plus allowlist, or the `Wizard` avatar). Every change is audited with the administrator and a reason (at least 5 characters).

| Method/path | Purpose |
|---|---|
| POST plan-grant | Put an avatar on free, bronze, silver, gold or enterprise without Stripe: `{ userId, planId, reason, expiresAtUtc? }` (expiry at most 10 years). 409 if the avatar has an active Stripe subscription; granting `free` reverts. |
| GET app-accounts | List app accounts (keys are never returned) |
| POST app-accounts | Create an app account `{ appId, name, planId, reason, pooledQuota?, perUserPlanId? }`. The response contains `apiKey` once; only its hash is stored. `appId` is 3 to 40 lowercase letters, digits or hyphens. |
| POST app-accounts/{appId}/rotate-key | Issue a new key; the old one stops working |
| POST app-accounts/{appId} | Change `planId`, `status` (`active`, `disabled`), and for finite plans `pooledQuota` / `perUserPlanId` |
| GET app-accounts/{appId}/top-users?limit=10 | The avatars using the most of a pooled app's quota this UTC month, busiest first: `userId`, `requests`, `units`, `settledCostUsd` (limit 1 to 100). 404 if the app does not exist. |
| GET app-accounts/{appId}/usage | Pool usage for a pooled app: this month and today against the plan limits, with used fractions (`monthlyRequestsUsedFraction`, `dailyCallsUsedFraction`) `poolActive` and a `status` of `ok`, `warning` (80% of a limit) or `exhausted` (100%). 404 if the app does not exist. |

**Calling with an app account.** An application's server may add `X-OASIS-App-Id` and `X-OASIS-App-Key` to a request that carries the user's bearer token. WEB4 then evaluates the request against the more generous of the app's plan and the user's own plan, so an app never makes a user worse off. Invalid, partial or disabled-app credentials are rejected with 401; requests without these headers behave exactly as before. Usage and settlement stay recorded against the signed-in avatar. By default the app plan is a per-user entitlement, so app accounts accept only the `enterprise` plan. With the server setting `OASIS_POOLED_APP_QUOTAS=true`, a finite plan may be created with `pooledQuota: true` (and optional `perUserPlanId`, default `silver`): it is then one pool shared by all the app's users, and exhaustion is denied with `APP_`-prefixed codes such as `APP_MONTHLY_REQUEST_LIMIT_EXCEEDED`. `GET app-accounts/{appId}/usage` shows the pool against its limits and `GET app-accounts/{appId}/top-users` shows who is using it. The metric `subscription.ledger.pool_threshold` fires at 80% and 100%. `OASIS_POOL_SHARDS` (1 to 16, default 1) spreads a very busy pool over several documents; with more than one shard the limit can be overshot by the requests in flight at that instant. The headers are accepted both on WEB4's own authorization path and on `usage/authorize` (used by WEB5 to WEB10); the shared client in OASIS-API-Core (used by WEB5 to WEB10) sends them on `usage/authorize` when both the `OASIS_APP_ID` and `OASIS_APP_KEY` environment variables are set on the calling service, and sends neither otherwise.

## Subscription and billing lifecycle

The controller also exposes:

- GET plans — public plan catalogue.
- POST checkout/session — authenticated Stripe checkout request.
- POST webhooks/stripe — verified Stripe webhook lifecycle.
- GET subscriptions/me and GET orders/me — authenticated subscriber records.
- POST toggle-pay-as-you-go — authenticated setting change; it does not bypass ledger budgets.
- GET usage — existing usage presentation, backed by the authoritative ledger.
- POST update-hyperdrive-config, GET hyperdrive-usage and POST check-hyperdrive-quota — HyperDrive configuration/read surfaces; they do not form a second WEB5–WEB10 accounting path.

Use the [executable Postman collection](WEB4-Subscription-Usage.postman_collection.json) against isolated staging subscribers. It covers each consuming service's reserve/start/settle sequence and retries. For credentials, live provider tests, outage injection, signed historical migration, Stripe cost-basis tags and retention, use the [operations runbook](../../../WIP/Subscriptions/WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md).

## Usage visibility, owners and warnings

Charts and alerts for people, app owners and administrators, built on the same day and month buckets as the usage ledger (read only, no request content). A person sees only their own usage, an app owner sees only the apps they own, and a Wizard sees everything.

### Usage series (charts)

| Endpoint | Who | Returns |
|---|---|---|
| `GET /api/subscription/usage/series?from=2026-11-01&to=2026-11-13` | the signed-in avatar | its own daily requests, daily and monthly limits, today, month total and status |
| `GET /api/subscription/admin/users/{userId}/usage/series` | Wizard | any avatar's series |
| `GET /api/subscription/admin/app-accounts/{appId}/usage/series` | Wizard | an app pool's series plus its pool usage and limits |

Dates are UTC `yyyy-MM-dd`. The default is the last 30 days, a reversed range is swapped, the end is clamped to today and a range longer than 92 days is rejected (400). Missing days are returned as zero. `status` is `ok`, `warning` (from 80% of the monthly limit) or `exhausted`, and empty when the plan has no limit.

### App owners

An administrator links one or more avatars (at most 10) to an app account: `POST /api/subscription/admin/app-accounts/{appId}/owners` with `{ "ownerAvatarIds": ["..."], "reason": "..." }` (audited). Owners then use:

| Endpoint | What it does |
|---|---|
| `GET /api/subscription/app-owner/apps` | The apps I own. |
| `GET /api/subscription/app-owner/{appId}/usage` | Pool size, used this month and today, daily series, which people used what (usernames resolved, no content) and the app's alerts. Anyone who does not own the app gets 403. |
| `GET` / `POST /api/subscription/app-owner/{appId}/settings` | This app's own on/off choices (see overrides below). Audited. |
| `GET` / `POST /api/subscription/app-owner/{appId}/notifications` | The owner's warning thresholds. |

### Warnings

`GET /api/subscription/usage/alerts` returns the person's own alert and one per app they own, once the chosen thresholds (default 80% and 100%) are reached; the portal shows it as a banner that can be dismissed for the day. `GET` / `POST /api/subscription/usage/notifications` read and change `thresholds` (1 to 4 whole percents), `emailRequested` (default true) and `warningsEnabled` (null follows the node default, true or false is the person's own choice). Every change is recorded in the audit trail.

Email reuses the existing OASIS email settings (`OASIS.Email`): one warning email per threshold per month, only when email is configured and the person has not opted out. Nothing is sent otherwise.

### Overrides (a person or app can opt in or out)

The node-wide defaults live in `OASIS_DNA.json` (below). Each person and each app account can override them for themselves, in either direction, in the portal: warnings (person and app), the owner view (app) and the access log (app). The override wins over the DNA value, `null` means follow the default.

### App pay-as-you-go (billed monthly)

`GET` / `POST /api/subscription/app-owner/{appId}/payg` show and change an app pool's pay-as-you-go. When it is on, requests above the pool's included monthly allowance keep working (instead of being refused) and are billed.

**Turning it on** needs the owner's consent and a monthly USD cap (from 0.50, whole cents), a price for the app's plan in `Configuration/app-overage-prices.json` (same per-request rates as individuals; the free plan has none), and the consenting owner must have an active paid subscription with a default payment method in Stripe (checked when enabling). The Stripe account of the person who turns it on is the one invoiced.

**What happens**
- **Admission:** once the allowance is used up, each extra request is admitted only while the month's extra charge stays within the cap, the owner has no unpaid app invoice, and every other limit (daily calls, tokens, budget) still passes. Otherwise it is refused as before (`APP_MONTHLY_REQUEST_LIMIT_EXCEEDED`).
- **Statement:** each month after it closes, the extra requests (net of any released requests) times the rate, limited to the cap, become one immutable statement. It is created once per app and month, and the existing overage worker collects it as a separate USD Stripe invoice (never part of the monthly subscription).
- **Failure:** if payment fails, extra usage is paused until the invoice is paid.
- **Owner view:** the GET returns the price, consent and cap, this month's estimate (`estimate`) and the statements with their invoice links. Statements and consent changes are audited.

Individuals get the same kind of figure at `GET /api/subscription/usage/estimate` (request charges so far and projected to month end at the current pace, against their cap; AI cost above the included budget is billed separately and is not estimated).

### DNA settings (`OASIS.UsageVisibility`)

| Key | Default | Meaning |
|---|---|---|
| OwnerViewEnabled | true | Owners can open usage for their own apps. |
| WarningsEnabled | true | Usage warnings and the banner. |
| EmailEnabled | true | Email the warnings (needs `OASIS.Email` configured). |
| DefaultWarningPercents | [80, 100] | Thresholds used until someone chooses their own. |
| AppPayAsYouGoEnabled | true | Settings and display for app pay-as-you-go (charging is not built). |

On Railway edit the `OASIS_DNA_JSON` variable; keep every DNA copy in sync.
