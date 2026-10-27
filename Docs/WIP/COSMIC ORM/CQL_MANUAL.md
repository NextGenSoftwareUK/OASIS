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

Queryable numeric/element terminals, selected cardinality and repeated Select remain
unimplemented and fail explicitly; indexed Where and custom comparers are also
unsupported. No client Enumerable fallback runs. Federated sequences retain their
separate explicit copy/merge contracts. Q01 is **not complete**; see its red row
and [native row tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/CqlLinqAcceptanceTests.cs).
Standard `Count()`, `LongCount()`, `Any()` and `All(predicate)` now execute through
native scalar plans and V2 HolonManager; they do not enumerate entity rows.
For example, `query.Skip(10).Take(5).Count()` counts that source window, while
`query.Take(5).All(row => row.IsActive)` tests the predicate after that window.

Count/LongCount/Any predicate overloads work on unwindowed sources. A predicate
on one of these terminals after Skip/Take currently returns the explicit
`CQL_COMPOSITION_UNSUPPORTED` exception. It is not moved before the window.
This remains Q01 work; All already has an independent post-window predicate.
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