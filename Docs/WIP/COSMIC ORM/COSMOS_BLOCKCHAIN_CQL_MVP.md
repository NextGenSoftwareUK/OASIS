# Cosmos blockchain CQL MVP

Updated 29 October 2026. Local native + Mongo-paired C#/HTTP/browser MVP verified. Mainnet and full parity remain unverified. This is distinct from Azure Cosmos DB.

## Implemented and verified

- CosmosBlockChainOASIS implements the common CQL provider contract. `CosmosCqlCompiler` intersects captured ID equality/membership/Boolean selection with an exact trusted Cosmos authority catalogue; paging and at most100 native keys are admitted.
- Query execution uses the shipping encrypted Backend and canonical compact-GUID `oasis/holon/{id:N}` layout via the shared RecordKey builder, then canonical stored-record decoding. Normal backend calls are CosmJS contract gets. No plaintext/local alternative, query-time key enumeration or managed business predicate.
- Missing keys are absent records; wrong identity, authentication/decode failure, transport failure, cancellation or result/4MiB budgets publish no partial rows. Deleted records are hidden. Reads may observe different blocks; no snapshot/finality guarantee.
- Producer build passes. Controls14/14 pass: seven expression/admission cases, plus actual private-codec execution over a controlled in-memory transport for success, missing/deleted rows, corrupt ciphertext, foreign identity, later transport failure and result budget. Query reads only selected physical keys, performs no list/write/delete and discards earlier rows on later failure.
- Evidence `TestResults/CosmosCqlCurrent/cosmos-cql-controls.trx`. Controlled evidence proves adapter/encryption boundaries; separate native acceptance below now proves the retained local-chain query.

Activation is separate from query execution: the existing private-storage activation verifies encrypted key material by enumerating existing records. Tests allow that existing activation behaviour, then enforce no enumeration during CQL. The first test attempt incorrectly applied its query-only prohibition to activation; the fixture boundary was corrected without changing production activation.

## Exact next step

Continue Polkadot, then NEAR MVPs before deeper parity. Hosted Azure stays pending by user instruction; legacy gaps are documentation-only.

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

## Mongo-paired preset

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider CosmosChain -Federated

# Bounded native + SDK/HTTP verification:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider CosmosChain -Federated -VerifyOnly
```

Open http://127.0.0.1:5101/ after startup. This selects Cosmos **blockchain**, not Azure Cosmos DB. All returns Cosmos3 + Mongo2 =5 physical rows; subset returns2 +2 =4. IDs retain copies from both sources. Actor/catalogues are server-owned and HTTP accepts named presets only.

Paired SDK/HTTP acceptance1/1 passes with provider provenance, unrelated-row exclusion, forged actor/unknown preset rejection and source deactivation503 with no partial Mongo result. Evidence `TestResults/CosmosCqlCurrent/cosmos-mongo-federation.trx`. Both paths execute the generic C# walkthrough through HolonManager; there is no separate HTTP query engine.

The original contract/key/three records/four signed submissions are reused; paired reads do not instantiate or save on-chain. The temporary Mongo DB is absent before setup, journaled before writes and validated by exact contents/actor/name/schema marker before removal. Read failure preserves unexpected state/journals. No mainnet, finality, shared snapshot, arbitrary metadata/order/graph or full ORM/HyperDrive parity claim.

Actual browser buttons verified all5/subset4 and CosmosBlockChainOASIS/MongoDBOASIS labels. Screenshot `TestResults/CosmosCqlCurrent/cosmos-mongo-browser.jpg`; final interactive1/1 duration1m56s includes UI verification/shutdown, not query latency. Original seed SHA256D981416656A1ABC862D033D8E5C0FCB62BF11026ECE24605314CFB0B368F5B66 and exactly four prepared/acknowledged submissions remain unchanged. Owned native/Mongo services stopped; exact temporary Mongo DB validated/dropped; pending/ready markers removed. Native ledger process stopped with SIGINT before Podman terminated the remaining idle container process. Routing12paired profiles/24plans,10pair rejections,8single/16plans passes with unchanged listeners.
