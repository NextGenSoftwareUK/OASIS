# COSMIC ORM / CQL

1. **[Current status — start here after compaction](CURRENT_STATUS.md)**

- [Approved content-query demo expansion: first10providers, features and acceptance](CONTENT_QUERY_DEMO_ROADMAP.md)
2. [Presenter run-through](CQL_DEMO_RUNTHROUGH.md) and [demo launcher](CQL_DEMO_LAUNCHER.md). [All-provider MVP expansion](CQL_DEMO_EXPANSION_ROADMAP.md) and [provider inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md): active priority before full parity. [Solana next-provider admission](CQL_SOLANA_DEMO_ADMISSION.md) and [chain query contract](CQL_CHAIN_QUERY_CONTRACT.md). [fresh reusable environments](CQL_DEMO_ENVIRONMENTS.md), [IPFS admission](CQL_IPFS_DEMO_ADMISSION.md) and [protocol admission](CQL_PROTOCOL_DEMO_ADMISSION.md) and [verified Mastodon CQL point profile](CQL_ACTIVITYPUB_NATIVE_PROFILE.md).
3. [CQL parity checklist](CQL_PARITY_CHECKLIST.md): one feature row, independent proof per database; red rows only.
3. [CQL completion roadmap](CQL_COMPLETION_ROADMAP.md) and [COSMIC ORM roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md).
4. [Implementation status](IMPLEMENTATION_STATUS.md) and [deferred parity handoff](DEFERRED_PARITY_HANDOFF.md).
5. [V2 failover/load-balancing acceptance](V2_ROUTING_ACCEPTANCE.md) and [lineage migration/audit](V2_LINEAGE_MIGRATION.md). [CQL manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
6. [Historical reports/reference](Archive/README.md) and [previously committed evidence](Evidence/README.md).

Recovery: read CURRENT_STATUS and the active provider guide first (together under 300 lines). The two short roadmaps and IMPLEMENTATION_STATUS provide broader programme context when needed.
Update CURRENT_STATUS at every milestone. Do not append a chronological work log to the active roadmaps.
New logs/test output belong in the worktree's TestResults, never Docs. Reports record counts, hashes and evidence paths; no new raw evidence commits.
Persistent, irreproducible fixture state belongs in ignored LocalState/COSMIC and is not disposable test output.

- [SOLID CQL record-point profile and verification](CQL_SOLID_NATIVE_PROFILE.md).

- [Interactive IPFS/SOLID point demo launch and limits](CQL_INTERACTIVE_POINT_DEMO.md).

- [Indexed metadata policy and schema3 implementation plan](CQL_INDEXED_METADATA.md): verified shared policy and SQLite schema3 rows/cardinality; declared-range live/HTTP/interactive acceptance now passes; production limits remain explicit.

- [Indexed SQL canonical rows: explicit schema, privacy, evidence and remaining integration](CQL_INDEXED_SQL_ROWS.md).

- [Indexed physical-read billing](CQL_INDEXED_BILLING.md): exact server tariffs, durable SQL/RPC attempts and current acceptance limits.

- [WEB4 indexed frozen preparation](CQL_INDEXED_WEB4_PREPARATION.md): server aliases, immutable intent and reservation bound; stock HTTP wiring verified; metadata live HTTP remains pending.

- [Indexed WEB4 HTTP checkpoint](CQL_INDEXED_HTTP.md): frozen reservation/intent execution, precise receipts and current deployment limits.

- [Three-database hub routing and retained-fixture preflight](CQL_DATABASE_DEMO_HUB.md): discovery verified; native launch pending reconciliation.
