# Cosmos blockchain CQL MVP

Updated 29 October 2026. Native encrypted three-record/two-record query and restart reuse verified; Mongo-paired demo is not ready. This is distinct from Azure Cosmos DB.

## Implemented and verified

- CosmosBlockChainOASIS implements the common CQL provider contract. `CosmosCqlCompiler` intersects captured ID equality/membership/Boolean selection with an exact trusted Cosmos authority catalogue; paging and at most100 native keys are admitted.
- Query execution uses the shipping encrypted Backend and canonical compact-GUID `oasis/holon/{id:N}` layout via the shared RecordKey builder, then canonical stored-record decoding. Normal backend calls are CosmJS contract gets. No plaintext/local alternative, query-time key enumeration or managed business predicate.
- Missing keys are absent records; wrong identity, authentication/decode failure, transport failure, cancellation or result/4MiB budgets publish no partial rows. Deleted records are hidden. Reads may observe different blocks; no snapshot/finality guarantee.
- Producer build passes. Controls14/14 pass: seven expression/admission cases, plus actual private-codec execution over a controlled in-memory transport for success, missing/deleted rows, corrupt ciphertext, foreign identity, later transport failure and result budget. Query reads only selected physical keys, performs no list/write/delete and discards earlier rows on later failure.
- Evidence `TestResults/CosmosCqlCurrent/cosmos-cql-controls.trx`. Controlled evidence proves adapter/encryption boundaries; separate native acceptance below now proves the retained local-chain query.

Activation is separate from query execution: the existing private-storage activation verifies encrypted key material by enumerating existing records. Tests allow that existing activation behaviour, then enforce no enumeration during CQL. The first test attempt incorrectly applied its query-only prohibition to activation; the fixture boundary was corrected without changing production activation.

## Exact next step

1. Add Mongo-paired5/4 C#/HTTP/browser and the launcher preset using the existing generic HolonManager walkthrough. Reuse the exact retained CQL contract, key and three IDs; do not instantiate or save again.
2. Verify provider provenance, failed-source no-partial publication and scoped Mongo cleanup. Then Polkadot and NEAR MVPs before deeper parity. Hosted Azure stays pending by user instruction; legacy gaps are documentation-only.

## Runtime evidence

Read-only WSL Podman inspection returned container58a873de020d2f79ab11e7087361db25063c277a5b5a5c2cabba72263b35f8f3, state exited, image `docker.io/cosmwasm/wasmd@sha256:3741178a4d747fd5cf281550ab4ac30d771163af1c543f9da503c411a1691773`. Its ledger is inside the retained container (no external mounts). Preserve it. Initial controlled stage used no native service. The later readiness inspection resumed the retained node, verified native reads, stopped wasmd with SIGINT, then stopped its idle container; no contracts or source records were changed.

## Reproduce controls

Build CosmosBlockChainOASIS and the maintained CosmosBlockChainOASIS.IntegrationTests project with one build lease, `--no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false`. Run `--filter 'FullyQualifiedName~CosmosCqlTests|FullyQualifiedName~CosmosCqlEncryptedTests' --logger 'trx;LogFileName=cosmos-cql-controls.trx' --results-directory TestResults/CosmosCqlCurrent`. Initial consumer assets were restored from the existing local NuGet cache only.

[Current status](CURRENT_STATUS.md) / [breadth roadmap](CQL_DEMO_EXPANSION_ROADMAP.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## SDK restoration and native readiness (29 October)

Neither checkout retained CosmJS node_modules; Windows npm cache and retained WSL directories did not contain a suitable installation. Restored the exact committed lock once with npmci/prefer-offline, applied the committed postinstall compatibility patch and verified bridge syntax/imports/direct pins. CosmJS0.39.0/cosmjs-types0.11.0:32packages,13708596bytes under this worktree's source SdkBridge/node_modules. Lock SHA256405F326C4B4C062331C5E9AA7DA2E1F2C553AF7F772D2854D16374997FC37FDC. Reuse this installation; output consumers should use the existing dependency-junction helper.

Retained node resumed on its original ledger (height10128 observed, not catching up). Official CosmJS bridge queried the default contract's public config and a missing key successfully. Default contract `wasm14hj2tavq8fpesdwxxcu44rty3hh90vhujrvcmstl4zr3txmfvw9s0phg4d`; no private signing key was exported. Compact evidence `TestResults/CosmosCqlCurrent/native-environment-inspection.json` records owner/config/code checksum and zero signing/source writes. This is native SDK connectivity, not native multi-record CQL acceptance.

Owned wasmd PID132 exited after the existing SIGINT stop script confirmed shutdown. Podman subsequently escalated termination of the remaining sleep-infinity container process after10seconds; the chain process had already stopped. Container is exited, ledger preserved. An initial oversized combined inspection command was rejected by automatic review before execution; smaller explicit ownership/read/stop steps completed the read-only inspection successfully. No unknown transaction or pending native mutation resulted.

## Native fixture and root-cause fix (29 October)

`Scripts/TestHosts/Run-CosmicCosmosCqlNative.ps1` now verifies the pinned container/image/code1 checksum and fresh local chain identity, links the one reusable SDK installation into test output and reuses CurrentUser-DPAPI encryption material under `LocalState/COSMIC/CosmosCql`. It refuses pending originals or a duplicate labelled contract without its manifest.

One separately owned contract was instantiated from existing code1 (no upload), then three encrypted holons were saved. Four signed physical preparations were verified and flushed before broadcast; four exact successful native receipts were retained before marking the seed complete. Signed fee ceiling10000stake per transaction; actual setup fees19117stake total, all local test units. No historical contract or public-chain funds were changed.

Contract: `wasm1yw4xvtc43me9scqfr2jr2gzvcxd3a9y4eq7gaukreugw2yd2f8tsu3v7ad`. State11files/14775bytes at first measurement contains original public signed transactions/receipts, seed manifest, environment lock and protected key; retain it. No signing key is stored in files/docs.

The first native query exposed a genuine bug: CQL guessed hyphenated GUID keys while the shipping storage layer saves compact GUIDs. The three saves were already committed and acknowledged, but the query returned an incorrect empty result. Fixed the root by exposing the existing shared RecordKey builder to derived providers and calling it from CQL; no second lookup/fallback path. Controlled fixtures now seed through actual SaveHolonAsync so they cannot repeat a query's format assumption.

Original failed proof is retained as `TestResults/CosmosCqlCurrent/cosmos-cql-native.initial-key-format-failure.trx`. The same original live node/contract/records then passed1/1 after the fix. A full stop/start run passed1/1 again, preserving the exact seed hash and four-submission set; no re-save/reseed/redeployment. Native assertions cover all3 names, filtered Espresso/Latte and unchanged source content.

Final evidence `TestResults/CosmosCqlCurrent/cosmos-cql-native.trx`; controlled14/14 pass after the SaveHolon fixture correction. The node was stopped with SIGINT and the container is exited. Podman subsequently killed only the remaining sleep-infinity container process after its10s stop timeout; the ledger process had already exited. Failure preservation/explicit original-PID continuation are test-runner modes, not alternate production reads.

Native setup/report remains local-chain proof, not Cosmos Hub/mainnet, finality, query snapshot, arbitrary metadata/order/graph or full parity acceptance.
