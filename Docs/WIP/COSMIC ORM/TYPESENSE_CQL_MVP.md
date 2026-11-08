# Typesense bounded CQL demo scope

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
