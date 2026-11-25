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
| Pinot native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | DONE 511b9b32 (popular matrix run 45519020083) |
| IPFS catalogue root pointer (dev replication-source fault) | HyperDrive replication | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | CODE DONE c2da27e5 (CI green); Railway dev DNA key applied at the next Development merge |
| ChainLinkOASIS / Connext / read-only chain adapters classification | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:03 | DONE 897db2c5 |
| Provider TestHarness projects build (all 172) | Providers/TestProjects | local: testharness-build-fix | work/testharness-build-fix | 2026-11-24 | DONE 603b3222 |
| AlgorandOASIS native storage (shared suite 26/26 on LocalNet) | Providers/Blockchain | local: testharness-build-fix | work/algorand-native-storage | 2026-11-24 23:30 | DONE b9cdfc0a (docs b1449877, master 89ea1acd) |
| COSMIC Q09 string transforms (Replace / Trim(char)) | COSMIC ORM | local: COSMIC ORM roadmap (work/cosmic-cql-ton-public session) | work/cosmic-cql-ton-public | 2026-11-25 00:20 | DONE 45515662485 |
| COSMIC Q65 compiled query cache | COSMIC ORM | local: COSMIC ORM roadmap (work/cosmic-cql-ton-public session) | work/cosmic-cql-ton-public | 2026-11-25 00:20 | DONE 45515662485 |
| COSMIC ORM checklist rows (all of CQL_PARITY_CHECKLIST.md) | COSMIC ORM | local: COSMIC ORM roadmap (work/cosmic-cql-ton-public session) | work/cosmic-cql-ton-public | 2026-11-25 00:20 | IN PROGRESS (claim one row at a time) |
| Live sandbox federation (private paired MongoDB + provider) presets sweep; Optimism/Linea/Scroll federation unavailable | CQL sandbox | local: CQL sandbox (cql-sandbox session) | work/cql-sandbox-providers | 2026-11-25 00:40 | DONE e88664cd (208/208 plain and 208/208 federation presets live) |
| Web4Site CQL Playground: remove stale pwsh / C:/Source/OASIS-cosmic local-demo instructions | CQL sandbox (Web4Site repo) | local: CQL sandbox (cql-sandbox session) | Web4Site work/cosmic-cql | 2026-11-25 00:40 | DONE Web4Site 6930aff (public promotion awaits David, ACTIONS-FOR-DAVID) |
| CI build gate for all provider TestHarness projects (stop harness rot) | CI / Providers/TestProjects | local: testharness-build-fix | work/testharness-ci-gate | 2026-11-25 00:50 | DONE 7af7e617 (PR 524) |
| COSMIC O15 as-of history reads | COSMIC ORM | local: COSMIC ORM roadmap (work/cosmic-cql-ton-public session) | work/cosmic-cql-ton-public | 2026-11-25 00:35 | IN PROGRESS |
| TelosOASIS native storage (reuse EOSIO Antelope fixture) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-telos | 2026-11-25 01:10 | DONE 48add53f (antelope-family-native-storage.yml run 45518765561 green) |
| CQL startup head-of-line blocking: a slow HoloOASIS activation keeps IPFS/NEAR/Neo4j/Polkadot/Polygon/SOLID/Solana/TON NOT_READY on live dev (serial canonical lane) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-activation-lane | 2026-11-25 00:38 | DONE 24688f3a (CqlActivationLaneTests pass) |
| COSMIC O18 EF6 databinding/local-view integration | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-o18 | 2026-11-25 02:00 | DONE 2f96062 (Core) CI 45519967748 |
| SEEDSOASIS native storage (Antelope fixture) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-seeds | 2026-11-25 00:35 | DONE 48add53f (antelope-family-native-storage.yml run 45518765561 green) |
| DaprOASIS native storage (daprd sidecar + Redis state store) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-dapr | 2026-11-25 00:40 | DONE f688e203 (popular-providers-native-storage run 45519314086 green) |
| ZcashOASIS native storage (regtest zcashd + Kubo, Bitcoin pattern) | HyperDrive providers | local: testharness-build-fix | work/zcash-native-storage | 2026-11-25 02:20 | DONE ddfdeeed (docs 07dfdb98) |
| COSMIC A15 set (HashSet) owned collections | COSMIC ORM | local: COSMIC ORM roadmap (work/cosmic-cql-ton-public session) | work/cosmic-cql-ton-public | 2026-11-25 02:10 | IN PROGRESS |
| GUNOASIS native storage (GUN relay) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-gun | 2026-11-25 00:45 | DONE a4391671 (popular-providers-native-storage run 45520161955 green) |
| MatrixOASIS native storage (Synapse, room state events) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-matrix | 2026-11-25 00:50 | DONE 543c91bc (popular-providers-native-storage run 45535697095 green) |
| ElrondOASIS (MultiversX) native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:46 | DONE 0b17810a (multiversx run 45524444807) |
| FhenixOASIS (EVM) native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 00:46 | DONE 9857135b (evm run 45518241302) |
| ArweaveOASIS native storage (arlocal) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-arweave | 2026-11-25 01:00 | DONE 543c91bc (popular-providers-native-storage run 45535697095 green) |
| COSMIC M24 model defaults, computed and generated properties | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-m24 | 2026-11-25 01:10 | DONE 2f96062 (Core) CI 45524783092 |
| OrbitDBOASIS native storage (@orbitdb/core + Helia service) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-orbitdb | 2026-11-25 01:10 | DONE 543c91bc (popular-providers-native-storage run 45535697095 green) |
| RootstockOASIS live public CQL source (anonymous public-node.rsk.co balance reads, BNB pattern) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-rootstock | 2026-11-25 01:04 | DONE (code) 24688f3a; live pending Railway enrollment by CQL sandbox session (CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md) |
| ScuttlebuttOASIS native storage (ssb-db2 feed service) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-scuttlebutt | 2026-11-25 01:25 | DONE 543c91bc (popular-providers-native-storage run 45535697095 green) |
| StoryProtocolOASIS live public CQL source (anonymous mainnet.storyrpc.io finalized balance reads, chain 1514) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-storyprotocol | 2026-11-25 01:11 | DONE (code) 4142d82b36; live pending Railway enrollment by CQL sandbox session (CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md) |
| LensOASIS live public CQL source (anonymous rpc.lens.xyz finalized balance reads, Lens Chain 232) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-lens | 2026-11-25 01:13 | DONE (code) 4142d82b36; live pending Railway enrollment by CQL sandbox session (CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md) |
| AppwriteOASIS native storage (self-hosted Appwrite in Docker) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-appwrite | 2026-11-25 01:20 | DONE 543c91bc (popular-providers-native-storage run 45535697095 green) |
| TablelandOASIS native storage (@tableland/local validator + Hardhat) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-tableland | 2026-11-25 01:20 | DONE 543c91bc (popular-providers-native-storage run 45535697095 green) |
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
| PineconeOASIS native storage (Pinecone Local container) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-pinecone | 2026-11-25 01:30 | DONE 543c91bc (popular-providers-native-storage run 45535697095 green) |
| QuestDBOASIS live public CQL source (port unmerged symbol adapter 9e67263e..35d38833; read-only demo.questdb.io) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-questdb | 2026-11-25 01:24 | DONE (code) 25a128c2aa; live pending Railway enrollment by CQL sandbox session (CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md) |
| Live CQL for self-hostable DB providers and keyed indexer APIs (needs Railway dev demo DBs / free keys) | CQL sandbox (WEB4) | cloud: cloud-cql-C | n/a | 2026-11-25 01:28 | BLOCKED: see ACTIONS-FOR-DAVID (Live CQL sandbox: demo databases and indexer keys) |
| ONODE Manager hardening, Manager tests and setup error reporting | ONODE/ONODEManager | local: ONODE Manager improvement loop | ONODEManager main/Development | 2026-11-25 01:31 | DONE ONODEManager 1f1b1c4 (OASIS pin 0019eaed36) |
| Auto-bump submodule pointers on Development so Solution Integrity stops failing on drift (WEB6, API.Core, STAR ODK so far) | CI | local: HyperDrive Client session | work/submodule-autobump | 2026-11-25 02:40 | DONE 64f58aef27 (PR #526). Schedule only runs from master, so it starts after the next Development to master promotion; until then run Scripts/bump_submodules_to_branch.py Development by hand |
| CeramicOASIS native storage (ceramic-one local node) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-ceramic | 2026-11-25 01:50 | DONE 543c91bc (popular-providers-native-storage run 45535697095 green) |
| CelestiaOASIS native storage (local Celestia devnet) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-celestia | 2026-11-25 01:50 | DONE 543c91bc (popular-providers-native-storage run 45535697095 green) |
| COSMIC M19 inheritance discriminator (TPH) | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-m19 | 2026-11-25 01:58 | DONE 2f96062 (Core) CI 45530746671 |
| EclipseOASIS native storage (SVM: local solana-test-validator) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-eclipse | 2026-11-25 02:10 | SUPERSEDED: done by hyperdrive-triad (f2717b1b); work/cloud-hd-eclipse not merged |
| BlueSkyOASIS native storage (self-hosted AT Protocol PDS) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-bluesky | 2026-11-25 02:10 | DONE 543c91bc (popular-providers-native-storage run 45535697095 green) |
| FilecoinOASIS native storage (local Lotus devnet) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-filecoin | 2026-11-25 02:10 | DONE 0c0954c0 (popular-providers run 45700279460, EVM suite run 45700279496 green) |
| Dependabot dev-tooling alerts: Web3CoreOASIS undici (Hardhat) and TONOASIS SdkBridge protobufjs | Security / Providers | local: WEB6 catalogue session | Development (direct, small) | 2026-11-25 03:00 | DONE: c89b6f9 (undici ^6.29.1, protobufjs ^7.5.6) |
| COSMIC Q55 multi-branch text CASE projections | COSMIC ORM | local_fa02a367 child COSMIC session | work/cosmic-cql-ton-public | 2026-11-25 | DONE 2026-11-25 (Dev 37c41f40) |
| Web4Site CQL Playground: sample records so built-in MongoDB/SQLite/Neo4j presets return rows for new visitors | CQL sandbox (Web4Site repo) | local: CQL sandbox (cql-sandbox session) | Web4Site work/cosmic-cql | 2026-11-25 00:55 | DONE Web4Site f301621 (public promotion awaits David) |
| IPFS/SOLID/ActivityPub test projects: reference the WebAPI project instead of compiling its CQL sources | CQL sandbox build | local: CQL sandbox (cql-sandbox session) | work/cql-sandbox-providers | 2026-11-25 00:50 | DONE 11c7dbdd (CI run 45528232647 green) |
| Web4Site CQL Playground: hide 127.0.0.1 local demo links from public visitors; bump changed script versions | CQL sandbox (Web4Site repo) | local: CQL sandbox (cql-sandbox session) | Web4Site work/cosmic-cql | 2026-11-25 01:30 | DONE Web4Site 3099bf0 (public promotion awaits David) |
| Solana/EOSIO storage provider type for composed Eclipse/Telos (ProviderUniqueStorageKey stamps outer type) | HyperDrive providers | cloud: vigilant-ramanujan | claude/vigilant-ramanujan-01l59a | 2026-11-25 | IN PROGRESS |
| Dependabot runtime alerts in Docker/PouchDbRuntime (qs, body-parser, path-to-regexp, uuid via express 4.21.2) | Security / Docker runtime | local: WEB6 catalogue session | Development (direct, small) | 2026-11-25 | DONE b3c857a (smoke-tested; image build runs verify-pouchdb-runtime.cjs) |
| BasechainOASIS (EVM) native storage | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 06:11 | DONE 6961e8d6 (evm run 45528939313) |
| AztecOASIS / MidenOASIS / BlockStackOASIS fake-storage removal | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 06:11 | DONE 6961e8d6, 511b9b32 |
| Dependabot runtime alerts in HashgraphOASIS SdkBridge (protobufjs 8.0.1, @grpc/grpc-js) | Security / Providers | local: WEB6 catalogue session | Development (direct, small) | 2026-11-25 | DONE e59c071 (164/164 bridge tests on Node 24.22.1) |
| COSMIC O16 full-text search provider extension | COSMIC ORM | local_fa02a367 child COSMIC session | work/cosmic-cql-ton-public | 2026-11-25 | DONE 2026-11-25 (Dev 37c41f40) |
| Dependabot alerts in Scripts/TestHosts/near-local (tar 6.2.1 via near-sandbox) | Security / Test hosts | local: WEB6 catalogue session | Development (direct, small) | 2026-11-25 | DONE e301bff (tar.x stream extraction verified on tar 7.5.23) |
| TONOASIS shared core storage suite on the local TVM sandbox (reuse ton-sqlite fixture); matrix: Web3CoreOASIS is a contract client, not a provider | HyperDrive providers | local: testharness-build-fix | work/ton-native-storage | 2026-11-25 03:10 | DONE 4de21f58 (docs b1449877) |
| Dependabot alerts in root package-lock.json (mermaid-cli dev tool: lodash-es, basic-ftp, minimatch) | Security / tooling | local: WEB6 catalogue session | Development (direct, small) | 2026-11-25 | DONE 45565ec (lodash-es, basic-ftp; minimatch needs mermaid-cli on glob 11) |
| Dependabot minimatch alert in oasisweb4.com (via @typescript-eslint/typescript-estree) | Security / sites | local: WEB6 catalogue session | Development (direct, small) | 2026-11-25 | DONE 45565ec (@typescript-eslint 8.74.0) |
| COSMIC M22 entity splitting / multiple mappings | COSMIC ORM | local_fa02a367 child COSMIC session | work/cosmic-cql-ton-public | 2026-11-25 | DONE 2026-11-25 (Dev 9024fe75) |
| ENSOffchainOASIS live public CQL source (anonymous api.web3.bio ENS name profiles) | CQL sandbox (WEB4) | cloud: cloud-cql-C | work/cloud-cql-ensoffchain | 2026-11-25 02:45 | DONE (code) 25a128c2aa; live pending Railway enrollment by CQL sandbox session (CLOUD_CQL_C_ENROLLMENT_HANDOFF_2026-11-25.md) |
| FarcasterOASIS native storage (local Hubble hub + local OP chain) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-farcaster | 2026-11-25 09:25 | CLASSIFIED non-storage: no keyed, durable app-data message type (FARCASTER_STORAGE_CLASSIFICATION_2026-11-25.md, e9e0d585) |
| RadixOASIS native storage (local Babylon network) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-radix | 2026-11-25 09:25 | IN PROGRESS |
| Run the full ONODE WebAPI.UnitTests project in CI (1,554 tests; LiveService quarantine) | CI / ONODE | local: onode-webapi-full-unit-tests | ci/onode-webapi-full-unit-tests | 2026-11-25 08:40 | DONE PR 528 (awaiting merge; job runs on push to Development) |
| ONODE.Core interop providers: remove sync-over-async InitializeAsync().Result (Java, JavaScript, Python, WebAssembly) | ONODE.Core | local maps session (a7a489fd) | work/onode-interop-async | 2026-11-25 | DONE 85b716166ee (ONODE.Core 9c77b5c, STAR 5db9aa2) |
| Core unit test HyperDriveProviderExecutionTests.HolonVisibilityAuthorizationCannotEscapeItsInjectedRuntime failing in OASIS CI/CD Test Coverage ("provider returned no result") | Core / CI | local: WEB6 catalogue session | Development | 2026-11-25 | DONE 462cdba (601/601 HyperDriveProviderExecutionTests) |
| Remove dead ONODE.Core OAPPManagerBase.cs (fully commented out; split not needed) | ONODE.Core | local maps session (a7a489fd) | work/onode-interop-async | 2026-11-25 | DONE 85b716166ee (ONODE.Core 9c77b5c, STAR 5db9aa2) |
| COSMIC Q36 arithmetic projection expressions | COSMIC ORM | local_fa02a367 child COSMIC session | work/cosmic-cql-ton-public | 2026-11-25 | DONE 2026-11-25 (Dev 9024fe75) |
| MapboxOASIS and WRLD3DOASIS: replace fake map operations (47 'return true' each) with the shared MapProviderBase and real vendor APIs | Maps | local maps session (a7a489fd) | work/map-providers-mapbox-wrld | 2026-11-25 | DONE 607db258b91 (Mapbox real; WRLD3D unavailable, decision in ACTIONS-FOR-DAVID) |
| Audit: find test projects CI never runs (WebAPI.IntegrationTests, provider TestProjects) and wire them in | CI | local: onode-tests session | ci/test-project-coverage | 2026-11-25 | PR 529: 21 projects wired in (1,277 tests; fixed Contracts, Managers, Elrond, ChainLink, BlockStack, Fantom, SEEDS, PLAN, Base, Avalanche, TRON); 244 remain, list in CI-TEST-PROJECT-COVERAGE-AUDIT.md |
| Post-release 2026-11-24: sandbox sweep owner-scoped listing (PR #527); prod SQLite held route ...AB2C2 (BINDING_MISMATCH, root cause found, release needs David) | HyperDrive / sandbox | local: post-release checks (57741641) | fix/sandbox-sweep-owner-scope | 2026-11-25 | IN PROGRESS: #527 CI; held-route release BLOCKED: see ACTIONS-FOR-DAVID |
| COSMIC Q36b Double and Int64 arithmetic projections | COSMIC ORM | local_fa02a367 child COSMIC session | work/cosmic-cql-ton-public | 2026-11-25 | DONE 2026-11-25 (CI 45675957893) |
| Core.UnitTests failing on Development CI/CD Test Coverage: CosmicProjectionIntentTests.CollectionInitializersAndComputedValuesAreNotNativeSelections and HolonVisibilityAuthorizationCannotEscapeItsInjectedRuntime (again, after 462cdba); coverage loop also stops at first failing project | Core / CI | local: onode-tests session | ci/test-project-coverage | 2026-11-25 | DONE cdb333a5 in PR 529 (8/8; loop now runs every project) |
| AztecOASIS native storage (local Aztec sandbox via Node SdkBridge; PXE proving) | HyperDrive providers | local: testharness-build-fix | work/aztec-native-storage | 2026-11-25 | IN PROGRESS |
| COSMIC M23 keyless entities / read-only views | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-m23 | 2026-11-25 02:25 | DONE 5079bfc (Core) CI 45570162116 |
| COSMIC A16 polymorphic/any associations | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-a16 | 2026-11-25 07:53 | DONE 526ebc2 (Core) CI 45572943708 |
| COSMIC A05 inverse reference (ParentHolon) fix-up | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-a05 | 2026-11-25 08:25 | BLOCKED: see ACTIONS-FOR-DAVID (ParentHolon drives persisted ids) |
| COSMIC M25 model-declared indexes and constraint management | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-m25 | 2026-11-25 09:10 | RELEASED 09:25: SQLite (json_each EXISTS) and MongoDB ($expr/$getField) metadata predicates cannot use native indexes; M25 needs index-aware compiler paths in all three stores first (performance work, deferred by breadth-first policy) |
| COSMIC A14 future/multi-query execution | COSMIC ORM | local_fa02a367 child COSMIC session | work/cosmic-cql-ton-public | 2026-11-25 | IN PROGRESS |
| EdgeSQLiteOASIS native storage (shared core-storage suite, local SQLite file) | Storage providers | local: HyperDrive provider programme (hyperdrive session) | work/hyperdrive-triad | 2026-11-25 21:00 | DONE: not a storage provider (Edge sync journal); HoloOASIS.Edge likewise; matrix corrected (commit 17c1598e, in next Development batch) |
| UrbitOASIS native storage (local fake ship) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-urbit | 2026-11-25 21:20 | DONE daa8d616 (popular-providers run 45704685480 green) |
| TelegramOASIS native storage (needs a bot token) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 21:20 | BLOCKED: see ACTIONS-FOR-DAVID (bot and pinning accounts) |
| DiscordOASIS native storage (needs a bot token) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 21:20 | BLOCKED: see ACTIONS-FOR-DAVID (bot and pinning accounts) |
| PinataOASIS native storage (hosted pinning) | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 21:20 | BLOCKED: see ACTIONS-FOR-DAVID (bot and pinning accounts) |
| PocketBaseOASIS native storage (local PocketBase binary in CI) | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 21:15 | IN PROGRESS |
| COSMIC M20 inheritance table-per-type equivalent | COSMIC ORM | local: maps/interop session | work/cosmic-m20-tpt | 2026-11-25 | RELEASED: checklist says a document/graph TPT layout needs a faithful definition first; needs a COSMIC design decision before code |
| BlockStackOASIS Gaia hub auth (signed v1 token) and native storage on a local gaia-hub | HyperDrive providers | local: HyperDrive provider programme | work/hyperdrive-triad | 2026-11-25 21:45 | IN PROGRESS |
| COSMIC Q30 DateOnly/TimeOnly/DateTimeOffset/TimeSpan translation | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-q30 | 2026-11-25 21:30 | IN PROGRESS |
| MidenOASIS native storage (local miden-node via @miden-sdk/miden-sdk worker) | HyperDrive providers | local: testharness-build-fix | work/miden-native-storage | 2026-11-25 | IN PROGRESS |
| Meilisearch, OpenSearch, Solr, Weaviate native storage: wire the existing (never-run) suites into popular-providers-native-storage.yml | HyperDrive providers | local: HyperDrive provider programme | work/blockstack-gaia | 2026-11-25 22:55 | IN PROGRESS |
| COSMIC A13 join-fetch strategy and per-query split/join switch | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-a13 | 2026-11-25 22:00 | IN PROGRESS |
| ONION-Protocol (OnionOASIS) native storage (local Tor onion service) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-onion | 2026-11-25 22:05 | IN PROGRESS |
| HoloOASIS.Desktop native storage (local Holochain conductor) | HyperDrive providers | cloud: cloud-hyperdrive-A | work/cloud-hd-holo-desktop | 2026-11-25 22:05 | IN PROGRESS |
| COSMIC Q56 user-defined function translation registry | COSMIC ORM | local_fa02a367 child COSMIC session | work/cosmic-cql-ton-public | 2026-11-25 | IN PROGRESS |
| RoninOASIS and ImmutableZkEVMOASIS: replace fake storage with Web3Core EVM storage; add to evm-native-storage suite | HyperDrive providers | local: HyperDrive provider programme | work/blockstack-gaia | 2026-11-25 23:10 | IN PROGRESS |
| COSMIC Q47 correlated subqueries beyond child/descendant Exists | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-q47 | 2026-11-25 22:05 | IN PROGRESS |
| COSMIC Q61 NHibernate Criteria/QueryOver compatibility | COSMIC ORM | cloud: cloud-cosmic-B | work/cloud-cosmic-q61 | 2026-11-25 22:12 | IN PROGRESS |
| LoomOASIS native storage | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 22:15 | CLASSIFIED non-storage (PLAN_LOOM_CLASSIFICATION_2026-11-25.md) |
| PLANOASIS native storage | HyperDrive providers | cloud: cloud-hyperdrive-A | n/a | 2026-11-25 22:15 | BLOCKED: backend unknown, see ACTIONS-FOR-DAVID (PLANOASIS) |
