# COSMIC ORM implementation roadmap

## Current verified milestone — 1 November 2026

**32 providers have scoped hosted HTTP evidence; 5 have browser evidence and 27 browser gates remain open.** Hedera now passes native 3/2, Mongo federation 5/4, repeat reuse (zero new/two existing), foreign-private zero/public three, and two matching replicas on both SQLite and Neo4j. The original owner cohort was resumed after the scheduling correction; no duplicate seed saves were required.

Application `4fb66c68705be82d37bb97d9764b97f165661fca`, Core `89b5ffad5c589e6607f26892f46a4ee718335fe2`, deployment `122310fc-11ec-45cf-a631-2ebd471ce66a` succeeded. Existing settings deployment `9dfe2fbd-45f9-4706-a3d4-85ca0f5c2a3d` was retained. Website main `07a36e7` is byte-identical to the served HTML (SHA256 `742262270af5294b0e16a3cb254a8152b545d668e81d7037314f64f024644367`); this proves publication, not browser interaction.

Sui brings implemented query types to **35**, with **30 native tests and an actual shared-manager CLI 3/2 pass**. Sui API admission, local HTTP/Mongo integration, hosting and browser verification remain open. Azure/Oracle hosting stays explicitly deferred. Full ORM/parity/HyperDrive certification remains incomplete. Dated older milestones below retain their original scope and are superseded by this current summary.


[Current status](CURRENT_STATUS.md) is the recovery entry point. Full goal remains active and incomplete.

| Order | Work | Acceptance |
|---|---|---|
| 1 | Preserve verified baseline on MongoDB/SQLite/Neo4j | Exact deployed source, acknowledged writes, verified destination copies, CQL rows/scalars/federation and tenant isolation |
| 2 | Close late replica enrollment and fenced reconciliation gaps (X10a/X10b) | Explicit authorization/baseline proof; no independent target overwrite, lineage relaxation, skipped queue entry or blind write replay |
| 3 | Continue content-query demo breadth and Mongo-paired presets | ActivityPub/IPFS leaf3/2and Mongo pairing5/4verified live; SOLID/public S3 metadata native3/2and Mongo5/4HTTP gates now pass; browser gates open. Solana HTTP gates pass/browser pending; Google/Ethereum/Polygon/Arbitrum/Cosmos/Polkadot/NEAR are locally ready, not hosted. Azure/Oracle hosting deferred; exact next work in CURRENT_STATUS |
| 4 | Reconcile remaining ORM graph/parameter, tracking/save ownership, conditional concurrency, rollback/history/delta requirements | Exact native per-provider tests and full-scope audit; prior focused proofs do not imply completion |
| 5 | Resume deferred LINQ/EF/NHibernate parity and broader providers | Red checklist cells only; deep entity-selected terminals, unrestricted POCO mapping and migrations tooling remain deferred |

Three-database baseline35/35and ActivityPub preparation/federation/replication/isolation are verified live within their declared scopes. Broader hosted providers and the full ORM roadmap remain incomplete. No overall completion percentage is inferred from these examples.
Only HyperDrive V2 receives implementation changes; legacy gaps are documentation-only.

[Strict feature checklist](CQL_PARITY_CHECKLIST.md) owns acceptance.
[Full original roadmap/history](Archive/COSMIC_ORM_IMPLEMENTATION_ROADMAP_history_2026-10-27.md) preserves earlier requirements.
