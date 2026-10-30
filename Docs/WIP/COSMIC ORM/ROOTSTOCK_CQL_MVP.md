# Rootstock V2 CQL MVP — controls verified, native pending

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

## Exact next step and remaining gates

Use the actual shipping Rootstock factory with explicit local chain31338. Reuse
retained Ganache7.9.2/solc0.8.26/ethers6.13.1, original chain files and DPAPI storage
key; journal one Rootstock-specific original contract/actor/threeIDs before writes.
Verify encrypted native membership3to2 and unchanged native hashes/nonce, restart
without source writes, then Mongo-paired C#/HTTP/browser5/4 and hub preset.

Example local test DNA fields: ConnectionString `http://127.0.0.1:18547`, ChainId
`31338`, plus the existing configured test key and compatible Web3Core contract.
Do not substitute that chain identity for public Rootstock in production.
Native/current factory network admission, public Rootstock/Bitcoin anchoring/
finality/bridge/gas behavior remain unverified. Full graph/history/conditional
concurrency/placement/EF parity remain open; no demo count increment yet.
