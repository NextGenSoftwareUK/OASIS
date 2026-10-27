# CQL completion roadmap

User priority27October: [eight-provider demo expansion](CQL_DEMO_EXPANSION_ROADMAP.md) takes precedence over unfinished parity-only depth. Original scope remains active.

[Current status](CURRENT_STATUS.md) owns the exact next action; [checklist](CQL_PARITY_CHECKLIST.md) owns acceptance.

1. Q01f shared IQueryable bridge is committed and tested; keep native database cells red.
2. All3 native entity stages pass 18 cases/store plus Neo snapshot rejection (1,556-check gate). Preserve source windows/protected errors and keep cells red pending terminal/projection integration.
3. Native count and standard Count/LongCount/Any/All now pass25 cases/store (1,631 native checks). Forward elements now pass20 cases/store (1,691 native checks); standard Reverse/tail/from-end now pass12 additional cases/store (1,745 native checks); explicit default overloads now pass12 cases/store (1,781 native checks), then native entity-stage projection (shared contract/owner now tested), standard Select and remaining selected-stage/numeric integration and proving native tests before promoting Q01f cells.
4. Continue remaining red rows in checklist priority order. Leave green rows/cursors frozen unless a bug is reproduced.
5. Finish deferred large parity items, including POCO mapping and migrations tooling, at their assigned priority.
6. Audit every declared requirement before claiming full EF/NHibernate or COSMIC completion.

The 12 investor-demo rows, Q23/Q23e/Q49a and Q01a–e are scoped completed milestones.
Unsupported shapes fail explicitly; client materialization or silent fallback cannot replace native implementation.
[Earlier checkpoints](Archive/CQL_COMPLETION_ROADMAP_checkpoint_2026-10-27.md) and
[original roadmap](Archive/CQL_COMPLETION_ROADMAP_history_2026-10-27.md) preserve history.
