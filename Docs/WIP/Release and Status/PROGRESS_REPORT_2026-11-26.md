# OASIS progress report: 26 November 2026

Progress is written as done/total. Sources: the status docs and checklists on Development (`CQL_PARITY_CHECKLIST.md`, `HyperDrive-Providers/CURRENT_STATUS.md`, `PROVIDER_MATRIX.md`, `NATIVE_DATA_IMPORT.md`) and the sessions' reports.

## Summary

| Area | Progress |
|---|---|
| COSMIC ORM parity (Entity Framework / NHibernate features, native on MongoDB, SQLite and Neo4j) | **130/164** (79%) |
| HyperDrive native storage (26-check storage suite on the real backend, in CI) | **about 115/234** providers |
| CQL sandbox sources returning real records (dev) | **107** sources available |
| Priority CQL providers working | **14/15** (TRON needs a TronGrid key) |
| Native data import | **8/234** providers |
| Virtual holon mappings | **3/234** providers (demo sources only) |
| SDK route coverage | WEB4 **849/849**, WEB5 **639/639**, WEB6 **125/125** |

## COSMIC ORM (Entity Framework / NHibernate parity)

The checklist is `Docs/WIP/COSMIC ORM/CQL_PARITY_CHECKLIST.md`. A row counts as done only when it passes natively in CI on all three stores.

| When | Done |
|---|---|
| 22 Nov 23:34 | 51/164 |
| 24 Nov 12:00 (Claude sessions take over) | 82/164 |
| 26 Nov | **130/164** |

- Done since the handover: about 48 rows in roughly a day and a half. Part of the first jump came from a review that confirmed rows GPT had built but never credited.
- Recently finished and merged: O18, M24, O17, M19, M23, A16, Q60, A13, Q47, Q61, Q30.
- Committed and in CI: Q55 (CASE in projections), Q56 (user-defined functions), A14 (futures). If they pass: 133/164.
- What's left is the hardest part: M01 (plain C# classes with no base class), M04 (full NHibernate XML mapping), inheritance and relationship mapping, and rows marked "deferred by owner" or with a recorded blocker.
- Estimate: 2 to 4 working days to 164/164 once the sessions resume.

## HyperDrive and providers

- Native storage: about **115/234** providers pass the 26-check suite on their real backends.
- Added this round: BlockStack (Gaia, signed auth), Flow, Ronin, ImmutableZkEVM, Cardano, Starknet, Marqo, Druid, Pinot, Fhenix, Elrond/MultiversX, Basechain, Eclipse, and from cloud worker A: Telos, SEEDS, Dapr, GUN, Matrix, OrbitDB, Arweave, Scuttlebutt, Appwrite, Pinecone, Tableland, Celestia, Ceramic and BlueSky. KeyDB and Dragonfly now run the full suite.
- The EVM suite runs in CI (15 chains pass all 30 checks).
- No more fake storage: 22 providers that pretended to save data now return a clear "no record store" error (PolygonID, Biconomy, EAS, Pimlico, Polybase, Privy, XMTP, ZeroDev, Connext, ChainLink and others). A search of every provider found no more.
- Being given real storage: Aztec (code done, 10/10 unit tests) and Miden (Node worker started).
- Dev replication: I1 (Neo4j to SQLite holds) cleared; I2 (IPFS catalogue key) set to the correct path; DNA copies synced, including the AccessLog block.
- Next: the HyperDrive v2 release gates (fail-over, replication and load-balancing gaps), starting with a `ProviderSwitcher` fix. The remaining providers need hosted accounts (see ACTIONS-FOR-DAVID).

## CQL and the WEB4 sandbox

- 107 sources are available in the dev sandbox, including Blockscout and HoloOASIS.
- Priority providers: 14/15. TRON needs a TronGrid key.
- Recent fixes: Blockscout timeout, CQL diagnostics.
- Query modes in the sandbox: JSON CQL, SQL-like, and free text (AI). Only JSON CQL is an API endpoint today. The other two run in the browser (`cql-text.js`, `cql-ask.js`). Making them API endpoints is now phase 2a (below).

## Native data import and virtual holon mappings

- Native data import (turn another app's data into holons): **8/234** providers (MongoDB, SQLite, Neo4j, Azure Cosmos DB, SOLID, ActivityPub, IPFS, Solana). Shared machinery, stable ids, convention mapping, saved encrypted connections, API, STAR CLI and sandbox are done.
- Virtual holon mappings (query native data in place): **3/234** providers (MongoDB, SQLite, Neo4j), and only on whitelisted sandbox demo sources.

## Roadmap: next phases

Recorded at the top of `Docs/WIP/COSMIC ORM/CQL_COMPLETION_ROADMAP.md`.

1. Phase 1: COSMIC ORM parity checklist to 164/164.
2. Phase 2a (do first): SQL-like and free-text queries as WEB4 API endpoints (`/api/data/query-cql/sql` and `/ask`), plus the SDK, Postman, docs, Learn site and STAR CLI. **In progress now.**
3. Phase 2b: every parity feature on all 234 providers (native where possible, clearly flagged fallback otherwise), tracked as a provider x feature table.
4. Phase 2c: native data import and virtual holon mappings on every provider, and virtual mappings on any source the user maps.

## Docs, SDKs and sites (done)

- WEB4, WEB5 and WEB6 JavaScript SDKs cover every live route; WEB7 to WEB10 SDKs have `health()`. Not yet published to npm.
- Postman: a full WEB4 collection (849 requests) and its generator; the STAR collection fixed (639 requests).
- Docs site: new pages for CQL Queries, CQL Sandbox, Subscription & Usage, Health Checks, the WEB6 SDK and WEB7 to WEB10 SDKs; REST reference uses the real routes.
- Learn site: Module 18, COSMIC ORM & CQL.
- "Smart Bricks" rename done across the docs and sites.

## Working arrangements

- Work is claimed on the `claims` branch (`WORK-CLAIMS.md`) so local Claude sessions, cloud workers and GPT agents never overlap.
- Pushes to Development are batched, at most once every 30 minutes.
- CI queue rule: count only runs from the last 12 hours (22 stale "queued" entries can't be cancelled). Batch pushes under 35 queued, extra runs under 20.
- Sessions take their recommended option on decisions and only ask for secrets, accounts or payments.
- The session keeper nudges idle sessions every 20 minutes and paused them all when weekly usage reached 90%. They resume after the weekly reset (Sunday 15:00 UTC).

## Speed compared with GPT

- COSMIC ORM: 82/164 to 130/164 in about a day and a half, against 1 or 2 rows over several days before.
- HyperDrive: about 88 to about 115 providers in two days.
- Main brakes: the laptop's local builds, GitHub Actions capacity, and the Railway deploy queue.

## Needs David

- Publish the updated WEB4 to WEB10 SDKs to npm.
- Railway: set Watch Paths on WEB4 to WEB10 to skip `Docs/**` and `*.md`.
- GitHub Actions: check billing or capacity.
- TronGrid key for TRON, and accounts for the hosted-account providers.
- Cloud workers: give them the git identity line and the claims and CI rules.
