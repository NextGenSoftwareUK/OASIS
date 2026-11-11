# Current provider status

11 November Web3Core deployment job [43092941435](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43092941435) passed the shipping Hardhat deployment on native BSC. Hardhat compiled 148 Solidity files with the Paris target and the native RPC verified deployed bytecode.

11 November BNB NFT verification. The shipping Web3Core contract now implements owner-authorized ERC-721 burn, matching the adapter ABI. A native Parlia job deploys the contract and checks mint, collection metadata, transfer, non-owner burn rejection, and owner burn with independent native ownership reads. Existing deployments need a compatible contract for this operation. [Run 43092221100](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43092221100) passed mint, collection metadata, transfer, non-owner rejection and owner burn against the freshly deployed contract. The earlier queued revision was superseded before it ran.

11 November Valkey storage verification. Official Valkey 9.0.4 passed 52/52 through the shipping StackExchange.Redis adapter in [run 43091045329](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43091045329). Both modes passed exact fixture cleanup and the native database ended empty. Earlier native smoke and CQL restart/federation evidence carries forward. Hosted TLS and plaintext migration remain open.

11 November BNB public wallet verification. CI will verify both shipping balance entry points against the official BSC mainnet RPC with the published wrapped BNB contract as the public read target. It checks chain ID 56, rejection when configured for chain 97, and that storage stays inactive. The job needs no signer, storage contract or private-chain fixture. The official endpoint returned chain 56 and a positive raw balance in a separate readiness probe. Both public BNB mainnet wallet balance modes passed through unsigned Nethereum, rejected chain 97 and left storage inactive in [run 43087353377](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43087353377).

11 November Valkey and Dragonfly evidence reconciliation. Development run 43059249089 at d93d984b7b passed four native avatar/holon smoke cases for each provider and separate encrypted CQL restart/federation jobs. The shared 52-case native lifecycle remains open for both. No tests were repeated.

11 November BNB BootLoader registration shipped to Development at febb6971d8. The [Development BootLoader job 43086956668](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43086956668) passed passive registration, activation and deactivation with Development dependencies. CI confirmed fixture cleanup.

11 November BNB Smart Chain storage [run 43084066216](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43084066216) at bde9eb8a0c passed 52/52 on private Parlia chain 714 after the shipping Nethereum adapter switched to the node gas-price quote. Both modes verified the native validator RPC and shared cleanup. The shipping BootLoader registered, activated and deactivated BNB on native BSC v1.7.8 in [run 43085641543](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43085641543).

11 November AvalancheGo storage [run 43080136029](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43080136029) at fb90b97f8f passed 52/52 through shipping Nethereum on AvalancheGo 1.14.1 local C-Chain 43112. Both separately deployed contracts started empty, verified ownership and passed shared lifecycle cleanup. CI removed the node and temporary files. Hosted and other capability verification remain open.

11 November public documentation update. OASIS Docs main 41b6723 and OASIS Learn main 082ecdc link the live provider guides to this status page. Learn describes failover, replication and load balancing for eligible configured providers. Fresh public responses served the corrected markup, both status links and the 234-project inventory. The update required no local build, download or retained test artifact.

11 November Milvus storage [run 43078055694](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43078055694) at 652853ec7f passed 52/52 through the shipping REST v2 adapter on Milvus 2.6.26. All 14 write-ownership checks passed. Each mode independently removed five exact owned holon tombstones and confirmed all three collections empty. Hosted storage, historical versions and vector-specific capabilities remain open.

11 November Memcached storage [run 43075704036](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43075704036) at 8ba3eb3adb passed 52/52 through shipping EnyimMemcachedCore on two independent Memcached 1.6.40 daemons. Enyim verified exact native holon fixture removal. CI removed both daemons and checkpoints. The lifecycle pass is scoped to volatile cache storage. Hosted operation, eviction behavior and multi-key atomicity remain open.

