# Implementation status

Read [CURRENT_STATUS](CURRENT_STATUS.md) for the active phase, verified cohorts, exact next step and commands.

- Full COSMIC ORM/CQL programme: **incomplete**, goal active.
- Investor demo: 12 scoped rows green on SQLite/MongoDB/Neo4j.
- Q23 All parent, Q49a source composition and Q23e projected-All are verified on all three. Shipping C# demo:24 examples pass through V2 HolonManager. Active red row: Q01 IQueryable/IQueryProvider; rows/cardinality/All increment has25 entity-cardinality/IQueryable focused/2,524 CQL-regression/441 native checks passing, both Core targets build. Flat Select/selected stages/All/Count/LongCount/Any are verified; entity and selected elements are verified; selector/unstaged value aggregates are verified; Int32 double Average and all3 checked Int64 double Average are verified; entity cardinality predicates after paging are verified; staged numeric reductions, repeated Select, entity element predicates after paging and general post-window entity composition remain pending.
- Core source capture/admission: verified by 3,851 tests and both Core targets.
- Projected All: native implementations exist on all three; focused gates are recorded in CURRENT_STATUS.
- Source composition: Mongo185 compiler/53 native checks pass; SQLite35 compiler/26 native tests pass; Neo4j163 compiler tests pass; all three enrolled in passing native acceptance.
- Source matrix:216 checks passed,72/store across rows/count/projections; exact fixture cleanup and unchanged original fixture verified.
- Combined gate:168 checks passed, zero skips; no create replay; owned cycle controls and exact six-ID cleanup/store verified, original fixture unchanged.
- Green features/cursors frozen; legacy HyperDrive receives issue documentation only.

Historical ORM, replication, billing, history, child graph, encryption and transport checkpoints are evidence of their original scope, not a blanket completeness claim.
[Full implementation history](Archive/IMPLEMENTATION_STATUS_history_2026-10-27.md) preserves prior detail, including original bytes with legacy encoding.

Full Core run:3,853 passed/6 failed for missing new-worktree WEB4 host artifact; no skips. This prerequisite remains unverified, not a full-suite success.
