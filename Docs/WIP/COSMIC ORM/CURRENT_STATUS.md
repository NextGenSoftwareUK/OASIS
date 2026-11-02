# COSMIC ORM / CQL current status

Updated 2 November 2026. Goal active and incomplete. Priority: breadth-first live demos; deep LINQ parity remains paused.

## Verified scope

- 53 integrated query types: 3 canonical adapters,46bounded public/source profiles,4local PostgreSQL/Azure Cosmos/Oracle/AWS implementations.
- 49 scoped hosted functional HTTP proofs at recorded releases. Browser5verified/44open. These are not full ORM/native-write/reliability certifications.
- MongoDB/SQLite/Neo4j parity50/82rows each; original investor-demo subset green. Work red rows only unless a reproducible bug appears.
- Latest addition Matrix: native3/2, Mongo5/4, repeat0new/2existing, foreign-private0/public3, SQLite/Neo4j replicas2/2 each pass. Two version1 saves/zero pending intents; original cohort frozen.
- Matrix local source30tests, native qualifier4controls, actual HolonManager/LINQ CLI3/2, Core1/API3/localHTTP1/publisher9/prepared-presets1 pass. Frontend101tests pass.
- Website main/taskeda15be publishes Stacks, Matrix and prior providers;104client tests pass. Public HTML plus both scripts match exact git blobs. Browser interaction remains separate.
- Generic browser copies retain identity/name/description/provenance and the native query key, not full native payload. Full safe payload mapping remains red in AFTER_DEMO_BACKLOG.md.

## Current phase: Stacks hosted demo and public assets verified

- Matrix acceptance release was parent7b45ee85628bd6d1ceeaf769d4c1bcc44ccb465b/Coredd2c32c8925aaf53017f4fedc1d1f11d330a55e3; its original source/cohort/receipts remain frozen.
- Code jobd398b754-8d88-4837-a934-44e4cd9aad0d SUCCESS. Three settings applied exactly once/verified with no per-key deploys. Settings jobaaff57a2-a6ed-4e0e-939b-712420bd9d4e SUCCESS at the same parent SHA. Task branch restored after matching-release application.
- Source/owner/config/ACKs: LocalState/COSMIC/HostedMatrixPublicSourceCurrent. Release journals: HostedMatrixReleaseCurrent. Protected settings snapshots/intents/ACKs: HostedSourceRolloutCurrent/matrix-public-*.
- Original hosted cohort: LocalState/COSMIC/HostedQueryAcceptance-MatrixOASIS. Immutable cql-http-acceptance.acknowledged.json verifies IDs, source owner/keys, copies, provenance, versions, visibility, exact replicas, two operation ACKs and zero pending intents. Do not rerun/resave/replace the owner.
- Raw proofs: TestResults/HostedCqlIntegrationCurrent/MatrixOASIS-{native-http,client-http-flow,isolation-http}.json; hashes in MATRIX_PUBLIC_SOURCE_STATUS.md. Browser false.

## Exact next actions

1. Preserve Matrix original source/config/owner/copies/ACKs. No rerun/resave/reapply/redeploy just to reproduce green results. Public site ACK: LocalState/COSMIC/HostedMatrixReleaseCurrent/site-publication.acknowledged.json; raw observation TestResults/MatrixPublicSourceCurrent/public-assets-observation-1.json.
2. Stacks complete within scoped HTTP demo: source43/CLI3/2/Core1/API3/localHTTP1/publisher9/prepared1/client104/continuation12 pass. Code3768c2050/Core6aa17e79e, code job8f13e4b3 and settings jobd6bb89ee SUCCESS. Original native3/2, Mongo5/4, reuse0new/2existing, privacy0/3 and exact SQLite/Neo4j2/2 pass; two original version1 saves/pending0/no additional saves. Changing-observation reuse bug fixed; initial failure retained. Immutable source/cohort/acceptance ACKs preserved. Websiteeda15be with pinned guide23e70d10 and C#3768c2050 published; public HTML178107bytes/SHA256ba219e8e890914e0c811cd15de9e435561389a7388b516fe2e93d1de423b04ea and both scripts match exact git blobs. Next select the next unimplemented provider scope from the full report; preserve completed cohorts. See STACKS_PUBLIC_SOURCE_STATUS.md.
3. Farcaster stays unadmitted: public Pinata Hub failed before HTTP; separate DNS probe confirms name error. Original intent/result/DNS evidence and4offline parser controls preserved in FARCASTER_PUBLIC_SOURCE_STATUS.md. Do not retry or switch to paid Neynar silently.
4. Update CURRENT_STATUS/full provider report/ledger at the next milestone. Keep hosted49/browser5/open44; public asset proof is not browser interaction.

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
