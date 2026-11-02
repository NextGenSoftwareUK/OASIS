# Akash public source — hosted HTTP accepted; sandbox card next

2 November 2026. Original hosted acceptance passes native3/2, Mongo5/4,
repeat0new/2existing, foreign0private/3public, exact SQLite/Neo4j2/2 and two
normal API version1saves/pending0. Independent offline audit ACK frozen.
Code105329a3f/Coref42dd8c and activation2a5ec8a6SUCCESS;3settings applied once,
DNA/sourceowner unchanged. Sandbox card is prepared/unpublished; browser and
reliability remain unverified. Integrated60/hosted55/browser5/open50.
Earlier sections are historical; latest evidence/next action are at the end.

Existing adapter: Providers/Network/NextGenSoftware.OASIS.API.Providers.AkashOASIS/AkashOASIS.cs.
It targets api.akash.network Cosmos REST, maps balances to avatars and deployments
to holons. Its node-info activation does not establish complete identity/query
semantics. Legacy implementation remains unchanged; gaps are documentation-only.

## Official interface distinction

[Integration overview](https://akash.network/docs/api-documentation/getting-started/)
separates anonymous indexed network data from authenticated managed deployment
APIs and blockchain-node RPC. A public catalogue demo must not imply it creates
compute deployments or verifies chain state. No key/card/wallet is needed for the
documented network-data scope; native anonymous eligibility still needs testing.

[Provider API](https://akash.network/docs/api-documentation/rest-api/providers-api/)
documents https://console-api.akash.network with GET /v1/providers and
GET /v1/providers/{address}. Records expose registered owner, display name,
service URL, observation flags and resource measurements. Fields can be nullable
or change between observations; indexer health flags are not an uptime guarantee.
The list endpoint documents scope=all/trial, not arbitrary server-side pagination.

## Proposed bounded read-only MVP

Before any native request, record one exclusive endpoint/method/byte/time budget
intent using the production guarded .NET transport. Reuse existing runtimes and
Cosmos/bech32 helpers; no installation, database, wallet or cloud allocation.

Discover at most one bounded provider list, freeze three unique canonical akash
owner addresses from that observation, then qualify three individual reads.
Stop at the declared byte/depth/time limit; do not loop pages or substitute
another endpoint after failure. No final catalogue/owner allocated until all
three records are proved; point selectors must match the requested owner exactly.

Prefer a strict public metadata allowlist: owner, safe display label, service URL
as text, explicit observation flags and bounded exact integer capacity values.
Exclude contact email, exact IP/geolocation, arbitrary attributes and unneeded
nested payloads. Never connect to a provider-supplied hostUri or execute its
workload endpoint; the URL is descriptive data, not an instruction.

Potential CQL scope is configured AkashProviderAddress equality/IN and paging,
then actual spaced C#3/2, HTTP/Mongo5/4/privacy, original hosted/save/replica
acceptance and website card. Label it indexer-reported provider metadata, not a
shared finalized snapshot, consensus proof, owned cloud database or provisioned
compute service. Unknown values remain explicit, not zero or inferred availability.

Next after Eclipse's existing release completes: define exact discovery/probe
budgets and test identity/response codecs, then freeze the native intent. No
Akash native request or environment has been created during this review.

## Pure identity/response codec verified during Eclipse wait

Production AkashPublicProviderCodec validates canonical lowercase44character
akash Bech32 addresses (20bytes/checksum1), exact requested owner, unambiguous
root fields, bounded credential-free HTTPS service URI as descriptive text,
nullable display name and explicit online boolean. It copies only four declared
fields; no contact email, geolocation, arbitrary nested data or capacity/audit
claims. No request is made to a provider-supplied URI. Capacity measurement
semantics remain unqualified rather than guessed from numeric JSON.

13controlled tests pass0failed/0skipped using the actual Akash project in the
reused Ethereum test host. Official example address passes checksum; corrupted
checksum/prefix/case/width, mismatched owner, duplicates/errors, credentials,
unsafe URI scheme, bad labels and boolean types reject.

Evidence TestResults/AkashPublicSourceCurrent/codec-controls.trx, SHA256
65b1bc53672e507045f1f768d5e519b8cd0a99de9e7d6c4cc8460a80f5f0d9cc.
No Akash native call/source owner/config/admission/count increase. Existing
Eclipse source/candidate/release unchanged. Next after the Eclipse release: exact
bounded discovery/probe intent and guarded native reader qualification.

## Bounded HTTP reader controls passed

Production AkashPublicProviderReader separates initial discovery from configured
point reads. Discovery is one GET/2MiB/max1024unique canonical owners, selecting
three deterministically. Queries never rediscover;1–8explicit owners,256KiB per
point response,30second method deadline and16JSONdepth. Point selections either
complete or throw, no partial rows or retries. Conservative process-local1req/s
admission, not an upstream SLA or distributed rate cap.

Eight controlled tests pass0failed/0skipped: discovery3/point3, request spacing,
descriptive hostUri never contacted, duplicate/incomplete/invalid discovery,
final-point HTTP failure, identity mismatch, oversized body and no-dispatch invalid
or cancelled selection. Test fixture public addresses are synthetic, not wallets
or funded accounts. Existing codec13controls remain the separate prior proof.

Evidence TestResults/AkashPublicSourceCurrent/reader-controls.trx, SHA256
00dbd926283cded8eb6e8d8c2e0803d2d584bc81ec444bcd584974cd1a06d19e.
No native Akash request, discovery intent, source owner, CQL or hosted admission.
Next explicit native qualification intent remains pending; Eclipse release
failure diagnostics are current priority.

## Original native qualification failed at bounded discovery

Original exclusive qualification intent persisted before dispatch at
LocalState/COSMIC/AkashPublicQualificationCurrent/qualification.intent.json.
One discovery request reached a successful HTTP status, but its body exceeded
the declared2MiB limit; reader stopped before parsing a complete catalogue or
dispatching provider point reads. Native test1failed/0passed. No source owner,
CQL admission, wallet, account, node, data write or provider-hostUri request.

Failure/log/TRX retained in TestResults/AkashPublicSourceCurrent/native-*.
Native failure JSON SHA256 42bd3545b986815bb203b671233977a61eaa536c6773b141e998ee84119886b6.
No original intent/result replacement or automatic replay. The full-list discovery
strategy needs a bounded selection design; do not raise limits blindly or label
the public catalogue qualified. Existing codec13/reader8controlled proofs remain
scoped controls, not proof of successful native integration.

Next review documented bounded selection/discovery options and implement the
correct read strategy with tests before any explicit evidence-bound continuation.
Eclipse image-build failure still lacks actionable diagnostic evidence; no
Eclipse settings/activation/cohort/card has been published. Counts59/54 unchanged.


## Bounded-prefix root cause fixed and native continuation verified

The invariant is bounded discovery of three configured sample identities, not
validation/download of the entire public network catalogue. The old reader
buffered the whole array before choosing three owners; real catalogue size
exceeded its budget. Discovery now incrementally parses exactly the first three
complete objects, rejects invalid/duplicate owners or fields in that selected
prefix, sorts only that sample, and disposes the remaining HTTP response.
The unconsumed tail is neither downloaded in full nor certified. This is not a
complete network listing or a globally smallest-three selection.

Limits remain2MiB total bytes read,256KiB buffered record,16JSONdepth and30s
method deadline; point reads remain1–8configured owners/256KiB per response.
One guarded transport/rate gate serves both operations; no retry, endpoint
substitution, wallet, source-owner allocation or hostUri request.

17 reader controls pass0failed/0skipped. Nine added cases cover1/7/4096byte
chunks, a catalogue tail exceeding the old body limit, response disposal,
wrong root/missing owner/duplicate field, oversized record, total-byte exhaustion
and truncated selected data. Existing point/error/no-dispatch cases also pass.
Evidence `TestResults/AkashPublicSourceCurrent/streaming-reader-controls.trx`,
SHA25641847bf7932ec51f5be0b574052167da862f2c5523cc9380ebba1c77d0b2fac0.

One distinct guarded native continuation passed1test: selected3identities and
read3matching records, no writes. Explicit continuation intent links hashes of
the original failed intent/failure; both original bytes were checked unchanged.
Durable `LocalState/COSMIC/AkashPublicQualificationCurrent/` now contains
`bounded-prefix-continuation.intent.json`, its acknowledged JSON and preserved
`original-discovery-failure.json`. Continuation intent SHA256
b246f74cb8c1a89ecbfa5c4a26c733bd1bb6ac77660030e6b97f6a4e88712576.
Original `qualification.intent.json` remains unchanged/unacknowledged; never
rerun either native gate to obtain another green result.
Native evidence `TestResults/AkashPublicSourceCurrent/bounded-prefix-native.trx`,
SHA256f3584b317754ce59b26e3c2510beabc8ef398161a0d96eb0d1835fcfcb980ab3.

Next: bind the acknowledged sample to one source configuration/owner; implement
query-only AkashProviderAddress equality/IN+paging with capability/error tests,
then actual spaced C#3/2, HTTP/Mongo5/4/privacy and original hosted/replica gates.
Counts remain59integrated/54hosted. Native metadata proof is not CQL acceptance,
cloud provisioning, shared finalized snapshot or independent chain consensus.

## Query-only CQL adapter controlled acceptance

`AkashPublicProviderMetadataSource` now implements the shared query/activation
contracts over the existing key-value provider base. Explicit config contains
exactly schema1/apiUrl/providerOwners/sourceOwner;1–8unique canonical Bech32
owners. Unknown/duplicate/credential fields reject; guarded anonymous transport,
no cookies/API keys/signers or provider-hostUri access.

CQL supports `AkashProviderAddress` equality/IN and captured paging. Every key
must belong to the configured sample before dispatch, including keys skipped by
paging. Point failures return an error/no partial rows; Take(0) does not hide a
native selection failure. Result budgets, cancellation and inactive sources are
explicit. Save/delete are rejected without native HTTP. Query-profile activation
is separate from legacy provider transaction/storage activation.

Projection is public identity/name/description, descriptive service URI, online
flag and indexer-reporting/consensus-not-verified policy. Stable IDs bind source
owner, Akash provider type, configured API endpoint and native owner address.
Email/arbitrary attributes/capacity/geolocation are excluded. Name/description
are projected values, not additional admitted predicates. No compute provision,
shared finalized snapshot, native writes or full ORM parity claim.

Controlled evidence covers17unique cases: initial run16passed/1fixture failure;
the point fixture used unsupported captured array indexing instead of a bound
scalar. Fixed that fixture and ensured the unsupported-name test builds a valid
query before asserting capability rejection. Affected11query cases then passed;
the6unchanged config cases passed in the original run. No native replay.

- `TestResults/AkashPublicSourceCurrent/adapter-controls.trx`:
  SHA25620ce76bb6785a7fd41a528444f98fa050596fdfadce11e3f01a277fc9554b5c7.
- `TestResults/AkashPublicSourceCurrent/adapter-query-controls-corrected.trx`:
  SHA256b2f72796e34ef549605b733e82bfc8bbdb93a09eca10f43aefe93e925c3cb74d.

Next bind the original acknowledged sample to one source configuration/owner,
add the spaced C# manager/deferred-LINQ3/2walkthrough and actual CLI proof, then
HTTP/Mongo/privacy/hosted/replica/browser gates. No source owner allocated in this
batch, CQL host admission/card/count increase or Eclipse retry. Legacy untouched.
59existing build-output directories measured306,306,252logical bytes, reusable
for remaining Akash gates then disposable after verified teardown. No downloads.

## Actual published C# console3/2 passed

One stable source owner/config was bound to the native continuation ACK; no
rediscovery or replacement native identity. `AkashPublicWalkthrough` is routed
by actual Program.cs `--akash-public`, uses the shared NativeKeySetWalkthrough,
HolonManager and deferred LINQ, and returned3full/2filtered public providers.
Actual demo build0errors; pre-existing warnings remain. Cached restore only,
no new downloads/environments. Console gate passed once without writes, compute
provisioning, wallets, Mongo copies or provider-hostUri requests.

Durable `LocalState/COSMIC/HostedAkashPublicSourceCurrent/` retains source.json,
native-binding.json and original console.intent/acknowledged.json. Freeze these;
never recreate source owner or replay acceptance for green. SourceSHA256
02f8dbf26d700cba6bd4feefa851eb876cca3ffad81aa72d87e3ef1187418cab;
native continuation ACKSHA256
0e42330d3796b2a224ee09ae322a9aba856140f8c631d2011a0f8391424e6774.
Console intentSHA256e15f11635733ac4ab92b2360434c5f42239899cc5696a7388ff17e9b4b4bafa4;
actual assemblySHA25642a945ab8087c95c0b4a44fd33e66a158520398d5c86eaf647f45610eb98317d.
Raw console output TestResults/AkashPublicSourceCurrent/console-actual.stdout.log,
SHA2563e37d84c3ac5f1627cb60596ed1842f9ed8e15129503730ecdc01dd56bd1d4b0.
Guide: [developer walkthrough](../../Devs/AKASH_PUBLIC_PROVIDER_CQL_GUIDE.md).

Counts now60locally integrated query types (3canonical/53bounded profiles/4local),
54scoped hosted HTTP proofs,5browser/49open. Akash has no host tariff/admission,
local HTTP/Mongo acceptance or website card yet. Next register the explicit
query-only HTTP profile, guard/read-only billing capabilities and actual local
Mongo paired/privacy proof; then hosted/private-save/replica/card gates.
Eclipse failure remains unaltered; no speculative deployment or settings apply.

## HTTP admission and actual local Mongo federation passed

Core query-tariff support now admits Akash only with independent server
permission and a captured read tariff. It cannot price SaveHolon/provisioning.
HTTP compiler supports the named query profile and the shared metadata lookup
shape; Startup enrolls it only through CQL_AKASH_PUBLIC_SOURCE_JSON. Legacy
provider activation/write behavior is unchanged. New direct project reference
makes shipping source closure explicit.

One Core tariff control and3APIadmission controls pass. Four configured presets
(native3/native2/Mongo-paired3/Mongo-paired2) plan successfully; missing independent
permission/tariff and write pricing reject. Without Mongo enrollment, native
examples remain available and paired examples are explicitly unavailable.

One original native HTTP test passed through actual loopback CQL endpoints:
native3/2, preserve-copy Mongo federation5/4, foreign private0/public3. Every row
checks server-derived ID/key/provider/visibility and allowlisted policy metadata;
private copies retain exact native allowlisted metadata and version1ownership.
Two actual SDK conditional-create saves and9query usage receipts. Controlled
bearer/authority harness: not production signup/billing/replication/browser proof.

Exact fixed DBcosmic_cql_akash_http_demo contained only2Holons and matching
CosmicOwnedFixture owner/nonce; dropped after success and absence checked.
Owned Mongo8.3.11process stopped; listener27029verified absent. Existing runtime,
replica data/config and original source were reused, never reset/reseeded.
Durable AkashMongoHttpCurrent intent/two-save ACKs/fixture.cleaned.json and
HostedAkashPublicSourceCurrent/web4-local-qualified.acknowledged.json are frozen.
Do not replay the completed fixture or replace its owner to produce green.

Evidence under TestResults/AkashPublicSourceCurrent:

- core-tariff-controls.trx SHA25632e862be806d424ae6d7ea1046740693ae05e864a54d5d4707bbf8855c563284.
- api-admission-controls.trx SHA256bd3be96d36fbd03748cfa89cf6b9027630af6e9d3cb47d655fe99e28ed28c2ea.
- mongo-http-native.trx SHA256af76c5ee17cb4b6c822f9a3ad30f0f6b0f272a112f31172c82a2cda153322d5c.
- akash-mongo-native-summary.json reports3/5/2/4/0/3counts,2SDKsaves/9receipts.

Core task commitf42dd8cbb63573644793656af5de95d3a18a773a pushed/ls-remote verified;
parent gitlink/manifest move together. No Development/production publication,
hosted configuration change, native qualification replay or website card yet.
Next audited hosted publisher/candidate and actual preset checks; verify exact
code release before any configuration apply/hosted cohorts. Earlier Eclipse
image failure still lacks a specific cause; do not blindly replace its job.

## Hosted candidate prepared and actual presets verified

Scripts/Sandbox/Publish-HostedAkashPublicConfiguration.py reuses the shared
protected mutation owner.9pure controls pass: exact sample/schema, independent
permission/free tariff, no enrollment/example replacement, no paid reads,
explicit sandbox, unchanged DNA/other settings, required queryable-key policy
for encrypted copies, source-bound console/local proofs and exact-release guard.
No service variables changed or deployment triggered by preparation.

Protected original before188,598bytes/candidate191,254bytes retained under
HostedSourceRolloutCurrent/akash-public-*-variables.dpapi. Exactly3candidate
values: source JSON, read-only admission/free tariff and4partitioned examples.
Legacy provider/DNA/unrelated configuration unchanged. One test over the actual
public-only candidate export plans all4presets with shipping native/Mongo
capabilities, without source activation or network requests.

Evidence TestResults/AkashPublicSourceCurrent/publisher-controls.log
SHA2562bc3ffc897ba7ebf81fb120305cf13b7f717fdc3fdad17fea445f823af224957;
prepared-presets.trx SHA25653860528fd91bc35fa7eb8eef3846b9f9731907fe2716a24d46534a723c21956.
Public reviews prepared-review.json/actual-preset-review.json are raw regenerable
outputs, not committed evidence. Protected originals/source owners remain frozen.

Next one code-only Development publication of the newly verified Akash feature,
with its own exclusive release intent and exact-job observation. This is the
first Akash code publication; earlier failed Eclipse job remains terminal and
unexplained, with no speculative packaging correction. Apply no source settings
until the exact new release succeeds, then use the original owner/cohort gates.
Shared baselines for separately prepared Eclipse/Akash candidates require explicit
reconciliation if either changes; never overwrite or blindly replay them.

After tests/services stopped,186reviewed generated-output targets cleaned under
lease:2,154,485,642logical bytes removed, secrets retained. No SDK/packages/source,
LocalState/config/ACKs or test evidence removed. Free61,041,795,072before /
63,212,961,792after; concurrent allocation means delta differs from logical bytes.
No .NET rebuild/native replay merely to restore green receipts.

## First Akash Development code publication observed

After fresh fetch/ancestor checks, Coref42dd8cbb63573644793656af5de95d3a18a773a
and parent105329a3fc84e8505a6779d3291de806858c23c2 normal fast-forward pushed to
Development; both exact ls-remote SHAs verified. No branch switches, force push,
production/master promotion or hosted settings mutation. Manifest/sevenDockerfiles/
gitlinks validated before publication. This is a first release of tested Akash
functionality, not a speculative correction/restart of the old Eclipse failure.

Exact new code jobc8ebe618-9946-4546-bdf7-3f810039640b observedQUEUED for105329a3f.
Durable HostedAkashReleaseCurrent/code-publication.intent.json, observed JSON and
metadata.dpapi1398bytes retained. Compact report TestResults/AkashPublicSourceCurrent/
code-deployment-observation.json. Observe the same handle to terminal; no new
submission because observation expires. Keep all settings/cohorts unapplied.

Only after exact codeSUCCESS: clean checkout of105329a3f, expected-release env,
guarded original candidate apply once, restore task branch; then one original
activation and hosted/private-save/replica acceptance. Reconcile any baseline
change explicitly. No root cause established for old Eclipse job, which remains
preserved. Current60local/54hosted/5browser counts unchanged by publication.

## Offline hosted acceptance audit ready; code job BUILDING

Audit-HostedAkashAcceptance.py sends no requests or saves. It checks configured
public owner/endpoint/native key/policy, bounded descriptive credential-free
HTTPS service URI, online flag, no unsupported consensus/contact-payload claim,
3/2native selections, private version1copy identity/provenance/name/description,
5/4federation,0new/2existing repeat preparation, exact two-copy SQLite/Neo4j
projections, foreign0private/3public and2matching normal API save-operation ACKs
with no pending intents. Generic private copies do not contain the full native
payload; this checker does not certify full migration or independent consensus.

Two controlled methods pass, including12negative mutation cases. Raw log
TestResults/AkashPublicSourceCurrent/acceptance-audit-controls.log SHA256
6f9e2677cbce50f67c788322092a1c3b88e22688e45e9864a5c5e5e3909656d5.
Fixtures are synthetic; no real account, cohort, source reads or ACK created.
Verify-HostedQuerySource adds explicit Akash choice for the future original gate.

Same exact first code jobc8ebe618-9946-4546-bdf7-3f810039640b for105329a3f is now
BUILDING, re-observed through the existing observer. No restart/new deployment,
settings apply, activation or hosted cohort yet. Original queued observation and
all protected candidate/source ownership records remain unchanged.

## Original hosted functional acceptance passed

Code jobc8ebe618-9946-4546-bdf7-3f810039640bSUCCESS at parent105329a3fc84e8505a6779d3291de806858c23c2/Coref42dd8cbb63573644793656af5de95d3a18a773a.
Original three settings applied exactly once from guarded exact-release checkout;
per-key ACKs/full candidate equality/sourceowner/DNA unchanged. One activation
2a5ec8a6-c82f-471c-bbfa-c655f9fb1628SUCCESS at same code. Protected original
before/candidate and all release/source records remain frozen.

One original hosted cohort passed native3/2, Mongo5/4, repeat0new/2existing,
foreign-private0/public3, exact SQLite/Neo4j2/2. Two normal API saves are version1
with matching operation ACKs/no pending intents. No replacement owner, native
qualification replay, repeated save or failure continuation. Independent offline
original-cohort audit passed without additional requests or saves. Freeze
LocalState/COSMIC/HostedQueryAcceptance-AkashOASIS and source/release ACKs.

Evidence under TestResults/HostedCqlIntegrationCurrent:

- AkashOASIS-native-http.json SHA25602d98951df77835b56068a053c16701ad69950bf20a483ee6fa37970d3b6921d.
- AkashOASIS-client-http-flow.json SHA25640ad1c3dd2f34530a8a0d8cfcca4e04c8033c82ee5e5df62b5b7fdb16a9f65a5.
- AkashOASIS-isolation-http.json SHA256e05a1745648c15f6f4fc057828a16ca3924e0014b12ecba25b5d84c94e73edfa.
- Durable cql-http-acceptance.acknowledged.json SHA256591cd558a71d828cbd63a63c5d7c0442913d0ff85b3f87bb0d6e273d09b14063.

Published client helper c1006abf6074616428f93e83e13774ba8a6198f35d8ed097d84323466a8b309a
matches pinned Web4Site4530304git blob; local checkout differs only CRLF, with
canonical byte equality verified. This was real HTTP/client-module execution,
not a browser interaction test. Generic private snapshots retain identity/name/
description/provenance/query key, not all native provider metadata.

Next publish prepared Akash card with pinned guide/C#links and verify public
bytes. Browser control remains closed; do not claim interaction verification.
Then explicitly reconcile Eclipse's preserved candidate against the new live
baseline; retain its original owner/config/qualifications/candidates. Old failed
Eclipse image cause remains unexplained; new code success is separate evidence.
