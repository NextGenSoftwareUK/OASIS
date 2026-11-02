# COSMIC ORM / CQL current status

Updated 1 November 2026. Goal active; full roadmap incomplete. Priority: breadth-first live demos. Deep LINQ parity remains paused.

## Verified scope

- 45 integrated query types:3 canonical,38 bounded public/source profiles,4 local PostgreSQL/Azure Cosmos/Oracle/AWS implementations.
- 40 providers have scoped hosted HTTP proof and deployed query tariffs. Browser proof:5;35browser gates open. Read-only discovery now reports40/40providers ready at the current release; this is readiness, not a fresh replay of all provider data proofs. Current provider-by-provider authority: PROVIDER_CQL_AND_SANDBOX_STATUS.md.
- MongoDB/SQLite/Neo4j parity checklist50/82each; original investor-demo subset green. Work red rows only unless a reproducible bug appears.
- Latest completed hosted cohort: AzureStorageOASIS public Blob properties, native3/2, Mongo5/4, reuse0new/2existing, foreign-private0/public3, exact SQLite/Neo4j replicas2/2. Two saves at version1; no pending intents. Completion ACK exists; never replay/resave.
- Azure native/local:24source controls,43API controls,6publisher controls, SDK/manager/actual console3/2 and local HTTP/Mongo/privacy pass. No account, source write or object-body download. This is not hosted Cosmos DB.
- Website9694621 publishes Azure/TON cards and local-guide links; public HTML and both scripts match exact git blobs. All 92 client tests pass. Browser interaction is not inferred.
- PostgreSQL retained content:16controls,17native cases, local paired SDK/HTTP2/4/4/2 and identity5/4 pass. Original JSON/xmin/source metadata unchanged; owned Mongo fixture cleaned/services stopped. Hosted PostgreSQL explicitly deferred by David; do not re-ask.
- AWSOASIS retained DynamoDB Local:13controls, native3/2 and Mongo SDK/HTTP5/4 pass. Source preserved; runtime3.3.2 binding repaired. Owned hosted AWS and separate DynamoDBOASIS alias native acceptance remain open. Public S3 is already hosted.

## Exact deployed release and immutable evidence

- Parent Developmentbccef53d6dbe0ec75028220b34eb0d3f2b0f4db8/Core Developmentd133cc20c29441158da0e590d7f3c85b351dd826; remote refs verified. ONODE dependency308a7860 preserved; manifest/gitlinks validate.
- Code jobe6a98438-bf39-444e-89eb-5367322525bc and single Azure settings job8e382a80-86ec-4d1d-8709-ca459f9e9781 both SUCCESS atbccef53d6. Exactly3settings applied once; per-key ACKs/full candidate matched, DNA/unrelated settings unchanged. No reapply/redeploy.
- Durable release ACKs LocalState/COSMIC/HostedAzureBlobReleaseCurrent; protected before/candidate/intent/ACK/outcome files LocalState/COSMIC/HostedSourceRolloutCurrent/azure-blob-public-*.
- Azure source/qualification ACK LocalState/COSMIC/HostedAzureBlobPublicSourceCurrent; original source SHA256cdd1607e68e647fc2c3d932abd151658479eaa2aa0e2b1bbfb730c7fe2be1124. Original cohort LocalState/COSMIC/HostedQueryAcceptance-AzureStorageOASIS has completion ACK.
- TON cohort/settings/card proof remains preserved in TON_PUBLIC_SOURCE_STATUS.md. All completed cohorts are frozen; never replay cleaned local fixtures or resave acknowledged copies.

## Next work