11 November RavenDB storage [run 43071621377](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43071621377) at 0e7f9d396e passed 52/52 through the shipping RavenDB.Client adapter on RavenDB 7.2.7. All four write-ownership regressions passed. Both independent databases had zero documents after exact cleanup, and CI removed the disposable resources. Hosted and COSMIC CQL acceptance are separate.

11 November ScyllaDB storage [run 43069185380](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43069185380) at 3dfbf4908b passed 52/52 through the shipping DataStax adapter on ScyllaDB 2026.3.4. All five write-ownership regressions passed. Both independent keyspaces had three empty native tables after exact cleanup, and CI removed the disposable resources. Hosted and COSMIC CQL acceptance are separate.

11 November Cassandra storage [run 43066716817](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43066716817) at ad046dcec1 passed 52/52 through the shipping DataStax adapter on Apache Cassandra 4.1.12. All five write-ownership regressions passed. Both independent keyspaces had five empty native tables after exact cleanup, and CI removed the disposable resources. Hosted and COSMIC CQL acceptance are separate.

11 November PlanetScale storage [run 43064133424](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43064133424) at 69e965847a passed 52/52 against MySQL 8.4.13 through the shipping adapter. The serializer-only run 43063702938 verified all nine avatar and detail checks per mode before stopping at graph save. The full candidate verified atomic graph persistence, metadata and owner export, import, exact deletion and empty native tables in both modes. Managed PlanetScale and COSMIC CQL acceptance are separate.

11 November TimescaleDB [run 43061942830](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43061942830) at 49b6c88a17 passed all 52 shared storage checks through the shipping Npgsql provider against official TimescaleDB 2.31.0. Each mode used its own database with the native extension loaded. Independent SQL verified ten exact owned tombstones and three empty current tables per database. CI removed the container, volumes and fixture. Existing COSMIC CQL evidence carries forward.

11 November CockroachDB [run 43060207358](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43060207358) at 8c630a82ae passed all 52 shared storage checks through the shipping Npgsql provider on official CockroachDB 26.2.9. Each mode used its own database. Independent native SQL verified and removed five exact owned tombstones per mode and confirmed three empty current tables in each database. CI removed the runtime and fixture. Hosted and COSMIC CQL acceptance are separate.

11 November live publication recheck. The public OASIS Docs overview and OASIS Learn production tutorial both serve the updated inventory. The three registry totals were independently counted from their live project rows. The Web4Site, oasisweb4.com and Omniverse registries each returned 234 distinct projects. The OASIS Docs provider overview reports 234 projects, with 178 storage and specialized-storage and 56 non-storage. The Founders, WEB6, STARNET and STAR homepages carried 234. The public OPORTAL homepage and developer route, OASIS Learn, and WEB7–WEB10 homepages had no older OASIS-wide total in the inspected pages. The repository provider summary now links directly to the public registry and guide. GitHub's default master README still shows the October figure pending release promotion.

11 November Convex native companion recovery. [The official Linux engine and pinned CLI run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43017572009) passed 1/1 at 4bd86ed215. One provider instance rejected activation on a fresh engine without the companion, then activated and deactivated after the native deployment. The provider and companion match the tested revision. The disposable engine stopped. Hosted Convex remains open.

11 November Supabase capped pagination. [Native PostgREST CI](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43016561338) verified 121 exact IDs for holons, avatars and avatar details with a server cap of 37 rows per response. The verified provider and test match a994ffa8b2. The disposable PostgreSQL service was removed. Hosted project verification remains open.

11 November Supabase native PostgREST. The [credential case](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43014993069) rejected the crossed key and saved and loaded through two independently authenticated endpoints sharing one client. The [storage suite](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43015778894) passed both modes, 26 checks each, with exact fixture cleanup against PostgREST 16.5 and PostgreSQL 16.6. The graph fix writes a uniform parent column on every row. The verified provider and tests match daae2ff8a2. Hosted Supabase gateway and project verification remain open.

11 November Neo4j V2. The package activation and deactivation [smoke](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43012374982) passed 1/1 against digest-pinned Neo4j Community 5.26.0. The [standalone harness](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43013084815) passed avatar and holon saves, point reads, holon search and deletion on the same native service version. The integration matches the tested V2 package, harness and canonical provider. Hosted Aura verification is separate and remains open.

