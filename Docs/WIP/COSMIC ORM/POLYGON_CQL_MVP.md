# Polygon CQL MVP

Updated 29 October 2026. Native local-chain query verified; Mongo-paired investor preset is not ready.

## Implemented and verified

- Concrete `PolygonOASIS` inherits the CQL adapter in `Web3CoreOASISBaseProvider`. It uses the shipping Web3Core contract ABI, native `lookupHolon` key reads and existing provider/chain/contract-bound authenticated decoder.
- `Web3CoreCqlCompiler` currently admits only the Polygon authority. Captured ID equality/membership, AND/OR, constants and paging are bounded to100 native keys and intersect the trusted server catalogue. Unsupported metadata, sorting, child graphs and unbounded shapes reject before RPC.
- Every lookup uses one observed block number. No mainnet, finality, reorg-immunity or cross-provider snapshot claim.
- Shared Web3Core and Polygon builds pass. Initial controls7/7 pass (captured selection, access/authority mismatch, empty selection, five unsupported shapes, inactive execution and pre-cancellation). Report `TestResults/EthereumCqlCurrent/polygon-cql-controls.trx`.
- Native acceptance1/1 passes through concrete Polygon save/read/query: three encrypted records, filtered Espresso/Latte subset, zero transactions during queries. Report `TestResults/EthereumCqlCurrent/polygon-cql-native.trx`. Failure-RPC and broader semantics remain unverified for this adapter.
- Shipping Web3Core Solidity compiled with existing solc0.8.26, and compiled ABI equals embedded shipping ABI. Artifact `TestResults/EthereumCqlCurrent/web3-storage.json` is regenerable, not committed.

## Exact next step

Add the Mongo-paired5/4 C#/HTTP/browser preset using the same generic federation helper and launcher. Verify original seed reuse after restart during that run, source-failure behaviour and scoped temporary Mongo cleanup. Then continue Arbitrum, Google, Cosmos blockchain, Polkadot and NEAR before deeper parity. Arbitrum has a shipping custom adapter and a separate Web3Core variant: inspect the actual shipping path before deciding implementation reuse.

## Reproduce native verification

```powershell
Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 -Provider Polygon
```

The reusable EVM runner now accepts an explicit provider. Existing Ethereum defaults remain unchanged. Polygon federation is explicitly rejected until enrolled; no pretend-ready demo route.

Reuses Ganache7.9.2/solc0.8.26/Node24.19.0, retained local chain31338/loopback18547 and root DPAPI encryption key. Polygon has its own contract, actor and three IDs in `LocalState/COSMIC/EvmCql/Polygon/demo-seed.json`. Native chain state stays in root `EvmCql/chain-data`. Pending setup fences replay; original state is preserved on uncertain outcomes. Shutdown flushes/stops the owned node; port18547 is absent after acceptance. No new installation, runtime, chain reset or public funds.

The first runner admission stopped before service/native work because its runtime path was incorrectly relative to the newly nested Polygon state folder. It now resolves the single runtime from the worktree root; no duplicate runtime or fallback path was created.

[Current status](CURRENT_STATUS.md) / [Ethereum setup](ETHEREUM_CQL_MVP.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
