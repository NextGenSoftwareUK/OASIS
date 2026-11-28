# HyperDrive and provider completion: current status

Last updated: 2026-11-27. This file stays short: replace sections, do not prepend log entries (append them to the archive). The old 700 KB running log, which also covered OPORTAL and billing, is now
`Archive/CURRENT_STATUS_LOG_TO_2026-11-23.md`. The per-provider detail is in `PROVIDER_MATRIX.md` and
`CONFORMANCE_RESULTS.json`.

Scope of this workstream: HyperDrive failover, replication and load balancing, plus provider storage completion.
OPORTAL, the ONODE popup and billing UI are now owned by a different session. CQL/WEB4 sandbox work happens on
`work/cosmic-cql-ton-public` in `C:\Source\OASIS2`.

## Current-Development rechecks (gpt-hyperdrive, 2026-11-27)

| Scope | Done/total | Evidence |
|---|---|---|
| Requested Firebase/Turso/PocketBase rechecks | 2/3 | Firebase CI 46099124837; Turso CI 46099193518, both Development `d91bf074fa16`, 26 sync + 26 async with cleanup. PocketBase remains claimed by the existing HyperDrive session. |
| Programme estimate carried from 26 November | about 115/234 | Rechecks refresh acceptance; they are not counted as new providers. Exact consolidated recount remains pending. |

Both runs used isolated real backends in CI; no shared Atlas test operations or local runtime/download/build output. Compact receipts are in `NativeEvidence/*-development-2026-11-27.json`. Hosted acceptance and global v2 gates remain open. OPORTAL continuation is limited to gaps beyond the other session’s accepted work; verified MCP source-link correction is tracked in OPORTAL-JS.

## Progress (local HyperDrive programme, updated 2026-11-25 03:00 UTC)

- Done: 91 storage providers pass the native core-storage-v2 suite on their real backend (today: Cardano, Starknet,
  Marqo, Druid, Pinot, Fhenix); the EVM suite now runs in CI (`evm-native-storage.yml`, 15 providers 30/30).
- Fake storage removed (explicit errors instead of false success): Connext, ChainLink, Aztec, Miden; BlockStack's Gaia
  client now reports failures. Ten read-only chain adapters reclassified as non-storage.
- Dev replication: the IPFS replication-source fault is fixed (durable catalogue root pointer); the Neo4j to SQLite hold
  at sequence 14 awaits David (ACTIONS-FOR-DAVID I1).
- Since then: Elrond/MultiversX, Basechain and Eclipse pass in CI; OpenZeppelin Defender, Wormhole, Stargate and Synapse are
  non-storage. In progress: the Development batch
  merge with the Railway dev `OASIS_DNA_JSON` key `IPFSOASIS.CatalogueRootPointerPath=/data/IPFS/catalogue-root`.
- Next 3: Ceramic signed commits (DID), BlockStack Gaia auth tokens, Aztec/Miden real storage designs.
- Blockers: ACTIONS-FOR-DAVID I1 and the hosted-account providers.

## Progress (cloud-hyperdrive-A, updated 2026-11-26 02:30 UTC)

Done in CI (native core-storage suite 26/26, sync + async, against real backends):
- TelosOASIS and SEEDSOASIS: storage through the EOSIO (Antelope) implementation; `antelope-family-native-storage.yml`, run 45518765561.
- DaprOASIS (run 45519314086) and GUNOASIS (run 45520161955).
- MatrixOASIS (Synapse room state), OrbitDBOASIS (@orbitdb/core 4 service), ArweaveOASIS (arlocal; owner-pinned, revisioned,
  encrypted), ScuttlebuttOASIS (ssb-db2 feed service), AppwriteOASIS (self-hosted 1.7.4), PineconeOASIS (Pinecone Local),
  TablelandOASIS (local validator + Hardhat), CelestiaOASIS (local devnet), CeramicOASIS (ceramic-one 0.58.0) and BlueSkyOASIS
  (self-hosted PDS + local PLC): all in `popular-providers-native-storage.yml` run 45535697095 (`work/cloud-hd-scuttlebutt`).
  OrbitDB and Scuttlebutt also pass a service-restart durability check in CI.
- FilecoinOASIS: FEVM records through the shared Web3Core EVM base on a local Lotus devnet (chain 31415926); Web3Core gained
  an opt-in `EstimateContractGas` (FEVM gas units). popular-providers run 45700279460; EVM regression suite run 45700279496.
- UrbitOASIS: purpose-built %oasis-kv Gall agent on an offline fake ship, poke-ack acknowledged writes, ship-restart
  durability check; popular-providers run 45704685480.
