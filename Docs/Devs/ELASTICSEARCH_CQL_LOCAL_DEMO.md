# Elasticsearch and MongoDB local CQL demo

This Windows teaching demo reads three retained Elasticsearch Holons and two
MongoDB copies through HolonManager. The full query returns five physical rows;
the two-ID subset returns four. Each row shows its source provider. It uses the
real Elasticsearch REST API and MongoDB driver. No hosted WEB4 bearer or refresh
cookie is forwarded to the local page or native backend.

## Prepared environment

The recovery laptop retains one pinned Elasticsearch8.19.24 Windows bundle and
its complete vendor JDK under `LocalState/COSMIC/RecoveryLaptopElasticsearch81924`.
Runtime manifests, source identity and exact configuration are checked before
startup. The verified520MB archive has been removed. No system/PATH install.

The single teaching node binds HTTP19400 and transport19500 only to loopback.
Security is disabled only for this isolated teaching configuration; hosted TLS,
authentication and least-privilege access remain separate gates. Heap128/512MiB,
two processors. Default disk watermarks remain unchanged. Native index health
must be green before source reads. Missing or changed admission/runtime/source
stops the run; the launcher never recreates or reseeds the retained index.

Mongo reuses the admitted recovery laptop MongoDB 8.2 replica set/runtime/data through
`Get-CosmicMongoDemoSource.ps1 -RecoveryMongo`. Native provenance, failures and
exact acceptance evidence are in [the scope record](../WIP/COSMIC%20ORM/ELASTICSEARCH_CQL_MVP.md).

## Build and start

From the task checkout root, build only when outputs are missing or source changed:

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-elasticsearch-demo-build -Operation {
    dotnet build Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.IntegrationTests.csproj /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE) { throw 'Elasticsearch demo build failed.' }
}
```

Start the interactive demo:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicElasticsearchMongoDemo.ps1 -Serve
```

The launcher holds the provider build lease, refuses occupied ports or unfinished
fixtures, and starts only its own processes. It runs manager/HTTP checks before
serving the page. Wait for
`LocalState/COSMIC/RecoveryLaptopElasticsearch81924/demo-ready.json`, then open
[the local query page](http://127.0.0.1:5139/).

Each explicit session has a new nonsecret run identity and evidence files, using
the same native store and Mongo environment. The fixed temporary database
`cosmic_cql_mongo_elasticsearch_demo` must be absent before its three journalled
Mongo saves: two selected copies and a third record proving catalogue exclusion.
No Elasticsearch seed/save is replayed. All four original native documents must match
their bootstrap payload hashes and remain version1; the missing control stays absent.

## Present and stop

Choose **Query all records**, then **Query two-record subset**. Inspect the actual
count, physical row count, provider and ID in the returned JSON. Copies stay
separate rows. MovingProviderReads is explicit; no distributed snapshot is claimed.
The expression/policy/planning code is in
[IdentityFederationWalkthrough.cs](../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs).
Selection IDs do not grant access. The host accepts fixed presets and rejects
caller actors or unknown presets. Provider failure returns an explicit error
without partial rows.

Use **Stop demo (keep original Elasticsearch source)** and wait for the runner to
exit. It verifies the exact temporary Mongo fixture, drops that database and
checks absence. The original Elasticsearch payloads/versions must be unchanged.
Both owned native processes stop through Ctrl+C delivered to their exact hidden
consoles; unrelated console members are refused. Elasticsearch's logs must show
stopped/closed. Closing the tab alone leaves the runner active.

If a run fails, preserve its original intent, logs and pending journal. Reconcile
it; do not delete evidence, replay saves or choose another ID to bypass unresolved
state. Readiness handles documented timed health waits under a bounded deadline;
source/authentication/foreign responses remain errors. Hosted enrollment, wider
search/vector operations, CRUD/storage conformance and full ORM/HyperDrive remain
separate acceptance gates.
