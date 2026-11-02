# COSMIC ORM / CQL current status

Updated 1 November 2026. Goal active and incomplete. Priority: breadth-first live demos; deep LINQ parity remains paused.

## Verified scope

- 48 integrated query types: 3 canonical adapters, 41 bounded public/source profiles, 4 local PostgreSQL/Azure Cosmos/Oracle/AWS implementations.
- 43 providers have scoped hosted HTTP proof at their recorded releases. Browser proof: 5; 38 browser gates remain open. These are not full ORM/native-write certifications.
- MongoDB/SQLite/Neo4j parity: 50/82 checklist rows each; original investor-demo subset green. Work red rows only unless a reproducible bug appears.
- Latest completed hosted provider: Telos. Native 3/2, Mongo pairing 5/4, repeat 0 new/2 existing, foreign-private 0/public 3, SQLite/Neo4j replicas 2/2; two version1 saves, no pending intents. Completion ACK freezes the original cohort.
- Telos parent7c498e0f/Core80b879b; code91bf543d and settingsad7aab39 succeeded. Filecoin remains frozen at its earlier recorded release. Website4ff3661 publishes Telos/Filecoin cards/guides;95client tests pass; public HTML/scripts match exact git blobs. Browser interaction remains separate.
- Generic browser Mongo preparation copies identity/name/description/provenance, not all native metadata. Full payload mapping remains red in AFTER_DEMO_BACKLOG.md.

## Current phase: EOSIO hosted rollout

- EOSIO native source and actual manager/LINQ/CLI3/2 pass, with28controlled tests. Core1/API2/localHTTP1/publisher9/prepared-planner1/client1 pass. Local pairing5/4 and foreign-private0/public3 verified; exact owned Mongo fixture cleaned and process stopped.
- Parent Development `4d3e0b7cd148bed7ebdf0400ca371867873932c0` / Core `86e38a37ee860785a8273ade3308f6fc1775294d` published/ref-verified; manifest validates.
- Code job `68db832f-1742-423d-9362-febcf9620466` is BUILDING. Latest12-line build sample1917B has a publish marker/no compiler error marker; that is not deployment success. No EOSIO settings applied, hosted sessions or saves yet.
- Prepared three-key baseline/candidate and four presets remain retained. No replay of completed native/local cohorts. Frontend test-only branch9cca7a9 is pushed; card not published.

## Exact next actions

1. Telos card4ff3661/pinned links/95client tests/public HTML/scripts are verified. Original cohort is frozen; no native/local/hosted replay or settings reapply/redeploy.
2. Observe that exact code job. Only after SUCCESS check out exact code release4d3e0b7cd for guarded settings application (no detached commits), apply reviewed settings once, issue one journalled settings deployment, restore task branch and verify hosted acceptance/card. See EOSIO_PUBLIC_SOURCE_STATUS.md and LocalState/COSMIC/HostedEosioReleaseCurrent. No EOSIO hosted/browser claim yet.
3. Implement only the new provider's red scope after qualification: source/controlled tests/native manager+C#3/2, API/query-only admission/Mongo/privacy, actual prepared presets, exact hosted rollout/acceptance/card. Keep green scopes frozen absent a bug.

## Durable state and rules

- Work only `C:/Source/OASIS-cosmic` on `work/cosmic-cql-ton-public`; Core branch `work/cosmic-cql-google-public`. Frontend `C:/Source/Web4Site/work/cosmic-cql`. Never edit/build original OASIS checkout.
- Telos source/ACKs: `LocalState/COSMIC/HostedTelosPublicSourceCurrent`; cleaned local fixture journals: `LocalState/COSMIC/TelosMongoHttpCurrent`; code publication/job ACKs: `LocalState/COSMIC/HostedTelosReleaseCurrent`.
- Protected settings snapshots/intents/ACKs: `LocalState/COSMIC/HostedSourceRolloutCurrent/telos-public-*`. Never print their contents or overwrite/replay journals.
- Raw logs/results: worktree `TestResults/` only. Durable identities/config/receipts stay in LocalState. Ledger: ENVIRONMENT_AND_CLEANUP_LEDGER.md (mixed encoding; append UTF8 bytes). Unknown root `logs` preserved.
- Build under the existing provider lease with `/m:2 /nr:false /p:UseSharedCompilation=false`. Reuse SDK10.0.401/Node24.19.0/Python3.12/Mongo8.3.11; no new environment planned. No production/master promotion.
- Completed hosted/native/local cohorts are frozen; never resave acknowledged copies or recreate catalogue owners. Existing native failure diagnostics for TRON/Rootstock are preserved, not blindly retried.

## Deferred and remaining full scope

- Hosted Azure Cosmos, Oracle and PostgreSQL explicitly deferred by David. No setup re-ask or paid account/resource. Public Azure Blob and AWS S3 are already hosted; local/emulator proof is not owned hosted-database proof.
- Browser/OS control remains closed; no automation/repeat owner request. Solana and other browser gates remain open.
- Full tracking/save ownership, atomic concurrency, children/depth/query parameters, rollback/history/deltas, migration/placement and broader HyperDrive operational acceptance remain on the roadmap. No overall completion percentage follows from demos.
- Deep entity-selected terminals, unrestricted POCO mapping and migrations tooling stay deferred behind demo breadth. V2 implementation only; legacy gaps documentation-only.

[Telos proof](TELOS_PUBLIC_SOURCE_STATUS.md) | [Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Manual](CQL_MANUAL.md) | [Architecture](CQL_ARCHITECTURE.md) | [Parity](CQL_PARITY_CHECKLIST.md) | [Backlog](AFTER_DEMO_BACKLOG.md) | [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
