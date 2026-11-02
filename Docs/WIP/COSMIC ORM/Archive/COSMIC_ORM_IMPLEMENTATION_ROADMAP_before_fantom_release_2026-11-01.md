Superseded by [current COSMIC_ORM_IMPLEMENTATION_ROADMAP.md](../COSMIC_ORM_IMPLEMENTATION_ROADMAP.md) on 1 November 2026. Historical counts and pending statements below are not current status.

# COSMIC ORM implementation roadmap

## Current verified milestone — 1 November 2026

**40 providers have scoped hosted HTTP evidence; 5 have browser evidence and 35 browser gates remain open.** Azure Blob passes native3/2, Mongo-paired5/4, repeat0new/2existing, foreign-private0/public3 and exact SQLite/Neo4j replicas2/2. Two private saves acknowledged at version1; completion ACK prevents replay. Azure is anonymous public Blob properties, not hosted Cosmos DB or owned Azure writes.

Latest hosted acceptance: parent bccef53d6dbe0ec75028220b34eb0d3f2b0f4db8 / Core d133cc20c29441158da0e590d7f3c85b351dd826. Code job e6a98438-bf39-444e-89eb-5367322525bc and single settings job8e382a80-86ec-4d1d-8709-ca459f9e9781 succeeded. Exactly3settings applied once with per-key ACKs and complete candidate equality; canonical DNA/unrelated settings preserved. Original Azure cohort completion ACK retained; never replay it. Website969462188cd122f3dd0a2523b782e6f3ea2deefa publishes the Azure card with guide781bed195/C#bccef53d6 links. Public HTML158677bytes/SHA256f0ff7eccd617edcb3a754ce82a8b09184ce3a666ccf57aa84328ba07025c2106 and both script bytes match exact git blobs;92client tests pass. Browser interaction and real-expiry gates remain separate.

There are **45 currently integrated query types**: 3 canonical adapters, 38 bounded public/source profiles and 4 local PostgreSQL/Azure/Oracle/AWS implementations. PostgreSQL content and AWS DynamoDB Local identity queries pass native and local Mongo/HTTP checks; hosted PostgreSQL/owned AWS remain open. Azure Blob public properties pass SDK, manager/LINQ, local and hosted HTTP/Mongo checks. Hosted40/tariffs40/browser5/open35. Preserved broad-branch prototypes are not silently counted as current hosted coverage. Full ORM/EF parity/HyperDrive certification remains incomplete.


[Current status](../CURRENT_STATUS.md) is the recovery entry point. Full goal remains active and incomplete.

| Order | Work | Acceptance |
|---|---|---|
| 1 | Preserve verified baseline on MongoDB/SQLite/Neo4j | Exact deployed source, acknowledged writes, verified destination copies, CQL rows/scalars/federation and tenant isolation |
| 2 | Close late replica enrollment and fenced reconciliation gaps (X10a/X10b) | Explicit authorization/baseline proof; no independent target overwrite, lineage relaxation, skipped queue entry or blind write replay |
| 3 | Continue content-query demo breadth and Mongo-paired presets | ActivityPub/IPFS leaf3/2and Mongo pairing5/4verified live; SOLID/public S3 metadata native3/2and Mongo5/4HTTP gates now pass; browser gates open. Solana HTTP gates pass/browser pending; Google/Ethereum/Polygon/Arbitrum/Cosmos/Polkadot/NEAR are locally ready, not hosted. Azure/Oracle hosting deferred; exact next work in CURRENT_STATUS |
| 4 | Reconcile remaining ORM graph/parameter, tracking/save ownership, conditional concurrency, rollback/history/delta requirements | Exact native per-provider tests and full-scope audit; prior focused proofs do not imply completion |
| 5 | Resume deferred LINQ/EF/NHibernate parity and broader providers | Red checklist cells only; deep entity-selected terminals, unrestricted POCO mapping and migrations tooling remain deferred |

Three-database baseline35/35and ActivityPub preparation/federation/replication/isolation are verified live within their declared scopes. Broader hosted providers and the full ORM roadmap remain incomplete. No overall completion percentage is inferred from these examples.
Only HyperDrive V2 receives implementation changes; legacy gaps are documentation-only.

[Strict feature checklist](../CQL_PARITY_CHECKLIST.md) owns acceptance.
[Full original roadmap/history](COSMIC_ORM_IMPLEMENTATION_ROADMAP_history_2026-10-27.md) preserves earlier requirements.

Radix hosted milestone: original cohort completed at74aaa9b4d/Core8b79301 after settlement correction; no duplicate saves. [Scope, failure history and hashes](../RADIX_PUBLIC_SOURCE_STATUS.md#hosted-acceptance-complete). Browser renewal/interaction remain separate. Earlier deployed-tariff headlines incorrectly included local Azure/Oracle: actual live catalogue35, implemented types37.
