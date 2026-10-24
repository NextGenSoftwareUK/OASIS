# CQL GroupBy contract

23 October 2026. Core captures, plans, validates and executes grouped intents through the V2 physical-attempt owner. SQLite native reduction is verified below; MongoDB/Neo4j native adapters, grouped HTTP/demos and post-group composition remain open.

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

## Initial key-capture evidence and remaining gates

Both Core targets build (netstandard2.1/net10.0). Full Core/HTTP suite:2,169 passed, zero skips, including14 new key-capture controls. Evidence: TestResults/CoreCurrent/cql-grouping-foundation-current.trx and TestResults/CosmicCqlDemo/grouping-foundation-final.log. The first test compilation used `.Result` on the direct query factory; corrected before the passing run. Core production key capture, Shared rebuild and the unit consumer share the same new Core assembly.

Still required: existing physical-attempt owner integration, native MongoDB/SQLite/Neo4j reductions and fault controls, bounded HTTP description, spaced/commented examples and live three-provider acceptance. No native grouping, GroupBy transport or completion claim follows from key capture.


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


## Grouped planning/readmission — verified

`CosmicGroupedCapabilities` is an independent grouping enrollment profile. It binds explicit key projection permissions, aggregate scalar permissions, group count, result width and payload ceilings. These permissions do not follow automatically from row/Select/scalar support. All key components are admitted even when the result omits them. Every aggregate uses the same internal operation/read-path admission owner as ordinary scalar queries.

`CosmicGroupedPlanner.Plan` snapshots a bounded eligible target set and delegates source scope/feature planning to the existing query planner. Every selected target must admit the whole grouped shape before a plan is returned. A single-provider native call must use `CosmicGroupedPlanner.Admit` against its actual query and grouping contracts; planned/borrowed permissions cannot replace actual ones. Source windows and consistency remain present during readmission. Default/named/all-eligible scope semantics remain unchanged; no provider substitution or input-row evaluator was added.

Both Core targets build and the full suite passes **2,200 tests, zero skips**, including18 new planning cases. Controls cover immutable target snapshots, omitted/composite key permissions, independent operation/read-domain checks, actual-profile/source rejection, group/width/payload bounds, invalid/duplicate target sets and an unsupported later all-eligible target. Evidence: TestResults/CoreCurrent/cql-grouped-planning-{current,final-current}.trx and TestResults/CosmicCqlDemo/grouped-planning{,-final}.log, grouped-planning-artifacts.json. Private Corea4e3e6d0a69b524d3a7b2cd0e87a9eb0dadf71f2 is paired with the parent pin.

This is planning evidence, not native GroupBy execution certification. The existing physical-attempt owner, native adapters, HTTP and demos remain required. No current provider advertises grouping execution.


## Grouped result validation/materialization — verified

The native row protocol carries a required positive input count and immutable key cells separately from selected result cells. Selected Count/LongCount must agree with that proof, and summed group input cannot exceed a composed source window even when counts are omitted. No default count or inferred proof is permitted. Every key component is validated even when omitted from the result. Selected key slots must agree with the identity cells. Rows must have strictly increasing composite keys (null first, Unicode scalar string order); duplicates and unordered buffers fail rather than being silently merged or sorted in the manager.

Validation snapshots the whole bounded buffer before constructing any DTO. Group cardinality cannot exceed the composed source window, and group-count/payload limits are independent. The payload budget includes identity/proof cells as well as selected cells. Count/LongCount must be positive for a published group; nullable Sum still cannot publish null; Average must fit its declared integer input range. Required/null/CLR domains, finite numbers, UTC dates, defined enums and strict Unicode must share one validation owner with ordinary detached projection values. Constructor/member assignment now shares one owner with ordinary detached projections.

All 24 new controls pass in the full **2,224-test suite, zero skips**, and both Core targets build. The final producer/Shared/unit copies match. Controls include large exact LongCount proof, nullable/all-null aggregates, source-window and numeric input-work bounds, immutable row snapshots and whole-buffer rejection before any DTO constructor. Grouped executor/physical-attempt integration and native adapters remain subsequent requirements; no provider advertises execution.


The shared detached scalar validator also closes two projection gaps: non-UTC DateTime values and undefined enum values now fail before construction. Exact CLR types, finite numbers, strict Unicode and primitive-only cells share this owner; no arbitrary serializer/getter/ToString is invoked. Payload checks stop after each key/result cell. Numeric grouped input work counts nullable nonparticipants too; a LongCount-only group can retain its exact Int64 count without a binary64 conversion. Grouped source cardinality uses mandatory input proofs rather than just the number of groups.

Evidence: TestResults/CoreCurrent/cql-grouped-results-{current,final-current,budget-current}.trx; TestResults/CosmicCqlDemo/grouped-results{,-fixed,-final,-budget}.log and grouped-results-artifacts.json. The initial build hit an Enum name collision; System.Enum was qualified before the passing build. Private Core92a5d5e419b9e673287d930e345cad6c77457b65 is paired with the parent gitlink/manifest. This verifies detached result handling, not native GroupBy execution.


## Grouped execution owner — verified

