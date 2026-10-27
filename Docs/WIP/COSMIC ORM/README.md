# COSMIC ORM / CQL

1. **[Current status — start here after compaction](CURRENT_STATUS.md)**
2. [Eight-provider demo expansion](CQL_DEMO_EXPANSION_ROADMAP.md): active priority before full parity. [Solana next-provider admission](CQL_SOLANA_DEMO_ADMISSION.md) and [chain query contract](CQL_CHAIN_QUERY_CONTRACT.md).
3. [CQL parity checklist](CQL_PARITY_CHECKLIST.md): one feature row, independent proof per database; red rows only.
3. [CQL completion roadmap](CQL_COMPLETION_ROADMAP.md) and [COSMIC ORM roadmap](COSMIC_ORM_IMPLEMENTATION_ROADMAP.md).
4. [Implementation status](IMPLEMENTATION_STATUS.md) and [active projected-All/source contract](CQL_PROJECTED_ALL_EXECUTION_CONTRACT.md).
5. [CQL manual](CQL_MANUAL.md), [architecture](CQL_ARCHITECTURE.md), [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
6. [Historical reports/reference](Archive/README.md) and [previously committed evidence](Evidence/README.md).

Recovery: read CURRENT_STATUS, the two short roadmaps, IMPLEMENTATION_STATUS and the active contract; together they stay below 300 lines.
Update CURRENT_STATUS at every milestone. Do not append a chronological work log to the active roadmaps.
New logs/test output belong in the worktree's TestResults, never Docs. Reports record counts, hashes and evidence paths; no new raw evidence commits.
Persistent, irreproducible fixture state belongs in ignored LocalState/COSMIC and is not disposable test output.
