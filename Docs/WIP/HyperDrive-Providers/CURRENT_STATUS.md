# HyperDrive and provider completion: current status

Last updated: 2026-11-24. This file stays short: replace sections, do not prepend log entries (append them to the archive). The old 700 KB running log, which also covered OPORTAL and billing, is now
`Archive/CURRENT_STATUS_LOG_TO_2026-11-23.md`. The per-provider detail is in `PROVIDER_MATRIX.md` and
`CONFORMANCE_RESULTS.json`.

Scope of this workstream: HyperDrive failover, replication and load balancing, plus provider storage completion.
OPORTAL, the ONODE popup and billing UI are now owned by a different session. CQL/WEB4 sandbox work happens on
`work/cosmic-cql-ton-public` in `C:\Source\OASIS2`.

## Progress (cloud-hyperdrive-A, updated 2026-11-25 02:55 UTC)

Done in CI (native core-storage suite 26/26, sync + async):
- TelosOASIS and SEEDSOASIS: storage now goes through the EOSIO (Antelope) implementation; `antelope-family-native-storage.yml`, run 45518765561.
- DaprOASIS: gRPC health probe, emptied key indexes removed; `popular-providers-native-storage.yml`, run 45519314086.
- GUNOASIS: real GUN wire protocol, chunked node replies merged; run 45520161955.

Passing 26/26 in both modes locally against real backends, waiting for CI (`work/cloud-hd-scuttlebutt` carries these in one `popular-providers-native-storage.yml` run):
- MatrixOASIS (Synapse): records as encrypted room state events. Tests passed in run 45521345637; the job failed only removing Synapse-owned fixture files (fixed on the branch).
- OrbitDBOASIS (@orbitdb/core 4 service): stable identity and address across restarts.
- ArweaveOASIS (arlocal): owner-pinned, revisioned, encrypted records.
- ScuttlebuttOASIS (ssb-db2 service): records in a signed feed.
- AppwriteOASIS (self-hosted 1.7.4), PineconeOASIS (Pinecone Local), TablelandOASIS (local validator + Hardhat), CelestiaOASIS (local devnet), CeramicOASIS (ceramic-one 0.58.0).

Also passing locally: BlueSkyOASIS (self-hosted PDS) and EclipseOASIS (delegates to SolanaOASIS; eclipse-native-storage.yml). In progress: FilecoinOASIS.

Blocked on hosted accounts or a retire decision: see "Hosted-only providers" in `Docs/WIP/Release and Status/ACTIONS-FOR-DAVID.md`.

CI capacity is the bottleneck: provider runs queue for over an hour behind Development pipelines.

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

Done (89 pass the native core-storage-v2 suite 52/52 against their real backend; ActivityPub passes capability-v1 5/5):

- Every priority provider is done: HoloOASIS, Solana, MongoDB, SQLite, Neo4j, Supabase, SQL Server, Oracle (OracleDB),
  AWS, IPFS, NEAR, Cosmos, Azure (CosmosDB + Storage), Google Cloud, Polkadot, ActivityPub, SOLID, Polygon, Arbitrum,
  TRON, EOSIO, TON and Hedera (Hashgraph).
- On 2026-11-23 the source of all 23 priority providers was scanned for `NotImplementedException`, TODO stubs, placeholders
  and mocks. Every hit was commented-out legacy code or an explanatory comment. No shipping path is a stub.
- Others (Neon and Convex are not priority providers and take their normal place in popularity order): Neon, Convex, Ethereum, Avalanche, BNB Chain, Sui, AWS S3, Cloudflare, DynamoDB, Firebase, PocketBase, Turso, Upstash,
  ThreeFold, Cassandra, CockroachDB, Dragonfly, DuckDB, Elasticsearch, Garnet, KeyDB, LocalFile, Meilisearch, Memcached,
  Milvus, MinIO, OpenSearch, PlanetScale, PostgreSQL, Qdrant, RavenDB, Redis, ScyllaDB, Solr, TimescaleDB, Typesense,
  ValKey, Weaviate.
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

Partly done, with open items:
- Turso: managed Turso and hosted WEB4 activation are still open.
- HoloOASIS.Edge: the shared suite and cross-agent replication are still open.
- HoloOASIS.Unity: the Android conductor device lifecycle is still open.
- Algorand: chain reads and payments pass; durable storage is still open.

Next, breadth first, in order of popularity (shared suite not yet run):
1. Pinecone, LanceDB (Cloud), Zilliz: hosted vector stores, need accounts
2. Appwrite, Fauna, Xata, Nile, CloudflareD1, VercelKV, NetlifyBlobs, Tigris (hosted; need accounts, see ACTIONS-FOR-DAVID)
3. BigQuery, Snowflake, Redshift, Databricks, MotherDuck (hosted analytics; need accounts)
   Druid, Pinot and Marqo are implemented but heavy (Pinot/Druid ingest one segment or task per row); queued after the chains.
4. The remaining non-EVM chains need real storage implementations, not fixes:
   - Cardano: transactions are built as JSON objects, but Cardano only accepts CBOR-encoded signed transactions.
   - Starknet: holon save falls back to HolonManager and calls a Cairo `create_holon` entry point that is not in the repo.

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
