# CQL SQLite native execution — work in progress

Current documentation: [manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md), [three-provider demos](CQL_DEMO_RUNBOOK.md), [WEB4 v1 contract](CQL_V1_HTTP_CONTRACT.md). Historical checkpoints retain their original scope/counts.

This is a native leaf-row adapter milestone, not complete CQL or WEB4 authorization. Current metadata-extended acceptance is focused17/17 and full SQLite832/832, zero skips; prior cohorts below retain their own epochs.

`SQLLiteDBOASIS.ExecuteCosmicQueryAsync(plan, maximumResults, cancellationToken)` requires a single-target SQLite plan. It translates the complete immutable predicate before database access, uses fixed column identifiers and separately bound SQLite parameters, and materializes through the existing repository converter. It never fetches all rows for client predicate evaluation.

## Current native admission

| Fields | Operators and semantics |
|---|---|
| Name, Description | Equality/inequality/null tests, Contains and explicit-ordinal StartsWith/EndsWith; literal case-sensitive matching |
| Version, HolonType | Equality/inequality and ordered comparisons over native integer columns |
| IsActive, IsPublic | Equality/inequality |
| Id, ParentHolonId, VersionId, PreviousVersionId, CreatedByAvatarId, ModifiedByAvatarId | GUID equality/inequality, case-insensitive canonical text comparison; null legacy audit strings map to Guid.Empty consistently with materialization |
| Metadata Exists / IsNull | Literal object-property presence and explicit JSON null, with missing kept distinct |
| Boolean constants and AND/OR/NOT | SQL null results normalize before boolean composition, preserving admitted CLR null semantics |

Other fields, typed scalar metadata comparisons, dates and unsupported field/operator combinations reject before the connection opens. Planner capability profiles are admission prerequisites; the translator still validates its exact native grammar, even if a caller supplies an overbroad profile.

The result budget is 1–10,000 rows (default 100). The query requests one extra row and returns `CQL_QUERY_LIMIT` with no result when the budget is exceeded. Ordering by native ID is deterministic for that read; there is no pagination or snapshot-consistency claim. Cancellation returns `CQL_QUERY_CANCELLED`; native/materialization errors return `CQL_QUERY_FAILED`. No writes are issued.

## Remaining integration

The method is a direct provider adapter. It is not an HTTP endpoint or a complete HolonManager query pipeline. An authenticated WEB4 boundary must derive eligible targets and preserve actor/tenant/visibility/residency/encryption/cost restrictions before execution. Policy-controlled tombstone visibility and bounded timeout/scan budgets, typed runtime hydration/tracking, graph selection, typed scalar metadata translation, paging, projection/aggregates, other adapters and federation remain required. Do not expose arbitrary caller-created plans as authorized requests.

## Native evidence

Eight cases compare supported results against separately materialized CLR predicates and cover case sensitivity, null composition, injection-like literals, integer/enum/GUID/boolean predicates, unsupported-before-I/O rejection, cancellation, exclusive target selection and result overflow. The focused gate passes8/8 in3s (sqlite-cql-native-focused.trx), followed by full823/823 in127s (sqlite-cql-native-full.trx). Producer and consumer builds succeed with zero errors. Reports and actual assembly hashes are retained under TestResults/SQLiteCurrent; exact hashes are recorded in the integration boundary. The reused fixture database is absent after successful teardown.

## Exact field capability profile — verified

`SQLLiteDBOASIS.CqlCapabilities` publishes the immutable native profile. Canonical fields now declare exact CLR types and operators; support for one string field does not imply support for every string field. The native translator and published profile consume one `SqliteCqlContract` mapping, so the field/operator whitelist has one owner. Native translation still refuses unsupported paths when presented with an overbroad caller-supplied plan.

Two new Core cases pass in full1,310/1,310 (15s); the added native production-profile case passes in the full824/824 SQLite cohort (122s), zero skips. Both Core TFMs and SQLite producer/consumers build with zero errors. Reports: cql-fields-core-full.trx and sqlite-cql-fields-full.trx. Earlier823 evidence remains a distinct captured epoch.

## Metadata presence and explicit JSON null — verified

New marker predicates are `CosmicQueryMetadata.Exists(h, "nested", "key")` and `CosmicQueryMetadata.IsNull(h, "nested", "key")`. Exists is true for any present object property, including JSON null; IsNull is true only for an explicitly present JSON null. Missing paths are false for both. Intermediate segments traverse JSON objects only, with literal case-sensitive keys; arrays are not implicitly indexed or expanded. NOT/AND/OR compose normally. Typed `Value<T>` scalar translation remains required.

The SQLite implementation uses native `json_each` object traversal with separately bound key parameters. Dots, quotes and SQL-like key text remain literal labels; no user key is inserted into SQL or a JSON-path string. Separate MetadataExists/MetadataNull capability flags do not grant scalar Metadata capability. Translation has a 900-predicate-parameter budget (plus one separately bound result-limit parameter), in addition to query/compiler/result limits; excess rejects with CQL_QUERY_LIMIT before I/O. FullCore1,313/1,313 and SQLite832/832 pass, zero skips.