11 November IPFS separate-process publication. The [baseline](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43010626932) acknowledged all eight saves but the reader found only the first writer records. With a file lease around catalogue mutation, the [native Kubo 0.43.1 run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43010855757) passed both distinct writer processes and the reader. All eight records remained visible in one persisted catalogue and the daemon stopped. The scope is Windows processes sharing one local DNA file. The verified provider, resolver, SDK and test match 57da4b22ef.

11 November IPFS independent writers. The [baseline](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43009364727) acknowledged every save but exposed only part of the catalogue. The [corrected Kubo run](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43009731953) recovered all eight records after saves through two independently configured provider instances sharing one DNA file in the same process. The candidate uses the same provider, DNA resolver, IPFS SDK and test as 919b7e2a40. The task branch carries additional provider settings unrelated to IPFS. Separate-process writers remain unverified.

11 November IPFS persistence. A [native Kubo restart](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43007340696) recovered both encrypted holon versions after the daemon and test process restarted. The [concurrent publication regression](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43008574348) then passed on Kubo 0.43.1 with eight acknowledged saves and an exact eight-record read from a fresh provider. The fix serializes one provider instance. The integration matches the tested provider, tests and IPFS SDK sources. The task branch has additional unrelated Neon, Convex and Supabase DNA settings. Independent writer instances and historical fixture recovery remain open.

11 November IPFS activation. Both sync and async provider activation and deactivation cases passed against checksum-pinned offline Kubo 0.43.1 on [GitHub Actions](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43006440924). The provider and integration tests match verified revision fdac8a7b81. Retained storage restart and concurrency acceptance remain open.

11 November SQL Server avatar wallet verification. The shipping provider preserves the stored JSON format and materializes wallet transactions with native evidence. On SQL Server LocalDB 17.0.4025.3 the focused test passed 1/1 with zero skips, covering a current avatar read after a version update and archived wallet and transaction reads by ID and by collection. The disposable database and instance were removed. The verified task revision is b3622db1fe. Full provider acceptance remains open.

11 November MongoDB graph transaction. At c3229c908a the shipping provider passed on a disposable one-member MongoDB 8.2.1 replica set. A native child validation rejection left zero holon rows and both caller IDs unset. The corrected graph committed both records, published both IDs and loaded the expected child. The scoped Development change carries the matching native test. Multi-node and hosted acceptance are separate.

11 November MongoDB metadata queries. The shipping provider passed synchronous and asynchronous All and Any queries against an isolated MongoDB 8.2.1 standalone server at 740c02a863. All four calls returned the exact expected GUID sets and loaded counts. The Development change matches the tested provider and shared dependencies. The disposable server and files were removed. This run covers standalone metadata queries.

11 November public EVM wallet balance. The shared unsigned Nethereum wallet reader passed on Arbitrum One at cfdae88ff3 and on Polygon PoS at c5b3909ada. The shipping providers returned positive native ETH and POL balances without activating contract storage. The Arbitrum test rejected a mismatched configured chain. The Development change matches the tested providers and shared dependency source. Public-network storage lifecycle verification remains open.

11 November SQLite wallet encryption. The focused native test passed at 77f9ef72fe using the shipping WalletManager and an ephemeral DNA encryption key. It independently inspected the file-backed SQLite row for ciphertext and absence of plaintext, reopened the database, and recovered the same private key and recovery phrase without changing the loaded ciphertext. The scoped Development change carries the verified test. Deployment key custody and rotation remain open.

11 November Neon connection policy. The adapter applies Npgsql VerifyFull to Neon endpoints with explicit ports and host lists. Single-endpoint pooler mapping preserves the port, and explicit lists retain their endpoints and order. All 13 provider and configuration unit tests passed at e4e9605424. The Development change matches the tested Neon source and dependencies. Hosted sync and async lifecycle verification still requires the dedicated Neon database.

