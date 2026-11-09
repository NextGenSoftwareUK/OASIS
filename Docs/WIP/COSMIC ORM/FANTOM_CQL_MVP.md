# Fantom V2 CQL MVP — native and Mongo-paired verified

Updated 29 October 2026. The green inventory row certifies only the local EVM/direct-construction scope below.

## Reproduced root causes and fixes

The shipping Web3Core Fantom class inherited a CQL adapter but its authority was
excluded by the compiler and its constructor omitted the encryption chain identity.
Initial controls reproduced3failed/5passed: missing default250, trusted catalogue
rejection, and access failure before expected inactive-source handling.

- Admit explicit FantomOASIS authority to the existing bounded GUID compiler;
  trusted catalogue/frozen selection/unsupported-shape rejection, at most100IDs.
- Keep the original three-string constructor and delegate to one initializer with
  default250. Explicit positive BigInteger chain supports configured networks;
  zero/negative reject before native access. Local tests explicitly select31338.
- Legacy files were not changed. Central Fantom BootLoader case remains commented;
  direct construction must remain distinct from central registration proof.
- No alternate provider/store path, network probing or automatic retry added.

## Verified controls

[Fantom tests](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/FantomCqlTests.cs):
**11passed**. Changed-compiler EVM regression controls72 pass: **83total**, zero
failed/skipped. Web3Core/Fantom/test producers build with bounded workers and
cached-only dependency restores. No native process or source fixture yet.

Evidence under `TestResults/FantomCqlCurrent`:
- `fantom-repro.trx` SHA256 `26d3f2c12179763be5b22e1a3d9cd25810968d63bee3568b4130c1c113cd2490` (original failures retained).
- `fantom-family-controls.trx` SHA256 `7ac95c04ba2bc2d29186d41e59398dbf003c80ec1402cd68332af81e55768ba9`.

## Native local EVM proof

Reused Ganache7.9.2/solc0.8.26/ethers6.13.1, original shared chain31338/18547 and
existing DPAPI key. No new runtime/download/key/chain/environment. Shipping Fantom
V2 constructor explicitly selects31338 for encryption. Original contract
`0x2d8be6bf0baa74e0a907016679cae9190e80dd0a` stores3encrypted records; native membership
filters to2. Queries preserve raw native-response hashes and account nonce. Full
node restart reuses same contract/IDs/ciphertext without source writes:
**native1/restart1 passed**, zero failed/skipped.

[Native test](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/FantomCqlNativeTests.cs).
Original metadata `LocalState/COSMIC/EvmCql/Fantom`885B retains durable intent,
deployment receipt/per-write acknowledgements/manifest/native hashbaseline.
Pending outcomes refuse replay; missing original hashes refuse adoption. Shared
chain/key retained. Owned18547node flushed/stopped through stdin, no listener.

Evidence:
- `TestResults/FantomCqlCurrent/fantom-native-first.trx` SHA256 `9921aaf773a4962b09ac50fca5887beb85b919b31c4b079afa0496297ba2f3a9`.
- `TestResults/EthereumCqlCurrent/fantom-cql-native.trx` SHA256 `3310f69566a8ecc20b316b3b7dba88795ce56adf3cfaa73227cf702ec4896603`.
- Original native-record-hashes manifest SHA256 `659abb25f185298c7e0982cb021a728d60221b872c2ac92e5f52bad5fee65e75`.

```powershell
Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 -Provider Fantom
```

## Mongo-paired teaching preset

[Executable C#/HTTP example/test](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/EvmMongoCqlNativeTests.cs)
selects Fantom V2/original contract and runs the shared membership walkthrough
through HolonManager. All: Fantom3+Mongo2 = **5physical rows**; subset2+2 = **4**,
preserving provider copies and excluding an unrelated Mongo record. Forged actor/
unknown preset reject400; inactive secondary returns503 without rows.
Bounded hub and actual hub/browser each pass native1/paired1; browser tab41 verifies
both query presets/provenance and controlled shutdown. Hub routing:8single/16plans,
37paired/74plans,60paired rejection,3archive rejection; listeners unchanged.
Exact temporary `cosmic_cql_mongo_fantom_demo`3records verified/dropped, pending/
ready removed. Original contract/IDs/key retained. Owned18547node flushed/stopped,
Mongo27029/host5128 stopped, no listeners. No shared snapshot claim.

Latest `TestResults/EthereumCqlCurrent/fantom-mongo-federation.trx` SHA256:
`bfc6df8e2612b7772626779dc4e235d65aaacc1476ba8050877e5ef1e0f5c475`.
Screenshot `TestResults/FantomCqlCurrent/browser.jpg` is disposable proof.

```powershell
# Interactive; stop with the page button.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Fantom -Federated
# Bounded native/C#/HTTP proof and teardown.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Fantom -Federated -VerifyOnly
```

## Remaining gates

Central registration remains commented,
so direct construction must remain labelled. Public network availability/
consensus/finality/bridging/gas or migration to other networks is not certified
by this local EVM proof. Central DNA/factory routing, full graph/history/
conditional concurrency/placement/EF parity remain open.
