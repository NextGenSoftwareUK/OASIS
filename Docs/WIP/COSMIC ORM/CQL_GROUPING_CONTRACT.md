# CQL GroupBy contract

23 October 2026. Key and grouped-result selection capture are implemented; planning, grouped result validation/execution and native adapters are not yet enrolled. This contract preserves the full grouping requirement rather than declaring completion from capture alone.

## Key capture

`source.GroupBy(h => h.Name)` captures a scalar key. Composite keys use the existing detached projection shape, for example:

```csharp
var grouped = source.GroupBy(h => new
{
    h.Version,
    City = CosmicQueryMetadata.Value<string>(h, "location", "city")
});
```

Construction performs no I/O and invokes no selected getters, arbitrary methods, DTO constructors or setters. It reuses the projection compiler. Keys contain one to eight exact scalar columns. Source scope, trusted actor, predicates, relationships, ordering/window, graph shape and consistency are retained unchanged. Failed source results propagate their original error.

`CosmicGroupingOptions.Create` admits one to 10,000 groups (default1,000) and one to32MiB of detached output (default32MiB). These are output bounds, not an input truncation permission. Native execution requires independent input-work/memory/time budgets and an explicit excess-work error. No adapter currently advertises grouping.

## Native identity and execution requirements

Access precedes participation. Source Skip/Take apply before grouping; graph output expansion is not aggregate participation. Protected ownership/traversal checks remain outside windows and reductions. Grouping an empty source returns zero groups.

Every key column has one declared CLR domain. Wrong types fail, rather than coerce into an existing group. Required missing/null values fail; nullable missing and explicit null share the null key, matching the declared nullable projection semantics. String null is also a null key. Invalid strings fail. String equality is exact Unicode scalar equality, with deterministic scalar ordering rather than a database locale. GUID identity is its logical value across admitted persisted representations. Dates compare exact UTC ticks; unsupported native date reads reject before execution. Enum keys retain the declared enum domain and require defined values. Composite equality compares every typed component; numeric strings and integers cannot collapse into one group.

Native plans must validate key read permissions and every selected aggregate independently. Row, Select or scalar capabilities cannot grant GroupBy. All-target preflight and actual-adapter readmission precede I/O. Borrowed plans must fail without a native attempt. Detached grouped publication must validate the entire buffered result before invoking any user result constructor, and must not create tracked holons.

Default group order must be deterministic by the composite key (ascending, null first). Post-group filters, ordering and paging require explicit native composition, not a manager evaluator. A result bound detects excess groups and returns CQL_QUERY_LIMIT; it never publishes the first N groups as an apparently complete result. An aggregate input budget is independent of group count and source row-publication limits. Traversal, invalid input, overflow, cancellation or provider failure cannot yield partial grouped output.

Native grouped Count/LongCount, Min/Max and exact integer Sum/Average must retain their existing scalar empty/null/domain/overflow semantics inside each group. Full grouping scope includes grouped selection, multiple aggregates and explicit post-group composition. A first Count-only slice cannot close this gate. Federated groups remain per provider until explicit logical identity/conflict/merge/accounting semantics are implemented.

## Current evidence and remaining gates

Both Core targets build (netstandard2.1/net10.0). Full Core/HTTP suite:2,169 passed, zero skips, including14 new key-capture controls. Evidence: TestResults/CoreCurrent/cql-grouping-foundation-current.trx and TestResults/CosmicCqlDemo/grouping-foundation-final.log. The first test compilation used `.Result` on the direct query factory; corrected before the passing run. Core production key capture, Shared rebuild and the unit consumer share the same new Core assembly.

Still required: grouped result validation/materialization, operation-specific planning/readmission, existing physical-attempt owner integration, native MongoDB/SQLite/Neo4j reductions and fault controls, bounded HTTP description, spaced/commented examples and live three-provider acceptance. No native grouping, GroupBy transport or completion claim follows from key capture.


## Grouped selection capture — verified

The typed selection is `grouped.Select(g => new { g.Key.Name, Rows = g.LongCount(), Total = g.Sum(h => h.Version), Mean = g.Average(h => h.Version) })`. The expression parameter is a non-enumerable CQL grouping contract, with Key and native aggregate terminals only. Its integer Average overloads return Decimal/nullable Decimal consistently with the existing CQL scalar API. Arbitrary method calls, grouped arithmetic and aggregate selector code are rejected rather than evaluated on the client.

Flat result scalar/anonymous/DTO construction shares one detached result-shape parser with ordinary Select. Composite keys are selected through explicit flat members. Key slots retain the original key-column index; aggregate slots retain operation, typed selector and result domain. Capture does not invoke result constructors. All key read domains must be admitted even if a result omits the key columns. Output columns remain bounded and aliases unique. All 13 new controls pass in the full 2,182-test suite, zero skips. They cover key slots, multiple aggregate domains, Decimal/nullable Average, deferred DTO constructors, arbitrary code/arithmetic rejection, failed/missing inputs and the 32/33-column boundary. Both Core targets build. Ordinary Select now uses the same extracted shape parser; its existing regression tests pass in the same full run. This proves capture only, not native grouped execution.


```csharp
var selected = source
    .GroupBy(h => new { h.Version, h.Name })
    .Select(g => new
    {
        g.Key.Name,
        Rows = g.LongCount(),
        Total = g.Sum(h => h.Version),
        Mean = g.Average(h => h.Version)
    });
```

The selection remains an immutable intent; no provider advertises GroupBy execution. `Mean` has Decimal type. It uses the CQL grouping contract rather than Enumerable.Average's Double result. Count/LongCount have no predicate overload; filter the source explicitly. Unsupported grouped arithmetic and arbitrary selector code fail capture instead of becoming a client evaluator.

Evidence: TestResults/CoreCurrent/cql-grouped-selection-current.trx and cql-grouped-selection-final-current.trx; TestResults/CosmicCqlDemo/grouped-selection.log, grouped-selection-final.log and grouped-selection-artifacts.json. Private Core297b971ab628a763848c5f39a649d8c13570a128 is paired with the parent gitlink/manifest. This extends the earlier key-only checkpoint and leaves native grouping/HTTP/demos open.
