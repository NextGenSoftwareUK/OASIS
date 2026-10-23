# CQL developer manual

22 October 2026. Start here for COSMIC Query Language. CQL composes supported C# expressions into an immutable query, checks provider capabilities and executes native database queries through generic HolonManager. WEB4 v1 exposes a bounded JSON transport over that same pipeline for MongoDB, SQLite and Neo4j.

## SQLite, MongoDB and Neo4j SDK child graphs

SQLite, MongoDB and Neo4j support immutable `WithChildren(options)` query composition. SQLite/Mongo native graph readers are certified under snapshot semantics; Neo4j native81/unit31 and joint82 cases also pass under its documented read-committed semantics. These are provider-specific guarantees, not portable snapshot admission. Create options with `CosmicQueryGraphOptions.Create(recursive: true, maxChildDepth: 2, childType: HolonType.All, maximumNodes: 1000)` and check its OASISResult before passing Result to WithChildren. Check the resulting query/plan before executing through the usual generic HolonManager pipeline. Apply root Where/OrderBy/Skip/Take as usual: expansion occurs after the root page is selected.

For example, with the selected root IDs already stored in a local `rootIds` array:

```csharp
// One bounded graph shape, retained through filtering, ordering and paging.
var graph = CosmicQueryGraphOptions.Create(
    recursive: true,
    maxChildDepth: 2,
    childType: HolonType.All,
    maximumNodes: 1000,
    maximumPayloadBytes: 8 * 1024 * 1024);

if (graph.IsError)
    throw new InvalidOperationException(graph.Message);

// Select the root page first. Descendants do not consume the ten-root limit;
// they do consume the shared graph node and byte budgets above.
var query = CosmicQuery<Holon>.Create()
    .Where(holon => rootIds.Contains(holon.Id))
    .OrderBy(holon => holon.Name)
    .Take(10)
    .WithChildren(graph.Result);

if (query.IsError)
    throw new InvalidOperationException(query.Message);

// Plan with the actual selected provider's capabilities, then execute through
// HolonManager as shown below. A leaf adapter cannot be upgraded by the caller.
```

Roots are depth zero. Depth one includes immediate children; zero means recursion within the explicit node/payload budgets. `recursive: false` reads immediate children only. `WithoutChildren()` restores leaf shape. Node and byte limits apply to the whole selected graph; exceeding them rejects the query rather than truncating it. Filtered/depth-boundary results retain omitted membership and cannot replace unobserved relationships on a child-selecting save. Use `saveChildren: false` for root-only edits.

SQLite reads roots/children from one deferred Serializable snapshot. Existing caller transactions must also be Serializable and remain caller-owned. MongoDB uses one snapshot session across root/child commands; the tested environment is Mongo8.3.11 replica set with driver3.12.0. Neo4j uses one read-committed native transaction; committed child updates between commands may be visible, so this is not a repeatable snapshot. Its driver checks cancellation between native awaits and before publication, with a30-second transaction timeout. HTTP graph transport is still pending; the three-provider v1 examples below continue to demonstrate the certified leaf API. [Graph details and evidence](CQL_GRAPH_LOADING.md).

## Run a demonstration

From `C:/Source/OASIS` in PowerShell 7:

```powershell
# Annotated C# SDK and HTTP examples, running against all three native databases.
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1

# Guided browser demo and Swagger over the shipping v1 query controller.
.\Scripts\TestHosts\Run-CosmicCqlV1Demo.ps1 -Serve
```

For Serve, wait for `TestResults/CosmicCqlDemo/v1-session.json`, then open `http://127.0.0.1:5088/demo`. Swagger is at `/swagger`. Use **Finish demo & clean up** when finished; the host awaits requests, verifies durable settlement and removes only its owned fixture. An interrupted/failed fixture is retained for inspection, not reset automatically. The launcher reuses the installed MongoDB, retained Neo4j container, SDK/cache and producer lease; it does not install a new environment. Another active native environment/build lease must finish first.

For the existing typed coffee/save/delta walkthrough:

```powershell
.\Scripts\TestHosts\Run-CosmicCqlDemo.ps1
.\Scripts\TestHosts\Run-CosmicCqlDemo.ps1 -NoBuild -Serve
```

That SQLite host uses `http://127.0.0.1:5087/swagger`. Close one leased demo before starting another. [Full presenter script](CQL_DEMO_RUNBOOK.md).

## Which API should I use?

| Interface | Intended use | Contract |
|---|---|---|
| C# `CosmicQuery<T>` + HolonManager | Trusted application/service code | Richer expression tree subset, explicit adapter/capability admission and provider scope |
| WEB4 `POST /api/data/query-cql` | Authenticated application clients | One active Mongo/SQLite/Neo provider; bounded leaf queries; server-owned access and pricing |
| SQLite coffee demo API | Isolated walkthrough | Demonstrates typed queries and saving; its wider scope controls are not WEB4 v1 transport features |

