# WEB4 subscription authority for WEB5–WEB10

WEB4 owns all subscription entitlements, quotas, budgets, usage reservations, settlement and billing-facing totals. WEB5–WEB10 use API Core's shared durable usage SDK. The compatibility `authorize-request` endpoint is retired (HTTP 410), and no consumer may fall back to it.

The [complete protocol](WEB4_SUBSCRIPTION_USAGE_LEDGER.md) defines reserve → durable execution claim → start → provider execution → settle, with the sequence diagram, state machine, idempotency, concurrent transactions, immutable audit and recovery behavior. The [operations runbook](WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md) covers credentials, pricing, historical opening balances, all-service tests, reconciliation and rollout.

## Customer and service flow

1. The customer selects a plan and completes WEB4's Stripe checkout.
2. WEB4 verifies the Stripe webhook and persists the authoritative subscription.
3. A consuming service receives a validated user bearer and stable Idempotency-Key.
4. It authorizes with WEB4 using the user bearer plus its independent service credential.
5. It persists a unique execution claim, obtains WEB4 start acknowledgement, performs bounded work and records actual receipts.
6. It durably queues and synchronously settles the terminal outcome; WEB4 acknowledgement precedes success.
7. If delivery fails, the original payload remains in the durable outbox and the caller receives visible pending status.

Enterprise provisioning must update WEB4's authoritative subscription through a secured administrative workflow. JWT plan hints and consumer environment allowlists do not grant entitlement. Provider-specific pricing/measurement remains in the owning service; WEB4 alone makes subscription and budget decisions.

## Configuration and verification

`WEB4_API_BASE_URL` and a distinct per-service credential are required on each consumer. Durable Mongo outboxes reuse the deployed OASIS DNA connection and an explicit OTLP collector endpoint is required when telemetry export is enabled. WEB4 requires its transaction-capable ledger store and service keys. There is no implicit production URL or local quota fallback. WEB6 uses its existing reviewed, versioned `ModelCatalogueManager` prices.

For development WEB5, configure `WEB4_API_BASE_URL=https://dev.api.web4.oasisomniverse.one`. A successful direct development WEB4 authorization paired with WEB5 `SUBSCRIPTION_AUTHORITY_UNAVAILABLE` requires checking WEB5's deployed authority URL and credentials. Correct its configuration and redeploy; do not bypass WEB4 in the client, middleware or quest seeder. The former production-default URL diagnosis applies to old consumers; this protocol requires the URL explicitly. Use the isolated-subscriber live runner for a complete authorize/start/settle diagnostic.

The Mongo persistence bucket has exactly one `_id` mapping and remains separate from the public aggregate DTO. Duplicate inherited `[BsonId]` members can make every request fail before quota evaluation; the mapping regression test locks this invariant in. Deploy a corrected WEB4 and verify its protocol before resuming an idempotent development reseed.

WEB4 resolves the avatar from the bearer token, loads its subscription, checks status and expiry, checks the monthly plan limit, and records one request. The response includes `allowed`, `statusCode`, `code`, `message`, `planId`, `currentUsage`, `limit`, and `remaining`. A limit and remaining value of `-1` means unlimited Enterprise usage.

Consumers return the decision status when access is denied. If WEB4 cannot be reached or returns an invalid transport response, consumers return `503 SUBSCRIPTION_AUTHORITY_UNAVAILABLE`; they do not silently permit the request and do not consult a local entitlement store.

Unauthenticated requests continue to the normal authentication middleware so each API preserves its existing public-route and authentication behavior. Swagger, health, favicon, and OpenAPI routes bypass subscription accounting.

## Configuration

Set `WEB4_API_BASE_URL` on WEB6-WEB10. WEB5 also accepts its existing `WEB4_OASIS_API_BASE_URL` name during the environment-variable naming migration. The production default is `https://api.web4.oasisomniverse.one`.

For the development WEB5 service, set
`WEB4_API_BASE_URL=https://dev.api.web4.oasisomniverse.one`. If a bearer token
receives `AUTHORIZED` from development WEB4 directly but development WEB5 still
returns `SUBSCRIPTION_AUTHORITY_UNAVAILABLE`, test the same token against
production WEB4. A matching production-side failure proves that WEB5 is using
the production default; correct the service variable and redeploy WEB5 rather
than adding a client or middleware bypass.

WEB6 retains its AI token and provider-cost metering. That data measures model usage and cost; it does not determine the user's OASIS subscription entitlement or monthly cross-service request allowance.

## Verification

Run:

```powershell
python Scripts/validate_web4_subscription_authority.py
python Scripts/validate_railway_dependency_manifest.py --require-gitlinks
```

The first check ensures every WEB5-WEB10 middleware uses the shared WEB4 authorization client and that WEB5's former local subscription service is absent. The dependency check ensures Railway builds use the exact API Core, STAR ODK, and WEB6 commits recorded in `Docker/oasis-dependency-versions.env`.

The ONODE WebAPI subscription tests also validate that the Mongo usage aggregate
document has exactly one `_id` mapping. Keep the persistence document separate
from `SubscriptionUsageAggregate`; hiding an inherited `Id` with another
`[BsonId]` makes MongoDB reject every authorization request before quota policy
can run. The consumer then correctly surfaces `503
SUBSCRIPTION_AUTHORITY_UNAVAILABLE`.

When that response blocks a development reseed, verify WEB4 directly before
changing subscription data or client behavior:

```http
POST https://dev.api.web4.oasisomniverse.one/api/subscription/authorize-request
Authorization: Bearer <development avatar JWT>
Content-Type: application/json

{"consumingService":"WEB5"}
```

The prerequisite is a successful WEB4 decision response. Deploy the corrected
ONODE WebAPI to development, verify this request, and only then resume the
idempotent seeder; do not bypass the authority in WEB5 or the seed scripts.
Use the [Subscription API](API%20Documentation/WEB4%20OASIS%20API/Subscription-API.md), [Postman collection](API%20Documentation/WEB4%20OASIS%20API/WEB4-Subscription-Usage.postman_collection.json), and [test commands/live release gates](WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md). Change deployed submodule gitlinks and `Docker/oasis-dependency-versions.env` together and run the required validators and all seven publishes.
