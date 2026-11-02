# COSMIC ORM / CQL current status

Updated 1 November 2026. Goal active and incomplete. Priority: breadth-first live demos; deep LINQ parity remains paused.

## Verified scope

- 50 integrated query types: 3 canonical adapters, 43 bounded public/source profiles, 4 local PostgreSQL/Azure Cosmos/Oracle/AWS implementations.
- 46 providers have scoped hosted HTTP proof at their recorded releases. Browser proof: 5; 41 browser gates remain open. These are not full ORM/native-write certifications.
- MongoDB/SQLite/Neo4j parity: 50/82 checklist rows each; original investor-demo subset green. Work red rows only unless a reproducible bug appears.
- Latest completed provider: ChainLink public feed observations. Native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3 and matching SQLite/Neo4j replicas2/2 each. Two version1 saves/no pending intents; original cohort frozen.
- Website80633dc publishes ChainLink/EOSIO/Telos/Filecoin cards and pinned setup/C#/HTTP links;97client tests pass. Public HTML/scripts match exact git blobs. Browser interaction remains separate.
- Generic browser Mongo preparation copies identity/name/description/provenance, not all native metadata. Full payload mapping remains red in AFTER_DEMO_BACKLOG.md.

## Current phase: publish verified BlueSky card

- BlueSky hosted HTTP acceptance passes at parentf36dd0d56/Core499e4e0, settings6dfdbc6c SUCCESS: native3/2, Mongo5/4, reuse0new/2existing, foreign-private0/public3, SQLite/Neo4j replicas2/2 each. Two version1 saves/no pending intents; original cohort frozen. Cached public AppView only; generic copies retain identity/name/description/provenance, not all native metadata. Card publication pending; browser unverified.
- Existing local gates:37source controls, actual CLI3/2, Core1/API2/localHTTP1/publisher9/prepared1/client1. No replays or new cohorts.

## Exact next actions

1. Publish frontend task pending cardecfdd54 after changing its acceptance wording and guide link to the verified release. Run the client suite once, fetch/inspect main ancestry, fast-forward main only with scoped tested work.
2. Verify public HTML and both scripts against exact git blobs; HTTP/asset matching does not prove browser interaction.
3. Update current provider reports with publication evidence; preserve all completed cohorts. Deep parity remains paused; resume next red breadth scope after this milestone.

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

[ChainLink proof](CHAINLINK_PUBLIC_SOURCE_STATUS.md) | [EOSIO proof](EOSIO_PUBLIC_SOURCE_STATUS.md) | [Telos proof](TELOS_PUBLIC_SOURCE_STATUS.md) | [Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Manual](CQL_MANUAL.md) | [Architecture](CQL_ARCHITECTURE.md) | [Parity](CQL_PARITY_CHECKLIST.md) | [Backlog](AFTER_DEMO_BACKLOG.md) | [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
