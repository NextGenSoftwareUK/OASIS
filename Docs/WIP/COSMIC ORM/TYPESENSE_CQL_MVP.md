# Typesense CQL MVP — controls verified; native demo pending

Updated30October2026. Inventory remains red until native/restart/paired/browser proof.

## Root causes fixed

Common upsert previously ignored every HTTP response. It now requires200/201 and
a bounded parsed document with the submitted identity, with native dirty_values=reject
rather than silent field coercion/drop. One native upsert only; an unknown outcome
must be reconciled, never replayed. Caller Holon ID/copied provider keys/payload
are staged until acknowledgement; only then IsSaved/SavedCount/identity publish.
Batch honors stop/continue and acknowledged/error counts. HolonManager retains
change-tracking acceptance. Avatar upsert shares the corrected native boundary,
but avatar caller-state ownership needs a separate audit.

Activation formerly treated any failed collection GET as absence and ignored
creation errors. Only confirmed404 permits creation; existing/new schema must
match collection name and wildcard auto storage contract. Typesense id is the
built-in string key; new schema does not pretend it is a searchable field.
Creation requires201, schema envelope bounded64KiB, active flag only after all3
collections; failure clears stale active state. Injected dedicated HttpClient
remains caller-owned. Upsert response bounded4MiB, nativeoperation deadline15s.

## Implemented CQL scope

Shared bounded identity algebra, trusted complete catalogue <=100GUIDs, identity
Boolean/equality/membership and stable paging. Native wildcard query has explicit
exact id membership filter, per_page=selectedcount+1/page1/exhaustive_search=true,
analyticsdisabled and bounded cutoff15s. No full-index scan, per-key read loop,
text ranking or hidden client-side content predicate. Emptycatalogue avoids I/O.
Native response search_cutoff/page/found/hits must certify the complete admitted
set. Wrong/duplicate IDs, authority mismatch, partial/cutoff/HTTP/malformed payloads
reject without publishing partial rows. Stream bounded4MiB, linked cancellation/
15sdeadline; native unordered rows reordered before paging. Metadata/order/children
and historyflag reject instead of claiming unimplemented semantics.

## Proving controls

38passed/0failed/0skipped (includes5existingprovider tests): shared compiler/admission,
stagedidentity,200/201/HTTP/foreign/oversized/disconnected acknowledgements, schema/
create/HTTP failure, active-state publication, cutoff/truncation/page/foreign/
duplicate/budget/native errors, emptycatalogue/noI/O, history and batchstop/counts.
Cache-only producer/unit restores and bounded builds pass; no new dependencies.
These inject nativeHTTP; actual deployed Typesense behavior remains unverified.

[Compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.UnitTests/TypesenseCqlTests.cs),
[native boundary tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.UnitTests/TypesenseNativeTests.cs).
Raw evidence `TestResults/TypesenseCqlCurrent/controls.trx` SHA256
`5b01bd881966e1a502cc3e18313864a9c252460893f4391a2b843df344c5ebc2`.

## Exact next step / gaps

Inspect caches/images before choosing official supported free native Typesense.
Reuse if present; otherwise inventory one authenticated retained source before
allocation. Verify collection/native source identities, original3records and one
replacement/removedfield, native3/filter2/missing0/page1/budget and same-source
restart preservation. Then Mongo-paired C#/HTTP/browser5/4 and hub enrollment.
No reset/reseed/replay/replacementbaseline on unexpected outcomes.

Generic load/search still map HTTP errors to missing/empty and impose250rowcap;
delete acknowledgement, avatar ownership, broad child/history/atomicconditional
writes/replication/migration/fullEFparity remain gaps. Auto-schema data type inference
and complex scalar precision need native proof; no general data precision claim.
No hosted, similarity or distributed snapshot certification.

[Official collection contract](https://typesense.org/docs/30.0/api/collections.html),
[search scope/cutoff](https://typesense.org/docs/29.0/api/search.html).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