Initial metadata evidence: full Core1,313/1,313 passes13s. Native focused17 initially14pass/3fail; nested-path generation used a static empty-object JSON literal that EF interpreted as a composite-format placeholder. The SQL builder now uses native json_object() instead. Original sqlite-cql-metadata-focused.trx is retained; repaired/native full acceptance is pending. The passing initial cases establish explicit dictionary null is retained and distinct from missing; no serialization rule was changed.

Repaired focused metadata acceptance: sqlite-cql-metadata-focused-verified.trx passes17/17 in3s, zero skips. This verifies native json_object() resolves the EF formatting failure and confirms nested quote/dot/SQL-like labels remain literal. Full832/832 integration gate passes113s (sqlite-cql-metadata-full-verified.trx), zero skips, on captured copied assemblies. Fixture database is absent after normal teardown.

Shared contract checkpoint: SQLite implements ICosmicQueryStorageProvider via its existing native execution path. A new integration case runs through CosmicQueryExecutor with actual SQLite capabilities and native name-plus-metadata-null filtering. Full833/833 passes2minutes0skips (sqlite-cql-executor-full.trx), fixture absent after teardown. See [execution pipeline](CQL_EXECUTION_PIPELINE.md) for provenance/budget/error contracts and remaining authorization/manager/federation requirements.

## Ordinal string matching — verified

New AST/compiler/native work supports string Contains, plus StartsWith/EndsWith only with explicit StringComparison.Ordinal. Other culture/case modes reject before I/O; no lowercasing or database-collation substitution occurs. Patterns are captured literals, bounded to4,096 characters and valid Unicode scalar sequences. NULL receivers yield false for the match predicate; NOT then follows CQL two-valued boolean semantics. Use an explicit non-null guard when comparing the query with CLR instance-method evaluation.

SQLite uses bound patterns and BLOB instr/substr/length, preserving embedded NUL bytes (TEXT length stops at NUL). Percent/underscore/bracket/quote text remains literal rather than LIKE syntax. Empty patterns match every non-null value. Exact field capabilities and native translation use the same map; scalar metadata string paths and unlisted canonical string fields remain unsupported. Core1,341/1,341 and SQLite874/874 pass, zero skips.

String verification checkpoint: fullCore1,341/1,341 passes14s and bothTFMs build0errors. Initial focusednative26 had25pass/1fail for empty-prefix against an empty stored value; native empty-pattern translation now directly tests columnISNOTNULL for all three operations. The original red report is retained. Repaired focused26/26 passes6s (sqlite-cql-string-focused-verified.trx); full874/874 SQLite regression passes116s, zero skips (sqlite-cql-string-full-verified.trx), with the fixture absent after teardown. Core unit and native copies share Core3CC0F440 epoch.

## Bounded scalar-array membership — verified

`values.Contains(h.Name)` and equivalent scalar-field forms compile from captured one-dimensional zero-based arrays or explicit array initializers, with at most128 entries. The array element type must exactly match the declared path type. Values are captured into immutable literal nodes; arbitrary enumerables and non-null comparers are rejected without invoking user enumeration. The compiler normalizes both Enumerable.Contains and the SDK-generated ReadOnlySpan lowering, including the known null-default-comparer enum overload, without executing either call. Every normalized node consumes the expression depth/work budget.

SQLite uses parameterized IN for non-null values and an explicit IS NULL branch for null entries. Empty arrays are false and NOT complements the entire two-valued membership predicate, avoiding SQL NULL poisoning. Duplicates do not change matching; GUID, enum, integer, boolean and binary string fields use their existing canonical equality semantics. The whole-query900predicate-parameter limit applies before I/O.

The precision audit also found that implicit decimal/integer coercion could change query meaning. Mixed scalar numeric kinds require an explicit adapter capability. At this membership checkpoint SQLite rejected them; the subsequent exact Int32 translation below replaces that restriction for its admitted canonical integer fields. Literal strings and metadata segments reject unpaired Unicode surrogates at their shared admission boundary.

Final Core1,347/1,347 passes12s, bothTFMs build0errors. Focusednative26/26 passes5s on the final parser; full900/900 SQLite regression passes117s, zero skips (sqlite-cql-membership-depth-full.trx), with fixture absent after normal teardown. Prior reports preserve the original SDK-shape failures, enum default-comparer failure, and one red depth-budget regression; no failed report is admitted as acceptance.


## Exact SQLite mixed numeric comparisons — 20 October 2026

SQLite now declares MixedNumericComparison for its admitted canonical Int32 fields (Version and Int32-backed HolonType storage). Decimal/double literals are compared in their original precision against Int32 boundaries, then translated to exact integer bounds using floor/ceiling. Fractional equality is false; fractional inequality matches non-null values. Out-of-range literals reduce to the correct constant/non-null predicate. Only exact integer parameters reach SQLite, preventing REAL affinity from rounding a high-precision decimal into a false match. Unsupported numeric path types still reject explicitly.

