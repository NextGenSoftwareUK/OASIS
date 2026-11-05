# Solr bounded CQL scope

5 November 2026. Controlled provider/CQL tests: 16 passed, zero failures or
skips. This is an adapter milestone, not native or hosted acceptance. Coverage
counts remain 97 local profiles, 60 hosted HTTP proofs and 28 hosted browser flows.

The CQL path uses the shared bounded identity compiler and exact reads through
`GET /solr/oasis_holons/get?id={canonical-guid}&wt=json&omitHeader=false`.
Read-only activation checks the existing collection using
`GET /solr/oasis_holons/schema/uniquekey?wt=json`; successful Solr status and
`uniqueKey=id` are required. Neither operation creates collections. The normal
provider activation/provisioning path is not used by CQL activation.

Each successful response must contain a successful responseHeader and doc.
Only HTTP success with explicit `doc:null` means an absent record; HTTP 404,
authentication, collection errors, malformed responses, wrong identities and
payload failures return explicit OASISResult errors without partial rows.
Canonical Holon identity/read authority, deletion filtering, ordering and paging
are enforced. The transport caps each response at 1 MiB, the query aggregate
at 4 MiB, and the overall query at 15 seconds. Child graphs are excluded.
Default HTTP redirects are disabled; an injected client must obey the same
transport policy. Historical/version-control reads remain explicitly unsupported.

The adapter consumes stored camel-case Holon JSON fields, matching the existing
provider serialization contract. Arbitrary native Solr schema compatibility and nested Holon data have NOT been
certified; the later native checkpoint proves the explicit scalar teaching schema. The existing
CRUD collection provisioning/save implementation is not certified by these tests.
Do not advertise complete provider conformance or import support from this work.

## Native acceptance: first seed and retained restart pass

`SolrCqlNativeTests.cs` requires explicit COSMIC_SOLR_CQL_SOURCE metadata:
Endpoint (HTTP loopback origin), Actor, LiveIds (three), DeletedId and MissingId.
COSMIC_SOLR_CQL_USERNAME and COSMIC_SOLR_CQL_PASSWORD are optional together,
provided only through the environment. No credentials belong in source metadata.
It verifies the existing deleted/missing controls, 3/2/0 reads, paging and hashes
of the four existing native document payloads before/after. It never provisions,
seeds, repairs or deletes a source. A real source and retained fixture evidence
are still needed before incrementing coverage or adding a public demo card.

At the initial adapter checkpoint no new runtime/download was installed. Java
was absent from PATH; the retained
QuestDB 10.1.0 runtime is Java 25.0.2 but is a custom module image (14 modules,
including io.questdb), not an admitted general Solr runtime. No assumption that
its version alone proves Solr compatibility. The later native checkpoint supersedes that runtime gate.
Manager/Mongo/HTTP/browser acceptance remains open.

Protocol references: [RealTime Get](https://solr.apache.org/guide/solr/9_1/configuration-guide/realtime-get.html),
[Schema API](https://solr.apache.org/guide/solr/latest/indexing-guide/schema-api.html),
[Java requirements](https://solr.apache.org/guide/solr/latest/deployment-guide/system-requirements.html).


## Native checkpoint — 5 November 2026

Apache Solr 10.1.0 slim on pinned Temurin21.0.13+9 now passes the native fixture:
one test per run, zero failures/skips, on first accepted seed and retained restart.
Three live private records and one deleted control were seeded exactly once.
The missing control stayed absent. Queries returned3/2/0 plus the expected page;
all four native JSON document hashes remained unchanged across each read-only run.
The scalar teaching fields have explicit single-valued stored schema definitions;
this proves that bounded schema only, not arbitrary nested Holon persistence.

The first two startup attempts are retained as failures, with no seed dispatch,
ACK or tests: first a readiness socket timeout during core load, then a503 caused
by treating core-list presence as readiness. Both owned processes exited0 through
Jetty's graceful stop. The runner now waits on the actual collection schema API
with a bounded60second deadline; only connection/timeouts and503 are readiness
states. Wrong unique keys and other HTTP failures remain errors. Four controlled
runner tests pass, including readiness/deadline/exit and no seed replay checks.
The explicit unseeded recovery checked all original intended record keys absent
before the one journalled POST with commit=true. No save retries occurred.

Source, original intents, dispatch/ACK and failure/success logs are retained under
`LocalState/COSMIC/RecoveryLaptopSolr1010`; compact native-evidence.json records
both TRX hashes and counters. The initial successful run was
unseeded-resume-c75bb54aa04d47329bbdb590fbb65186; restart was
retained-0d7a9085cb334bd79012c5c7fe71092b. Exact TRX hashes below:

- `retained-0d7a9085cb334bd79012c5c7fe71092b.trx`: SHA256 `94b183d594fe7bc87057bc5298e215d968913c0316ce1736848c40d89b69a703`.

- `unseeded-resume-c75bb54aa04d47329bbdb590fbb65186.trx`: SHA256 `afcb81655de402f459560deca34dd73480349b822fc23791acb11322d2bc20e0`.

The loopback listener18883 and stop port17883 are closed. Processes are owned
by this runner and stop through Jetty, with per-run exit receipts. The default
runner never seeds. --initialize refuses any existing source/home;
--resume-unseeded refuses any dispatched or acknowledged seed. Builds and runner
are serialized under Invoke-WithProviderBuildLease.ps1. All1,942 installed runtime
files match the pinned archive-derived manifest before execution. The verified
archives were then removed to free132,169,999bytes; expanded reusable runtimes,
checksums, manifest and receipts remain. No system/PATH/service changes.

Manager/Mongo/HTTP, local browser and hosted enrollment remain open. Counts are
unchanged until integrated demo acceptance. Next: reuse this source and the
existing Mongo environment to prove5/4 native/replica provenance and HTTP flow.
