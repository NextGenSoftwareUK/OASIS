# Subscription API

WEB4 is the only subscription, quota, budget and usage-ledger authority for WEB5–WEB10. See the [protocol and sequence diagram](../../WEB4_SUBSCRIPTION_USAGE_LEDGER.md) and [deployment/recovery runbook](../../WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md).

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

## Subscription and billing lifecycle

The controller also exposes:

- GET plans — public plan catalogue.
- POST checkout/session — authenticated Stripe checkout request.
- POST webhooks/stripe — verified Stripe webhook lifecycle.
- GET subscriptions/me and GET orders/me — authenticated subscriber records.
- POST toggle-pay-as-you-go — authenticated setting change; it does not bypass ledger budgets.
- GET usage — existing usage presentation, backed by the authoritative ledger.
- POST update-hyperdrive-config, GET hyperdrive-usage and POST check-hyperdrive-quota — HyperDrive configuration/read surfaces; they do not form a second WEB5–WEB10 accounting path.

Use the [executable Postman collection](WEB4-Subscription-Usage.postman_collection.json) against isolated staging subscribers. It covers each consuming service's reserve/start/settle sequence and retries. For credentials, live provider tests, outage injection, signed historical migration, Stripe cost-basis tags and retention, use the [operations runbook](../../WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md).
