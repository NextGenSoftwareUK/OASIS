# CQL v1 WEB4 contract

[Manual](CQL_MANUAL.md) · [Architecture](CQL_ARCHITECTURE.md) · [Three-provider demos](CQL_DEMO_RUNBOOK.md).

## Route and security

`POST /api/data/query-cql` requires the existing authenticated WEB4 bearer, a stable `Idempotency-Key`, and an explicitly configured prepared-storage route/reservation. Absent admission, absent query tariffs, a mismatched avatar/reservation, an inactive/unregistered provider or unsupported capability rejects before provider I/O. An eligible default is the existing active default, never an activation/failover instruction. Explicit providers are MongoDBOASIS, SQLLiteDBOASIS and Neo4jOASIS. One provider per request; grouped/global federation is outside HTTP v1.

The server adds `IsPublic || CreatedByAvatarId == authenticatedAvatarId` before the same native plan's user predicates, sort and page. The DTO cannot supply an actor, capability profile, plan, runtime type, SQL/Cypher or executable expression. Wizard does not bypass visibility. Authoritative shared-recipient list membership is not yet in this v1 language: shared-index metadata is not treated as an access grant. This follows OASIS's owner/globally-public model; it does not invent a tenant field or certify tenant isolation for applications needing a separate tenant policy. Native HTTP acceptance exercises this predicate against MongoDB, SQLite and Neo4j.

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

OrderBy is Name, Version or Id, followed by logical Id as a deterministic tie-breaker. Maximum 12 total root/child filter entries and four anyChildren groups; Skip 0–1000, Take 1–100 (default 25). Literal strings are at most 512 UTF-16 code units and metadata segments 64. Unknown DTO fields reject. Request body maximum 32 KiB is applied before usage fingerprint buffering as well as MVC binding. Output JSON maximum one MiB is enforced while writing to a bounded stream; oversize output returns 413 without a partial result. Cancellation is propagated with a five-second cooperative deadline; existing bounded native driver behavior remains authoritative, rather than a promise of hard interruption at precisely five seconds.

String operations use explicit ordinal semantics. Exists includes a present null member; IsNull requires a present null member; missing paths satisfy neither. Exact Int64 behavior is checked above 2^53. JavaScript clients must preserve exact integer JSON: ordinary Number values cannot represent every Int64. Native typed comparisons do not coerce numeric strings or treat invalid GUID data as satisfying typed inequality.

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

See [native acceptance](CQL_V1_MILESTONE_3.md) and [developer/operator instructions](CQL_V1_OPERATIONS.md). The shipping JWT validator and HTTP boundary execute against all three actual native databases and a real Mongo usage outbox. Avatar-directory resolution and reservation/settlement authority are controlled fixture components; existing accounting regression is verified separately. These checks do not boot the entire production application, approve commercial prices, create production accounts or deploy the route. The larger CQL roadmap remains separate from this deliberately bounded v1 contract.


## Optional access-bound child graph

Omit `graph` for the existing leaf response. An explicit graph applies the server-selected owner/public policy to every native child command before limits. The three adapters must declare `GraphReadAccess` as well as `GraphExpansion`; the actual adapter is re-admitted before dispatch. Root filtering/sort/page precede expansion.

```json
{
  "provider": "SQLLiteDBOASIS",
  "filters": [{ "field": "Name", "operator": "Equal", "value": "C-public" }],
  "take": 1,
  "graph": {
    "recursive": true,
    "maxChildDepth": 2,
    "childType": "All",
    "maximumNodes": 256,
    "maximumPayloadBytes": 1048576,
    "consistency": "ProviderDefault"
  }
}
```

Graph defaults: recursive=true, depth2, typeAll, nodes256, payload1MiB, ProviderDefault. Public limits are depth1–8, nodes1–1000 and payload1–1048576 bytes; exact enum names are required. SDK depth0 retains its bounded unbounded-recursion meaning, but is rejected by this public transport. Root depth is0; recursive=false selects immediate children only. Budgets reject the whole result rather than silently truncate it. The serialized response still has a one-MiB bound and five-second cooperative endpoint deadline.

