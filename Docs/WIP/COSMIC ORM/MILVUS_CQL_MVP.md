# Milvus CQL MVP — bounded query controls pass; native demo pending

Updated30October2026. Inventory remains red. Bounded CQL controls pass; native/paired demo remains unverified.

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

Activation now uses read-only describe admission for all3explicitly provisioned
collections. It never creates/replaces after errors; staleactiveflag clears first.
Required: exactname/positiveInt64collectionID, autoId=false/dynamic=false,
Strong defaultconsistency, LoadStateLoaded, sixnonpartitionedfields with exact
bounds: idVarChar64primary,docVarChar65535,username/emailVarChar512,parent_holon_id
VarChar64,vectorFloatVector2; vector_idx/L2 index. Twozero vector components match
that admitted schema. Previous dimension1 sources need explicit migration, never
silent replacement. Schema256KiB/15s bound and nativecode checked. Native index
implementation details beyond returned describe metadata still require acceptance.

28totaltests nowpass (19save/provider +9schema). Includes HTTP/nativecode/bounds/
dimension/consistency/load/autoID/index rejection, read-only operation/no creation,
active-state publication and exactcollectionID above2^53. Fresh evidence
TestResults/MilvusCqlCurrent/schema-save-controls.trx SHA256
`b3393932a790007cecf4fff93135c22c184ba82c87b210b263f93e0a7c86aec7`.
[Schema tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MilvusOASIS.UnitTests/MilvusSchemaTests.cs).
[Native describe contract](https://milvus.io/api-reference/restful/v2.6.x/v2/Collection%20(v2)/Describe.md).

Bounded CQL now binds the shared identitycompiler: trusted completecatalogue
<=100GUIDs, identityBoolean/equality/membership, stablepaging. One native
entities/query sends exact id-in-JSONlist, outputFields id/doc, selectedcount+1,
offset0 and consistencyLevel=Strong; Request-Timeout15 and linked15sdeadline.
No vector similarity, fullscan, per-key read loop or hidden content predicates.
Emptycatalogue avoids I/O. Nativecode/array/count/duplicate/foreign/native-vs-payload
identity/read authority checked beforepublication; canonical doc bytebound65535
and envelope4MiB enforced. Deletedpayloads excluded; stable key order beforepaging.
Failures/cancellation/resultbudgets publish no partialrows; unsupportedmetadata/
order/children/history reject through capability admission.

50totaltests pass (28prior +9compiler/admission +13queryboundary). NativeHTTP is
injected; nativeSDK/runtime/consistency/durability are not certified by controls.
[Query tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MilvusOASIS.UnitTests/MilvusNativeQueryTests.cs),
[compiler tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MilvusOASIS.UnitTests/MilvusCqlTests.cs).
Fresh raw evidence TestResults/MilvusCqlCurrent/cql-controls.trx SHA256
`1944eb45642960424617b1321ff6216dad1b644dcc13eab44a0b540152c0ab4f`.
The2.6RESTQuery documentation omits consistencyLevel, but pinned2.6.4 server code
explicitly maps it into the native QueryRequest. This is a source-verified request
contract, with nativebehavior acceptance still pending; no olderGet assumption.
[Query handler source](https://github.com/milvus-io/milvus/blob/v2.6.4/internal/distributed/proxy/httpserver/handler_v2.go#L765).

Next: Inspect caches before nativeallocation. Reuse suitable runtime/service or create
   one inventoried free authenticated retained source if missing. Verify original
   records/nativecollectionIDs/receipts/payload across restart, then Mongo-paired
   C#/HTTP/browser5/4 and hub. Never replay/reseed/reset/rebaseline unknownoutcomes.

Existing vector is a zero-valued schema placeholder, not semantic embedding search.
Collection vector dimension/index parameters need native validation. Generic reads
still map nativeerrors to missing/empty and query1000rowcap; fieldexpressionescaping,
projectionlimit checks, deletes/softdelete errors, child/history/conditionalwrites/
replication/migration/fullEFparity remain gaps. No hosted or durability certification.
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

## Native environment checkpoint — 30October2026

Official2.6.26 image downloaded once and pinned to
`sha256:e6066d0afc4eeab69b7c9269e49be2b9ae54c740a21c05f230c4e5743c646ee3`.
No prior Milvus runtime/image found. Retainedsource `/var/tmp/oasis-milvus-cql`,
container oasis-milvus-cql, loopback19531/nativeTLS, embeddedetcd/localstorage/
RocksMQ,2CPU/4GiBcap. No externaletcd/MinIO instance or hosted subscription created.
Protected rootcredential/CA/config/creationreceipts stay under source, never Docs.

Initial UID1000 launch failed126 before native execution; source/data verifiedempty.
Originalfailedcontainer receipt preserved, exitedcontainer removed and recreated
once as vendorroot with same source/key/config/image. Milvus then started embedded
etcd but panicked contextcanceled in Session.Init/mixCoord initialization and exited134.
No OOM, no businessseed, no collectioncreate request. Failed source retained;
no reset/reseed/replay attempted. Embeddedstartup log identifies originalcluster
cdf818194e3a8c32/member8e9e05c52164694d; preserve identity during diagnosis.
Currentnative source144,815,277B logical (mostly preallocated etdcWAL), image
969,614,562B expanded, containerwritablelayer12,270B. Service confirmed exited.

Exactnext: inspect pinned native startup/session lifecycle and config before any
restart, retain original failure and pending intent. Diagnose on same source;
verify TLS/auth/emptycatalogue/originalcluster identity and actualrestart before
provisioning schema or seeding records. The environment helper currently fails
acceptance; no native/paired demo proof. Raw logs remain
TestResults/MilvusCqlCurrent/environment.log and environment-reconciled.log.

### Retained TLS startup correction — 30 October 2026
Pinned upstream `internal/distributed/proxy/listener_manager.go:95-99` rejects
TLS when HTTP shares the gRPC listener. Configure proxy.port19532 and
proxy.http.port19530; retain host19531, same container, source, CA and credentials.
Authenticated native collections/list now returns code0/empty; unauthenticated
access rejects. Embedded identity matches the original cluster/member exactly.
No collections or demo records created. Initial authenticated identity receipt is
retained as /var/tmp/oasis-milvus-cql/initial.ack.json.
Shutdown then exposes a separate native panic: dependency/kv.CloseEtcdClient
(kv_client_handler.go:38), after coordinators/nodes stop, exits134/noOOM.
The strict stop check correctly rejects it: restart acceptance remains incomplete,
environment.pending remains, and no full-demo/inventory green is claimed.
Next: inspect pinned embedded-client close ownership and prove safe restart on
the same source. Preserve the failure; do not accept134 as clean shutdown.
