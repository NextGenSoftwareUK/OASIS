# Arbitrum V2 CQL MVP

Updated 29 October 2026. Local native + Mongo-paired C#/HTTP/browser MVP verified. Hosted/mainnet/rollup and full parity remain unverified.

## Shipping path and scope

BootLoader `EvmProviderFactory.CreateArbitrum` selects `ArbitrumOASIS_Web3Core` when HyperDrive mode isV2. Acceptance compiles/links that exact factory and asserts its concrete result. The separate custom Arbitrum provider is the legacy-mode path; it is unchanged and its CQL gap remains documentation-only.

The common Web3Core adapter now admits Polygon and Arbitrum authorities, each requiring its exact trusted catalogue. ID equality/membership, Boolean AND/OR/constants and paging compile to at most100 native keys; unsupported metadata, sorting and child graphs reject before I/O. Native lookup uses shipping contract ABI and existing provider/chain/contract-bound authenticated decoder, pinned to one observed block number. Failure publishes no partial result.

This uses retained Ganache7.9.2/local chain31338. It does not prove hosted Arbitrum, L2 sequencer/rollup behaviour, mainnet, finality or a snapshot shared with Mongo.

## Verified

- Controls14/14 pass: seven new Arbitrum cases plus seven Polygon regressions after the common authority compiler changed. Evidence `TestResults/EthereumCqlCurrent/web3core-cql-controls.trx`.
- Native1/1 passes: concrete shipping V2 factory, one local contract, three encrypted records, filtered two-record selection and no query transactions. Evidence `arbitrum-cql-native.trx`.
- Mongo-paired SDK/HTTP1/1 passes: all5 (Arbitrum3 + Mongo2), subset4 (2+2), provider provenance, unrelated record exclusion, forged actor/unknown preset rejection and source deactivation503 with no partial rows. Evidence `arbitrum-mongo-federation.trx`.
- Shipping Web3Core source compiled with pinned solc0.8.26; compiled ABI equals shipping embedded ABI. No provider-specific HTTP query engine: both SDK and HTTP use the generic HolonManager federation walkthrough.

## Launch

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Arbitrum -Federated

# Bounded native/SDK/HTTP checks:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Arbitrum -Federated -VerifyOnly
```

Open http://127.0.0.1:5099/ after startup. Show **Query all records**, then **Query two-record subset**, and inspect both provider labels. The server owns actor/catalogues; browser input selects named presets only.

Reuses existing runtime/chain/rootDPAPIkey; Arbitrum has distinct contract/actor/three IDs in `LocalState/COSMIC/EvmCql/Arbitrum/demo-seed.json`. Pending journals fence replay. Exact temporary Mongo fixture is validated before removal; chain/key/seed are retained after graceful node shutdown. No downloads, duplicate runtime, new chain or public funds.

## Next

Continue Google Cloud (existing Firestore provider), Cosmos blockchain, Polkadot and NEAR MVPs before deeper parity. Hosted Azure stays pending by user instruction. Broader RPC failure injection, content queries, history and full parity remain red.

[Current status](CURRENT_STATUS.md) / [launcher](CQL_DEMO_LAUNCHER.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

Actual browser all5/subset4 and both provider labels verified. Screenshot `TestResults/EthereumCqlCurrent/arbitrum-mongo-browser.jpg`; final interactive1/1 duration27s includes UI verification and shutdown. A second process run reused original Arbitrum seed/contract without source writes. Both owned services stopped; exact temporary Mongo fixture validated/dropped; pending/ready markers removed; native chain/key/seeds retained. Launcher discovery10paired profiles/20plans,8pair rejections,8single/16plans passes with unchanged listeners.