1. Fantom native/manager/actual CLI3/2 now pass;278controls include21Fantom and preserved constructor contract. Explicit immutable latest-canonical-at-read, not finalized. WEB4/local HTTP now pass40controls and native3/2/Mongo5/4/privacy0/3; exact local cleanup done.8publisher controls pass,3-key/free4preset candidate prepared. Merged OLAND quote updates preserved,46combined controls pass. Parent Development e13244820/Core01b7545 published/remote-verified; code job7b8c95d9-b9dd-47c1-9ea9-5151f40be81b BUILDING, authoritative last observation retained. Exact next: observe that same code job to SUCCESS, then apply prepared3settings once under exact release guard, single settings rollout and hosted proof/card. No Fantom live admission yet; see FANTOM_PUBLIC_SOURCE_STATUS.md. Native-qualified ACK freezes source owner/addresses.
2. PostgreSQL hosting explicitly deferred by David ("Leave hosted PostgreSQL pending"); no owner setup, re-ask or new paid resource. Azure Cosmos and Oracle hosted access also remain deferred.
3. TRON remains unqualified after native429/diagnostic403; Rootstock finalized-header endpoint rejected400; separate explicit canonical-at-read qualification now rejects the first EIP1898 balance with RPC-32603. No Rootstock source/catalogue or admission created. Preserve both failures; do not blindly rerun them.
4. Keep full ORM tracking/concurrency/children/history/deltas/migration/placement acceptance and wider HyperDrive recovery work on the roadmap. Deep LINQ, POCO mapping and migrations tooling remain deferred behind demo breadth. V2 implementation only; legacy gaps documentation only.

## Guarded release handoff

- Observe only code job7b8c95d9-b9dd-47c1-9ea9-5151f40be81b at e13244820ddcffc27b94326e70ccfef52fb044ab. It is live/building, not failed or blocked. Never restart it or repeat either publication intent.
- Once exact/latest release is SUCCESS, the apply guard requires HEAD to equal that immutable code SHA. Task branch may now include later docs-only commits. With tracked work clean, temporarily `git switch --detach e13244820ddcffc27b94326e70ccfef52fb044ab` in this assigned worktree, set COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT to the same SHA, then run `python Scripts/Sandbox/Publish-HostedFantomPublicConfiguration.py apply`. No commits while detached; restore work/cosmic-cql-ton-public after guarded mutations. No new worktree or guard bypass.
- Full candidate equality/per-key ACKs must precede exactly one journalled settings redeploy. Then hosted native3/2/Mongo5/4/reuse/privacy/replicas, immutable completion ACK and public card/asset proof. Counts stay45integrated/40hosted until that gate passes. Hosted PostgreSQL/Azure Cosmos/Oracle remain deferred.

## Commands and recovery rules

- Work only C:/Source/OASIS-cosmic, branch work/cosmic-cql-ton-public; Core work/cosmic-cql-google-public. Frontend C:/Source/Web4Site/work/cosmic-cql. Never edit/build original C:/Source/OASIS.
- Raw reports TestResults/; durable sources/config/identities/intents/ACKs LocalState/COSMIC/. Resource ownership/cleanup: ENVIRONMENT_AND_CLEANUP_LEDGER.md. Unknown root logs preserved. No new runtime/account/environment for Azure.
- Browser/OS control remains closed; no automation resumed or repeat owner request. Solana and other browser gates remain open.
- Build under Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 with /m:2 /nr:false /p:UseSharedCompilation=false. Reuse cached runtimes. No production/master promotion.
- Observe a known deployment only: python Scripts/Sandbox/Observe-HostedDeployment.py ID --expected-commit SHA --output TestResults/AzurePublicSourceCurrent/observation.json. Never restart on an observation timeout.
- Azure local demo/setup: Docs/Devs/AZURE_PUBLIC_BLOB_CQL_GUIDE.md; documented actual console command passes3/2. The completed Run-CosmicAzureBlobHttpNative.ps1 fixture is immutable and refuses replay.

[Azure proof](AZURE_PUBLIC_BLOB_STATUS.md) | [Full provider report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) | [Manual](CQL_MANUAL.md) | [Architecture](CQL_ARCHITECTURE.md) | [Parity](CQL_PARITY_CHECKLIST.md) | [Deferred work](AFTER_DEMO_BACKLOG.md) | [David actions](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
