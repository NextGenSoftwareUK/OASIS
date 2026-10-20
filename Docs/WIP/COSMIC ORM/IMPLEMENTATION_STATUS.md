# COSMIC ORM implementation status

Checkpoint: 20 October 2026. The goal remains active on `fix/hyperdrive-v2-providers`. Selected foundations are implemented and verified; the full roadmap is unfinished. Source commits, assembly hashes, original failures and fixture reconciliation are retained in [integration evidence](INTEGRATION_BOUNDARY.md) and [verification history](VERIFICATION.md).

## Verified scope

| Area | Evidence | Acceptance still open |
|---|---|---|
| Tracking and save ownership | Detached preparation, private baselines, no-op detection, failed-write isolation, submitted membership and acknowledgement checks; Core 1,320 tests pass, including predicate compilation, composition and pure capability planning | Complete encryption/key-context and arbitrary callback failure acceptance |
| Cross-client concurrency | Native revision checks and idempotent operation receipts under MongoDB, SQLite and Neo4j transaction owners | Wider provider capability admission and complete public route/fault coverage |
| Typed child roles | Captured manifests, root-only authoritative-null preservation, hydration before tracking, explicit partial selections | Remaining parameter, visibility, history and provider conformance |
| V2 graph loading | ID/key, collection, metadata and parent routes; source-bound member reads, heap traversal, depth limits, unlink protection and overlapping roots | Coherent historical snapshots and wider failure/security policies |
| Search selection | Child source and type reach V2 execution; navigation objects are excluded from root search values | Cache implementation and explicit relationship predicates/CQL |
| Selected native regression | MongoDB 77/77, SQLite 815/815, Neo4j 202/202; separate Oracle 96/96 query/role cohort | Counts belong to recorded assembly epochs and do not certify every provider |
| Explicit replication | Selected native primary/outbox transactions, fenced delivery, committed-replica receivers and four MongoDB/SQLite delivery combinations | Default enrollment, crash journals, authorization/encryption context, retention, limits and operator migration |

The search-navigation fix has Shared 828/828, SQLite 815/815 and Neo4j 202/202 evidence. Core 1,289/1,289 and MongoDB 77/77 have their own recorded search-selection cohorts. These are separate runs, not one all-provider release test. Subsequent Shared projection changes are tracked by the provider session under `Docs/WIP/HyperDrive-Providers` and need independent acceptance.

Recent local commits include Core `768d2e6` (manager graph ownership) and `ccb7b64` (search selection), paired parent commits `b5b984185` and `a19405b71`, root-search fix `5129b53d2`, and Neo4j certification `69852c20c`. The strict dependency manifest/gitlink validator passed at the paired commits. Local commits do not establish remote publication or deployment readiness.

## Full roadmap remaining

| Stage | Current boundary and remaining work |
|---|---|
| 1. Entity lifecycle | Foundation implemented; finish encryption, runtime binding and failure-publication cases |
| 2. Atomic concurrency | Selected native owners verified; finish provider admission and full route/fault matrix |
| 3. Children, queries and history | Selected graph/search fixes verified; finish parameter/visibility/cache matrices, provider conformance and historical graph policy |
| 4. Durable delivery | Explicit owners/receivers/workers implemented; finish automatic enrollment, security context, process-crash recovery, limits, retention and operator migration |
| 5. Controlled migration | Full checkpoint/catch-up/verification/cutover workflow is not implemented; import/export alone is insufficient |
| 6. Gas-aware placement | Authorized durable settlement, quote freshness, budgets, deadlines, finality and reconciliation remain open |
| 7. Per-save deltas | Local comparison/preview prerequisites exist; atomically persisted, authorized committed deltas and replay semantics are incomplete |
| 8. CQL | [Predicate compiler and typed composition/scope](CQL_EXPRESSION_CONTRACT.md) implemented and unit-tested; pure scope/capability planning is verified, first SQLite leaf-row native adapter and exact field capabilities verified (833/833 integration cohort, including metadata presence/null and shared execution); security/manager integration, broader native adapters and federation remain open |

The [full roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md) retains the original requirements and acceptance criteria. No overall percentage is asserted without a verified weighted checklist. Failover selection does not prove 100% uptime, completed migration or finalized settlement.

## Coordination and publication

Implementation targets Hyperdrive V2; [legacy gaps](LEGACY_HYPERDRIVE_GAPS.md) are documentation only. Reuse the task checkout and retained environments. Preserve other sessions' changes and original uncertain-write evidence.

Keep coherent verified batches on the task branch. Merge and push to `Development` only once or twice daily, as requested to limit Railway builds. Core task-branch publication and substantive STAR/WEB6 remote changes still require coordinated release review. Never advance foreign dependency pins merely to clear a branch gate.

## Evidence history

Detailed dated checkpoints remain in the linked verification, integration and topic documents. Earlier counts, branch names and descriptions of unfinished work describe their original cohorts; use this page and the latest integration entry to interpret current acceptance. This status page is a current synthesis, not a replacement for those original records.

CQL composition checkpoint: immutable `CosmicQuery<T>` with captured AND predicates and explicit current-default/named/all-eligible scope is implemented. Named-scope validation and query-wide composition limits pass five new tests; full Core 1,303/1,303 and both TFMs build. Execution planning, authorization and native execution remain open. See [CQL contract](CQL_EXPRESSION_CONTRACT.md).

SQLite CQL checkpoint: parameterized native predicates with CLR-compatible admitted null/boolean semantics, fixed column identifiers, cancellation and bounded result materialization pass focused8/8 and full823/823. NativeCore4B2E/SharedCA844 epoch is separate from earlier gates. See [native scope](CQL_SQLITE_NATIVE.md); full CQL and earlier roadmap requirements remain open.

Exact field capability checkpoint: Core1,310/1,310 and SQLite824/824 pass. `CqlCapabilities` and native translation share a single field/type/operator map; unsupported canonical fields reject during planning. Current copied Core38E010/SharedCA844/SQLite2FF450 epoch is retained separately from prior gates.

Metadata CQL checkpoint: native Exists/IsNull supports literal nested object keys and distinguishes missing from explicit JSON null. Core1,313/1,313 and SQLite832/832 pass. Native metadata scalar comparisons remain open; presence/null predicates do not imply scalar conversion or full metadata querying.

Shared CQL execution checkpoint: native provider interface and preflight/execution coordinator pass seven new Core cases; SQLite implements and exercises the contract natively. FullCore1,320/1,320 and SQLite833/833 pass. Results retain per-provider groups rather than claiming global deduplication, ordering or snapshot consistency. AuthenticatedWEB4 routing/typedmanager/federation remain open. See [execution pipeline](CQL_EXECUTION_PIPELINE.md).