The grouped adapter interface is additive to the existing query provider contract. Generic execution snapshots adapters and preflights every actual source/group profile before the first native call. It reuses CosmicQueryAttemptOwner and prepared storage admission/receipts; no new accounting path or entity scan is permitted. Generic HolonManager execution is V2-only; legacy behavior remains documented, not repaired.

Group count and detached payload budgets apply across all selected provider buffers. Native methods receive remaining rejection limits, including zero; they must not truncate input or skip mandatory scope checks. A later empty provider still executes when the publication budget is exhausted. Results retain per-provider provenance without implicit identity merging. Every buffer validates before any provider's DTO constructor executes. Cancellation, an unacknowledged response, invalid later buffer or provider failure must produce no partial grouped publication.

All 15 new executor/hosted controls pass in the full **2,239-test suite, zero skips**. They prove durable attempt-before-native/receipt-after-native ordering on all three priority identities with controlled adapters, inherited-provider denial, V2 manager gating, all-target preflight, cross-provider no-partial DTO construction, cancellation, identity rejection and global group/payload budgets. An empty later provider still executes with both remaining limits zero. Both Core targets build. This is controlled execution evidence, not native store certification.


The host executes an admitted plan with `HolonManager.ExecuteGroupedQueryAsync(plan, adapters, cancellationToken)`. Native enrollment requires ICosmicQueryGroupedStorageProvider plus actual GroupedCapabilities. Remaining output bounds are rejection limits: zero is valid for an empty result but cannot authorize truncation or skipping protected scope checks. Per-provider results retain provenance and independent row sets; there is no implicit replica merge.

Evidence: TestResults/CoreCurrent/cql-grouped-executor-{current,final-current}.trx; TestResults/CosmicCqlDemo/grouped-executor{,-final}.log and grouped-executor-artifacts.json. Private Core237f84b3c729cabe949ddbff0ff9fc92bff6972e is paired with the parent gitlink/manifest. At that historical checkpoint no native provider advertised grouping. Native grouped Count/LongCount/Min/Max/Sum/Average, bounded HTTP, developer demos and later post-group composition remain required.


## Native grouped numeric prerequisite — 23 October 2026

Adapters can now call `CosmicNumericScalarResults.FromParts(operation, inputType, high, low, participatingCount)` for a constant-size grouped Sum/Average packet. The generic scalar overload delegates to the same normalization body. Permission and source/scope admission remain the adapter/plan owner's responsibility; this arithmetic helper grants no query capability or I/O. Final overflow, exact Decimal division, nullable empty semantics and work bounds are unchanged.

Both Core targets build; full **2,252 tests pass, zero skips**, including all13 reference vectors through the new adapter-facing entry. The first vector run lacked the JSON fixture in Core test output; the project now declares CopyToOutputDirectory, and the final run discovers/passes every vector. Evidence: TestResults/CoreCurrent/cql-group-numeric-normalizer-{current,final-current}.trx and TestResults/CosmicCqlDemo/group-numeric-normalizer{,-final}.log, group-numeric-normalizer-artifacts.json. Private Core36db020cb80e6737a3ebf7d177e95aa6026df001 is paired with the parent pin.

SQLite native GroupBy baseline is captured: three new integration controls reject with the expected unsupported grouped-adapter contract before native query dispatch. They specify multiple aggregates/no hydration, source-window ordering and exact cancelling Int64 totals. The fixed fixture was removed after the run. That red baseline was subsequently replaced by the verified SQLite implementation below.


## SQLite native GroupBy checkpoint - 23 October 2026

SQLite now advertises the independent grouped adapter contract for Count/LongCount, Min/Max and exact Int32/Int64 Sum/Average. Reduction runs in SQL over the admitted source predicate and ordered source window; it does not hydrate holons or evaluate groups in memory. Every key and selected aggregate domain is checked, including unselected identity keys. Composite keys, NULL-first ordering, case-insensitive logical GUID equality and normalized UTC date equality follow the Core contract.

The current native profile permits up to 1,000 output groups, 32 selected columns and 32 MiB of buffered proof/result data. Input participation has an explicit Int32.MaxValue work bound, independently of the output-group limit: the native sentinel detects excess and returns an error. Excess groups, payload, invalid domains and true final numeric overflow return no partial results. Exact grouped numeric normalization uses the shared Core signed-limb owner; nullable all-empty Sum is zero and Average is null.

Read execution reuses SQLite's scope predicate, deadline/traversal guards and concurrency detector. Relationship scope checks run before source windows. An owned read snapshot is rolled back and its connection closed; a caller-owned serializable transaction/connection remains owned by the caller. Cancellation and subsequent reuse are covered. Inactive providers return CQL_PROVIDER_NOT_READY explicitly.

Verification: **1,364 native SQLite tests passed, zero skipped**, including 19 new grouping controls. Evidence: TestResults/SQLiteCurrent/cql-grouped-full-current.trx; TestResults/CosmicCqlDemo/sqlite-group-full.log and sqlite-group-artifacts.json. Core SHA256 starts 6D193362, Shared 996EE8E6, SQLite 98D67EC2; producer and test-output copies match. The fixed fixture database, WAL and SHM are absent after teardown. This certifies this SQLite slice only: MongoDB/Neo4j native grouping, grouped HTTP/demos, post-group filters/order/pages and remaining roadmap gates are still open.
