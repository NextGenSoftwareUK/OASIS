# CQL developer manual

Current support and limitations: [parity checklist](CQL_PARITY_CHECKLIST.md). Active work/commands: [CURRENT_STATUS](CURRENT_STATUS.md).

Start with the maintained, spaced/commented [C# and HTTP demo](../../../Examples/CosmicCqlDemo/README.md).
CQL captures immutable query intent, plans against explicit eligible provider capabilities, then executes through native adapters and the V2 HolonManager owner.
Check every OASISResult error/acknowledgement before reading Result. Unsupported semantics and exhausted work limits are errors.

## Reference map

| Topic | Detailed preserved guide |
|---|---|
| C# query construction, provider scope, metadata, filters/order/windows | [Full manual](Archive/CQL_MANUAL_history_2026-10-27.md#cql-developer-manual) |
| HTTP/API, SDK examples, typed hydration, projections, joins/grouping, scalar domains | [Full manual](Archive/CQL_MANUAL_history_2026-10-27.md) and maintained demo |
| Child/descendant filtering and graph loading | [Graph reference](Archive/CQL_GRAPH_LOADING.md), [relationships](Archive/CQL_RELATIONSHIPS.md) |
| Native provider details | [MongoDB](Archive/CQL_MONGO_NATIVE.md), [SQLite](Archive/CQL_SQLITE_NATIVE.md), [Neo4j](Archive/CQL_NEO4J_NATIVE.md) |
| Projected All and source-composition caveats | [Active contract](CQL_PROJECTED_ALL_EXECUTION_CONTRACT.md) |
| Exact per-database feature support/proofs | [Parity checklist](CQL_PARITY_CHECKLIST.md) |

Detailed references retain historical checkpoints; use the checklist/current status for present support, not old dated completion claims.
Investor-demo rows, projected All (Q23e) and source relationship composition (Q49a) are green on the three focus stores; this does not mean full EF/NHibernate parity.
Source composition captures nested child/descendant predicates and AND/OR/NOT through the ordinary Where/WhereAnyChild/WhereAnyDescendant builders. Independent capability and expansion admission precede native execution. Protected scope errors survive Boolean short-circuiting and empty windows. See the216-check source matrix and168-check selected-All gate linked from the checklist; the runnable developer walkthrough is linked below.

## Source composition and projected All in C#

[SourceAndProjectedAllDemo.cs](../../../Examples/CosmicCqlDemo/SourceAndProjectedAllDemo.cs)
contains eight complete examples with error handling and capability-aware planning.
Its host supplies a V2 HolonManager, one activated native adapter and owned fixture
IDs; see the [demo runner instructions](../../../Examples/CosmicCqlDemo/README.md).

| Example | What it demonstrates |
|---|---|
| 1 | WhereAnyChild with bounded AnyDescendant, then native entity rows |
| 2 | NOT relationship predicate followed by native LongCount |
| 3 | Owner/public visibility at every hop before a flat projection |
| 4–5 | Select, selected Where/OrderBy/Skip/Take, then native All; true and false |
| 6–7 | Empty selected/source windows produce true after successful source validation |
| 8 | A true OR branch and empty page cannot conceal a protected CQL_QUERY_LIMIT error |

Use an explicit provider scope. Bound descendant depth and visited nodes. Capture
query intent, check each OASISResult, and plan against the actual adapter's query,
projection and composition capabilities before passing the plan to HolonManager.
A returned false Boolean is successful execution with a counterexample. An error
has no successful Boolean or partial result to consume.

Source predicates choose holons before selection. Selected predicates operate on
the selected aliases after Select. All reduces the final selected sequence after
its filters and windows. CQL performs these operations natively; developers do not
need to fetch records and call Enumerable.All. The result preserves a separate
Boolean per physical provider; no distributed snapshot or consensus is implied.
## Standard IQueryable bridge — Q01 in progress

`CosmicLinq.Create(manager, adapter, source, maximumResults)` returns an
`OASISResult<IQueryable<T>>`. Supply one explicitly named matching provider in the
source scope and check the factory result before composing a query. `T` currently
requires `IHolon` and a public parameterless constructor.

The initial increment supports standard `Queryable.Where`, `OrderBy`/`ThenBy`
families, `Skip` and `Take`, followed by synchronous enumeration or
`await query.ToCosmicListAsync(cancellationToken)`. The async form preserves an
`OASISResult<IReadOnlyList<T>>`; standard enumeration throws `CosmicLinqException`
with the real CQL `Code` on failure. Output bounds are explicit and overflow is an
error, never silent truncation. Negative standard Skip/Take counts follow LINQ
semantics without changing the stricter fluent CQL builders.

Captured variables bind for each execution, then the resulting CQL intent is
frozen for admission and native dispatch. Changing a variable before a subsequent
execution changes that execution's parameters. The existing fluent CosmicQuery
API continues to freeze values during construction.

Queryable repeated Select remains unimplemented. Staged reductions have the
explicit provider/domain enrollment documented below; indexed Where and custom comparers are also
unsupported. No client Enumerable fallback runs. Federated sequences retain their
separate explicit copy/merge contracts. Q01 is **not complete**; see its red row
and [native row tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqAcceptanceTests.cs).
Standard `Count()`, `LongCount()`, `Any()` and `All(predicate)` now execute through
native scalar plans and V2 HolonManager; they do not enumerate entity rows.
For example, `query.Skip(10).Take(5).Count()` counts that source window, while
`query.Take(5).All(row => row.IsActive)` tests the predicate after that window.

Entity Count/LongCount/Any predicate overloads now work after Skip/Take on
MongoDB, SQLite and Neo4j. The independently enrolled
`SupportsPostWindowCardinality` contract keeps the terminal predicate separate
from the immutable source page. Matching rows are reduced natively through the
same protected scalar predicate pipeline used by All. Child/descendant grammar
and traversal budgets retain their explicit permissions.

```csharp
// Count matching entities in the first page, preserving the page boundary.
var page = query
    .OrderBy(row => row.Name)
    .Skip(10)
    .Take(5);

var activeCount = page.Count(row => row.IsActive);
var hasActive = page.Any(row => row.IsActive);
var matchingParents = page.LongCount(row => row.Children.Any(child => child.Name == "coffee"));
```

Terminal descendant scopes are visited only for rows in the selected source
page. Protected source errors are still checked before that page, including an
empty page. Protected terminal traversal errors cannot be hidden by a business
true-OR/false-AND expression. Fluent CQL exposes Count(predicate),
LongCount(predicate) and Any(predicate) on both source queries and OASISResult
source builders; captured fluent parameters stay frozen as usual.
Standard scalar/flat-record `Select` now supports selected `Where`, ordering and
paging, followed by synchronous or async enumeration or native selected `All`.
For example, after checking `CosmicLinq.Create(...)`:

```csharp
var selected = created.Result
    .Select(row => new { row.Name, row.Version })
    .Where(value => value.Version > 0)
    .OrderBy(value => value.Name)
    .Take(5);

var rows = await selected.ToCosmicListAsync(cancellationToken);
if (rows.IsError)
    throw new InvalidOperationException(rows.ErrorCode + ": " + rows.Message);

bool allMatch = selected.All(value => value.Name == expectedName);
```

Selected aliases bind to native columns, not hydrated holons. Each execution
rebinds captured variables in source and selected predicates. The projection
planner or composition planner admits the exact stages; selected All uses its
independent native reduction owner. Unsupported selectors never compile to
client delegates, and missing projection capability never fetches entity rows.
Entity sequences now support standard First/FirstOrDefault, Single/SingleOrDefault,
Last/LastOrDefault and ElementAt/ElementAtOrDefault (Int32 and Index). The bridge
uses CosmicElementPlanner and V2 typed hydration; it never fetches a whole list to
pick an element locally. Last and from-end indexing require the existing explicit
ordering/tail capabilities.

```csharp
var first = query.FirstOrDefault();
var last = query.OrderBy(row => row.Name).LastOrDefault();
var fromEnd = query.OrderBy(row => row.Name).ElementAt(^1);
```

Supplied OrDefault values are returned only after successful empty-sequence proof.
Native errors and multiple matches never return that supplied value. Single checks
cardinality before invoking application entity constructors. Empty/multiple/index
errors preserve their CQL codes through CosmicLinqException. Entity terminal
predicates after a source window remain explicitly unsupported; they are not moved
before the window.

Selected scalar and flat-record sequences now support First/FirstOrDefault,
Single/SingleOrDefault, Last/LastOrDefault and ElementAt/ElementAtOrDefault
(Int32 and Index). Predicates remain after all preceding selected stages.
The shared V2 composition owner validates native stage proofs and Single's
cardinality before calling a DTO constructor. A present null is distinguished
from an empty sequence, including with a caller-supplied OrDefault value.
Tail operations require explicit source or selected ordering and independently
enrolled native reversal; ordering and windows remain in their original positions.

```csharp
// Project only the values the application needs, then page that selected sequence.
var summaries = query
    .Select(row => new { row.Name, row.Version })
    .OrderBy(value => value.Name)
    .Take(10);

// Each terminal runs through native composition and the V2 manager owner.
var firstSummary = summaries.FirstOrDefault();
var lastSummary = summaries.LastOrDefault();
var secondFromEnd = summaries.ElementAtOrDefault(^2);

// This predicate runs after Take(10), retaining the selected window's meaning.
var firstRevisionTwo = summaries.FirstOrDefault(value => value.Version == 2);
```

Direct async users can plan with `CosmicProjectionElementPlanner.Plan` and call
`HolonManager.ExecuteProjectionElementQueryAsync`. Its result exposes HasValue
separately from Value. Empty/multiple/index errors use CQL_SEQUENCE_EMPTY,
CQL_SEQUENCE_MULTIPLE and CQL_INDEX_OUT_OF_RANGE. Input, payload, stage-work and
proof budgets remain enforced even when the terminal returns zero or one value.
Standard selector Min/Max/Sum and decimal/Single/Double Average now dispatch through
native scalar capabilities. An unstaged scalar selection can use the corresponding
parameterless aggregate, for example `query.Select(row => row.Version).Sum()`.
This is a typed path transformation into the same native aggregate, with no DTO
construction, row fetching or client sum. Exact Int64 values beyond2^53, decimal
precision, floating means, nullable Sum and empty inputs are verified natively.

Int32 and nullable Int32 Average now support the standard double/double? result
through independent `SupportsInt32DoubleAverage` capability enrollment on the
three native providers. Existing fluent `Average(int-selector)` still returns
its exact decimal result. `AverageAsDouble(int-selector)` exposes the explicit
new fluent result contract; standard Queryable Average selects it automatically.

The shared numeric owner validates native high/low/count bounds, converts the
bounded Int64 sum once to double, then divides by the count. No decimal result or
input rows are converted on the client. This matches the pinned [.NET10 Int32
Average reference](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Linq/src/System/Linq/Average.cs).
Tests compare standard runtime results, nullable/empty behavior, boundary packets
and a sum above binary64's exact integer range.

MongoDB and SQLite now enroll `SupportsInt64DoubleAverage`: standard `Average(long)` and
`Average(long?)`, including unstaged scalar Select, return double/double?. The
fluent `AverageAsDouble(long-selector)` exposes the same explicit result mode.
MongoDB computes ordered Decimal128 prefix sums **after** source paging and
returns a mandatory Boolean overflow acknowledgement alongside exact sum limbs
and count. Any visited prefix outside Int64 throws `CQL_SCALAR_OVERFLOW`, even
when later cancellation makes the final sum fit. Nullable inputs do not
participate; empty required means fail and empty nullable means return null.
SQLite runs a connection-owned aggregate in explicit SQL aggregate order,
using a checked Int64 total and an overflow latch. Source paging precedes this
accumulator; exact native sum limbs/count are validated by the shared owner.
The hook checks the read budget and is removed when its owner is disposed.
Neo4j collects ordered participating scalar values inside its native query
**after** source paging, then uses a guarded Cypher reduction. Sign-specific
boundary checks precede every addition and an overflow flag remains latched.
A mandatory Boolean acknowledgement accompanies its exact limb/count packet.
This server-side collection uses memory proportional to the selected input count;
the numeric work bound, transaction timeout and server memory limits still apply.
No selected rows are fetched for a client accumulator. Existing fluent `Average(long)`
keeps its decimal result. Aggregates after selected result stages require the independent native
selected-reduction contract described below.
Q01 remains red.
## Selected Count, LongCount and Any

Standard selected scalar and flat-record sequences now support Count, LongCount
and Any, including predicate overloads. Predicates execute after all preceding
source and selected windows. Selected null values count as rows. These scalar
terminals construct zero DTOs and preserve native error/cancellation codes.

```csharp
// Count a selected page; this predicate runs after Take(10).
var selectedPage = query
    .Select(row => new { row.Name, row.Version })
    .OrderBy(value => value.Name)
    .Take(10);

var pageCount = selectedPage.Count();
var revisionTwoCount = selectedPage.LongCount(value => value.Version == 2);
var hasRevisionTwo = selectedPage.Any(value => value.Version == 2);
```

The native provider independently advertises SupportsCardinality. SQLite returns
count(*) inside its serializable read snapshot; MongoDB returns a count facet
under its existing snapshot session; Neo4j returns its native source/final-stage
count header. The shared owner validates the acknowledgement against complete
source/stage evidence and enforces input, payload, proof and terminal-work bounds.
The default selected-input limit is1,000 rows; configurable composition options
allow up to10,000. Exceeding an admitted bound returns an error. Any validates
complete bounded participation before returning its Boolean result.

Direct async callers use CosmicProjectedCardinalityPlanner.Plan with the exact
Count/int, LongCount/long or Any/bool result domain, then
HolonManager.ExecuteProjectedCardinalityQueryAsync. Results retain per-provider
provenance. The standard IQueryable bridge remains bound to one named provider.
Entity element predicate overloads after source windows remain pending Q01 work.

## Numeric reductions after selected stages — current enrollment

SQLite, MongoDB and Neo4j now support Sum/Average over selected Int32/Int64 values and their nullable
forms, including exact column selectors from flat records. The selected filters,
ordering and windows run before the native reduction. No DTO is constructed.
Standard integer Average returns double/double?; direct CQL plans can retain the
existing decimal/decimal? integer-mean result contract. Nullable Sum returns zero
for an empty/all-null input; required Average reports an empty-sequence error.

```csharp
// Enrolled on SQLite, MongoDB and Neo4j.
var revisions = query
    .Select(row => row.Version)
    .Where(value => value > 0)
    .OrderBy(value => value)
    .Skip(1)
    .Take(5);

var revisionSum = revisions.Sum();
var revisionMean = revisions.Average();

// A record selector binds a numeric column without constructing the records.
var page = query
    .Select(row => new { row.Name, row.Version })
    .OrderBy(value => value.Name)
    .Take(5);

var pageRevisionSum = page.Sum(value => value.Version);
```

The shared planner admits NumericReductions separately from ordinary projection,
scalar and cardinality permissions. Exact native words and participating counts
are validated under the selected snapshot owner before normalization. Int64 double
means additionally require checked-prefix evidence after every selected window.
Complete source/stage participation and terminal work/proof budgets still apply.
SQLite, MongoDB and Neo4j also support staged decimal, Single, Double and nullable Sum/Average. Direct CQL supports staged UInt64/nullable UInt64 Sum and decimal-result Average; standard Queryable does not define UInt64 Sum. Selected Min/Max (Q01c) is enrolled on SQLite, MongoDB and Neo4j.

```csharp
// SQLite: reduce the selected page using the exact native decimal kernel.
var prices = query
    .Select(row => CosmicQueryMetadata.Value<decimal>(row, "price"))
    .OrderBy(price => price)
    .Take(10);

var total = prices.Sum();
var mean = prices.Average();
```

MongoDB runs selected integer reductions in the existing snapshot session. Its
shared exact numeric kernel receives the selected column and final stage rank;
checked means therefore follow every selected ordering/window. A native facet
returns an explicit zero-word acknowledgement on empty input. The strict decoder
requires its exact integer/checked-prefix schema before shared normalization.

Neo4j reduces the captured final selected positions within its existing single
query. Exact integer words and sign-guarded prefix evidence are acknowledged in
one terminal record, including an explicit empty packet. Complete selected proof
and numeric participation are validated before the result is published. This
retains the selected owner’s existing consistency contract; it adds no separate
MATCH or stronger database snapshot promise.

Selected Min/Max (Q01c) has a shared V2 plan/executor foundation; all three databases now pass native acceptance. Calls fail explicitly until enrolled; source/unstaged extrema support is unchanged. The new owner preserves windows, ignores null participants and reads the winning field without constructing a selected record.

SQLite, MongoDB and Neo4j now enroll selected Min/Max (Q01c) for its exact selected ordering domains. For example, `prices.Take(10).Min()` reduces that page; `page.Max(value => value.Version)` reads the winning field without constructing records. Nullable/reference Min/Max ignore null/missing cells and return null when none participate; required value types report `CQL_EMPTY_SEQUENCE`. The218-case native proof covers33 domains and record/window behavior. All three pass the same218-case matrix. String ordering follows the declared CQL ordinal contract.

```csharp
// All three stores: the page is fixed before Min/Max.
var page = query
    .Select(row => new { row.Name, row.Version })
    .OrderBy(row => row.Name)
    .Skip(1)
    .Take(10);

// Native extrema read the winning field; no selected records are constructed.
var minimumVersion = page.Min(row => row.Version);
var maximumVersion = page.Max(row => row.Version);
```

Repeated Select (Q01d) is verified on SQLite, MongoDB and Neo4j with26 native cases per store. It supports scalar/flat-record rename, reorder, duplicate, drop and identity selection, preserving earlier windows and filters. Computations/conversions, nested shapes and indexed selectors remain explicit errors. Intermediate records are not constructed; retained native inputs remain subject to capability/data validation and payload/work budgets.

```csharp
// Select again without constructing the intermediate anonymous records.
var page = query
    .Select(row => new { row.Name, row.Version })
    .OrderBy(row => row.Name)
    .Skip(1)
    .Take(10);

// Rename a field, then choose a scalar. The earlier page stays fixed.
var revisions = page
    .Select(row => new { Revision = row.Version })
    .Select(row => row.Revision);

var count = revisions.Count();
var maximum = revisions.Max();
var values = await revisions.ToCosmicListAsync();
```

Retained native columns remain validated even if the last Select drops them. A selection does not bypass source authorization, data-domain errors or the existing work/payload limits. A closure in an earlier Where is rebound on each standard IQueryable execution; fluent CQL capture remains immutable.

Entity element predicates after Skip/Take (Q01e) now have a shared plan/owner, but all3 native enrollment passes45 cases/store plus Neo snapshot rejection. Such calls return `CQL_CAPABILITY_UNSUPPORTED` until implemented; the predicate is never moved before the source page. SQLite expanded-graph source support is verified. Existing pre-window predicate behavior remains unchanged.

```csharp
// SQLite/MongoDB/Neo4j Q01e: evaluate the predicate within the original page.
var page = query.OrderBy(row => row.Name).Skip(5).Take(10);
var firstAvailable = page.FirstOrDefault(row => row.Version > 1);
var lastAvailable = page.LastOrDefault(row => row.Version > 1);
var onlyMatch = page.Single(row => row.Name == "Coffee");
```

These terminals preserve admitted child-graph depth, snapshot and resource options. Empty terminal results do not expand graphs. Source scopes validate before paging; protected terminal scopes validate within the source page before First/Single/Last can truncate it. Neo4j supports admitted provider-default graphs and rejects ProviderSnapshot requests explicitly.

Neo4j Q01e now passes the same element/graph/visibility cases under its admitted provider-default consistency. A requested `ProviderSnapshot` returns `CQL_CONSISTENCY_UNSUPPORTED`; no snapshot guarantee is inferred from sharing a transaction. Neo post-window element capture admits at most10,000 source-page roots; a larger offset-only page returns `CQL_QUERY_LIMIT` without publishing a truncated match. SQLite/MongoDB snapshot support is unchanged.

General entity stages after paging (Q01f) remain unfinished on SQLite/MongoDB/Neo4j. A shared explicit `source.Compose()` contract now captures later Where/order/Skip/Take stages without changing the original source; it executes only through adapters implementing the independent entity-stage interface. The standard IQueryable bridge now preserves these stages and rebinds captured values per execution. SQLite now enrolls native entity filter/order/window stages, with one serializable read owner, source row/payload and cumulative stage-work limits. Focused native and V2 tests pass, including typed hydration/change tracking, deferred capture, graph visibility/depth under ProviderSnapshot, and protected traversal cycles/limits before later empty windows. Full cross-database matrix and staged terminal/projection integration remain pending. MongoDB now implements entity stages through the same native snapshot/deadline owner as row and graph queries: it validates bounded source and stage outcomes before later windows, without managed filtering/sorting. Compiler/admission tests and 18 live native cases pass on MongoDB (also on SQLite): source windows, closures/tracking, relationship error visibility, source/work budgets and graph depth/visibility/snapshot. Neo4j now implements these entity stages in one native statement, preserving bounded input references, stage ordering, protected traversal outcomes and cumulative work across later windows. Its existing row owner validates streamed input snapshots and final output budgets. Neo graph consistency is ProviderDefault; ProviderSnapshot requests return CQL_CONSISTENCY_UNSUPPORTED. All three have live entity-stage, element and final-projection acceptance. All3 now also support selected stages after entity selection; remaining terminal integration is pending.

Entity-stage cardinality has a separate `ICosmicQueryEntityCountStorageProvider` contract and `HolonManager.ExecuteEntityCountQueryAsync` owner. SQLite now enrolls native entity count: the final SQL relation is reduced inside the same source/stage proof transaction, without application constructors or final graph expansion. MongoDB also enrolls native entity count through its scalar snapshot/deadline owner, validating source/stage proofs before the native reduction. Nine live count cases pass each on SQLite/MongoDB. Neo4j also enrolls native count: size(selected) is emitted independently of source payload proofs, with sticky traversal/work outcomes and one read owner. Nine native count cases pass per database. Standard staged Count/LongCount/Any/All now use this native count contract on all three providers. Terminal predicates append after prior stages; All counts native counterexamples. No entity materialization or final graph expansion substitutes for reduction. A native count must preserve every preceding stage/protected outcome, acknowledge completion, fit source/window bounds and avoid entity materialization or graph expansion as a substitute for reduction.

### Standard cardinality after entity stages

```csharp
// Bind one explicit provider through a HyperDrive V2 HolonManager.
var source = CosmicQuery<Holon>.Create(
    CosmicQueryScope.Named(adapter.QueryProviderType).Result);

var bound = CosmicLinq.Create(manager, adapter, source);
if (bound.IsError)
    throw new InvalidOperationException(bound.Message);

// Take runs first: this filters the first twenty names, not the whole store.
var featured = bound.Result
    .OrderBy(holon => holon.Name)
    .Take(20)
    .Where(holon => holon.Name.StartsWith("coffee"))
    .OrderByDescending(holon => holon.Version);

// Each terminal performs its own independently admitted native count read.
int count = featured.Count();
long longCount = featured.LongCount();
bool any = featured.Any();

// Terminal predicates stay after the page and all preceding stages.
int updated = featured.Count(holon => holon.Version >= 2);
bool allVersioned = featured.All(holon => holon.Version >= 1);

// All over an empty sequence is true; protected errors still propagate
// when they belong to a preceding nonempty stage.
bool emptyAll = featured.Take(0).All(holon => false);
```

These terminals preserve source/stage row, payload and work bounds. `Any`/`All` do not bypass protected traversal outcomes through an early managed short-circuit. Separate terminal calls do not promise the same snapshot across calls. Staged projections and remaining numeric reductions are still pending.

The shared forward-element foundation is available through `CosmicEntityElementPlanner`, `CosmicEntityElementExecutor` and `HolonManager.ExecuteEntityElementQueryAsync`. First/Single/indexed/default operations append native stage windows; ambiguous Single is rejected before application constructors or setters. Typed output uses the existing element hydration/change-tracking owner. Standard staged First/FirstOrDefault/Single/SingleOrDefault and forward Int32 ElementAt/ElementAtOrDefault now pass20 live cases/database. Terminal predicates remain after earlier windows. Last/from-end indexing use native entity reversal; explicit First/Single/Last default-value overloads are now verified on all3.

### Forward element terminals after entity stages

Using the `featured` sequence above:

```csharp
// Returns null if no staged row matches. The predicate stays after the page.
Holon revised = featured.FirstOrDefault(holon => holon.Version >= 2);

// This later page makes cardinality unambiguous before typed hydration.
Holon only = featured.Take(1).SingleOrDefault();

// Indexes address the staged sequence, not the original provider source.
Holon second = featured.ElementAtOrDefault(1);
```

`Single` and `SingleOrDefault` return `CQL_SEQUENCE_MULTIPLE` for multiple matches before application constructors/setters run. Missing required elements produce sequence/index errors; optional forms return null. Negative `Queryable.ElementAt(int)` uses the framework's `ArgumentOutOfRangeException`; direct admitted-plan APIs report their CQL index validation. Protected traversal failures from preceding stages still propagate before a later element window can hide them. Explicit default-value overloads are supported as shown below.

Native entity reversal is independently enrolled through `CosmicEntityStageFeatures.Reverse`. An explicit `CosmicEntityCompositionQuery<T>.Reverse()` stage reverses the current relation after all preceding filters/windows, including logical identity ties. Repeated reversal restores the sequence, and new OrderBy starts a fresh ordering. SQLite renumbers its native ordinal, Mongo reverses effective native keys and identity direction, and Neo reverses bounded native candidate references. Six live cases/provider pass; standard Queryable.Reverse, Last/default/predicate and from-end Index binding now pass12 additional live cases/provider. Last requires explicit source or stage ordering.

### Reversal and tail selection

```csharp
// Reverse applies to the current staged sequence, including every earlier page.
var reversePage = featured.Reverse().Take(3);

Holon tail = featured.LastOrDefault();
Holon revisedTail = featured.LastOrDefault(holon => holon.Version >= 2);

// ^1 is the final staged element; ^2 is the one before it.
Holon penultimate = featured.ElementAtOrDefault(^2);
```

Tail predicates remain after prior stages, and protected traversal errors are validated before the terminal window. Reversal includes logical identity ties; a later OrderBy starts a new ordering. Last requires explicit sequence ordering. `Queryable.ElementAt(^0)` follows the framework's ArgumentOutOfRangeException; its optional form returns null. Staged projection/numeric integration remains pending.

### Supplying an explicit default entity

```csharp
// This object remains caller-owned. CQL does not hydrate it or accept its changes.
var empty = new Holon { Name = "No matching coffee" };

Holon first = featured.FirstOrDefault(empty);
Holon only = featured.SingleOrDefault(holon => holon.Version >= 2, empty);
Holon last = featured.LastOrDefault(holon => holon.Version >= 2, empty);
```

The supplied reference is returned only after a successful native empty result. Present rows use normal typed hydration/tracking. Single ambiguity, protected traversal errors, incomplete acknowledgements and storage failures propagate; none return the supplied default instead.

Entity-stage projection now has a shared foundation: `CosmicEntityCompositionQuery<T>.Select(selector)` captures a `CosmicEntityProjectionQuery<T,TResult>` without invoking DTO constructors. Its plan retains the unchanged source page and every entity stage. `HolonManager.ExecuteEntityProjectionQueryAsync` requires `ICosmicQueryEntityProjectionStorageProvider` with independent enrollment, actual stage capabilities and exact projection domains. The existing projection owner validates all native cells and output-window bounds before DTO construction. SQLite now enrolls native entity-stage projection:47 CQL tests and10 live cases pass. Source/stage proofs share the row/count helper, with selected-cell decoding under the same snapshot/budget and final ordinal. All three providers now enroll native entity projection and final standard Select. Combined selected stages are also verified on all3; remaining terminal integration is pending. Broader provider demo now takes priority.
# Azure Cosmos DB canonical rows (27 October 2026)

Cosmos now implements the standard `ICosmicQueryStorageProvider` row contract.
This is a limited canonical-field profile; it is not yet the complete cloud demo.
Activate the configured provider explicitly before querying. A query never creates
storage or activates a provider implicitly. Bind visibility from authenticated
server context before paging, as for the existing database adapters.

```csharp
// cosmos is an explicitly activated AzureCosmosDBOASIS instance.
// actorId comes from the authenticated server context.
var access = CosmicQueryReadAccess.OwnerOrPublic(actorId);
if (access.IsError)
    throw new InvalidOperationException(access.Message);

var scope = CosmicQueryScope.Named(ProviderType.AzureCosmosDBOASIS);
if (scope.IsError)
    throw new InvalidOperationException(scope.Message);

// Native visibility and predicates run before the native source window.
var query = CosmicQuery<Holon>.Create(scope.Result)
    .WithReadAccess(access.Result)
    .Where(holon => holon.IsActive && holon.Version >= 2)
    .OrderBy(holon => holon.Id)
    .Take(10);

if (query.IsError)
    throw new InvalidOperationException(query.Message);

var plan = CosmicQueryPlanner.Plan(
    query.Result,
    ProviderType.AzureCosmosDBOASIS,
    new[]
    {
        new CosmicQueryTarget(
            ProviderType.AzureCosmosDBOASIS,
            AzureCosmosDBOASIS.CqlCapabilities)
    });

if (plan.IsError)
    throw new InvalidOperationException(plan.Message);

var result = await cosmos.ExecuteCosmicQueryAsync(plan.Result, maximumResults: 10);
if (result.IsError)
    throw new InvalidOperationException(result.Message, result.Exception);

// Complete, canonical leaf records; embedded children are not graph expansion.
foreach (var holon in result.Result)
    Console.WriteLine($"{holon.Id}: {holon.Name}, version {holon.Version}");
```

Current profile supports Boolean composition; canonical Name/Description string
equality/inequality/null, IsActive/IsPublic equality, GUID identity/audit equality,
and Int32 Version comparisons. Nested metadata supports string, Boolean/nullable
Boolean equality/inequality, explicit nulls, existence and null tests. Missing paths
and wrong types do not satisfy a metadata comparison; explicit nullable nulls obey
inequality semantics. Exact-number wire strings remain outside the string domain.
Numeric metadata, graph expansion and mixed numeric semantics
are rejected during admission. Compound ordering
requires an explicitly configured Cosmos composite index; no index is created by
CQL. `AzureCosmosDBOASIS.GetCqlCompositeIndexDefinitions()` returns detached SDK
definitions for explicit deployment setup:

| Composite index | First path | Second path |
|---|---|---|
| A | `/Version` ascending | `/Id` ascending |
| B | `/Version` descending | `/Id` ascending |

Opposite-direction scans cover the other two Version/Id combinations. Merge these
definitions into the existing container policy without discarding other paths or
settings, use the observed container ETag, and verify index readiness before the
demo. The helper performs no network or policy mutation. Duplicate ordering keys
are redundant, and a canonical Id key completes a total order; trailing keys are
removed before SQL construction. All four Version/Id combinations, ties and a
compound-order projection now pass the native emulator gate.

Count, LongCount and Any use the independent `ICosmicQueryScalarStorageProvider`
contract. Build a terminal with `query.Count()`, `query.LongCount()` or `query.Any()`,
plan it with `CosmicScalarPlanner` and a `CosmicScalarTarget` carrying both
`AzureCosmosDBOASIS.CqlCapabilities` and `AzureCosmosDBOASIS.CqlScalarCapabilities`,
then call `cosmos.ExecuteCosmicScalarAsync(plan.Result)` after checking each result.
The native source count is mapped to the immutable source window using exact
integer arithmetic, without fetching rows. This also works for a typed source
whose constructor cannot run. Counts above Cosmos's exact binary64 integer domain
are rejected; Count additionally checks Int32 overflow. Post-window terminal
predicates, All and selected numeric aggregates remain unsupported.

Projection uses the independent `ICosmicQueryProjectionStorageProvider` contract.
`query.Select(holon => new { holon.Id, holon.Name, holon.Version })` captures a
selection without constructing an entity or DTO. Plan with `CosmicProjectionPlanner`
and a `CosmicProjectionTarget` carrying `CqlCapabilities` and
`CqlProjectionCapabilities`, then execute through `CosmicProjectionExecutor` to
validate every provider buffer before constructing any DTO. Direct provider calls
return `CosmicProjectionRow` cells; `CosmicProjectionResults.Materialize` validates
and constructs a single provider's result. Check every `OASISResult` before using
its `Result`.

Canonical and nested string/Boolean metadata selection is admitted. Missing/null
nullable values become null; wrong native domains, encoded numbers selected as
strings and missing non-nullable values reject the result. Source identity and
visibility proofs remain required even when Id is not selected. Selected-result
composition and entity-stage projection are not yet enrolled for Cosmos.

Native emulator evidence: 46 focused provider tests, 47 native cases and five shared
HolonManager/LINQ demo operations.
Hosted Azure, prepared usage admission and the remaining common demo features
remain unverified. See [Cosmos admission report](CQL_COSMOS_NATIVE_ADMISSION.md).

Cosmos save lifecycle remains a separate gap. Existing native ETag replacement
does not enroll `IConditionalHolonStorageProvider` or
`IConditionalHolonDeltaStorageProvider`. An explicit `CosmicWriteOptions` request
currently returns `COSMIC_CONDITIONAL_WRITE_UNSUPPORTED` before activation,
leaving caller identity/version unchanged. Do not replace that request with an
ordinary Save and describe it as atomic COSMIC concurrency or a durable delta.
Both synchronous and asynchronous rejection paths are covered by focused tests.

WEB4's existing row, scalar and projection routes now admit the Cosmos read
profile, provided the host already has an active provider, authenticated actor,
query tariff and required read policy. Other routes keep their original provider
enrollment; this does not enroll Cosmos federation/cursors or composition.
For example, send this body to `POST /api/data/query-cql`:

```json
{
  "Provider": "AzureCosmosDBOASIS",
  "OrderBy": "Id",
  "Skip": 0,
  "Take": 10,
  "Filters": [
    {
      "MetadataPath": ["location", "city"],
      "Scalar": "String",
      "Operator": "Equal",
      "Value": "London"
    }
  ]
}
```

Specify `OrderBy: "Id"` or the declared Version profile: the shared wire default
`Name` ordering is not admitted by Cosmos yet. Native HTTP row, scalar and projection requests now pass through the real
controller/endpoints under controlled test identity and receipt storage. Core
query-tariff admission includes explicitly permitted Cosmos profiles;405 affected
WEB4/tariff tests pass. Hosted Azure, production JWT and durable billing remain
unverified.
Canonical membership is now admitted for GUID, string, Boolean and Int32 fields.
This enables `CosmicQueryReadAccess.AuthoritativeRecords` for Cosmos: the complete
server-authorized catalogue replaces owner/public visibility, including an empty
catalogue that admits no records. Native row, count and projection tests prove a
private catalogue member is visible while public/owned outsiders remain excluded.
Metadata membership remains a separate unsupported capability. The native HTTP gate also verifies catalogue enforcement under prepared admission
and one configured query-attempt receipt per successful row/scalar/projection
request. Anonymous and forged-actor requests reject before provider lookup. The
receipt store and validated identity are controlled fixtures; this is not proof
of production authentication, reservation settlement or billing durability.

# ActivityPub/Mastodon native status points (27 October 2026)

An exact native status-key row profile now works through the shared manager and
LINQ, with public/unlisted visibility and mandatory access checks. See the
[native profile guide and C# example](CQL_ACTIVITYPUB_NATIVE_PROFILE.md).
The WEB4 row-route code is enrolled with explicit server catalogue/tariff admission;
production JWT/durable billing/host deployment and interactive-demo acceptance remain open. Broader metadata/scalar/projection/storage semantics remain open.

## IPFS and SOLID native point reads

Spaced C# examples: `Examples/CosmicCqlDemo/IpfsPointWalkthrough.cs` and
`SolidPointWalkthrough.cs`. Both use HolonManager/LINQ and explicit read policies;
native source gates pass. Exact CID/GUID point rows only; richer index queries
remain open. WEB4 request/compiler/tariff code now admits their explicit row
profiles with OrderBy None, with721 affected controls passing. Native HTTP and
interactive deployment acceptance remain open. See [request examples and limits](CQL_PROTOCOL_DEMO_ADMISSION.md#web4-ipfs--solid-row-admission-implementation).

Native WEB4 HTTP acceptance for IPFS/SOLID point rows now passes against actualKubo/privatePod, using controlled identity/outbox and productioncontroller/filter. Sixrequests/profile include401/400 guards and200rows withoneexplicitqueryreceipt. ProductionJWT/durablebilling/interactivehost deployment remainopen. [Proof and limits](CQL_PROTOCOL_DEMO_ADMISSION.md#native-web4-row--receipt-acceptance-milestone).

IPFS/SOLID point teachinghost is now native-tested withreadableHTML/API and explicitserve/shutdown lifecycle. See [interactive launch guide](CQL_INTERACTIVE_POINT_DEMO.md). This is scoped syntheticpointdemo; combinedeightprovider/commonquerydemo and productionauthbilling remainopen.

Unified demo launcher now selects SQLite coffee or IPFS/SOLID/ActivityPub point hosts. ActivityPub nativeinteractivegate1pass against existingpublicstatus, no posting/authentication. See [commands/evidence/limits](CQL_INTERACTIVE_POINT_DEMO.md). Fourproviders inlauncher doesnotcertify full8provider/commonquerydemo orfullparity.

## Indexed LINQ SDK rows

Use an explicitly authorized schema2 source, registered index reader and trusted
source observer. The same CQL expression binder handles standard Where, ordering,
Skip and Take; execution stays native SQL with independent selected source proofs.

```csharp
// source includes a named logical provider, indexed read requirement and server actor policy.
var created = CosmicLinq.CreateIndexed(manager, index, observer, source);
if (created.IsError)
    throw new InvalidOperationException(created.Message);

var name = "A";
var query = created.Result
    .Where(row => row.Name == name)
    .OrderBy(row => row.Name)
    .Take(5);

// Nothing has executed yet. Closure values bind separately for each read.
var result = await query.ToCosmicIndexedPageAsync();
if (result.IsError)
    throw new InvalidOperationException(result.ErrorCode + ": " + result.Message);

var page = result.Result;
Console.WriteLine(page.Checkpoint.Source.SourceProvider);
Console.WriteLine(page.Checkpoint.Source.ExecutionProvider);
foreach (var row in page.Rows)
{
    // Typed metadata is hydrated and change tracking starts clean.
    Console.WriteLine(row.Name);
}

// page.Receipts aligns with page.Rows; page.Head records the fresh source head.
```

ToCosmicListAsync and sequence enumeration are also available when only rows are
needed; retain the page result when provenance/checkpoint/receipts are required.
Each terminal is a separate execution. Unsupported Select/scalar/element/reverse/
post-window filter shapes reject before reader I/O; there is no client reduction.
Prepared contexts require an exact server-created physical-read tariff policy;
missing policies reject before I/O. See [indexed accounting](CQL_INDEXED_BILLING.md)
for composition, per-RPC receipts and failure semantics. This SDK profile does not
enroll WEB4 or certify original native chain coverage.

Details and acceptance evidence: [indexed SQL rows](CQL_INDEXED_SQL_ROWS.md).

[WEB4 indexed preparation](CQL_INDEXED_WEB4_PREPARATION.md) now captures server alias authorization, immutable query/freshness and conservative reservation bound once. Stock HTTP route/usage binding is verified; native acceptance remains next; this does not certify native coverage or production deployment.

[Indexed HTTP checkpoint](CQL_INDEXED_HTTP.md): actual controller/context tests and full Core4,140/SQLite31/Solana789 gates pass. Exact measured cost, units, attempts and frozen intent are enforced; stock middleware fingerprint/admission integration passes; real-backend/native HTTP and full parity remain open. Register server bindings and existing native factory first, then call `services.AddCqlIndexedReadAdmission()`; see the checkpoint for explicit route policy and middleware ordering.

## Indexed HTTP rows

Use POST `/api/data/query-cql/indexed` with a server-registered alias. The host must
bind its captured preparation and measured reservation before dispatch; automatic
stock usage-middleware integration is still pending. No client endpoint, price,
provider override or fabricated checkpoint is accepted.

```json
{
  "alias": "solana-coffee",
  "query": {
    "orderBy": "Name",
    "take": 5
  }
}
```

The response keeps logical provider and physical execution provider separate,
with public rows, source checkpoint, fresh head and per-row source receipts.
Slot values are strings, including values beyond JavaScript's integer precision.
The server reserves maximum service cost and read units; completed SQL/RPC
attempts are measured individually. Units count read attempts, not result rows.
See [HTTP contract, examples and acceptance limits](CQL_INDEXED_HTTP.md).


The indexed HTTP acceptance suite also runs actual SQLite and canonical Solana
decoding through shipping MVC and stock middleware: rows, empty results, missing
source and wrong-network failures. RPC responses, identity and ledger remain
controlled; see [HTTP evidence and remaining limits](CQL_INDEXED_HTTP.md).

Indexed freshness includes block identity: older minimum/checkpoint/previous slots
require independent finalized anchors, even for empty results. Missing or conflicting
anchors reject the whole page. Custom source observers must implement the bounded
requiredSlots head contract; [details](CQL_CHAIN_QUERY_CONTRACT.md#finalized-anchor-identity-correction-27-october).


### Live chain C# walkthrough

[ChainIndexedWalkthrough.cs](../../../Examples/CosmicCqlDemo/ChainIndexedWalkthrough.cs)
is a spaced, commented shared SDK example with explicit source/index binding,
Where/OrderBy/Take, clean typed rows, empty results and retained source evidence.
It is exercised verbatim by the real Solana/SQLite native gate. Pass an already
published index, trusted source observer, V2 manager, freshness requirement and
server actor policy; the walkthrough itself never submits source writes.

```csharp
var demo = await ChainIndexedWalkthrough.RunAsync(
    manager,
    sqliteIndex,
    solanaObserver,
    freshness,
    serverReadAccess,
    selectedHolonId,
    Console.Out,
    cancellationToken);
```

[Native runner, evidence and limits](CQL_SOLANA_DEMO_ADMISSION.md#live-declared-range-sdk-demo-new-range-not-original-recovery).


### Native chain HTTP acceptance

The reusable Solana runner's `-IndexedHttpDemo` mode exercises shipping HTTP with
the acknowledged native seed and actual index. It runs nine bounded requests
(entity/projected rows and empty results, four replays, unsupported graph) and shuts the host down afterward.
It never creates a replacement seed. [Command and limits](CQL_INDEXED_HTTP.md#live-solana-source-through-shipping-http-27-october).

The accepted wire shape is:

```json
{
  "alias": "solana-native-demo",
  "query": {
    "filters": [
      { "field": "Id", "scalar": "Guid", "operator": "Equal", "value": "<selected-holon-id>" }
    ],
    "orderBy": "Name",
    "take": 1
  }
}
```

Use the synthetic Id from LocalState/COSMIC/SolanaCqlCurrent/seed.acknowledged.json.
The server must own that alias and its actor/source/index/tariff grant; this is
not a command that enrolls the alias in a running deployment. Production JWT and
durable ledger acceptance remain separate. Retain X-OASIS-Operation-Id and source
receipts; slots are returned as strings to preserve integer precision.


### Indexed native projection SDK

Use the shared projection builder/planner and V2 manager. The source query must
already have its explicit indexed freshness and server actor grant. The selected
target names the logical source provider, while the registered reader owns the
physical SQLite execution. Always inspect OASISResult.IsError before using Result.

```csharp
var selected = sourceQuery.Select(holon => new
{
    holon.Id,
    holon.Name,
    holon.Version
});

var target = new CosmicProjectionTarget(
    index.Source.SourceProvider,
    index.IndexedRowCapabilities,
    index.IndexedProjectionCapabilities,
    index.Source);

var plan = CosmicProjectionPlanner.Plan(
    selected.Result,
    index.Source.SourceProvider,
    new[] { target });

var result = await manager.ExecuteIndexedProjectionQueryAsync(
    plan.Result,
    index,
    sourceObserver,
    maximumResults: 100,
    maximumPayloadBytes: 64 * 1024,
    cancellationToken: cancellationToken);

// On success, result.Result.Rows contains detached selected values.
// Checkpoint, Head and Receipts retain the source evidence for those rows.
```

This is the explicit indexed projection SDK; standard IQueryable.Select now uses
the same owner. HTTP now accepts optional columns on the existing indexed route. Canonical Id/Name/Version/VersionId/CreatedByAvatarId/
IsPublic/IsDeleted columns are enrolled; metadata and aggregates remain unsupported.
[Contract, tests and remaining gates](CQL_INDEXED_SQL_ROWS.md#indexed-native-selected-cell-sdk-milestone-27-october).


### Indexed LINQ Select

```csharp
var page = await query
    .Where(holon => holon.Id == selectedHolonId)
    .OrderBy(holon => holon.Name)
    .Take(10)
    .Select(holon => new
    {
        holon.Id,
        holon.Name,
        holon.Version
    })
    .ToCosmicIndexedProjectionPageAsync(cancellationToken);

if (page.IsError)
    throw new InvalidOperationException(page.ErrorCode + ": " + page.Message);

foreach (var item in page.Result.Rows)
    Console.WriteLine($"{item.Name} (version {item.Version})");
```

Here query is the explicitly bound IQueryable returned by CosmicLinq.CreateIndexed.
The reader must advertise selected-column support. Flat repeated Select is admitted;
selected-result filters/windows/aggregates remain unsupported. Put the supported
source operators before Select. Use ToCosmicListAsync or enumeration when consuming
selected values; use the page terminal when retaining source evidence in a demo.
RunProjectionAsync in [ChainIndexedWalkthrough.cs](../../../Examples/CosmicCqlDemo/ChainIndexedWalkthrough.cs)
shows a spaced/commented public DTO example and is tested against the live source.
Anonymous shapes or public parameterless DTO/member assignments are supported;
positional DTO/record constructors without explicit expression member bindings
remain an M17 gap. [Contract and evidence](CQL_INDEXED_SQL_ROWS.md#standard-indexed-linq-select-27-october).


### Indexed HTTP selected values

Add a columns array to the existing /api/data/query-cql/indexed request. The server
alias owns the actual source/index, actor grant, freshness and tariffs. Fields are
capability checked before reservation; columns cannot request the entity metadata
envelope. An omitted/null columns array retains ordinary entity-row behavior.

```json
{
  "alias": "solana-native-demo",
  "query": { "orderBy": "Name", "take": 10 },
  "columns": [
    { "alias": "name", "field": "Name" },
    { "alias": "version", "field": "Version" }
  ]
}
```

On success, mode is indexed-projection. Columns describe the typed cells in each
rows array; checkpoint, finalizedHead and receipts retain provenance/freshness.
[Registration, complete contract and native evidence](CQL_INDEXED_HTTP.md#optional-native-selected-columns-on-indexed-27-october).


### Indexed Count / LongCount / Any SDK

Use a bound CosmicQuery source with explicit indexed freshness and server actor
policy. These scalar builder methods on CosmicQuery also have standard synchronous
Count/LongCount/Any terminals on the indexed IQueryable bridge. The separate reader capability is mandatory.

```csharp
var scalar = sourceQuery.Count(); // Or LongCount() / Any().
if (scalar.IsError)
    throw new InvalidOperationException(scalar.Message);

var target = new CosmicScalarTarget(
    index.Source.SourceProvider,
    index.IndexedRowCapabilities,
    index.IndexedScalarCapabilities,
    index.Source);

var plan = CosmicScalarPlanner.Plan(
    scalar.Result,
    index.Source.SourceProvider,
    new[] { target });

if (plan.IsError)
    throw new InvalidOperationException(plan.ErrorCode + ": " + plan.Message);

var result = await manager.ExecuteIndexedScalarQueryAsync(
    plan.Result,
    index,
    sourceObserver,
    cancellationToken);

if (result.IsError)
    throw new InvalidOperationException(result.ErrorCode + ": " + result.Message);

Console.WriteLine(result.Result.Value);
Console.WriteLine(result.Result.Checkpoint.Position.Slot);
```

SQL counts the actor-visible source window, not a materialized result page.
The returned assurance is authenticated-index-snapshot with fresh finalized
head/anchors. It does not assert per-record source availability or expose global
index manifest statistics. Current SQLite index integrity scanning is bounded to
10,000 history entries; larger-index scaling remains a separate gap. LINQ/HTTP,
terminal predicates and additional aggregates are not enrolled by this milestone.
[Contract and evidence](CQL_INDEXED_SQL_ROWS.md#indexed-native-cardinality-sdk-27-october).


### Ordinary indexed LINQ cardinality

```csharp
var visibleCoffee = query.Where(holon => holon.Name == "Coffee");

var count = visibleCoffee.Count();
var longCount = visibleCoffee.LongCount();
var any = visibleCoffee.Any();

// Source windows retain their normal LINQ position in SQL.
var firstPageCount = visibleCoffee
    .OrderBy(holon => holon.Name)
    .Skip(1)
    .Take(10)
    .Count();
```

query is the explicitly bound indexed IQueryable. Each terminal executes its own
native SQL snapshot and source head/anchor checks. These return primitives and
throw CosmicLinqException on failure; use the asynchronous scalar SDK when you
need OASISResult, cancellation and checkpoint/head evidence. Predicates on an
unwindowed Count/LongCount/Any are supported; predicates after Skip/Take, All and
selected-result/numeric/element terminals remain unsupported before I/O.
RunCardinalityAsync in [ChainIndexedWalkthrough.cs](../../../Examples/CosmicCqlDemo/ChainIndexedWalkthrough.cs)
is a spaced/commented live-tested example. HTTP cardinality remains pending.

### Indexed HTTP cardinality

POST `/api/data/query-cql/indexed/scalar` with `{"alias":"solana-demo","query":{"operation":"LongCount","orderBy":"Id"}}`. The host must explicitly enroll the measured route and alias. Omitted Take counts the whole eligible source; zero creates an empty window. Count/LongCount/Any execute native indexed SQL under the actor grant. LongCount returns an exact decimal string. See [indexed HTTP contract](CQL_INDEXED_HTTP.md#indexed-cardinality-http) for freshness, reservation and assurance limits; live Solana/SQLite HTTP cardinality passes nine cases; production identity/durable settlement remains pending.

### Indexed metadata in C#

Use [ChainIndexedMetadataWalkthrough.cs](../../../Examples/CosmicCqlDemo/ChainIndexedMetadataWalkthrough.cs)
for the spaced, commented example. The trusted host first constructs an explicit
schema3 SqliteCosmicChainIndexStore with HolonQueryableMetadataPolicy selecting
only encryption-exempt roots, then publishes the authenticated index. Source
network/index/profile and actor access remain server-owned. The sample uses
CosmicLinq.CreateIndexed, nested typed metadata markers and ordinary Where/order/
LongCount/Any. Rows retain source evidence; cardinality uses snapshot assurance.

Private metadata stays out of the plaintext index. Encrypted row hydration still
requires manager.MetadataEncryptionPolicy with an authorized key resolver; index
enrollment grants no decryption authority. SDK/actual SQLite acceptance is verified,
including rejection without that runtime context. Declared-range live Solana/SQLite metadata SDK/HTTP and interactive acceptance now pass. Run the demo hub with `-Provider SolanaMetadata -ArchiveCompletedDemo`; add `-VerifyOnly` for bounded acceptance. Production identity/durable billing remain open; see [verified scope and next gates](CQL_INDEXED_METADATA.md).

### Selecting indexed metadata columns

The explicit schema3 profile can now select typed nested metadata alongside
canonical fields with standard LINQ Select, including exact Int64 and nullable
values. Projection roots are independently enrolled; private or wildcard roots
reject during planning/re-admission. Native selected values must match validated
source witnesses before any DTO construction. See the [C# example and acceptance
scope](CQL_INDEXED_METADATA.md#selected-metadata-shared-sdk-and-sqlite-verified).
SDK/actual SQLite and declared-range live Solana selected-metadata HTTP/demo gates are verified. In the SolanaMetadata page, choose Select metadata columns. Exact Int64 values use decimal strings and missing nullable cells use null; production identity/durable settlement remain open.

### Azure Cosmos DB teaching host
From C:/Source/OASIS-cosmic run Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider CosmosDB (or add -VerifyOnly for bounded native acceptance). Open http://127.0.0.1:5088/ after readiness. The page shows C# and real WEB4 JSON for rows, projection and LongCount over the retained local emulator. It uses a controlled server-owned catalogue and receipts, not production JWT/billing. Stop via the page to complete exact fixture cleanup and emulator teardown. See [interactive guide](CQL_INTERACTIVE_POINT_DEMO.md) and [cloud evidence and limits](CQL_COSMOS_NATIVE_ADMISSION.md).


### Selection after entity windows and stages (Q01f, partial)

The explicit entity-projection API is native-verified on SQLite, MongoDB and Neo4j. Final standard IQueryable Select after entity windows/stages is also native-verified on all three. Sync and async enumeration rebind closures on each execution and validate native cells before DTO construction, without constructing source application entities. SQLite, MongoDB and Neo4j now support native Where/order/ThenBy/Skip/Take/Reverse after this selection. Reselection and element/cardinality/numeric terminals after entity selection remain unsupported. Q01f remains red; this is not general LINQ parity.

For an already bound `IQueryable<Holon>` named `query`, the final selection can use standard LINQ:

```csharp
// Keep the first page intact before filtering and reversing it.
// Execution is deferred until enumeration; the trusted host uses V2.
var names = query
    .OrderBy(holon => holon.Name)
    .Take(20)
    .Where(holon => holon.Version >= 1)
    .Reverse()
    .Take(5)
    .Select(holon => holon.Name);

var values = names.ToArray();
```

Do not append selected-stage operators to this entity projection yet. They return explicit unsupported errors rather than evaluating the page locally.

An earlier page must retain its meaning when a later filter/order/selection runs.
For example, select the first twenty names, then filter/reorder that page, then
select columns. The provider must not push the later filter before that page.
Use an already activated adapter and the trusted host's manager/actor:

```csharp
var scope = Require(CosmicQueryScope.Named(provider.QueryProviderType));
var access = Require(CosmicQueryReadAccess.OwnerOrPublic(actorId));

var source = Require(CosmicQuery<Holon>.Create(scope)
    .WithReadAccess(access)
    .OrderBy(holon => holon.Name)
    .Take(20));

var entities = Require(source.Compose());
entities = Require(entities.Where(holon => holon.Version >= 1));
entities = Require(entities.OrderByDescending(holon => holon.Version));
entities = Require(entities.Take(5));

var selected = Require(entities.Select(holon => new
{
    holon.Name,
    holon.Version
}));

var plan = Require(CosmicEntityProjectionPlanner.Plan(
    selected,
    provider.QueryProviderType,
    new CosmicProjectionTarget(
        provider.QueryProviderType,
        provider.QueryCapabilities,
        provider.ProjectionCapabilities),
    provider.EntityCompositionCapabilities));

var result = Require(await manager.ExecuteEntityProjectionQueryAsync(
    plan,
    new ICosmicQueryStorageProvider[] { provider },
    5,
    1024 * 1024));
```

`Require` is the sample boundary helper that throws on an OASISResult error;
applications should handle that result explicitly. Source and stage row/work/byte
limits are independent of final output limits. Neo4j streams canonical input proof and selected cells in one native Cypher query with explicit output positions. Its existing snapshot-consistency restrictions remain; a read transaction is not an upgrade to ProviderSnapshot. Canonical proof records are validated internally without constructing application source entities or result DTOs. Mongo runs source and every staged
protected-scope proof in the same snapshot before publishing selected cells. The
existing projection decoder validates exact domains and payload shape; the shared
owner constructs application DTOs only after complete validation. Empty output
cannot bypass a preceding protected error. There is no managed filtering or
source-entity hydration substitute.

Mongo proof:218focused CQL tests (including3new compiler/admission cases) and10
native cases cover source/stage page order, reversal, Int64/UInt64/Decimal
precision, visibility, protected scopes, source/work/output bounds and invalid
cells before DTO construction. Raw evidence:
TestResults/CoreCurrent/mongo-entity-projection-unit.trx SHA256
D7B10A9E508D573EF7A06A0D887C71CE46F35ED737A4592E2956A32BE0378CBA;
TestResults/Neo4jCurrent/mongo-entity-projection-native.trx SHA256
A72ACC73C6876AFF92870E10DC5740738B2FB57F72741C1BF77FF7BACBB4135F;
native JSON E497533119AEFADBEB32F6CAA00E1C29A761545AC54EC77618EA9CB1D3106940.
Run Scripts/TestHosts/Inspect-CqlDatabaseRetainedStorage.ps1 -MongoEntityProjection
under its own single lease. It uses the existing replica environment, journals a
small absent-at-preflight five-record database, preserves failed outcomes and
verifies exact successful cleanup. It never changes preserved SQLite evidence.

Neo4j proof:149focused CQL tests (including3new compiler/admission cases) and the same10native cases as Mongo/SQLite. Unit TRX TestResults/CoreCurrent/neo-entity-projection-unit.trx SHA256E554D51BBB5F82DF2B800C50B4C3724550E729DC81BA68B12FB1359AA3897DCD. Native TRX TestResults/Neo4jCurrent/neo-entity-projection-native.trx SHA256E3313399375F4C27BDBE0B0FF0E1F61CF10FCDA37421EB38CB7F825AEF6776A1; native JSON A6391BC347A7B59A2306C8A32A74099BE7FBAE75373D18C4093E91F3BA4E90DD. Run Scripts/TestHosts/Inspect-CqlDatabaseRetainedGraph.ps1 -EntityProjection under its own single lease. It reuses the retained container, journals five owned nodes before creation, preserves failed outcomes and verifies exact-ID cleanup before stopping the container. No Mongo service, old SQLite fixture or historical journal is needed by this mode. The new path reuses native cell decoding, canonical source-proof validation and the original entity-stage compiler rather than materializing an entity page for managed Select.


### Explicit capture of selected stages after entity projection

SQLite, MongoDB and Neo4j now execute the combined entity/selected pipeline
natively, with23 common native cases per database. This feature remains part of
red Q01f because other integrations are unfinished.

```csharp
// `query` is an explicitly named-provider IQueryable from CosmicLinq.Create.
// First keep a page of entities, then reverse that page.
var page = query
    .OrderBy(holon => holon.Name)
    .Skip(10)
    .Take(20)
    .Reverse();

// Select native cells, then filter/order/page those selected values.
// The source offset is applied once, before the entity reversal.
var selected = page
    .Select(holon => new { holon.Name, holon.Version })
    .Where(row => row.Version >= 1)
    .OrderBy(row => row.Name)
    .ThenByDescending(row => row.Version)
    .Take(5);

var result = await selected.ToCosmicListAsync();
// Check result.IsError before using result.Result.
```

The explicit builder also supports these operations on
`CosmicEntityProjectionQuery<T,TResult>`, each returning OASISResult. Plan that
combined query with `CosmicProjectionCompositionPlanner.PlanEntity`, supplying
one matching target and the adapter's actual entity/selected capabilities; execute
with V2 `HolonManager.ExecuteProjectionCompositionQueryAsync`.

`ICosmicQueryEntityProjectionCompositionStorageProvider` is independently enrolled.
One native snapshot owner validates the full entity source/stages, then every
selected input cell, then each selected stage. The shared owner validates complete
selected participation/proofs before DTO construction. Input order here is the
final native entity-stage order; the original source ordering is not reapplied.
The flat composition entry point rejects combined plans. No application source
entities are constructed, and no managed filtering/sorting substitutes for native SQL, Mongo aggregation or Cypher.

Explicit captured literals are immutable; standard LINQ closures rebind at each
read. Existing exact-column/domain rules,32 selected-stage and10000-window bounds
apply alongside independent entity-source/work/payload and selected-proof bounds.
Later Take(0) cannot hide invalid selected inputs or protected entity errors.

Combined graph sources, reselection and element/cardinality/numeric terminals
after this selection remain unsupported; ordinary entity-stage terminals and
flat selected terminals retain their existing separate supported scopes.

MongoDB carries the final effective entity ordering and identity direction into
native selected ranks; it never sorts that input back into the original source
order. Source/entity-stage proof, complete selected-input decoding and every
selected stage share the same30second deadline/snapshot session. Nine combined
native cases cover ordering and reversed ties, original offsets, exact Int64/
UInt64/decimal, per-read closures, hidden invalid cells and protected scope/budget
failures. The fixture is removed only after success; a pending journal must be
preserved/reconciled before another run.

Neo4j uses one statement: the existing entity compiler exports its final bounded
relation, the detached-cell compiler captures selected values in entity position
order, and the existing selected-stage compiler consumes those captured cells.
Canonical source snapshots/revisions and sticky traversal/work failures are
streamed before selected-input and stage proofs. The reader validates source
proofs before accepting the selected packet; shared validation still precedes
DTO constructors. Neo4j retains its ProviderDefault consistency restrictions;
this read transaction does not establish ProviderSnapshot support.

## Oracle + Mongo: native content examples (30 October 2026)

The reusable local demo runs four captured CQL predicates on both native stores through HolonManager, preserving provider provenance. Launch from the task worktree with:

```powershell
pwsh -File Scripts/TestHosts/Run-ReusableOracleMongoTests.ps1 -Content -Serve -ArchiveCompletedDemo
```

Open `http://127.0.0.1:5090/`. The runner owns its build lease and retained service lifetime; do not nest leases. Stop through `/demo/shutdown` or the page button. Its temporary Mongo fixture is removed after exact ownership checks; the four retained Oracle records remain unchanged.

```csharp
// Both adapters must already be activated for the trusted configured stores.
// Obtain readable ID catalogues from application policy, not HTTP input.
var presets = ContentFederationPresets.Create(ProviderType.OracleDBOASIS);

// This captures string metadata equality on an explicit Mongo + Oracle scope.
// Each provider translates its predicate natively before the bounded page.
var result = await IdentityFederationWalkthrough.RunQueryAsync(
    manager,
    mongo,
    oracle,
    presets["metadata"],
    readableMongoIds,
    readableOracleIds,
    actorId,
    Console.Out);
```

| Preset | CQL expression | Expected physical rows |
|---|---|---|
| `name` | `h.Name == "Espresso"` | 2:one per provider |
| `description` | `h.Description.Contains("coffee")` | 4:two per provider |
| `metadata` | `CosmicQueryMetadata.Value<string>(h, "category") == "coffee"` | 4:two per provider |
| `combined` | `(h.Name == "Espresso" || h.Name == "Tea") && category == "coffee"` | 2:one per provider |

HTTP uses `POST /api/cql/federation` with `{"preset":"metadata"}` on this loopback teaching host. Unknown fields/presets are rejected; a submitted actor cannot grant access. This local endpoint is distinct from the authenticated deployed WEB4 API. The actual shared C# implementations are in `Examples/CosmicCqlDemo/ContentFederationPresets.cs`, `IdentityFederationWalkthrough.cs` and `IdentityFederationApiHost.cs`.

Oracle's profile admits identity membership/equality, name/description equality/Contains, nested string-metadata equality, AND/OR and bounded paging over at most128 authorized rows/512 native parameters. Case-sensitive strings, literal wildcard characters and object/string type guards are covered by native tests. Unsupported ordering/numeric/null/graph features are explicitly rejected. Moving provider reads do not establish a shared distributed snapshot. Native local proof does not establish hosted OCI acceptance.

### Existing public CQL tab: Oracle local catalogue

Open https://web4.oasisomniverse.one/web4-sandbox.html and choose CQL Playground, section4. Start the reusable local Oracle+Mongo runner using the displayed command, select one of four examples, then OPEN LOCAL QUERY. Only a fixed loopback URL and supported preset name are passed; no hosted bearer, actor or JSON editor body is forwarded. The native page automatically runs the chosen read and echoes its preset in the response. URL startup is limited to all/subset/name/description/metadata/combined; shutdown cannot be auto-invoked through the preset parameter. Oracle is deliberately labelled local; the hosted provider selector remains Mongo/SQLite/Neo. This integration does not establish hosted OCI access.

Verified actual browser path: CQL tab -> Metadata equality -> /?preset=metadata ->4physical rows (Mongo2/Oracle2).17client controls pass; public client bytes match tested source. See CURRENT_STATUS/release report for exact commits and evidence. Stop the local native page after the demo to complete owned fixture teardown.