CQL is LINQ-style, not a general `IQueryable` implementation or unrestricted SQL/Cypher evaluator. Query construction performs no I/O. `Where`, ordering and composition return `OASISResult<T>`; handle errors before using Result. Unsupported expressions fail explicitly rather than evaluating predicates against a downloaded dataset.

## C# SDK: read the code in this order

1. [V1NativeQueries.cs](../../../Examples/CosmicCqlDemo/V1NativeQueries.cs): identical LINQ-style query code executed on MongoDB, SQLite and Neo4j; comments explain access, metadata, native paging and Int64 precision.
2. [V1Walkthrough.cs](../../../Examples/CosmicCqlDemo/V1Walkthrough.cs): spaced C# HTTP client examples, immutable operation intent, nested predicates and settlement headers.
3. [CoffeeWalkthrough.cs](../../../Examples/CosmicCqlDemo/CoffeeWalkthrough.cs): annotated typed Coffee properties, membership/field comparisons, clean tracking, no-op Save, deltas and stale-client conflict rejection.

The central sequence is:

```csharp
// Existing authorized manager/adapter instances are supplied by your host.
// Check every OASISResult before consuming Result in application code.
var scope = CosmicQueryScope.Named(adapter.QueryProviderType).Result;

var query = CosmicQuery<Holon>.Create(scope)
    .Where(holon => holon.IsPublic || holon.CreatedByAvatarId == actorId)
    .Where(holon =>
        CosmicQueryMetadata.Value<string>(holon, "location", "city") == "London")
    .OrderBy(holon => holon.Name)
    .ThenBy(holon => holon.Id)
    .Take(10);

var plan = CosmicQueryPlanner.Plan(
    query.Result,
    adapter.QueryProviderType,
    new[]
    {
        new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities)
    });

var result = await manager.ExecuteQueryAsync(
    plan.Result,
    new[] { adapter },
    maximumResults: 10,
    cancellationToken: cancellationToken);
```

The full runnable source handles errors and verifies returned rows. A local SDK host owns authorization; the explicit owner/public predicate above is deliberate. WEB4 v1 always supplies its authoritative visibility predicate on the server. Supplying a scope or capability object does not authorize access or widen an admitted request.

`CurrentDefault`, named scope and all-eligible scope exist in the local SDK. Results across multiple providers are grouped with provenance, not globally deduplicated, ordered or paged. All-eligible scope is **not** part of HTTP v1. [Scope/execution details](CQL_EXECUTION_PIPELINE.md).

## WEB4 v1 request

Child graph work is in progress: [Core publication, certified SQLite SDK expansion and remaining Mongo/Neo work](CQL_GRAPH_LOADING.md). All three native SDK adapters support graphs; v1 HTTP remains leaf-only.

```json
{
  "provider": "MongoDBOASIS",
  "filters": [
    {
      "metadataPath": ["location", "city"],
      "scalar": "String",
      "operator": "Equal",
      "value": "London"
    },
    {
      "metadataPath": ["stock"],
      "scalar": "Integer",
      "operator": "GreaterThanOrEqual",
      "value": 7
    }
  ],
  "anyOf": [
    { "field": "Name", "operator": "Equal", "value": "A-private" },
    { "field": "Name", "operator": "Equal", "value": "D-owned" }
  ],
  "orderBy": "Name",
  "skip": 0,
  "take": 10
}
```

This means **server visibility AND every Filters entry AND (any AnyOf entry)**. An empty AnyOf adds no extra restriction. Only one OR group is exposed by v1; the C# engine supports richer supported expression-tree composition.

Use the exact provider names `MongoDBOASIS`, `SQLLiteDBOASIS` or `Neo4jOASIS`. Omit Provider for the already-active server default. The demo's default is SQLite; production uses its configured current provider. `All`, aliases, numeric enum strings and requests to activate/fail over to other providers reject.

| Input | HTTP v1 support |
|---|---|
| Canonical filter fields | Id, Name, Description, Version, IsActive, ParentHolonId |
| Metadata path | One to four literal segments, case/spelling matching persisted keys |
| Metadata scalar | String, Integer (Int64), Boolean, Guid (D format) |
| Comparison | Equal / NotEqual; GreaterThan / GreaterThanOrEqual / LessThan / LessThanOrEqual where the CLR scalar supports ordering |
| String operation | Contains / StartsWith / EndsWith, explicit ordinal semantics |
| Metadata tests | Exists / IsNull, with no Value |
| Ordering | Name / Version / Id, Descending optional; Id tie-breaker |
| Metadata output | IncludeMetadata optional, false by default |

