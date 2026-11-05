# SurrealDB and MongoDB Windows CQL demo

This teaching demo selects three retained SurrealDB Holons and two temporary
MongoDB copies through HolonManager and the shared C# CQL walkthrough. Full
selection returns five physical rows; the two-ID subset returns four, with each
provider shown. MovingProviderReads is explicit; no distributed snapshot claim.

## Prepared recovery laptop

One official SurrealDB3.3.5 Windows executable is retained in
`LocalState/COSMIC/RecoveryLaptopSurrealDB335`. Its version, exact322004480-byte
size and vendor SHA256 are recorded in runtime.json. No service/PATH install or
additional Java/Linux environment. The teaching node uses authenticated,
loopback-only18001 and one persistent SurrealKV source under windows-source.
This is a newly admitted recovery source, separate from the original laptop's
Linux records and receipts. Missing, changed or unfinished admission stops use;
initialization never overwrites an existing source.

The query provider uses a database-scoped VIEWER credential. Its native HTTP
authentication headers identify the database where that account exists, through
the official SDK's HTTP factory. Root credentials provision the owned local
environment and independently verify unchanged records; CQL uses the reader.
Credentials stay in private local source files, never the web page or hosted
sign-in. Do not publish those files or forward hosted bearer/cookie credentials.

Mongo reuses the admitted MongoDB8.2 recovery environment and replica set27029.
The three temporary records use one initially absent database,
`cosmic_cql_mongo_surrealdb_windows_demo`, with durable pending intent. Two are
selected copies; the third proves catalogue exclusion. No SurrealDB save replay.

## Build and start

From the matching task checkout, rebuild only when source or outputs changed:

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-surreal-windows-build -Operation {
    dotnet build Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.IntegrationTests.csproj /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE) { throw 'SurrealDB consumer build failed.' }
}
```

Start the interactive demo:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicSurrealDbMongoDemo.ps1 -Serve
```

The launcher holds the shared build lease and starts only its own processes.
Wait for windows-source/demo-ready.json, then open
[the local query page](http://127.0.0.1:5140/). Start one native demo at a time.
Choose **Query all records**, then **Query two-record subset**. Inspect actual
counts, IDs and provider provenance. Unknown presets and caller-supplied actors
are rejected; inactive-provider failure returns an error without partial rows.

## Stop and retain

Use the local **Stop demo** button and wait for the runner to finish. The fixture
checks and removes only its exact Mongo database, verifies absence and rechecks
the original SurrealDB native hash. Both owned services receive Ctrl+C through
their exact consoles; unrelated console members are refused. Closing the tab
does not stop the runner. Retain native source, credentials, ACKs and receipts.

Any failure preserves original logs and pending state. Reconcile unknown writes;
do not reseed, overwrite receipts or choose fresh IDs to bypass them. SurrealDB's
native shutdown currently emits a retained SurrealKV warning even on exit0;
logical restart preservation does not certify crash durability. Wider graph,
history/content queries, conditional writes, full storage/EF/NHibernate parity,
HyperDrive replication and hosted enrollment remain separate acceptance gates.
Exact evidence and remaining gaps: [scope record](../WIP/COSMIC%20ORM/SURREALDB_CQL_MVP.md).
