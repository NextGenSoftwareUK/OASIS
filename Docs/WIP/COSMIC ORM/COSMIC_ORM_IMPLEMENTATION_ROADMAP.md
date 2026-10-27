# COSMIC ORM implementation roadmap

Current execution state: [CURRENT_STATUS](CURRENT_STATUS.md). Full objective remains active; no completion claim.

| Order | Work | Acceptance owner |
|---|---|---|
| 1 | Finish current three-store CQL source/projected-All gap | Q49a/Q23e in the parity checklist; complete native gate |
| 2 | Continue remaining CQL red rows by scoped priority | Exact per-database tests; no green-feature/cursor hardening |
| 3 | Reconcile remaining ORM correctness, child graph/parameter, history and replication requirements | Roadmap requirements preserved in historical plan; current source + proving tests |
| 4 | Complete migration/placement/delta/history and operational acceptance still missing | Independent full-scope audit; do not infer success from focused tests |
| 5 | Broader EF/NHibernate parity and final programme audit | Every declared requirement independently verified |

Investor-demo rows are already green. Large POCO-without-IHolon mapping and migrations tooling stay deferred until the demo/current priority set is complete.
Only HyperDrive V2 receives implementation changes; legacy gaps are documentation-only.
Keep change tracking, save ownership, concurrency, conditional writes, rollback/versioning and per-save deltas in the full objective; prior implementations do not replace final acceptance.

[Feature checklist](CQL_PARITY_CHECKLIST.md) is the authoritative remaining feature inventory.
[Full original roadmap and history](Archive/COSMIC_ORM_IMPLEMENTATION_ROADMAP_history_2026-10-27.md) preserves all earlier requirements and checkpoints.
