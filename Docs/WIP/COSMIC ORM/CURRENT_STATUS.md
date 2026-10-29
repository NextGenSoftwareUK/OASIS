# Current COSMIC ORM / CQL status

Updated 29 October 2026. Full programme remains incomplete. Read this first after compaction.

## Current phase and exact next step

MVP breadth first across **all providers**, then deepen. Work only on red rows;
no green/federation-cursor hardening without a reproduced bug. Legacy HyperDrive
issues are documentation-only. Hosted Azure Cosmos stays pending by user instruction.

**Next: Redis native dataset admission**, then3/2/missing/restart and Mongo/browser.
Redis8 controls pass; encrypted backend reused, unadmitted aliases cannot inherit
Redis query capabilities. Existing stopped local container/volume/keys are available;
no new service/download. [Redis exact next](REDIS_CQL_MVP.md).
Hedera controls/public runtime audit pass; existing routed-read funding guard is
not met by indexed testnet balance. No paid queries/key export performed; retain
historical admission guards. [Hedera gate](HEDERA_CQL_MVP.md). Sui full demo verified.
Aptos native/restart/paired/browser5/4 is verified; main-hub enrollment remains
review-gated. [Direct commands/gate](APTOS_CQL_MVP.md). All owned nodes are stopped.
LocalFile native/restart/SDK/HTTP/browser5/4 is verified and stopped/cleaned.
[Proof](LOCALFILE_CQL_MVP.md). TRON original source/fence recovery and S3 local launch
policy gate remain open; both stay red. [TRON](TRON_CQL_MVP.md), [S3](AWS_S3_CQL_MVP.md).

## Scoped demo proofs

| Provider scope | Current proof |
|---|---|
| Mongo/SQLite/Neo4j | Initial investor rows green; full parity remains open. [Checklist](CQL_PARITY_CHECKLIST.md). |
| Sui + Mongo | Dedicated encrypted native/restart/paired5/4, original Storage preserved. [Guide](SUI_CQL_MVP.md). |
| LocalFile + Mongo | Native files/restart/paired5/4; plaintext scope. [Guide](LOCALFILE_CQL_MVP.md). |
| Oracle + Mongo | Native/SDK/HTTP/browser5/4. [Scope](CQL_ORACLE_DEMO_ADMISSION.md). |
| Azure Cosmos + Mongo | Emulator5/4 only; hosted pending, no free subscription. [Gate](CQL_HOSTED_AZURE_FREE_ADMISSION.md). |
| IPFS/SOLID/ActivityPub + Mongo | Native3/2 and paired 5/4; arbitrary content predicates open. [Scope](CQL_PROTOCOL_MULTI_RECORD.md). |
| Solana + Mongo | Retained validator/index and SDK/HTTP/browser. Preserve missing-history gate. [Proof](CQL_MONGO_SOLANA_NATIVE_VERIFICATION.md). |
| AWS + Mongo | DynamoDB Local native3/2/paired 5/4. [Guide](AWS_CQL_MVP.md). |
| Ethereum/Polygon/Arbitrum/Base + Mongo | Local EVM31338 native3/2/paired 5/4. No public-chain/finality claim. [Ethereum](ETHEREUM_CQL_MVP.md), [Polygon](POLYGON_CQL_MVP.md), [Arbitrum](ARBITRUM_CQL_MVP.md), [Base](BASE_CQL_MVP.md). |
| Google Firestore + Mongo | Emulator native/restart/paired 5/4. [Guide](FIRESTORE_CQL_MVP.md). |
| Cosmos blockchain + Mongo | Native encrypted contract/restart/paired 5/4. Original four signed writes retained. [Guide](COSMOS_BLOCKCHAIN_CQL_MVP.md). |
| Polkadot + Mongo | Native encrypted contract/restart/paired 5/4. Original three signed writes retained. [Guide](POLKADOT_CQL_MVP.md). |
| NEAR + Mongo | Original encrypted records/restart/paired 5/4; historical write gate remains open. [Guide](NEAR_CQL_MVP.md). |
| Azure Blob + Mongo | Azurite native/restart/paired5/4; no hosted Azure claim. [Guide](AZURE_BLOB_CQL_MVP.md). |
| Avalanche + Mongo | Chain-identity/factory root fix; controls16/family37, native/restart/paired 5/4. Original empty-state continuation preserved contract/IDs. [Guide](AVALANCHE_CQL_MVP.md). |

[Inventory](CQL_ALL_PROVIDER_MVP_INVENTORY.md): 226 concrete identities, 22 scoped MVP
proof links, 33 active central cases, 11 extra project names requiring classification.
Green means only linked scope; it does not establish full provider/production parity.

## Deferred correctness and parity gates

- Save ownership/change tracking, concurrency and committed deltas have scoped proofs;
  see the strict [parity checklist](CQL_PARITY_CHECKLIST.md) and implementation roadmap.
- Effective avatar routing, failure isolation, native strategy reads, durable route
  descriptors/outbox and selected delivery failures have retained proofs; no universal
  uptime claim. [Routing](V2_ROUTING_ACCEPTANCE.md), [routes](DURABLE_ROUTE_DESCRIPTORS.md).
- SQLite/Mongo matching-lineage adoption and selected coordinator/crash scopes pass.
  Neo adoption, empty-target transfer, historical admission, source/outbox CAS,
  durable final decision/cutover and broader crash recovery remain red. No global
  cutover enabled. [Detailed gates](V2_LINEAGE_MIGRATION.md).
- Deep LINQ All/element/numeric terminals, POCO mapping, migrations and broader
  child/history/provider parity remain deferred for breadth. Keep original acceptance.

## Workspace, integration and resource rules

- Work/build/test only C:/Source/OASIS-cosmic; parent/Core branch work/cosmic-cql.
- Development87df54630 integrated; merged Corec2b1627 pushed/remote-verified. Core4528,
  Shared1599, WEB4schema/admission5 and WEB4 build pass. [Proof](DEVELOPMENT_SYNC_2026-10-29.md).
- Later task work is pushed to the task branch. Outgoing Development deployment is a
  separate verified scoped batch; never sweep deferred work into deployment to sync.
- Named git adds only; one build lease/batch, /m:2 /nr:false /p:UseSharedCompilation=false.
- Reuse cached SDKs/services. Persistent identities/stores/keys/journals stay under
  LocalState/COSMIC or their retained protected location; logs/TRX only TestResults.
- Preserve unknown outcomes; never replay/reseed/reset. Record all resources and
  exact teardown in [the ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md). Free-only services.

```powershell
Scripts/TestHosts/Run-CosmicAzureBlobCqlNative.ps1
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Avalanche -Federated -ShowPlan
Scripts/TestHosts/Update-CosmicCqlProviderInventory.ps1
```

[Manual](CQL_MANUAL.md) / [architecture](CQL_ARCHITECTURE.md) / [launcher](CQL_DEMO_LAUNCHER.md)
/ [previous detailed status](Archive/CURRENT_STATUS_PRE_AZURE_BLOB_NATIVE_2026-10-29.md).
