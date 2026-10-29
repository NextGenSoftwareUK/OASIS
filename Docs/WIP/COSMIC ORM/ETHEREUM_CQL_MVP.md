# Ethereum CQL MVP

Updated 29 October 2026. Stage: local native + Mongo-paired C#/HTTP/browser MVP verified. Hosted/mainnet and deeper parity remain unverified.

## Current evidence

- Ethereum provider implements the common `ICosmicQueryStorageProvider` contract.
- The compiler intersects captured ID equality/membership with the trusted Ethereum record catalogue. Boolean AND/OR and bounded paging are admitted; at most 100 selected keys. No chain scan or managed business filtering.
- Native lookups use the existing contract and authenticated encrypted-record decoder. Every lookup uses one observed block number. This is not a finality, reorg-immunity or cross-provider snapshot guarantee.
- Missing contract tuples are absent records. Corrupt payloads, foreign identities, RPC failure and cancellation fail the query without publishing partial rows.
- Provider build passes. Controls12/12 pass, zero skipped: initial seven plus five actual JSON-RPC/ABI cases (success, explicit absence, corrupt ciphertext, RPC failure and foreign identity). Each checks the pinned block and exact selected keys; later failures publish no partial rows. These use controlled transport, not a chain.
- Native chain acceptance1/1 passes on Ganache7.9.2, chain31338, loopback18547: three encrypted records, filtered two-record selection, zero transactions during queries. A second full stop/start run reuses the same contract, original three IDs and unchanged seed manifest, passing1/1 without reseeding. This proves local EVM behaviour, not hosted Ethereum/mainnet finality.
- Evidence: `TestResults/EthereumCqlCurrent/ethereum-cql-controls.trx` and `ethereum-cql-native.trx`. Reports are regenerable and not committed.

## Exact next step

1. Move to Polygon MVP breadth, reusing the existing EVM-family provider and this fixed runtime where technically suitable. Do not deepen green Ethereum features without a reproduced bug.
2. Reuse `Scripts/TestHosts/Run-CosmicEthereumCqlNative.ps1`, original `LocalState/COSMIC/EvmCql` chain/manifest/key and the fixed runtime. No old chain is adopted or reset.
3. Keep unsupported business predicates explicit; full text/metadata/ordering/graph/parity remains outside this bounded identity profile.
4. Continue Polygon, Arbitrum, Google, Cosmos blockchain, Polkadot and NEAR MVPs before deeper parity.

## Reproduce initial controls

Use one provider build lease. Build Ethereum with `--no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false`, then run the maintained `EthereumOASIS.IntegrationTests` project with the same flags and `--filter FullyQualifiedName~EthereumCqlTests --logger 'trx;LogFileName=ethereum-cql-controls.trx' --results-directory TestResults/EthereumCqlCurrent`.

The older `EthereumOASIS.UnitTests` project fails compilation against removed APIs (nearby/NFT/wallet types and old category values). This was observed before any tests executed; it remains unchanged. New CQL tests reside in the maintained integration project. Both restores used only `C:/Users/david/.nuget/packages`, with no package downloads. During those initial compiler controls no chain, database or listener was created; the later native environment is recorded below.

[Current status](CURRENT_STATUS.md) / [breadth roadmap](CQL_DEMO_EXPANSION_ROADMAP.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
## Reusable local environment

Pinned ganache7.9.2, solc0.8.26 and ethers6.13.1 are installed once in LocalState/COSMIC/EvmRuntime; see the cleanup ledger for measured sizes and lock hash. Node24.19.0 is reused. Ganache reports its optional native uWS addon is unavailable for Node24 and explicitly uses its JavaScript transport; this third-party warning is retained in evm-current.err. No application/provider fallback exists.

The runner owns its local process and requests graceful stdin shutdown, flushes the persistent chain and awaits exit. A failed setup retains the original pending manifest and refuses replay. Encryption material is CurrentUser-DPAPI protected, never stored in docs. No public-chain funds or paid resources used. The native seed and all original state must be retained for later paired demos.

## Launch and show the paired demo

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Ethereum -Federated

# Native and SDK/HTTP checks, then stop without an interactive browser host:
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Ethereum -Federated -VerifyOnly
```

Open http://127.0.0.1:5097/ after startup. **Query all records** returns Ethereum3 + Mongo2 =5 physical rows; **Query two-record subset** returns Ethereum2 + Mongo2 =4. Identical IDs retain both provider copies. Query selection narrows each server-owned catalogue and cannot enlarge access.

The generic [C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs) builds one capability-aware federation plan and calls HolonManager. The [HTTP host](../../../Examples/CosmicCqlDemo/IdentityFederationApiHost.cs) calls the same walkthrough. Tests link these source files directly. Browser callers send only `{ "preset": "all" }` or `{ "preset": "subset" }` to `POST /api/cql/federation`; forged actor fields and unknown presets are rejected. Source failure returns503 without partial rows.

Native Ethereum/Mongo SDK + HTTP acceptance1/1 passes. Actual browser buttons verified5 then4 rows and both provider labels. Rebuilt browser verification also confirms the reproduced shared loading-message encoding bug is fixed at its source. Screenshot: `TestResults/EthereumCqlCurrent/ethereum-mongo-browser.jpg`; report: `ethereum-mongo-federation.trx`. The interactive test duration includes the UI verification and explicit shutdown, not query latency.

Launcher discovery verifies8single profiles/16plans,8paired profiles/16plans and6unsupported-pair rejections with unchanged listeners. Both owned EVM/Mongo services stopped after verification, ready/pending journals removed, exact temporary Mongo fixture validated and dropped. Retained native chain/seed/key are reusable. No mainnet/public-chain, hosted Ethereum, arbitrary content query or full parity claim.
