# Cosmos blockchain CQL MVP

Updated 29 October 2026. Adapter/controlled encrypted execution and retained native SDK connectivity verified; native multi-record/paired demo is not ready. This is distinct from Azure Cosmos DB.

## Implemented and verified

- CosmosBlockChainOASIS implements the common CQL provider contract. `CosmosCqlCompiler` intersects captured ID equality/membership/Boolean selection with an exact trusted Cosmos authority catalogue; paging and at most100 native keys are admitted.
- Query execution uses the shipping encrypted Backend and `oasis/holon/{id}` layout, then canonical stored-record decoding. Normal backend calls are CosmJS contract gets. No plaintext/local alternative, query-time key enumeration or managed business predicate.
- Missing keys are absent records; wrong identity, authentication/decode failure, transport failure, cancellation or result/4MiB budgets publish no partial rows. Deleted records are hidden. Reads may observe different blocks; no snapshot/finality guarantee.
- Producer build passes. Controls14/14 pass: seven expression/admission cases, plus actual private-codec execution over a controlled in-memory transport for success, missing/deleted rows, corrupt ciphertext, foreign identity, later transport failure and result budget. Query reads only selected physical keys, performs no list/write/delete and discards earlier rows on later failure.
- Evidence `TestResults/CosmosCqlCurrent/cosmos-cql-controls.trx`. This proves adapter/encryption boundaries, not native-chain execution.

Activation is separate from query execution: the existing private-storage activation verifies encrypted key material by enumerating existing records. Tests allow that existing activation behaviour, then enforce no enumeration during CQL. The first test attempt incorrectly applied its query-only prohibition to activation; the fixture boundary was corrected without changing production activation.

## Exact next step

1. Reuse the now-restored CosmJS SDK and retained chain. Node0.70.3/chain oasis-cosmos-local/denom stake, code1 checksum8FB3C70F96A3FA83E3563B7CBDCE097C5FA4EA77DA7610EC725275AB39424A87 and default contract config were inspected with zero signing/submissions. The native node/container are exited again.
2. Create one distinctly owned CQL contract using retained compatible code, with a journal persisted before any signed submission, explicit local fee limits and no replay on uncertain outcome. Preserve the historical contract unchanged: a fresh storage encryption key must not be used to read its old encrypted records. Do not bypass shipping activation/decryption checks.
3. Inventory one fixed CQL fixture before allocation. Keep manifests, encryption key and seed under LocalState/COSMIC; raw logs/reports under TestResults/CosmosCqlCurrent. Prove native3/2 filtered reads, source-read-only reuse and then Mongo-paired5/4 C#/HTTP/browser through the existing generic teaching flow.
4. Enrol the launcher only with verified scope, then Polkadot and NEAR before deeper parity. Hosted Azure stays pending by user instruction. Legacy HyperDrive gaps are documentation-only.

## Runtime evidence

Read-only WSL Podman inspection returned container58a873de020d2f79ab11e7087361db25063c277a5b5a5c2cabba72263b35f8f3, state exited, image `docker.io/cosmwasm/wasmd@sha256:3741178a4d747fd5cf281550ab4ac30d771163af1c543f9da503c411a1691773`. Its ledger is inside the retained container (no external mounts). Preserve it. Initial controlled stage used no native service. The later readiness inspection resumed the retained node, verified native reads, stopped wasmd with SIGINT, then stopped its idle container; no contracts or source records were changed.

## Reproduce controls

Build CosmosBlockChainOASIS and the maintained CosmosBlockChainOASIS.IntegrationTests project with one build lease, `--no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false`. Run `--filter FullyQualifiedName~CosmosCql --logger 'trx;LogFileName=cosmos-cql-controls.trx' --results-directory TestResults/CosmosCqlCurrent`. Initial consumer assets were restored from the existing local NuGet cache only.

[Current status](CURRENT_STATUS.md) / [breadth roadmap](CQL_DEMO_EXPANSION_ROADMAP.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## SDK restoration and native readiness (29 October)

Neither checkout retained CosmJS node_modules; Windows npm cache and retained WSL directories did not contain a suitable installation. Restored the exact committed lock once with npmci/prefer-offline, applied the committed postinstall compatibility patch and verified bridge syntax/imports/direct pins. CosmJS0.39.0/cosmjs-types0.11.0:32packages,13708596bytes under this worktree's source SdkBridge/node_modules. Lock SHA256405F326C4B4C062331C5E9AA7DA2E1F2C553AF7F772D2854D16374997FC37FDC. Reuse this installation; output consumers should use the existing dependency-junction helper.

Retained node resumed on its original ledger (height10128 observed, not catching up). Official CosmJS bridge queried the default contract's public config and a missing key successfully. Default contract `wasm14hj2tavq8fpesdwxxcu44rty3hh90vhujrvcmstl4zr3txmfvw9s0phg4d`; no private signing key was exported. Compact evidence `TestResults/CosmosCqlCurrent/native-environment-inspection.json` records owner/config/code checksum and zero signing/source writes. This is native SDK connectivity, not native multi-record CQL acceptance.

Owned wasmd PID132 exited after the existing SIGINT stop script confirmed shutdown. Podman subsequently escalated termination of the remaining sleep-infinity container process after10seconds; the chain process had already stopped. Container is exited, ledger preserved. An initial oversized combined inspection command was rejected by automatic review before execution; smaller explicit ownership/read/stop steps completed the read-only inspection successfully. No unknown transaction or pending native mutation resulted.
