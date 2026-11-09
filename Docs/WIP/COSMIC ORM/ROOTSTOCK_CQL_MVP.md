# Rootstock V2 CQL MVP — native and Mongo-paired verified

Updated 29 October 2026. The green inventory row certifies only the local EVM/shipping-factory scope below.

## Root causes and fixes

Shipping Rootstock inherited Web3Core CQL but its authority was excluded by the
compiler. Initial controls reproduced **2failed/5passed** (authorized selection
rejected; access error instead of inactive-source handling).
Its constructor fixed chain30, DNA exposed no chain choice, and the active
BootLoader registration ignored endpoint overrides. This prevented correct test
network configuration at the actual factory boundary.

- Admit only explicit Rootstock authority to the existing bounded GUID compiler;
  trusted source catalogue, frozen selection, unsupported-shape rejection,100IDbound.
- Keep the original three-string constructor and default30; a single explicit
  positive BigInteger-chain constructor supports configured networks.
- Rootstock DNA now exposes `ChainId` (long, default30). Actual BootLoader case
  calls EvmProviderFactory.CreateRootstock, preserving configured identity/key/
  contract and endpoint override. Nonpositive identities reject before activation.
- Existing legacy code was not modified. No alternate storage/query path added.

## Verified controls

[Rootstock tests](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/RootstockCqlTests.cs):
**13passed**, including preserved constructor/default, configured30/31/31338,
endpoint override, invalid chains, authority/captured catalogue/shape/lifecycle.
Changed-compiler family regression controls48 also pass: **61total**, zero failed/
skipped. DNA/Web3Core/Rootstock/BootLoader/test producers build with bounded
workers and existing dependencies. No native process or new fixture yet.

Evidence under `TestResults/RootstockCqlCurrent`:
- `rootstock-repro.trx` SHA256 `515f434c0c73411bcb57e9ace4e42a1668a291e5ce308d8324cf748c7961bce8` (original failures retained).
- `rootstock-family-controls.trx` SHA256 `ace83a6af0fb75853d9b6115e54d5c4550d9a8cc11f6a88e389c1807f8385dbd`.

## Native local EVM proof

Actual shipping Rootstock factory uses explicit chain31338. Reuses pinned
Ganache7.9.2/solc0.8.26/ethers6.13.1, original shared chain files and existing
DPAPI storage key. No runtime/download/key or second chain created.
Original dedicated contract `0xd86c8f0327494034f60e25074420bccf560d5610` stores3encrypted records;
native CQL membership selects2. Queries preserve raw native response hashes and
account nonce. Full runner restart reuses the original contract/IDs/ciphertext
without source writes: **native1/restart1 passed**, zero failed/skipped.

[Native test](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/RootstockCqlNativeTests.cs)
uses [shipping factory helper](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/EvmCqlDemoProviders.cs).
Retained metadata `LocalState/COSMIC/EvmCql/Rootstock`885B includes original durable
intent/deployment receipt/per-write acknowledgements/manifest/native response hash
baseline. Pending outcomes refuse replay; missing original hashes refuse adoption.
Shared chain/key retained. Owned18547node flushed/stopped through stdin, no listener.

Evidence:
- `TestResults/RootstockCqlCurrent/rootstock-native-first.trx` SHA256 `a2f909b21ff3bff8603dc251053397ff6e3d2c89bb6357f575ab85c16d85032c`.
- `TestResults/EthereumCqlCurrent/rootstock-cql-native.trx` SHA256 `e61da8ebfe211920237be99b7a7e956e1fcd285011de95ff8f36ad3278142f68`.
- Original native-record-hashes manifest SHA256 `63cab327e5cba58d977bc2cc118dbc1eaa8b6bd393e0ef127f0984f93fd3dc83`.

```powershell
Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 -Provider Rootstock
```

## Mongo-paired teaching preset

[Executable C#/HTTP example/test](../../../Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/EvmMongoCqlNativeTests.cs)
selects the actual Rootstock factory/authority and original contract. Shared CQL
membership walkthrough runs through HolonManager. All: Rootstock3+Mongo2 =
**5physical rows**; subset2+2 = **4**, retaining provider copies and excluding an
unrelated Mongo record. Forged actor/unknown preset reject400; inactive Rootstock
returns503 without rows. No shared snapshot claim.
Bounded hub and actual hub/browser each pass native1/paired1; browser tab39 verifies
both presets/provenance and controlled shutdown. Hub routing:8single/16plans,
35paired/70plans,56paired rejection,3archive rejection; listeners unchanged.
Exact temporary `cosmic_cql_mongo_rootstock_demo`3records verified/dropped; pending/
ready markers removed. Original contract/IDs/key retained; owned18547node flushed/
stopped, Mongo27029/host5126 stopped and no listeners remain.

Latest `TestResults/EthereumCqlCurrent/rootstock-mongo-federation.trx` SHA256:
`f6aa2edc815e73267026f11b0297634c36a49688b2399f5f8b42ab05c17de10f`.
Screenshot `TestResults/RootstockCqlCurrent/browser.jpg` is disposable proof.

```powershell
# Interactive; stop through the page button.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Rootstock -Federated
# Bounded native/C#/HTTP verification and teardown.
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Rootstock -Federated -VerifyOnly
```

## Remaining gates

Example local test DNA fields: ConnectionString `http://127.0.0.1:18547`, ChainId
`31338`, plus the existing configured test key and compatible Web3Core contract.
Do not substitute that identity for public Rootstock in production.
Public Rootstock/Bitcoin anchoring/finality/bridge/gas behavior remains unverified.
Full graph/history/conditional concurrency/placement/EF parity remain open.
