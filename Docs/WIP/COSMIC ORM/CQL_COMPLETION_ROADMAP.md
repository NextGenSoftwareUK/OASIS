# CQL completion roadmap

User priority27October: [eight-provider demo expansion](CQL_DEMO_EXPANSION_ROADMAP.md) takes precedence over unfinished parity-only depth. Original scope remains active.

[Current status](CURRENT_STATUS.md) owns the exact next action; [checklist](CQL_PARITY_CHECKLIST.md) owns acceptance.

1. Q01f shared IQueryable bridge is committed and tested; keep native database cells red.
2. All3 native entity stages pass 18 cases/store plus Neo snapshot rejection (1,556-check gate). Preserve source windows/protected errors and keep cells red pending terminal/projection integration.
3. Native count and standard Count/LongCount/Any/All now pass25 cases/store (1,631 native checks). Forward elements now pass20 cases/store (1,691 native checks); standard Reverse/tail/from-end now pass12 additional cases/store (1,745 native checks); explicit default overloads now pass12 cases/store (1,781 native checks), then remaining selected-stage/numeric integration after native entity-stage projection and final standard Select and proving native tests before promoting Q01f cells.
4. Continue remaining red rows in checklist priority order. Leave green rows/cursors frozen unless a bug is reproduced.
5. Finish deferred large parity items, including POCO mapping and migrations tooling, at their assigned priority.
6. Audit every declared requirement before claiming full EF/NHibernate or COSMIC completion.

The 12 investor-demo rows, Q23/Q23e/Q49a and Q01a–e are scoped completed milestones.
Unsupported shapes fail explicitly; client materialization or silent fallback cannot replace native implementation.
[Earlier checkpoints](Archive/CQL_COMPLETION_ROADMAP_checkpoint_2026-10-27.md) and
[original roadmap](Archive/CQL_COMPLETION_ROADMAP_history_2026-10-27.md) preserve history.

Indexed SDK rows, typed manager/LINQ, frozen WEB4 preparation and stock usage/storage
middleware integration are verified with controlled identity/ledger/RPC boundaries.
Actual SQLite and canonical Solana decoding run through shipping HTTP. Older
minimum/checkpoint/previous block identity validation is corrected after an HTTP
reproduction; [contract](CQL_CHAIN_QUERY_CONTRACT.md#finalized-anchor-identity-correction-27-october).
Live declared-range chain SDK acceptance and shared C# walkthrough pass. Next:
production admission/durable settlement (live-source HTTP five-request gate now passes),
remaining indexed scalar/projection/metadata policies and the eight-provider demo.
Original source recovery and full parity remain red. See CURRENT_STATUS for gates.

Indexed canonical projection SDK now passes8newCore/11newSQLite checks, through
the existing shared planner/row-proof owner and V2 HolonManager. LINQ Select, HTTP,
live-chain projection acceptance, aggregates and metadata stay open.
[Projection contract](CQL_INDEXED_SQL_ROWS.md#indexed-native-selected-cell-sdk-milestone-27-october).

Standard indexed LINQ Select and flat reselection/list/enumeration now pass7new
SQLite cases and the live Solana shared-demo gate. Core4154/SQLite49/Solana799pass.
Next: projection HTTP; metadata, aggregates, selected stages and M17 constructor
shapes stay open. [Current contract](CQL_INDEXED_SQL_ROWS.md#standard-indexed-linq-select-27-october).

Indexed HTTP projection is implemented via optional frozen columns on the existing
indexed route. FullCore4165 and nine-request live Solana/SQLite HTTP gate pass.
Next: indexed aggregate/metadata capabilities and common-demo gaps; production
authentication/durable billing and full parity remain open.

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
Shared queryable metadata export policy and exact source-root admission now pass17focusedcases; schema3 SQLite rows/cardinality now pass22newcases (98indexed/205fullprovider), shared metadata SDK encrypted-row/missing-context acceptance and7WEB4/tariff admission cases now pass (Core4235/SQLite207); live schema3 SDK/HTTP42requests and interactive/read-only reuse now pass; selected metadata SDK/SQLite now passes8newCore/12newSQLite cases with explicit roots and proof-before-DTO; live selected SDK/HTTP50requests and interactive lifecycle now pass, no source writes; CosmosDB teaching page/hub now passes native and interactive HTTP lifecycle, readiness ordering and exact cleanup; three-database fixture reconciliation and fresh681SDK/138HTTP/30save-check demo now pass with exact cleanup; Q01f explicit entity projection now passes Mongo218CQL/10native and Neo149CQL/10native cases alongside SQLite; standard final Select now passes14native cases/store; selected-stage/numeric integration follows, all Q01f cells remain red (CQL_MANUAL.md);
incremental transport and production worker scheduling/accounting remain open.
See CURRENT_STATUS and CQL_INDEXED_HTTP.md for evidence scope.

Standard final entity-stage Select is now verified on all3 (14 native cases/store, including throwing source constructors, closure rebinding, source-page ordering and invalid-cell rejection);6 new Core tests,2165 CQL/4249 full Core pass. Q01f stays red for operators/terminals after entity selection and remaining numeric integration.
