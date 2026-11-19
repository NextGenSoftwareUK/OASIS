# CQL demo launcher

[Presenter sequence and expected results](CQL_DEMO_RUNTHROUGH.md).

Run from C:/Source/OASIS-cosmic. One interactive demo at a time; each runner owns
its build lease and verified fixture teardown. Do not nest a lease around the hub.
All examples below launch native-backed teaching presets with synthetic fixtures.
Hosted Azure remains unavailable until free-account setup; local Cosmos is labelled.

## Mongo-paired presets

```powershell
# Discover the command and URL without starting services or writing records.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Oracle -Federated -ShowPlan

# Start the Oracle teaching page; archive only a completed unskipped pass.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Oracle -Federated -ArchiveCompletedDemo

# Bounded SDK/HTTP verification instead of waiting for browser shutdown.
& Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Oracle -Federated -VerifyOnly -ArchiveCompletedDemo
```

| Provider argument | URL | Matching native participation | ArchiveCompletedDemo |
|---|---|---|---|
| CosmosChain | http://127.0.0.1:5101/ | Mongo2/retained Cosmos blockchain3, then2/2 | Omit; retained native contract/key/seed, temporary Mongo fixture |
| Polkadot | http://127.0.0.1:5102/ | Mongo2/retained Polkadot3, then2/2 | Omit; retained encrypted native seed, temporary Mongo fixture |
| NEAR | http://127.0.0.1:5103/ | Mongo2/retained NEAR3, then2/2 | Omit; original encrypted source/key, temporary Mongo fixture |
| Base | http://127.0.0.1:5104/ | Mongo2/Base3 on local EVM31338, then2/2 | Omit; retained encrypted native seed, temporary Mongo fixture |
| Avalanche | http://127.0.0.1:5105/ | Mongo2/Avalanche3 on local EVM31338, then2/2 | Omit; original contract/IDs and encrypted seed retained |
| Google | http://127.0.0.1:5100/ | Mongo2/retained Firestore emulator3, then2/2 | Omit; immutable seed/checkpoint, temporary Mongo fixture |
| Arbitrum | http://127.0.0.1:5099/ | Mongo2/shipping Arbitrum V2 adapter3, then2/2 on local EVM | Omit; retained chain/seed, temporary Mongo fixture |
| Polygon | http://127.0.0.1:5098/ | Mongo2/concrete Polygon adapter3, then2/2 on retained local EVM | Omit; retained chain/seed, temporary Mongo fixture |
| Ethereum | http://127.0.0.1:5097/ | Mongo2/local Ethereum3, then2/2 | Omit; retained local chain/seed, temporary Mongo fixture |
| AWS | http://127.0.0.1:5096/ | Mongo2/DynamoDB Local3, then2/2 | Omit; reusable AWS seed, verified temporary Mongo cleanup |
| Oracle | http://127.0.0.1:5090/ | Mongo2/Oracle3, then2/2 | Explicit opt-in; rejects failed/pending outcomes |
| CosmosDB | http://127.0.0.1:5092/ | Mongo2/local Cosmos3, then2/2 | Omit; Cosmos runner owns hash-preserved report reuse |
| SolanaMetadata | http://127.0.0.1:5091/ | Mongo1/Solana1 via explicit SQLite index;0empty retains assurance | Pass to preserve reviewed completed evidence |
| IPFS | http://127.0.0.1:5088/ | Mongo2/IPFS3, then2/2; native CID membership | Pass for completed evidence reuse |
| SOLID | http://127.0.0.1:5088/ | Mongo2/SOLID3, then2/2; native resource membership | Pass for completed evidence reuse |
| ActivityPub | http://127.0.0.1:5088/ | Mongo2/public Mastodon3, then2/2; native status membership | Pass for completed evidence reuse |

Use `-Federated` for every row. Actor, source scopes and readable catalogues are
server-owned. The Solana host uses independent bounded ingestion; no chain write.
Oracle, AWS, Ethereum, Polygon, Arbitrum, Google, CosmosChain, Polkadot, NEAR, Base and Avalanche are exposed through paired presets only. Plain Solana (schema2), SQLite
and the Databases group do not accept this new flag; the group retains its existing
federation walkthrough. Unsupported combinations reject before starting services.
Protocol `-Federated` already selects the multi-record mode; `-MultiRecord` is optional.

Follow the page's shutdown instructions. Oracle/Cosmos/Solana have cleanup buttons;
protocol hosts expose `POST /demo/shutdown`. Do not kill a runner midway through a
write or discard a pending journal. Unknown outcomes require reconciliation.

## Existing individual profiles

Without `-Federated`, existing SQLite, Databases, CosmosDB, Solana,
SolanaMetadata, IPFS, SOLID and ActivityPub launchers retain their current behavior.
Protocol `-MultiRecord` selects native several-record membership. `-VerifyOnly`
keeps acceptance bounded; `-ShowPlan` is read-only and starts no services.

## Verification scope

