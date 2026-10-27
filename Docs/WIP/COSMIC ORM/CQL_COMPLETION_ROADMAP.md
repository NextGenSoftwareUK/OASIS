# CQL completion roadmap

[Current status](CURRENT_STATUS.md) owns the exact next action; [checklist](CQL_PARITY_CHECKLIST.md) owns acceptance.

1. Q01f shared IQueryable bridge is committed and tested; keep native database cells red.
2. Implement MongoDB and Neo4j native stages; SQLite focused row/graph/visibility/V2 acceptance passes. Extend cross-database native matrix, preserving source windows and protected errors.
3. Complete staged terminal/projection integration and proving native tests before promoting Q01f cells.
4. Continue remaining red rows in checklist priority order. Leave green rows/cursors frozen unless a bug is reproduced.
5. Finish deferred large parity items, including POCO mapping and migrations tooling, at their assigned priority.
6. Audit every declared requirement before claiming full EF/NHibernate or COSMIC completion.

The 12 investor-demo rows, Q23/Q23e/Q49a and Q01a–e are scoped completed milestones.
Unsupported shapes fail explicitly; client materialization or silent fallback cannot replace native implementation.
[Earlier checkpoints](Archive/CQL_COMPLETION_ROADMAP_checkpoint_2026-10-27.md) and
[original roadmap](Archive/CQL_COMPLETION_ROADMAP_history_2026-10-27.md) preserve history.
