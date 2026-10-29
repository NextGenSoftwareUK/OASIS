# Ethereum CQL MVP

Updated 29 October 2026. Stage: native local-chain query verified; Mongo-paired demo is not ready.

## Current evidence

- Ethereum provider implements the common `ICosmicQueryStorageProvider` contract.
- The compiler intersects captured ID equality/membership with the trusted Ethereum record catalogue. Boolean AND/OR and bounded paging are admitted; at most 100 selected keys. No chain scan or managed business filtering.
- Native lookups use the existing contract and authenticated encrypted-record decoder. Every lookup uses one observed block number. This is not a finality, reorg-immunity or cross-provider snapshot guarantee.
- Missing contract tuples are absent records. Corrupt payloads, foreign identities, RPC failure and cancellation fail the query without publishing partial rows.
- Provider build passes. Controls12/12 pass, zero skipped: initial seven plus five actual JSON-RPC/ABI cases (success, explicit absence, corrupt ciphertext, RPC failure and foreign identity). Each checks the pinned block and exact selected keys; later failures publish no partial rows. These use controlled transport, not a chain.
- Native chain acceptance1/1 passes on Ganache7.9.2, chain31338, loopback18547: three encrypted records, filtered two-record selection, zero transactions during queries. A second full stop/start run reuses the same contract, original three IDs and unchanged seed manifest, passing1/1 without reseeding. This proves local EVM behaviour, not hosted Ethereum/mainnet finality.
- Evidence: `TestResults/EthereumCqlCurrent/ethereum-cql-controls.trx` and `ethereum-cql-native.trx`. Reports are regenerable and not committed.

## Exact next step

1. Add Mongo-paired5/4 C#/HTTP/browser using the existing generic federation helper and demo launcher. Do not mark the investor preset green before it passes.
2. Reuse `Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1`, original `LocalState/COSMIC/EvmCql` chain/manifest/key and the fixed runtime. No old chain is adopted or reset.
3. Keep unsupported business predicates explicit; full text/metadata/ordering/graph/parity remains outside this bounded identity profile.
4. Continue Polygon, Arbitrum, Google, Cosmos blockchain, Polkadot and NEAR MVPs before deeper parity.

## Reproduce initial controls

Use one provider build lease. Build Ethereum with `--no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false`, then run the maintained `EthereumOASIS.IntegrationTests` project with the same flags and `--filter FullyQualifiedName~EthereumCqlTests --logger 'trx;LogFileName=ethereum-cql-controls.trx' --results-directory TestResults/EthereumCqlCurrent`.

The older `EthereumOASIS.UnitTests` project fails compilation against removed APIs (nearby/NFT/wallet types and old category values). This was observed before any tests executed; it remains unchanged. New CQL tests reside in the maintained integration project. Both restores used only `C:/Users/david/.nuget/packages`, with no package downloads. No chain, database or listener was created.

[Current status](CURRENT_STATUS.md) / [breadth roadmap](CQL_DEMO_EXPANSION_ROADMAP.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
## Reusable local environment

Pinned ganache7.9.2, solc0.8.26 and ethers6.13.1 are installed once in LocalState/COSMIC/EvmRuntime; see the cleanup ledger for measured sizes and lock hash. Node24.19.0 is reused. Ganache reports its optional native uWS addon is unavailable for Node24 and explicitly uses its JavaScript transport; this third-party warning is retained in evm-current.err. No application/provider fallback exists.

The runner owns its local process and requests graceful stdin shutdown, flushes the persistent chain and awaits exit. A failed setup retains the original pending manifest and refuses replay. Encryption material is CurrentUser-DPAPI protected, never stored in docs. No public-chain funds or paid resources used. The native seed and all original state must be retained for later paired demos.