- RadixOASIS: OasisKeyValueStore Scrypto blueprint on a single-validator Babylon localnet, writes acknowledged on
  CommittedSuccess, node-restart check; popular-providers run 45709199011.
- OnionOASIS: Tor SOCKS5 transport over the repository storage-service contract, tested on a loopback-only private
  Tor network; popular-providers run 45711682481.
- SignProtocolOASIS: records as attestations on the real Sign Protocol SP contract (local Hardhat), revocation as delete,
  other attesters ignored; popular-providers run 45713760017.
- HoloOASIS.Desktop: shared suite on the same Holochain 0.7.1 conductor as canonical HoloOASIS; holo-desktop-provider-build.yml
  storage run 45723092086 (workflow baseline and hApp provenance pins fixed).

EclipseOASIS: done by the hyperdrive-triad session (f2717b1b); the duplicate `work/cloud-hd-eclipse` branch is not merged.

Classified non-storage: LoomOASIS (PLAN_LOOM_CLASSIFICATION_2026-11-25.md) and FarcasterOASIS (no keyed, durable app-data message type; see FARCASTER_STORAGE_CLASSIFICATION_2026-11-25.md).

Delegated storage identity: Telos and SEEDS now preserve their outer provider keys through EOSIO; saves and provider-key reads use the same canonical app-record repositories. Both modes pass in CI (Telos46101946563; SEEDS46101275004), including key round trips and independent native table cleanup. Eclipse/Solana adaptation remains on `work/gpt-delegated-storage-keys` pending Eclipse CI46101901966.

Summary (cloud-hyperdrive-A, 2026-11-26): every self-hostable storage provider this session took on now passes the
native core-storage suite in CI (20 above). Nothing self-hostable is left unclaimed; the remaining providers need David:

- Accounts needed (ACTIONS-FOR-DAVID): Nile, Cloudflare D1, Netlify Blobs, MotherDuck, Cloudinary, Deno Deploy, Fly.io,
  Neo4j Aura, LanceDB Cloud, Telegram, Discord, Pinata, Oracle Object Storage (plus the earlier hosted entries: Zilliz,
  Xata, Tigris, BigQuery, Snowflake, Redshift, Databricks, Algolia, Fastly).
- Decisions needed: retire Fauna, Vercel KV and Polybase (services discontinued); what PLAN is, or retire PLANOASIS;
  approve the pinned community ceramic-one CI image.
- Classified non-storage: FarcasterOASIS, LoomOASIS.
- Remaining delegated-provider gate: Eclipse/Solana, pending the scoped native CI run. Telos/SEEDS canonical identities are verified separately above.

## Branches

This work is on `work/hyperdrive-triad`, which was cut from `origin/Development` (d1936908). The old GPT task branch
`work/hyperdrive-provider-completion` (277560b0, pushed) has a stale Core gitlink, a88a9926. Its provider source no longer
compiles against that Core (`AvatarLoginReadBudget` and `RefreshToken.AuthorityProvider` are missing). Its verified batch
was already merged into Development as 94fc39ff. Do not resume work on that branch.

## HyperDrive: global failover, replication and load balancing

**Passed on 2026-11-23: a real end-to-end run across three independent backends.** The backends were MongoDB 8.2, run as
an owned single-node replica set on 127.0.0.1:27031; SQLite; and SQL Server 2025 LocalDB. The run went through the
shipping provider classes and `OASISHyperDrive` V2 routing with no mocks. The test is
`OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/MongoSqliteSqlServerTriadHyperDriveTests.cs`.
It passed three times in a row, taking about 1 minute each run.

| Check | Result |
|---|---|
| Auto-replication: a save routed to each of the 3 primaries | Record read back directly from all 3 backends |
| Round-robin load balancing: 9 reads | Exactly 3 reads served by each provider (checked from route metadata) |
| Auto-failover: each provider taken down in turn (dead TCP port or missing DB file) | Reads and writes that asked for the down provider were served by a healthy one. The route metadata shows the requested provider and the provider that actually served it |
| Outbox convergence: SQLite commits with SQL Server and MongoDB enrolled while MongoDB is down | SQL Server converged at once. The MongoDB delivery was kept (`COSMIC_OUTBOX_TARGET_NOT_APPLIED`), retried with backoff, then completed once MongoDB was back. All 3 backends ended on the same VersionId |
| Cleanup | Run-owned Mongo database, SQL database and SQLite directory dropped in `finally` |

