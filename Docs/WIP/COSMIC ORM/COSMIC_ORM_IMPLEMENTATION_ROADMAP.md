# COSMIC ORM implementation roadmap

[Current status](CURRENT_STATUS.md) is the recovery entry point. Full goal remains active and incomplete.

| Order | Work | Acceptance |
|---|---|---|
| 1 | Preserve verified baseline on MongoDB/SQLite/Neo4j | Exact deployed source, acknowledged writes, verified destination copies, CQL rows/scalars/federation and tenant isolation |
| 2 | Close late replica enrollment and fenced reconciliation gaps (X10a/X10b) | Explicit authorization/baseline proof; no independent target overwrite, lineage relaxation, skipped queue entry or blind write replay |
| 3 | Continue content-query demo breadth and Mongo-paired presets | ActivityPub/IPFS leaf3/2and Mongo pairing5/4verified live; SOLID/public S3 metadata native3/2and Mongo5/4HTTP gates now pass; browser gates open. Next Oracle/Solana and remaining providers per CURRENT_STATUS; Azure pending/free-only |
| 4 | Reconcile remaining ORM graph/parameter, tracking/save ownership, conditional concurrency, rollback/history/delta requirements | Exact native per-provider tests and full-scope audit; prior focused proofs do not imply completion |
| 5 | Resume deferred LINQ/EF/NHibernate parity and broader providers | Red checklist cells only; deep entity-selected terminals, unrestricted POCO mapping and migrations tooling remain deferred |

Three-database baseline35/35and ActivityPub preparation/federation/replication/isolation are verified live within their declared scopes. Broader hosted providers and the full ORM roadmap remain incomplete. No overall completion percentage is inferred from these examples.
Only HyperDrive V2 receives implementation changes; legacy gaps are documentation-only.

[Strict feature checklist](CQL_PARITY_CHECKLIST.md) owns acceptance.
[Full original roadmap/history](Archive/COSMIC_ORM_IMPLEMENTATION_ROADMAP_history_2026-10-27.md) preserves earlier requirements.
