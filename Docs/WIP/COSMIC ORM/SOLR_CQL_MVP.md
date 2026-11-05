# Solr bounded CQL scope

Current local UI address (5 November correction): `http://127.0.0.1:5136/`.
Original cohort retained; new-address browser/provenance and consumer1/1 pass.
[Address correction and exact evidence](LOCAL_DEMO_ADDRESS_CORRECTION.md) owns
publication status; earlier UI addresses below are historical evidence.

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

## Integrated local acceptance — 5 November 2026

Manager/Mongo/HTTP paired1/1 and interactive browser1/1 now pass, zero skips.
Both use the admitted existing Mongo8.2 environment and the acknowledged original
Solr source. Manager/LINQ and actual Kestrel HTTP return5/4 physical rows with
Solr3/2 and Mongo2 in both presets; out-of-catalogue Mongo record is excluded.
Caller actor/unknown preset return400; deactivated Solr returns503 without rows.
The page accurately says native Solr, without claiming account authentication.

Browser tab5 verified the rendered full5/subset4 JSON and MovingProviderReads,
then used the stop button. Interactive run0e08b8fd4142424da146fe06e2aceab7 passed;
original four Solr records remain unchanged. The exact3record Mongo fixture was
verified/dropped, database absence checked and pending journal removed. Ready
marker absent; all owned processes stopped and18883/17883/27029/5117 closed.
No native seed/save replay. Compact paired-browser-evidence.json retains counts
and TRX hashes. No standalone screenshots were generated.

This adds one scoped local profile:98=3canonical+57public/source+38otherlocal,
including31recovered historical demos. Hosted HTTP60 and hosted browser28/open32
are unchanged. Full ORM, CRUD/storage conformance and hosted Solr remain open.
[Local launcher guide](../../Devs/SOLR_CQL_LOCAL_DEMO.md). WEB4 card is next.

- `paired.trx`: SHA256 `70058347ba4fc1dce30e27136ed150fe5b7a24706f2be11836762752aa717666`.

- `interactive-0e08b8fd4142424da146fe06e2aceab7.trx`: SHA256 `27e93a7635dfc32eeb1c436463793177b0d858efd08ff8ec35d01af103abfee4`.

## Published WEB4 local card

Web4Site72e29ba9e0db5e96df4de1b1293ce61327da6756 publishes card41,
Solr + MongoDB / Windows local. It explicitly states that Solr is not enrolled
on the hosted API and uses the retained scalar loopback source. The launch
command points to Run-CosmicSolrMongoDemo.ps1 -Serve; localhost5117 is shared
with other teaching demos, so start one at a time. The new launcher audit is
1/1present; no old accepted launcher/environment was rerun.

All112 existing client tests pass, zero skips. GitHub's Vercel status is success.
The public sandbox HTML returns200 and matches the exact published Git blob:
208,508bytes/SHA2565427f03213d97eded12944a949a2e2fc3885904a7a80174080f902707dff82f6.
The guide and C#links are pinned to parente3e4749ae86c653715561d7acc0d61c722c0d996;
both raw sources return200 and match committed Git bytes. Guide SHA256
c44d33d536cdab931d0121646d289f9552e5004b04d070281d9aea94a54da1f9;
walkthrough SHA2569f5b33ed7c403cea32d7a1db84602fa4b01e5fc77d6d21f28bb8190a8c8455bf.
An initial comparison incorrectly used checkout bytes with different line endings;
verification was corrected to the pinned Git blobs. No source mismatch remained.

A signed-out live browser tab verified the CQL card, honest local label and
expanded exact launcher command. No hosted avatar/session, credentials, source
cohort, API deployment or settings were created/changed by this publication.
Hosted Solr and full ORM/HyperDrive/provider gates remain open.
