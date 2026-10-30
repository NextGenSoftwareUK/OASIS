# BNB Chain V2 CQL MVP — native and Mongo-paired verified

Updated 29 October 2026. The green inventory row certifies only the local EVM/direct-construction scope below.

## Reproduced root causes and fixes

The shipping Web3Core BNB class inherited CQL, but its authority was excluded by
the compiler and its constructor omitted the chain identity needed by encryption.
Initial controls reproduced3failed/5passed: missing default56, rejected trusted
catalogue and an access error before expected inactive-source handling.

- Admit explicit BNBChainOASIS authority to the existing bounded GUID compiler;
  trusted catalogue/frozen selection/unsupported-shape rejection, at most100IDs.
- Preserve original three-string constructor and delegate to one initializer with
  default56. A positive BigInteger chain overload supports explicit networks,
  including31338 for retained local tests. Zero/negative chains reject before access.
- Legacy BNB code remains unchanged. Central BootLoader BNB registration is
  commented out, so later direct-construction demo proof must be labelled clearly.
- No alternate query/storage path, factory redirection or network probing added.

## Verification

[BNB controls](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/BNBChainCqlTests.cs):
**11passed**. Other changed-compiler EVM regression controls61 pass: **72total**,
zero failed/skipped. Web3Core/BNB/test producer builds pass; cached-only restores
and bounded workers. No native service/contract/actor/key created for this step.

Evidence under `TestResults/BNBChainCqlCurrent`:
- `bnb-repro.trx` SHA256 `28c45d15d921963295286275512d11111679432201d0841d1174e54b89da3cb4` (original failures retained).
- `bnb-family-controls.trx` SHA256 `95f9675d86f614aaeb56a217283da097d4f640e194811c2e2d99b424a104df90`.

## Native local EVM proof

Reused Ganache7.9.2/solc0.8.26/ethers6.13.1, original shared chain31338/18547 and
existing DPAPI storage key. No new runtime/download/key/chain/environment.
Actual shipping BNB V2 constructor explicitly selects31338 for encryption.
Original contract `0x5017a545b09ab9a30499de7f431df0855bcb7275` stores3encrypted records;
native membership filters to2. Queries preserve raw native-response hashes and
account nonce. Full node restart reuses same contract/IDs/ciphertext without source
writes: **native1/restart1 passed**, zero failed/skipped.

[Native test](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/BNBChainCqlNativeTests.cs).
Original metadata `LocalState/COSMIC/EvmCql/BNBChain`885B retains durable initial
intent/deployment receipt/per-write acknowledgements/manifest/native hashbaseline.
Pending outcomes refuse replay; missing original hashbaseline refuses adoption.
Shared chain/key retained. Owned18547node flushed/stopped through stdin; no listener.

Evidence:
- `TestResults/BNBChainCqlCurrent/bnb-native-first.trx` SHA256 `8d8552be8cc9a3c3bf4a304475da965d488b486733f9af4b4f19a77f7f288d6b`.
- `TestResults/EthereumCqlCurrent/bnbchain-cql-native.trx` SHA256 `7bf10cac3f20dd025020eeba53ee530eda8e4d4f812bbc1dfe281cc5388792a8`.
- Original native-record-hashes manifest SHA256 `3cc9acd53a8c101bca96f7f0e1de6907ae93da1447969ce5c0fd9eafb73d9b9b`.

```powershell
Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 -Provider BNBChain
```

## Mongo-paired teaching preset

[Executable C#/HTTP example/test](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/EvmMongoCqlNativeTests.cs)
selects BNB V2 and its original contract, then runs the shared membership
walkthrough through HolonManager. All: BNB3+Mongo2 = **5physical rows**; subset2+2
= **4**, preserving provider copies and excluding an unrelated Mongo record.
Forged actor/unknown preset reject400; inactive secondary returns503 without rows.
Bounded hub and actual hub/browser each pass native1/paired1. Browser tab40 verifies
both query presets/provenance and controlled shutdown. Hub routing:8single/16plans,
36paired/72plans,58paired rejection,3archive rejection; listeners unchanged.
Exact temporary `cosmic_cql_mongo_bnbchain_demo`3records verified/dropped, pending/
ready removed. Original contract/IDs/key retained; owned18547node flushed/stopped,
Mongo27029/host5127 stopped, no listeners. No shared snapshot claim.

Latest `TestResults/EthereumCqlCurrent/bnbchain-mongo-federation.trx` SHA256:
`9be978d25507ddaa9802c4d4983595586fef1389dbd0cf1267d8eefa336da92e`.
Screenshot `TestResults/BNBChainCqlCurrent/browser.jpg` is disposable proof.

```powershell
# Interactive; stop with the page button.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider BNBChain -Federated
# Bounded native/C#/HTTP proof and teardown.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider BNBChain -Federated -VerifyOnly
```

## Remaining gates

Direct construction is distinct from central
registration, which remains commented out. Public BNB Chain consensus/finality/
bridging/gas and production DNA/factory routing remain unverified. Full graph/
history/conditional concurrency/placement/EF parity remain open.
