# CQL getting started

**Current v1 — 23 October 2026:** [manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md), [three-provider demo](CQL_DEMO_RUNBOOK.md). Bounded authenticated WEB4 v1 is verified on MongoDB, SQLite and Neo4j. Local SDK features are broader than the HTTP DTO. Earlier checkpoints below retain historical scope; “Neo4j” is not current v1 status.

CQL translates supported C# expression trees into a provider-independent query representation, checks selected providers capabilities, then executes native queries. MongoDB, SQLite and Neo4j now have verified native adapters and a bounded authenticated WEB4 v1 boundary. Local SDK features remain broader than that transport; full CQL/ORM roadmap completion is not claimed.

## Run the demo

The [three-provider v1 walkthrough](CQL_DEMO_RUNBOOK.md) runs identical annotated C# SDK and HTTP queries on MongoDB, SQLite and Neo4j, with a guided browser page and Swagger. It exercises shipping query/authentication/admission code with synthetic identities and accounting authority over real databases. The separate SQLite coffee example also demonstrates clean tracking, no-op saves, committed deltas and cross-client conflict rejection.

## Local SDK feature overview (HTTP v1 is narrower)

| Area | Available now | Remaining |
|---|---|---|
| Expressions | Where, AND/OR/NOT, scalar comparisons, null tests, ordinal string matching, bounded scalar-array membership; annotated scalar member paths; [native field-to-field comparisons](CQL_FIELD_COMPARISONS.md) | Wider expression forms |
| Metadata | Nested literal paths, Exists, IsNull; SQLite/MongoDB string/bool/int/long comparisons, membership and supported strings; [exact byte/sbyte/short/ushort/uint domains](CQL_INTEGER_DOMAINS.md); [defined HolonType/ProviderType enums](CQL_ENUM_METADATA.md), including nullable forms; Guid/Guid? equality/membership; UTC DateTime/DateTime? comparisons/membership over each provider's declared storage domain | Native decimal/double/UInt64, unregistered enums, broader schema registration and canonical audit-date policy |
| Scope | Current default, named providers, all eligible targets; capability admission before I/O | HTTP v1 resolves one active named/default provider; broader policy placement/federation remains separate |
| Ordering/paging | [Fluent OrderBy/ThenBy, Skip/Take](CQL_ORDERING_PAGING.md); native SQLite/MongoDB and Neo4j scalar/domain ordering, identity ties and resource bounds | Subquery composition, global federated pages and snapshot/continuation semantics |
| Native execution | SQLite SQL, MongoDB BSON expressions and Neo4j Cypher predicates; cancellation and result/query budgets; SQLite native work/deadline/record bounds and MongoDB native deadline/sort/payload bounds | Additional scalar/resource certification and other adapters; v1 authenticated admission/accounting is implemented |
| Returned entities | V2 typed leaf entities, metadata hydration/decryption, clean tracking, conditional save integration | Child loading options, relationship predicates, broader entity-kind mapping |
| Federation | Provider-grouped results with provenance and errors | Deduplication, freshness/conflict policy, paging/order/projection/aggregates |

This is currently a fluent expression API, not a textual SQL-like parser or a complete EF IQueryable implementation. Query compilation can represent more scalar types than any particular adapter can execute. Capability checks distinguish the two.

Ordering and paging use the same capability-aware pipeline:

```csharp
var page = CosmicQuery<Holon>.Create()
    .Where(h => h.IsActive && h.Version >= 2)
    .OrderBy(h => h.Name)
    .ThenByDescending(h => h.Version)
    .Skip(20).Take(10);
if (page.IsError) throw new InvalidOperationException(page.Message);
```

Use `page.Result` with the same planner/executor below. Chaining preserves the first error. Offset pages read the current data on each request; they do not promise a snapshot across concurrent writes. Multi-provider global pages reject until the federation implementation can preserve their meaning.

## Neo4j subset

