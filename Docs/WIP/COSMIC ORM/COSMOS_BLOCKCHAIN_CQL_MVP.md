# Cosmos blockchain CQL MVP

Updated 29 October 2026. Adapter/controlled encrypted execution verified; native-chain/paired demo is not ready. This is distinct from Azure Cosmos DB.

## Implemented and verified

- CosmosBlockChainOASIS implements the common CQL provider contract. `CosmosCqlCompiler` intersects captured ID equality/membership/Boolean selection with an exact trusted Cosmos authority catalogue; paging and at most100 native keys are admitted.
- Query execution uses the shipping encrypted Backend and `oasis/holon/{id}` layout, then canonical stored-record decoding. Normal backend calls are CosmJS contract gets. No plaintext/local alternative, query-time key enumeration or managed business predicate.
- Missing keys are absent records; wrong identity, authentication/decode failure, transport failure, cancellation or result/4MiB budgets publish no partial rows. Deleted records are hidden. Reads may observe different blocks; no snapshot/finality guarantee.
- Producer build passes. Controls14/14 pass: seven expression/admission cases, plus actual private-codec execution over a controlled in-memory transport for success, missing/deleted rows, corrupt ciphertext, foreign identity, later transport failure and result budget. Query reads only selected physical keys, performs no list/write/delete and discards earlier rows on later failure.
- Evidence `TestResults/CosmosCqlCurrent/cosmos-cql-controls.trx`. This proves adapter/encryption boundaries, not native-chain execution.

Activation is separate from query execution: the existing private-storage activation verifies encrypted key material by enumerating existing records. Tests allow that existing activation behaviour, then enforce no enumeration during CQL. The first test attempt incorrectly applied its query-only prohibition to activation; the fixture boundary was corrected without changing production activation.

## Exact next step

1. Inspect/reuse retained native chain and signing/contract configuration without exposing key contents. Container oasis-wasmd exists and is exited; it was not started or modified during this stage.
2. Worktree SdkBridge/node_modules and TestResults/HyperDriveCosmos evidence are absent. Find retained compatible CosmJS dependencies/cache before restoring anything. Do not replay the original runner's missing journals or reset/redeploy historical contracts. Preserve any original unresolved outcomes.
3. Inventory one fixed CQL fixture before allocation. Keep manifests, encryption key and seed under LocalState/COSMIC; raw logs/reports under TestResults/CosmosCqlCurrent. Prove native3/2 filtered reads, source-read-only reuse and then Mongo-paired5/4 C#/HTTP/browser through the existing generic teaching flow.
4. Enrol the launcher only with verified scope, then Polkadot and NEAR before deeper parity. Hosted Azure stays pending by user instruction. Legacy HyperDrive gaps are documentation-only.

## Runtime evidence

Read-only WSL Podman inspection returned container58a873de020d2f79ab11e7087361db25063c277a5b5a5c2cabba72263b35f8f3, state exited, image `docker.io/cosmwasm/wasmd@sha256:3741178a4d747fd5cf281550ab4ac30d771163af1c543f9da503c411a1691773`. Its ledger is inside the retained container (no external mounts). Preserve it. No native service, signing operation, new database, download or installation occurred.

## Reproduce controls

Build CosmosBlockChainOASIS and the maintained CosmosBlockChainOASIS.IntegrationTests project with one build lease, `--no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false`. Run `--filter FullyQualifiedName~CosmosCql --logger 'trx;LogFileName=cosmos-cql-controls.trx' --results-directory TestResults/CosmosCqlCurrent`. Initial consumer assets were restored from the existing local NuGet cache only.

[Current status](CURRENT_STATUS.md) / [breadth roadmap](CQL_DEMO_EXPANSION_ROADMAP.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
