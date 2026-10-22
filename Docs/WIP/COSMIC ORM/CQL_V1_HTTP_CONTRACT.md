# CQL v1 WEB4 contract — milestone 2

## Route and security

`POST /api/data/query-cql` requires the existing authenticated WEB4 bearer, a stable `Idempotency-Key`, and an explicitly configured prepared-storage route/reservation. Absent admission, absent query tariffs, a mismatched avatar/reservation, an inactive/unregistered provider or unsupported capability rejects before provider I/O. An eligible default is the existing active default, never an activation/failover instruction. Explicit providers are MongoDBOASIS, SQLLiteDBOASIS and Neo4jOASIS. One provider per request; grouped/global federation is outside HTTP v1.

The server adds `IsPublic || CreatedByAvatarId == authenticatedAvatarId` before the same native plan's user predicates, sort and page. The DTO cannot supply an actor, capability profile, plan, runtime type, SQL/Cypher or executable expression. Wizard does not bypass visibility. Authoritative shared-recipient list membership is not yet in this v1 language: shared-index metadata is not treated as an access grant. This follows OASIS's owner/globally-public model; it does not invent a tenant field or certify tenant isolation for applications needing a separate tenant policy. Native acceptance must prove this predicate on all three actual providers before the feature is certified production-ready.

A second result check refuses unexpected private rows; it is not a replacement for native access predicates or an in-memory page filter. Leaf DTOs omit provider keys/revisions, runtime objects and children. Metadata is optional and remains subject to holon read visibility; do not mark private secrets public.

## Request example

```json
{
  "provider": "SQLLiteDBOASIS",
  "filters": [
    { "metadataPath": ["Category"], "scalar": "String", "operator": "Equal", "value": "coffee" },
    { "metadataPath": ["Rating"], "scalar": "Integer", "operator": "GreaterThanOrEqual", "value": 7 }
  ],
  "anyOf": [
    { "field": "Name", "operator": "Equal", "value": "Espresso" },
    { "metadataPath": ["location", "city"], "scalar": "String", "operator": "Equal", "value": "London" }
  ],
  "orderBy": "Name",
  "descending": false,
  "skip": 0,
  "take": 25,
  "includeMetadata": false
}
```

`Filters` are ANDed; a nonempty `AnyOf` adds one parenthesized OR group, also ANDed with the mandatory visibility restriction. Canonical filter fields: Id, Name, Description, Version, IsActive and ParentHolonId. Metadata paths accept one to four segments. Metadata scalar types: String, Integer (exact Int64), Boolean, Guid (canonical D text). Canonical types come from their declared property, not the request's Scalar. Operators: equality/inequality, supported ordered comparisons, string Contains/StartsWith/EndsWith, metadata Exists/IsNull. Existence/null tests omit Value. Unsupported scalar/operator/provider combinations are planning errors, not client-side evaluation. Metadata decimals/doubles, null-valued comparison syntax and arbitrary property names are outside this transport subset.

OrderBy is Name, Version or Id, followed by logical Id as a deterministic tie-breaker. Maximum 12 total filter entries; Skip 0–1000, Take 1–100 (default 25). Literal strings are at most 512 UTF-16 code units and metadata segments 64. Unknown DTO fields reject. Request body maximum 32 KiB is applied before usage fingerprint buffering as well as MVC binding. Output JSON maximum one MiB is enforced while writing to a bounded stream; oversize output returns 413 without a partial result. Cancellation is propagated with a five-second cooperative deadline; actual driver deadlines/cancellation require milestone 3 verification, not an assumption of hard interruption.

## Accounting and activation

Reuse the existing `SUBSCRIPTION_USAGE_ENABLED`, local WEB4 reservation authority, Mongo usage outbox, `Web4UsageLedgerMiddleware`, prepared-storage middleware and settlement worker. No second billing ledger or self-HTTP call is introduced. Publish/configure the actual current schema/pins using the existing operator process; code alone does not turn on a route or select a paid tariff.

Ordinary database queries can use an ordinary-query-only `PREPARED_STORAGE_ADMISSION_JSON` profile with explicit empty NativeProviders, permitted OrdinaryProviders and QueryTariffs. This mode requires no CoinGecko credential or native fee service. Native bindings retain their existing explicit quote/recovery requirements. The following is a **schema illustration, not an approved commercial price**:

```json
{
  "nativeProviders": [],
  "ordinaryProviders": ["MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS"],
  "queryTariffs": [
    { "provider": "MongoDBOASIS", "costUsd": 0.002, "catalogueVersion": "operator-approved-v1" },
    { "provider": "SQLLiteDBOASIS", "costUsd": 0.002, "catalogueVersion": "operator-approved-v1" },
    { "provider": "Neo4jOASIS", "costUsd": 0.002, "catalogueVersion": "operator-approved-v1" }
  ],
  "routes": [{
    "method": "POST",
    "path": "/api/data/query-cql",
    "policy": {
      "meterCategory": "cql.query",
      "reservedUnits": 1,
      "estimatedCostUsd": 0.002,
      "fixedCostUsd": 0,
      "pricingCatalogueVersion": "operator-approved-v1",
      "requiresProviderMeasurement": true,
      "maximumProviderAttempts": 1,
      "maximumNativeFeeCostUsd": 0
    }
  }]
}
```

The provider tariff must fit the reservation and match its explicit catalogue. Zero is allowed only as an explicit operator tariff, never a fallback or invented provider invoice. A completed ordinary query attempt records one immutable receipt bound to the exact durable physical attempt, including empty results and returned provider errors. This is service/API query pricing, not the database vendor's measured invoice. Thrown/indeterminate invocation or failed receipt persistence cannot invent successful completion evidence: the existing reconciliation path remains authoritative. The result body is not published until the existing middleware durably processes settlement. Model-binding/validation failures before dispatch record explicit no-provider-execution evidence, including actions short-circuited by MVC.

Duplicate operation identities follow the existing durable protocol: they do not silently repeat provider work. Retain the `X-OASIS-Operation-Id` and use existing operation/reconciliation endpoints for an uncertain result; do not create fresh keys simply to retry an uncertain provider operation.

## Acceptance status

Controlled compilation, policy, DTO, accounting and MVC checks are recorded in the milestone report. Real JWT/authority/outbox/native-provider acceptance and deployment/operational certification remain milestone 3 work. No production account, price, deployment or native database is created merely by these tests.
