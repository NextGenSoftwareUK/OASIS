# Azure Blob CQL demo MVP

Updated 30 October 2026. Current task-branch integration verified; full parity remains incomplete.

## What works

- Actual Azure SDK reads against the retained free Azurite3.37.0 local store.
- CQL identity equality/membership, AND/OR, constants and paging over at most100 selected canonical GUID keys, with a trusted per-provider authorization catalogue. No container scan or managed predicate filtering.
- Three retained Blob holons; one query across Blob and Mongo returns5 physical rows, or4 for the two-ID subset. Mongo's extra record is excluded; provider provenance is preserved.
- SDK, HTTP and both browser buttons verified on the current branch. Forged actor/unknown preset rejected; inactive Blob fails the entire request with503, without partial rows.
- Bounded4MiB streaming and strict UTF-8/identity/access checks; only BlobNotFound means absence. Caller cancellation remains explicit.
- Native read-only proof checks exact Blob ETags/content before/after. Reopened the original source without reseeding. Exact temporary Mongo database verified/dropped;10000/27029/5106 stopped and ownership journals cleared.

## Run the demo

From C:/Source/OASIS-cosmic (PowerShell7.5+):

```powershell
Scripts/TestHosts/Run-CosmicAzureBlobCqlNative.ps1 -Federated -Serve
```

Open http://127.0.0.1:5106/, select **Query all records**, then **Query two-record subset**. Stop with the local stop button and wait for the runner to exit. Start only one native paired demo at a time. Without -Serve, the same runner verifies SDK/HTTP and exits.

The runner builds the Azure Blob producer before its consumer; it does not reuse a stale provider DLL. Existing successful reports are archived by hash before replacement; unresolved failures stop the run. Runtime and source are reused, with graceful Azurite shutdown to flush storage.

## Current evidence

All paths below are under C:/Source/OASIS-cosmic/TestResults/AzureBlobCqlCurrent/; raw evidence stays outside Docs.

| Evidence | Result | SHA256 |
|---|---|---|
| azureblob-cql-current-controls.trx |14passed,0skipped |793D58EA3E3A1D52F51FD80DC975027BE3F015868EFA915EF3D19B9CD55B73B4 |
| azureblob-cql-native.trx |1passed,0skipped; retained3/subset2/missing0, unchanged ETags/content |835D8B7D8F36ED6E2A938BD8147DC8BB6E74893BF8433645A87985B881D9965D |
| azureblob-mongo-federation.trx |1passed,0skipped; SDK/HTTP/browser5/4 and exact cleanup |F69A20AD7754A9EB272D9C777869FAC3E567502106FD083778B11D35A0DFB3EF |

Browser proof: azureblob-mongo-subset-current.png. Previous noninteractive integration passes archived under Archive/ by hash. First integration invocation overwrote historical native/paired filenames before the runner's evidence-preservation fix; historical report hashes are not presented as current proof.

Current source C:/Source/OASIS-cosmic/LocalState/COSMIC/AzureBlobCql/{demo-seed.json,azurite-data,environment.lock} remains retained. Seed SHA2569758C6A96B95BC00B2B3A4E8C8A800E61AED320ED943CBE0B8114DAB14C6E23A matches the original. Never reset/reseed or infer missing source from a stopped service.

## Explicit remaining gaps

- Hosted Azure Cosmos/Blob remains unproved and pending: user requires free-only and has no Azure subscription. Emulator success does not imply hosted acceptance.
- Blob MVP uses canonical JSON, without application-level encryption. Do not advertise encrypted Blob storage.
- Metadata/name/description/order/aggregate queries, conditional business writes, cross-provider snapshots and full EF/NHibernate parity are not supplied by this bounded identity profile.
- Complete HyperDrive replication/failover/load-balancing acceptance remains separate work.

Source ported selectively from preserved parent f89cf0085; no wholesale broad-branch merge. Public sandbox catalogue section15 links this local demo; hosted selector remains Mongo/SQLite/Neo only.

[Current status](CURRENT_STATUS.md) · [After-demo backlog](AFTER_DEMO_BACKLOG.md) · [Resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md)

Publication verified: Web4Site mainbb2839909fa412e0054006b5597cfa7de311091e, Production7452409547success. Public CQL section15 exposes the expanded exact runner command and fixed5106link; screenshot TestResults/AzureBlobCqlCurrent/azureblob-catalogue-public.png. Backend adapter remains on the task branch, not a new Railway deployment.
