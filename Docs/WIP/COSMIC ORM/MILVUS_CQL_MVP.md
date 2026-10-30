# Milvus CQL MVP — save ownership controls pass; query/demo pending

Updated30October2026. Inventory remains red. No CQL/native/paired demo claim yet.

## Root causes fixed in the save boundary

The previous helper checked HTTP success only. Native REST failures can be HTTP200
with a nonzero code; old SaveHolon also assigned caller identity before native I/O.
New common upsert sends exactly one request and requires HTTPsuccess, code0,
upsertCount1 and exactly the submitted string ID in upsertIds. Native/body/ID/count/
oversized/disconnected errors reject without caller save publication. No retry or
write replay; unknown outcomes require reconciliation. Acknowledgement64KiB stream
bound,15slinkedoperation deadline and Request-Timeout15 passed to nativeHTTP.

Holon ID/provider keys and runtime-typed payload are staged before submission;
caller ID/keys/IsSaved/SavedCount publish only after acknowledgement. Existing caller
keys survive failures. HolonManager retains change-tracking acceptance ownership.
Canonical existing Pascal-case doc JSON retained; runtime serialization preserves
derived fields instead of serializing only IHolon. Full metadata scalar JSON is
stored inside doc. The schema's65,535UTF8byte VarChar limit is checked before I/O;
no truncation. Batch honors stop/continue/error and acknowledged save counts.
Historical Holon writes explicitly reject. Shared avatar/delete upserts now check
native acknowledgements, but their caller-state/error propagation remains unaudited.
Injected dedicated HttpClient is caller-owned. Provider name/description now set.

## Proving controls

19passed/0failed/0skipped:14newsavecontrols+5provideridentity/category controls.
Coverage: HTTP200/nativefailure, wrong/empty/numeric IDs, wrongcount, acknowledgement
size, disconnect/no retry, caller ID/key preservation, derivedproperty/numeric
payload preservation, pre-I/O docsize/history rejection, batchstop/counts.

Cached producer/unit restore and boundedbuilds pass; nativeHTTP injected.
Existing unit fixture referenced nonexistent GetProviderVersion and did not compile.
Replaced that invalid assumption with actual ProviderCategory contract; retained
provider type/activation/name/description assertions. No fake version method added.
[Test proof](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MilvusOASIS.UnitTests/MilvusSaveOwnershipTests.cs).
Raw regenerable evidence TestResults/MilvusCqlCurrent/save-controls.trx SHA256
`5e3c8c73b096f9118c5800b642867fd076349dd6d434cc9e379163cf1e4797c8`.
[Official native upsert response](https://milvus.io/api-reference/restful/v2.5.x/v2/Vector%20(v2)/Upsert.md).

## Exact next step / remaining gaps

1. Fix activation: existing create helper ignores both HTTP/nativecode and does
   not validate existingcollection schema/index/loadstate or clear staleactiveflag.
   Validate explicit storage schema and defaultconsistency; no replacement aftererrors.
2. Bind shared bounded identitycompiler and one native multikey operation. Verify
   actual REST version supports selected consistency; older Get APIs may ignore
   consistencyLevel. No unsupported parameter or stale-read acceptance claim.
3. Inspect caches before nativeallocation. Reuse suitable runtime/service or create
   one inventoried free authenticated retained source if missing. Verify original
   records/nativecollectionIDs/receipts/payload across restart, then Mongo-paired
   C#/HTTP/browser5/4 and hub. Never replay/reseed/reset/rebaseline unknownoutcomes.

Existing vector is a zero-valued schema placeholder, not semantic embedding search.
Collection vector dimension/index parameters need native validation. Generic reads
still map nativeerrors to missing/empty and query1000rowcap; fieldexpressionescaping,
projectionlimit checks, deletes/softdelete errors, child/history/conditionalwrites/
replication/migration/fullEFparity remain gaps. No hosted or durability certification.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
