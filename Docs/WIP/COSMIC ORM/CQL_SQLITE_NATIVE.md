# CQL SQLite native execution — work in progress

This is a native leaf-row adapter milestone, not complete CQL or WEB4 authorization. The focused eight native cases and full SQLite 823/823 integration gate pass, with zero skips.

`SQLLiteDBOASIS.ExecuteCosmicQueryAsync(plan, maximumResults, cancellationToken)` requires a single-target SQLite plan. It translates the complete immutable predicate before database access, uses fixed column identifiers and separately bound SQLite parameters, and materializes through the existing repository converter. It never fetches all rows for client predicate evaluation.

## Current native admission

| Fields | Operators and semantics |
|---|---|
| Name, Description | Equality, inequality and null tests; binary case-sensitive comparison |
| Version, HolonType | Equality/inequality and ordered comparisons over native integer columns |
| IsActive, IsPublic | Equality/inequality |
| Id, ParentHolonId, VersionId, PreviousVersionId, CreatedByAvatarId, ModifiedByAvatarId | GUID equality/inequality, case-insensitive canonical text comparison; null legacy audit strings map to Guid.Empty consistently with materialization |
| Boolean constants and AND/OR/NOT | SQL null results normalize before boolean composition, preserving admitted CLR null semantics |

Other fields, metadata paths, dates and unsupported field/operator combinations reject before the connection opens. Planner capability profiles are admission prerequisites; the translator still validates its exact native grammar, even if a caller supplies an overbroad profile.

The result budget is 1–10,000 rows (default 100). The query requests one extra row and returns `CQL_QUERY_LIMIT` with no result when the budget is exceeded. Ordering by native ID is deterministic for that read; there is no pagination or snapshot-consistency claim. Cancellation returns `CQL_QUERY_CANCELLED`; native/materialization errors return `CQL_QUERY_FAILED`. No writes are issued.

## Remaining integration

The method is a direct provider adapter. It is not an HTTP endpoint or a complete HolonManager query pipeline. An authenticated WEB4 boundary must derive eligible targets and preserve actor/tenant/visibility/residency/encryption/cost restrictions before execution. Policy-controlled tombstone visibility and bounded timeout/scan budgets, typed runtime hydration/tracking, graph selection, metadata translation, paging, projection/aggregates, other adapters and federation remain required. Do not expose arbitrary caller-created plans as authorized requests.

## Native evidence

Eight cases compare supported results against separately materialized CLR predicates and cover case sensitivity, null composition, injection-like literals, integer/enum/GUID/boolean predicates, unsupported-before-I/O rejection, cancellation, exclusive target selection and result overflow. The focused gate passes8/8 in3s (sqlite-cql-native-focused.trx), followed by full823/823 in127s (sqlite-cql-native-full.trx). Producer and consumer builds succeed with zero errors. Reports and actual assembly hashes are retained under TestResults/SQLiteCurrent; exact hashes are recorded in the integration boundary. The reused fixture database is absent after successful teardown.
