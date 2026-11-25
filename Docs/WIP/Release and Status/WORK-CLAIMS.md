# Work claims (who is working on what)

Every session, local or cloud, that works in the OASIS repo claims its work here first, so that no two sessions do the same thing.

## How to claim

Claims live on the **`claims` branch**, not on Development. Railway redeploys WEB4-WEB10 on every Development push, so claim-only pushes there restart the dev servers and fill the deploy queue.

1. `git fetch origin claims Development` and read `WORK-CLAIMS.md` on `origin/claims`, plus the status docs on Development:
   - `Docs/WIP/HyperDrive-Providers/CURRENT_STATUS.md`
   - `Docs/WIP/COSMIC ORM/` (the checklist and per-provider `*_CQL_MVP.md` files)
   - `Docs/WIP/Release and Status/ACTIONS-FOR-DAVID.md`
2. Pick an item that is not claimed (or whose claim is older than 24 hours with no commits since; mark that one `STALE` and take it over).
3. On a checkout of `claims`, add one row to the table, commit only this file with a message that starts `claim: <item> [skip ci]`, and push to `claims` **before** writing any code. If the push is rejected, pull, re-check, and pick again if someone else claimed it.
4. Work on your own branch (`work/<session>-<item>`), and merge to Development as usual. Batch your commits: one Development push per finished item, never a push just to retrigger CI.
5. When the item is done or blocked, change its row on `claims` (`DONE <commit>` or `BLOCKED: see ACTIONS-FOR-DAVID`) and take the next one.

Keep claims small (one provider, one checklist row or one feature), so that work spreads across sessions.

## Claims

