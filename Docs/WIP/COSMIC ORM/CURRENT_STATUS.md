# COSMIC ORM / CQL current status

Updated 2 November 2026. Goal active and incomplete. Priority: breadth-first live demos; deep LINQ parity remains paused.

## Verified scope

- 51 integrated query types: 3 canonical adapters, 44 bounded public/source profiles, 4 local PostgreSQL/Azure Cosmos/Oracle/AWS implementations.
- 46 providers have scoped hosted HTTP proof at their recorded releases. Browser proof: 5; 41 browser gates remain open. These are not full ORM/native-write certifications.
- MongoDB/SQLite/Neo4j parity: 50/82 checklist rows each; original investor-demo subset green. Work red rows only unless a reproducible bug appears.
- Latest completed provider: BlueSky cached public profile observations. Native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3 and matching SQLite/Neo4j replicas2/2 each. Two version1 saves/no pending intents; original cohort frozen.
- Websiteac0ca0c publishes BlueSky/ChainLink/EOSIO/Telos/Filecoin cards and pinned setup/C#/HTTP links;98client tests pass. Public HTML/scripts match exact git blobs. Browser interaction remains separate.
- Generic browser Mongo preparation copies identity/name/description/provenance, not all native metadata. Full payload mapping remains red in AFTER_DEMO_BACKLOG.md.

## Current phase: diagnose Nostr intermittent hosted read failure

- BlueSky hosted HTTP acceptance passes at parentf36dd0d56/Core499e4e0, settings6dfdbc6c SUCCESS: native3/2, Mongo5/4, reuse0new/2existing, foreign-private0/public3, SQLite/Neo4j replicas2/2 each. Two version1 saves/no pending intents; original cohort frozen. Cached public AppView only; generic copies retain identity/name/description/provenance, not all native metadata. Card published/byte-verified; browser unverified.
- Existing local gates:37source controls, actual CLI3/2, Core1/API2/localHTTP1/publisher9/prepared1/client1. No replays or new cohorts.

## Exact next actions

1. Preserve BlueSky original cohort/ACKs; no reapply/redeploy/resave. Websiteac0ca0c3cffea71aa9b99ca437d2ee46fa20d9c1 publishes BlueSky with pinned guidea10c6661c and C#f36dd0d56 links. All98client tests pass. Public HTML171143bytes/SHA256dfa7084ba40e8fe691eb2fbf7f9c1c4d9cda7dcdcbade32026f6f9d0fd7fceee and both scripts match exact git blobs. Browser interaction remains open.
2. Nostr code79b7be91/settings21ee8d23 SUCCESS at6ee3ebfcd/Core84a2151. Original owner native3/2 and Mongo5/4 pass; two normal private version1 saves ACKed, pending0. Independent read-only audit confirms matching Mongo/SQLite/Neo4j2records each with exact owner/version/metadata;0additional saves. Native read intermittently fails503/CQL_QUERY_FAILED during repeat preparation; foreign privacy/reuse/full hosted acceptance remain red. No replacement owner, source switch or resave; card task-only2ffca06/mainac0ca0c unchanged. Original progress, two save ACKs and partial replica observation retained under HostedQueryAcceptance-NostrOASIS. Never rerun --prepare/resume-unstarted-native, settings or any acknowledged save.
3. Next add sanitized operator failure diagnostics (exception type/WebSocket stage/HTTP status, no messages/URLs/tokens), reproduce the native failure and fix the actual cause. Then explicitly finish the original cohort through read-only continuation, preserving completed3/2+5/4/replica proofs. No fresh cohort/green hardening. See NOSTR_PUBLIC_SOURCE_STATUS.md.

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
