# Meilisearch CQL MVP — native/restart/paired demo verified

Updated 30 October 2026. Scoped native/restart/Mongo-paired C#/HTTP/browser proof passes.

## Root cause and implemented boundary

Existing saves ignored the native queued-task response and immediately reported
success. Existing activation ignored index-creation errors. New task submission
requires HTTP202, nonnegative exact taskUid (including0), matching index/type and
enqueued acknowledgement. Only observations of that same task follow; no write
retry/resubmission. Matching terminal succeeded plus null error and document
received/indexed counts1 are required before publishing IsSaved/SavedCount/identity.
Native failed/canceled/unknown states, wrong identity/type/index, HTTP/oversized/
disconnected observations fail. The exception retains acknowledged task ID for
reconciliation. A lost acknowledgement does not prove absence: never replay/reset.
Observation deadline15s and polling100ms are bounded native task observation,
not a second submission. Task/provisioning envelopes bounded64KiB before parsing.

POST documents replaces the complete document; previous PUT retained removed
fields. Holon ID/provider keys are staged in serialized payload and published only
after terminal success. HolonManager retains change-tracking acceptance ownership.
Batch honors stop/continue, error counts and acknowledged save counts. Common avatar
upsert now also waits for completion; avatar caller mutation needs separate audit.
Activation GETs/validates exact index and primaryKey=id; only native index_not_found
404 permits creation, whose task and resulting schema are verified. Active flag
publishes after all3indices; failed activation clears stale state.

## CQL scope

Shared bounded identity compiler; trusted complete catalogue <=100GUIDs, identity
Boolean/equality/membership and stable paging. Native POST documents/fetch receives
explicit nonempty IDs and selected-count+1 bound, never an empty full-index query.
Complete response total/page/IDs/duplicates/read-authority validated before any
result publication. Response stream bounded4MiB; caller cancellation and15s deadline.
Native unordered rows are reordered before local page selection. History flag and
unsupported metadata/order/graph predicates reject; no silent full-index scan.
Dedicated injected HttpClient remains caller-owned; API key is caller configured.

## Evidence

39passed/0failed/0skipped: existing provider5 plus34new compiler/admission/native
boundary/task ownership/schema/batch/history controls. Actual Meilisearch transport
is injected in these tests; they do not certify a native deployment.

[Compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.UnitTests/MeilisearchCqlTests.cs),
[native boundary tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.UnitTests/MeilisearchNativeTests.cs).
Cached restore and bounded provider/unit builds pass. Raw evidence is regenerable
`TestResults/MeilisearchCqlCurrent/controls.trx`, SHA256
`a703a5f28a0ebfa137a15f32cb377e280ac3faa3ace0d9d6131cea41eedf3fe3`.

## Native, restart and federated acceptance

Official free1.56.0 image pinned to digest
`sha256:3a2dda1ade2280eecb9b777fbe4e27dc82b458e4e335929884481b3850e7cd03`.
Retained source `/var/tmp/oasis-meilisearch-cql`, native HTTPS17700 with private CA
and API-key authentication, production mode, telemetry disabled. No global trust
change. Container/image/mount/source inode/version verified before use.

Native3/filter2/missing0/page1/budget pass, plus three original creates and one
full replacement removing an obsolete field. Actual same-store signalstop/reopen
preserves document JSON, native index timestamps and all7completed task receipts.
Five fresh phases each1pass/0skip: native-first/restart, Mongo initialization,
federation and native-postpair. Paired C#/HTTP verify source provenance, excluded
row, forged actor/unknown preset, inactive-provider error without partial results.
Browser all5/subset4 independently verified at5147. Stop button finished runner0;
owned Mongo fixture removed and both services stopped. Source770,176B retained.

Initial environment verifier expected exit0, but native1.56.0 deliberately disables
Actix signal handling; SIGTERM produces143. Original empty-source receipt and
failure observation were retained, then exact source restarted without allocation,
credential changes or task replay. This verifies signalstop/reopen, not graceful
shutdown or general crash recovery. [Pinned native signal code](https://github.com/meilisearch/meilisearch/blob/v1.56.0/crates/meilisearch/src/main.rs#L178).

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Meilisearch -Federated -Serve
# Dedicated runner:
Scripts/TestHosts/Run-CosmicMeilisearchLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5147/ once ready; use the stop button for verified teardown.
[Native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.IntegrationTests/MeilisearchCqlNativeTests.cs),
[paired proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.IntegrationTests/MeilisearchMongoCqlNativeTests.cs).
Raw evidence remains `TestResults/MeilisearchCqlCurrent/` (regenerable/uncommitted).
Federation TRX SHA256 `5eca9d2358105da1bb771f6f00a9e65af97b2af8153179f00a60fd632f5523b1`;
postpair `e65b27499c58e65dd5574b554e8bcbeda2bb16dac3fd5d3e3ba28d9c199a03b5`.
Hub56paired/112plans/98paired rejections pass without changing listeners.

## Remaining gaps

Generic load helpers still turn HTTP errors into missing/empty data and cap1000;
legacy search field/filter settings, delete acknowledgements and avatar ownership
remain gaps. Children/history/atomic conditional writes/replication/migrations and
full EF parity are not certified. No hosted or distributed snapshot claim.

[Official task contract](https://www.meilisearch.com/docs/reference/api/async-task-management/list-tasks),
[native key retrieval](https://www.meilisearch.com/docs/reference/api/documents/list-documents-with-post).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
