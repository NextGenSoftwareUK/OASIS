# Linea CQL MVP admission

Updated 29 October 2026. Controls verified; native and paired demo remain pending.

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

## Next acceptance

Reuse the existing local EVM chain31338, runtime and protected storage key. Add a
journalled Linea contract fixture, verify a three-record query filtered to two,
restart and reread original records without replaying writes. Then verify Linea +
Mongo through C#, HTTP and the browser (five records / four selected) and enroll
its hub preset. Only then add an inventory proof link.

No native, central registration, public Linea network, sequencer, finality, bridge,
production replication or full EF/NHibernate parity claim is made by these controls.
