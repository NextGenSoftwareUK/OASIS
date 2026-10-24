# CQL developer manual

23 October 2026. Start here for COSMIC Query Language. CQL composes supported C# expressions into an immutable query, checks provider capabilities and executes native database queries through generic HolonManager. WEB4 v1 exposes a bounded JSON transport over that same pipeline for MongoDB, SQLite and Neo4j.

## Partial typed child roles

Type-filtered loads now hydrate registered child-role collections with the selected typed members. Persisted role order and the full stored membership remain intact; omitted members are not passed to factories. Registered factories receive admitted/decrypted metadata before tracking. A partial view supports root-only edits (`saveChildren: false`); local AcceptChanges cannot grant permission to replace hidden or unloaded membership.

For trusted custom materializers, `HolonChildRoleHydration.ApplySelectedView(owner, fullStoredManifest, selectedIds, resolveSelected)` explicitly hydrates a partial view. Supply exact selected IDs; a missing selected record is an error, not an empty role. Keep the owner unpublished until success and discard it after any failure. This method does not authorize the selected data or read storage. The owner must retain its full `ChildRoleManifestJson` observation before `ApplySelectedView`; the API rejects a missing observation before factories or callbacks. `ApplyFullySelected` remains the complete-membership operation. Visibility-scoped graph composition now requires GraphReadAccess capability at planning and actual-adapter admission; Mongo, SQLite and Neo declare it. HTTP uses explicit bounded graph DTOs. Native child predicates and detached/typed policy checks are implemented; full stored relationship observations remain internal state and must not be serialized as public graph output.

## Mandatory read access in trusted server code

WEB4 v1 binds owner/public visibility from the authenticated avatar and matching usage admission. Clients cannot supply an actor in the DTO. Trusted SDK/service code can bind the same immutable restriction before filters/order/pages:

```csharp
var access = CosmicQueryReadAccess.OwnerOrPublic(authenticatedAvatarId);
if (access.IsError)
    throw new InvalidOperationException(access.Message);

var query = CosmicQuery<Holon>.Create()
    .WithReadAccess(access.Result)
    .Where(holon => holon.IsActive)
    .OrderBy(holon => holon.Name)
    .Take(10);

if (query.IsError)
    throw new InvalidOperationException(query.Message);
```

The binding is retained by scope and window composition; a later actor cannot replace it. The mandatory predicate runs natively before paging. Core checks detached rows before typed constructors and rejects visibility violations as a whole result. Metadata labels such as sharedWith/tenantRole are not grants under this owner/public rule. Creating an SDK policy does not authenticate a caller or grant storage credentials.

Access-bound graphs enforce the same owner/public policy at each native child hop on MongoDB, SQLite and Neo4j. Selected registered roles retain complete private membership observations; HTTP returns bounded public graph DTOs. Shared-recipient/tenant authorization remains separate unfinished work.

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

To require a snapshot rather than accept a provider's default isolation:

```csharp
// One snapshot per selected provider, covering its root page and descendants.
var snapshot = CosmicQueryGraphOptions.Create(
    CosmicQueryGraphConsistency.ProviderSnapshot,
    maxChildDepth: 2,
    maximumNodes: 1000);

if (snapshot.IsError)
    throw new InvalidOperationException(snapshot.Message);

// Compose WithChildren(snapshot.Result), then plan/execute normally.
// SQLite and MongoDB support this request. Neo4j rejects it before I/O
// with CQL_CONSISTENCY_UNSUPPORTED; it cannot silently downgrade the read.
```

`Create()` retains `ProviderDefault`; use it when the selected provider's documented native isolation is acceptable. A per-provider snapshot does not synchronize different databases to one shared instant. Unsupported strong requests also reject during actual-adapter readmission, even if a caller claimed a stronger planning profile.

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

All three SDK/native adapters and WEB4 HTTP v1 now support access-bound child graphs; see [graph guarantees/evidence](CQL_GRAPH_LOADING.md) and [HTTP graph contract](CQL_V1_HTTP_CONTRACT.md). Relationship predicates such as child Any/Count remain separate roadmap work.

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


## Access-bound graph example

Bind a trusted host-selected actor with the immutable policy; a plain root Where expression does not propagate child authorization. The same query works against eligible Mongo, SQLite and Neo adapters. Plan against the actual adapter capabilities and check every OASISResult before execution.

