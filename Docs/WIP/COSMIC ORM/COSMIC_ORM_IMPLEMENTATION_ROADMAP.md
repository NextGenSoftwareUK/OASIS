# COSMIC ORM implementation roadmap

## Current verified milestone — 1 November 2026

**38 providers have scoped hosted HTTP evidence; 5 have browser evidence and 33 browser gates remain open.** Arweave passes native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3 and matching SQLite/Neo4j replicas2/2. Two private saves were acknowledged once. Source writes and browser interaction are not inferred.

Latest hosted acceptance: parent37a530a87e601397f187430728bb9fb02b224f87/Core5fd361db11f5d54e3e99282536d7c7be842edf86, job3da7f986 SUCCESS. This docs-only descendant preserves testedb158bda61 code; original settings jobec59010e was removed and never replayed. Original Arweave cohort completion ACK retained. Website61e85b75a933004d1f005f85f1318fa05ad139de publishes Arweave with guided089a778c/C#b158bda61. Public HTML155229bytes/SHA2566fda659c10e90bb99d7636e04b62d3dc343b3a0c36cd85bb5b3f3c3e3c5c94f3 matches the committed git blob. Browser/real-expiry gates remain separate.

There are **40 implemented query types**: 3 canonical adapters,35bounded public/source profiles and2local Azure/Oracle implementations. Arweave native/control/manager/API/local and hosted federation/reuse/privacy/replica gates pass. Deployed tariffs38/hosted38/browser5/open33. TON pacing fix/13query controls pass but nativeHTTP500 remains red and isolated; no TON support grant. Full ORM/EF parity/HyperDrive certification remains incomplete.


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
