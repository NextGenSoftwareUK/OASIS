# CQL v1 milestone 2 — authenticated WEB4 boundary

22 October 2026. Implemented on `fix/hyperdrive-v2-providers`; native end-to-end certification remains milestone 3.

`POST /api/data/query-cql` compiles a bounded JSON description into the existing generic CQL plan. Server-selected MongoDB, SQLite or Neo4j identity is pinned before execution. The authenticated reservation/avatar must agree. Owner-or-public visibility is part of the native predicate before filtering and paging; metadata share indexes grant no access. Shared-recipient and tenant-specific authorization are outside this v1 contract.

The route enforces 32 KiB input, twelve filters, Skip 0–1000, Take 1–100, one concrete provider, a five-second cooperative cancellation deadline and one MiB serialized output. Unknown DTO members and unsupported operations fail before dispatch. The existing durable usage owner records attempts before native I/O and one explicitly configured service-query tariff receipt after a completed attempt. No guessed vendor price, native fee or implicit zero is introduced. Ordinary-only query profiles require no native quote credential; existing native profiles retain their quote/recovery requirements. Resource filtering supplies no-execution evidence when MVC rejects binding before dispatch.

## Verification

- Full Core: **1,676 passed, zero failed/skipped**, including 25 new WEB4 cases and the previous 16 admission cases.
- Existing real-Mongo accounting protocol: **312 passed, zero failed/skipped**, plus its separate one-case replica initialization. Existing owned MongoDB 8.3.11 stopped afterward; port 27029 clear.
- WEB4 application builds with zero errors; existing warnings remain. Both Core targets build. Final Core DLL SHA-256 matches its test, protocol and WEB4 consumers: `51DB795347AF0E09AED9F4D22874EBDB0EE7D79DF2440E84CF0173B73ED2503E`.
- Actual MVC routing/model binding/custom authorization is checked over awaited loopback Kestrel with a controlled authenticated identity/admission fixture. These tests do **not** constitute real JWT plus native-provider/outbox acceptance.

Fixed evidence: `C:/Source/OASIS/TestResults/CosmicCqlV1Web4/milestone-2-evidence.json`, `web4-core-final.trx`, `api-final-build.log`; protocol reports are under `TestResults/SubscriptionUsageCurrent/`. Earlier failed reports remain preserved: optional DTO nullability and ordinary-profile compatibility defects were fixed; an unconfigured protocol run failed mandatory environment checks and was replaced by the configured owned runner, not counted as passing. The runner's stale 310-case gate was corrected to the actually discovered 312 regression cases without excluding tests.

## Resume

Run the published HTTP subset through real native MongoDB, SQLite and Neo4j with JWT verification, durable usage records, visibility-before-paging, duplicate-operation and failure controls. Reuse the existing joint Mongo/Neo environment and journal fixtures before mutation. Complete operational acceptance and developer/operator guidance before claiming production-ready v1. Broader original CQL/ORM roadmap remains separate.

[HTTP request/configuration examples](CQL_V1_HTTP_CONTRACT.md), [dependency inventory](CQL_V1_WEB4_DEPENDENCIES.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