Six focused native cases pass, covering 420 comparisons against separately compiled CLR predicates plus the decimal equality regression. Cases include six operators, reversed operands, fractional/negative values, floating-point neighbors, Int32 boundaries and enum storage. Full SQLite regression passes 905/905 with zero skips; the owned fixture is absent after teardown. This does not certify arbitrary numeric metadata, undefined enum literals, other adapters or the unfinished security/graph/federation roadmap.


## Native SQLite scalar metadata checkpoint — 20 October 2026

The [typed scalar metadata contract](CQL_SCALAR_METADATA.md) adds native comparisons, membership and ordinal string matching for string, boolean, Int32 and Int64 metadata, including nullable value types. The production profile and translator share exact type/operator declarations. Missing and wrong-type paths remain distinct from explicit JSON null; integer bounds and SQLite runtime types are checked before comparison. Arrays and JSON-encoded strings are not traversed as objects. Shared native string/membership helpers preserve existing canonical field behavior. Unsupported metadata types and mixed numeric metadata comparisons reject before I/O, including with overbroad caller plans.

The compiler also handles method-free built-in nullable scalar literal lifting, fixing the nullable boolean comparison exposed by the original red fixture. Full Core 1,353/1,353 and focused native 43/43 pass with zero skips. The original 26-case failure report is retained. Native decimal/double/GUID/date/enum metadata, authenticated WEB4 policy/routing, broader adapters, graph queries, paging/aggregates/federation and all unfinished original roadmap stages remain required.

Final native regression: SQLite 948/948 passes125s, zero skips (`sqlite-cql-scalar-metadata-full-verified.trx`), with the fixture absent after normal teardown. Full Core1,353/1,353 and focused native43/43 belong to the recorded matching Core/provider epoch.

## Native Min/Max — 23 October 2026

SQLite now admits typed Min/Max selections through generic V2 scalar planning/execution. Native domain validation covers every participating value before selecting an extremum, including malformed non-winning canonical values. Required/nullable empty results, negative/exact Int64, Unicode/GUID ordering and UTC ticks follow the shared contract. Source visibility/windowing and relationship/snapshot ownership remain protected; graph output caps do not truncate aggregate input. The existing read-budget owner manages raw UTF8 validation and native resource limits; caller connections/transactions remain caller-owned.

Full integration regression passes1,318tests/0skip in2m9s, including33new native controls and all19reference vectors. Producer and consumer SQLite/Core/Shared identities match. [Usage](CQL_MANUAL.md#native-minmax--sqlite-sdk), [semantics/evidence](CQL_AGGREGATE_IMPLEMENTATION_CONTRACT.md#sqlite-native-minmax-slice). MongoDB/Neo4j Min/Max, selected-scalar HTTP/demos and the later aggregate/grouping phases remain required.


## SQLite native Sum/Average verified — 23 October 2026

SQLite now advertises Sum/Average for Core's exact Int32/Int64 and nullable domains. It reuses the projection operand guard and scalar read owner. One native ordered/windowed CTE reduces signed high/low integer limbs, invalid domains and participating count; Core normalizes only that constant-size packet. Every participating value is validated, so a numeric string cannot coerce into a successful total. Sum checks the final mathematical range; Average remains exact through its integer numerator and Decimal division, including cases where an Int64 sum would overflow.

A separate native input-work sentinel allows at most 2^31 selected rows to enter the limb reduction (the permitted bound plus one detection row). Both limb sums still fit Int64 at that sentinel. Exceeding the bound returns CQL_QUERY_LIMIT, never a truncated success. Missing/null optional rows count toward native work even though they do not participate in the arithmetic. The root-row/graph publication caps do not limit numeric input. Existing protected traversal validation, snapshot/deadline/cancellation and caller connection/transaction ownership remain unchanged.

Evidence: genuine capability-red18/18 failures, then18/18 native green, then expanded1,344/1,345. The single failure was a test using137 membership IDs beyond CQL's existing membership bound; the corrected prefix predicate now reaches the intended aggregate. The final fresh **full SQLite suite passes 1,345 tests, zero skips (2m20s)**, including all **27 new numeric cases** (five primary, 13 shared reference vectors, nine boundary controls). Coverage includes intermediate cancellation, final Int32/Int64 overflow, averages beyond2^53/over an overflowing sum, empty/null/wrong domains, windows, visibility before overflow,137 inputs under graph-output cap1, canonical type corruption, actual-scope cancellation/reuse, acknowledged concurrent writer/snapshot, borrowed transaction retention and traversal errors outside paging.

Authoritative reports: TestResults/SQLiteCurrent/cql-numeric-red-current.trx, cql-numeric-green-current.trx, cql-numeric-expanded-current.trx and cql-numeric-full-current.trx. Compact logs and matching producer/consumer hashes are in TestResults/CosmicCqlDemo/sqlite-numeric-*.log and sqlite-numeric-artifacts.json. The fixed DB/wal/shm paths are absent after verified teardown. No runtime, environment or download was added.

MongoDB and Neo4j numeric execution, numeric HTTP/Decimal wire encoding and demonstrations remain required. GroupBy, broader scalar domains and the remaining CQL/ORM roadmap remain open. SQLite's native gate does not imply all-provider numeric support or overall completion.
