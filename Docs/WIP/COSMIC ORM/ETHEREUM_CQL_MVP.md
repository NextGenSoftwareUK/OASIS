# Ethereum CQL MVP

Updated 29 October 2026. Stage: implementation and initial controls; demo is not ready.

## Current evidence

- Ethereum provider implements the common `ICosmicQueryStorageProvider` contract.
- The compiler intersects captured ID equality/membership with the trusted Ethereum record catalogue. Boolean AND/OR and bounded paging are admitted; at most 100 selected keys. No chain scan or managed business filtering.
- Native lookups use the existing contract and authenticated encrypted-record decoder. Every lookup uses one observed block number. This is not a finality, reorg-immunity or cross-provider snapshot guarantee.
- Missing contract tuples are absent records. Corrupt payloads, foreign identities, RPC failure and cancellation fail the query without publishing partial rows.
- Provider build passes. Seven initial controls pass, zero skipped: captured membership/access intersection, authority mismatch/empty catalogue, five unsupported shapes, inactive adapter and pre-cancellation. The result covers compiler/admission, not successful RPC execution.
- Evidence: `TestResults/EthereumCqlCurrent/ethereum-cql-controls.trx`. Evidence is regenerable and not committed.

## Exact next step

1. Add controlled RPC execution checks for successful multi-key reads at the pinned block and failure/no-partial publication.
2. Locate/reuse a cached local EVM runtime. Worktree Hardhat is absent and no listener was observed on18545. Do not reset or adopt unrelated chain state. Inventory any setup before creating it; persistent state belongs under `LocalState/COSMIC`.
3. Verify three native records, a two-record filtered query, then Mongo-paired5/4 C#/HTTP/browser through the existing demo helper and launcher. Only then mark the demo green.
4. Continue Polygon, Arbitrum, Google, Cosmos blockchain, Polkadot and NEAR MVPs before deeper parity.

## Reproduce initial controls

Use one provider build lease. Build Ethereum with `--no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false`, then run the maintained `EthereumOASIS.IntegrationTests` project with the same flags and `--filter FullyQualifiedName~EthereumCqlTests --logger 'trx;LogFileName=ethereum-cql-controls.trx' --results-directory TestResults/EthereumCqlCurrent`.

The older `EthereumOASIS.UnitTests` project fails compilation against removed APIs (nearby/NFT/wallet types and old category values). This was observed before any tests executed; it remains unchanged. New CQL tests reside in the maintained integration project. Both restores used only `C:/Users/david/.nuget/packages`, with no package downloads. No chain, database or listener was created.

[Current status](CURRENT_STATUS.md) / [breadth roadmap](CQL_DEMO_EXPANSION_ROADMAP.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).