11 November Convex transport. The shipping adapter now bounds response body reads by its configured HTTP timeout. At 3251b0d07c a loopback HTTP test stalled after headers, observed the timeout, and confirmed a later readiness probe succeeded on the same adapter. The Development change matches the tested provider, test source and shared dependencies. Hosted companion and lifecycle verification remain open.

11 November Polkadot Asset Hub wallet balance. The synchronous shipping wallet method passed a live finalized treasury DOT read through @polkadot/api 17.0.2 at 4ecb58e194. The test verified the Asset Hub genesis and required zero caller-context posts. It used no signer or storage contract. The scoped Development change matches the tested shipping source, SDK lockfile and shared dependencies. Writable storage verification remains open.

11 November TRON mainnet wallet balance. The synchronous shipping wallet call passed against PublicNode's TLS full and solidity gRPC endpoints at 6e4449a68f. The SDK verified both genesis responses and returned a positive solidified TRX balance with no caller-context posts, signer or storage activation. The Development candidate uses the same shipping provider, SDK configuration and shared dependencies. Writable storage verification remains open.

11 November Cosmos Hub wallet balance. The synchronous shipping wallet call passed a real Cosmos Hub bank read through CosmJS 0.39.0 at 051b3c03c2. It returned positive ATOM without a signer, storage activation or caller-context posts. The Development candidate matches the tested shipping provider, SDK bridge and shared dependency source. This evidence covers wallet balance only. Writable storage remains open.

11 November NEAR public wallet balance. Both shipping wallet entry points passed against finalized NEAR mainnet state through near-api-js 7.3.1 at 507f4dd419. The scoped Development change uses the same wallet source, bridge, regression test and Core revision. The only difference in the SDK service file is an independent avatar deletion method on the task branch. The existing public mainnet tests were reused. Writable NEAR storage acceptance remains open.
Updated28October2026. Read this first, then query relevant matrix rows; do not load archives by default.

## Phase and rules

Active checkpoint: Cloudflare Workers KV now shares encrypted record policy;52controlled core checks and25transport/metadata controls pass, two native cases gated. No cloud resource/install/download. Native eventual-consistency/rate-limit/CAS limits remain open (CLOUDFLARE_KV_STORAGE_BASELINE.md). Next: Tigris S3 consolidation. Upstash native52/52 pushed90716cb0b; its sole scratch service expires31October.

- Breadth first: real provider implementations and canonical core-storage-v2 (26checks per sync/async mode), approximately one day per family. Non-storage uses capability-v1, not storage tests.
- All applicable provider baselines precede global failover/load balancing/replication verification, then priority-provider depth. No Polkadot/NEAR/SQLite depth now.
- Use C:/Source/OASIS, fix/hyperdrive-v2-providers; never recreate deleted checkout. No new clones/worktrees/environments; reuse cached runtimes.
- Stage exact owned files, preserve concurrent dirty work/submodules, no force pushes/attribution trailers. COSMIC is isolated in OASIS-cosmic; contact only for overlapping Core/Shared public API work.
- Integrate scoped verified batches to Development once/twice daily; fetch first, verify submodule commits remotely and manifest/gitlink agreement. Never deploy unrelated/unverified changes just to synchronize.
- New raw evidence under TestResults; compact hash-bound summaries only are tracked. Maintain resource ledger, ten recent entries, byte-preserved archive rollovers.

## Evidence now

| State | Scope |
|---|---|
| Native core-storage-v2 52/52 | OracleDB, PostgreSQL, Redis, Mongo, Neo4j, CosmosDB emulator, Firestore emulator, AWS alias, DynamoDB, LocalFile, Azure Blob, SQLite, DuckDB, MinIO, Convex, Upstash |
| Native compatible52/52 | Shipping AWSS3OASIS on existing CI MinIO; independent empty-bucket checks passed; hosted AWS/IAM open |
| Recovered native52/52 summaries | SOLID/IPFS reports recovered from isolated checkout with exact registry hashes/contracts; original failed reports preserved, lifecycle/full-interface gates open |
| Controlled52/52 only | KeyDB, ValKey, Dragonfly, Garnet and ThreeFold; product-native shared-suite runs pending |
| Non-storage ActivityPub | Native public-GET capability5/5, controlled291; full social operations open |
| Other controlled cases | Firebase23, Appwrite19, PocketBase24, Convex24; these do not certify native storage |
| Partial native | Solana4/52; original referenced history unavailable, unresolved outcomes preserved |
229 matrix projects:56non-storage/173storage-specialized. SDK-source-unverified0, reviewed missing9; client detection is not implementation completeness. No overall100% claim.

