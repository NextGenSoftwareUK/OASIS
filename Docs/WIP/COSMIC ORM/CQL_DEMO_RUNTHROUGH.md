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