```csharp
var access = CosmicQueryReadAccess.OwnerOrPublic(authenticatedActorId);
var shape = CosmicQueryGraphOptions.Create(
    recursive: true,
    maxChildDepth: 2,
    maximumNodes: 256);

// Check access.IsError and shape.IsError before using their Result values.
var query = CosmicQuery<Holon>.Create()
    .WithReadAccess(access.Result)
    .Where(holon => holon.Name == "C-public")
    .WithChildren(shape.Result)
    .Take(1);
```

The actor comes from trusted authentication/admission; SDK policy creation does not authenticate someone. The normal planner/manager pipeline checks native GraphReadAccess and rechecks detached/typed nodes before publication. Hidden intermediate children are not traversed. Selected/depth-limited views cannot replace unloaded children on Save; use saveChildren:false for root-only edits. SDK models retain private store observations for correct save ownership; public JSON should use the WEB4 graph DTOs instead of serializing raw holons.

HTTP graph defaults/limits, safe fields, parent IDs and consistency are documented in the [HTTP contract](CQL_V1_HTTP_CONTRACT.md). Public depth is1–8; SDK depth0 retains bounded unbounded recursion. C#/HTTP/browser/Swagger demo files include the new graph example. Native certification is being refreshed; current exact evidence is in the graph loading log.


### Saving an authorized partial graph

Native tests now verify this on all three providers, both with a stored role manifest and with older parent-link records. AcceptChanges does not authorize child replacement. After editing a selected root, use the normal conditional SaveHolonAsync with saveChildren:false; the root version advances once and child versions/provider revisions remain unchanged. A child-selecting save from that partial view rejects before it can replace hidden membership. For intentional replacement, use a trusted complete-load/hydration path that proves full stored membership and independent write authority; repeating a filtered access query does not grant completeness. Public HTTP graph DTOs are read projections, not concurrency-bearing SDK save models.


### Registered custom child roles

A trusted owner type declares every role through CaptureChildCollections and GetChildRoleBindings and exposes its complete runtime selection through AllChildren. The same names must match its stored role manifest. Registered typed factories receive admitted/decrypted selected records; they map their child metadata into typed properties. Complete and selected views share the hydration owner, preserving role order and full store observation. A hidden role is an empty selected collection, not permission to delete its omitted records. See the source-linked native contract fixture Scripts/TestHosts/Fixtures/CqlCustomRoleGraph.cs for the owner, typed child and factory registration used by all three suites.


## Direct-child existence (SQLite first)

Use WhereAnyChild(child=>child.Name=="match") before root ordering/Skip/Take. The generic Core contract freezes the child expression; native membership/current parent and mandatory child access decide whether the parent matches before paging. MongoDB, SQLite and Neo4j declare ChildExists. Mongo activation upgrades older membership projections without changing logical revisions; native lookup and pre-page validation share a bounded snapshot. This does not load child graphs. HTTP expresses the same direct-child conditions through bounded anyChildren groups. See [relationship contract/examples](CQL_RELATIONSHIPS.md) for membership/reparenting semantics and remaining work.

