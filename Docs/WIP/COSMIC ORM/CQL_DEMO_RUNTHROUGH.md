# CQL presenter run-through

Run from `C:/Source/OASIS-cosmic`. Use one host at a time and retain existing environments.
This is the presentation sequence; [launcher details](CQL_DEMO_LAUNCHER.md) own lifecycle options.

## 1. Start with the C# story

Open `Examples/CosmicCqlDemo` in your IDE. Begin with the grouped database walkthrough:

```powershell
# Inspect the selected runner before starting any services.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Databases -ShowPlan

# Launch the MongoDB / SQLite / Neo4j walkthrough.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Databases
```

Explain provider scope, typed filters, metadata, ordering, projection and aggregates.
Then show saving an edited holon, its committed delta, a no-op save and a stale-client
conflict. These are CQL/ORM demonstrations; they do not certify automatic failover,
load balancing or every replication scenario. Follow the runner's displayed URL.

## 2. Show provider breadth with Mongo pairing

For each provider below, show the C# query, execute the full membership query, then
its smaller subset. Explain the provider provenance and native/indexed assurance.
Use the browser page to display the same provider participation and HTTP results.

```powershell
# Oracle first: local native Oracle + Mongo, five records then four.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Oracle -Federated -ArchiveCompletedDemo

# After completing shutdown and cleanup, select ONE next provider.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider CosmosDB -Federated
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SolanaMetadata -Federated -ArchiveCompletedDemo
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider IPFS -Federated -ArchiveCompletedDemo
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SOLID -Federated -ArchiveCompletedDemo
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider ActivityPub -Federated -ArchiveCompletedDemo
```

| Preset | Expected participation | Explain explicitly |
|---|---|---|
| Oracle | Mongo2 + Oracle3; subset2 + 2 | Local Oracle, not hosted OCI. |
| CosmosDB | Mongo2 + Cosmos3; subset2 + 2 | Local emulator; hosted Azure remains pending. |
| SolanaMetadata | Mongo1 + retained Solana1; empty query0 | Local validator; explicit SQLite index; no new chain write or missing-history recovery. |
| IPFS | Mongo2 + IPFS3; subset2 + 2 | Bounded CID membership, not arbitrary content search. |
| SOLID | Mongo2 + SOLID3; subset2 + 2 | Authenticated resource membership, not arbitrary Pod search. |
| ActivityPub | Mongo2 + public statuses3; subset2 + 2 | GET-only status membership, not posting or ActivityPub server federation. |

## 3. Rehearse safely

For a bounded SDK/HTTP rehearsal, add `-VerifyOnly` to the chosen command.
For command discovery without services or fixture writes, add `-ShowPlan` instead.
Neither discovery nor an old report proves a fresh live pass after a code change.

Finish each host using its displayed shutdown instructions before starting another.
Oracle/Cosmos/Solana expose cleanup buttons; protocol hosts expose `POST /demo/shutdown`.
Do not discard pending journals or kill a runner during a write. Archive only completed,
unskipped evidence; pending or unknown outcomes require reconciliation.

Current launcher routing check: 8 single profiles/16 plans, 6 paired profiles/12 plans,
4 unsupported-pair and 3 archive rejections pass; listeners unchanged. This checks
routing only. Native proof and revision limits remain in [CURRENT_STATUS](CURRENT_STATUS.md).

Latest Oracle+Mongo bounded SDK/HTTP regression: 1 passed, 0 failed, 0 skipped;
runner exit0 with exact cleanup and no remaining owned27029/5090listeners.
TRX: `TestResults/OracleCurrent/oracle-cql-native-current.trx`, SHA256
`3D7CAF3F830D2E2A60849C3D33407C9CE02BE09F91D3987ACA6481DFD60B6DE2`.
This verifies the current working tree, including uncommitted backend foundations;
it is not a new browser, hosted-cloud or deployed revision certification.

