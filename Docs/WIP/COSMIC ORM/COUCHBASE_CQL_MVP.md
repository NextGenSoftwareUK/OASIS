# Couchbase CQL MVP — controls verified; native demo pending

Updated 30 October 2026. Inventory remains red until real native/restart/paired proof.

## Implemented

CouchbaseOASIS explicitly implements ICosmicQueryStorageProvider bound to its own
shared bounded identity algebra: trusted complete catalogue at most100GUIDs,
identity equality/membership/Boolean composition and paging. Parameterized native
SQL++ USE KEYS over configuredbucket._default.holons, read-only/15stimeout/native
cancellation. No unrestricted index scan or fallback to older load APIs. Bucket
identifier validation protects existing interpolated SQL++ statements too.

Native DTO carries metadata key/stored ID/deleted marker/dataJson from the existing
wrapper format. Stream rows through one materialization boundary, check nativekey/
stored ID/payload ID/readauthority, duplicate/foreign IDs and4MiB decoded budget
before retaining another row. Native terminal query status must succeed before
publishing any rows; stream failure returns no partial result. Stable catalogue
order applied before paging. Missing native records are empty; deleted rows omitted.
Wire-byte bounding inside the SDK remains unverified; no distributed snapshot claim.

SaveHolon stages ID/copiedproviderkeys/payload and publishes caller identity plus
IsSaved/SavedCount only after nonzero nativeCAS acknowledgement. Exact unsigned64
CAS preserved as invariant decimal string metadata Couchbase.Cas; no floating-point
or signed conversion. This acknowledgement is not a conditional write/history policy.
Failed saves preserve caller identity/keys, batchstop/continue/counts fixed; manager
owns tracking acceptance. Shared existing collection wrapper remains unchanged.

Activation now clears stale state, creates missing named collections in the existing
bucket before primary-index DDL, consumes/checks DDL completion and activates only
on success. Deactivation clears state. Collection/index provisioning is implemented
and compiles against pinnedSDK; native acceptance remains pending. Bucket itself
must be explicitly provisioned by the environment. No mobile-sync certification.

## Proving controls

31passed/0failed/0skipped: metadata4, compiler/admission/identifier13, saveownership6,
streamingquery8. Provider/unit builds and final cache-only restores pass.
Tests include corrupt/foreign/duplicate/payload keys, stablepaging/resultbudget,
empty/missing records, decodedbudget stopping enumeration, activecancellation,
terminal nativefailure/no partial rows, incomplete/failed/zeroCAS acknowledgement,
ulong.MaxValue precision and batchstop/continue. NativeIO is injected in controls.
[Query tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS.UnitTests/CouchbaseCqlTests.cs),
[save tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS.UnitTests/CouchbaseSaveOwnershipTests.cs),
[stream tests](../../../Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS.UnitTests/CouchbaseNativeQueryBoundaryTests.cs).
Raw TestResults/CouchbaseCqlCurrent/controls.trx SHA256 `09f7a88b4029abb23d6404c96abc868ecf32b242b6e550e6a4dcdebb6126f0dd`.
Stale metadata tests use EnumValue.Value; nonexistent GetProviderVersion assertion
removed rather than adding a fake API. No real backend used by these controls.

## Exact next step

Inspect cached runtime/images and choose one official free Community Couchbase
runtime (no enterprise trial/hostedsignup). Create one retained authenticated local
source/bucket with original node/cluster/bucket identity acknowledged before records.
Verify collection/index creation on native server, originalactor/3IDs/acknowledgedCAS,
native3/filter2/missing0/page1/budget and original JSON/CAS across actual restart.
Then Mongo-paired C#/HTTP/browser5/4 and hub routing. No schema/identity replacement,
seed replay or replacement baseline. Native runtime/source not created yet.

Arbitrary predicates/fullSQL++ translation/graphchildren/conditionalCAS/history/
replication/migrations/unknownwrite recovery/fullEFparity remain open. Other avatar/
legacy unrestricted lookup serializer/error paths require separate native audit;
new CQL does not silently reuse them. Legacy HyperDrive issues documentation only.
[Collection API documentation](https://docs.couchbase.com/sdk-api/couchbase-net-client/api/Couchbase.Management.Collections.ICouchbaseCollectionManager.html).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
