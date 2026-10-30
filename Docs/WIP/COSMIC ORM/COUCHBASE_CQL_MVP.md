# Couchbase CQL MVP — controls/native/restart verified; paired demo pending

Updated 30 October 2026. Inventory remains red until Mongo-paired demo proof.

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
and passes native collection/index provisioning. Bucket itself
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

Mongo-paired C#/HTTP/browser5/4 and hub routing, reusing the original source below.
No schema/identity replacement, seed replay or replacement baseline.

## Native proof and retained environment

Official free Community8.0.2-5503 (no Enterprise/trial), original cluster
8c927880b18838e9224c996be8d552da/node ns_1@127.0.0.1/bucket
ba639725706cd9cbd5635411816a03a2 retained. Native services kv/n1ql/index;
authenticated plaintext host-loopback8091/8093/11210, no hosted/TLS claim.
Original source /var/tmp/oasis-couchbase-cql20,021,382B logical, container
oasis-couchbase-cql stopped0/noOOM/noError. Privatecredentials, actor/3IDs,
write acknowledgements/exactunsignedCAS, JSONhash baseline and native data retained.
31controls plus1native/1actual-restart pass. Native3/filter2/missing0/page1/budget
and collection/index provisioning verified; JSON and CAS unchanged across restart.

Initial environment checker ran before nodehealthy after acknowledged cluster/bucket
setup. Corrected bounded readiness; explicit originalreceipt readback, no setup replay.
Initial SDK readiness included unexposed legacy Views and timed out; diagnostic
ping proved KV/query healthy, Views error. Scope now waits specifically KV/query,
and runner waits original bucket healthy/active vbucket map before SDK bootstrap.
No service/container/source replacement. Old integration file's stale Objects import
corrected to actual Holons namespace; unrelated historical tests were not executed.

Reproduce: `Scripts/TestHosts/Run-CosmicCouchbaseLinuxDemo.ps1` (one build lease,
cached restores, bounded build workers, retained source required). Setup only once:
WSLroot `python3 -B Scripts/TestHosts/Create-CosmicCouchbaseEnvironment.py`;
existing state is refused. Raw evidence stays in TestResults/CouchbaseCqlCurrent:
native-first.trx SHA256 `5461aff924235827aa56f3b97833cdb276b597583cc9fb21265b664156c7f56a`;
native-restart.trx SHA256 `dbff9418c785c9131df4508a75552ae5ae4ca78278b07808c1cb09f1b1b31e21`.

Arbitrary predicates/fullSQL++ translation/graphchildren/conditionalCAS/history/
replication/migrations/unknownwrite recovery/fullEFparity remain open. Other avatar/
legacy unrestricted lookup serializer/error paths require separate native audit;
new CQL does not silently reuse them. Legacy HyperDrive issues documentation only.
[Collection API documentation](https://docs.couchbase.com/sdk-api/couchbase-net-client/api/Couchbase.Management.Collections.ICouchbaseCollectionManager.html).
[Current status](CURRENT_STATUS.md) / [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).
