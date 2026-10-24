# CQL post-group filtering contract

24 October2026. Shared capture, capability admission and proof validation are verified. SQLite and MongoDB native execution are enrolled for the domains recorded below; Neo4j and HTTP remain pending.

## Captured intent

`GroupBy(...).Select(...).Where(row => ...)` creates immutable ResultPredicates over selected output columns. It never modifies Source.Predicates or source windows. Scalar results, DTO properties and explicitly cast literal CosmicProjectionRecord aliases use one selected-column resolver shared with grouped ordering. A one-column DTO remains distinct from its scalar cell.

Source and grouped predicates use the same CosmicQueryExpression grammar, node/depth/string/membership limits and closed-value snapshots. Logical operators, selected comparisons, selected-field comparisons, membership, nullable values and ordinal string operations are captured without invoking getters, arbitrary methods or compiled application delegates. At most32 Where clauses are accepted. Ordering/paging preserve captured predicates.

## Operator order

Filtering or reordering after grouped Skip/Take returns CQL_COMPOSITION_UNSUPPORTED, matching source-query behavior. Moving these operators before a window would silently change semantics; native subquery stages are required before those compositions can be admitted. Earlier grouped ordering incorrectly allowed this; the shared stage guard now rejects it, including Take0. Compose Where/OrderBy before Skip/Take.

## Remaining implementation

Profiles without FilteringCapabilities reject result predicates with CQL_CAPABILITY_UNSUPPORTED. SQLite and MongoDB declare explicit native permissions; Neo4j remains unenrolled. New profiles must explicitly declare key domains, aggregate operation/result domains, exact comparisons, string operations, membership and mixed-numeric permissions. Planning and actual-adapter readmission both check them; source/ordering support cannot grant filtering. SQLite enrollment and current limitations are recorded below. Manual Materialize performs pure all-buffer filter/page proof validation, rejecting incorrect native markers; it does not dispatch providers or grant capabilities.

SQLite/MongoDB complete-proof native filtering is verified below. Next implement Neo4j with the same independent capabilities. Nonmatching groups must still undergo domain/arithmetic/work/payload validation. Result paging must count matching groups while Core independently checks native filter/page agreement. Excluded groups cannot hide final overflow, malformed data or protected traversal failures. HTTP selected-result filters and three-provider SDK/HTTP demos follow native verification. Exact Average comparison needs normalized Decimal semantics and must not substitute unrounded ratios or binary64.

## Evidence

Both Core net10.0/netstandard2.1 targets build. Full Core/linked-production suite: **2,343 passed, zero skips**, including nine new filter cases. Initial2341green plus a focused genuine1case red exposed scalar/DTO rebinding; the corrected resolver passes the final suite. Tests cover immutable source/literal preservation, typed/scalar/record binding, field comparisons/membership, rejected getters/methods, stage order, bounded clauses, default-denied provider admission and manual materialization.

Evidence: TestResults/CosmicCurrent/group-filter-{capture,shape-red,final}-current.trx and TestResults/CosmicCqlDemo/group-filter-{build,unit,shape-red,build-final,unit-final}.log. PrivateCore043dd8aa2ab69456d2c97a091c4ab020eee3a7c2; net10SHA256 B65182B2F5393A3AB691208178C0BD5F54B0B4B08FAB420CF09F00B7B44CA633; netstandardSHA256 4BD27FA2FA9EA6E22D281849B6DCDF1F324FF1C109CB22DEE557CB8E0FC7FD07. Native provider/demo evidence predates this new Core cohort until consumer refresh.


## Capability and proof checkpoint — 24 October2026

CosmicGroupedFilteringCapabilities snapshots bounded unique key-type and aggregate-operation/result-type declarations. Each reuses the immutable scalar operator descriptor; logical features, null/Boolean semantics, string operations, membership and mixed numeric literals need explicit support. Generic predicate admission shares the existing source capability walker. Old constructor signatures remain available and default to no filtering.

CosmicGroupedResults validates every native row's domains, counts, source work, uniqueness, order and payload, including nonmatching groups. It independently evaluates captured predicates over validated scalar cells, counts only matches for result paging and checks IsResult against the expected filtered rank. Only validated selected rows construct DTOs. This verifier never invokes application getters/delegates and never substitutes for native filtering. Numeric verification compares exact rational representations of integers, Decimal coefficients/scales and finite binary floating-point values; it does not round Int64 through Double or underflow subnormal literals to Decimal zero. Nullable equality/inequality and ordinal string matching retain the shared grammar semantics.

Both Core targets build; **2,364 full Core/linked-production tests pass, zero skips**, including21 new capability/proof cases. Coverage includes independent operation/type/operator permissions, membership/mixed-literal features, actual readmission, immutable capability snapshots, filter-before-page matching, nonmatching invalid/count/duplicate/payload rejection, nullable NOT, exact Int64/Decimal boundaries, binary64 subnormals, strings/field comparisons, normalized Decimal fractions/sign, GUIDs, enums and UTC ticks. The first netstandard build caught an Enum namespace collision; System.Enum qualification fixed it before final build/tests. No native provider filtering claim is made.

