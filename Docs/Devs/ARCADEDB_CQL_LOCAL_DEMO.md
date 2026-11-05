# ArcadeDB and MongoDB local CQL demo

This Windows teaching demo queries three retained ArcadeDB records and two
MongoDB copies through HolonManager. The full query returns five physical rows;
the two-ID subset returns four. Each returned row identifies its native provider.
It uses actual ArcadeDB SQL over HTTP and the MongoDB driver. The local page
receives no hosted WEB4 bearer or refresh cookie.

## Prepared environment

The recovery laptop has one pinned ArcadeDB 26.11.1 base distribution and native
source under `LocalState/COSMIC/RecoveryLaptopArcade26111`. It reuses the already
qualified Temurin 21.0.13+9 JRE under RecoveryLaptopSolr1010. Runtime manifests are
verified before execution; no second Java, system/PATH or service install occurs.
ArcadeDB binds only 127.0.0.1:24800 with HA/optional protocol plugins off, using
low-ram profile and a 64 MiB initial/256 MiB maximum JVM heap.

The native root password stays protected by Windows DPAPI and in owned process
environments, not command arguments, source metadata or the browser. This is an
owned loopback teaching account; native least-privilege credentials and hosted
ArcadeDB enrollment remain separate qualification work. Do not copy another
user's protected password or treat these laptop-specific paths as universal.
Before use elsewhere, provision and qualify an explicitly owned source.

Mongo reuses the admitted recovery laptop MongoDB 8.2 replica set/runtime/data through
`Get-CosmicMongoDemoSource.ps1 -RecoveryMongo`. Missing runtimes, source identity,
original bootstrap ACKs or changed teaching values stop the run. Nothing is
provisioned, repaired or reseeded implicitly. The original three live records,
deleted control and absent control remain in the same ArcadeDB database.
Provenance and exact test evidence are in [the scope record](../WIP/COSMIC%20ORM/ARCADEDB_CQL_MVP.md).

## Build and launch

From the task checkout root, rebuild only when outputs are absent or source changed:

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-arcade-demo-build -Operation {
    dotnet build Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS.IntegrationTests.csproj /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE) { throw 'ArcadeDB demo build failed.' }
}
```

Start the interactive demo:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicArcadeDBMongoDemo.ps1 -Serve
```

The launcher holds the provider build lease, refuses occupied ports or pending
fixtures, and starts only its owned processes. It verifies manager/HTTP checks
before serving the page. Wait for
`LocalState/COSMIC/RecoveryLaptopArcade26111/demo-ready.json`, then open
[the local query page](http://127.0.0.1:5137/).

An explicit session has its own nonsecret run ID/evidence, reusing the same native
source and Mongo environment. The fixed temporary database
`cosmic_cql_mongo_arcadedb_demo` must be absent before its three journalled saves:
two copies of native IDs and a third record proving catalogue exclusion. No
ArcadeDB seed/save occurs during the demo.

## Present and stop

Choose **Query all records**, then **Query two-record subset**. Inspect the actual
JSON count, physical row count, provider and ID. Native and Mongo copies retain
separate rows. The page states MovingProviderReads; it does not claim a distributed
snapshot. The complete expression/policy/planning code is in
[IdentityFederationWalkthrough.cs](../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs).
Record selection does not grant read authority. HTTP accepts fixed presets and
rejects caller-supplied actors or unknown presets. Native failure returns an
explicit error without partial rows.

Use **Stop demo (keep original ArcadeDB source)** and wait for the runner to exit.
It verifies the exact temporary Mongo contents, drops the database and checks
absence. All four original ArcadeDB documents must still match acknowledged
teaching values and their hashes from before the run. The owned JVM shuts down
through ArcadeDB's native shutdown API; the launcher stops its owned Mongo
process. Closing the tab alone leaves the runner active.

If a run fails, preserve its intent, logs and pending journal. Reconcile the
original run; do not delete evidence, replay saves or choose another ID to bypass
unfinished state. This is bounded CQL/manager teaching coverage. Wider mapping,
CRUD/storage conformance, owned writes, full ORM/HyperDrive and hosted enrollment
remain separate gates.