Graph rows add `children` arrays containing only visible selected child DTOs. Stored role manifests, provider keys/revisions, hidden child counts and runtime objects are omitted. Graph roots use empty ParentHolonId for ancestors outside the returned view; child ParentHolonId points to the visible parent. Leaf shape remains unchanged. Inaccessible intermediate children are not traversed even when a descendant is public. Metadata sharing labels do not grant access. Optional metadata has the same business-data meaning as the existing leaf contract.

ProviderSnapshot is explicitly supported by SQLite/Mongo within one provider; Neo rejects it before I/O. ProviderDefault preserves each provider's documented guarantee and never promises a distributed common snapshot. One provider query attempt/receipt includes its bounded native child reads under the configured service-query attempt tariff; it is not a per-child fee or an assertion about external provider billing.

Signed route acceptance passes on Mongo, SQLite and Neo: Core1781/Shared960/Mongo23/SQLite19/joint86 and executable demo1, zero skips. See dated graph evidence for exact scope and hashes.

## Direct-child existence over HTTP

`anyChildren` accepts at most four independent groups. Each group has `filters` (AND) and `anyOf` (one local OR), requires at least one filter and uses the same canonical fields, typed metadata paths and operators as root filters. The twelve-entry budget is shared across all root and child filters. Groups are ANDed and may match different visible direct children. Nested/descendant groups are not accepted.

```json
{
  "provider": "MongoDBOASIS",
  "filters": [
    { "metadataPath": ["fixture"], "scalar": "String", "operator": "Equal", "value": "your-fixture-tag" }
  ],
  "anyChildren": [
    { "filters": [{ "field": "Name", "operator": "Equal", "value": "public child" }] },
    { "filters": [{ "field": "Name", "operator": "Equal", "value": "owned child" }] }
  ],
  "orderBy": "Name",
  "take": 1
}
```

The authenticated actor is server-owned and applies to roots and matching children. Private children and untrusted SHARED_WITH metadata cannot grant a match. Native membership, current parentage and actor predicates run before root paging on MongoDB, SQLite and Neo4j. This selects parents without loading graphs; optional `graph` may independently expand the selected roots. Each new logical request still requires its own idempotency key and prepared usage admission.

## Bounded descendant existence over HTTP

Optional anyDescendants groups compile to generic WhereAnyDescendant before root ordering/paging. Existing v1 requests remain valid. Direct-child and descendant groups share a maximum of four groups and the twelve-filter budget with roots. Each nonempty group ANDs Filters and its local AnyOf disjunction; separate groups may match different nodes.

```json
{
  "provider": "MongoDBOASIS",
  "filters": [],
  "anyDescendants": [
    {
      "maximumDepth": 2,
      "maximumNodes": 250,
      "filters": [
        { "field": "Name", "operator": "Equal", "value": "coffee" }
      ]
    }
  ],
  "orderBy": "Name",
  "take": 10
}
```

Depth is1–32 (default8); nodes1–10000 (default1000), separately from response Graph limits. Traversal does not request graph output. Malformed/null/empty groups, excessive combined groups/filters or out-of-range bounds fail validation before native execution. Actor identity is server-bound; no group can override it. A private intermediate prunes the branch before visiting/counting even a public grandchild. Exact integer metadata literals retain Int64 semantics. Actual adapter capability/bounds admission remains mandatory.

Full Core1830 and signed joint109 pass unskipped. The existing owned fixture verifies owner/outsider requests on Mongo/SQLite/Neo, public descendants before paging, public-behind-private denial and exact visible-node budgets. The fixture adds read operations only; no new dataset or ownership format is needed. Fresh78364 terminal0 demo passes18 SDK/18 HTTP requests,152 settled operations/151 attempts; joint109 remains green.
