> Superseded by ../COUCHBASE_CQL_MVP.md for current integration; retained historical native/CAS proof and limits.

# Couchbase CQL MVP — controls/native/restart/paired/browser verified

Updated 30 October 2026. Green means the bounded identity demo scope only.

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

Continue red providers, next ClickHouse audit; do not deepen this green demo scope.
No schema/identity replacement, seed replay or replacement baseline.

## Native proof and retained environment

Official free Community8.0.2-5503 (no Enterprise/trial), original cluster
8c927880b18838e9224c996be8d552da/node ns_1@127.0.0.1/bucket
ba639725706cd9cbd5635411816a03a2 retained. Native services kv/n1ql/index;
authenticated plaintext host-loopback8091/8093/11210, no hosted/TLS claim.
Original source /var/tmp/oasis-couchbase-cql28,780,007B logical, container
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

## Mongo-paired demo proof

Launch `Scripts/TestHosts/Run-CosmicCqlDemoHub.ps1 -Provider Couchbase -Federated`
from the task worktree; add `-VerifyOnly` for bounded checks. Browser port5142.
C# example: `Examples/CosmicCqlDemo/IdentityFederationWalkthrough.cs` uses
HolonManager's actual federation executor and explicit per-provider read catalogues.
HTTP/browser all = Couchbase3 + Mongo2 =5physicalrows; subset =2+2=4.
Copies retain provider provenance. Forged actor/unknown preset400 and inactive
provider503/no partial rows pass. This is native bounded key selection, not general
SQL++ predicate translation or a shared distributed snapshot.

Bounded and interactive runners each passed five phases: native-first,
native-restart, mongo-init, mongo-federation and native-postpair. Browser all5/subset4
verified. Hub8single/16plans,51paired/102plans,88pairedrejections,3archive pass,
listeners unchanged. Original Couchbase source/JSON/CAS preserved; exact temporary
cosmic_cql_mongo_couchbase_demo verified/dropped, pending/ready cleared, own
Couchbase/Mongo/demo services stopped; nativeexit0/noOOM/noError.
Raw interactive evidence in TestResults/CouchbaseCqlCurrent (not committed):
- native-first.trx SHA256 `f143dee4b5f069aba9788ec9f69465dea137a3cb7d1c62be5ed58bc78cab22bd`
- native-restart.trx SHA256 `6f623c3fd106677e78dec29428401a444d4bd1600fc923cc624bfc8de6559495`
- mongo-init.trx SHA256 `368f80369da1769cdcbd7ef4d7bcb621156f7f0a42a92007070f6d7f78a8d63c`
- mongo-federation.trx SHA256 `42a6cfb2be97005e8528ed42db31f87376dada9f6aefa701e5e075bf2ed1c36f`
- native-postpair.trx SHA256 `ceffb1e36ecaec4929c47fb415d65ef80076e3da723f74a86a8b52eae3e1b408`
Browser screenshot browser.jpg159,147B, runner80686terminal0; native records are
retained outside TestResults. No full EF/history/children/conditional-write claim.