## Ethereum + Mongo local MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Ethereum -Federated`, then open http://127.0.0.1:5097/. Explain that this is the retained local EVM chain31338 and native Mongo, not Ethereum mainnet. Click **Query all records** (five rows), then **Query two-record subset** (four). Point out the two provider copies of each selected coffee ID and the explicit consistency scope. The page shows the same C# identity-membership query used by SDK/HTTP tests. Stop through the demo shutdown route when finished; original chain/seed are preserved and the owned Mongo fixture is cleaned. [Detailed scope](ETHEREUM_CQL_MVP.md).

## Polygon + Mongo

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Polygon -Federated`, then open http://127.0.0.1:5098/. Show the explicit Polygon/Mongo scope and identity-membership C# query. Click **Query all records** (5), then **Query two-record subset** (4); point out that identical IDs retain separate source-labelled physical copies. State that the concrete Polygon adapter uses a retained local EVM deployment, not mainnet. Stop through the demo shutdown route; the original chain/seed survives and only the verified temporary Mongo fixture is removed. [Scope](POLYGON_CQL_MVP.md).

## Arbitrum V2 + Mongo

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Arbitrum -Federated`, then open http://127.0.0.1:5099/. Show the C# identity-membership query, all5/subset4 rows and provider copies. Explain that the shipping V2 Arbitrum adapter runs on the retained local EVM chain, not a hosted rollup. Stop the demo to clean the owned Mongo fixture while preserving original native state. [Scope](ARBITRUM_CQL_MVP.md).

## Google Firestore + Mongo

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Google -Federated`, then open http://127.0.0.1:5100/. Show explicit Google/Mongo scope and the C# ID-membership query; run all5/subset4 and inspect source-labelled copies. Explain that Firestore is a retained local emulator checkpoint, not hosted Google Cloud. Stop the host to clean the owned Mongo fixture while preserving the original Firestore seed/checkpoint. [Scope](FIRESTORE_CQL_MVP.md).

## Cosmos blockchain + Mongo

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider CosmosChain -Federated`, then open http://127.0.0.1:5101/. Show explicit Cosmos blockchain/Mongo scope and the C# ID-membership query. All5/subset4 retain both source copies. Explain that records are encrypted on the retained local Cosmos chain, separate from Azure Cosmos DB and not mainnet. Stop the host to clean the owned Mongo fixture while preserving the original contract/key/seed/receipts. [Scope](COSMOS_BLOCKCHAIN_CQL_MVP.md).

## Polkadot + Mongo

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Polkadot -Federated
```

Open http://127.0.0.1:5102/. This preset reuses the encrypted three-record seed on
the retained local Substrate chain. It is not a public Polkadot-network demo.

1. Show the explicit MongoDB/Polkadot scope and the displayed C# query.
2. Select **Query all records**: expect five physical results,
   including two Mongo copies and three native Polkadot records.
3. Select **Query two-record subset**: expect four physical results, two per
   provider. Provider labels identify the source of each copy.
4. Explain that this MVP filters a bounded, trusted ID catalogue. Arbitrary
   metadata predicates and a shared cross-provider snapshot are not promised.
5. Stop the demo. Its owned Mongo fixture is removed after validation; the native
   seed, key, signed preparations and ledger remain reusable.

Add `-VerifyOnly` for bounded SDK/HTTP acceptance without an interactive host.
See [current verification and limitations](POLKADOT_CQL_MVP.md).

## NEAR + Mongo

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider NEAR -Federated
```

Open http://127.0.0.1:5103/. Show the explicit MongoDB/NEAR scope and C# membership
query. **Query all records** returns five physical rows; **Query two-record subset**
returns four. Compare IDs and provider labels to show that source copies are retained.

This preset reuses three existing encrypted synthetic `TestHolon` records on the
retained local NEAR chain. No signing or source writes are needed. It demonstrates
bounded ID selection over an authorized catalogue, not arbitrary metadata predicates,
public NEAR or a distributed snapshot. Stop the demo to validate and remove its
owned Mongo fixture while keeping the original key/ledger/catalogue. Add
`-VerifyOnly` for bounded SDK/HTTP acceptance. [Scope and evidence](NEAR_CQL_MVP.md).

