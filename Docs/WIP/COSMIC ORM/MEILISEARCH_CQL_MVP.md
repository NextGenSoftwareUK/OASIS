# Meilisearch CQL MVP — controls pass; native demo pending

Updated 30 October 2026. Inventory stays red until native/restart/paired/browser proof.

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

## Exact next step / gaps

Inspect existing runtimes/images before selecting an official free supported native
release. Create one retained authenticated environment only if missing, inventory
before allocation. Verify native index/source/task receipts, original3records plus
one full replacement/removal, native3/filter2/missing0/page1/budget, actualrestart
preservation, then Mongo-paired C#/HTTP/browser5/4 and hub enrollment.

Generic load helpers still turn HTTP errors into missing/empty data and cap1000;
legacy search field/filter settings, delete acknowledgements and avatar ownership
remain gaps. Children/history/atomic conditional writes/replication/migrations and
full EF parity are not certified. No hosted or distributed snapshot claim.

[Official task contract](https://www.meilisearch.com/docs/reference/api/async-task-management/list-tasks),
[native key retrieval](https://www.meilisearch.com/docs/reference/api/documents/list-documents-with-post).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
