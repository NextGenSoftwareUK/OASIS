# OpenSearch and MongoDB local CQL demo

This Windows teaching demo reads three retained OpenSearch Holons and two
MongoDB copies through HolonManager. The full query returns five physical rows;
the two-ID subset returns four. Each row shows its source provider. It uses the
real OpenSearch REST API and MongoDB driver. No hosted WEB4 bearer or refresh
cookie is forwarded to the local page or native backend.

## Prepared environment

The recovery laptop has one pinned OpenSearch 3.9.0 Windows bundle under
`LocalState/COSMIC/RecoveryLaptopOpenSearch390`. It reuses the qualified Temurin
21.0.13+9 JRE in RecoveryLaptopSolr1010. Installed files match pinned manifests;
no second Java, system/PATH or service installation is needed. The verified
1.05 GB archive has been removed after native qualification.

The retained teaching node binds HTTP19200 and transport19300 only to loopback.
Its security plugin is disabled for this isolated local test configuration.
Hosted TLS/authentication and least-privilege credentials remain separate gates.
It uses the shipped JVM options parser, a 128 MiB initial/512 MiB maximum heap
and two active CPUs. This local node does not use hosted sandbox credentials.

The laptop crossed OpenSearch's default high disk watermark during qualification.
Existing-index queries and retained restart pass; new-index/storage capacity
remains open. No watermark has been lowered or disabled. The launcher never
recreates or reseeds the existing index. Missing source/runtime/ACK or a changed
original payload/version stops the run. Before use on another machine, qualify
an explicitly owned source; these paths and receipts are laptop-specific.

Mongo reuses the admitted recovery laptop MongoDB 8.2 replica set/runtime/data through
`Get-CosmicMongoDemoSource.ps1 -RecoveryMongo`. Native provenance, failures and
exact acceptance evidence are in [the scope record](../WIP/COSMIC%20ORM/OPENSEARCH_CQL_MVP.md).

## Build and start

From the task checkout root, build only when outputs are missing or source changed:

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-opensearch-demo-build -Operation {
    dotnet build Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.IntegrationTests.csproj /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE) { throw 'OpenSearch demo build failed.' }
}
```

Start the interactive demo:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicOpenSearchMongoDemo.ps1 -Serve
```

The launcher holds the provider build lease, refuses occupied ports or unfinished
fixtures, and starts only its own processes. It runs manager/HTTP checks before
serving the page. Wait for
`LocalState/COSMIC/RecoveryLaptopOpenSearch390/demo-ready.json`, then open
[the local query page](http://127.0.0.1:5138/).

Each explicit session has a new nonsecret run identity and evidence files, using
the same native store and Mongo environment. The fixed temporary database
`cosmic_cql_mongo_opensearch_demo` must be absent before its three journalled
Mongo saves: two selected copies and a third record proving catalogue exclusion.
No OpenSearch seed/save is replayed. All four original native documents must match
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

Use **Stop demo (keep original OpenSearch source)** and wait for the runner to
exit. It verifies the exact temporary Mongo fixture, drops that database and
checks absence. The original OpenSearch payloads/versions must be unchanged.
Both owned native processes stop through Ctrl+C delivered to their exact hidden
consoles; unrelated console members are refused. OpenSearch's logs must show
stopped/closed. Closing the tab alone leaves the runner active.

If a run fails, preserve its original intent, logs and pending journal. Reconcile
it; do not delete evidence, replay saves or choose another ID to bypass unresolved
state. Readiness handles documented timed health waits under a bounded deadline;
source/authentication/foreign responses remain errors. Hosted enrollment, wider
search/vector operations, CRUD/storage conformance and full ORM/HyperDrive remain
separate acceptance gates.
