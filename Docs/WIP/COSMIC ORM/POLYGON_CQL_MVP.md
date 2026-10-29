# Polygon CQL MVP

Updated 29 October 2026. Local native + Mongo-paired C#/HTTP/browser MVP verified; hosted/mainnet and full parity remain unverified.

## Implemented and verified

- Concrete `PolygonOASIS` inherits the CQL adapter in `Web3CoreOASISBaseProvider`. It uses the shipping Web3Core contract ABI, native `lookupHolon` key reads and existing provider/chain/contract-bound authenticated decoder.
- `Web3CoreCqlCompiler` now admits Polygon and Arbitrum V2 authorities, each requiring its exact catalogue. Captured ID equality/membership, AND/OR, constants and paging are bounded to100 native keys and intersect the trusted server catalogue. Unsupported metadata, sorting, child graphs and unbounded shapes reject before RPC.
- Every lookup uses one observed block number. No mainnet, finality, reorg-immunity or cross-provider snapshot claim.
- Shared Web3Core and Polygon builds pass. Initial controls7/7 pass (captured selection, access/authority mismatch, empty selection, five unsupported shapes, inactive execution and pre-cancellation). Report `TestResults/EthereumCqlCurrent/polygon-cql-controls.trx`.
- Native acceptance1/1 passes through concrete Polygon save/read/query: three encrypted records, filtered Espresso/Latte subset, zero transactions during queries. Report `TestResults/EthereumCqlCurrent/polygon-cql-native.trx`. Failure-RPC and broader semantics remain unverified for this adapter.
- Shipping Web3Core Solidity compiled with existing solc0.8.26, and compiled ABI equals embedded shipping ABI. Artifact `TestResults/EthereumCqlCurrent/web3-storage.json` is regenerable, not committed.

## Exact next step

Arbitrum V2 MVP is now verified; continue Google, Cosmos blockchain, Polkadot and NEAR before deeper parity. BootLoader EvmProviderFactory.CreateArbitrum explicitly selects ArbitrumOASIS_Web3Core for HyperDrive V2; the custom adapter is the legacy-mode path. Reuse the V2 Web3Core contract/query implementation with authority-specific admission and native/Mongo-paired proof; document legacy gaps only.

## Reproduce native verification

```powershell
Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1 -Provider Polygon
```

The reusable EVM runner accepts an explicit provider and `-Federated`; the hub enrolls Polygon on5098. Existing Ethereum defaults remain unchanged. Both presets use the same parameterized native acceptance, generic C# walkthrough and HTTP host.

Reuses Ganache7.9.2/solc0.8.26/Node24.19.0, retained local chain31338/loopback18547 and root DPAPI encryption key. Polygon has its own contract, actor and three IDs in `LocalState/COSMIC/EvmCql/Polygon/demo-seed.json`. Native chain state stays in root `EvmCql/chain-data`. Pending setup fences replay; original state is preserved on uncertain outcomes. Shutdown flushes/stops the owned node; port18547 is absent after acceptance. No new installation, runtime, chain reset or public funds.

The first runner admission stopped before service/native work because its runtime path was incorrectly relative to the newly nested Polygon state folder. It now resolves the single runtime from the worktree root; no duplicate runtime or fallback path was created.

[Current status](CURRENT_STATUS.md) / [Ethereum setup](ETHEREUM_CQL_MVP.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Paired preset

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Polygon -Federated
```

Open http://127.0.0.1:5098/ after startup; add `-VerifyOnly` for bounded checks. C# and HTTP acceptance1/1 passes: all5 (Polygon3+Mongo2), subset4 (2+2), source provenance, forged access/unknown preset rejection and fail-whole-query when the source is deactivated. Native seed reuse across process restart passes without reseeding. Ethereum native1/1 and paired1/1 also pass after the common acceptance/runner changes.

`EvmMongoCqlNativeTests.cs` explicitly selects a concrete enrolled provider; there is no alternative query engine. Original native records are read-only during federation; a separate exactly-owned Mongo fixture is verified and removed after the host stops. Evidence: `TestResults/EthereumCqlCurrent/polygon-mongo-federation.trx`, per-provider logs and `polygon-mongo-browser.jpg`. Actual browser buttons verified5 then4 rows with Polygon/Mongo provenance. Final interactive1/1 includes32seconds of UI verification/shutdown, not query latency. Both owned services stopped, original chain/seed retained, exact temporary Mongo DB validated/dropped and ready/pending markers removed. No hosted/mainnet, finality or distributed snapshot claim.

Launcher routing now verifies8single profiles/16plans,9paired profiles/18plans,7paired rejections and unchanged listeners. The initial common-fixture build caught a C# variable-name collision before any service/native run; the collection-loop variable was renamed, then native/paired tests passed. No pending native outcome or workaround resulted.
