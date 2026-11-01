# COSMIC ORM implementation roadmap

## Current verified milestone — 1 November 2026

**35 providers have scoped hosted HTTP evidence; 5 have browser evidence and 30 browser gates remain open.** Radix native3/2 and Mongo5/4 pass; the original cohort resumed after the atomic settlement fix with zero new saves/two reused copies, foreign-private0/public3 and matching SQLite/Neo4j replicas2/2. Source writes and browser interaction are not inferred.

Latest hosted acceptance: parent `74aaa9b4daeadfad00ce9c667d78bb957f219ea8`, Core `8b79301b275536c04e7a971dc88d61735ebde96f`, correction job `f4c02dbd-7e7d-4482-9e2c-1ee28f5e1cc6` SUCCESS. Initial Radix app0b41c4b9/settings15f047fd succeeded at1f39016fa. Original owner was renewed through the normal refresh endpoint after scoped owned-fixture credential recovery; automatic sandbox/browser session renewal remains open. Website main95c53cc still serves Elrond/Sui/Hedera; Radix card publication is next.

There are **37 implemented query types**: 3 canonical adapters, 32 bounded public/source profiles and 2 local Azure/Oracle implementations. Actual deployed query-tariff catalogue has **35 entries**, confirmed from live configuration (the two local cloud implementations are excluded). Radix50native tests, actual CLI3/2, API/local HTTP and hosted continuation pass; atomic settlement fix has6native/125related/4shipping checks. Azure/Oracle hosting remains deferred. TRON429 stays red; Starknet native API headers3/2qualified, CQL not implemented. Full ORM/EF parity/HyperDrive certification remains incomplete.


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