The selected Neo4jOASIS provider now implements the same ICosmicQueryStorageProvider contract and exposes CqlCapabilities. Use it with the same planner/executor and an explicitly authorized Neo4j target. The initial profile admits Name/Description predicates, Version comparisons, canonical bool/Guid equality, string/bool/int/long metadata plus byte/sbyte/short/ushort/uint and nullable forms, membership and metadata Exists/IsNull. Narrow integer paths reject stored integers outside their declared CLR range. HolonType/ProviderType metadata and nullable forms admit only defined integer values, supporting comparisons, membership, fields and ordering; enum names and undefined integers do not match typed paths. Guid/Guid? metadata equality, membership, field comparisons and ordering now admit exact canonical D text in either hex case; malformed representations reject the typed domain while string markers keep case-sensitive matching. Field-to-field comparisons preserve typed guards and nullable semantics, including lossless integer promotions. Guid.Empty is recognized as a known framework literal without evaluating arbitrary statics. OrderBy/ThenBy/Skip/Take now use the shared buckets and logical-ID ties. String ordering is binary Unicode scalar order; explicit CLR culture/UTF-16 comparer selection is not implemented. Wider scalar domains and snapshot/global-page guarantees remain open. Activation upgrades stored query projections without changing logical snapshots/revisions. [Native acceptance, migration behavior and limits](CQL_NEO4J_NATIVE.md).

## Build a query

```csharp
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

var minimumVersion = 2;
var built = CosmicQuery<Holon>.Create().Where(h =>
    h.Version >= minimumVersion &&
    h.Name.StartsWith("Coffee", StringComparison.Ordinal));

if (built.IsError)
    throw new InvalidOperationException(built.Message);

var query = built.Result;
```

Captured values are frozen when Where is called. Changing minimumVersion afterwards does not change this query. Composition is immutable: another Where returns another OASISResult containing a new query and combines the predicates with AND. Check each result before using it.

## Query nested metadata

Annotated scalar members also use the shared metadata path compiler:

```csharp
public class Coffee : Holon
{
    [NextGenSoftware.OASIS.API.Core.CustomAttrbiutes.CustomOASISProperty]
    public int Rating { get; set; }
}
```

Use that model in your calling method:

```csharp
var built = CosmicQuery<Coffee>.Create().Where(coffee =>
    coffee.Rating >= 7 && coffee.Name.StartsWith("Coffee", StringComparison.Ordinal));
if (built.IsError) throw new InvalidOperationException(built.Message);
```

Place the Coffee type in your model source and the query in your calling method. This translates Rating to its persisted metadata key without running a getter or application attribute constructor. It requires the trusted scalar persistence marker and supported provider type/operators; StoreAsJsonString and unmarked/read-only properties are not inferred. It uses metadata missing/null semantics, including case-sensitive stored keys; application constructor defaults for absent keys are not evaluated by CQL.

```csharp
var built = CosmicQuery<Holon>.Create().Where(h =>
    CosmicQueryMetadata.Value<int>(h, "stats", "score") >= 7 &&
    CosmicQueryMetadata.Value<string>(h, "category") == "coffee" &&
    CosmicQueryMetadata.Exists(h, "merchantId"));
```

The segments "stats", "score" mean nested objects. A single segment "stats.score" means one literal key containing a dot. Paths and values are bound parameters, not native query fragments.

Exists matches a present entry, including explicit JSON null. IsNull matches explicit JSON null, not a missing path. Typed predicates reject missing/wrong-type values as nonmatches; NOT complements the entire predicate. See the detailed [metadata semantics](CQL_SCALAR_METADATA.md) before relying on inequality/null combinations.

## Select provider scope

```csharp
// Default intent: the current default in the authorized target snapshot.
var defaultQuery = CosmicQuery<Holon>.Create();

var named = CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS);
if (named.IsError) throw new InvalidOperationException(named.Message);
var sqliteQuery = query.WithScope(named.Result);

// Every target supplied by the trusted eligibility boundary.
var allQuery = query.WithScope(CosmicQueryScope.AllEligible);
```

These examples also require `using NextGenSoftware.OASIS.API.Core.Enums;`. AllEligible means all supplied eligible targets, not every registered provider. If any selected target cannot execute the complete predicate, planning rejects the query; it does not silently omit that target or evaluate the filter locally.

## Plan and execute against SQLite

This example assumes an activated SQLite provider and an existing HolonManager configured to use Hyperdrive V2. The hosting application must already have authorized this actor and target. Passing an adapter or constructing a plan does not grant authorization; automatic WEB4 policy integration is still open.

```csharp
using NextGenSoftware.OASIS.API.Core.Interfaces;

ICosmicQueryStorageProvider adapter = provider;
var targets = new[] {
    new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities)
};

var plan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType, targets);
if (plan.IsError) throw new InvalidOperationException(plan.Message);

var result = await manager.ExecuteQueryAsync(
    plan.Result, new[] { adapter },
    maximumResults: 100, cancellationToken: cancellationToken);
if (result.IsError) throw new InvalidOperationException(result.Message);

foreach (var group in result.Result.Providers)
    foreach (var holon in group.Rows)
        Console.WriteLine($"{group.Provider}: {holon.Id} {holon.Name}");
```

