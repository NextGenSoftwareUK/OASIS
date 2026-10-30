# Solr CQL MVP — controls verified; native demo pending

Updated30October2026. Inventory remains red until native/restart/paired/browser proof.

## Root causes and storage contract

Old saves posted arbitrary nested holon fields to Solr schema inference and ignored
HTTP acknowledgements; activation blindly attempted SolrCloud collection creation.
New storage boundary preserves complete canonical JSON in one stored, unindexed,
single-value string field data_json. Only id/name/username/email/parentHolonId/
holonType/isDeleted are native typed projections; nested metadata, arrays and
precise scalar JSON remain inside the payload. All generic point/search readers
use this same canonical payload and validate native/payload identity; no raw-document
fallback. HTTP/status/partial/malformed errors now return real errors rather than
missing/empty success in those common read helpers. Legacy raw documents need an
explicit migration; no migration, deletion or automatic schema conversion performed.

Upsert is one full-document update with commit=true/waitSearcher=true/versions=true.
HTTPsuccess, responseHeader.status0/no error/partial and exact adds[id,positiveInt64]
receipt required before publishing caller identity/provider keys/IsSaved/SavedCount.
The native version is an acknowledgement, not implemented conditional-write/CAS
support. Unknown outcomes never trigger replay. HolonManager owns tracking acceptance.
Batch stop/continue/error/acknowledged counts honored; historyflag rejects.
Dedicated injected HttpClient remains caller-owned. Envelopes stream-bounded:
256KiB schema,64KiB update acknowledgement,4MiB reads; native deadlines15s.

Activation requires explicitly provisioned cores/collections and verifies all3
schemas. It never guesses standalone vsSolrCloud admin operations or creates on
an HTTPerror. Missing/wrong schema rejects and clears stale active state. Required
uniqueKey=id; exactfield/type/flags verified with showDefaults=true:

| Fields | Type | Indexed | Stored | DocValues | MultiValued |
|---|---|---|---|---|---|
| id,name,username,email,parentHolonId | string / solr.StrField | true | true | false | false |
| data_json | string / solr.StrField | false | true | false | false |
| holonType | pint / solr.IntPointField | true | true | false | false |
| isDeleted | boolean / solr.BoolField | true | true | false | false |
| _version_ | plong / solr.LongPointField | false | false | true | false |

## CQL scope

Shared bounded identity compiler, trusted complete catalogue <=100GUIDs, identity
Boolean/equality/membership and stable paging. One native RealTimeGet ids request
retrieves latest heads without searcher-refresh delay. Explicit fl=id,data_json,
_version_,omitHeader=false,shards.tolerant=false; emptycatalogue avoids I/O. Validate
complete response/start/numFound, exact/native/payload IDs, positiveInt64 versions,
duplicates/read authority. partialResults (including omitted), earlytermination,
HTTP/malformed/oversized/cancelled reads reject without publishing partialrows.
Native unordered rows reordered before localpaging. Metadata/order/children/history
reject; no full-index scan, per-key read loop or hidden content predicate.

## Proving controls

48passed/0failed/0skipped, including5existingprovider tests. Controls cover schema/
read-only admission, precise nestedpayload serialization, stagedsave publication,
failed/wrong/oversized/disconnected/status/partial acknowledgements, exactInt64
versions above2^53, canonical genericload/error paths, partial/omitted/truncated/
foreign/duplicate/payload/version/page/budget/nativequery errors, batch andhistory.
Cached producer/unit restores and bounded builds pass; nativeHTTP is injected.
[Compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SolrOASIS.UnitTests/SolrCqlTests.cs),
[native boundary tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SolrOASIS.UnitTests/SolrNativeTests.cs).
Raw regenerable evidence TestResults/SolrCqlCurrent/controls.trx SHA256
`82709e7fcb099c3d1f31b68a94d2d31d7ba3c88e32304dcbec8202b4fffc3636`.

## Exact next step / remaining gaps

Inspect existing caches/images/runtimes. Reuse a suitable native service, or inventory
one official free authenticated retained source before allocation. Explicitly
provision3cores/collections with schema above. Verify native source identities,
original3records/versions, one replacement/removal, native3/filter2/missing0/page1/
budget and actualrestart preservation. Then Mongo-paired C#/HTTP/browser5/4 andhub.
No reset/reseed/unknown-outcome replay/replacement baseline.

Legacy raw-data migration, generic1000rowlist cap/search escaping, avatar ownership,
delete acknowledgements/softdelete error propagation, child/history/replication/
conditionalwrites/migration/fullEFparity remain open. No native/hosted/SolrCloud
cluster acceptance or distributedsnapshot claim from these injected controls.
[RealTimeGet](https://solr.apache.org/guide/solr/9_5/configuration-guide/realtime-get.html),
[version receipts](https://solr.apache.org/guide/solr/9_9/indexing-guide/partial-document-updates.html).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
