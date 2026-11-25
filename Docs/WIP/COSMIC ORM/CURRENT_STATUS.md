# COSMIC / CQL status (plain English)

Updated 25 November 2026. Checked against the LIVE dev sandbox API `https://dev.api.web4.oasisomniverse.one` (`/api/data/query-cql`), signed in as a sandbox visitor, running every preset in the provider catalogue.

## Headline

- **107 providers are in the live catalogue; 208 of 208 plain presets returned real records** in the 24 November sweep (plus MongoDB, SQLite and Neo4j, which use your own data).
- **Private paired (MongoDB + provider federation) presets:** 129 of 208 passed in a first sweep. The failures came from a test token expiring and from WEB4 redeploying mid-run, not from providers; failures re-run individually pass (Optimism, Linea and Scroll included). A clean full re-run is waiting for the Railway deploy queue to clear.
- **Checked in a real browser:** the CQL Playground returns HoloOASIS's three records.
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
- **Tenant-bound sessions (O14).** A tenant rule is applied to every query and cannot be bypassed, and writes to another tenant's records are refused. Run 45448347544.
- **Save-failed callback (S21), cancellation (S13), async streaming (Q64).** Run 45443081733.
- **Batch fetch (A14), filtered include (A09) and lazy loading (A11).** Runs 45443657726 and 45449694421.
- **Synchronous forms of all of the above (S14).** Run 45450233980.
- **Relationships (A01-A04).** One-to-many through any foreign-key metadata key, one-to-one (a unique key on the foreign key), and many-to-many through join records with a loader that skips the join rows. Runs 45452551451, 45455392584 and 45458370855.
- **Insert from a query (S18) and a session factory (O11).** Runs 45458370896 and 45458371046.
- **Seed data (M29), parent-cycle refusal (A17) and query interceptors (O13).** Runs 45461052081, 45463218586 and 45461718171.

Development has these batches up to 4b9b0d70, and the docs are on master.

Checklist: 64 of 181 rows are still open, down from 107 this morning. Each remaining row now names its gap or blocker; the transaction rows (O02-O07) are blocked on session-held native transactions across reads and writes.

Also: WEB4 did not compile, because the avatar-password JSON filter from cee7dc1d was never committed (its file name matched the repo's `*secret*` ignore rule). Development fixed this in b66e3d4e, and this branch now uses that fix.

## Sandbox page fixes waiting to go public

All on Web4Site branch `work/cosmic-cql`; promoting it to the public site needs David's approval (ACTIONS-FOR-DAVID):

- 6930aff removes 49 laptop-only `pwsh` launcher steps from the public page.
- f301621 adds "Prepare my sample records", so the built-in MongoDB, SQLite and Neo4j presets return rows for a new visitor (before, they returned none). Verified against the live dev API.
- 3099bf0 hides 47 `127.0.0.1` local-demo links for public visitors and bumps the changed scripts' cache versions.

## Calling the API yourself

1. `POST /api/sandbox/session` with `{}` returns a visitor `jwtToken`.
2. `GET /api/data/query-cql/providers` lists providers and their preset requests.
3. `POST /api/data/query-cql` with a preset's `request` body, plus `Authorization: Bearer <token>` and a fresh `Idempotency-Key` header.

## Still open (not done, not claimed)

- HoloOASIS: a startup now logs each phase. On 24 November the cell was ready 98s after boot (96s of it the Holochain network join), inside the 480s budget. Live HyperDrive writes to HoloOASIS belong to the HyperDrive session.
- Ask Your Data: the tab is built, but WEB6 dev has no free model route for demo visitors (`WEB6_FREE_ROUTE_UNAVAILABLE`). It needs a key from David (ACTIONS-FOR-DAVID).
- Docs-only pushes redeploy WEB4-WEB10 (7 deploys queued at once on 25 November). Fixing it needs Railway Watch Paths set by David (ACTIONS-FOR-DAVID).
- Providers whose own public APIs are down at startup (e.g. Chainflip HTTP 525, Blockscout timing out above 15s per address on 24 Nov) show as unavailable until the next restart or an operator recovery call.
- TRON: needs a TronGrid API key from David (see ACTIONS-FOR-DAVID.md).
- Remaining EF/NHibernate rows: see CQL_PARITY_CHECKLIST.md. General (non-preset) queries and writes on chain providers: see the archive.

## History

The previous detailed log (GPT agent, to 23 November) is in [Archive/CURRENT_STATUS_LOG_to_2026-11-23.md](Archive/CURRENT_STATUS_LOG_to_2026-11-23.md). The summary report is [FULL_STATUS_REPORT_2026-11-22.md](FULL_STATUS_REPORT_2026-11-22.md).