| Item | Area | Session (local/cloud + name) | Branch | Claimed (UTC) | Status |
| --- | --- | --- | --- | --- | --- |
| Cardano native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | DONE 988b4bf4 (cardano-native-storage.yml green) |
| Starknet native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | DONE 988b4bf4 (starknet-native-storage.yml green) |
| Marqo native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | DONE 720c268f (matrix green) |
| Druid native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | DONE 988b4bf4 (druid-native-storage.yml green) |
| Pinot native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | IN PROGRESS |
| IPFS catalogue root pointer (dev replication-source fault) | HyperDrive replication | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | CODE DONE c2da27e5 (CI green); Railway dev DNA key applied at the next Development merge |
| ChainLinkOASIS / Connext / read-only chain adapters classification | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | DONE 897db2c5 |
| Provider TestHarness projects build (all 172) | Providers/TestProjects | local: testharness-build-fix | work/testharness-build-fix | 2026-11-24 | DONE 603b3222 |
| AlgorandOASIS native storage (shared suite 26/26 on LocalNet) | Providers/Blockchain | local: testharness-build-fix | work/algorand-native-storage | 2026-11-24 23:30 | IN PROGRESS |
| COSMIC Q09 string transforms (Replace / Trim(char)) | COSMIC ORM | local: COSMIC ORM roadmap (work/cosmic-cql-ton-public session) | work/cosmic-cql-ton-public | 2026-11-25 00:20 | DONE 45515662485 |
| COSMIC Q65 compiled query cache | COSMIC ORM | local: COSMIC ORM roadmap (work/cosmic-cql-ton-public session) | work/cosmic-cql-ton-public | 2026-11-25 00:20 | DONE 45515662485 |
| COSMIC ORM checklist rows (all of CQL_PARITY_CHECKLIST.md) | COSMIC ORM | local: COSMIC ORM roadmap (work/cosmic-cql-ton-public session) | work/cosmic-cql-ton-public | 2026-11-25 00:20 | IN PROGRESS (claim one row at a time) |
| Live sandbox federation (private paired MongoDB + provider) presets sweep; Optimism/Linea/Scroll federation unavailable | CQL sandbox | local: CQL sandbox (cql-sandbox session) | work/cql-sandbox-providers | 2026-11-25 00:40 | IN PROGRESS |
| Web4Site CQL Playground: remove stale pwsh / C:/Source/OASIS-cosmic local-demo instructions | CQL sandbox (Web4Site repo) | local: CQL sandbox (cql-sandbox session) | Web4Site work/cosmic-cql | 2026-11-25 00:40 | IN PROGRESS |
| CI build gate for all provider TestHarness projects (stop harness rot) | CI / Providers/TestProjects | local: testharness-build-fix | work/testharness-ci-gate | 2026-11-25 00:50 | IN PROGRESS |
| COSMIC O15 as-of history reads | COSMIC ORM | local: COSMIC ORM roadmap (work/cosmic-cql-ton-public session) | work/cosmic-cql-ton-public | 2026-11-25 00:35 | IN PROGRESS |
| TelosOASIS native storage (reuse EOSIO Antelope fixture) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-telos | 2026-11-25 01:10 | DONE 48add53f (antelope-family-native-storage.yml run 45518765561 green) |
| CQL startup head-of-line blocking: a slow HoloOASIS activation keeps IPFS/NEAR/Neo4j/Polkadot/Polygon/SOLID/Solana/TON NOT_READY on live dev (serial canonical lane) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-activation-lane | 2026-11-25 00:38 | DONE 24688f3a (CqlActivationLaneTests pass) |
| COSMIC O18 EF6 databinding/local-view integration | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-o18 | 2026-11-25 02:00 | DONE 2f96062 (Core) CI 45519967748 |
| SEEDSOASIS native storage (Antelope fixture) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-seeds | 2026-11-25 00:35 | DONE 48add53f (antelope-family-native-storage.yml run 45518765561 green) |
| DaprOASIS native storage (daprd sidecar + Redis state store) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-dapr | 2026-11-25 00:40 | DONE f688e203 (popular-providers-native-storage run 45519314086 green) |
| ZcashOASIS native storage (regtest zcashd + Kubo, Bitcoin pattern) | HyperDrive providers | local: testharness-build-fix | work/zcash-native-storage | 2026-11-25 02:20 | IN PROGRESS |
| COSMIC A15 set (HashSet) owned collections | COSMIC ORM | local: COSMIC ORM roadmap (work/cosmic-cql-ton-public session) | work/cosmic-cql-ton-public | 2026-11-25 02:10 | IN PROGRESS |
| GUNOASIS native storage (GUN relay) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-gun | 2026-11-25 00:45 | DONE a4391671 (popular-providers-native-storage run 45520161955 green) |
| MatrixOASIS native storage (Synapse, room state events) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-matrix | 2026-11-25 00:50 | IN PROGRESS |
| ElrondOASIS (MultiversX) native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:46 | IN PROGRESS |
| FhenixOASIS (EVM) native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:46 | IN PROGRESS (evm-native-storage.yml) |
| ArweaveOASIS native storage (arlocal) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-arweave | 2026-11-25 01:00 | IN PROGRESS |
| COSMIC M24 model defaults, computed and generated properties | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-m24 | 2026-11-25 01:10 | DONE 2f96062 (Core) CI 45524783092 |
| OrbitDBOASIS native storage (@orbitdb/core + Helia service) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-orbitdb | 2026-11-25 01:10 | IN PROGRESS |
| RootstockOASIS live public CQL source (anonymous public-node.rsk.co balance reads, BNB pattern) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-rootstock | 2026-11-25 01:04 | DONE (code) 24688f3a; live pending Railway enrollment by CQL sandbox session (CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md) |
| ScuttlebuttOASIS native storage (ssb-db2 feed service) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-scuttlebutt | 2026-11-25 01:25 | IN PROGRESS |
| StoryProtocolOASIS live public CQL source (anonymous mainnet.storyrpc.io finalized balance reads, chain 1514) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-storyprotocol | 2026-11-25 01:11 | DONE (code) 4142d82b36; live pending Railway enrollment by CQL sandbox session (CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md) |
| LensOASIS live public CQL source (anonymous rpc.lens.xyz finalized balance reads, Lens Chain 232) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-lens | 2026-11-25 01:13 | DONE (code) 4142d82b36; live pending Railway enrollment by CQL sandbox session (CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md) |
| AppwriteOASIS native storage (self-hosted Appwrite in Docker) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-appwrite | 2026-11-25 01:20 | IN PROGRESS |
| TablelandOASIS native storage (@tableland/local validator + Hardhat) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-tableland | 2026-11-25 01:20 | IN PROGRESS |
| COSMIC O17 model validation and diagnostics | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-o17 | 2026-11-25 01:22 | DONE 2f96062 (Core) CI 45526770753 |
| Nile native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| CloudflareD1 native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| NetlifyBlobs native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| MotherDuck native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| Cloudinary native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| DenoDeploy native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| FlyIO native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| Neo4jOASIS.Aura native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| LanceDB native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| Fauna native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| VercelKV native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| Polybase native storage (hosted only) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 01:30 | BLOCKED: see ACTIONS-FOR-DAVID (hosted-only providers) |
| PineconeOASIS native storage (Pinecone Local container) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-pinecone | 2026-11-25 01:30 | IN PROGRESS |
| QuestDBOASIS live public CQL source (port unmerged symbol adapter 9e67263e..35d38833; read-only demo.questdb.io) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-questdb | 2026-11-25 01:24 | DONE (code) 25a128c2aa; live pending Railway enrollment by CQL sandbox session (CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md) |
| Live CQL for self-hostable DB providers and keyed indexer APIs (needs Railway dev demo DBs / free keys) | CQL sandbox (WEB4) | cloud: cloud-cql-C | n/a | 2026-11-25 01:28 | BLOCKED: see ACTIONS-FOR-DAVID (Live CQL sandbox: demo databases and indexer keys) |
| ONODE Manager hardening, Manager tests and setup error reporting | ONODE/ONODEManager | local: ONODE Manager improvement loop | ONODEManager main/Development | 2026-11-25 01:31 | DONE ONODEManager 33c01d1 (OASIS pin 767a60b5b5) |
| Auto-bump submodule pointers on Development so Solution Integrity stops failing on drift (WEB6, API.Core, STAR ODK so far) | CI | local: HyperDrive Client session | work/submodule-autobump | 2026-11-25 02:40 | IN PROGRESS |
| CeramicOASIS native storage (ceramic-one local node) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-ceramic | 2026-11-25 01:50 | IN PROGRESS |
| CelestiaOASIS native storage (local Celestia devnet) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-celestia | 2026-11-25 01:50 | IN PROGRESS |
| COSMIC M19 inheritance discriminator (TPH) | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-m19 | 2026-11-25 01:58 | DONE 2f96062 (Core) CI 45530746671 |
| EclipseOASIS native storage (SVM: local solana-test-validator) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-eclipse | 2026-11-25 02:10 | IN PROGRESS |
| BlueSkyOASIS native storage (self-hosted AT Protocol PDS) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-bluesky | 2026-11-25 02:10 | IN PROGRESS |
| FilecoinOASIS native storage (local Lotus devnet) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-filecoin | 2026-11-25 02:10 | IN PROGRESS |
| Dependabot dev-tooling alerts: Web3CoreOASIS undici (Hardhat) and TONOASIS SdkBridge protobufjs | Security / Providers | local: WEB6 catalogue session | Development (direct, small) | 2026-11-25 03:00 | DONE (Web3Core: 148 contracts compile, 45 Hardhat tests pass; TON: Tact output identical) |
| ENSOffchainOASIS live public CQL source (anonymous api.web3.bio ENS name profiles) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-ensoffchain | 2026-11-25 02:45 | DONE (code) 25a128c2aa; live pending Railway enrollment by CQL sandbox session (CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md) |
| COSMIC M23 keyless entities / read-only views | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-m23 | 2026-11-25 02:25 | IN PROGRESS |
| Dependabot runtime alerts in Docker/PouchDbRuntime (qs, body-parser, path-to-regexp, uuid via express 4.21.2) | Security / Docker runtime | local: WEB6 catalogue session | Development (direct, small) | 2026-11-25 04:00 | IN PROGRESS |
| COSMIC A16 polymorphic/any associations | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-a16 | 2026-11-25 07:53 | IN PROGRESS |
| COSMIC A05 inverse reference (ParentHolon) fix-up | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-a05 | 2026-11-25 08:25 | BLOCKED: see ACTIONS-FOR-DAVID (ParentHolon drives persisted ids) |
