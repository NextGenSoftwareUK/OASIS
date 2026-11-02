# COSMIC ORM / CQL current status

Updated 1 November 2026. Goal active and incomplete. Priority: breadth-first live demos; deep LINQ parity remains paused.

## Verified scope

- 49 integrated query types: 3 canonical adapters, 42 bounded public/source profiles, 4 local PostgreSQL/Azure Cosmos/Oracle/AWS implementations.
- 44 providers have scoped hosted HTTP proof at their recorded releases. Browser proof: 5; 39 browser gates remain open. These are not full ORM/native-write certifications.
- MongoDB/SQLite/Neo4j parity: 50/82 checklist rows each; original investor-demo subset green. Work red rows only unless a reproducible bug appears.
- Latest completed provider: EOSIO/Vaulta native account metadata. Native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3, matching SQLite/Neo4j replicas2/2. Two version1 saves/no pending intents; original cohort frozen by completion ACK.
- Website72dee68 publishes EOSIO/Telos/Filecoin cards and pinned setup/C#/HTTP links;96client tests pass. Public HTML/scripts match exact git blobs. Browser interaction remains separate.
- Generic browser Mongo preparation copies identity/name/description/provenance, not all native metadata. Full payload mapping remains red in AFTER_DEMO_BACKLOG.md.

## Current phase: next red provider qualification

- EOSIO source28controls and actual CLI/manager/LINQ3/2 pass. Core1/API2/localHTTP1/publisher9/prepared-planner1/client1 pass; exact local fixture cleaned/Mongo stopped.
- Development parent `4d3e0b7cd148bed7ebdf0400ca371867873932c0` / Core `86e38a37ee860785a8273ade3308f6fc1775294d` published/ref-verified; manifest validates. Code68db832f/settings20b3a558 SUCCESS.
- Exactly3reviewed settings applied once; whole candidate/per-key digests match/DNA unchanged. Hosted original acceptance complete; no reapply/redeploy/replay.

## Exact next actions

1. Preserve completed EOSIO/Telos/Filecoin/Fantom cohorts, sources/owners/ACKs. EOSIO card72dee68/pinned links/96client tests and public bytes verified; no browser proof inferred.
2. ChainLink native protocol3/2,43controlled CQL tests and actual manager/LINQ/CLI3/2 now pass. Read-only source/explicit feed descriptors and commented demo/guide implemented. Next: query-only API config/permission/tariff, Core/API tests, local Mongo/privacy/prepared gates, then hosted rollout/card. See CHAINLINK_PUBLIC_SOURCE_STATUS.md; no API/hosted claim and no replay of completed native/console cohorts.
3. After qualification, implement only that red source/controlled tests/native manager+C#3/2, API permission/Mongo/privacy/prepared gates, exact hosted rollout/acceptance/card. Keep green scopes/deep parity frozen absent a reproduced bug.

## Durable state and rules

- Work only `C:/Source/OASIS-cosmic` on `work/cosmic-cql-ton-public`; Core branch `work/cosmic-cql-google-public`. Frontend `C:/Source/Web4Site/work/cosmic-cql`. Never edit/build original OASIS checkout.
- Previous Telos source/ACKs: `LocalState/COSMIC/HostedTelosPublicSourceCurrent`; cleaned local fixture journals: `LocalState/COSMIC/TelosMongoHttpCurrent`; code publication/job ACKs: `LocalState/COSMIC/HostedTelosReleaseCurrent`.
- EOSIO source/native ACKs: LocalState/COSMIC/HostedEosioPublicSourceCurrent; local journal EosioMongoHttpCurrent; release ACKs HostedEosioReleaseCurrent; frozen hosted cohort HostedQueryAcceptance-EOSIOOASIS. Exact paths/evidence in EOSIO_PUBLIC_SOURCE_STATUS.md.
- Protected settings snapshots/intents/ACKs: `LocalState/COSMIC/HostedSourceRolloutCurrent/telos-public-*`. Never print their contents or overwrite/replay journals.
- Raw logs/results: worktree `TestResults/` only. Durable identities/config/receipts stay in LocalState. Ledger: ENVIRONMENT_AND_CLEANUP_LEDGER.md (mixed encoding; append UTF8 bytes). Unknown root `logs` preserved.
- Build under the existing provider lease with `/m:2 /nr:false /p:UseSharedCompilation=false`. Reuse SDK10.0.401/Node24.19.0/Python3.12/Mongo8.3.11; no new environment planned. No production/master promotion.
- Completed hosted/native/local cohorts are frozen; never resave acknowledged copies or recreate catalogue owners. Existing native failure diagnostics for TRON/Rootstock are preserved, not blindly retried.

## Deferred and remaining full scope

- Hosted Azure Cosmos, Oracle and PostgreSQL explicitly deferred by David. No setup re-ask or paid account/resource. Public Azure Blob and AWS S3 are already hosted; local/emulator proof is not owned hosted-database proof.
- Browser/OS control remains closed; no automation/repeat owner request. Solana and other browser gates remain open.
- Full tracking/save ownership, atomic concurrency, children/depth/query parameters, rollback/history/deltas, migration/placement and broader HyperDrive operational acceptance remain on the roadmap. No overall completion percentage follows from demos.
- Deep entity-selected terminals, unrestricted POCO mapping and migrations tooling stay deferred behind demo breadth. V2 implementation only; legacy gaps documentation-only.

[EOSIO proof](EOSIO_PUBLIC_SOURCE_STATUS.md) | [Telos proof](TELOS_PUBLIC_SOURCE_STATUS.md) | [Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Manual](CQL_MANUAL.md) | [Architecture](CQL_ARCHITECTURE.md) | [Parity](CQL_PARITY_CHECKLIST.md) | [Backlog](AFTER_DEMO_BACKLOG.md) | [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
