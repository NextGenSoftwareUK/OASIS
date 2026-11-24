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
# Typesense bounded CQL demo scope

## Embedded-child defect resolved and verified — 8 November2026

Source032e0a10ee4e85687f91a05c03cd856b8806d395 fixes the reproduced root-query
failure by projecting children before canonical materialization. The original
failure source6b8847247/run42499253849 remains retained below. No alternate
parser, source, retry, native mutation or swallowed error was introduced.

[Query regression42499481264](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42499481264)
passed9/9 affected cases, zero skipped, including the formerly failing canonical
root-with-child case and existing authorization/error/budget/paging controls.
Artifact13624434881/2305bytes SHA256
`8417067ece3f4f7243e12f719fff6a77cb7540dfb8b6641315173fee8f30d62a`;
TRX SHA256`b03e6cc85ae9684e860d21a4fc2071765ac86073b6acf3ecb57fb13813f90078`.

[Native manager/HTTP regression42499481245](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42499481245)
passed1/1, zero skipped, against an actual Typesense29.0 root containing a child.
The fixture independently confirms the native child remains present before and
after CQL reads; selected query roots have empty children and source hashes
match. Manager/HTTP5/4, provenance, unrelated private control exclusion,
forged/unknown requests and inactive503/no partial rows remain verified. Four
native document creates only, restricted-reader write denied, exact owned
Mongo database plus both container/volume sets removed, exit0/no OOM.
Artifact13624732028/4160bytes SHA256
`88604385cd68dcecbac2308c6fecb97c555b78b74e49dfdb9395a256f884d909`;
TRX SHA256`57c7dcc7139c8a9b8c876f3028e7b18b5ad5daabf62b071bb33cf9d6583d9b6a`.

Both artifacts inspected in memory. Compact original failure and success
receipts are under LocalState/COSMIC/RecoveryLaptopResume20261107/typesense-*.
No native-only/restart replay, local install/build/database/image/model,
Development/live setting change, browser proof or coverage-count increment.
Hosted HTTPS source, authenticated visitor acceptance/reuse/replicas and browser
remain open. This current checkpoint supersedes the pending statements below.


## Current manager/HTTP checkpoint — 8 November 2026

[Run42498399586](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42498399586),
job128785632985, sourceac71b92fb9d8d4a1078862eadf79523a826b500f passed1/1,
zero skipped. Actual shared HolonManager captured ID predicates return5/4
physical rows over real Typesense and Mongo; loopback HTTP returns the same
counts and provenance. A third unrelated private Mongo control is excluded.
Forged actor/unknown preset requests reject; inactive Typesense returns503
without partial rows. Native Typesense document hashes remain unchanged.

The reader is restricted to collection/document GETs; native write denial is
verified before the test. Two selected private Mongo copies plus one unrelated
private sentinel are fixture-only records. The test verified exact ownership,
removed its previously absent database, and wrote its completion ACK. Typesense
exited0/no OOM; exact owned Typesense container/data volume and Mongo container/
anonymous volumes were confirmed absent. Six ownership guards also pass.
Native-only/restart tests were not rerun in this new scope.

Artifact13624825762,4127bytes, SHA256
`2807f96459c39e82d5797fbb9ac8855de655511e5e030c9157cf033507bc24e6`.
TRX SHA256
`54d9a12b35666b63c21c167559c68eb4fe551dc3bfe79fe0dd0c14c3e0a3409e`.
All nine artifact entries inspected in memory; compact receipt retained at
LocalState/COSMIC/RecoveryLaptopResume20261107/typesense-mongo-qualified.json.
No local runtime/image/database, live setting or Development deployment.

Live HTTPS source access/enrollment and actual browser interaction remain open;
normal visitor privacy/reuse and SQLite/Neo4j replicas must be verified there.
This loopback teaching host is not the authenticated live WEB4 API. Full storage
interfaces, content predicates, EF parity and HyperDrive remain unqualified.
The existing Typesense profile gains stronger evidence; no additional unique
profile or hosted/browser count is claimed. Priority live queue stays unchanged.
Dated earlier manager/HTTP-open statements below are historical.


