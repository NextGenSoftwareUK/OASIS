# Meilisearch and MongoDB local CQL demo

This Windows teaching demo reads three retained Meilisearch documents and two
temporary MongoDB copies through HolonManager. The full query returns five
physical rows; the subset returns four, with each source provider visible.
It uses the actual Meilisearch REST API and MongoDB driver. It is separate from
the hosted WEB4 playground and does not receive its bearer or refresh cookie.

## Retained environment

The recovery laptop has one pinned community Meilisearch 1.56.1 executable,
native database, protected local master key, and fixed source catalogue under
`LocalState/COSMIC/RecoveryLaptopMeili1561`. The runner verifies the executable
digest and original acknowledged document batch before and after each demo.
The source uses canonical plaintext Holon JSON. The master key stays in
Windows DPAPI storage and the owned process environment.

The Mongo side reuses the admitted recovery-laptop 8.2 replica set and its
existing runtime/data. `Get-CosmicMongoDemoSource.ps1 -RecoveryMongo` verifies
that admission. Missing runtimes, source records or admission stop the run;
they are not replaced or seeded implicitly. A Linux environment is not needed.

Before first use on another machine, provision and qualify an explicitly owned
source. Do not copy another user's protected key or treat these laptop-specific
paths as universal installation defaults. Runtime and bootstrap provenance are
in [the scope/evidence record](../WIP/COSMIC%20ORM/MEILISEARCH_CQL_MVP.md).

## Build and start

Run from the task checkout root. Rebuild only if the current integration outputs
are missing or source has changed:

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-meili-demo-build -Operation {
    dotnet build Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.IntegrationTests.csproj /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE) { throw 'Meilisearch demo build failed.' }
}
```

Start the retained interactive demo:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicMeilisearchMongoDemo.ps1 -Serve
```

The runner takes the provider lease, refuses occupied demo ports or unresolved
fixture journals, and starts only its own loopback processes. It verifies the
manager/HTTP checks before opening the interactive host. Wait for
`LocalState/COSMIC/RecoveryLaptopMeili1561/demo-ready.json`, then open
[the local demo](http://127.0.0.1:5116/).

Each explicitly started session has its own nonsecret run ID and evidence files.
It reuses the same native Meilisearch store and Mongo environment. One small
temporary Mongo database must be absent before its three fixture records are
journalled and saved. Two records participate in federation; the third verifies
catalogue exclusion. No Meilisearch seed is replayed.

## Present the query

Choose **Query all records**, then **Query two-record subset**. The page shows
the actual returned JSON, physical row count, provider and record identity.
Copies of the same ID remain separate rows.

The shared C# walkthrough captures an expression and supplies trusted per-source
read catalogues:

```csharp
source.Where(row => selectedIds.Contains(row.Id));
await manager.ExecuteFederationQueryAsync(plan, adapters);
```

See [IdentityFederationWalkthrough.cs](../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
for the complete source/policy/plan sequence. Caller-supplied IDs are selection,
not authorization. The HTTP host accepts fixed preset names and rejects attempts
to supply an actor or unknown preset.

## Stop and retain evidence

Use **Stop demo (keep original Meilisearch source)** and wait for the runner to
exit. The fixture verifies the exact temporary Mongo contents before dropping
that database and confirming absence. Meilisearch documents must still match
their original acknowledged batch. The owned native processes are then stopped.
Closing the tab alone does not stop the runner.

If a run fails, retain its original intent, logs and pending fixture journal.
Do not delete evidence, reseed, or use another run ID to bypass unresolved state.
The original paired startup failure remains recorded even though explicit
reconciliation and later functional checks passed. Its cause remains unknown;
there is no uptime or restart-reliability guarantee.

Compact receipts stay beside the retained source. Regenerable reports are under
`TestResults/MeilisearchCqlNativeCurrent`; owned output accounting is in the
[recovery-laptop ledger](../WIP/COSMIC%20ORM/RECOVERY_LAPTOP_CLEANUP_LEDGER.md).

## Scope

This demonstrates bounded ID CQL, native reads, manager federation, local HTTP,
and browser interaction. It does not certify hosted WEB4 enrollment, independent
visitor authorization, HyperDrive replication, native write concurrency, history,
graphs, general text/search predicates or full ORM parity. Reads are moving
provider observations, not a shared distributed snapshot. Windows process
termination after a quiescent run is not a general crash-durability proof.
