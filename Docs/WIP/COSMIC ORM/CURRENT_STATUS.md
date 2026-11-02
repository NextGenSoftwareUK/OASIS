# COSMIC ORM / CQL current status

Updated 1 November 2026. Goal active and incomplete. Priority: breadth-first live demos; deep LINQ parity remains paused.

## Verified scope

- 47 integrated query types: 3 canonical adapters, 40 bounded public/source profiles, 4 local PostgreSQL/Azure Cosmos/Oracle/AWS implementations.
- 42 providers have scoped hosted HTTP proof at their recorded releases. Browser proof: 5; 37 browser gates remain open. These are not full ORM/native-write certifications.
- MongoDB/SQLite/Neo4j parity: 50/82 checklist rows each; original investor-demo subset green. Work red rows only unless a reproducible bug appears.
- Latest completed hosted provider: Filecoin. Native 3/2, Mongo pairing 5/4, repeat 0 new/2 existing, foreign-private 0/public 3, SQLite/Neo4j replicas 2/2; two version1 saves, no pending intents. Completion ACK freezes the original cohort.
- Filecoin parent fcc4f735d/Core b386875; code d1fe285f and settings 082c1f0d succeeded. Website 9413da3 publishes its card/guides; 94 client tests pass; public HTML/scripts match git blobs. Browser interaction remains separate.
- Generic browser Mongo preparation copies identity/name/description/provenance, not all native metadata. Full payload mapping remains red in AFTER_DEMO_BACKLOG.md.

## Current phase: Telos activation

- Telos native manager/C#/actual CLI: 3/2; 18 new controlled tests pass. Shared reader algorithm unchanged. Original catalogue and native ACK retained; no replay.
- Core admission1/API controls2/local HTTP1/publisher9/client1/actual prepared-planner1 pass. Native 3/2, Mongo5/4, privacy0/3, two SDK saves and nine query receipts. Exact marked fixture cleaned; Mongo stopped/27029 absent.
- Three reviewed settings applied once; whole candidate equality/all three per-key ACK hashes verified. Free four-preset profile; DNA/unrelated values unchanged.
- Parent Development `7c498e0f2e8a97207fb8d511d35cea521e7e240c` and Core Development `80b879b0ef289ccfff3b7efefa5a9ff26e6b4135` pushed/remote-ref verified. Manifest/gitlinks validate; no incoming executable changes.
- Code job `91bf543d-030d-4efe-b2db-31c57aeae139` succeeded. Single journalled settings job `ad7aab39-5e83-47f8-b7e9-e03920cb3d2f` is BUILDING at the same code SHA. Never reapply settings/redeploy. Telos is not yet counted as hosted.

## Exact next actions

1. Observe the same settings job:
   `python Scripts/Sandbox/Observe-HostedDeployment.py ad7aab39-5e83-47f8-b7e9-e03920cb3d2f --expected-commit 7c498e0f2e8a97207fb8d511d35cea521e7e240c --output TestResults/TelosPublicSourceCurrent/settings-deployment.json`
2. After SUCCESS, retain settings-deployment ACK and run first `python Scripts/Sandbox/Verify-HostedQuerySource.py TelosOASIS --prepare` with existing pinned Node on PATH. At most two journalled visitors/two normal private saves; inspect native3/2, paired5/4, reuse0/2, privacy0/3, replicas2/2, two version1 ACKs/no pending. Write immutable completion ACK only after proof.
3. Publish the Telos card/pinned guide/C# links, run client checks and compare public bytes. HTTP/asset matching is not browser interaction. Then select the next red shipping-provider scope; Chainlink feeds are researched only, not qualified or implemented.

Code application recovery: exact-release detached checkout was used for guarded3settings, then existing task branch restored. Code-deployment ACK, all per-key ACKs and protected settings-rollout intent/outcome exist. Do not detach/reapply or repeat either publication/rollout intent. Observe only the returned settings job above; a timeout is not failure. No new worktree/guard bypass/restart.

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
