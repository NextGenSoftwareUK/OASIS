# COSMIC ORM implementation roadmap

Updated 1 November 2026. Full programme remains incomplete. [CURRENT_STATUS](CURRENT_STATUS.md) owns release state and the exact next action.

1. Complete breadth-first live provider demos: current 45 integrated types, 41 hosted HTTP proofs and 5 browser proofs. Preserve green cohorts. Fantom hosted acceptance is complete; card/public-byte proof is next in CURRENT_STATUS.
2. Continue explicit filtered source queries and Mongo federation for remaining red providers. Azure Cosmos, Oracle and PostgreSQL hosting are deferred. Public source reads do not establish native writes or full ORM support.
3. Finish scoped HyperDrive V2 operational acceptance: authorized late-replica baseline/reconciliation, retained obligations and uncertain-commit recovery. Preserve existing six-direction native proofs; broader hosted/operator/transport/scale acceptance remains open.
4. Reconcile original ORM requirements against provider-specific evidence: change tracking, save-state ownership, atomic cross-client conditional writes, child load/save/query parameters and depth, rollback, version/history, per-save deltas, migration and placement.
5. Define and implement primary-authority cutover with authority epochs and in-flight ownership; replica baseline delivery alone is not authority migration.
6. Resume deferred EF/NHibernate parity, unrestricted POCO mapping and migration tooling only after demo priorities. Work red checklist rows; freeze green rows/cursors absent a reproduced bug.

Only HyperDrive V2 receives implementation changes; document legacy gaps without fixing legacy code. Preserve root-cause-only policy and explicit capability errors. No overall completion percentage follows from focused demos.

[Acceptance checklist](CQL_PARITY_CHECKLIST.md) · [Detailed backlog](AFTER_DEMO_BACKLOG.md) · [Original requirements/history](Archive/COSMIC_ORM_IMPLEMENTATION_ROADMAP_history_2026-10-27.md) · [Prior milestone history](Archive/COSMIC_ORM_IMPLEMENTATION_ROADMAP_before_fantom_release_2026-11-01.md).