Evidence: TestResults/CosmicCurrent/group-filter-proof-{current,final-current}.trx (files are group-filter-proof-current.trx and group-filter-proof-final-current.trx), TestResults/CosmicCqlDemo/group-filter-proof-{build,build-final,unit,unit-final}.log. PrivateCorebb3efc493a5aacb3b84a6ee8b156ab8f453508c2; net10SHA256318DD259BD399963568FCE25A9CC0633D96F51EA19BA11E2C218417BA4802D73; netstandardSHA256F3D7C08BE674F7004D8D58491E6D156AC135FF2C5834C116468DD1A7C769EE89.


## SQLite native grouped filters — 24 October2026

SQLite now enrolls grouped Where for exact selected keys, Count/LongCount, Min/Max and Int32/Int64 Sum (including nullable domains). Supported clauses include comparisons, selected-field comparisons, AND/OR/NOT, null/Boolean tests, bounded membership and ordinal Contains/StartsWith/EndsWith. Integral mixed Decimal/finite floating-point literals use exact floor/ceiling boundaries and parameters; Decimal/double values never become approximate SQLite numeric parameters. Exact normalized Average filtering remains unsupported and fails at planning.

Native predicates compute filter membership after reduction, then a cumulative matching count applies result Skip/Take. The complete ordered group buffer remains proof, so nonmatches consume group/payload budgets and cannot hide malformed values or final overflow. Core independently checks marker agreement. Sum filtering, sum ordering and integral field comparisons share normalized floor-limb arithmetic, preserving adjacent Int64 totals and negative borrowing without recombining an overflowing total. Source Int32 mixed comparisons share the same boundary owner; their prior behavior remains covered by the full suite.

Verification: **1,401 full SQLite integration tests passed, zero skips**, including17new controls; the first15focused cases also pass. New controls cover multi-aggregate selection without holon hydration, filter-before-page matching ranks, Sum/Count field comparisons, nullable negation and fractional inequality, adjacent values beyond2^53, both Int64 limits, negative borrowing, subnormal/fractional numeric boundaries, numeric/string membership, literal wildcard/quote text, empty suffix versus null, Boolean/GUID/enum/UTC-tick keys, excluded malformed/overflow groups, full group budgets and explicit Average denial.

Evidence: TestResults/SQLiteCurrent/group-filter-{selected,full}-current.trx and group-filter-artifacts.json; TestResults/CosmicCqlDemo/sqlite-group-filter-{build,selected,full}.log. Actual93674terminal0 focused15 and59762terminal0 full1401. Executed producer/consumer pairs match Core318DD259BD399963568FCE25A9CC0633D96F51EA19BA11E2C218417BA4802D73 and SQLite2A41618E5AB15C5AAC11D533336082959E72BF5798359D107A7223332FCE6B26 (exact paths/measured bytes in artifact report). Fixed fixture DB/WAL/SHM verified absent after teardown. No service/download/install/restore/environment was added.

MongoDB/Neo4j native filters, HTTP filters/demos and exact Average semantics remain open. This certifies SQLite's enrolled grouped filters, not full CQL/ORM completion.


## MongoDB native grouped filters — 24 October2026

MongoDB now enrolls grouped Where over selected keys, Count/LongCount, Min/Max and exact integer Sum, including nullable domains. Clauses support comparisons, selected-field comparisons, AND/OR/NOT, null/Boolean tests, bounded membership and ordinal Contains/StartsWith/EndsWith. Integer sums use the same exact Decimal128 reduction expression as grouped ordering; bounded totals fit its precision. Decimal literals retain exact Decimal128 values. Native matching ranks count only accepted groups for paging, while complete ordered proof remains subject to domain/work/group/payload checks. No $match discards reduced proof. Average filtering remains explicitly unsupported pending normalized Decimal semantics.

The first full native run found one GUID mismatch: normalized grouping keys use32hex characters while ordinary GUID literals contain hyphens. Grouped filter literals now use the existing ComparableGuid owner, covering equality and membership consistently. No parallel encoding rule or fallback was added.

Verification: **385 Mongo unit tests and183 full native CQL tests passed, zero skips**, including15new native cases and3new unit controls. Coverage includes complete proof, aggregate combinations, matching-rank pages, nullable negation/fractional inequality, exact adjacent Int64 values and both limits, negative borrowing, binary64 subnormals, numeric/string membership, literal wildcard/quote text, null/empty suffix semantics, Boolean/GUID/enum/UTC-tick key equality+membership, excluded malformed/overflow groups, full group limits and explicit Average denial.

Evidence: TestResults/MongoCurrent/group-filter-unit-final-current.trx and group-filter-artifacts.json; TestResults/SubscriptionUsageCurrent/mongo-cql-native-current.trx; TestResults/CosmicCqlDemo/mongo-group-filter-{unit-final,native-final}.log. Initial82274terminal1:182pass/1Guidred; failure evidence retained in MongoCurrent/group-filter-native-red-current.trx and mongo-group-filter-native.log. Final20476terminal0 after normalization fix and expanded key-membership assertions. Producer/native copies match Core318DD259BD399963568FCE25A9CC0633D96F51EA19BA11E2C218417BA4802D73 and MongoDD4FB0C9AA25AF7481659D33D8A1F295750D52BD31E7147291686555236AD5E0; exact paths/measured bytes in artifact report. Fixtures acknowledged cleanup; runner-owned Mongo stopped and27029 listener verified absent. Runtime/data reused, no new environment/download/restore.

SQLite and MongoDB native grouped filters are verified. Neo4j filters, HTTP filters/demos and exact Average semantics remain open; full CQL/ORM completion is not claimed.