Canonical property types come from the model, not the supplied Scalar. The user cannot supply CreatedByAvatarId, IsPublic, a runtime type, raw query or executable expression as an authority override. Unknown request members reject.

## Metadata and precision

- A numeric string `"7"` does not match Integer 7. Invalid GUID data does not satisfy typed GUID inequality.
- Int64 literals such as `9007199254740993` remain exact. In C#, use a `long`/`L` literal. JavaScript Number cannot represent all Int64 values; retain exact JSON text or use a suitable exact-integer serializer. The demo intentionally sends/pretty-prints JSON without numeric coercion.
- Guid literals use D-format text; GUID case normalizes. Metadata key spelling is not normalized.
- Exists includes a present null member. IsNull requires a present null member. A missing path satisfies neither. Null-valued comparisons are outside HTTP v1; use the explicit metadata tests.
- Queryable metadata must remain available to the native projection under the encryption/reserved-key policy. There is no fallback that decrypts/scans all provider data.

The local SDK has additional provider-declared scalar domains, annotated properties, membership and field comparisons. Do not assume every such feature is exposed by the narrower HTTP DTO. [Scalar contract](CQL_SCALAR_METADATA.md), [expression reference](CQL_EXPRESSION_CONTRACT.md).

## Access, budgets and operation ownership

WEB4 verifies the existing bearer identity, binds its avatar to the exact durable reservation, resolves an eligible concrete provider, then adds owner-or-globally-public visibility before native filtering/sorting/paging. A metadata sharing index or Wizard role cannot bypass it. Authoritative shared-recipient membership and tenant-specific isolation are not part of this v1 policy.

Limits: 32 KiB request; twelve total filters; Skip 0–1000; Take 1–100 (default 25); strings at most 512 UTF-16 code units; path segments at most 64; output at most one MiB; one provider attempt per admitted request. A five-second cancellation deadline is cooperative, not a guarantee of hard driver interruption at precisely five seconds. Provider-native work/result budgets remain active.

Send an `Idempotency-Key` for each new logical request. Keep it and `X-OASIS-Operation-Id` if the outcome becomes uncertain. `X-OASIS-Usage-Settlement` indicates the durable response settlement state. Duplicate keys produce a conflict/terminal result without another execution; this is not response-body replay. A timeout does not establish no execution. Do not create a new key merely to conceal uncertainty; use the existing authorized reconciliation workflow.

Ordinary query tariffs are explicit server-owned service prices, not vendor invoices or guessed gas fees. Empty results and completed provider-error envelopes follow that attempt tariff. Pre-dispatch rejection records no-execution evidence; interrupted/unknown completion retains reconciliation state. [Operator configuration and errors](CQL_V1_OPERATIONS.md), [full transport contract](CQL_V1_HTTP_CONTRACT.md).

## Saving queried holons

Local typed CQL hydration returns clean tracked entities in the admitted leaf or graph shape. A change can be saved through HolonManager's conditional-write path; stale-client conflicts reject without overwriting the winner. A no-op Save does not create a new version, and an acknowledged write can expose its committed delta. These are shown in the seven-step CoffeeWalkthrough; the v1 query endpoint itself reads data. [Demo runbook](CQL_DEMO_RUNBOOK.md).

## Documentation map

| Read | Purpose |
|---|---|
| [Architecture](CQL_ARCHITECTURE.md) | Ownership, layers, execution flow and boundaries |
| [Getting started](CQL_GETTING_STARTED.md) | Concise SDK feature overview |
| [Demo runbook](CQL_DEMO_RUNBOOK.md) | Commands, expected results and presenter script |
| [HTTP contract](CQL_V1_HTTP_CONTRACT.md) | Exact DTO, validation, admission and pricing |
| [Operations](CQL_V1_OPERATIONS.md) | C# HTTP call, setup, errors and reconciliation |
| [Ordering/paging](CQL_ORDERING_PAGING.md) | Fluent composition and per-provider semantics |
| [MongoDB](CQL_MONGO_NATIVE.md), [SQLite](CQL_SQLITE_NATIVE.md), [Neo4j](CQL_NEO4J_NATIVE.md) | Native implementation/evidence history |
| [v1 acceptance](CQL_V1_MILESTONE_3.md) | Exact verified scope and retained reports |
| [Demo verification](CQL_V1_DEMO_VERIFICATION.md) | Current walkthrough, browser/API checks and cleanup evidence |

The bounded v1 implementation is verified. Joins/relationships, aggregates, global federation/pages, broader transport scalar families and application-specific shared/tenant policies remain separate roadmap work. Automatic migration/replication and HyperDrive placement are related ORM capabilities, not features invented by this query endpoint.