Finding: MongoDB receives replicas inside multi-document transactions, so it **must be a replica set**. A standalone
mongod fails with "Mongo holon import preparation/session failed". The outbox correctly kept that delivery for retry. Hosted
deployments (Atlas) are replica sets already. A local or self-hosted standalone Mongo cannot be a HyperDrive replica target.

Earlier evidence: selected pair evidence covering Mongo+SQLite (login failover, committed replicas, outbox) and
SQL Server+SQLite (routing, outbox, round-robin, failover), in `MongoSqlite*Tests` and `SqlServerSqliteHyperDriveTests`.

Not yet covered:
- Hosted WEB4 (Railway) HyperDrive runs across several live providers.
- Fan-out over blockchain/network providers, which are slow and expensive to write to.
- Deliveries 535/543 remain on protected hold. They must not be replayed (see `SHARED_MONGO_STANDDOWN_2026-11-18.md`).

## Providers

There are 234 shipping provider projects: 178 storage or specialised storage, 56 non-storage.

Done (91 pass the native core-storage-v2 suite 52/52 against their real backend; ActivityPub passes capability-v1 5/5):

- Every priority provider except TON is done: HoloOASIS, Solana, MongoDB, SQLite, Neo4j, Supabase, SQL Server, Oracle (OracleDB),
  AWS, IPFS, NEAR, Cosmos, Azure (CosmosDB + Storage), Google Cloud, Polkadot, ActivityPub, SOLID, Polygon, Arbitrum,
  TRON, EOSIO and Hedera (Hashgraph). Correction (2026-11-25): TON was listed here in error; it was qualified for
  HyperDrive routing only, never through the shared core-storage suite. That run is now claimed (TONOASIS on the local
  TVM sandbox).
- On 2026-11-23 the source of all 23 priority providers was scanned for `NotImplementedException`, TODO stubs, placeholders
  and mocks. Every hit was commented-out legacy code or an explanatory comment. No shipping path is a stub.
- Others (Neon and Convex are not priority providers and take their normal place in popularity order): Neon, Convex, Ethereum, Avalanche, BNB Chain, Sui, AWS S3, Cloudflare, DynamoDB, Turso, Upstash,
  ThreeFold, Cassandra, CockroachDB, Dragonfly, DuckDB, Elasticsearch, Garnet, KeyDB, LocalFile, Memcached,
  Milvus, MinIO, PlanetScale, PostgreSQL, Qdrant, RavenDB, Redis, ScyllaDB, TimescaleDB, Typesense, ValKey.
- Correction (2026-11-25): Firebase, PocketBase, Meilisearch, OpenSearch, Solr and Weaviate were listed here in error.
  Their evidence is verified CQL, WEB4 cutover or demo runs (e.g. 6fbc64f3 Meilisearch, e54973c3 OpenSearch, a8548dfb
  Solr, cead58e3 Firebase, 9adc67d5 PocketBase), not the shared core-storage suite; they are back in the queue.
- Added on 2026-11-24, all 26/26 checks in both sync and async modes against real servers in CI
  (`.github/workflows/popular-providers-native-storage.yml`): CouchDB, Couchbase, ArangoDB, SurrealDB, ClickHouse,
  InfluxDB, Chroma and PgVector. On a local Hardhat chain (`Scripts/TestHosts/run-local-evm-storage-conformance.cjs`):
  Optimism, Base, Linea, Scroll and zkSync, alongside Ethereum, Arbitrum and Polygon (16/16).
- Root-cause fixes in that batch, each provider: exact save/load/delete acknowledgements and counts; RecordNotFound on
  absence; child holons saved with their parent; avatar hard delete also removes the avatar detail; records stored as
  canonical `StoredRecordJson` so interface-typed members and holon children round-trip; a native hard delete for the
  fixture cleanup. Also: ClickHouse `Esc` escaped quotes before backslashes (broken SQL strings) and used asynchronous
  `ALTER ... DELETE`; Couchbase read documents as `JsonElement` through its Newtonsoft transcoder (always failed) and
  ran SQL++ lookups without request-plus consistency; Chroma turned native HTTP failures into "not found"; PgVector
  serialised through the interface type (lost members) and had no ProviderName/Description. The CouchDB, ArangoDB and
  SurrealDB CQL workflows were re-run after the serializer change and pass.
