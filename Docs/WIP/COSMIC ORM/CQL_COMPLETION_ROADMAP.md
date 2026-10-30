# CQL completion roadmap

[Current status](CURRENT_STATUS.md) owns the exact next step; [checklist](CQL_PARITY_CHECKLIST.md) owns provider acceptance. Full scope remains active.

1. Complete hosted MongoDB/SQLite/Neo4j acceptance. The deployed fix batch is healthy and three source saves succeeded; an older SQLite lineage barrier prevents convergence. Point-load provider-scope fix is tested on the task branch, awaiting scheduled integration.
2. Establish explicit late-replica baseline and authorized reconciliation acceptance (X10a/X10b). Never weaken receiver lineage or skip/delete retained obligations to obtain a green demo.
3. Continue demo content-query breadth: Oracle Mongo-paired C#/HTTP/browser presets, followed by the approved provider list in CURRENT_STATUS. Each preset needs actual rows, provider provenance and tenant isolation; point reads alone are insufficient.
4. Hosted Azure remains pending at the user's direction (free-only, no existing free subscription). Native emulator proof does not satisfy hosted acceptance.
5. Preserve earlier SQLite/MongoDB/Neo4j query proofs within their declared scopes. Freeze green features/cursors absent a reproducible bug.
6. LINQ parity depth after entity-selected stages (All/element/numeric terminals), unrestricted POCO mapping and migrations tooling remain deferred. Q01f stays red until its complete matrix passes; do not resume it ahead of demo breadth.
7. Complete remaining ORM correctness, history/deltas, migration/placement, auth/settlement/scaling and broader-provider acceptance. Demo milestones do not establish full ORM or EF/NHibernate parity.

Update CURRENT_STATUS and the resource ledger at milestones. Raw evidence goes only in worktree TestResults; persistent source/identity/ACK journals stay under LocalState. Use bounded builds and one producer/consumer lease, no repeated environments. Integrate verified Development batches once/twice daily.

[Earlier roadmap history](Archive/CQL_COMPLETION_ROADMAP_before_entity_selected_sqlite_2026-10-28.md), [implementation roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md), [demo expansion](CQL_DEMO_EXPANSION_ROADMAP.md).
