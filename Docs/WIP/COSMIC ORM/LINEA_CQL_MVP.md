# Linea CQL MVP admission

Updated 29 October 2026. Controls and local native/restart verified; paired demo remains pending.

## Verified

- Original three-string constructor preserves its signature and supplies chain59144
  to the shared encryption boundary. Explicit positive chain identities are supported;
  zero and negative identities reject before native access.
- Shared CQL compiler admits Linea's explicit authoritative record catalogue.
  Captured IDs are immutable, cross-provider authority rejects, and unsupported
  metadata/business predicates, ordering, graph expansion and over-bound catalogues reject.
- Lifecycle and pre-cancellation controls pass without opening an RPC connection.
- Initial reproduction: 3 failed / 5 passed. Fixed Linea controls:11 passed;
  affected EVM family:94 passed,0 failed,0 skipped.

Proof: `Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/LineaCqlTests.cs`.
Evidence: `C:/Source/OASIS-cosmic/TestResults/LineaCqlCurrent/linea-family-controls.trx`.
SHA256: `4ebb6cdcb4542e995b2d3e7531f7fff6c65b38ed684fd8227dd9f867b7a2eed6`. Raw evidence is regenerable and remains outside Docs.

## Native and restart proof

The shipping Linea provider ran against the reused Ganache7.9.2 chain31338 and
pinned Web3Core contract. Initial native test1/1 and independent runner restart1/1
passed. Three encrypted records load; explicit ID membership selects Espresso and
Latte from Espresso/Latte/Tea. Raw native response hashes match the retained original
baseline before/after queries and after restart; account transaction count is unchanged.
Original fixture metadata is885B under `LocalState/COSMIC/EvmCql/Linea`; no pending
intent remains. The owned EVM node flushed and exited after both runs.

Commands: `Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 -Provider Linea`.
Native test: `LineaCqlNativeTests.cs` in the Ethereum integration test project.
Evidence in `TestResults/LineaCqlCurrent/native-first.trx` and
`TestResults/EthereumCqlCurrent/linea-cql-native.trx` (restart), plus runner logs.

## Next acceptance

Reuse the original Linea fixture, shared chain and protected storage key. Verify Linea +
Mongo through C#, HTTP and the browser (five records / four selected) and enroll
its hub preset. Only then add an inventory proof link.

No central registration, public Linea network, sequencer, finality, bridge,
production replication or full EF/NHibernate parity claim is made by these controls.

Evidence SHA256 `TestResults/LineaCqlCurrent/native-first.trx`: `2a17f4eb1d424c496e5963e341b6376f03391326c9dea3af41a6cd7303f8ed79`.

Evidence SHA256 `TestResults/EthereumCqlCurrent/linea-cql-native.trx`: `cc9b71c96370e7ccc43e7f47b87348889f805eb0bd2849907fd1bfec2d1bf26f`.
