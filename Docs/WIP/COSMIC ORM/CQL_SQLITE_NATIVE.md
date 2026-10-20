# CQL SQLite native execution — work in progress

This is a native leaf-row adapter milestone, not complete CQL or WEB4 authorization. Current metadata-extended acceptance is focused17/17 and full SQLite832/832, zero skips; prior cohorts below retain their own epochs.

`SQLLiteDBOASIS.ExecuteCosmicQueryAsync(plan, maximumResults, cancellationToken)` requires a single-target SQLite plan. It translates the complete immutable predicate before database access, uses fixed column identifiers and separately bound SQLite parameters, and materializes through the existing repository converter. It never fetches all rows for client predicate evaluation.

## Current native admission

| Fields | Operators and semantics |
|---|---|
| Name, Description | Equality, inequality and null tests; binary case-sensitive comparison |
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
