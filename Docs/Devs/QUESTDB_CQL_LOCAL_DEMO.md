# QuestDB symbol CQL local demo

Query the same enrolled symbol metadata through native QuestDB and private
MongoDB copies. This is a read-only CQL projection of three synthetic symbols,
separate from the QuestDB holon storage provider and hosted WEB4 enrollment.
No prices or amounts are queried.

## Start and present

From the prepared recovery checkout `C:/Source/OASIS2`:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicQuestDbMongoDemo.ps1 -Serve
```

Open <http://127.0.0.1:5134/>. Choose **Query all records**, then
**Query two-record subset**. The full query returns three QuestDB rows and two
Mongo copies. The subset selects BTC-USDT and ETH-USDT, returning two from each.
Each physical copy retains its provider, even when copies share a Holon ID.
The results explicitly use moving provider reads, not a distributed snapshot.

Finish with **Stop demo (keep QuestDB symbols)** and wait for the runner to exit.
It verifies and removes only its temporary Mongo database, checks the original
QuestDB symbols, and stops both owned native processes. The retained QuestDB
source and receipts remain. Run one native teaching demo at a time.

## What the query does

The trusted application captures these CQL presets; browser input only names
one preset and cannot change its actor, providers or authorized catalogues:

```csharp
source.Where(row => selectedSymbols.Contains(
    CosmicQueryMetadata.Value<string>(row, "QuestDbSymbol")));

await manager.ExecuteFederationQueryAsync(plan, adapters);
```

The QuestDB adapter reads the enrolled `cql_demo_symbols` table through its native
REST SQL endpoint. It validates exact selected symbols, schema and row count.
Mongo runs the same metadata predicate over its private copies. The manager
keeps provider provenance. Native query failure fails the complete operation.

## Prerequisites and retained data

The admitted QuestDB10.1.0 Windows linked runtime remains under
`LocalState/COSMIC/RecoveryLaptopQuestDB1010`. This command reuses it and the
admitted Mongo8.2 replica/store; it installs nothing. Ports29000,27029 and5134
must be free. Services bind to loopback; the native SQL endpoint is an owned
teaching database, not a public restricted-query service. Hosted sign-in is
never forwarded.

The separate retained source is `browser-source` under that runtime cache.
It replaces no historical cohort: the prior HTTP fixture was verified and
removed after its accepted run. Initializing a fresh browser source is explicit:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicQuestDbMongoDemo.ps1 -Initialize
```

Initialization refuses an existing source directory. Subsequent runs require
native bootstrap acknowledgements and unchanged source identity/configuration.
Unfinished process or Mongo journals stop the run; preserve and reconcile them
without replaying writes or creating another cohort.

Hosted HTTPS qualification, public endpoint SQL restrictions, enrollment and
replication remain open. This example does not certify PGWire Holon writes,
general metadata/text queries, exact financial arithmetic or full ORM parity.
See [current evidence and remaining gates](../WIP/COSMIC%20ORM/QUESTDB_PUBLIC_SOURCE_STATUS.md)
and [the laptop cleanup ledger](../WIP/COSMIC%20ORM/RECOVERY_LAPTOP_CLEANUP_LEDGER.md).
