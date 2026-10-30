# Monad CQL MVP admission

Updated 29 October 2026. Controls and local native/restart verified; paired demo remains pending.

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

## Exact next step

Verify Mongo-paired C#/HTTP/browser5/4 and hub preset using original Monad source.
Scope is local EVM only; no public Monad parallel execution/consensus/finality or
network proof inferred. Central registration absent in inspected active switch;
DNA helper testnet endpoint is not runtime enrollment. Explicit chain ID must
accompany non-mainnet construction. Full parity/replication remains open.
