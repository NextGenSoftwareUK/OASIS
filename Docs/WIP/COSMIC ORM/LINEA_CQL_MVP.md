# Linea CQL MVP admission

Updated 29 October 2026. Local native/restart and Mongo-paired C#/HTTP/browser MVP verified.

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

## Ready Mongo-paired demo

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Linea -Federated`.
Add `-VerifyOnly` for bounded C#/HTTP verification. Browser address is
`http://127.0.0.1:5129/`. Choose all records for5physical rows (Linea3/Mongo2),
then the two-record subset for4rows (Linea2/Mongo2). Both source copies preserve
provider provenance. C# uses the shared `IdentityFederationWalkthrough` and
`HolonManager.ExecuteFederationQueryAsync`, explicit named scopes and separate
trusted provider catalogues:

```csharp
// Select only the requested IDs inside each provider's authorized catalogue.
source.Where(row => selectedIds.Contains(row.Id));

// Execute the native provider reads and preserve each source copy.
await manager.ExecuteFederationQueryAsync(plan, adapters);
```

Paired scenario1/1 passes in bounded and interactive runs. HTTP rejects forged actor
and unknown preset with400; inactive Linea returns503 without partial rows. Browser
verified both5/4results. Stop demo verifies/drops only the journalled temporary
Mongo fixture; original Linea contract/keys/source remain. Owned18547/27029/5129
listeners stopped, journal absent, browser tab closed. Mongo process termination
is not evidence of clean native disk flush.

Hub routing:8single/16plans,38paired/76plans,62paired rejections,3archive rejections;
listener inventory unchanged. Screenshot `TestResults/LineaCqlCurrent/browser.jpg`.
Next provider is Scroll; deferred correctness/parity remains open.

No central registration, public Linea network, sequencer, finality, bridge,
production replication or full EF/NHibernate parity claim is made by these controls.

Evidence SHA256 `TestResults/LineaCqlCurrent/native-first.trx`: `2a17f4eb1d424c496e5963e341b6376f03391326c9dea3af41a6cd7303f8ed79`.

The fixed current native TRX is refreshed by each runner; the earlier restart hash is historical, not a hash of its current contents.

Evidence SHA256 `TestResults/LineaCqlCurrent/native-paired-recheck.trx`: `8a8e9508bb18f7cb53eb7405634b15f79eae1a81056e70944db339cb06f4c304`.

Evidence SHA256 `TestResults/EthereumCqlCurrent/linea-mongo-federation.trx`: `9b13f8c7c2c59327d8677c2baf526a2134ba452c3c4b3e6883a3005996bc9141`.