## Base + Mongo

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Base -Federated
```

Open http://127.0.0.1:5104/. Show the explicit MongoDB/Base scope and C# membership
query. **Query all records** returns five physical rows; **Query two-record subset**
returns four. Compare provider labels and shared IDs: both physical copies remain.

The concrete Base Web3Core adapter reads encrypted records on local EVM chain31338;
this does not demonstrate public Base or rollup finality. The trusted catalogue
bounds native key selection; arbitrary metadata filters remain unsupported. Stop
preserves the original native seed/key/ledger and removes the validated owned Mongo
fixture. Add `-VerifyOnly` for bounded acceptance. [Scope](BASE_CQL_MVP.md).

## Avalanche + Mongo

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Avalanche -Federated
```

Open http://127.0.0.1:5105/. Show the explicit MongoDB/Avalanche scope and readable
C# membership query. **Query all records** returns five physical rows; **Query
two-record subset** returns four. Compare provider labels and duplicate IDs to show
that source copies remain distinct.

This uses the actual BootLoader Avalanche factory and encrypted Web3Core backend
on local EVM chain31338. It does not demonstrate public Avalanche finality. The
factory now binds the configured chain identity into encryption. Stop preserves
the original contract/IDs/key/ledger and removes the validated Mongo fixture. Add
`-VerifyOnly` for bounded acceptance. [Proof and recovery limits](AVALANCHE_CQL_MVP.md).

## Azure Blob + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider AzureBlob -Federated`.
The retained Azurite source supplies three records and Mongo two copies: full query
returns five physical rows; the two-ID subset returns four. C#, HTTP and browser
share HolonManager federation and preserve provider provenance. Reads observe
separate moments. This is local Azurite, not hosted Azure; arbitrary blob discovery,
content predicates and full provider parity remain outside this MVP.
See [commands, tests and limits](AZURE_BLOB_CQL_MVP.md).

## LocalFile + Mongo MVP

`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider LocalFile -Federated`
serves localhost5108. A bounded authorized ID query returns five physical rows;
the two-ID subset returns four, preserving both provider copies. Native file reads,
process restart reuse and C#/HTTP/browser are verified. Source files are plaintext;
reads have separate observation times. [Tests, commands and limits](LOCALFILE_CQL_MVP.md).

## Aptos + Mongo direct preset

Start `Scripts/TestHosts/Start-CosmicAptosCqlNode.ps1` in terminal1, then
`Scripts/TestHosts/Run-CosmicAptosCqlNative.ps1 -Federated -Serve` in terminal2.
Browser localhost5109 shows full5/subset4 rows with provider provenance. Native,
restart, SDK, HTTP and browser are verified; main-hub enrollment remains pending.
The retained plaintext Move source/key is reused; no faucet or chain reset. Stop
the demo to clean Mongo, then Ctrl-C terminal1. [Tests and scope](APTOS_CQL_MVP.md).

## Sui + Mongo MVP

`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Sui -Federated` serves localhost5110
and owns its reusable local node/Mongo lifecycle. Full query returns5 physical rows,
two-ID subset4, with provenance. Native/restart/C#/HTTP/browser are verified on the
same dedicated encrypted source. Original deployment/Storage stays untouched; signed
preparations persist. No hosted/finality/snapshot or arbitrary content-query claim.
[Commands, evidence and limits](SUI_CQL_MVP.md).

## Redis + Mongo MVP

`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Redis -Federated` serves localhost5111.
Full query5/subset4 physical rows preserve provider copies. Native encrypted DB11,
exact ciphertext restart, C#/HTTP/browser and lifecycle cleanup are verified.
Original data/key persists; other Redis databases stay untouched. Direct demo
registration does not certify BootLoader registration or aliases. Bounded ID-query
scope only, no distributed snapshot. [Tests and commands](REDIS_CQL_MVP.md).