- Also on 2026-11-24: Bitcoin (regtest + Esplora + Kubo, `.github/workflows/bitcoin-native-regtest.yml`) and Aptos (local
  testnet with the repo's Move module, `.github/workflows/aptos-native-storage.yml`), both 26/26 in both modes. Bitcoin
  now stamps every publication as the next record version, saves child holons, and supports avatar hard delete by
  crypto-shredding: a tombstone lists the erased publications, their encrypted Kubo envelopes are unpinned and
  garbage-collected, and readers accept a missing envelope only when a tombstone records that erasure. Aptos now stores
  canonical record JSON (nested children previously failed to read) and reports exact acknowledgements.
- Also on 2026-11-24: ArcadeDB, QuestDB and PouchDB in the CI matrix, and Abstract, Berachain, Fantom, Monad, Rootstock
  and Sei in the local EVM suite (now 14 EVM providers, 28/28). Fixes: ArcadeDB built SQL by string concatenation
  (quote-only escaping) and hid HTTP failures, so it now uses named parameters and surfaces errors; QuestDB writes are
  acknowledged only after the WAL writer has applied them, and hard delete is a DEDUP erasure marker because QuestDB has
  no row DELETE; PouchDB never checked write status and turned every read error into "not found"; Berachain and Sei had
  no chain identity, which the EVM base requires for signing, so their writes failed on every network; PgVector and
  ArcadeDB were missing ProviderName/Description.
- TON: the shared suite now runs too (2026-11-25): 26/26 in both modes against the shipping TONOASIS on a disposable local TVM sandbox running the compiled TonHolonStore (`.github/workflows/ton-native-storage.yml`). It was listed as done from its HyperDrive routing qualification only.
- Web3CoreOASIS is the EVM contract client, not a storage provider; the matrix no longer lists it as pending.
- Zcash now has real storage and passes 26/26 in both modes against a zcashd regtest node + Kubo (`.github/workflows/zcash-native-storage.yml`). ZcashOASIS sits on KeyValueStorageProviderBase: each record is an encrypted raw-block Kubo envelope (CIDv1 = SHA-256 of its bytes) and every put/delete is a transparent transaction from the wallet-held storage address whose 71-byte OP_RETURN commits SHA-256(key) and the envelope SHA-256. The old ZcashRepository invented txids and holons. Fixes found on the way: OP_RETURN is appended to the unsigned transparent bundle (zcashd createrawtransaction has no data outputs); the ZIP-317 fee is measured in transparent bytes (OP_RETURN costs extra actions); v5 signing is given the spent output; coins are taken from the node UTXO set, not the trailing wallet list; immature coinbase is never spent.
- NATS JetStream passes in the CI matrix (new integration-test project). Neo4jOASIS2 is a thin subclass of Neo4jOASIS (same
  driver and record implementation), so the Neo4jOASIS run covers it.
- XRPL now has real storage and passes 26/26 in both modes against rippled standalone + Kubo
  (`.github/workflows/xrpl-native-storage.yml`). XRPLOASIS sits on KeyValueStorageProviderBase: records are compressed
  and encrypted into Kubo envelopes, and every put/delete is an Ed25519-signed, validated AccountSet whose memo commits
  the key with the envelope's CID and SHA-256 (canonical binary encoding in `XrplTransactionCodec.cs`). The old code
  submitted transactions with an empty secret. Its integration test and harness also used a non-existent search API.
- Stellar now has real storage and passes 26/26 in both modes against a Stellar quickstart standalone network + Kubo
  (`.github/workflows/stellar-native-storage.yml`). Each key is an account data entry (name = base64url SHA-256 of the
  key, value = envelope CID + 30 bytes of its SHA-256) written by an Ed25519-signed ManageData transaction (XDR in
  `StellarTransactionCodec.cs`); deletes remove the entry. It was previously a read-only Horizon wrapper.
- Nostr now has real storage and passes 26/26 in both modes against a nostr-rs-relay in CI: NIP-78 application-data
  events (kind 30078, one replaceable event per key) with compressed, encrypted content, relay OK required for every
  write, BIP-340 signature verification on every read, and empty-content tombstones plus NIP-09 deletions. Signing moved
  from a BigInteger reference implementation to the constant-time NBitcoin.Secp256k1 port.

- Cardano now has real storage and passes 26/26 in both modes against a Yaci DevKit devnet (yaci-store
  Blockfrost-compatible API) + Kubo (`.github/workflows/cardano-native-storage.yml`). Each put/delete is a CBOR
  transaction (Shelley body, Ed25519 vkey witness, fee from the size-dependent protocol parameters) whose metadata under
  label 1967 commits the key with its encrypted Kubo envelope's CID and SHA-256 (`CardanoTransactionCodec.cs`,
  `CardanoLedgerKeyValueBackend.cs`). The old provider built transactions as JSON, which Cardano never accepts. Key
  derivation moved off a sibling Chaos.NaCl project reference that only resolved on one machine. yaci-store omits
  `tx_index`, so ordering uses block height and refuses ambiguous same-block order.
