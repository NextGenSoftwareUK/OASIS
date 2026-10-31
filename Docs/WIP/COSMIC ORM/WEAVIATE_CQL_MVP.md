# Weaviate CQL demo — explicit native gRPC client

Updated 30 October 2026 (America/Cancun). Scoped local TLS/gRPC acceptance;
hosted cloud, semantic search and full ORM/provider parity remain unverified.

## Launch and demonstrate

From `C:/Source/OASIS-cosmic`:

```powershell
Scripts/TestHosts/Run-CosmicWeaviateLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5132/. Choose **Query all records** (5 physical rows), then
**Query two-record subset** (4). Both retain Weaviate/Mongo provider labels. Finish
with the local **Stop demo** button and wait for native post-demo verification,
fixture cleanup and runner exit. Start one native demo at a time.

The shared [commented C# walkthrough](../../../Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs)
selects explicit provider scope and applies
`Where(row => selectedIds.Contains(row.Id))` through HolonManager. Each source owns
its authoritative catalogue; requested IDs do not grant access. Weaviate requires
an explicit configured official gRPC client; the old REST-only constructor does
not supply one. See the [native construction example](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.IntegrationTests/WeaviateCqlNativeTests.cs).

## Current verified scope

- Actual Weaviate1.39.8/official Weaviate.Client1.2.0, authenticated HTTPS18080/
  gRPCTLS15051/private CA. GraphQL disabled in this retained fixture; CQL uses
  native FetchObjectsByIDs and never falls back to GraphQL or existing load helpers.
- Original node/source inode/schema/3object IDs/JSON/native timestamps unchanged
  across actual restart and post-pair check. Retains the original acknowledged
  update shown as **Espresso updated**; current verification issued no business saves.
- Maximum100 authorized IDs,15s deadline/cancellation, required typed properties/
  UUID/payload/access/duplicate checks, decoded4MiB budget, stable order before paging
  and no partial rows on failure. SDK materializes the response before validation;
  decoded limit is not proof of a complete aggregate wire-allocation bound.
- Save stages caller ID/keys/payload until single-object native batch upsert
  acknowledges matching class/ID/SUCCESS/no errors. Removes the old GET/create-update
  race; no write replay/retry. Caller saved state/count and batch policies controlled.
  This is not observed-token CAS or durable unknown-write recovery.
- Schema activation creates only after404 and validates class/vectorizer/required
  property types and acknowledgements. Vectorizer remains none. State follows lifecycle.
- Native3/filter2/missing0/page1/budget; SDK/HTTP5/4, forged actor/unknown preset400,
  inactive source503 without rows; browser independently returned5/4. Unrelated Mongo
  record excluded. Normal native client explicitly disables SDK retries.
- Exact `cosmic_cql_mongo_weaviate_demo` identities/collections verified/dropped;
  native exited0/noOOM/noError, Mongo stopped, pending/ready absent,
 18080/15051/27039/5132 listeners absent. No setup replay/TLS bypass/global trust change.

## Current evidence

All43 cases passed, zero failures/skips. Paths below are relative to
`C:/Source/OASIS-cosmic/TestResults/WeaviateCqlCurrent/`; do not commit raw output.

| Evidence | Passed | SHA256 |
|---|---:|---|
| weaviate-current-controls.trx |38|AC88A6E7F950DCEA4F27A31BE3348B3B627EA6C0C770F8653D375C7B0852CEAC|
| native-first.trx |1|69493D111914CDBFE9AD8D25B2FC1A51DE8F218E1D642CED937A666362F12632|
| native-restart.trx |1|E49EA815435F80033F8900BDA7BBD49112F80632300E3C102905E05847335EBF|
| mongo-init.trx |1|B2631DF9A62EFBD7305826A518959F7D54045939F35868508C62B8C64076ABB9|
| mongo-federation.trx |1|1903AF07DDC73CC877C6CA8E09CC86AD2D9BC008B4876451F8FBC5079E15566F|
| native-postpair.trx |1|D8155F16A810A7E61BD8D42DE6DD52710A1B8E86056B971D7D12B1E8EDF5559A|

Browser evidence: `weaviate-mongo-subset-current.png`. Public CQL section38 main
e94049f supplies local launch instructions, not hosted Weaviate connectivity.
Backend remains task branch; no Railway deployment.

## Retention and remaining work

Preserve `/var/tmp/oasis-weaviate-cql` objects/schema/APIkey/CA/TLS/receipts together.
All23 top-level metadata/source/TLS files unchanged; original source snapshot SHA256
09C10F298A4218DDD67D418781A1F0771507F6B04B974B8AB26C04D434C7A293.
Retained source380,339logicalB/446,464allocatedB. Prior completed5TRXs hash-archived.
Removed105,781,571B completed private test-bin copies; runner rebuilds integration
bin, unit-test runs rebuild unit bin using retained obj/cache. Source/evidence untouched.

Canonical JSON is plaintext. Full generic load/search migration, history/children/
replication/migrations/atomic conditional writes/unknown-write recovery, name/text/
metadata predicates, central client registration, hosted configuration, SDK wire bounds,
semantic search and full HyperDrive/failover/load-balancing/distributed snapshots
remain separate work. [Historical proof](Archive/WEAVIATE_CQL_MVP_PRESERVED_2026-10-30.md),
[resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md), [later work](AFTER_DEMO_BACKLOG.md).
