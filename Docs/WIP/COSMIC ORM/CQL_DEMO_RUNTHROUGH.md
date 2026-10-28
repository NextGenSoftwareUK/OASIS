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
