# CQL scalar HTTP contract

23 October 2026. The scalar endpoint and shared source compiler are implemented. Core has 1,982 passing tests, including 18 scalar HTTP/compiler/admission controls. Signed three-provider transport certification passed: joint native 129/129, zero skips. Each store returned the exact unwindowed LongCount 137; source-window, private-root, private-branch and per-attempt settlement checks also passed. The expanded annotated demo passes:27SDK/27HTTP,182settled operations/181physical attempts, including scalar no-replay checks on all3 stores and generated Swagger coverage for both routes. The existing row route retains its default page size and strict request/response contract.

## Source and window ownership

`POST /api/data/query-cql/scalar` accepts explicit `Count`, `LongCount` or `Any` intent (default `LongCount`). Its result discriminator is `cql-scalar-v1`. Reuse the bounded root/direct-child/descendant filter vocabulary and the same immutable Core compiler. Do not accept executable expressions, plans, actor IDs, tenant grants, adapter profiles or arbitrary fields.

The scalar request has nullable `take`, default **unbounded**, with explicit bounded Skip and optional Take preserving Core window algebra. The existing `CqlV1Request.Take` defaults to25; wrapping that DTO unchanged would silently count at most25 roots. Fix this at the shared source-compilation boundary rather than stripping/overwriting a window after construction. Preserve the row DTO's default25 and its existing validation. Empty input/window semantics remain zero/false.

Graph output does not participate in a scalar count. If graph/consistency intent is accepted, preserve requested snapshot admission and independent relationship budgets; never hydrate children or lower input to the graph publication cap. Native instruction, memory and cancellation/deadline bounds still apply.

## Exact result representation

Return an explicit version/discriminator, concrete provider, operation and scalar domain. Count is an Int32 JSON integer; Any is a JSON Boolean. LongCount is a canonical invariant decimal **string** tagged Int64, including zero, so browser clients never round values above2^53 through JavaScript Number. Do not alternate integer/string encoding by value size.

```json
{
  "version": "cql-scalar-v1",
  "provider": "MongoDBOASIS",
  "operation": "LongCount",
  "valueType": "Int64",
  "value": "9007199254740993"
}
```

Keep the existing per-provider Core result/provenance. This first route resolves the current/default or one explicitly admitted concrete provider. All-provider/global deduplication and count federation remain the separately specified phase; do not sum replicated stores.

## Admission and accounting

Bind the actor from the existing authenticated WEB4 avatar/JWT and require the same exact prepared storage execution and reservation handle. Resolve existing providers only: no activation, implicit fallback, failover or client-supplied permission. Plan/re-admit the actual scalar interface and both source/scalar capabilities before dispatch. Unsupported row-only adapters must cause zero native invocation and affirmative no-execution evidence.

Use `HolonManager.ExecuteScalarQueryAsync` and the common query attempt owner. The durable physical attempt precedes native execution; completed empty/error/success results retain the normal explicit service-query receipt. Units represent the admitted query attempt, not the returned count. Do not invent provider invoices or replay an operation after an indeterminate outcome.

Retain strict unknown-member/body/filter/provider/domain limits, existing JSON/authentication rejection behavior, actor/handle affinity, no partial publication and the existing resource-filter no-execution owner. Scalar service code must not duplicate the ledger or evaluate downloaded rows.

## Required verification and demos

- Generic compiler controls preserve source access, relationships, scope, explicit windows and consistency, including omitted Take versus explicit Take25 and Take0.
- Hosted controls reject anonymous/foreign actor, wrong reservation, unsupported adapter/operation, unknown fields, malformed exact integers and request limits before dispatch.
- Signed native controls run on MongoDB, SQLite and Neo4j, with private/public/child traversal visibility, real counts above25/100, empty values, checked overflow, cancellation/error outcomes and durable receipts/no replay.
- Exact LongCount serialization above2^53 is checked from a controlled typed scalar result; do not seed billions of rows or fabricate native certification from a mocked count.
- Update spaced/commented C# and HTTP/browser examples after three-provider native and hosted certification. Use actual completed demo counts and preserve the existing six row examples.

[Core/native aggregate contract](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md), [row HTTP contract](CQL_V1_HTTP_CONTRACT.md), [roadmap](CQL_COMPLETION_ROADMAP.md).

## Request example

Use the same authenticated avatar and a new idempotency key as the row route.
Omit `provider` to use the server's active default. This example explicitly selects MongoDB; use `SQLLiteDBOASIS` or `Neo4jOASIS` unchanged otherwise.

```http
POST /api/data/query-cql/scalar
Authorization: Bearer <token>
Idempotency-Key: <new-logical-intent>
Content-Type: application/json

{
  "provider": "MongoDBOASIS",
  "operation": "LongCount",
  "filters": [
    {
      "metadataPath": ["category"],
      "scalar": "String",
      "operator": "Equal",
      "value": "coffee"
    }
  ]
}
```

No `take` means count the complete matching visible input, subject to native execution budgets. Explicit `take: 0` produces zero/false. Optional `skip` is 0–1,000 and optional `take` is 0–10,000. Source predicates, independent `anyChildren`/`anyDescendants` conditions and requested consistency use the same compiler as rows. The JSON example above shows the `result` payload inside the normal `OASISResult` envelope.

The startup tariff validator covers both exact routes; a nonzero scalar query tariff requires a sufficient reservation before serving traffic. Units represent one physical query attempt, including an empty result, rather than the returned count.