## Current native checkpoint — 8 November 2026

Real pinned Typesense29.0 native reads and same-source restart are accepted.
[Run42497834056](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42497834056),
job128783906994, source2fa81ea8b0bc1969d5e060f57b134bdc2998fcc6:
1/1 initial native and1/1 post-restart, zero skipped. Both execute the existing
C# provider and shared planner against actual Typesense document reads:
full3/subset2/deleted+missing0/paging1, with all four native document hashes
unchanged before/after. The actual reader has only documents:get and
collections:get for oasis_holons; its attempted document create was denied.
Four synthetic records were provisioned once before qualification; neither
native test nor restart reseeded them. The final native process exited0 and
exact owned container/named volume absence was verified.

Artifact13624436951,3661bytes, SHA256
`2929983f0dbf86d9190683a68b56221dc4373f4e1740d3a9d455a1d9710dfbdf`.
Native TRX SHA256
`71450c85e33e6ea6d47a1d8520e80a39253cbd7fe78197a8dc4900c1e6f631d7`;
restart TRX SHA256
`cf462564c680709c0e43b565f224ff2a69fe7fa3bf28a33ee09bb193d5804dc7`.
Artifact inspected entirely in memory. Compact durable receipt:
LocalState/COSMIC/RecoveryLaptopResume20261107/typesense-native-qualified.json.
No local runtime/database/image, live settings or deployment was created.

Next is manager/LINQ and HTTP/Mongo federation; hosted HTTPS admission and
browser acceptance remain open. The broader scoped integration total is not
increased for this native-only gate. Priority-provider live work still takes
precedence. Dated earlier native-open statements below are historical and
superseded by this checkpoint; the laptop WSL device state itself is unchanged.


5 November 2026: provider build and 13 controlled tests pass, zero failed/skipped.
Eight tests cover the new CQL/HTTP boundary; five are existing metadata checks.
No native service, manager/LINQ, federation, hosted or browser acceptance yet.
Coverage totals remain unchanged.

