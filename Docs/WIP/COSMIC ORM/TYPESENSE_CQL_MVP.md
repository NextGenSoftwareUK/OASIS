# Typesense CQL MVP — native/restart/paired demo verified

Updated30October2026. Scoped native/restart/Mongo-paired C#/HTTP/browser proof passes.

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

## Native and federated acceptance

Official free30.2 image pinned to digest
`sha256:b405d4443bdb9254fab4792b7fd9ee95d969f3b5485975917a3a3f84274eba27`.
Retained source `/var/tmp/oasis-typesense-cql`, native TLS18108/private CA/APIkey,
container/image/mount/source inode and stable native metadata-store identity checked.
No global trust change. Native3/filter2/missing0/page1/budget and one full replacement
removing obsolete field pass. Actual restart preserves original3document hashes.

Typesense rebuilds its derived RocksDB index from retained Raft history on startup
without a snapshot; derived db ID changes. Stable meta ID remains
`02ecc411-42a2-40e4-a772-dcae4065af6a`. Native collection created_at is also regenerated
by time(nullptr) during replay: **native created_at immutability remains a gap**.
Original full-schema baseline was retained, never replaced. Exact original schema
preimage was recovered by varying only created_at over the actual seed-intent to
first-write-ack interval (<=15s), matching original SHA256
`76475e18bcb9e36853f5d1f656172994b4464a3be4a9cb0b560e8c1fd6c36533`.
Original1793363442 vs observed1793363570; all other schema properties and document
hashes matched. Protected source-schema.json retains that exact original evidence.
Tests require this original hash/preimage and compare all properties except the
proven volatile created_at. Original identity/hash failures remain documented.
[Native derived-index replay](https://github.com/typesense/typesense/blob/v30.2/src/raft_server.cpp#L105),
[native collection clock](https://github.com/typesense/typesense/blob/v30.2/src/collection_manager.cpp#L1693).

Five fresh phases each1pass/0skip: native-first/restart, Mongo init, federation and
native-postpair. C#/HTTP assert provenance, excludedrecord, forgedactor/unknownpreset,
inactiveprovider error/no partial. Browser all5/subset4 independently verified on5148.
Stopbutton completed runner0, ownedMongo fixture removed and bothservices stopped0.
Native source209,141B logical retained; originalseed/receipts/baseline unchanged.

```powershell
Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Typesense -Federated -Serve
# Dedicated runner:
Scripts/TestHosts/Run-CosmicTypesenseLinuxDemo.ps1 -Federated -Serve
```

Open http://127.0.0.1:5148/ once ready; use stopbutton for verified teardown.
[Native proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.IntegrationTests/TypesenseCqlNativeTests.cs),
[paired proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.IntegrationTests/TypesenseMongoCqlNativeTests.cs).
Raw regenerable evidence `TestResults/TypesenseCqlCurrent/`; federation TRX SHA256
`847c771158a253ff5ae4a3352dc2b7b4bd60bd5d198d17961e45fb47ce57341c`, postpair
`828f0232d1424d459634241344fd48c520c195d4af02a211717fcc3a991cac2b`.
Hub57paired/114plans/100paired rejections pass; listeners unchanged.

## Remaining gaps

Generic load/search still map HTTP errors to missing/empty and impose250rowcap;
delete acknowledgement, avatar ownership, broad child/history/atomicconditional
writes/replication/migration/fullEFparity remain gaps. Auto-schema data type inference
and complex scalar precision need native proof; no general data precision claim.
No hosted, similarity or distributed snapshot certification.

[Official collection contract](https://typesense.org/docs/30.0/api/collections.html),
[search scope/cutoff](https://typesense.org/docs/29.0/api/search.html).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
