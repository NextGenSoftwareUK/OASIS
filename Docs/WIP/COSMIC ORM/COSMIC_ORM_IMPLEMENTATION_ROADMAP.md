# COSMIC ORM implementation roadmap

## Current verified milestone — 1 November 2026

**33 providers have scoped hosted HTTP evidence; 5 have browser evidence and 28 browser gates remain open.** Sui now passes native 3/2, Mongo federation 5/4, repeat reuse (zero new/two existing), foreign-private zero/public three, and two matching replicas on both SQLite and Neo4j. No source/chain writes or browser certification are inferred.

Application `7e0b60994921d03e09abe4a7050ca5bf5f02bfdb`, Core `0e540eb911671a6a249458ea9b69d3e96fb1d212`, app deployment `81794c84-1f8b-41e1-a9e4-6b79015fa173` and settings deployment `1d5007cb-e593-458e-b838-9d535ba93e70` succeeded. Website main `9679699d391b5b49117c3009ee2bfcc0add41464` serves byte-identical committed HTML (148135bytes, SHA256 `b8d17101a869a1dffb8cd2c076b8c6577a24086bfcd052394100e7245d069a35`), including Sui setup/C# links. This proves publication, not browser interaction.

There are **36 implemented query types**, including the new MultiversX/Elrond native profile:34tests plus actual shared-manager/C# CLI3/2pass; API/local HTTP native3/2+Mongo5/4+privacy now verified, hosting remains open. Sui retains30native tests and its CLI/hosted proof. Sui API admission (2 tests), Core tariff (1), generated preset planning (1), local HTTP/Mongo (1), and configuration controls (3) also pass. TRON is an unadmitted candidate with34controlled passes and a real429failure; it is not counted. Azure/Oracle hosting stays explicitly deferred. Full ORM/parity/HyperDrive certification remains incomplete. Dated milestones retain their original scope.


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
