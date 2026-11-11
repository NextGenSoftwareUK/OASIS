# MapLibre public style source qualification

8 November 2026. Source readiness only; no CQL acceptance or live admission.

The existing MapLibreOASIS map facade still contains literal success returns
without a rendering client. Those methods are not qualified by this review.
Full rendering and map-provider implementation remain later roadmap work.

For the breadth-first CQL demo, MapLibre publishes real style documents with
no account requirement in its [official demo repository](https://github.com/maplibre/demotiles).
The README at commit
[601ae60796ceceda2cbd2ed3d2ea92d17a84be4b](https://github.com/maplibre/demotiles/tree/601ae60796ceceda2cbd2ed3d2ea92d17a84be4b)
explicitly lists all three candidate URLs:

| Native style key | Official document | Readiness |
|---|---|---|
| world | https://demotiles.maplibre.org/style.json | HTTP200; 77224 bytes; name MapLibre; 2 sources, 8 layers |
| bright | https://demotiles.maplibre.org/styles/osm-bright-gl-style/style.json | HTTP200; 119336 bytes; name OSM Bright; 1 source, 117 layers |
| terrain | https://demotiles.maplibre.org/styles/osm-bright-gl-terrain/style.json | HTTP200; 75856 bytes; name OSM Bright Terrain; 3 sources, 118 layers |

Each response is application/json and style version8, matching the
[MapLibre style specification](https://maplibre.org/maplibre-style-spec/root/).
Recursive duplicate JSON properties are rejected; each source collection is
nonempty and layer IDs are unique. This is not complete renderer or style
specification validation. Source URLs, glyphs, tiles and other referenced
resources were not followed or downloaded. No browser or GPU was started.

SHA256, in table order:
dd86fd257f9895da23b7da5dac62829a0b25ab458f71b06a3e06857b076caf06;
4733a24f6ae359b7a104cf1a5cde845eb5f16dd590c65059d8de89a40242cb36;
277139cde0c525ddaa274fe3751bb46eafedb2986d544fa7116ff7aa3ac81742.

All raw bodies/status/hashes were retained before parsing, with one bounded
GET per document, TLS validation, no redirects/proxies/cookies/retries/writes.
The three JSON bodies total272416 bytes. Primary README provenance and
compact validation receipts are in the existing recovery state under
maplibre-primary-catalogue-reviewed.json and maplibre-*-style-readiness.*.
No package, runtime, source checkout, image or database was installed.

Next: implement an explicit three-key read-only style-document CQL source,
preserving native document identity and provenance without requesting child
resources. Qualify malformed JSON/schema, unknown/mixed IDs, payload bounds,
privacy, native3/2, manager/HTTP/Mongo and later hosted reuse/replicas/browser.
No map renderer, tile-serving performance, style editing or full SDK parity
claim follows from a public document query. Coverage counts are unchanged.

## Bounded read implementation prepared

MapLibreStyleQuerySource uses the existing bounded ID compiler and public
source mapper, with explicit world/bright/terrain keys. It issues one GET for
each selected reviewed URL, rejects redirects/partial responses/non-JSON,
bounds decoded bodies to262144bytes and JSON depth48, and rejects duplicate
properties recursively. Version8, bounded nonempty names/sources/layers,
supported source/layer kinds, distinct layer IDs and local source references
are required. It preserves the entire original UTF-8 style JSON, SHA256,
native URL/key/version and counts as portable scalar metadata. Referenced
resources remain opaque data and are never requested or executed.

Unknown/mixed ID selections preflight before transport; source writes and
scans return explicit unsupported errors. Creator/public/active/childless
projection and owner-dependent logical identity use existing shared contracts.
The original map facade is unchanged and its unimplemented rendering remains
open. No shared helper, Core enum or admission tariff changed for this source.

New provider test project contains27controlled cases and1opt-in real native
CQL3/2case. These cover HTTP/schema/JSON/UTF-8/payload boundaries, no child
resource requests, no partial rows, inactive/cancelled reads and catalogue
validation. New cloud workflow enforces exact counters/no skips, one build
lease and bounded .NET workers; no local compilation/runtime/environment.
Project XML and expected case counts checked; cloud qualification pending.
Coverage counts unchanged; no WEB4 registration or hosted source prepared.

Tasksourcea1ebbf5f75a10ce426103d7bce77fa9dc06b9641 pushed on original task branch; cloud42454653284 observedin_progress. One automatic run, no manual dispatch or local compiler.

Cloud42454653284/job128650985638 succeeds:27controlled+1actual native CQL3/2,
zero skips. Artifact13607083066/5626bytes SHA256
bff12b69e30c9710e237577a52ea5d1813cc5cbb7cd8084da3824cbfab25ec8a and both
TRX hashes/counters verified wholly in memory. Real complete style documents
round-trip with their SHA256 and source/layer counts; no child requests.
WEB4 explicit owner registration/read compiler/project binding,8boundary
cases and real shared HTTP/Mongo fixture prepared. Core260d7c7dcc5512cccceb049480bd323d274b7be7
adds only the read-tariff entry on the original Core task branch; parent
manifest/gitlink advance together. No hosted source/admission/count change.

First WEB4 cloud42455134733/job128652414955 compiles and passes8boundary
cases, but actual paired-3returns413/CQL_FEDERATION_OUTPUT_LIMIT under the
existing1MiBwire budget. Native3already returned successfully; no hosted claim.
Root: full pretty-printed JSON metadata is repeatedly escaped in federation
projections. Retained source bodies compact losslessly from77224/119336/75856
bytes to approximately18763/45419/45833bytes, keeping every JSON value.

The single provider representation now serializes the complete parsed JSON
without formatting whitespace. Original body SHA256/byte count remain explicit;
MapLibreStyleJsonSha256 binds the actual compact full JSON separately. No field,
layer, source or geometry is discarded; original byte layout is not promised
for the projected JSON string. Existing1MiBAPI limit and federation behavior
are unchanged. Controlled positive fixtures are pretty-printed to exercise
this invariant; full JSON/hash/count round trips remain required. Source-path
changes now trigger its WEB4 workflow directly. Corrected native and HTTP
qualification remain required; no accepted source/cohort replay.

Corrected source70f8f7c3c20d0f0b628719d0e37b4302d172cb81 pushed; native42455893389 and WEB442455893404 freshly observedlive. Original first HTTP413/failed artifact preserved; no same accepted cohort replay or platform budget increase.

Corrected native42455893389/job128654707996 succeeds:27controls+1real CQL3/2, zero skips. Artifact13608085483/5607bytes SHA256b64f056e980a281e3ba1589de04228a9d1a23b953715737de4c76c3aab2c8ade and both TRX hashes/counters verified wholly in memory. Full compact JSON, explicit original-body SHA/byte count and compact JSON SHA/count round trips pass; WEB442455893404 still live. No local build/ZIP/TRX archive retained.

Fixed-catalogue planner binds only CQL_MAPLIBRE_STYLE_QUERY_SOURCE_OWNER with namespace maplibre/demo-styles and world/bright/terrain keys.39candidate controls+5publisher guards pass using cached Python -B/self-cleaning fixtures; their simulated preparation output is not live configuration. Fixture actora5808c3e-7031-47e7-92de-48a659568f1b logical IDs b0f5d38d-cc44-8e60-93b2-e201be79f2b9,025230b9-0731-8dca-8443-af589d04f4cd,2f4cf1cc-ed1b-85e2-88fc-75930d4b582a. Same assertions added to the existing C#catalogue case for the next cloud check. No real source actor/snapshot/enrollment generated. Corrected WEB442455893404 has passed8boundaries and is starting its isolated MongoDB replica.

Corrected WEB442455893404/job128654708111 succeeds:8boundary+1real HTTP/Mongo,
zero skips. Artifact13607493979/4611bytes SHA256
d47f7f1c1d4a247d1eee8c8b3789d196b759092a29e8d56ab86a27a1b8164efd,
TRX/summary/cleanup counters and hashes verified wholly in memory. Full3/2,
paired5/4,foreign private0/public3,2SDKsaves/9receipts and databaseAbsent:true
pass with the same1MiBAPI limit. Complete losslessly compact style documents
remain present; no field/geometry/source/layer removal or larger budget.
Coverage124scoped local/95liveHTTP/28browser. No hosted, replication or browser
acceptance for MapLibre; legacy map rendering/full SDK scope stays open.

Scoped candidate23532a6fc9146d5535152c182e61ec21d8889d6e/Core681a1d0b2d4aa8f3b2751a1fad8857a1dd5f86e0 prepared from Civic88c378c1e/Core81862b2 with15scoped paths, including the new test project. Source reader blob matches native/HTTP-qualified70f8f7c3c; API edits constructed from the scoped base. Manifest/gitlinks validate; temporary indices removed in finally. No checkout/worktree/runtime/database, ref publication or deployment. Candidate publication awaits accepted Civic rollout.

Cross-language fixture source181e3e9928416a518f7412107135ee074e9a0502 cloud42456430837 succeeds:27controlled+1native/no skips, including the hard-coded Python/.NET logical-ID assertions. Artifact13607831994/5589bytes SHA2567344c079a4a2367f41fc11d7d5ebf07d30c74b689be4cf74d80ecef145167334 and both TRX hashes/counters verified wholly in memory. Candidate23532a6f includes this fixture; no extra local environment/build or hosted actor.

8 November after sealed Civic and public card81: existing scoped candidate23532a6fc9146d5535152c182e61ec21d8889d6e/Core681a1d0b2d4aa8f3b2751a1fad8857a1dd5f86e0 published normally to existing candidate refs after fresh ancestry/ref/manifest checks. Exact original auto-triggered native42458784027,WEB442458783911 and six-reader admission42458784079 observed in_progress; no duplicate dispatch. No Development/settings change. Two-local-day-batch cadence means no third8November Development batch.

One offline MapLibre source plan prepared from accepted Civic259-setting baseline:owner77afeb39-df6d-4df4-8686-5ff43bd01cbb; logical IDs3d29fed1-2d6f-85df-b5f6-cf3bcaccab99,5d42b004-6864-8572-a3f6-bbe391fb34a1,91954212-291a-8d08-9f23-b3009fc64eaf. Prefixmaplibre-style-public,261candidate settings/fourchangedkeys, protected snapshots341110/343478bytes, source-plan.json records hashes. First operator import stopped before any source creation; corrected module search path, then original plan prepared once. No native requests/source writes/live enrollment, fresh live baseline comparison still required.

8 November exact candidate native42458784027 SUCCESS:27controlled+1native, no skips. Artifact13608398052/5621bytes SHA256e2a3bdf188bb547cc6a97a310e29735c52da6dd269909851f684b1c3abb2237c, both TRX counters/hashes inspected in memory; compact maplibre-exact-registry-qualified.json retained. WEB442458783911/job128663363860 and admission42458784079/job128663364341 freshly observed in_progress on their original handles. Image dispatch operator prepared only; requires successful exact WEB4/admission receipts before execution. Conditional full-style card82 websitec1cbfd7 pushed only to original task branch, public main stays accepted Civicfa0f848.

8 November exact candidate admission42458784079/job128663364341 SUCCESS:25cases executed/passed, no skips. Artifact13609305931/4400bytes SHA2564552ff4faa5d436c6634fbeaf99d911e7821c297e2da8843ea24537f63f1cbf2 and TRX counters/hash verified wholly in memory. Native+admission53cases now inspected; exact WEB442458783911/job128663363860 remains observed live in controlled WEB4 integration. Seven-dependency/full259setting/accepted Civic deployment reviewer prepared/syntax checked only, gated by remaining WEB4 proof. Image dispatch not invoked; no Development/settings mutation.

8 November exact WEB442458783911/job128663363860 SUCCESS:8boundaries+1real HTTP/Mongo, no skips. Artifact13609087006/4602bytes SHA256c680a7429f56305584313b265a7e4b7792b2e4cf2a432d830040be945aa4c984; TRX/summary/owned fixture databaseAbsent verified in memory. Native3/2,paired5/4,foreign private0/public3,2SDKsaves/9receipts pass; browser/replication acceptance remain false. Exact candidate62total cases inspected.

One image dispatch acknowledged204, original cloud run42459320270 observed in_progress on exact23532a6f; no Railway deployment. Read-only prereview verifies seven advertised dependency pins, full259settings unchanged, current accepted Civic51ca9b77 SUCCESS/source88c378/actual73c657c1 image and Dapr/Self/SubQuery/Holonym/Civic preserved. Protected deployment observation and compact receipt retained. No MapLibre enrollment; image gate and next permitted Development integration window remain open. Daily cadence and fresh baseline checks still apply at actual integration.

8 November original exact image42459320270/job128664993499 freshly observed in_progress Build and push. Read-only package inspector prepared/Python+cached Node24 syntax checked only:14SDK/provider assemblies,6helper hashes, full protected baseline/settings comparison, native/runtime proof and retained Dapr/SubQuery/Self/Holonym/Civic enrollments mandatory. Explicit MapLibre owner absent during code phase and exact original77afeb39 owner during settings phase. Not invoked; no Development/settings/native mutation.

8 November exact image42459320270/job128664993499 SUCCESS. GHCR digestsha256:ca3595325ea217c1b9bfdc86ba9bf03c87f9711ac1068c311ad8276366b8040b; exact23532a6fc9146d5535152c182e61ec21d8889d6e-teaching tag, vcs:revision and teaching_sidecars:true/build argument verified from182607character job log inspected in memory. Compact maplibre-image-qualified.json retained; large memory log discarded, no local image downloaded. Candidate62tests, image and seven-pin/full259setting prereview gates pass. No Development push/settings mutation; next agreed integration window and fresh baseline revalidation remain required before code deployment/package audit, four-setting activation and first live cohort acceptance.

Guarded Development publisher and four-setting activation operators prepared/Python syntax checked only. Publisher rejects any operation before9November America/Cancun next window; it requires exact62case/image/prereview proofs, current shared refs, seven advertised pins, unchanged259settings and accepted Civic deployment. Activation requires inspected exact code package, original77afeb39owner,259to261settings equality and four per-key ACKs. Neither operator invoked, no integration/settings/native mutation.

Website staging update: taska0883cf now contains pending82MapLibre,83Sandbox,84PolygonID,85SEEDS and86OpenVINO. For accepted MapLibre-only publication, publish82while omitting83-86 in an ordinary scoped task/main commit, then restore remaining pending cards only on the task branch; never sweep all task HTML onto public main. Public mainfa0f848b remains accepted Civic81. New pending cards do not alter client JavaScript/discovery;99selected controls pass.

The recovered Git-triggered WEB4 deployment 0d024452 at 485448ac3
passed read-only package inspection. The MapLibre reader is packaged
and disabled. The active settings match the protected baseline, and
the original MapLibre candidate still differs by exactly four reviewed
keys. The retained Civic, Holonym, Self Protocol and SubQuery
enrolments and mounted Dapr receipt passed the same inspection.
The settings batch remains pending the Development rollout interval.

The guarded settings apply preflight passed on recovered WEB4. The
one-hour interval from the Git-triggered deployment permits the next
batch from 05:06:26 UTC on 9 November. Apply uses the original
protected snapshots, four individual acknowledgements and no deploy
per write. Activation is pinned to the already qualified source
485448ac3 and requires all four readbacks. No settings write or
activation request was sent during preflight.

The queued four-provider candidate was prepared locally on the recovered
Development base 485448ac3 as ac83d5ee9. Comparison against the former
candidate found only the three recovery paths. The HTTPS package source,
Docker health probe and Waku retention are preserved. The Railway
manifest validator passed for all seven Dockerfiles and parent gitlinks.
The candidate remains unpublished pending MapLibre acceptance and
requires exact qualification before a Development rollout.

After the 05:06 UTC rollout gate, the four original MapLibre settings
were applied individually with deployment disabled and verified against
the complete protected candidate. One activation request at exact
485448ac3 was acknowledged. Railway deployment bbfb91ce is building.
The prior recovered deployment 0d024452 was still SUCCESS at that
observation. Inspect the settings package and run the original
MapLibre HTTP acceptance once after the new instance is ready.

## Live Development HTTP acceptance

Deployment bbfb91ce at 485448ac3 passed exact settings inspection.
The original owner and independent visitor verified native CQL 3/2
with complete style documents and matching hashes, Mongo federation
5/4, two private saves, zero repeat writes with two reused snapshots,
and exact SQLite and Neo4j replicas at 2/2. The independent visitor
saw all three public records and no private snapshots across storage.
The first acceptance invocation stopped before cohort creation because
the deployment readiness visitor did not yet exist. The dedicated
readiness pass established it before the accepted run.

The local acceptance seal binds all three report hashes and the
exact instance. Browser acceptance remains open. The four-provider
candidate ac83d5ee and Core e242de41 are published to the existing
qualification refs. Four scoped WEB4 checks and the exact image build
were dispatched. No new Development batch was deployed.

The four-provider candidate's package control run 42566647198 exposed
verifier drift and a missing shared Docker-resource helper. Candidate
0b3b72976 aligns the workflow, verifier and control files with the
passing task-branch harness. The correction touches qualification files
only. Eight local controls and the dependency manifest check passed.
Corrected image run 42566836454 is queued behind 42566683160. Once the
corrected image publishes, the prepared dispatcher checks the exact
source and manifest digest before requesting full package and isolated
startup qualification. No Development rollout was requested.

All four scoped WEB4 integration workflows passed on ac83d5ee:
Sandbox 42566680876, Polygon ID 42566681028, SEEDS 42566680841,
and OpenVINO 42566683083. The subsequent 0b3b72976 correction
changes only qualification files. Its image build is still queued;
exact package/startup acceptance and Development rollout are pending.

Exact four-provider image 42566836454 and full package/startup run
42570518249 passed at 0b3b72976. Development fast-forwarded to that
commit and Railway queued c172bc9d. The original eleven-setting plan
reconstructed exactly from the four retained source descriptors and
matches the active MapLibre baseline. An exact protected copy of that
baseline is available for guarded apply. The new code/settings package
inspector is prepared. No enrollment values have been written.

Railway code deployment c172bc9d failed before startup with a GHCR
scope rejection for the pinned NATS runtime image. The accepted
MapLibre instance still returned public HTTP 200. The candidate
8b81437e restores the working deployment's checksum-verified public
Waku and NATS release stages. Five download controls passed and
image run 42572801789 is building. Exact package/startup qualification
has been prepared for that image. The old native-consumer workflow
42572801482 failed because its verifier still expects private GHCR
stage aliases. The public release stage workflow 42572801420 is the
relevant artifact producer check. No live settings changed.

Railway deployment 24303b7a failed without creating a container.
The public-asset correction removed the previous GHCR scope error.
The last retained logs show .NET restore progress and build
cancellation without a compiler error or a Railway diagnosis. This
does not establish a resource-exhaustion cause. MapLibre continued
to return HTTP 200. The task branch now bounds restore to the same
two workers as publish and uses its Release assets directly rather
than restoring again. Exact image qualification is still required
before a new Development attempt.

The bounded Release restore image passed exact package/startup run
42581966154. Development advanced to ab0e9b2c, and Railway code
deployment 7eda6b32 passed inspection on running instance f375a31b.
All four assemblies are present, the MapLibre settings and mounted Dapr
receipt match, and OpenVINO is still disabled. The original eleven
settings reconstructed and matched the protected candidate. Guarded
apply and exact-commit activation are prepared. The next rollout
interval opens at 09:16:15 UTC on 9 November. No new settings were
written during the preflight.

## Four-provider live HTTP acceptance

Railway settings deployment 9d5e9388 at ab0e9b2c passed package
inspection on instance fa26240e, with all 11 settings acknowledged and
the complete protected settings verified. Sandbox, Polygon ID, SEEDS
and Intel OpenVINO each passed native CQL 3/2, MongoDB federation 5/4,
two private saves, zero repeat saves with two reused snapshots,
SQLite/Neo4j replica readback 2/2, and zero independent private rows.
The four original acceptance seals bind the report hashes and instance.
Sandbox names include the block of each observation. The first
evidence seal assumed the native and prepared names used the same block
and stopped before writing a receipt. The corrected seal checked the
same contract label and valid block format from the retained reports,
without replaying queries or writes. Browser acceptance remains open.

11 November UTC: MapLibre public style-document presets rendered 3 full and 2 subset rows in the live Development WEB4 CQL Playground. Private MongoDB paired browser verification and rendering SDK coverage remain open.