## PostgreSQL + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider PostgreSQL -Federated`.
Use `-ShowPlan` to inspect or `-VerifyOnly` for automated checks and teardown.
The teaching preset returns five physical rows, then four for a two-ID selection,
with provider labels. PostgreSQL uses one parameterized native SELECT over an
explicit trusted UUID catalogue; unsupported content predicates reject. This is
retained local canonical JSON, not application encryption or hosted certification.
Stop using the page button; the original PostgreSQL source is preserved and only
the verified temporary Mongo fixture is removed. [Full scope](POSTGRESQL_CQL_MVP.md).

## SQL Server + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SQLServer -Federated`.
Use `-ShowPlan` to inspect or `-VerifyOnly` for automated verification/teardown.
The preset returns five physical rows, then four for the two-ID subset, preserving
provider copies. SQL Server uses parameterized native UUID selection and paging;
unsupported content predicates reject. This is Windows-authenticated local LocalDB
canonical JSON, not Azure SQL or full LINQ certification. Stop with the page button;
original SQL Server source is retained, verified temporary Mongo fixture removed.
[Full scope, tests and evidence](SQLSERVER_CQL_MVP.md).

## DuckDB + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider DuckDB -Federated`.
Use `-ShowPlan` to inspect or `-VerifyOnly` for automated checks/teardown. The shared
C# example uses `Where(row => selectedIds.Contains(row.Id))`, returning five rows,
then four for the subset, with provider labels. DuckDB performs native UUID selection,
tombstone filtering and paging over its retained local file. Canonical JSON is
plaintext; this does not certify MotherDuck or full LINQ parity. Stop with the page
button; original embedded source stays and only verified temporary Mongo data is
removed. [Full scope and evidence](DUCKDB_CQL_MVP.md).

## Hosted Upstash + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Upstash -Federated`.
Use `-ShowPlan` for a non-launching preview or `-VerifyOnly` for checks/teardown.
The C# ID-membership example queries hosted Upstash and local Mongo, preserving
provider-labelled copies: five physical rows, four in the two-ID subset. Upstash
uses native bounded MGET, then pages the live bounded result. Source JSON is
plaintext; multi-command save atomicity/full LINQ parity are not certified.
The retained free scratch expires2026-10-31T21:15:49.3541451Z; expired admission or
missing source must be reconciled, never silently replaced. Stop with the page
button; only the verified temporary Mongo fixture is removed.
[Tests, commands and full scope](UPSTASH_CQL_MVP.md).

## Linux Litestream + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Litestream -Federated`.
`-ShowPlan` previews; `-VerifyOnly` runs automated checks and teardown. The preset
executes in existing Ubuntu WSL with pinned native runtimes and one reusable Linux
Mongo store, keeping both services on loopback. CQL ID-membership returns five
physical rows, then four for the subset, preserving provider-labelled copies.
Actual Linux live file replication and restored CQL are separately proved against
the same retained source lineage. Stop with the page button; original source,
replicas and reusable native store remain, verified temporary Mongo data is removed.
Windows Litestream directory-fsync failure and remote/PITR/crash acceptance remain
separate open scopes. [Setup, tests, resource limits and evidence](LITESTREAM_CQL_MVP.md).

## Volatile Memcached + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Memcached -Federated`.
`-ShowPlan` previews; `-VerifyOnly` checks and cleans up. The retained Linux runtime
starts one empty loopback cache generation using canonical synthetic identities.
Native CQL3/2/miss/tombstone/paging checks precede the shared C#/HTTP/browser preset:
five physical rows, four for the two-ID subset, preserving provider labels.
Independent native checks confirm cache values/CAS stay unchanged through the demo.
Stop with the page button to remove verified temporary Mongo data and retire the
volatile cache. Subsequent explicit warmup is not restart persistence; unknown
pending outcomes refuse replay. Existing Linux Mongo/runtime/store are reused.
[Scope, commands, tests and resource lifecycle](MEMCACHED_CQL_MVP.md).

