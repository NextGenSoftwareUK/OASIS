# COSMIC / CQL status (plain English)

Updated 24 November 2026. Checked against the LIVE dev sandbox API `https://dev.api.web4.oasisomniverse.one` (`/api/data/query-cql`), signed in as a sandbox visitor, running every preset in the provider catalogue.

## Headline

- **102 providers return real records** for their full and filtered presets in the live sandbox (plus MongoDB, SQLite and Neo4j, which take your own data rather than presets).
- **14 of 15 priority providers are live.** Only TRON is blocked (needs a TronGrid key from David).
- All of this runs through the real provider classes (`*OASIS.Cql.cs`), not ONODE adapters.

## Priority providers

| Provider | Live sandbox demo |
|---|---|
| Holo | Yes: full preset 3 records, filtered preset 2 records |
| Solana | Yes: full preset 3 records, filtered preset 2 records |
| Ethereum | Yes: full preset 3 records, filtered preset 2 records |
| Polygon | Yes: full preset 3 records, filtered preset 2 records |
| Arbitrum | Yes: full preset 3 records, filtered preset 2 records |
| NEAR | Yes: full preset 3 records, filtered preset 2 records |
| EOSIO | Yes: full preset 3 records, filtered preset 2 records |
| SOLID | Yes: full preset 3 records, filtered preset 2 records |
| IPFS | Yes: full preset 3 records, filtered preset 2 records |
| ActivityPub | Yes: full preset 3 records, filtered preset 2 records |
| CosmosBlockChain | Yes: full preset 3 records, filtered preset 2 records (fixed 24 Nov: empty DNA fee/escrow placeholders no longer break startup) |
| Polkadot | Yes: full preset 3 records, filtered preset 2 records |
| TON | Yes: full preset 3 records, filtered preset 2 records |
| TRON | BLOCKED: not enrolled on Railway (CQL_TRON_PUBLIC_HEADER_QUERY_SOURCE_JSON and COSMIC_TRON_PUBLIC_READER_KEY unset). Anonymous TronGrid returned HTTP429 in the native test, so it needs a dedicated TronGrid API key from David. |
| Hashgraph | Yes: full preset 3 records, filtered preset 2 records (fixed 24 Nov: empty DNA query-policy placeholder no longer breaks startup) |

## Every provider with a working live demo

AWSS3, Abstract, ActivityPub, Akash, Algorand, Aptos, Arbitrum, Arweave, Avalanche, Axelar, AzureStorage, BNBChain, Base, Berachain, Bitcoin, Bittensor, BlockStack, Blockscout, BlueSky, Cardano, Celestia, ChainLink, Chainflip, Civic, ClickHouse, Cloudinary, Connext, Dapr, Decentraland, Discord, DuckDB, EAS, ENS, EOSIO, Eclipse, EigenLayer, Elrond, EspressoSystems, Ethereum, Fantom, Fhenix, Filecoin, Flow, GUN, GoogleCloud, Holo, Holonym, Hyperlane, IPFS, ImmutableZkEVM, IntelOpenVINO, LayerZero, LayerZeroV2, Linea, Livepeer, LocalFile, MapLibre, Matrix, Miden, Monad, NATSJetStream, NEAR, Nostr, Optimism, OracleObjectStorage, OrbitDB, Pinata, Polkadot, Polygon, PolygonID, PouchDB, ProofOfHumanity, PushProtocol, Radix, ReclaimProtocol, Ritual, Ronin, SEEDS, SOLID, Scroll, Sei, SelfProtocol, SignProtocol, Solana, Stargate, Starknet, Stellar, Subquery, Sui, Synapse, TON, Telos, TheGraph, TheSandbox, ThreeFold, Waku, WorldID, Wormhole, XRPL, Zcash, ZkSync, deBridge

## Calling the API yourself

1. `POST /api/sandbox/session` with `{}` returns a visitor `jwtToken`.
2. `GET /api/data/query-cql/providers` lists providers and their preset requests.
3. `POST /api/data/query-cql` with a preset's `request` body, plus `Authorization: Bearer <token>` and a fresh `Idempotency-Key` header.

## Still open (not done, not claimed)

- HoloOASIS: cold-start reliability, private-data copies and live HyperDrive writes. The public read works.
- Providers whose own public APIs are down at startup (e.g. Chainflip HTTP 525, Blockscout timing out above 15s per address on 24 Nov) show as unavailable until the next restart or an operator recovery call.
- TRON: needs a TronGrid API key from David (see ACTIONS-FOR-DAVID.md).
- Full EF/NHibernate parity, general (non-preset) queries and writes on chain providers: see the archive.

## History

The previous detailed log (GPT agent, to 23 November) is in [Archive/CURRENT_STATUS_LOG_to_2026-11-23.md](Archive/CURRENT_STATUS_LOG_to_2026-11-23.md). The summary report is [FULL_STATUS_REPORT_2026-11-22.md](FULL_STATUS_REPORT_2026-11-22.md).
