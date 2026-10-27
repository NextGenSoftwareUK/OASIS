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

Repeated Select (Q01d) now has a shared exact-column binding implementation; native acceptance is still pending on the three databases. It supports scalar/flat-record rename, reorder, duplicate, drop and identity selection, preserving earlier windows and filters. Computations/conversions, nested shapes and indexed selectors remain explicit errors. Intermediate records are not constructed; retained native inputs remain subject to capability/data validation and payload/work budgets.