## Native Valkey + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Valkey -Federated`.
`-ShowPlan` previews; `-VerifyOnly` checks and tears down. Independently verified
Valkey9.0.4 uses its retained encrypted source and the existing loopback Linux Mongo
store. Shared C# ID-membership returns five physical rows, then four for the two-ID
subset, preserving provider labels. Native tests cover missing/deleted records,
paging, budgets and unchanged ciphertext across a clean server restart. Stop with
the page button; Valkey source stays and verified temporary Mongo data is removed.
Full LINQ, cluster behavior and distributed snapshots remain separate scopes.
[Commands, evidence and lifecycle](VALKEY_CQL_MVP.md).

## Native KeyDB + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider KeyDB -Federated`.
`-ShowPlan` previews; `-VerifyOnly` checks and tears down. The reusable Linux
KeyDB6.3.4 runtime was built from pinned official source without vendor patches.
Its encrypted source survives native restart unchanged. The shared C# membership
query returns five physical rows, then four for the two-ID subset, retaining source
labels. Missing/deleted records, paging, budgets and HTTP errors are verified.
Stop with the page button; original KeyDB data remains, verified temporary Mongo
fixture is removed and services stop. Full LINQ/cluster/distributed snapshots stay
separate scopes. [Commands, tests and lifecycle](KEYDB_CQL_MVP.md).

## Native Dragonfly + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Dragonfly -Federated`.
`-ShowPlan` previews; `-VerifyOnly` checks and tears down. Pinned Dragonfly2.0.2
uses retained encrypted source and a completed native RDB snapshot; all original
record/index bytes survive a clean process restart. Native shutdown saves again,
so snapshot file metadata changes are separately tracked. The shared C# membership
query returns five physical rows, then four for the two-ID subset, preserving
provider labels. Stop with the page button; services stop, original source remains,
and verified temporary Mongo data is removed. This scope does not certify AOF,
crash durability, clusters or general LINQ. [Commands/evidence](DRAGONFLY_CQL_MVP.md).

## Native Garnet + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Garnet -Federated`.
`-ShowPlan` previews; `-VerifyOnly` checks and tears down. The explicitly admitted
second synthetic fixture uses the existing Garnet2.2.5 runtime and completed native
checkpoint/restart proof. Shared C# ID-membership returns five physical rows, then
four for the two-ID subset, preserving provider labels. Stop retains the source,
removes verified temporary Mongo data and stops services. The initial failed fixture
and its loss remain documented separately; no original writes were replayed. Full
LINQ/AOF/crash/cluster/HyperDrive acceptance remains separate.
[Commands, evidence and fixture history](GARNET_CQL_MVP.md).

## PocketBase + Mongo MVP

