# Monad CQL MVP admission

Updated 29 October 2026. Local native/restart and Mongo-paired C#/HTTP/browser MVP verified.

## Root fixes and proof

Original three-string constructor now supplies mainnet143 to the shared encryption
boundary, preserving signature. Explicit positive chain-ID overload supports testnet
and local networks; zero/negative values reject before native access. Stale41454
source/package metadata corrected against [official Monad Developers](https://github.com/monad-developers).
Shared CQL compiler admits explicit Monad authorized catalogues; immutable captured
IDs, authority separation, unsupported shape/bound rejection, inactive state and
pre-cancellation covered. Actual repro3failed/5passed; fixed Monad11passed and
EVM family127passed/0failed/0skipped. Test `MonadCqlTests.cs` in Ethereum integration
project. Evidence `TestResults/MonadCqlCurrent/monad-family-controls.trx`, SHA256
`8fa265c61d9c88c2c454d27a5f7f77842267ddbfaa6cabcdfe0f7845eed28c0d`.

## Native and restart proof

The shipping Monad provider uses a dedicated logical contract on the original
Ganache7.9.2 local EVM31338 and protected shared key. Initial1/1 and independent
runner restart1/1 pass. Three encrypted records return Espresso/Latte/Tea; explicit
ID membership selects Espresso/Latte. Native response hashes match the retained
original baseline before/after queries and after restart; account nonce unchanged.
Original fixture metadata885B under `LocalState/COSMIC/EvmCql/Monad`; no pending
intent. Both owned nodes flushed/exited. Test `MonadCqlNativeTests.cs` in the Ethereum
integration project. Run `Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 -Provider Monad`.

Evidence `TestResults/MonadCqlCurrent/native-first.trx`, SHA256 `1c189377417fbe6b6d15203640a9b4e2dcf1acbf849f01590496eaa5cd6405bb`.

Evidence `TestResults/MonadCqlCurrent/native-restart.trx`, SHA256 `1353fa03fff5c3a1831da844aedae65d4e8e809791470d2fefe02f7595b8e456`.

## Ready Mongo-paired demo

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Monad -Federated`.
Add `-VerifyOnly` for bounded verification. Browser `http://127.0.0.1:5131/`:
all records5physical rows (Monad3/Mongo2), subset4rows (Monad2/Mongo2).
Shared `IdentityFederationWalkthrough` uses explicit named scopes, separate trusted
catalogues and HolonManager; source copies retain provider provenance.

```csharp
// Select requested IDs inside each provider's authorized catalogue.
source.Where(row => selectedIds.Contains(row.Id));

// Execute native reads while retaining each source copy.
await manager.ExecuteFederationQueryAsync(plan, adapters);
```

Bounded paired1/1 and interactive paired1/1 pass. HTTP rejects forged actor/unknown
preset400 and inactive Monad503 without partial rows. Browser AX results5/4 verified;
screenshot capture failed, no image proof retained. Shutdown verified exact temporary
Mongo fixture ownership/contents and drop. Pending/ready absent; owned18547/27029/5131
listeners stopped, tab44 closed; original885B Monad metadata/source/key retained.
Mongo termination is not proof of clean native disk flush. Hub controls8single/16plans,
40paired/80plans,66paired rejections,3archive rejections pass; listeners unchanged.

Public Monad parallel execution/consensus/finality, central registration, distributed
snapshots and full parity remain unverified. Helper DNA testnet endpoint is not
runtime enrollment; explicit network ID required. Next Abstract runtime/chain/query
audit, preserving zkSync's independently reproduced native checkpoint gate.

Evidence `TestResults/MonadCqlCurrent/paired-bounded.trx`, SHA256 `403fee6dab555763eff5c5cb948e4e963e5ff1bb990722ffbf6b91be3a40719e`.

Evidence `TestResults/EthereumCqlCurrent/monad-mongo-federation.trx`, SHA256 `78739a15aed32aa47c287d4863a73a9a9697e124477aba655cabad82671e1ec0`.
