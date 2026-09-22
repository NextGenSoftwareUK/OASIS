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

Use the [Subscription API](API%20Documentation/WEB4%20OASIS%20API/Subscription-API.md), [Postman collection](API%20Documentation/WEB4%20OASIS%20API/WEB4-Subscription-Usage.postman_collection.json), and [test commands/live release gates](WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md). Change deployed submodule gitlinks and `Docker/oasis-dependency-versions.env` together and run the required validators and all seven publishes.