[Verified scope, commands and examples](POCKETBASE_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider PocketBase -Federated`
(add `-VerifyOnly` for bounded verification). The shared C# membership query
runs over retained native PocketBase0.40.6 and Mongo:5physical rows,4subset,
provider provenance preserved. Source is plaintext JSON; no distributed snapshot
or atomic conditional-write certification. Stop through the browser button;
the runner verifies temporary Mongo cleanup and preserves the original source.

## Supabase REST + Mongo MVP

[Verified scope, commands and C# example](SUPABASE_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Supabase -Federated`
(add `-VerifyOnly` for bounded verification). One CQL membership query returns
5physical rows or4for the subset, retaining provider copies. This uses real
JWT-authenticated PostgREST16.5/PostgreSQL, with retained plaintext JSON. Hosted
Supabase gateway/Auth and distributed snapshots remain unverified. Page shutdown
verifies temporary Mongo cleanup and preserves the original REST source.

## Firebase Realtime Database + Mongo MVP

[Verified scope, commands and C# example](FIREBASE_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Firebase -Federated`
(add `-VerifyOnly` for bounded verification). Native indexed key-equality union
plus Mongo yields5physical rows or4subset, preserving source provenance. This
uses cached RTDB4.11.2 emulator and original export/import checkpoint, distinct
from Firestore. No hosted-auth, distributed-snapshot or crashdurability claim.
Page shutdown verifies temporary Mongo cleanup and retains the original fixture.

## Optimism V2 + Mongo MVP

[Verified scope, commands and C# example](OPTIMISM_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Optimism -Federated`
(add `-VerifyOnly` for bounded verification). One native membership query returns
5physical rows or4subset through HolonManager, preserving provider copies. This
uses the actual V2 provider and encrypted original contract on retained local
EVM31338. Direct construction only; public OP Stack/finality and central registration
are not certified. Page shutdown cleans the temporary Mongo fixture and retains source.

## Rootstock V2 + Mongo MVP

[Verified scope, commands and C# example](ROOTSTOCK_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Rootstock -Federated`
(add `-VerifyOnly` for bounded verification). The shipping Rootstock factory uses
explicit local chain31338 and retained encrypted contract/records. CQL through
HolonManager returns5physical rows or4subset with provider copies preserved.
Public Rootstock/Bitcoin anchoring/finality and shared snapshots are unverified.
Page shutdown cleans temporary Mongo data and retains the original native source.

## BNB Chain V2 + Mongo MVP

[Verified scope, commands and C# example](BNB_CHAIN_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider BNBChain -Federated`
(add `-VerifyOnly` for bounded verification). The shipping V2 provider uses retained
encrypted contract/records on local EVM31338. CQL through HolonManager returns
5physical rows or4subset with source copies preserved. Direct construction only;
central registration/public BNB consensus/bridging/finality and shared snapshots
remain unverified. Page shutdown cleans temporary Mongo data and retains source.

## Fantom V2 + Mongo MVP

[Verified scope, commands and C# example](FANTOM_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Fantom -Federated`
(add `-VerifyOnly` for bounded verification). Shipping Fantom V2 uses retained
encrypted contract/records on local EVM31338. CQL through HolonManager returns
5physical rows or4subset with provider copies preserved. Direct construction only;
central registration/public network behavior/migration and shared snapshots remain
unverified. Page shutdown cleans temporary Mongo data and retains original source.

## Linea V2 + Mongo MVP

[Verified scope and examples](LINEA_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Linea -Federated`
(add `-VerifyOnly` for bounded verification). Shipping Linea V2 reads retained
encrypted records on local EVM31338 through HolonManager:5physical rows or4subset,
with provider provenance. Browser port5129; shutdown verifies temporary Mongo
fixture cleanup and preserves original source. Direct construction only; public
Linea network/finality/bridging, central registration and distributed snapshots
remain unverified.

## Scroll V2 + Mongo MVP

[Verified scope and examples](SCROLL_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Scroll -Federated`
(add `-VerifyOnly` for bounded verification). Shipping Scroll V2 reads retained
encrypted records on local EVM31338 through HolonManager:5physical rows or4subset
with provider provenance. Browser5130; shutdown verifies temporary Mongo fixture
cleanup and preserves original source. Direct construction only; public Scroll
network/finality/bridge, central registration and distributed snapshots unverified.

## Monad V2 + Mongo MVP

[Verified scope and examples](MONAD_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Monad -Federated`
(add `-VerifyOnly` for bounded verification). Shipping Monad V2 reads retained
encrypted records on local EVM31338 through HolonManager:5physical rows or4subset,
with provider provenance. Browser5131; shutdown verifies temporary Mongo fixture
cleanup and retains original source. Public Monad consensus/parallel execution,
central registration and distributed snapshots remain unverified.

## CouchDB + Mongo MVP

[Verified scope, examples and limits](COUCHDB_CQL_MVP.md). Launch
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider CouchDB -Federated`
(add `-VerifyOnly` for bounded verification). The shared HolonManager executes
bounded native identity queries: original CouchDB3 + Mongo2 =5physical rows;
two-ID subset =4. Browser5132 displays provider provenance. Shutdown verifies
Mongo fixture cleanup and retains original CouchDB records. Plaintext CouchDB
scope; cross-client CAS, distributed snapshots and full parity remain unverified.

## SurrealDB + Mongo MVP

[Verified scope and evidence](SURREALDB_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider SurrealDB -Federated`
(add `-VerifyOnly` for bounded acceptance). Retained authenticated native SurrealDB
records and Mongo copies pass through shared HolonManager:5physical rows, or4for
the two-ID subset. Browser5133 shows provider provenance. Shutdown cleans the
owned Mongo fixture and preserves original native records. Plaintext envelopes,
bounded identity filters and independent reads; broader parity remains open.

## ArangoDB + Mongo MVP

[Verified scope and evidence](ARANGODB_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider ArangoDB -Federated`
(add `-VerifyOnly` for bounded acceptance). Retained authenticated native ArangoDB
records and Mongo copies pass through shared HolonManager:5physical rows, or4for
the two-ID subset. Browser5134shows provider provenance. Shutdown cleans the owned
Mongo fixture and preserves original records/revisions. Plaintext JSON and bounded
identity queries; broader predicates, graph/history/CAS and full parity stay open.

## RavenDB + Mongo MVP

[Verified scope, certificates and evidence](RAVENDB_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider RavenDB -Federated`
(add `-VerifyOnly` for bounded checks). Native client-certificate-authenticated
RavenDB and Mongo copies query through shared HolonManager:5physical rows or4for
the two-ID subset. Browser5135shows source provenance. Shutdown cleans only the
owned Mongo fixture and preserves original records/change vectors. Plaintext JSON
envelopes and bounded identity predicates; full graph/history/CAS parity stays open.

## CockroachDB + Mongo MVP

[Verified native scope and evidence](COCKROACHDB_CQL_MVP.md). Run
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider CockroachDB -Federated`
(add `-VerifyOnly` for bounded checks). Secured native CockroachDB and Mongo copies
query through shared HolonManager:5physical rows or4for the two-ID subset, source
provenance visible on browser5136. Shutdown verifies temporary Mongo cleanup and
preserves original rows/MVCC timestamps. No PostgreSQL-server substitution; single
node internal development scope, broader predicates/CAS/distributed parity open.


## TimescaleDB + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider TimescaleDB -Federated`
(add `-VerifyOnly` for bounded checks). Native TimescaleDB2.31.0/PG17.11 uses TLS,
original records and restart checks. C#/HTTP/browser return five physical rows,
then four for the two-ID subset, retaining each provider's provenance. Browser
port5137. Stop through the demo control to verify cleanup and retain native source.
This is bounded identity filtering; hypertables/time-series aggregates and full
ORM parity remain open. [Commands, tests and hashes](TIMESCALEDB_CQL_MVP.md).


## Cassandra + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Cassandra -Federated`
(add `-VerifyOnly` for bounded checks). Authenticated native Cassandra4.1.12 uses
original records and real restart checks. C#/HTTP/browser show5physical rows,
then4for the two-ID subset with provider provenance. Browser5138; stop through
the demo control to check fixture cleanup and preserve native source. Native
transport is local plaintext; no TLS/hosted/replication/full-parity claim.
[Commands, tests and hashes](CASSANDRA_CQL_MVP.md).


## ScyllaDB + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider ScyllaDB -Federated`
(add `-VerifyOnly` for bounded checks). Native ScyllaDB2026.3.4 uses authenticated
original records, tablet-compatible schema and real restart verification. C#/HTTP/
browser show5physical rows then4for the2-ID subset, retaining provider provenance.
Browser5139; stop through the demo control to verify exact Mongo cleanup and keep
native source. Local plaintext transport, no hosted/TLS/replication/full-parity claim.
[Commands, tests, scope and hashes](SCYLLADB_CQL_MVP.md).


## Elasticsearch + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Elasticsearch -Federated`
(add `-VerifyOnly` for bounded checks). Elasticsearch9.5.5 uses authenticated HTTPS,
freeBasiclicense, original records and restart verification. C#/HTTP/browser show
5physical rows then4for the2-ID subset, retaining each provider copy/provenance.
Browser5140; stop through the demo control for exact Mongo cleanup/native retention.
Bounded identity selection only; full-text translation/replication/parity remain open.
[Commands, scope, tests and hashes](ELASTICSEARCH_CQL_MVP.md).


## OpenSearch + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider OpenSearch -Federated`
(add `-VerifyOnly` for bounded checks). Independent OpenSearch3.9.0 uses authenticated
HTTPS/securityplugin/customcerts, original rawHolon records and restart verification.
C#/HTTP/browser show5physical rows then4for the2-ID subset with provider provenance.
Browser5141; stop through the demo control to check exactMongo cleanup/native retention.
Bounded identity selection only; full-text translation/replication/parity remain open.
[Commands, scope, tests and hashes](OPENSEARCH_CQL_MVP.md).

## Couchbase + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Couchbase -Federated`
(add `-VerifyOnly` for bounded checks). The free Community8.0.2 native server and
Mongo return five physical rows, or four for the two-record subset. Browser5142
shows provider provenance. The spaced/commented C# example is
`Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs`, executed by HolonManager;
HTTP uses POST `/api/cql/federation` with `{ "preset": "all" }` or `"subset"`.
Only bounded identity equality/membership/Boolean selection/paging is admitted;
other predicates are rejected. Original JSON/exact unsigned CAS survive native
restart. Local authenticated plaintext loopback scope; no hosted, TLS, conditional
CAS write, distributed snapshot or full EF parity claim. Stop with the page button
so the runner verifies/removes only its temporary Mongo fixture and preserves the
original Couchbase source. [Proof and prerequisites](COUCHBASE_CQL_MVP.md).

## ClickHouse + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider ClickHouse -Federated`
(add `-VerifyOnly` for bounded checks). Free local ClickHouse26.8.36.3 uses authenticated
HTTPS/private CA. Native restart preserves JSON, UUIDs and exact UInt64 versions.
Browser5143 shows five physical rows or four for the subset, with source labels.
The spaced/commented C# example is `Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs`,
executed by HolonManager; HTTP POST `/api/cql/federation` accepts presets `all`/`subset`.
Only bounded identity equality/membership/Boolean selection/paging is admitted;
other predicates reject. FINAL uses analytical ReplacingMergeTree semantics, including
its existing millisecond-version tie risk; it is not atomic CAS, history or a shared
snapshot. No hosted/full-parity claim. Stop with the page button so the runner checks
and removes its temporary Mongo fixture while retaining the original ClickHouse
source. [Proof and prerequisites](CLICKHOUSE_CQL_MVP.md).

## InfluxDB + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider InfluxDB -Federated`
(add `-VerifyOnly` for bounded checks). Free local OSS2.7.12 uses authenticated HTTPS
and a private CA. Original points/JSON/timestamps survive native restart. Browser5144
shows five physical result rows or four for the subset, with provider labels.
The spaced/commented C# example is `Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs`,
executed by HolonManager; HTTP POST `/api/cql/federation` accepts presets `all`/`subset`.
Only bounded identity equality/membership/Boolean selection/paging is admitted.
Native controls prove UTF8 literals, future-head visibility and tied-head rejection;
this is not atomic CAS/history/full parity or a shared distributed snapshot. Six
retained source points include controls; three logical records are admitted by the
presets. Stop with the page button so the runner checks/removes only its temporary
Mongo fixture and preserves the original InfluxDB source. [Proof/prerequisites](INFLUXDB_CQL_MVP.md).

## Qdrant + Mongo MVP

Run `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Qdrant -Federated`
(add `-VerifyOnly` for bounded checks). Free local Qdrant1.16.3 uses authenticated
HTTPS/private CA. Original peer/JSON/zero vectors/native collection config survive
restart. Browser5145 shows five physical result rows or four for the subset, with
provider labels. The spaced/commented C# example is
`Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs`, executed by HolonManager;
HTTP POST `/api/cql/federation` accepts presets `all`/`subset`. Only bounded identity
equality/membership/Boolean selection/paging is admitted. This demonstrates payload
retrieval over the vector database, not embedding generation, similarity search,
atomic CAS, history or a shared distributed snapshot. Stop with the page button so
the runner checks/removes only its temporary Mongo fixture and retains the original
Qdrant source. [Proof/prerequisites](QDRANT_CQL_MVP.md).

Weaviate + Mongo is verified: all5/subset4 with provenance, retained TLS/gRPC source
and restart checks. [Commands and scope](WEAVIATE_CQL_MVP.md).
