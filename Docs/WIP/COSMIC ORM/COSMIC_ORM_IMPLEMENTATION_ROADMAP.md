# COSMIC ORM implementation roadmap

## Current verified milestone — 1 November 2026

**34 providers have scoped hosted HTTP evidence; 5 have browser evidence and 29 browser gates remain open.** MultiversX/Elrond now passes native 3/2, Mongo federation 5/4, repeat reuse (zero new/two existing), foreign-private zero/public three, and two matching replicas on both SQLite and Neo4j. No source/chain writes or browser certification are inferred.

Latest verified hosted acceptance: application `801795a648a1ae0b9211c0a478027673cb79c156`, Core `f41b2a11253c222aac6bfdf67278d226e01ff96e`, app `19f65f59-e370-4cad-94ce-b0246872b891` and settings `18c4f04e-6ac3-4fa7-ac2a-ff312d095159` succeeded. Website main `95c53ccd3d20a99df4e8059c105c2aa4fcb8ebbb` serves byte-identical committed HTML (149523bytes, SHA256 `988e89929f268a5a447f00a09e7602d0c59a01e6bd15ef44ed0c37be16343a63`), including Elrond setup/C# links. This proves publication, not browser interaction.

There are **37 implemented query types**: 3 canonical adapters, 32 bounded public/source profiles and 2 locally scoped Azure/Oracle implementations. Radix now passes 50/50 focused tests (zero skips), real shared-manager three/two selection and the actual C# console demo; API admission/local HTTP/hosted/browser gates remain pending. Deployed query-tariff catalogue remains 36 types and scoped hosted HTTP count remains 34. Elrond hosted acceptance stays green. Azure/Oracle hosting remains deferred. TRON real429 remains red; Starknet transport works but its CQL contract is unqualified. Full ORM/parity/HyperDrive certification remains incomplete.


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
