# PouchDB and MongoDB local CQL demo

Select three retained PouchDB Holons and two temporary MongoDB copies through
HolonManager and the shared C# CQL walkthrough. Full selection returns five
physical rows; the two-ID subset returns four, each labelled with its provider.
MovingProviderReads is explicit; this is not a distributed snapshot.

## Prepared Windows environment

One pinned PouchDB9.0.0 environment uses the installed Node18.18.0 runtime and
official express-pouchdb4.2.0 middleware in minimumForPouchDB mode. Actual native
HTTP routes read a persistent LevelDB store on loopback19600. There is no CouchDB
substitute or memory backend. Runtime files, Node binary, source descriptor,
original revisions and payload hashes are checked by the launcher.

The synthetic teaching service is unauthenticated and loopback-only. Hosted
access, TLS and restricted credentials require separate qualification. Hosted
WEB4 tokens and cookies are never forwarded. Runtime, package lock, native
source and receipts live in `LocalState/COSMIC/RecoveryLaptopPouchDBCql`.
The new source contains four canonical documents: three live and one deleted;
one live record includes an embedded child excluded from the root projection.
The missing-ID control stays absent. Initialization cannot overwrite a source
or replay an uncertain write. Normal demos only read PouchDB.

Mongo reuses the admitted recovery MongoDB8.2 runtime/store and replica set27029.
The fixed temporary database `cosmic_cql_mongo_pouchdb_demo` must be absent
before the three journalled saves. Two copies are selected and a third proves
catalogue exclusion. The original PouchDB source is retained between demos.

## Build and start

From the matching task checkout, build only when code or outputs changed:

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-pouchdb-demo-build -Operation {
    dotnet build Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests.csproj /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE) { throw 'PouchDB demo build failed.' }
}
```

Start the interactive demo:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicPouchDbMongoDemo.ps1 -Serve
```

The launcher holds one provider build lease, refuses occupied ports or unresolved
fixtures and starts only its own processes. Wait for demo-ready.json in the cache,
then open [the local page](http://127.0.0.1:5141/). Choose **Query all records**,
then **Query two-record subset**, and inspect counts, IDs and provenance.
Unknown presets and supplied actors are rejected; provider failure returns an
explicit error without partial rows. Native conflicts must be resolved before
selection; missing databases and authentication failures are errors.

## Stop and retain

Use **Stop demo (keep original PouchDB source)** and wait for the runner to finish.
It verifies and drops only the exact temporary Mongo fixture, checks absence,
and rechecks original native payload hashes and revision1. The owned HTTP server
stops and explicitly closes its native database; Mongo closes storage and exits.
Both receive Ctrl+C through their exact hidden consoles. Unrelated processes
are never adopted. Closing a browser tab alone leaves the runner active.

Preserve original intent, receipts, logs and pending state after any failure.
Reconcile unknown outcomes instead of replaying saves or generating another
source. Native restart verification does not certify crash durability, arbitrary
replication/conflict resolution, wider graph/history queries, full storage or
EF/NHibernate parity, or hosted enrollment. Exact evidence and open gates are
in [the scope record](../WIP/COSMIC%20ORM/POUCHDB_CQL_MVP.md).