## Exact next actions and gates

1. DuckDB thin shared-record adapter passes52/52 native plus6controls, zero current rows, default soft deletion preserved; see DUCKDB_STORAGE_BASELINE.md. Continue remaining SQL/document family baselines; FlyIO/Nile already reuse PostgreSQL. Neon10controls and MotherDuck10controls only, hosted native open; MotherDuck now shares DuckDB transport/record policy (SQL_FAMILY_BASELINE.md). Fauna default host does not resolve; self-hosted source exists. Convex now supplies its companion and native52/52; other REST shared-record consolidation remains an implementation gate.
2. Firebase canonical binding builds;23controls pass,2native cases skip without configuration. One RTDB4.11.2 JAR cached (34,495,935 B); execution policy rejected loopback19000 launch. No retry/bypass, owner file or listener; native run remains open.
3. Dynamo relocation evidence revalidated: pinned JAR hash matches; both aliases52/52, five tables empty, compacted DB43,008B, evidence folder under200MB. Scripts already pushed asf446b70a2; current summaries now under TestResults. S3 CI40731869610/job122970030713 passed52/52 and both post-suite bucket-empty checks. See S3_COMPATIBLE_STORAGE_BASELINE.md. PR501 merged externally at7b834548e; later work is not covered by that merge. Shared S3 base serves AWSS3/MinIO/ThreeFold; MinIO native52 and AWSS3 compatible52 verified in CI40754776833/job123050672281.51family/41AWS/60shared/5QSS controls pass; full/hosted gates remain (S3_COMPATIBLE_STORAGE_BASELINE.md).
4. Oracle cloud credentials/bucket/ADB access absent at last check. EVM signer/ABI/runtime and Cosmos bridge inputs remain gates; reuse existing resources before any installation. Keep original stores/keys/outcomes intact.
5. Development 953cb7968 synchronized for PR502. Published Core261fce1 pin/manifest matched; local Core checkout remains owner-held e46ce28 with dirty files preserved, so CI must establish the new source cohort. Core includes7c45e2c: per-avatar request routing, owned-write outbox, fault gates and native-import contracts now present. Follow Docs/Devs/HYPERDRIVE_PER_AVATAR_SETTINGS.md and NATIVE_DATA_IMPORT.md; schema-unsafe holon properties must be excluded from GraphQL. Local changes preserved; no new all-provider routing proof claimed.

## Key commands

```powershell
git status --short
git fetch origin
python -B Scripts/Providers/update_provider_matrix.py
python -B Scripts/Providers/test_provider_matrix.py
python -B Scripts/Providers/test_conformance_reports.py
```
Wrap producer/consumer builds once in Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1; use cached restores and bounded workers. Native runners require original configuration/report review; do not reset failed fixtures to obtain a pass.

## Recovery links

[Matrix](PROVIDER_MATRIX.md), [gates](HYPERDRIVE_V2_CURRENT_RELEASE_GATES.md), [inventory](ENVIRONMENT_INVENTORY.md), [cleanup](WORKSPACE_CLEANUP_LEDGER.md), [I/O scope](HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md), [evidence](EVIDENCE_LOCATIONS.md), [document family](DOCUMENT_FAMILY_BASELINE.md).

Provider-specific audits hold details; [archive index](Archive/ARCHIVE_INDEX.md) is historical lookup only. Update at each milestone; status under100lines, each summary under150, entire default recovery set about300lines.
