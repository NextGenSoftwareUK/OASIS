# Solr and MongoDB local CQL demo

This Windows teaching demo reads three retained native Solr documents and two
temporary MongoDB copies through HolonManager. The full query returns five
physical rows; the two-ID subset returns four. Every row identifies its provider.
It uses the real Solr REST API and MongoDB driver. The local page receives no
hosted WEB4 bearer or refresh cookie.

## Retained environment

This recovery laptop uses one Apache Solr10.1.0 slim installation and one
Temurin21.0.13+9 JRE under `LocalState/COSMIC/RecoveryLaptopSolr1010`.
Solr is bound to127.0.0.1:18883 with no account authentication; this teaching
source is for loopback access only. Jetty's local stop port is17883. The owned
JVM heap is capped at256MiB. No system Java, PATH or service install is needed.
The runner checks all1,942 runtime files against the pinned archive-derived
manifest and preserves the original source metadata, seed intent and ACK.
The scalar Holon teaching fields have explicit single-valued stored schema
entries. Arbitrary nested Holon storage and complete Solr CRUD are not certified.

Mongo reuses the admitted recovery-laptop8.2 replica set/runtime/data through
`Get-CosmicMongoDemoSource.ps1 -RecoveryMongo`. Missing source admission or runtime
stops the run. The launcher never implicitly provisions, repairs or seeds Solr.
Before use on another machine, qualify an explicitly owned source and runtime;
these retained paths and receipts are laptop-specific. The archive digests,
source bootstrap and native evidence are in [the scoped evidence record](../WIP/COSMIC%20ORM/SOLR_CQL_MVP.md).

## Build and start

From the task checkout root, build only when outputs are missing or source changed:

```powershell
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-solr-demo-build -Operation {
    dotnet build Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SolrOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SolrOASIS.IntegrationTests.csproj /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE) { throw 'Solr demo build failed.' }
}
```

Start the retained interactive demo:

```powershell
pwsh -File Scripts/TestHosts/Run-CosmicSolrMongoDemo.ps1 -Serve
```

The launcher holds the provider build lease for the entire run and refuses any
occupied required port or pending Mongo fixture. It verifies manager/HTTP checks
before serving the page. Wait for
`LocalState/COSMIC/RecoveryLaptopSolr1010/demo-ready.json`, then open
[the local demo](http://127.0.0.1:5117/).

Each explicit interactive session has a new nonsecret run identity and evidence
files, using the same native source and Mongo environment. The fixed temporary
database `cosmic_cql_mongo_solr_demo` must be absent before three owned records
are journalled/saved. Two are copies of selected Solr IDs; the third proves
catalogue exclusion. No Solr writes occur during the demo.

## Present and stop

Choose **Query all records**, then **Query two-record subset**. The returned JSON
shows actual physical rows, provider and ID. Copies of an ID stay separate rows.
The complete query/policy/planning code is in
[IdentityFederationWalkthrough.cs](../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs).
Selection IDs do not grant read access. The HTTP host accepts fixed presets and
rejects caller-supplied actors or unknown presets; provider failure returns an
explicit error rather than partial results.

Use **Stop demo (keep original Solr source)** and wait for the runner to finish.
It verifies the exact temporary Mongo contents, drops that database and confirms
absence. Original Solr documents must match their acknowledged teaching data
and remain unchanged across the run. The owned Solr JVM stops gracefully through
Jetty, and the launcher stops its owned Mongo process. Closing the tab alone does
not stop the processes.

If a run fails, preserve its intent, logs and pending journal. Reconcile the
original run; do not delete evidence, replay saves or choose another ID to bypass
unfinished state. The two initial pre-seed readiness failures remain preserved.
Corrected readiness waits on the actual core schema API rather than the mere
presence of a core descriptor.

This is a bounded local CQL/manager demo. Hosted Solr enrollment, arbitrary ORM
mapping, owned writes, full provider conformance and HyperDrive replication are
separate acceptance gates.
