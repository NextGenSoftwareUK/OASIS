# COSMIC ORM implementation roadmap

## Current verified milestone — 1 November 2026

**39 providers have scoped hosted HTTP evidence; 5 have browser evidence and 34 browser gates remain open.** TON passes native 3/2, Mongo-paired 5/4, repeat 0 new/2 existing, foreign-private 0/public 3 and exact SQLite/Neo4j replicas 2/2. Two private saves acknowledged once; completion ACK prevents replay. Source writes and browser interaction are not inferred.

Latest hosted acceptance: parent fb29aa936870dcfb3182102642e5997b2cedd90f / Core 2ac915f709ded7aabcd28a9db02aaea92fc52d97, settings job e3ae497c-3f19-45b2-a577-b0fc40a7ae6b SUCCESS. Revised settings applied once, all ACK hashes/full candidate matched, unrelated changes preserved. Original TON cohort completion ACK retained. Website6cfa7dcf91097108d8de679a1dc317ecc33da29d publishes TON with guide34ae56281/C#fb29aa936. Public HTML156743bytes/SHA25667f86c263203e2f88d3a485abe72d938f9847a229f12ee6c29b771f172719612 matches the committed blob; both versioned JS assets also match. Browser and real-expiry gates remain separate.

There are **43 currently integrated query types**: 3 canonical adapters, 36 bounded public/source profiles and 4 local PostgreSQL/Azure/Oracle/AWS implementations. PostgreSQL content and AWS DynamoDB Local identity queries pass native and local Mongo/HTTP checks; hosted PostgreSQL/owned AWS remain open. Hosted39/tariffs39/browser5/open34. Preserved broad-branch prototypes are not silently counted as current hosted coverage. Full ORM/EF parity/HyperDrive certification remains incomplete.


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

Radix hosted milestone: original cohort completed at74aaa9b4d/Core8b79301 after settlement correction; no duplicate saves. [Scope, failure history and hashes](RADIX_PUBLIC_SOURCE_STATUS.md#hosted-acceptance-complete). Browser renewal/interaction remain separate. Earlier deployed-tariff headlines incorrectly included local Azure/Oracle: actual live catalogue35, implemented types37.
