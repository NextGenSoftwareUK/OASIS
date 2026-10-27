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

General entity stages after paging (Q01f) remain unfinished on SQLite/MongoDB/Neo4j. A shared explicit `source.Compose()` contract now captures later Where/order/Skip/Take stages without changing the original source; it executes only through adapters implementing the independent entity-stage interface. The standard IQueryable bridge now preserves these stages and rebinds captured values per execution. SQLite now enrolls native entity filter/order/window stages, with one serializable read owner, source row/payload and cumulative stage-work limits. Focused native and V2 tests pass, including typed hydration/change tracking, deferred capture, graph visibility/depth under ProviderSnapshot, and protected traversal cycles/limits before later empty windows. Full cross-database matrix and staged terminal/projection integration remain pending. MongoDB now implements entity stages through the same native snapshot/deadline owner as row and graph queries: it validates bounded source and stage outcomes before later windows, without managed filtering/sorting. Compiler/admission tests and 18 live native cases pass on MongoDB (also on SQLite): source windows, closures/tracking, relationship error visibility, source/work budgets and graph depth/visibility/snapshot. Neo4j now implements these entity stages in one native statement, preserving bounded input references, stage ordering, protected traversal outcomes and cumulative work across later windows. Its existing row owner validates streamed input snapshots and final output budgets. Neo graph consistency is ProviderDefault; ProviderSnapshot requests return CQL_CONSISTENCY_UNSUPPORTED. All three have live stage acceptance; staged element/projection and remaining numeric terminals still require integration. Staged element/projection and remaining numeric terminals still reject pending their integration; no client-side filtering substitutes for it.

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

Entity-stage projection now has a shared foundation: `CosmicEntityCompositionQuery<T>.Select(selector)` captures a `CosmicEntityProjectionQuery<T,TResult>` without invoking DTO constructors. Its plan retains the unchanged source page and every entity stage. `HolonManager.ExecuteEntityProjectionQueryAsync` requires `ICosmicQueryEntityProjectionStorageProvider` with independent enrollment, actual stage capabilities and exact projection domains. The existing projection owner validates all native cells and output-window bounds before DTO construction. SQLite now enrolls native entity-stage projection:47 CQL tests and10 live cases pass. Source/stage proofs share the row/count helper, with selected-cell decoding under the same snapshot/budget and final ordinal. Mongo/Neo enrollment, standard Select and selected-stage/numeric integration remain pending. Broader provider demo now takes priority.
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
Numeric metadata, graph expansion, projection and mixed numeric semantics
are rejected during admission. Compound ordering
requires an explicitly configured Cosmos composite index; no index is created by
CQL. Id ordering is native-proven; broader index/operator acceptance remains open.

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

Native emulator evidence: 34 focused tests plus a twenty-three-case public-provider gate.
Hosted Azure, manager/usage integration and the remaining common demo features
remain unverified. See [Cosmos admission report](CQL_COSMOS_NATIVE_ADMISSION.md).
