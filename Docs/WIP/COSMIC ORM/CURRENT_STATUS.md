# COSMIC ORM / CQL current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demos. Deep LINQ parity remains paused.

## Verified scope

- 45 integrated query types:3 canonical,38 bounded public/source profiles,4 local PostgreSQL/Azure Cosmos/Oracle/AWS implementations.
- 41 providers have scoped hosted HTTP proof and deployed query tariffs. Browser proof:5;36browser gates open. Prior release discovery reported40/40ready; Fantom now has its own additional hosted proof. Neither observation is a fresh replay of all provider data proofs. Current provider-by-provider authority: PROVIDER_CQL_AND_SANDBOX_STATUS.md.
- MongoDB/SQLite/Neo4j parity checklist50/82each; original investor-demo subset green. Work red rows only unless a reproducible bug appears.
- Earlier completed hosted cohort: AzureStorageOASIS public Blob properties, native3/2, Mongo5/4, reuse0new/2existing, foreign-private0/public3, exact SQLite/Neo4j replicas2/2. Two saves at version1; no pending intents. Completion ACK exists; never replay/resave.
- Azure native/local:24source controls,43API controls,6publisher controls, SDK/manager/actual console3/2 and local HTTP/Mongo/privacy pass. No account, source write or object-body download. This is not hosted Cosmos DB.
- Websiteeef0769 publishes Fantom/Azure/TON cards and local-guide links; public HTML and both scripts match exact git blobs. All93client tests pass. Browser interaction is not inferred.
- PostgreSQL retained content:16controls,17native cases, local paired SDK/HTTP2/4/4/2 and identity5/4 pass. Original JSON/xmin/source metadata unchanged; owned Mongo fixture cleaned/services stopped. Hosted PostgreSQL explicitly deferred by David; do not re-ask.
- AWSOASIS retained DynamoDB Local:13controls, native3/2 and Mongo SDK/HTTP5/4 pass. Source preserved; runtime3.3.2 binding repaired. Owned hosted AWS and separate DynamoDBOASIS alias native acceptance remain open. Public S3 is already hosted.

## Prior Azure release and immutable evidence

- Parent Developmentbccef53d6dbe0ec75028220b34eb0d3f2b0f4db8/Core Developmentd133cc20c29441158da0e590d7f3c85b351dd826; remote refs verified. ONODE dependency308a7860 preserved; manifest/gitlinks validate.
- Code jobe6a98438-bf39-444e-89eb-5367322525bc and single Azure settings job8e382a80-86ec-4d1d-8709-ca459f9e9781 both SUCCESS atbccef53d6. Exactly3settings applied once; per-key ACKs/full candidate matched, DNA/unrelated settings unchanged. No reapply/redeploy.
- Durable release ACKs LocalState/COSMIC/HostedAzureBlobReleaseCurrent; protected before/candidate/intent/ACK/outcome files LocalState/COSMIC/HostedSourceRolloutCurrent/azure-blob-public-*.
- Azure source/qualification ACK LocalState/COSMIC/HostedAzureBlobPublicSourceCurrent; original source SHA256cdd1607e68e647fc2c3d932abd151658479eaa2aa0e2b1bbfb730c7fe2be1124. Original cohort LocalState/COSMIC/HostedQueryAcceptance-AzureStorageOASIS has completion ACK.
- TON cohort/settings/card proof remains preserved in TON_PUBLIC_SOURCE_STATUS.md. All completed cohorts are frozen; never replay cleaned local fixtures or resave acknowledged copies.

## Next work

1. Fantom hosted acceptance complete at e13244820/Core01b7545, settings2177dd3d SUCCESS. Native3/2, paired5/4, repeat0new/2existing, private0/public3, SQLite/Neo4j replicas2/2; two saves/version1/no pending. Completion ACK freezes original cohort. Cardeef0769/public byte matching and93client tests complete. Exact next: implement Filecoin native genesis-CID identity over the shared reader with controlled tests, then native manager/C#3/2. Protocol qualification passed genesisCID/finalized/hash-bound reads; original archive-genesis failure retained. No Filecoin source/catalogue/admission yet. See FILECOIN_PUBLIC_SOURCE_STATUS.md; preserve deferred hosting/browser decisions. Do not replay acceptance. See FANTOM_PUBLIC_SOURCE_STATUS.md.
2. PostgreSQL hosting explicitly deferred by David ("Leave hosted PostgreSQL pending"); no owner setup, re-ask or new paid resource. Azure Cosmos and Oracle hosted access also remain deferred.
3. TRON remains unqualified after native429/diagnostic403; Rootstock finalized-header endpoint rejected400; separate explicit canonical-at-read qualification now rejects the first EIP1898 balance with RPC-32603. No Rootstock source/catalogue or admission created. Preserve both failures; do not blindly rerun them.
4. Keep full ORM tracking/concurrency/children/history/deltas/migration/placement acceptance and wider HyperDrive recovery work on the roadmap. Deep LINQ, POCO mapping and migrations tooling remain deferred behind demo breadth. V2 implementation only; legacy gaps documentation only.

## Guarded release handoff

- Code7b8c95d9 and settings2177dd3d succeeded at e13244820; three reviewed settings applied once, candidate equality/per-key ACKs verified. No reapply/redeploy. Existing task branch restored after exact-release guard.
- Original Fantom hosted cohort has immutable completion ACK, two acknowledged version1 saves and no pending intents. Preserve identities/copies and never rerun it. Source SHA25686b7ba36bc665a74cd77fa0469c6276156c95967a480e9eadbb1aa09389a5550.
- Websiteeef0769 is published/ref-verified and public bytes match;93tests pass. Fantom guide15fc93ccb/C#e13244820 are pinned. Counts45integrated/41hosted/browser5/open36. Hosting deferrals unchanged.

## Commands and recovery rules

- Work only C:/Source/OASIS-cosmic, branch work/cosmic-cql-ton-public; Core work/cosmic-cql-google-public. Frontend C:/Source/Web4Site/work/cosmic-cql. Never edit/build original C:/Source/OASIS.
- Raw reports TestResults/; durable sources/config/identities/intents/ACKs LocalState/COSMIC/. Resource ownership/cleanup: ENVIRONMENT_AND_CLEANUP_LEDGER.md. Unknown root logs preserved. No new runtime/account/environment for Azure.
- Browser/OS control remains closed; no automation resumed or repeat owner request. Solana and other browser gates remain open.
- Build under Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 with /m:2 /nr:false /p:UseSharedCompilation=false. Reuse cached runtimes. No production/master promotion.
- Observe a known deployment only: python Scripts/Sandbox/Observe-HostedDeployment.py ID --expected-commit SHA --output TestResults/AzurePublicSourceCurrent/observation.json. Never restart on an observation timeout.
- Azure local demo/setup: Docs/Devs/AZURE_PUBLIC_BLOB_CQL_GUIDE.md; documented actual console command passes3/2. The completed Run-CosmicAzureBlobHttpNative.ps1 fixture is immutable and refuses replay.

[Azure proof](AZURE_PUBLIC_BLOB_STATUS.md) | [Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Manual](CQL_MANUAL.md) | [Architecture](CQL_ARCHITECTURE.md) | [Parity](CQL_PARITY_CHECKLIST.md) | [Deferred work](AFTER_DEMO_BACKLOG.md) | [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
