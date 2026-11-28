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
- Query modes in the sandbox: JSON CQL, SQL-like, and free text (AI). SQL-like and free text are now API endpoints on Development too (phase 2a, done).

## Native data import and virtual holon mappings

- Native data import (turn another app's data into holons): **8/234** providers (MongoDB, SQLite, Neo4j, Azure Cosmos DB, SOLID, ActivityPub, IPFS, Solana). Shared machinery, stable ids, convention mapping, saved encrypted connections, API, STAR CLI and sandbox are done.
- Virtual holon mappings (query native data in place): **3/234** providers (MongoDB, SQLite, Neo4j), and only on whitelisted sandbox demo sources.

## Roadmap: next phases

Recorded at the top of `Docs/WIP/COSMIC ORM/CQL_COMPLETION_ROADMAP.md`.

1. Phase 1: COSMIC ORM parity checklist to 164/164.
2. Phase 2a: **done.** SQL-like and free-text endpoints (`POST /api/data/query-cql/sql` and `/ask`) are on Development, and the SDK, STAR CLI, docs and sandbox page use them. Production needs the next promotion.
3. Phase 2b: every parity feature on all 234 providers (native where possible, clearly flagged fallback otherwise), tracked as a provider x feature table.
4. Phase 2c: native data import and virtual holon mappings on every provider, and virtual mappings on any source the user maps.

## Hosting and cost

- Railway runs WEB4 to WEB10 in production, development and staging. Neo4j's Railway deployments are stopped (volume kept); HyperDrive fail-over routes around it.
- All WEB services: `DOTNET_gcServer=0`, `DOTNET_GCConserveMemory=5`, app sleeping, Watch Paths that skip docs, memory limits (WEB4 3 GB, WEB5/6 1 GB, WEB7 to WEB10 0.5 GB).
- Production memory: about 4.6 GB to about 2.5 GB.
- WEB4 sandbox sidecars can now run on demand (stopped after 10 idle minutes). Development measured 1,481 MB to 974 MB.
- Next (proposed, phase 2d): host profiles and per-provider connection modes in the DNA, and providers on free tiers. See `Docs/Devs/ONODE_HOST_PROFILES_AND_PROVIDER_HOSTING.md`.

## Design recommendations (26 Nov 2026)

These came out of a review of the OASIS design. David reports that 4, 5 and 6 are already built; the others are open roadmap items.

| # | Recommendation | Status |
|---|---|---|
| 1 | Publish a provider capability matrix through the API: what each provider supports natively and what uses the fallback, so developers and HyperDrive routing never assume a feature works where it does not. The phase 2b provider x feature table becomes this API. | Open |
| 2 | Tier the providers: a certified gold tier (full storage suite, CQL, replication, monitored in CI), with the rest labelled silver or bronze, and keep removing fake implementations. | Open |
| 3 | Make consistency explicit per route and provider (atomic, eventually replicated through the outbox, or final only once on chain), and let callers choose whether to wait for replication. | Open |
| 4 | Route by cost and speed as well as health, so routine reads never land on an expensive or slow chain. | Done (per David) |
| 5 | Encrypt by default on public providers (chains, IPFS), with clear key management and recovery. | Done (per David) |
| 6 | Report where every result came from: which provider answered, native or fallback, fresh copy or replica. | Done (per David) |
| 7 | Make SQL the front door: lead the docs and SDK with the SQL-like endpoint, JSON CQL for power users, free text for AI. | Endpoints done (phase 2a) |

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
- Railway Watch Paths on WEB4 to WEB10: done 26 Nov.
- GitHub Actions: check billing or capacity.
- TronGrid key for TRON, and accounts for the hosted-account providers.
- Cloud workers: give them the git identity line and the claims and CI rules.


## Verified continuation — 28 November 2026

Current provider baseline is123/163storage/specialized project rows (26sync+26async), within238tracked provider projects/75non-storage rows; historical26Nov denominators above are retained. This is not123current hosted/all-interface certifications. Global HyperDrive v2 gates remain open.

OPORTAL current source inventory45/45is reconciled, not45completed screens. Selected new native/browser continuation5/5: recovery46123580498(1), populated Map/Launcher46126474607(2), migration46130012906(1), ONET real reads/local start-stop46133501110(1). Separate real SDK/API OAPP missing-detail contract46132041520passed1/1. Current SDK delivery46125573999is3/3; read-only28Novrefresh confirms source heads and registry versions unchanged, so no accepted bundle/native reruns. Existing npm access actions A1/A2 remain open.

Holon Diff cold history/typed creation pass on current source, but first real metadata publication fails;46134706515is diagnosing the result. No comparison/seal acceptance, whole-portal100%or all-provider reliability claim. Paid billing/email/hosted/device gates remain scoped in the existing owner list; no new owner action. Shared Atlas test operations0; held535/543untouched. CI-owned resource cleanup receipts retained; laptop32.81GiBfree, owned temporary Git indexes0, no local downloads/env/build/database/results. Accepted OAPP/ONET source is staged for the shared Development batch after09:58CQL integration, not yet recorded as deployed.


2026-11-28 OPORTAL follow-through: selected new native/browser8/8(not45whole screens). Geo46142470160and actual MapLibre/WebGL2controls46146049497accepted, all owned cleanup; OPORTALdev830a97e6. App-labelled natural-place visits remain unpromoted/public-catalogue blocked. SDK JSON/gRPC/GraphQL map search repair46ce43e9image built46146213659, native46148591025pending. Storage baseline123/163within238projects unchanged; hosted/payment/device/remote-runtime and global HyperDrive gates open. No accepted suite replay, shared Atlas operation or new owner action.


### 2026-11-28 14:00 UTC — accepted search transports and manual map visit

Native46151924276passed actual SDK REST/source-proto gRPC/GraphQL map search3/3 on immutable sourcee60162c2aa1f94298a7cac0d1f8fcedd663df15f and digestsha256:0d54d74cb87f16ba9f111f66f49c6757fe7d4d18a25ec253424c34d5b2b0eae6. Native46152223451passed manual Map Visit1/1 with real acknowledged save, independent history/stats and popup close. Selected new journeys now9/9; OAPP missing-detail1/1and search transports3/3remain separate API contracts, not additional completed screens. No provider credit or accepted-suite rerun. Explicit Core collection tariffs and buffered gRPC logical-start invariants corrected; Core merge2d076f7cb13d021f3e2ae3aeed05d880d49cd749 preserves peerfa4ea2e2. Source-qualified files unchanged in scoped integration; current merged graph/hosted deployment acceptance separate.

SDK6795c431c3c3f0eccec6d48215bfce38fa8826b4 bodyParam generation now matches7actual controller bindings. Local actualHTTP wire regression1/1covers8request shapes; browser delivery46152132189passes3wire checks, existing guards and cleanup, retaining WEB5/WEB6 blobs. Portal dev0a3c854185f2b9ae669dcc07733f661a4ae38e22 integrated; npm/hosted gates open.

Receipts15265348826(4470B/SHAb4cb4f812152c6c62f3ff6a588a90167064fa6c1cd623fb4bdffec405e59b9a7) and15266098641(4335B/SHA215579cd70b553bf426dcb32ec9205e3b0e305b084e76e4892c1cb18a25b49ef) read in RAM: full-package container absent/dataWrites0 and5/5combined cleanup flags, APIexit0. No local download/extraction/install/build/database;31.9GiBfree/0temporary indexes. Failed diagnostic receipts also retained; no independent probe/cache directory absence asserted beyond CI cleanup step success. No new David action/shared Atlas operations; protected535/543and worker holds unchanged.


### 2026-11-28 — actual clan writer fix qualified

Native46156859773passed1/1ordinary member self-leave, absence of owner edit controls, acknowledged membership change, independent owner/membership preservation and ModifiedByAvatarId equal to the actual authenticated member, plus normal close. Exact sourcee6b5dfdf2c3bfab2fff3bb090506388253a936b0/Core43a70495e4b5722b0f5c0fc148b590e119132013/imagesha256:e6bf4175cb70a886c26292b21d8f379eb0a073581d5a0faa68e980647c2b0d2b/portal0a3c854185f2b9ae669dcc07733f661a4ae38e22. Actual shipping MCP compiler46156654176passed1/1against the same paired Core/WEB6 sourcece6d258c8fa425d84c7c49eb934ee81a540e2b3f; native MCP/gRPC/GraphQL actor behavior is not claimed from compilation. Core merge4b4ee0aa2c154630d1f14247fbf8df10ded2622e preserves peer CQLe31af3c198d4e8374a1b3dabc269d3d75485f428; actor source blobs match the frozen qualification.

Selected new journeys11/11; separate OAPP API1/1, map search transports3/3, native SDK purpose1/1 and source/bundle wire checks retain their scopes. No provider baseline increment, accepted suite replay, production/shared Atlas test or new David action. Actor binding adds no database lookup; permissions, billing/settlement and owner semantics remain. Missing Core actor gives CLAN_ACTOR_REQUIRED; empty owner gives CLAN_OWNER_REQUIRED. Source-compatible optional actingAvatarId must be supplied by trusted Core callers; public HTTP/protobuf/GraphQL/MCP argument shapes unchanged.

Native artifact15267384514(4352B/SHAeecb6d9f700da86ef41e5b719c2ac8724c5476958ad5fa3f10bf0e3d69a811da) and compiler artifact15267858143(353B/SHAac20849f349f68518aabd60f828ab91b2b2aa7c4da0df95c4d2295478c7d1b39) read in RAM. Native owned containers/network/database volumes removed(all5Startupflags true); compiler ignored outputs cleaned/owned cache absent. Initial writer failure46154472548cleanup3857Breceipt15266803696also true. No historical audit record rewritten, no protected535/543replay, no shipping DNA change or local runtime/download/database/results. Current combined-graph/deployed settings acceptance and legacy treasury atomicity remain separate.
