# CQL post-group filtering contract

24 October2026. Shared capture is verified; native/HTTP execution is not yet enrolled.

## Captured intent

`GroupBy(...).Select(...).Where(row => ...)` creates immutable ResultPredicates over selected output columns. It never modifies Source.Predicates or source windows. Scalar results, DTO properties and explicitly cast literal CosmicProjectionRecord aliases use one selected-column resolver shared with grouped ordering. A one-column DTO remains distinct from its scalar cell.

Source and grouped predicates use the same CosmicQueryExpression grammar, node/depth/string/membership limits and closed-value snapshots. Logical operators, selected comparisons, selected-field comparisons, membership, nullable values and ordinal string operations are captured without invoking getters, arbitrary methods or compiled application delegates. At most32 Where clauses are accepted. Ordering/paging preserve captured predicates.

## Operator order

Filtering or reordering after grouped Skip/Take returns CQL_COMPOSITION_UNSUPPORTED, matching source-query behavior. Moving these operators before a window would silently change semantics; native subquery stages are required before those compositions can be admitted. Earlier grouped ordering incorrectly allowed this; the shared stage guard now rejects it, including Take0. Compose Where/OrderBy before Skip/Take.

## Remaining implementation

Existing grouped capability profiles reject any result predicate with CQL_CAPABILITY_UNSUPPORTED. Manual Materialize also rejects it, preventing a caller from publishing an unfiltered buffer. These checks remain until operation/type-specific filtering capabilities, native filter membership and shared proof validation exist. No provider currently advertises grouped filtering.

Next implement independent key/aggregate-domain predicate capabilities, then complete-proof native filtering on SQLite/MongoDB/Neo4j. Nonmatching groups must still undergo domain/arithmetic/work/payload validation. Result paging must count matching groups while Core independently checks native filter/page agreement. Excluded groups cannot hide final overflow, malformed data or protected traversal failures. HTTP selected-result filters and three-provider SDK/HTTP demos follow native verification. Exact Average comparison needs normalized Decimal semantics and must not substitute unrounded ratios or binary64.

## Evidence

Both Core net10.0/netstandard2.1 targets build. Full Core/linked-production suite: **2,343 passed, zero skips**, including nine new filter cases. Initial2341green plus a focused genuine1case red exposed scalar/DTO rebinding; the corrected resolver passes the final suite. Tests cover immutable source/literal preservation, typed/scalar/record binding, field comparisons/membership, rejected getters/methods, stage order, bounded clauses, default-denied provider admission and manual materialization.

Evidence: TestResults/CosmicCurrent/group-filter-{capture,shape-red,final}-current.trx and TestResults/CosmicCqlDemo/group-filter-{build,unit,shape-red,build-final,unit-final}.log. PrivateCore043dd8aa2ab69456d2c97a091c4ab020eee3a7c2; net10SHA256 B65182B2F5393A3AB691208178C0BD5F54B0B4B08FAB420CF09F00B7B44CA633; netstandardSHA256 4BD27FA2FA9EA6E22D281849B6DCDF1F324FF1C109CB22DEE557CB8E0FC7FD07. Native provider/demo evidence predates this new Core cohort until consumer refresh.
