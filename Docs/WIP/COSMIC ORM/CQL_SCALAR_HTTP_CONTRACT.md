# CQL scalar HTTP contract

Current verification:2,117Core/HTTP tests and175combined native regression tests pass with zero skips; fresh12-example walkthroughs pass36SDK/36HTTP on allthree stores,216settled operations/211physical attempts. The verified selected-extrema gate below supersedes earlier historical checkpoints.

## Source and window ownership

`POST /api/data/query-cql/scalar` accepts explicit `Count`, `LongCount`, `Any`, `Min` or `Max` intent (default `LongCount`). Its result discriminator is `cql-scalar-v1`. Reuse the bounded root/direct-child/descendant filter vocabulary and the same immutable Core compiler. Do not accept executable expressions, plans, actor IDs, tenant grants, adapter profiles or arbitrary fields.

The scalar request has nullable `take`, default **unbounded**, with explicit bounded Skip and optional Take preserving Core window algebra. The existing `CqlV1Request.Take` defaults to25; wrapping that DTO unchanged would silently count at most25 roots. Fix this at the shared source-compilation boundary rather than stripping/overwriting a window after construction. Preserve the row DTO's default25 and its existing validation. Count/existence empty input/window semantics remain zero/false. Min/Max require one bounded Selection: canonical field or typed metadata path. Missing/null nullable/string inputs are ignored; empty nullable/string results are null and empty required results report CQL_EMPTY_SEQUENCE. Wrong participating domains error even if another value would win.

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


## Selected Min/Max HTTP checkpoint — 23 October 2026

Implemented bounded Selection for Min/Max on the existing scalar route. Count/LongCount/Any reject Selection; Min/Max require it. Projection and scalar HTTP share one typed bounded selector compiler through a generic consumer, without reflection/dynamic invocation or executable delegates. Actual adapter selection/read permissions remain authoritative. Selected values reuse the existing exact projection wire encoder (Int64 decimal text, Guid D, UTC O, nullable null), five-second response checks, one-attempt receipt owner and atomic1MiB output limit.

Full Core/HTTP tests pass **2,117, zero skips**, including11new selected endpoint controls. Browser JS syntax passes. SDK/HTTP walkthrough source now has12 spaced/commented examples/provider, with Min/Max over signed exact Int64 data; the runner requires fresh36SDK/36HTTP. Browser presets and Swagger include selected extrema.

Native verification is **not complete**: combined175case run passed172/failed3. Signed all-store test failed at a late stale Swagger assertion (expected LongCount; sample now Min), after the new signed selected-query checks. Two later tests encountered retained fixture data. Assertion now checks Min plus typed metadata selection. Walkthroughs were not reached, so36/36 is NOT certified. Retained marker SHA256 AC9E6BCCD1AF2A90133F85E32DB7DFD1A7CEA31E28C79EAA90E2908F3ADB2565, stage before-http-request,9graphIDs+137scalarIDs,396journalentries. Do not reset/replay. Existing six-entry no-execution reconciliation does not fit: add an exact reviewed reconciliation that verifies terminal usage outcomes, all146 owned records and scalar-write receipts before cleanup, then rerun broad175 and fresh36/36.


## Selected HTTP and demo gate verified — 23 October 2026

The retained fixture was reconciled by the exact AC9E6B... journal and76BF29... native evidence:180settled operations/175matching receipts,411acknowledged scalar writes and all146owned holons in each of3stores verified before cleanup. Original journal and438snapshot hashes are retained. No queries were replayed. The stale Swagger assertion now checks Min and its bounded Int64 metadata selection.

Fresh combined verification **passes175native regression tests, zero skips (3m7s)**, plus **36SDK queries and36HTTP examples** (12perprovider) on MongoDB, SQLite and Neo4j. The demo reports **216settled operations and211physical provider attempts**. Signed selected queries verify exact negative Int64 values beyond2^53, visibility before reduction (hidden rows contain stronger extrema), source windows, nullable empty inputs and no replay. Full Core/HTTP unit evidence remains2,117passing/0skip, including11new endpoint controls. Browser JavaScript syntaxcheck passes.

Evidence: TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx; TestResults/CoreCurrent/cql-selected-scalar-http-current.trx; TestResults/CosmicCqlDemo/v1-demo-result.json, v1-walkthrough.txt, selected-http-native-final.log and selected-http-native-artifacts.json. Five producer/consumer DLL pairs agree (Core109677, shared151E8DC5); the existing runner verified fixture ownership, stopped owned services and removed the exact SQLite/wal/shm/marker after process exit. All four paths and27029/17687listeners were explicitly absent. No runtime/download/new environment was introduced.

This supersedes the pending native/demo checkpoint above. The selected-extrema HTTP/demo gate is complete; the full CQL/ORM roadmap remains open, including Neo4j UTC-date enrollment and strict blocked-I/O interruption, Sum/Average/GroupBy and later federation/query/ORM acceptance.


## Numeric SDK/HTTP demos verified — 23 October 2026

The fresh joint acceptance run passes **195 native regression tests, zero skips**, plus **42 SDK queries and 42 signed HTTP examples** across MongoDB, SQLite and Neo4j. Examples13–14 demonstrate an exact signed Int64 Sum and an ordered two-row Average returning Decimal text `-9007199254740990.5`. Browser presets and Swagger match the spaced/commented C# examples. The actual run recorded **222 settled operations and 217 physical attempts**; no count was inferred from earlier reports. Full endpoint unit acceptance remains **2,155 tests, zero skips**.

Evidence: TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx; TestResults/CosmicCqlDemo/numeric-demo-native.log, v1-demo-result.json, v1-walkthrough.txt and numeric-demo-artifacts.json. Core, Shared and all three provider producer/native-consumer hashes agree. The retained Neo container is exited, Mongo/Bolt/demo ports have no listeners, and the readiness marker and fixed SQLite fixture are absent after acknowledged cleanup. No runtime/environment/download/restore was added.

Run `Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1 -IncludeRegression` for the same bounded acceptance or `-Serve` for the browser demo. Numeric transport and these integer-domain demos are now verified. Decimal/double/UInt64 selector expansion, GroupBy, composition/federation, strict blocked-I/O interruption, Neo UTC DateTime reads and all other unchecked roadmap gates remain open.