The existing provider stores camel-case canonical Holon JSON as documents in
`oasis_holons`. The new CQL path uses the shared bounded identity compiler,
intersects server-owned read authority before requests and retrieves exact IDs
through the actual [Typesense document API](https://typesense.org/docs/29.0/api/documents.html#retrieve-a-document).
It uses GET /collections/oasis_holons/documents/{canonical-guid}; no search scan
or client-side business predicate. Responses are bounded while streaming to
1 MiB each/4 MiB total, with one aggregate 15-second deadline. Native ID and
payload authority must agree. Duplicate top-level properties reject. Deleted
rows are excluded before paging in canonical GUID order. Historical reads and
unsupported query shapes reject rather than switching to legacy load methods.

Query activation uses GET /collections/oasis_holons and verifies the existing
collection name; it never invokes the ordinary collection-creation path. Failed
activation clears readiness. The default HTTP transport disables redirects;
explicitly injected clients remain owned/configured by their caller. Credentials
use the provider's existing X-TYPESENSE-API-KEY contract. No new API/client package.

Controlled tests verify 3/2 selections, paging after deleted/missing exclusions,
read-only missing-collection activation, authentication/authorization/server-error
propagation and failure without partial rows after wrong IDs, duplicate IDs or
oversized second records. Only document HTTP 404 becomes an absent row; other
HTTP errors remain errors. This is controlled HttpMessageHandler evidence,
not native Typesense proof or a distributed snapshot guarantee.

Next: qualify one real reusable owned Typesense runtime and source, prove exact
native 3/2/absent/deleted selections and unchanged source, then manager/LINQ and
HTTP/Mongo pairing, privacy/replicas. Host enrollment must use restricted source
credentials and explicit tariff/preset admission after native qualification.
Wider content/search, native write correctness, conditional concurrency, children,
history, replication and full ORM parity remain separate acceptance gates.

## Native read-only fixture

TypesenseCqlNativeTests requires COSMIC_TYPESENSE_CQL_SOURCE (metadata JSON:
Endpoint, Actor, LiveIds[3], DeletedId, MissingId) and the source credential only
in COSMIC_TYPESENSE_CQL_KEY. It accepts one explicit HTTP loopback origin and
five distinct nonempty IDs. No source is selected implicitly. It activates the
existing collection, independently retrieves each control, proves the deleted
record exists and missing record is HTTP404, then checks CQL 3/2/0 and paging.
Four native document SHA256 observations must match before/after. All calls are
GET; no provisioning, save, cleanup mutation or sample substitution occurs.
Native observation bodies are bounded to1MiB with15-second whole-read deadlines.

Native execution remains open. No retained Typesense runtime is recorded on this
laptop. WSL reported no installed distributions; Docker is not on PATH. Official
[Windows installation instructions](https://typesense.org/docs/guide/install-typesense.html)
use WSL. No Linux environment, native binary or cloud account was installed.
Choose one reusable actual service before dispatching the explicitly filtered
TypesenseCqlNative category. Existing old integration tests are not its runner.

The leased native fixture build passed with zero errors. This is compilation
only; no native test was dispatched and no coverage count increased.

## Recovery-laptop Linux host gate

Read-only host inspection on 5 November identified the actual WSL2 blocker:
System32/wsl.exe --status reports that the current configuration does not
support WSL2 and requires Virtual Machine Platform plus firmware virtualization.
Win32_Processor reports VirtualizationFirmwareEnabled=false; SLAT/VM monitor
properties also report false in this current configuration. This does not prove
that the physical CPU lacks support. No distributions are registered under
this user's Lxss key, and the standard Docker executable is absent.

Do not download a Linux provider runtime until one reusable host is available.
The recovery-laptop device action is in ACTIONS-FOR-DAVID.md, distinct from the
other laptop's old WSL disk-compaction item. No BIOS, Windows optional feature,
boot setting, reboot or distribution installation was performed. Follow
[Microsoft's WSL2 prerequisites](https://learn.microsoft.com/en-us/windows/wsl/install-manual#step-3---enable-virtual-machine-feature)
when the owner is ready for the device step. Keep native Typesense acceptance
open; controlled Windows tests and compilation are not a Linux service.

## Development WEB4 candidate

The query-only registration uses `CQL_TYPESENSE_QUERY_ORIGIN` and
`CQL_TYPESENSE_QUERY_API_KEY` for an explicit HTTPS teaching origin and
restricted key. The guarded HTTP client admits collection metadata and exact
document GETs on `oasis_holons`. Use a dedicated existing teaching collection
and keep the key in protected settings.

The native HTTPS fixture uses `TestCategory=TypesenseHostedCqlNative` and
`COSMIC_TYPESENSE_HOSTED_CQL_SOURCE` pointing to a reviewed catalogue with
`Actor`, three `LiveIds`, `DeletedId` and `MissingId`. Run it after a reviewed
source is available. The existing loopback fixture and its WSL prerequisites
remain separate. Development WEB4 rollout follows LocalFile and real
Typesense qualification.

## Cloud native qualification in progress — 8 November 2026

The laptop WSL prerequisite does not prevent a separate owned cloud fixture.
Source `c2ad4819dffb7e8bedaa375e60bb4eeb1d04c032` is running in
[qualification42497546582](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42497546582).
This is not a pass or hosted enrollment. It reuses the existing native test;
there is no replay of an accepted native Typesense cohort because none was
previously accepted. The cloud-only runner creates one explicitly disposable
source after compilation, four canonical records (three live/one deleted), a
missing control and an actual restricted documents:get/collections:get reader.
It requires write denial, native3/2/0/paging/source preservation, then an actual
stop/restart and the same unchanged-source read test. Label-checked container
and named-volume cleanup must be observed before acceptance.

Official image `typesense/typesense:29.0` is pinned to registry manifest digest
`sha256:316b7e71c21f7e5e5caa8daa150e1b3f2be8c876081ee1f77bc2d92cd7f137d0`.
This deliberately tests the existing29.0 adapter contract, not an unverified
upgrade to the currently documented30.2. [Official container installation](https://typesense.org/docs/guide/install-typesense.html)
and [29.0 key permissions](https://typesense.org/docs/29.0/api/api-keys.html)
define the runtime and restricted-reader contracts.

No service, runtime, build or database is installed on the recovery laptop.
No priority-provider cohort, live setting or Development branch changes.
TRON and the remaining priority browser gates retain live-rollout precedence.
Manager/LINQ, HTTP/Mongo, hosted and browser Typesense gates remain open.

First cloud run42497546582 failed at startup health before any seed or CQL read:
Docker published its port before the native listener was ready, producing
ConnectionResetError. Compilation succeeded. Original artifact13624431440,
697bytes, SHA256b4b1920cd0f8877a2cd66b5c7b7947a1f0819c07eb4db09cd64a373debec982a
was inspected in memory: native/restart false, final exit0, owned container and
volume cleanup verified. The readiness boundary now permits that reset only
while the exact labeled container is running without OOM, under the existing
60-second startup deadline. Native document/CQL calls gain no retries. No
accepted source was replayed; this failed fixture contained no seeded records.

## Manager/HTTP federation qualification started — 8 November 2026

New scope sourceac71b92fb, run42498399586 confirmed in_progress. One explicitly
separate disposable cloud fixture tests the real shared HolonManager and
IdentityFederationApiHost with Typesense plus a native Mongo replica. It must
prove manager and HTTP5/4 with both provider labels, unrelated private control
exclusion, forged actor/unknown preset rejection and inactive-source503 with
no partial rows. Before/after Typesense document hashes must match. The Mongo
fixture holds two selected private copies plus one unrelated private control;
that is test data, not a live visitor cohort. Exact owned database cleanup and
both runtime/container/volume cleanup are required. Pending, not accepted.

The native-only workflow is now manual so extending this new federation scope
does not automatically rerun the accepted native/restart tests. The original
native proof remains source2fa81ea8b/run42497834056. Existing Mongo lifecycle
helper now admits a distinct typesense owner namespace; six pure cleanup guards
pass locally. No local native process/database/install, production promotion,
Development push, browser proof or live enrollment.

## Reproduced embedded-child materialization defect — 8 November2026

The accepted initial native fixtures used minimal root records. A real canonical
Holon may also persist its children. Source6b8847247/run42499253849 executed the
new captured-wire child case once and failed once (no skips): valid root JSON
with one embedded child returned CQL_QUERY_FAILED/NotSupportedException before
returning any rows. This is an actual reproduced gap, not a reason to replay
unrelated accepted native cohorts. Artifact13624721713/1372bytes SHA256
021bf76eca8fd0a1cde7dae5bc200b5ef3f2d0b9cc8f1ef39c573b91ab557cbb;
TRX SHA256ec5ec52c6d6d6704c70914d4dfa8b8b6f7858842619fe6433216b334ad9e24dd.
Compact original failure retained under RecoveryLaptopResume20261107.

Source032e0a10e projects the declared camel-case children field out before
canonical deserialization, preserving duplicate-property, identity, authority,
deleted-row, budget and error checks. No fallback parser or query retry. Query
regression run42499481264 and real-native manager/HTTP run42499481245 both
passed. See the exact checkpoint above. The latter explicitly seeds one
root with an actual embedded child, verifies it exists before/after the reads,
and requires empty query child results plus unchanged native source hashes.
That stronger regression scope justifies the affected query/native reruns.
The accepted native-only/restart gate remains frozen; no live state changed.

The hosted qualification and enrollment descriptor includes Origin
alongside Actor, LiveIds, DeletedId and MissingId. The compiled fixture
rejected a mismatched CQL_TYPESENSE_QUERY_ORIGIN before registration.
Preflight requires the exact reviewed origin and the protected reader
key. Live source permissions and WEB4 queries remain unverified.
