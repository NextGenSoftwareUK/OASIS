# Implementation status

[Current status](CURRENT_STATUS.md) is the recovery entry point and milestone record.

- Full COSMIC ORM/CQL programme remains incomplete.
- The 12 investor-demo rows are green on SQLite, MongoDB and Neo4j.
- Q23/Q23e/Q49a and Q01a–e are verified within their checklist scopes.
- Active red row Q01f: shared stage contract is committed; IQueryable bridge is committed in Core b9bccf0.
- SQLite native stages are implemented with 42 passing CQL provider tests, including native V2 typed/graph/visibility and protected traversal acceptance; broader cross-database acceptance remains pending. MongoDB native stages are implemented with 212 passing compiler/admission tests; 18 live native entity-stage cases pass. Neo4j native stages now pass 144 compiler tests and 19 live stage cases; staged terminal/projection integration remains pending.
- Latest native checkpoint: 1,631 checks:18 entity-stage cases/store, Neo snapshot rejection,9 entity-count and16 standard staged cardinality cases/store.
- Current shared entity-count foundation regression: 2,559 passes; both Core targets build. SQLite native count enrolled (44 CQL provider tests pass); Mongo count passes215 compiler tests and9 live cases; Neo count passes147 compiler tests and9 live cases; standard staged Count/LongCount/Any/All now live-verified on all3. Element/projection/remaining numeric integration pending. Full Core suite still has an unverified WEB4 host prerequisite.
- Green features/federation cursors stay frozen unless a reproducible bug appears.
- HyperDrive V2 only; legacy gaps receive documentation.

[Checklist](CQL_PARITY_CHECKLIST.md) owns per-database acceptance.
Historical checkpoints do not establish blanket ORM completeness.
[Previous detail](Archive/IMPLEMENTATION_STATUS_checkpoint_2026-10-27.md) and
[original history](Archive/IMPLEMENTATION_STATUS_history_2026-10-27.md) retain prior evidence.