maximumResults is a total result safety budget, not pagination or Take. Exceeding it returns an error rather than publishing a truncated successful result. Results retain provider groups; there is no implicit identity deduplication or cross-provider winner selection.

## Membership and strings

Date predicates use UTC literals and keep .NET tick precision. SQLite admits its canonical UTC JSON text, while MongoDB uses native BSON dates plus the existing exact-tick companion. Choose targets whose declared metadata representation matches your data; the planner does not parse arbitrary timestamp strings or numeric epochs. [SQLite date domain and validation](CQL_SCALAR_METADATA.md#utc-dates), [MongoDB exact date contract](CQL_MONGO_NATIVE.md#utc-metadata-date-increment).

```csharp
var since = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc).AddTicks(1);
var dated = CosmicQuery<Holon>.Create().Where(h =>
    CosmicQueryMetadata.Value<DateTime>(h, "events", "recordedAt") >= since);
```

For MongoDB, the same planning/execution API accepts an activated MongoDB adapter and `ProviderType.MongoDBOASIS`. Its profile revalidates predicates before repository access, uses literal BSON values and binary collation, and returns clean leaf entities through HolonManager. It enforces count/query/payload budgets and a native deadline. See [MongoDB native CQL examples, exact evidence and remaining limits](CQL_MONGO_NATIVE.md) before choosing provider-specific scalar features.

```csharp
var ids = new[] { firstId, secondId }; // Guid values
var built = CosmicQuery<Holon>.Create().Where(h =>
    ids.Contains(h.Id) && h.Description.Contains("espresso"));
```

Use `using System.Linq;` for array membership. Membership arrays are captured, bounded to 128 values and must match the path's declared type. String matching is ordinal; StartsWith/EndsWith require explicit StringComparison.Ordinal. SQL wildcard characters remain literal text. Arbitrary application methods/getters are not executed by the compiler.

## Edit a query result

Typed manager query results have clean tracking baselines. An unchanged save preserves the revision; an edit can use the existing conditional save API and its accepted provider revision:

```csharp
var row = result.Result.Providers.Single().Rows.First();
row.Description = "Updated description";
var saved = await manager.SaveHolonAsync(
    row, actorId, new CosmicWriteOptions(Guid.NewGuid()),
    saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
if (saved.IsError) throw new InvalidOperationException(saved.Message);
```

Keep the operation ID stable for retries of the same operation. SQLite encrypted conditional saves now use [durable authenticated preparation recovery](WRITE_PREPARATION.md), including selected restart, key-rotation and concurrent-client cases. The provider must implement the preparation capability; providers without it refuse before persistence. DynamoDB/AWS also has selected native encrypted manager/V2 proof, recorded separately in the preparation documentation. Full fault, retention and authorization acceptance remains open.

## Encryption and querying

Reserved system metadata and explicitly configured AdditionalQueryableKeys remain clear and can be queried. Private entries stored inside __oasis_enc__ cannot be filtered by a native metadata predicate. Authorized typed results are decrypted before metadata hydration/tracking; this does not make their encrypted fields searchable in storage.

## Source and evidence

- [Expression and capability contract](CQL_EXPRESSION_CONTRACT.md).
- [Execution, typed results and write-back](CQL_EXECUTION_PIPELINE.md).
- [SQLite native translation](CQL_SQLITE_NATIVE.md).
- [Metadata type/null semantics](CQL_SCALAR_METADATA.md).
- [Current implementation status](IMPLEMENTATION_STATUS.md) and [exact verification epochs](INTEGRATION_BOUNDARY.md).

Executable native examples live in Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests: CosmicCqlTests.cs, CosmicCqlScalarMetadataTests.cs, CosmicCqlConcurrencyTests.cs and CosmicAuthenticatedMetadataTests.cs. These are fixture-backed tests, not production deployment examples. Historical test totals in the linked checkpoint documents describe those specific runs, not a CQL completion percentage.


Optional child graphs now use WithReadAccess + WithChildren in the SDK, or the bounded graph object on the same authenticated HTTP route. The three native adapters enforce access before child limits; public DTOs omit hidden membership/provider state. See the manual/HTTP contract for shape, consistency, budgets and safe root-only edits, and example4 in the C#/HTTP demos.