`Scripts/TestHosts/Test-CosmicCqlDemoHubRouting.ps1` checks8individual profiles,
16lifecycle plans,12paired profiles/24plans,10unsupported-pair rejections and3existing
archive rejections. Native/demo listeners remain unchanged during discovery.
Raw report:TestResults/ProtocolCqlCurrent/database-hub-routing.json.
Discovery proves routing only; native provider proof remains in each linked report
from [current status](CURRENT_STATUS.md), including explicit pre/post-merge scope.


Post-merge Oracle hub execution also passes1/1native SDK/HTTP case, hash
28CA5EE9AA3B227E2320A1A3A492369D8661B94D82FD105A45D6CF47C4360239,
with exact cleanup and service teardown. Discovery JSON SHA256
541AAFB947BF22DFD766621ECEF2C36784FA1F6725AD546C563D511F143CC37E.
Other paired hub routes are discovery-tested against their previously verified
individual runners; this milestone does not claim fresh native reruns of all providers.

## AWS local MVP

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider AWS -Federated
```

Open http://127.0.0.1:5096/ after startup. Native AWS SDK + HTTP and actual browser
buttons verify5/4 rows with provider copies. Bounded verification uses -VerifyOnly.
The existing DynamoDB runtime is reused read-only; persistent CQL data are inside
this worktree. No hosted AWS or arbitrary metadata-query claim. See [AWS MVP](AWS_CQL_MVP.md).

## Ethereum local MVP

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Ethereum -Federated
```

Open http://127.0.0.1:5097/ after startup. Add `-VerifyOnly` for bounded native/SDK/HTTP verification. The all preset returns Ethereum3 + Mongo2 physical rows; subset returns2 +2. Native Ethereum reads use one observed local block number; federation retains provider copies and makes no shared-snapshot/finality/mainnet claim. Original chain/key/seed are reused; shutdown removes only the verified temporary Mongo fixture. [Scope](ETHEREUM_CQL_MVP.md).

## Polygon local MVP

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Polygon -Federated
```

Open http://127.0.0.1:5098/ after startup. Add `-VerifyOnly` for bounded native/SDK/HTTP checks. Concrete PolygonOASIS uses its existing Web3Core contract/encrypted records on local chain31338; this is not Polygon mainnet. All/subset yield5/4 physical rows, with both provider copies preserved. [Scope and evidence](POLYGON_CQL_MVP.md).

Arbitrum V2 uses `-Provider Arbitrum -Federated` (5099), with the same5/4 teaching flow. BootLoader selects the Web3Core variant inV2; no legacy/hosted/rollup/mainnet claim. [Scope](ARBITRUM_CQL_MVP.md).

Google Firestore uses `-Provider Google -Federated` (5100), or add `-VerifyOnly` for bounded checks. Reuses the exact immutable local checkpoint, produces5/4 physical rows and labels emulator scope explicitly. No hosted Google claim. [Guide](FIRESTORE_CQL_MVP.md).

Cosmos **blockchain** uses `-Provider CosmosChain -Federated` (5101), distinct from `-Provider CosmosDB` (Azure emulator). Add `-VerifyOnly` for bounded native/SDK/HTTP checks. The existing encrypted native seed is reused without new submissions, and all/subset yield5/4 physical rows. No Cosmos Hub/mainnet or shared-snapshot claim. [Guide](COSMOS_BLOCKCHAIN_CQL_MVP.md).

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

Weaviate scoped MVP (30Oct): authenticated TLS native gRPC query, restart preservation,
Mongo-paired C#/HTTP/browser5/4 verified; 38 controls pass. Explicit ID catalogue only.
[Scope, commands and remaining gaps](WEAVIATE_CQL_MVP.md).

Meilisearch + Mongo scoped MVP:39controls/native/restart/C#/HTTP/browser5/4 pass.
Native HTTPS key fetch and terminal native write tasks; explicit ID predicates.
[Commands, proof and remaining gaps](MEILISEARCH_CQL_MVP.md).

Typesense + Mongo scoped MVP:38controls/native/restart/C#/HTTP/browser5/4 pass.
Explicit ID-filter queries preserve original document bytes. Native collection
created_at changes during native replay; no timestamp-immutability claim.
[Commands, proof and gaps](TYPESENSE_CQL_MVP.md).

Solr + Mongo scoped MVP:48controls/native/restart/C#/HTTP/browser5/4 pass.
Canonical stored JSON, hard-commit/version acknowledgements, bounded RealTimeGet.
[Explicit schema, commands, proof and remaining gaps](SOLR_CQL_MVP.md).

Chroma + Mongo scoped MVP:50controls/native/restart/C#/HTTP/browser5/4 pass.
`Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Chroma -Federated -Serve`
opens teachingHTTP5150; `-VerifyOnly` runs bounded acceptance instead. Explicit
trusted key catalogues, native multi-ID get, source provenance and original JSON/
collection values preserved across restart. Loopback plaintext local scope;
no vector similarity/general predicates/distributed snapshot/full parity claim.
[Chroma scope, examples and evidence](CHROMA_CQL_MVP.md).
