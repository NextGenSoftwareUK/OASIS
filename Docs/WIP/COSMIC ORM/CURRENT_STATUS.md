# COSMIC ORM / CQL current status

Updated 2 November 2026. Goal active and incomplete. Priority: breadth-first live demos; deep LINQ parity remains paused.

## Verified scope

- 52 integrated query types: 3 canonical adapters,45bounded public/source profiles,4local PostgreSQL/Azure Cosmos/Oracle/AWS implementations.
- 48 scoped hosted functional HTTP proofs at recorded releases. Browser5verified/43open. These are not full ORM/native-write/reliability certifications.
- MongoDB/SQLite/Neo4j parity50/82rows each; original investor-demo subset green. Work red rows only unless a reproducible bug appears.
- Latest addition Matrix: native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3, SQLite/Neo4j replicas2/2 each pass. Two version1 saves/zero pending intents; original cohort frozen.
- Matrix local source30tests, native qualifier4controls, actual HolonManager/LINQ CLI3/2, Core1/API3/localHTTP1/publisher9/prepared-presets1 pass. Frontend101tests pass.
- Website main/taskbd173e546a095c0ae6f9a23d20db69bb6de76bda publishes Matrix/Nostr and setup/C#/HTTP links. Public HTML175616bytes/SHA256b18edd89557ae17dd261d8fded04b12f7eec9ca6da2a8cc52ab7d73c2a7383a8 and both scripts match exact git blobs. Browser interaction remains separate.
- Generic browser copies retain identity/name/description/provenance and the native query key, not full native payload. Full safe payload mapping remains red in AFTER_DEMO_BACKLOG.md.

## Current phase: Stacks local CQL verified; HTTP/Mongo integration next

- Parent Development7b45ee85628bd6d1ceeaf769d4c1bcc44ccb465b/Coredd2c32c8925aaf53017f4fedc1d1f11d330a55e3 refs verified; manifest/gitlinks pass.
- Code jobd398b754-8d88-4837-a934-44e4cd9aad0d SUCCESS. Three settings applied exactly once/verified with no per-key deploys. Settings jobaaff57a2-a6ed-4e0e-939b-712420bd9d4e SUCCESS at the same parent SHA. Task branch restored after matching-release application.
- Source/owner/config/ACKs: LocalState/COSMIC/HostedMatrixPublicSourceCurrent. Release journals: HostedMatrixReleaseCurrent. Protected settings snapshots/intents/ACKs: HostedSourceRolloutCurrent/matrix-public-*.
- Original hosted cohort: LocalState/COSMIC/HostedQueryAcceptance-MatrixOASIS. Immutable cql-http-acceptance.acknowledged.json verifies IDs, source owner/keys, copies, provenance, versions, visibility, exact replicas, two operation ACKs and zero pending intents. Do not rerun/resave/replace the owner.
- Raw proofs: TestResults/HostedCqlIntegrationCurrent/MatrixOASIS-{native-http,client-http-flow,isolation-http}.json; hashes in MATRIX_PUBLIC_SOURCE_STATUS.md. Browser false.

## Exact next actions

1. Preserve Matrix original source/config/owner/copies/ACKs. No rerun/resave/reapply/redeploy just to reproduce green results. Public site ACK: LocalState/COSMIC/HostedMatrixReleaseCurrent/site-publication.acknowledged.json; raw observation TestResults/MatrixPublicSourceCurrent/public-assets-observation-1.json.
2. StacksPublicPrincipalMetadataSource now passes43scoped source tests and actual HolonManager/LINQ CLI3full/2filtered;6native-qualifier controls preserved. Existing resolved BouncyCastle2.7.0 reused with explicit assembly alias; no crypto substitute or new runtime. Source/owner/intent/ACK: LocalState/COSMIC/HostedStacksPublicSourceCurrent (sourceSHAaa5fe1f4934e1ed9670aee3064527a7becccee2332c3d416615288a93de634d1). Exact next: add query-only Core tariff/API compiler/host activation and4native/Mongo presets, then local HTTP native3/2+Mongo5/4/privacy before hosted publication. See STACKS_PUBLIC_SOURCE_STATUS.md. Do not rerun native/console qualification or replace owner. Story/DATA Trace remains denied/unadmitted; no bypass or legacy fixes.
3. Farcaster stays unadmitted: public Pinata Hub failed before HTTP; separate DNS probe confirms name error. Original intent/result/DNS evidence and4offline parser controls preserved in FARCASTER_PUBLIC_SOURCE_STATUS.md. Do not retry or switch to paid Neynar silently.
4. Update CURRENT_STATUS/full provider report/ledger at the next milestone. Keep hosted48/browser5/open43; public asset proof is not browser interaction.

## Other preserved checkpoints and open issues

- BlueSky/ChainLink/EOSIO/Telos/Filecoin original sources/cohorts/ACKs are frozen. See dedicated *_PUBLIC_SOURCE_STATUS.md reports; no reapply/redeploy/resave merely for another green run.
- Nostr functional HTTP/card/public bytes pass, but earlier intermittent query503cause remains unresolved despite8passing diagnostic reads. Deployed safe diagnostics are ready; capture recurrence, no speculative fix or green hardening.
- Rootstock/TRON failed native qualifications remain preserved/unadmitted. Farcaster qualifier4offline tests are parser controls, not native/CQL proof.

## Durable rules and remaining full scope

- Work/build/test only C:/Source/OASIS-cosmic on work/cosmic-cql-ton-public; Core work/cosmic-cql-google-public. Frontend C:/Source/Web4Site/work/cosmic-cql. Never edit/build original OASIS checkout.
- Raw logs/results belong in worktree TestResults; durable identities/config/receipts in LocalState/COSMIC. Ledger ENVIRONMENT_AND_CLEANUP_LEDGER.md is mixed encoding: append UTF8 bytes only. Preserve unknown root logs.
- Reuse SDK10.0.401/Node24.19.0/Python3.12/Mongo8.3.11 and existing environments. Build under provider lease with /m:2 /nr:false /p:UseSharedCompilation=false. No production/master promotion or paid hosting.
- Hosted Azure Cosmos, Oracle and PostgreSQL explicitly deferred by David; no setup re-ask or paid account/resource. Public Azure Blob/AWS S3 are already hosted; local/emulator proof is not owned hosted-database proof.
- Browser/OS control remains closed; no repeat owner request. Solana and other browser gates remain open.
- Full tracking/save ownership, atomic concurrency, children/depth/query parameters, rollback/history/deltas, migration/placement and broader HyperDrive operational acceptance remain on the roadmap. No overall completion percentage follows from demos.
- Deep entity-selected terminals, unrestricted POCO mapping and migrations tooling stay deferred behind demo breadth. V2 implementation only; legacy gaps documentation-only.

[Matrix proof](MATRIX_PUBLIC_SOURCE_STATUS.md) | [Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Manual](CQL_MANUAL.md) | [Architecture](CQL_ARCHITECTURE.md) | [Parity](CQL_PARITY_CHECKLIST.md) | [Backlog](AFTER_DEMO_BACKLOG.md) | [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
