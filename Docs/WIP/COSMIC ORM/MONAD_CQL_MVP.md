# Monad CQL MVP admission

Updated 29 October 2026. Controls verified; native/restart/paired remain pending.

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

## Exact next step

Reuse original EVM31338/runtime/key and add a journalled logical Monad fixture;
verify encrypted native3/filtered2, original hashes and nonce stability, restart,
then Mongo-paired C#/HTTP/browser5/4 and hub preset. Label the scope local EVM;
no public Monad parallel execution/consensus/finality or network proof inferred.
Central registration absent in inspected active switch; shipped DNA helper uses a
testnet endpoint and is not proof of runtime enrollment. Explicit chain-ID selection
must accompany non-mainnet construction. Full parity/replication remains open.
