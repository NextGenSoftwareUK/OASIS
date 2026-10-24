# CQL post-group filtering contract

24 October2026. Shared capture, capability admission and proof validation are verified; native/HTTP execution is not yet enrolled.

## Captured intent

`GroupBy(...).Select(...).Where(row => ...)` creates immutable ResultPredicates over selected output columns. It never modifies Source.Predicates or source windows. Scalar results, DTO properties and explicitly cast literal CosmicProjectionRecord aliases use one selected-column resolver shared with grouped ordering. A one-column DTO remains distinct from its scalar cell.

Source and grouped predicates use the same CosmicQueryExpression grammar, node/depth/string/membership limits and closed-value snapshots. Logical operators, selected comparisons, selected-field comparisons, membership, nullable values and ordinal string operations are captured without invoking getters, arbitrary methods or compiled application delegates. At most32 Where clauses are accepted. Ordering/paging preserve captured predicates.

## Operator order

Filtering or reordering after grouped Skip/Take returns CQL_COMPOSITION_UNSUPPORTED, matching source-query behavior. Moving these operators before a window would silently change semantics; native subquery stages are required before those compositions can be admitted. Earlier grouped ordering incorrectly allowed this; the shared stage guard now rejects it, including Take0. Compose Where/OrderBy before Skip/Take.

## Remaining implementation

Existing grouped capability profiles keep FilteringCapabilities null and reject result predicates with CQL_CAPABILITY_UNSUPPORTED. New profiles must explicitly declare key domains, aggregate operation/result domains, exact comparisons, string operations, membership and mixed-numeric permissions. Planning and actual-adapter readmission both check them; source/ordering support cannot grant filtering. No provider currently advertises grouped filtering. Manual Materialize performs pure all-buffer filter/page proof validation, rejecting incorrect native markers; it does not dispatch providers or grant capabilities.

Next implement complete-proof native filtering on SQLite/MongoDB/Neo4j using the verified independent capabilities. Nonmatching groups must still undergo domain/arithmetic/work/payload validation. Result paging must count matching groups while Core independently checks native filter/page agreement. Excluded groups cannot hide final overflow, malformed data or protected traversal failures. HTTP selected-result filters and three-provider SDK/HTTP demos follow native verification. Exact Average comparison needs normalized Decimal semantics and must not substitute unrounded ratios or binary64.

## Evidence

Both Core net10.0/netstandard2.1 targets build. Full Core/linked-production suite: **2,343 passed, zero skips**, including nine new filter cases. Initial2341green plus a focused genuine1case red exposed scalar/DTO rebinding; the corrected resolver passes the final suite. Tests cover immutable source/literal preservation, typed/scalar/record binding, field comparisons/membership, rejected getters/methods, stage order, bounded clauses, default-denied provider admission and manual materialization.

Evidence: TestResults/CosmicCurrent/group-filter-{capture,shape-red,final}-current.trx and TestResults/CosmicCqlDemo/group-filter-{build,unit,shape-red,build-final,unit-final}.log. PrivateCore043dd8aa2ab69456d2c97a091c4ab020eee3a7c2; net10SHA256 B65182B2F5393A3AB691208178C0BD5F54B0B4B08FAB420CF09F00B7B44CA633; netstandardSHA256 4BD27FA2FA9EA6E22D281849B6DCDF1F324FF1C109CB22DEE557CB8E0FC7FD07. Native provider/demo evidence predates this new Core cohort until consumer refresh.


## Capability and proof checkpoint — 24 October2026

CosmicGroupedFilteringCapabilities snapshots bounded unique key-type and aggregate-operation/result-type declarations. Each reuses the immutable scalar operator descriptor; logical features, null/Boolean semantics, string operations, membership and mixed numeric literals need explicit support. Generic predicate admission shares the existing source capability walker. Old constructor signatures remain available and default to no filtering.

CosmicGroupedResults validates every native row's domains, counts, source work, uniqueness, order and payload, including nonmatching groups. It independently evaluates captured predicates over validated scalar cells, counts only matches for result paging and checks IsResult against the expected filtered rank. Only validated selected rows construct DTOs. This verifier never invokes application getters/delegates and never substitutes for native filtering. Numeric verification compares exact rational representations of integers, Decimal coefficients/scales and finite binary floating-point values; it does not round Int64 through Double or underflow subnormal literals to Decimal zero. Nullable equality/inequality and ordinal string matching retain the shared grammar semantics.

Both Core targets build; **2,364 full Core/linked-production tests pass, zero skips**, including21 new capability/proof cases. Coverage includes independent operation/type/operator permissions, membership/mixed-literal features, actual readmission, immutable capability snapshots, filter-before-page matching, nonmatching invalid/count/duplicate/payload rejection, nullable NOT, exact Int64/Decimal boundaries, binary64 subnormals, strings/field comparisons, normalized Decimal fractions/sign, GUIDs, enums and UTC ticks. The first netstandard build caught an Enum namespace collision; System.Enum qualification fixed it before final build/tests. No native provider filtering claim is made.

Evidence: TestResults/CosmicCurrent/group-filter-proof-{current,final-current}.trx (files are group-filter-proof-current.trx and group-filter-proof-final-current.trx), TestResults/CosmicCqlDemo/group-filter-proof-{build,build-final,unit,unit-final}.log. PrivateCorebb3efc493a5aacb3b84a6ee8b156ab8f453508c2; net10SHA256318DD259BD399963568FCE25A9CC0633D96F51EA19BA11E2C218417BA4802D73; netstandardSHA256F3D7C08BE674F7004D8D58491E6D156AC135FF2C5834C116468DD1A7C769EE89.
