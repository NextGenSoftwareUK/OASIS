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

## ORM features delivered 24 November (MongoDB, SQLite and Neo4j)

Each one was checked on all three real stores, in throwaway CI containers (never the shared Atlas cluster), and the WEB4 WebAPI built in the same run.

- **One SaveChanges for creates, updates and deletes (S02/S03).** All the changes commit in one native transaction, or none of them do. If one update is stale, the deletes in the same SaveChanges are rolled back too. Run 45424892347.
- **Update or delete everything a query matches, without loading it into a session (S16/S17).** `ExecuteUpdateAsync` and `ExecuteDeleteAsync` on the stateless session. A size limit stops oversized sets, and a row that changed after it was selected fails the whole statement. Run 45424908039.
- **Cascade delete (A06/A07, delete only).** `RemoveCascadeAsync` deletes a record and all of its descendants in one atomic batch. Run 45424767497.
- **Load a record's children on demand (A10).** `LoadChildrenAsync`. Run 45424767497.
- **Integer sequences and HiLo ID blocks (M08).** Native and atomic on each store, and checked with 20 callers at once. Run 45427097545.
- **Global query filters with an explicit bypass (Q62)**, for example a tenant rule on every query. A bypass never widens read access. Run 45430117947.
- **Auto flush (S20).** A session can write its pending changes before each async query, so the query sees its own writes. Run 45430117947.
- **Unique keys and lookup by key (M10).** A key is claimed in the same transaction as the save. A duplicate fails the whole save and writes nothing, and `FindByKeyAsync` looks records up by their key. Run 45435353282.
- **Children fixes (A05/A06/A07/A12).** A new child added to a parent's `Children` is saved with the parent. A child removed from a loaded collection can be deleted automatically if you turn that on. Children can be counted or checked without loading them. Run 45440013839.
- **Merge and statistics (S07/O13).** A disconnected copy can be merged onto the tracked record, and the session reports counts of queries and saves. Run 45437319138.
- **In CI now:** tenant-bound sessions (O14), the save-failed callback (S21), cancellation (S13), async streaming (Q64) and batch fetch (A14).

Checklist: 86 of 181 rows are still open, down from 107 this morning. Each remaining row now names its gap or blocker; the transaction rows (O02-O07) are blocked on session-held native transactions across reads and writes.

Also: WEB4 did not compile, because the avatar-password JSON filter from cee7dc1d was never committed (its file name matched the repo's `*secret*` ignore rule). Development fixed this in b66e3d4e, and this branch now uses that fix.

## Calling the API yourself

1. `POST /api/sandbox/session` with `{}` returns a visitor `jwtToken`.
2. `GET /api/data/query-cql/providers` lists providers and their preset requests.
3. `POST /api/data/query-cql` with a preset's `request` body, plus `Authorization: Bearer <token>` and a fresh `Idempotency-Key` header.

## Still open (not done, not claimed)

- HoloOASIS: cold-start reliability, private-data copies and live HyperDrive writes. The public read works.
- Providers whose own public APIs are down at startup (e.g. Chainflip HTTP 525, Blockscout timing out above 15s per address on 24 Nov) show as unavailable until the next restart or an operator recovery call.
- TRON: needs a TronGrid API key from David (see ACTIONS-FOR-DAVID.md).
- Remaining EF/NHibernate rows: see CQL_PARITY_CHECKLIST.md. General (non-preset) queries and writes on chain providers: see the archive.

## History

The previous detailed log (GPT agent, to 23 November) is in [Archive/CURRENT_STATUS_LOG_to_2026-11-23.md](Archive/CURRENT_STATUS_LOG_to_2026-11-23.md). The summary report is [FULL_STATUS_REPORT_2026-11-22.md](FULL_STATUS_REPORT_2026-11-22.md).
