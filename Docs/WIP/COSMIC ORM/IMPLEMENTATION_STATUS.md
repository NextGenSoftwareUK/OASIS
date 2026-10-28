# Implementation status

[Current status](CURRENT_STATUS.md) is the recovery entry point and milestone record.

- Full COSMIC ORM/CQL programme remains incomplete.
- The 12 investor-demo rows are green on SQLite, MongoDB and Neo4j.
- Q23/Q23e/Q49a and Q01a–e are verified within their checklist scopes.
- Active red row Q01f: shared stage contract is committed; IQueryable bridge is committed in Core b9bccf0.
- SQLite native stages are implemented with 42 passing CQL provider tests, including native V2 typed/graph/visibility and protected traversal acceptance; broader cross-database acceptance remains pending. MongoDB native stages are implemented with 212 passing compiler/admission tests; 18 live native entity-stage cases pass. Neo4j native stages now pass 144 compiler tests and 19 live stage cases; staged terminal/projection integration remains pending.
- Latest native checkpoint: 1,781 checks:18 entity-stage,9 count,16 cardinality,20 forward element,6 native reversal,12 standard reversal/tail and12 explicit default cases/store, plus Neo snapshot rejection.
- Current shared entity-count foundation regression: 2,578 passes; both Core targets build. SQLite native count enrolled (44 CQL provider tests pass); Mongo count passes215 compiler tests and9 live cases; Neo count passes147 compiler tests and9 live cases; standard staged Count/LongCount/Any/All now live-verified on all3. Element/projection/remaining numeric integration pending. Full Core suite still has an unverified WEB4 host prerequisite.
- Green features/federation cursors stay frozen unless a reproducible bug appears.
- HyperDrive V2 only; legacy gaps receive documentation.

[Checklist](CQL_PARITY_CHECKLIST.md) owns per-database acceptance.
Historical checkpoints do not establish blanket ORM completeness.
[Previous detail](Archive/IMPLEMENTATION_STATUS_checkpoint_2026-10-27.md) and
[original history](Archive/IMPLEMENTATION_STATUS_history_2026-10-27.md) retain prior evidence.

Shared forward-element foundation implemented/tested:4 new tests, both Core targets build; cardinality precedes shared typed hydration. Standard forward elements now pass20 native cases/store and3 binding tests. Tail/from-end/explicit defaults and staged projection/numeric integration remain pending.

Native entity Reverse independently enrolled on all3,6 live cases/store;45 SQLite/216 Mongo/148 Neo compiler tests pass. Standard Reverse/Last/from-end binding now passes12 new native cases/store and3 shared tests; explicit defaults and staged projection/numeric integration remain pending.

Explicit default-value overloads verified on all3:3 shared tests and12 live cases/store. Caller identity/tracking preserved only after acknowledged empty selection; errors/ambiguity propagate. Staged projection/numeric integration remains pending.

Shared entity-projection foundation implemented with4 new tests and both Core targets built; source stages remain separate from selection, actual native enrollment is mandatory, all cells/count bounds validate before DTO constructors. Native providers/standard Select/selected-stage/numeric integration pending. Latest1,781 native cohort predates this Core increment.

Priority27October: broader demo before full parity. Cosmos recommended first cloud, Solana first chain, then IPFS, SOLID, ActivityPub; see CQL_DEMO_EXPANSION_ROADMAP.md. SQLite entity projection passes47 CQL tests/10 native cases (combined1,791); unfinished parity depth deferred unless demo needs it.

[Indexed HTTP checkpoint](CQL_INDEXED_HTTP.md): actual controller/context tests and full Core4,128/SQLite31/Solana789 gates pass. Exact measured cost, units, attempts and frozen intent are enforced; stock middleware fingerprint/admission integration, real-backend/native HTTP and full parity remain open.

27October indexed projection SDK: native SQLite selected canonical cells plus
shared proof-before-DTO construction and V2 manager implemented. FullCore4154,
SQLite42,Solana799pass. LINQ/HTTP/live-source projection gates remain open;
[contract](CQL_INDEXED_SQL_ROWS.md#indexed-native-selected-cell-sdk-milestone-27-october).

Standard indexed LINQ Select and flat reselection/list/enumeration now pass7new
SQLite cases and the live Solana shared-demo gate. Core4154/SQLite49/Solana799pass.
Next: projection HTTP; metadata, aggregates, selected stages and M17 constructor
shapes stay open. [Current contract](CQL_INDEXED_SQL_ROWS.md#standard-indexed-linq-select-27-october).

Indexed HTTP projection is implemented via optional frozen columns on the existing
indexed route. FullCore4165 and nine-request live Solana/SQLite HTTP gate pass.
Next: indexed aggregate/metadata capabilities and common-demo gaps; production
authentication/durable billing and full parity remain open.

Daily27October integration: incoming Development RecordNotFound fix and2legacy
regressions preserved without new legacy edits. Merged Core4eb3dab published to
Development/task branch; parent pin/gitlink advance together. FullCore4167,
SQLite49,Solana799pass; WEB4/demo/five controller consumers build.

Indexed Count/LongCount/Any now spans SDK, standard LINQ and the dedicated
`/indexed/scalar` HTTP route. Final Core4243pass; existing SQLite76/Solana799
checks remain verified. The live HTTP gate passes27requests:13accepted,
13replays rejected,1unsupported;9scalar cases cover whole/filtered-empty/zero
windows, six billed physical reads each, no source writes/new seed. Native
selected/row evidence remains intact. Production identity/durable settlement,
metadata/other aggregates/selected stages, scaling and full parity remain open.
Shared continuous ingestion session now serializes compare-and-publish, advances
only positively acknowledged cursors and latches failures. Core13newcases pass;
live host advances3308→3320 with LongCount1, no source writes, clean teardown.
Shared queryable metadata export policy and exact source-root admission now pass17focusedcases; schema3 SQLite rows/cardinality now pass22newcases (98indexed/205fullprovider), shared metadata SDK encrypted-row/missing-context acceptance and7WEB4/tariff admission cases now pass (Core4235/SQLite207); live schema3 SDK/HTTP42requests and interactive/read-only reuse now pass; selected metadata SDK/SQLite now passes8newCore/12newSQLite cases with explicit roots and proof-before-DTO; live selected SDK/HTTP50requests and interactive lifecycle now pass, no source writes; CosmosDB teaching page/hub now passes native and interactive HTTP lifecycle, readiness ordering and exact cleanup; three-database fixture reconciliation and fresh681SDK/138HTTP/30save-check demo now pass with exact cleanup; Q01f Mongo entity projection now passes218CQL/10native cases; Neo projection and standard Select/numeric integration follow, all Q01f cells remain red (CQL_MANUAL.md);
incremental transport and production worker scheduling/accounting remain open.
See CURRENT_STATUS and CQL_INDEXED_HTTP.md for evidence scope.