HTTP child existence uses up to four anyChildren groups; the twelve-filter budget is shared with roots. Server actor visibility applies before relationship matching and root paging. See [complete JSON example and limits](CQL_V1_HTTP_CONTRACT.md#direct-child-existence-over-http). The numbered SDK/HTTP demos now include example5 for independent visible siblings on every provider.

WhereAnyDescendant has a generic bounded contract with explicit traversal limits. MongoDB, SQLite and Neo4j declare DescendantExists (depth 32/nodes 10000). Mongo retains snapshot semantics with flat native breadth traversal (certified Mongo8.3). Neo descendant execution requires Neo4j5.9+ (certified5.26.12) and retains read-committed semantics. Scope overflow returns CQL_QUERY_LIMIT without partial rows. Descendant intent does not load child graphs; HTTP exposes bounded anyDescendants groups, sharing four relationship groups and twelve filters with roots/direct children; see [JSON contract](CQL_V1_HTTP_CONTRACT.md#bounded-descendant-existence-over-http). [Contract](CQL_DESCENDANT_CONTRACT.md).


## Scalar terminals — MongoDB, SQLite and Neo4j native execution

Core now supports immutable typed terminal construction. These statements perform no storage I/O:

```csharp
// Keep the source window, access policy and provider scope intact.
var source = CosmicQuery<Holon>.Create()
    .Where(holon => holon.Version == 1)
    .Skip(10)
    .Take(20);

var count = source.Count();         // Int32 result domain.
var longCount = source.LongCount(); // Int64 result domain.
var any = source.Any();             // Boolean result domain.
```

Execution uses `CosmicScalarPlanner` and the generic V2 `HolonManager.ExecuteScalarQueryAsync` entry point. MongoDB, SQLite and Neo4j now advertise native Count/LongCount/Any. SQLite full1,269, Mongo unit229/native76 and Neo unit322/jointnative129 pass. WEB4 scalar requests are implemented and pass signed native checks on all three providers; later aggregate shapes remain pending. [Progress and native acceptance contract](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md).


With an activated SQLite provider `sqlite` and its V2 `manager`, execute a scalar through the same generic Core contract:

```csharp
ICosmicQueryScalarStorageProvider adapter = sqlite;

// This is the authorized eligible target snapshot for this SDK operation.
var targets = new[]
{
    new CosmicScalarTarget(
        adapter.QueryProviderType,
        adapter.QueryCapabilities,
        adapter.ScalarCapabilities)
};

// Count the composed source window, without constructing tracked holons.
var intent = CosmicQuery<Holon>.Create()
    .Where(holon => holon.IsActive)
    .LongCount();

if (intent.IsError)
    throw new InvalidOperationException(intent.Message);

var planned = CosmicScalarPlanner.Plan(
    intent.Result,
    adapter.QueryProviderType,
    targets);

if (planned.IsError)
    throw new InvalidOperationException(planned.Message);

var result = await manager.ExecuteScalarQueryAsync(
    planned.Result,
    new ICosmicQueryStorageProvider[] { adapter });

if (result.IsError)
    throw new InvalidOperationException(result.Message, result.Exception);

long count = result.Result.Providers.Single().Value;
```

Count uses checked Int32 and reports `CQL_AGGREGATE_OVERFLOW`; LongCount preserves exact Int64. Empty input/window returns zero, and Any returns false. Source access and relationship predicates apply before participation. A graph output request does not expand children or limit scalar input, while its requested snapshot consistency remains enforced. Counts remain per provider; replicas are not summed into a logical global count. These SDK intents do not grant authentication or access by themselves.


The same scalar example works with an activated MongoDB provider and its V2 manager: bind `adapter` to the MongoDB instance and use its own query/scalar capability declarations. MongoDB counts natively without entity buffers or row caps. It validates canonical relationship membership and executes the scalar pipeline in one snapshot when relationship/graph intent requires it; cancellation and native deadlines cover validation and counting together. A concurrent-reparent control proves the old snapshot count and changed fresh count. WEB4 scalar transport passes the joint 129-test native batch, including a signed count of 137. Later aggregate operations remain pending.


The generic scalar example also works with Neo4j's activated provider and V2 manager. Neo4j counts in one read-committed native transaction and validates projection ownership and descendant errors outside the scalar window. It rejects a requested ProviderSnapshot guarantee before dispatch, including a borrowed caller profile. Its controls verify actual-fetch cancellation and post-fetch cumulative deadline expiry without publication, followed by successful reuse. Prompt interruption of an already blocked driver I/O await remains a separately open limitation; these controls do not prove a hard client I/O deadline. [Scalar HTTP contract](CQL_SCALAR_HTTP_CONTRACT.md).


### WEB4 scalar requests

Use `POST /api/data/query-cql/scalar` with the same bearer authentication and new logical-intent idempotency key as row queries. The server owns actor visibility, provider eligibility and durable usage accounting. Select `MongoDBOASIS`, `SQLLiteDBOASIS` or `Neo4jOASIS`, or omit the provider for the active default.

```json
{
  "provider": "SQLLiteDBOASIS",
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

Omitted `take` counts the complete visible matching input. Explicit `skip`/`take` limit the source before counting. `Count` returns an Int32 JSON number; `Any` a JSON Boolean. `LongCount` always returns an invariant decimal string tagged `Int64`, preserving exact browser values above 2^53. The normal `OASISResult.result` contains `version`, `provider`, `operation`, `valueType` and `value`. Native input remains bounded by execution budgets rather than a row publication cap.

See the [complete scalar contract](CQL_SCALAR_HTTP_CONTRACT.md) for limits and accounting, and [annotated demos](../../../Examples/CosmicCqlDemo/README.md) for copyable C# SDK, HTTP and interactive examples.


## Native Select — SQLite SDK

SQLite supports detached scalar, anonymous and flat DTO projections through the generic V2 manager. MongoDB now also implements the same projection contract (see below); Neo4j also implements the SDK projection contract as described below; the projection HTTP route is described below. Existing row/scalar APIs are unchanged. This SDK code runs within a trusted host: bind the actor from your authoritative application identity.

```csharp
ICosmicQueryProjectionStorageProvider adapter = sqlite;

var access = CosmicQueryReadAccess.OwnerOrPublic(actor);
if (access.IsError)
    throw new InvalidOperationException(access.Message);

var selected = CosmicQuery<Holon>.Create()
    .WithReadAccess(access.Result)
    .Where(holon => holon.IsActive)
    .OrderBy(holon => holon.Name)
    .Take(20)
    .Select(holon => new
    {
        holon.Id,
        holon.Name,
        Stock = CosmicQueryMetadata.Value<long?>(holon, "stock")
    });

if (selected.IsError)
    throw new InvalidOperationException(selected.Message);

var plan = CosmicProjectionPlanner.Plan(
    selected.Result,
    adapter.QueryProviderType,
    new[]
    {
        new CosmicProjectionTarget(
            adapter.QueryProviderType,
            adapter.QueryCapabilities,
            adapter.ProjectionCapabilities)
    });

if (plan.IsError)
    throw new InvalidOperationException(plan.Message);

var result = await manager.ExecuteProjectionQueryAsync(
    plan.Result,
    new ICosmicQueryStorageProvider[] { adapter });

if (result.IsError)
    throw new InvalidOperationException(result.Message, result.Exception);

var rows = result.Result.Providers.Single().Rows;
```

SQLite selects the columns natively; it does not load holons to run the selector. Core constructs detached results after every native buffer validates. Nullable/string missing or explicit null becomes null; a required missing value, wrong type or ambiguous duplicate path is an error. Int64 retains its exact value. Computed selectors, arbitrary methods, entity results and implicit numeric conversions are rejected. Bounds are rejection limits, not truncation: use explicit source Take for a page, and choose an adequate maximumResults/payload budget for execution. At most32 scalar columns are admitted, with the existing bounded native parameter/instruction/deadline limits.

Requested source snapshot consistency is retained; graph output node caps do not reduce projected rows and no children are hydrated. Scope traversal retains its independent depth/node budgets. [Native projection evidence and implementation](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#sqlite-native-projection-slice--23-october-2026).


### MongoDB Select availability

The same generic SDK projection code above is implemented for MongoDB: bind `ICosmicQueryProjectionStorageProvider adapter` to the activated MongoDB instance and use its own query/projection capabilities. Source eligibility, ordering and paging run in the native BSON pipeline; the driver receives scalar cells, never a fetched holon graph for client-side selection. Required relationship validation and column reads share one native snapshot. Graph output limits do not cap projected roots. UUID text/standard/legacy representations and exact UTC tick companions retain their stored semantics; ambiguous dictionary paths or precision companions error. Native row/byte/deadline limits fail without partial results.

Initial unit236/native81 and expanded unit237/native91 gates pass with zero skips. The expanded native cases include snapshot reparenting, actual-aggregate cancellation/reuse, large projections, both UUID layouts and ambiguity rejection. Neo4j SDK projection is described below; authenticated projection HTTP uses the shared endpoint described below. See [MongoDB projection contract](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#mongodb-native-projection-slice--23-october-2026).

Projection results contain detached selected values. Use an entity-row query when you intend to edit a holon and call `SaveHolonAsync` with its tracked state and provider revision.

### Neo4j Select availability

Bind the same generic SDK example to the activated Neo4j provider and its own `QueryCapabilities`/`ProjectionCapabilities`. Root access, direct-child/descendant predicates and ordering/paging execute before selected columns. Only native scalar cells are returned; neither entity snapshots nor children are hydrated. Scalar, anonymous and flat DTO shapes all use Core's existing detached validation and prepared-attempt owner.

Neo4j preserves its admitted GUID, Boolean, signed/narrow integer and defined-enum metadata domains; missing/null string or nullable leaves become null, while required or wrong-domain leaves error. DateTime metadata remains explicitly unsupported. Graph output caps do not cap projected roots; source traversal budgets still apply. This provider supplies read committed, and rejects a requested `ProviderSnapshot` guarantee even when a caller claims a more permissive profile.

Output limits reject the operation rather than return a shortened list. A native row-size lower bound removes impossible string rows before transfer; one-row fetches bound driver buffering, and the existing read-budget owner then checks exact UTF-8 bytes. Cancellation/deadline checks surround native awaits and publication. Prompt interruption of an already blocked driver await remains an open limitation. Final unit328/joint native-WEB4 147 acceptance passes with zero skips, including18new native projection controls and two actual cursor-buffer inspections. [Native contract](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#neo4j-native-projection-slice--23-october-2026).

### Runtime column records

A host that chooses its result columns at runtime can use `CosmicProjectionSelection<T>.Create` and `SelectColumns`. Each binding still requires a typed expression path; Core retains no executable selector delegate. The result is an immutable `CosmicProjectionRecord` with ordered `Aliases`, `Values` and an alias indexer. Native planning/readmission, exact raw-value validation and V2 execution are shared with typed Select.

```csharp
// Choose bounded typed columns; aliases are labels, never native identifiers.
var label = CosmicProjectionSelection<Holon>.Create("label", holon => holon.Name);
var stock = CosmicProjectionSelection<Holon>.Create(
    "stock",
    holon => CosmicQueryMetadata.Value<long?>(holon, "stock"));

if (label.IsError || stock.IsError)
    throw new InvalidOperationException("A requested column is invalid.");

var source = CosmicQuery<Holon>.Create().Take(10).Result;
var selection = source.SelectColumns(new[] { label.Result, stock.Result });

// Plan and execute selection.Result through the same projection API shown above.
// Each resulting record exposes record["label"] and record["stock"].
```

Core2,018 unit cases and both framework targets pass for the runtime-record extension. Authenticated projection HTTP and native runtime-record execution now pass signed three-store acceptance. Expanded annotated30SDK/30HTTP demo verification and broad147 native regression pass unskipped, with195settled operations/190physical attempts. [HTTP contract in progress](CQL_PROJECTION_HTTP_CONTRACT.md).

### HTTP selected columns

Send `POST /api/data/query-cql/projection` with the existing bearer token and one new idempotency key per logical intent. Omit Provider for the admitted current default, or select MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS. The host supplies actor access and eligibility. Take defaults to25 and is bounded0–100. Fields infer their types; metadata declares an exact read type and optional value-type nullability.

```json
{
  "provider": "Neo4jOASIS",
  "orderBy": "Name",
  "take": 10,
  "columns": [
    { "alias": "label", "field": "Name" },
    { "alias": "stock", "metadataPath": ["stock"], "valueType": "Int64", "nullable": true }
  ]
}
```

The `cql-projection-v1` result contains provider provenance, ordered column schema and matching row arrays. Int64 cells use decimal strings, so browsers retain values above2^53; GUIDs use D strings, UTC dates preserve seven tick digits, enums use defined names and null remains null. Selected values are detached. Native privacy/ordering/paging precede selection. Source limits and response limits reject excessive output without a partial result. Repeating a logical key produces a conflict without another dispatch.

FullCore2,059 and signed three-store acceptance pass. Neo4j DateTime metadata is rejected before execution. Prompt interruption of an already blocked Neo4j4.2 driver await remains open. [Complete request/wire limits and evidence](CQL_PROJECTION_HTTP_CONTRACT.md).

### Min/Max intent prerequisite — native execution pending

Core now supports typed `source.Min(holon => ...)` and `source.Max(holon => ...)` intent through the existing scalar API. The selected path retains its exact CLR domain and the original source restrictions. Operation-specific read capabilities are required at planning and actual-adapter readmission; row/filter or count permissions cannot grant selection. All three current native adapters now support admitted Min/Max selections; the following provider sections record their verified domains.

Required empty extrema report CQL_EMPTY_SEQUENCE. Nullable/string empty or all-null extrema return null; absent/null nullable leaves do not participate. Wrong stored types error. Exact negative Int64 results remain valid; native values are not converted through double. FullCore2,102 and both target builds pass for this contract. [Semantics,19reference vectors and remaining native gates](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#minmax-semantics-and-implementation-gate).

### Native Min/Max — SQLite SDK

SQLite now declares Min/Max through its own scalar capabilities. Use the same generic V2 manager/scalar planner; the trusted host supplies actor identity. The terminal selects one typed path and executes natively over the source window. Do not apply a row-publication limit to aggregate input accidentally.

```csharp
var adapter = (ICosmicQueryScalarStorageProvider)sqlite;
var access = CosmicQueryReadAccess.OwnerOrPublic(authenticatedActor).Result;

var source = CosmicQuery<Holon>.Create()
    .WithReadAccess(access)
    .Where(holon => holon.IsActive)
    .Result;

// Use long? when absent/null values should be ignored and an empty input
// should return null. A required long reports CQL_EMPTY_SEQUENCE instead.
var intent = source.Min(
    holon => CosmicQueryMetadata.Value<long?>(holon, "stock"));

var plan = CosmicScalarPlanner.Plan(
    intent.Result,
    adapter.QueryProviderType,
    new[]
    {
        new CosmicScalarTarget(
            adapter.QueryProviderType,
            adapter.QueryCapabilities,
            adapter.ScalarCapabilities)
    });

var result = await manager.ExecuteScalarQueryAsync(
    plan.Result,
    new ICosmicQueryStorageProvider[] { adapter });
```

Handle each OASISResult error before using Result in application code. Max uses the same API. Negative/exact Int64 extrema are valid; wrong types and malformed participating values error. Date comparisons retain UTC ticks. Initial and expanded native controls pass as detailed in the contract; full SQLite regression passes1,318tests with zero skips (2m9s), including all33new Min/Max cases. The same SDK API now works on MongoDB: its final native CQL matrix passes 124 tests and its full provider unit suite passes 237, with zero skips, including all 33 new Min/Max cases. Neo4j now supports the same SDK API for its admitted scalar domains; HTTP selected extrema are verified in the later HTTP checkpoint. [Native semantics/evidence](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#sqlite-native-minmax-slice).


### MongoDB selected extrema

The typed Min/Max example above also applies to the MongoDB adapter. Plan with that adapter's actual QueryCapabilities and ScalarCapabilities, then execute through HolonManager's existing scalar query API. Required empty sequences report `CQL_EMPTY_SEQUENCE`; nullable/string empty sequences return null. A wrong participating value reports `CQL_DATA_INVALID`, even when another row would win. Source ordering/paging, visibility and traversal validation keep their existing semantics. The manager does not load entities to calculate extrema. [Native implementation and acceptance](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#mongodb-native-minmax-slice).


### Neo4j selected extrema

Use the same typed Min/Max SDK example above with the Neo4j adapter's actual QueryCapabilities and ScalarCapabilities. Integers, Boolean, string, GUID and admitted enum paths use native reduction; no entities are loaded for aggregation. Nullable/string empty input returns null; required empty input reports CQL_EMPTY_SEQUENCE. Wrong participating values report CQL_DATA_INVALID even when they would not win. Source paging and protected traversal validation retain their established semantics.

The expanded CQL matrix passes 111 tests and the full provider unit suite passes 328, with zero skips. UTC DateTime selectors remain unsupported; query admission rejects them. Neo4j's read-committed behavior and existing strict-I/O limitations remain unchanged. [Native semantics, coverage and limits](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#neo4j-native-minmax-slice). HTTP selected extrema, Sum/Average/GroupBy and later CQL phases remain pending.


### Selected scalar HTTP request (verification checkpoint)

Use `/api/data/query-cql/scalar` with Operation Min or Max and exactly one Selection. Canonical fields infer their type; metadata requires a bounded path and exact type. The host still supplies actor/eligibility. Example:

```json
{
  "provider": "MongoDBOASIS",
  "operation": "Min",
  "selection": {
    "metadataPath": ["stock"],
    "valueType": "Int64",
    "nullable": true
  }
}
```

Int64 results remain decimal strings; a nullable empty sequence returns null. Unit/HTTP controls pass2,117; exact fixture reconciliation and fresh175native/36SDK/36HTTP verification now pass. [Current evidence and remaining gate](CQL_SCALAR_HTTP_CONTRACT.md#selected-minmax-http-checkpoint--23-october-2026).


The selected Min/Max HTTP/demo gate is verified on allthree providers:175native tests plus36SDK/36HTTP examples,0skips,216settled operations/211physical attempts. [Current verified evidence](CQL_SCALAR_HTTP_CONTRACT.md#selected-http-and-demo-gate-verified--23-october-2026).


## Integer Sum/Average Core checkpoint — 23 October 2026

Immutable typed intents, operation-specific exact read admission and constant-size native accumulator normalization now pass both Core targets and all **2,141 tests, zero skips**, including24new cases. Integer Sum checks its final mathematical domain; Average returns Decimal without a binary64 intermediate. Empty/null, precision/rounding and explicit work-limit semantics are specified. The initial foundation checkpoint preceded provider enrollment. All three native providers, integer numeric HTTP and demos are now verified in the current checkpoint below; broader scalar domains and the rest of the roadmap remain open. [Contract, examples and evidence](CQL_NUMERIC_AGGREGATES.md).


## SQLite numeric gate — 23 October 2026

Native Sum/Average now share the existing scalar read owner and exact projection operand guard. Native two-limb reduction preserves exact mathematical sums and Decimal averages without a binary64 accumulation or input-row buffer. Final **1,345 SQLite tests pass, zero skips**, including 27 new numeric cases; producer/consumer identities agree and fixed fixture files are absent after teardown. MongoDB/Neo4j numeric execution and HTTP/demos remain open. [Semantics and exact evidence](CQL_NUMERIC_AGGREGATES.md#sqlite-native-sumaverage-verified--23-october-2026).


## MongoDB numeric checkpoint — 23 October 2026

Native Sum/Average now use exact signed limb aggregation through the existing scalar read/snapshot/cancellation owner. The final Mongo suite passes **144 native tests and 245 unit tests, zero skips**;20 native and 8 decoder controls are new. Core and SQLite numerical support remain verified from their earlier checkpoints. See [numeric contract and evidence](CQL_NUMERIC_AGGREGATES.md). Neo4j numeric support, numeric HTTP/Decimal wire and demos, GroupBy, composition/federation, strict blocked-I/O interruption and the remaining roadmap gates are still open. This checkpoint is not full CQL/ORM completion.


## Neo4j numeric checkpoint — 23 October 2026

Neo4j Sum/Average now use exact integer limb reductions through the existing scalar transaction/read-budget owner. The final suite passes **131 native tests and 331 provider unit tests, zero skips**, including 20 new native cases and three packet unit controls. SQLite, MongoDB and Neo4j numeric SDK execution are now verified. See [numeric contract and evidence](CQL_NUMERIC_AGGREGATES.md). Numeric HTTP/Decimal wire and demonstrations are next; grouping, composition/federation, strict blocked-I/O interruption, Neo UTC DateTime reads and the remaining CQL/ORM gates remain open. This is not full roadmap completion.


## Numeric scalar HTTP checkpoint — 23 October 2026

The existing scalar route now accepts Sum/Average with a required typed Int32/Int64 selection (nullable allowed). Average returns valueType Decimal and an invariant string; Sum retains the existing exact integer wire contract. The existing admission, manager, receipt and atomic response owner are reused. All 14 new endpoint controls pass in the full **2,155-test suite, zero skips**. [Contract/evidence](CQL_NUMERIC_AGGREGATES.md). Live three-provider HTTP acceptance and numeric demo examples remain pending; this does not complete the roadmap.

```json
{
  "provider": "MongoDBOASIS",
  "operation": "Average",
  "selection": {
    "metadataPath": ["stock"],
    "valueType": "Int64",
    "nullable": true
  }
}
```

Use the same request shape with SQLLiteDBOASIS or Neo4jOASIS when admitted. Int64 and Decimal response values are strings; parse them with an exact numeric library when arithmetic is required, rather than JavaScript Number.


## Numeric SDK/HTTP demos verified — 23 October 2026

The fresh joint acceptance run passes **195 native regression tests, zero skips**, plus **42 SDK queries and 42 signed HTTP examples** across MongoDB, SQLite and Neo4j. Examples13–14 demonstrate an exact signed Int64 Sum and an ordered two-row Average returning Decimal text `-9007199254740990.5`. Browser presets and Swagger match the spaced/commented C# examples. The actual run recorded **222 settled operations and 217 physical attempts**; no count was inferred from earlier reports. Full endpoint unit acceptance remains **2,155 tests, zero skips**.

Evidence: TestResults/Neo4jCurrent/neo4j-cql-v1-web4-current.trx; TestResults/CosmicCqlDemo/numeric-demo-native.log, v1-demo-result.json, v1-walkthrough.txt and numeric-demo-artifacts.json. Core, Shared and all three provider producer/native-consumer hashes agree. The retained Neo container is exited, Mongo/Bolt/demo ports have no listeners, and the readiness marker and fixed SQLite fixture are absent after acknowledged cleanup. No runtime/environment/download/restore was added.

Run `Scripts/TestHosts/Run-CosmicCqlV1Demo.ps1 -IncludeRegression` for the same bounded acceptance or `-Serve` for the browser demo. Numeric transport and these integer-domain demos are now verified. Decimal/double/UInt64 selector expansion, GroupBy, composition/federation, strict blocked-I/O interruption, Neo UTC DateTime reads and all other unchecked roadmap gates remain open.


## GroupBy key foundation — 23 October 2026

Core now captures `source.GroupBy(h => h.Name)` and scalar/composite DTO keys through the existing projection compiler. One to eight key columns and separate immutable grouped-output bounds preserve the original source, trusted actor, relationships, ordering/window and consistency. No selected code or result constructor executes during capture. Both Core targets build and all **2,169 tests pass, zero skips**, including14 new controls. Private Core commit ec7d26c1ab9b3dffc268d4732348f59f17f0d000 is paired with the parent gitlink/manifest.

[Grouping contract and remaining acceptance](CQL_GROUPING_CONTRACT.md). This is capture only: grouped result selection, capability planning/readmission, attempt ownership, native grouping, HTTP and examples remain to implement. No provider advertises grouping yet; the full roadmap remains active.


## Grouped selection capture — 23 October 2026

`GroupBy(...).Select(g => new { g.Key.Name, Rows = g.LongCount(), Mean = g.Average(h => h.Version) })` now captures bounded typed key/aggregate slots without executing user code. Count/LongCount, Min/Max and exact integer Sum/Average are supported in the expression surface; Average returns Decimal. Ordinary and grouped Select share one detached DTO-shape parser. Both Core targets build; full **2,182 tests pass, zero skips**, including13 new grouped-selection controls. [Contract and evidence](CQL_GROUPING_CONTRACT.md). Planning, detached grouped-result validation, physical-attempt ownership, native grouping, HTTP and demos are still required; no provider advertises grouping execution yet.


## Grouped planning/readmission — 23 October 2026

Independent grouping profiles now admit all key components, every selected aggregate and group/column/payload ceilings. Source planning preserves scope, actor, relationships, windows and consistency. Actual single-provider readmission rejects borrowed query/group profiles. Scalar and grouped operation/read-path checks share one owner. Both Core targets build; full **2,200 tests pass, zero skips**, including18 new planning controls. [Contract/evidence](CQL_GROUPING_CONTRACT.md). Grouped result validation/materialization, physical-attempt ownership, native GroupBy, HTTP and demos remain open; no provider advertises grouping execution.


## Grouped result validation — 23 October 2026

Immutable native rows now require positive input-count proof, complete typed key cells and selected cells. Whole-buffer validation checks unique ordered identity, key/count agreement, source windows, scalar domains, numeric work and payload bounds before any DTO construction. Ordinary projection and grouped results share scalar validation and constructor/member assignment owners; non-UTC dates and undefined enums now reject in projections too. Full **2,224 tests pass, zero skips**, including24 new controls; both Core targets build. [Contract/evidence](CQL_GROUPING_CONTRACT.md). Grouped physical-attempt execution, native adapters, HTTP and demos remain required; no provider advertises grouping execution.


## Grouped V2 execution owner — 23 October 2026

Generic HolonManager grouped execution now preflights all actual adapters and reuses the existing prepared physical-attempt/receipt owner. Global group/payload budgets are enforced across all buffers before any DTO construction. Per-provider rows remain separate; zero remaining bounds do not skip a later empty provider's execution. Both Core targets build; full **2,239 tests pass, zero skips**, including15 new controlled executor/hosted cases. [Contract/evidence](CQL_GROUPING_CONTRACT.md). Native GroupBy, HTTP and demos are still required; no provider advertises native grouping yet.