- Starknet now has real storage and passes 26/26 in both modes against starknet-devnet 0.11 + Kubo
  (`.github/workflows/starknet-native-storage.yml`). The repo now holds the Cairo contract (`contracts/src/lib.cairo`,
  OasisKv: per-owner key -> commitment, Scarb 2.21.0); the storage account writes commitments to encrypted Kubo
  envelopes and every write must report SUCCEEDED. The old provider called a `create_holon` entry point that was never
  in the repo.

Partly done, with open items:
- Turso: managed Turso and hosted WEB4 activation are still open.
- HoloOASIS.Edge: cross-agent replication on a live conductor is still open. The shared core-storage suite does not apply
  (2026-11-25): HoloOASIS.Edge and EdgeSQLiteOASIS are Edge Runtime replication/sync components, not storage providers;
  their unit tests run in `edge-runtime-validation.yml`.
- HoloOASIS.Desktop: done 2026-11-25. The Desktop adapter is a thin subclass of canonical HoloOASIS (same storage code and zome calls), and now runs the shared suite itself: 26/26 in both modes on a Holochain 0.7.1 conductor (`HoloOASISDesktopNativeStorageConformanceTests`, run by the `storage` mode of `.github/workflows/holo-desktop-provider-build.yml` next to canonical HoloOASIS). No provider change was needed. The two suites share one conductor and one agent, so they now run serially: a concurrent hApp install by both failed on the conductor with "head moved".
- HoloOASIS.Unity: the Android conductor device lifecycle is still open.
- Aztec: done 2026-11-26. Passes the shared suite 26/26 in both modes on a disposable Aztec 5.3.0 local network (`.github/workflows/aztec-native-storage.yml`). Records are Kubo raw blocks committed by the OasisKv Noir contract (public state, owner-only writes) through a pinned aztec.js worker. The fake REST storage was removed.

- Algorand: done 2026-11-25. Passes the shared suite 26/26 in both modes against its native record-box application on a disposable LocalNet (`.github/workflows/algorand-native-storage.yml`). Fixes: detail lookups by username/email read the stored detail record; holon graphs are saved (children were refused); batch saves acknowledge counts; Import and owner export work on enrolled records; metadata queries select from the decrypted records instead of public Indexer fields; an unversioned save publishes the next revision instead of failing the predecessor check. The suite needs the application funded for its box storage (fixture `OASIS_ALGORAND_RECORD_APP_FUNDING`).

Next, breadth first, in order of popularity (shared suite not yet run):
1. Pinecone, LanceDB (Cloud), Zilliz: hosted vector stores, need accounts
2. Appwrite, Fauna, Xata, Nile, CloudflareD1, VercelKV, NetlifyBlobs, Tigris (hosted; need accounts, see ACTIONS-FOR-DAVID)
3. BigQuery, Snowflake, Redshift, Databricks, MotherDuck (hosted analytics; need accounts)
   Marqo (one no-model index, lexical prefix listing), Druid and Pinot (append-only rows, newest per key wins, each
   write waits until the broker serves it) now pass 26/26 in both modes.
4. The remaining non-EVM chains with no storage run (Algorand storage, Elrond, Aztec, Miden and the bridge/oracle
   adapters) need real implementations; bridge/oracle adapters (Axelar, LayerZero, Chainlink, Hyperlane, deBridge,
   Connext, Chainflip, Gelato) are being classified as non-storage first.

Blocked: nothing new is blocked. Hosted-account blockers are already tracked in `Docs/WIP/Release and Status/ACTIONS-FOR-DAVID.md`.

## How to rerun the HyperDrive triad locally

Needs SQL Server LocalDB and an owned mongod replica set, for example
`mongod --replSet triadRs --port 27031 --bind_ip 127.0.0.1 --dbpath <scratch dir>`. The test initiates a single-node set if it is not already initiated. The test refuses any non-loopback MongoDB, and
it never touches the shared Atlas cluster.

```bash
cd "OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests"
OASIS_TRIAD_MONGODB_URI="mongodb://127.0.0.1:27031/?replicaSet=triadRs&serverSelectionTimeoutMS=3000" \
OASIS_TRIAD_SQLSERVER="(localdb)\\MSSQLLocalDB" \
dotnet test -p:PrioritySqlRoutingTests=true --filter "FullyQualifiedName~MongoSqliteSqlServerTriad" -m:2 -nr:false -p:UseSharedCompilation=false
```

Do not point it at the shared Atlas cluster. The test creates its own Mongo database, SQL database and temp SQLite file. It drops all of them when it finishes.
