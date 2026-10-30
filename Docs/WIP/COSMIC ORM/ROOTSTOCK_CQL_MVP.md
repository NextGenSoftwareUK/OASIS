# Rootstock V2 CQL MVP — native verified, paired pending

Updated 29 October 2026. Rootstock remains red in the all-provider demo inventory.

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
- `TestResults/EthereumCqlCurrent/rootstock-cql-native.trx` SHA256 `de8bdd71af122e2d338ce7b86acd2a8e34b86921194e4f04c223adf4b77e652d`.
- Original native-record-hashes manifest SHA256 `63cab327e5cba58d977bc2cc118dbc1eaa8b6bd393e0ef127f0984f93fd3dc83`.

```powershell
Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 -Provider Rootstock
```

## Exact next step and remaining gates

Reuse this original source for Mongo-paired C#/HTTP/browser5/4 and hub. Keep the
all-provider demo inventory red until paired/browser proof passes.
Example local test DNA fields: ConnectionString `http://127.0.0.1:18547`, ChainId
`31338`, plus the existing configured test key and compatible Web3Core contract.
Do not substitute that identity for public Rootstock in production.
Public Rootstock/Bitcoin anchoring/finality/bridge/gas behavior remains unverified.
Full graph/history/conditional concurrency/placement/EF parity remain open.
