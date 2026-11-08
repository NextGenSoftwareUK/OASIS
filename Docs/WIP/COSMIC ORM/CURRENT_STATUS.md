# COSMIC ORM / CQL current status

Updated 8 November 2026. Goal active and incomplete. Demo breadth comes first;
full native provider implementation and EF/NHibernate parity remain on the
roadmap. Imported, virtual/pointer and hybrid holon modes remain in that roadmap;
this checkpoint does not claim completion of those designs.

## User priority order updated 8 November 2026

Solana → Ethereum → Polygon → Arbitrum → NEAR → EOS/EOSIO → SOLID → IPFS →
ActivityPub → Cosmos → Polkadot → TON → TRON → Hedera/Hashgraph, then remaining
providers in popularity order. This matches the latest user-edited goal. OLID
means SOLID; ActibityPub means ActivityPub; HetraGraph means Hedera/Hashgraph
(HashgraphOASIS).

Solana, Ethereum, Polygon, Arbitrum, NEAR, Cosmos and Polkadot retain accepted
scoped live HTTP and browser evidence. IPFS and ActivityPub also retain live HTTP
and browser proofs. SOLID, TON, Hedera and EOS retain accepted live HTTP evidence;
their browser gates remain open. TRON has no accepted current live CQL admission: preserve rate-limit/denial
outcomes and the authenticated-reader draft/credential preflight; do not retry
failed unauthenticated endpoints or recreate its original cohort. Use the single
owner-action list for any missing account/access requirement, preserving existing
entries and deferrals. Do not rerun accepted native sources or resave copies just
to change queue order.

Focus engineering/demo usability on this chain order before adding further
providers. Existing MapLibre and four-provider plans/owners/settings remain
prepared and unchanged behind the chain priorities; no unverified rollout or
third8November Development push. GOMap WEB4 run42480693015 has completed success;
close out its already-running artifact/cleanup evidence without starting another
GOMap feature ahead of this queue. Remaining-provider popularity ranking must use
current adoption/use evidence when that queue is resumed, with dependencies and
owner deferrals explicit.

## Immediate work

Latest package checkpoint: TRON task image cb38e07b7, immutable digest
sha256:e2a83122b1992aae4f998a08423085264c32c0f7385812fc8efdb882f00b3abd,
passed package plus isolated API/OpenVINO startup42487868944. Seven assemblies,
authenticated TRON reader metadata, source/helper/model hashes, real API health,
graceful exit0/no OOM and all owned containers/network/database volumes removed.
This proves packaging/startup, not native TRON reads or live CQL acceptance.
The empty extra-tag request unexpectedly applied GitHub's sandbox default; the
prior alias digest c9e3748b was restored in42487987112 and verified from its
hashed artifact. Build default is now empty; one-time repair mode removed.
Expanded captured-metadata planner check54e93d046 passed11/11/no skips in
42487361965. Existing private reader location and browser gates remain open;
no Development/master promotion, live settings change or source replay.

Priority TRON checkpoint: SDK38controlled and host configuration/admission11controlled
cases passed. Core query-only admission is eebf39b; manifest/gitlink match. The
native HTTP fixture uses exact height metadata and retained catalogue IDs.
Source54877be94 failed compilation on the ambiguous File type; source2240cc116
fixes it explicitly. Corrected run42484551166/job128743452146 PASSED11/11
controlled cases and compiled the HTTP fixture; native stage was not selected.
Artifact13618634457/2864bytes SHA256
306bd94371a01ecddde976b6e403218d5ec6866966b4f1cce085d516d0d9eb37
was verified in memory, including exact test class/report/no skips. Four owned Mongo
cleanup guard tests passed locally and cloud42484388393. No TRON native/live
acceptance or count increase is claimed. Existing private reader location is
pending David's answer: local/ignored/commented DNA, WEB4 user secrets and dev
settings have no located reader; repository secret-name metadata also has no
TRON entry. Organization metadata returned403, so no further access attempted.
Continue useful priority work without anonymous endpoint replay or a repeated ask.

Full WEB4 image source **8668fd2d040da71ba27e27e49fbed6792eb4e094** built and
published successfully in42475406236. Exact teaching tag/digest:
**sha256:dcd1e9f67e9e44cfb721b23c47c26ad0f79cf490472a648cd32f7d8e5cec0496**.
Combined cloud qualifier **42478524577** (checker sourceb34b55e103) PASSED:
exact package/source/helper/model hashes, six assemblies, genuine WEB4 API health,
OpenVINO metadata, graceful exit0, no OOM and complete owned container/network/
anonymous database-volume removal. Artifact13616282018/4834bytes
SHA256dcd008fe15813240031a2ca4e316f35b2e5b25d6ffae19861e778da2368a703f
verified entirely in memory. No local image/runtime/model/database download.

Waku/NATS immutable producer publication42474138782 and exact consumer42474992596
passed; seven pinned dependency checkouts42472240791 passed. Earlier image and
checker failures are retained in [OpenVINO evidence](OPENVINO_CQL_MVP.md).
API startup proof is task-image qualification, not live CQL acceptance or approval
to deploy unrelated task-branch changes. Exact scoped-candidate tests/image and
live cohort acceptance still remain. The chain priority queue above now takes
precedence over additional provider rollouts; existing prepared batches are retained.

[Full229-project status report](PROVIDER_CQL_AND_SANDBOX_STATUS.md) separates
SDK/native evidence, CQL/COSMIC depth, live/browser and HyperDrive scope. Latest
reconciled baseline has93query-only+3canonical=96accepted/enrolled query types;
129scoped integration checkpoint and28browser flows remain the recorded totals.
No fresh all-provider availability sweep is claimed.

## Live Development baseline and integration cadence

Live accepted baseline remains Civic source
**88c378c1e08ee270365b760c9d3abc29b629a0f4**, activation
**51ca9b77-38e5-4b20-9b26-33b932c8594c**,259settings/212critical/6helpers/13assemblies.
Original cohort18e405bd is sealed: native3/2,federation5/4,reuse0new2existing,
private0/public3,replicas2/2,2saveACKs/pending0. Earlier accepted Holonym, Self,
SubQuery, Dapr and all other sources/cohorts remain preserved; do not recreate them.
LocalFile is accepted; do not redo that resolved gate.

Two Development integrations already occurred8November. No third8November push;
next permitted integration window is9November America/Cancun. Never promote master.

MapLibre candidate **23532a6fc9146d5535152c182e61ec21d8889d6e** / Core
**681a1d0b2d4aa8f3b2751a1fad8857a1dd5f86e0** has62exact checks and image
qualification passed. Its original source owner77afeb39-df6d-4df4-8686-5ff43bd01cbb,
protected baseline/candidate and guarded rollout operators already exist.
Do not regenerate them. Next: permitted scoped integration, exact deployed package,
once-only settings/activation and first owned HTTP/reuse/privacy/replica seal.
[MapLibre details](MAPLIBRE_PUBLIC_SOURCE_STATUS.md).

## Four-provider follow-on batch

Sandbox contracts, Polygon ID schemas, SEEDS settings and OpenVINO metadata have
separate native CQL/WEB4 proofs. OpenVINO additionally passed actual production
runtime packaging, real loopback service startup/inference and graceful shutdown.
These scopes are not yet live-enrolled.

Current **unpublished** scoped parent candidate:
**d555cb2a7959a27a3af22dc3d45b06e25568bac7**, Core
**e242de41c41dbd4a1a4089694a2d945cdff828b4**,69scoped paths. Existing candidate
branch refs still point to MapLibre; publish only after accepted MapLibre and
fresh shared-ref checks. Candidate publisher is prepared, never invoked.
Exact combined-candidate qualification and candidate image remain required.
Task-image startup is now passed; the guarded publisher additionally requires
that exact API/native/graceful-stop/cleanup proof. It has not been invoked.

Original offline plan: `LocalState/COSMIC/HostedFourProviderSourcesCurrent`.
It reuses the original MapLibre261setting candidate and prepares270settings/
11changed keys/one intended activation. Source owners/public IDs and the353014byte
protected candidate are already created. Preserve them; amendments update only
candidate references. No settings were applied and no native source writes occurred.

[Sandbox](THE_SANDBOX_PUBLIC_SOURCE_STATUS.md),
[Polygon ID](POLYGON_ID_PUBLIC_SOURCE_STATUS.md),
[SEEDS](SEEDS_PUBLIC_SOURCE_STATUS.md), [OpenVINO](OPENVINO_CQL_MVP.md).

## Coverage and website

**128 scoped local /96live HTTP /28functional browser**. These are bounded demo
scopes, never full SDK/storage/ORM parity claims. Cloud HTTP tests do not increment
live counts. [Provider matrix](PROVIDER_CQL_AND_SANDBOX_STATUS.md).

Website public main remains accepted Civic card81 at
**fa0f848b2bbb7418fdbe2fca4f0e0a45367ec3bd** (byte-verified).
Website task **a0883cfccdf55aa4ef3e1a183d4913d69b454ce7** contains pending82MapLibre,
83Sandbox,84PolygonID,85SEEDS,86OpenVINO;99selected client checks pass.
For MapLibre-only publication, omit83-86; restore them only on the task branch.
Browser URL-confidence rejection is recorded; do not retry or bypass it. Recent
Windows discovery found no browser windows; no new navigation/browser acceptance.

## Other open gates

Gelato supported Gasless client passes34controlled cases on563e87ed1;
native credentials/CQL remain open. Access is deferred in the single owner list;
no immediate key/funding/account request. [Proof](GELATO_PUBLIC_SOURCE_STATUS.md).
Preserve Azure Cosmos/Oracle/Postgres hosting and Redis/Valkey TLS owner decisions,
Lens403no-retry restrictions, Nostr reliability and Cassandra/Astra hosting gates.
Cassandra's original accepted native/restart/CQL/HTTP/browser evidence is not a
reason to rerun it. Remaining inventory and deeper parity are still incomplete.

## Workspace and resource rules

Use `C:/Source/OASIS2`, parent `work/cosmic-cql-ton-public`, Core
`work/cosmic-cql-decentraland`, website `C:/Source/Web4Site` / `work/cosmic-cql`.
Preserve unrelated dirty TRON, general cleanup/owner-list edits, Core ProviderType,
STAR/WEB6, solution, native-evidence deletions and untracked provider directories.
Stage only owned paths/hunks; never reset, force-push, switch shared branches or
create another worktree/environment for this workflow.

All heavy builds/runtime/model/image downloads stay in cloud. Reuse cached tools;
retain compact evidence, verify owned cleanup, never repeat denied cleanup.
Last C: measurement45.76GiB free. This laptop's separate ledger:
[RECOVERY_LAPTOP_CLEANUP_LEDGER.md](RECOVERY_LAPTOP_CLEANUP_LEDGER.md).
Owner actions only: [ACTIONS-FOR-DAVID](../Release%20and%20Status/ACTIONS-FOR-DAVID.md).
Use the build lease for shared .NET outputs and bounded workers; no local build
or new SDK/environment is needed for current cloud qualification.

Historical checkpoints remain in git and the linked provider evidence docs.


GOMap follow-on: native SDK routing passed28controlled+1native case (3fixed
public routes); bounded CQL adapter now passed12controlled+1native case on
source525434ce2/run42480043896. Exact full3/subset2 route keys/IDs/native JSON
hashes/attribution verified from8168byte artifact13616947227 in memory. No source
writes, Core/WEB4 admission, Mongo federation/replicas/browser/live enrollment or
four-provider candidate change. Next: explicit query-only Core/WEB4 integration
and scoped cloud HTTP proof. Coverage checkpoint remains unchanged.
See [GOMap evidence](GOMAP_PUBLIC_SOURCE_STATUS.md).

GOMap query-only Core8b085f8/WEB4 owner registration,8boundary cases and1native loopback HTTP/Mongo case prepared; cloud qualification pending. Core gitlink/manifest updated together, existing source SDK/native CQL proof retained. No new live source or candidate change.

GOMap completed closeout: WEB4a1417375e/Core8b085f8 run42480693015 PASSED8+1,
native3/2/Mongo5/4/private0/public3/2SDK saves9receipts; DB absent. Hashed5132byte
main+237byte cleanup artifacts verified in memory; owned container/anonymous
volumes absent. Scoped integration checkpoint129, live96/browser28 unchanged.
No further GOMap feature/rollout before the priority chain queue. Existing Edge
window has no sandbox tab; no navigation, save replay or URL-policy bypass made.
TRON reader key missing in expected process/user/machine locations; one access
entry added to ACTIONS-FOR-DAVID and privately configuring it requested once.
Continue engineering while access is pending; do not retry anonymous endpoints.

TRON priority engineering: recovered authenticated header adapter/tests copied
byte-for-byte from existing HostedTronAuthenticatedQualification into the normal
provider/unit projects; no retained fixture/source rewritten. The exact separate
catalogue SHA2567f1c8fe3c35ef3c45ee31be6eaef72f34f8d10185a28e2177ace267822e89c02
is preserved as an explicit test fixture, not claimed as the absent old cohort.
Shared manager/LINQ walkthrough linked normally; test-only MSBuild injection and
branch switching are not used. New cloud-only workflow verifies38controlled cases
against current pinned Core; actual compilation pending. Native stage defaults off,
requires an explicit dispatch and dedicated configured reader secret, and consumes
frozen private outputs. No native request, grant, live enrollment or count increase.
Missing access remains recorded once in ACTIONS-FOR-DAVID; no repeat ask.

TRON integrated controlled qualification now PASSED38/38/no skips:
source946a3bc4f06dead2759daa18c7aa09719443eac2,
run42482265144/job128736560638, current pinned Core8b085f8.
Artifact13617703920/6582bytes SHA256
9ff7c04782b7999e84c68bc356b63876daf2f097d4c270eec3a987565db0004c
verified wholly in memory. This supersedes reliance on the older local-only
38-case injected build; original draft/catalogue retained unchanged. Authenticated
native stage was explicitly skipped by workflow selection, not counted as a pass.
Reader-access question remains pending once in ACTIONS-FOR-DAVID. Native CQL,
WEB4/live grant, browser and coverage increase remain open. Continue priority
engineering without another anonymous endpoint attempt.

TRON WEB4 configuration boundary prepared: explicit sandbox-only catalogue +
separate COSMIC_TRON_PUBLIC_READER_KEY pair; incomplete configuration fails,
no anonymous fallback or activation during registration. Nine controlled host
cases added with cloud-only build lease workflow; qualification pending. This
does not yet add compiler/tariff admission or native/live/browser acceptance.
No local build, installation, database, model/image or artifact download.

TRON query admission prepared in Coreeebf39b and WEB4 compiler, with exact
height-string native3/2 and Mongo federation preset planning plus independent
read tariff/write rejection tests (11 controlled host cases total). Cloud
qualification pending; no native/live increase. David chose existing private
configuration; names/presence-only searches of local settings, retained baseline
and current Development nested settings found no reader credential. Exact
private location requested in the existing owner entry. No values logged or
protected settings written, no local build/download/runtime/database created.

TRON host configuration VERIFIED9/9 executed/passed/no skips, sourceb10dd266f,
run42482939556/job128738613047. Artifact13618815990/2483bytes SHA256
843df72c88106d0a6476f3cc55141a6c6d8c7df06e6e25bbab95102868473c38
inspected in memory; compact local receipt only. Expanded admission/preset
sourcee4d9a6415/Coreeebf39b is running42483509747/job128740304255;
do not restart on observation timeout. No native grant/live/provider count change.

TRON native HTTP qualification prepared (not executed): shared HTTP/Mongo
fixture now supports explicit metadata-string selections alongside its existing
ID contract, preserving physical-ID/privacy assertions, two SDK-owned saves,
nine receipts and success-only owned DB cleanup. TRON case consumes the exact
retained separate catalogue hash/owner/heights and stable native IDs; no old
cohort rewrite. Cloud workflow defaults to controlled-only; authenticated HTTP
requires explicit dispatch plus a private secret before any native work. Owned
Mongo helper now accepts only gomap/tron profiles and preserves GOMap defaults;
TRON uses distinct labels/container/report paths. Syntax/help/diff checks passed;
C# compilation and native execution not yet verified. No local downloads/builds.

TRON controlled WEB4 admission/preset planning VERIFIED11/11/no skips:
sourcee4d9a64155ee645b026f3aaab792d455620bea3c/Coreeebf39b,
run42483509747/job128740304255. Artifact13617664870/2781bytes SHA256
27925a9e390543ddad4ca2ae8c4981303df5e405c686dcb5867010c33a953ff8
inspected in memory; compact receipt only. Exact-height3/2 and Mongo pairs
are planned, not natively executed. Updated source54877be94 HTTP fixture
compilation is running42483969440/job128741706991; native selection is off.
Native/live/browser/counts remain unchanged. C:41.07GiB free; this work has
created no local builds/downloads/databases. Preserve unrelated disk activity
and rejected cleanup scopes. Existing private credential location still pending.

Cloud source54877be94 compile failure identified from PolygonID run42483969430/
job128741706820 and Holonym run42483969460: CS0117 CqlTronHeaderHttpTests.cs17,
File resolved to an OASIS type. Fixed the filesystem reference explicitly to
System.IO.File; no alternate execution path. Compilation failed before native
HTTP/Mongo setup; earlier accepted cohorts are unchanged. Preserve original
failed runs; new corrected-source qualification required, no replay/restart of
those old handles. Lightweight ownership cloud run42484388393 PASSED, covering
all four controlled guard tests, with no real Docker/native operation.

Corrected TRON HTTP fixture compiled and controlled host tests PASSED11/11
with zero skipped, run42484551166/job128743452146/source2240cc116. Exact
artifact13618634457/2864bytes SHA256
306bd94371a01ecddde976b6e403218d5ec6866966b4f1cce085d516d0d9eb37
verified in memory, exact boundary.trx/test class and counters asserted. Native
HTTP/Mongo stage was not selected, not a native pass. Original failed source
run42483969440 is terminalfailure with CS0117 verified directly, no replay.
No archive/TRX extracted, no local build/runtime/database/model/image install.
Next: authenticated native SDK+HTTP qualification when the existing private
reader location is resolved; independent exact scoped package preparation may
continue, preserving accepted cohorts and Development cadence.

TRON package preparation: added explicit opt-in requirement for the shipped
TRON assembly and authenticated header-reader type/method metadata strings to
the existing full-image qualifier. This is package presence, not native/API
acceptance. Reused the typed Docker missing-resource contract for package
container absence; unknown errors cannot count as cleanup. Three existing
startup/health/absence controls pass locally with no images/native resources.
Next: one immutable task-image build, then exact package/isolated startup proof;
no Development promotion or live settings mutation. No local build/download.

TRON task-image build dispatched once and acknowledged204 after confirming no
active full WEB4 build. Exact sourcecb38e07b723ce3bc6800b09ce506b419be0f6ca1,
run42485543286 is in progress. Requested no extra alias; GitHub instead applied its sandbox input default.
See the explicit alias reconciliation correction below. Source manifest/gitlink and all provider SDK publication checks passed.
Three startup/absence controls passed; explicit TRON package-profile check is
prepared. Reuse this original run, do not duplicate it on timeout. After success,
verify exact published digest and run package+isolated startup with tron_reader
selected. This builds the task snapshot, not a deployment-scoped candidate or
native/live acceptance. No Development change, live settings, reader request,
local runtime/build/model/database/image download or new worktree/environment.

TRON controlled planner check expanded to the actual captured metadata-path
LINQ expression used by the shared HTTP fixture, exact height3/2 and retained
source IDs, before activation. This addresses an untested fixture expression
shape without a native request. Existing11test count retained; new cloud
qualification required. Original image42485543286 remains live; only test/docs
change, so do not rebuild that image. No local build/download or source replay.

Image publication VERIFIED sourcecb38e07b723ce3bc6800b09ce506b419be0f6ca1,
run42485543286/job128746476064, digest
sha256:e2a83122b1992aae4f998a08423085264c32c0f7385812fc8efdb882f00b3abd.
Exact checkout/summary/tag/push log verified in memory. CORRECTION: the empty
extra tag request was normalized to the workflow default sandbox, so the build
also updated that registry alias; prior no-alias statements were incorrect.
No service deployment requested. Prior sandbox publisher42441146593 at03:11UTC
had digestc9e3748ba13de0e0134c7e0046e1b30840d27ab6cc56ff056fdf4f3b187530b5;
all nine intervening full-build logs explicitly use unique teaching tags.
Changed the workflow default to empty and prepared an incident-scoped cloud
manifest-only restore: same build concurrency group, exact current digest
guard, no image layers/download, refuse foreign updates, verify prior digest.
Three pure decision checks pass locally; restoration not yet executed.
Exact-digest package+isolated startup qualification dispatched once204 using
checker54e93d046 and tron_reader=true; independent of mutable alias, not live
or native CQL acceptance. Native reader location remains pending once.

TRON package/isolated API+OpenVINO startup VERIFIED42487868944, checker54e93d046,
built sourcecb38e07b7/digeste2a83122. Artifact13620138373/4832bytes SHA256
3bf90b4309b54feb5d85e0a82b394395d8a494e27aa38cc5639b7cf328aef873
checked wholly in memory:7assemblies/TRON reader metadata, helper/model hashes,
real API health, native owned model metadata, graceful0/no OOM, owned package/API/
Mongo containers/internal network/anonymous volumes absent. Not a TRON native
request/live acceptance. Captured-expression planner11/11 passed42487361965;
artifact13619964037/2856bytes SHA256
57c601dd690a98290b0fd254adfcd3be27f181d96d9d97b96192a31f8f7eea81.
Alias incident CLOSED: restore42487987112, exact current guard and one manifest
PUT, prior digestc9e3748b independently verified,0image-layer downloads/no service
deployment. Artifact13619784846/678bytes SHA256
da8cec98a2074bd053f55006b16e8edf1fc17610a15c916ba3ed0f6eee9b0160.
Removed the owned one-time repair workflow mode/helper after success; retained
compact journals/receipts and permanent empty-default root fix. No local images,
ZIP/TRX extraction, SDK/runtime/build/database/model installation or new worktree.

Scoped TRON runtime/test draft prepared against Development88c378c1/Core81862b2:
TRON_SCOPED_DEVELOPMENT_PARENT.patch (13paths), CORE.patch (one tariff file),
and PLAN.json record exact trees/hashes. Both patches independently reapply to
temporary indexes and reproduce their recorded trees. Exact public fixture SHA
7f1c8fe3 unchanged; SDK walkthrough already exists identically in baseline.
Only explicit TRON references/registration/compiler/read tariff, source/tests
and metadata HTTP fixture change are selected; queued provider candidates and
unowned files untouched. Manifest/gitlink advance together in the draft only.
77,322bytes of source patches retained with -text attributes, no worktree/runtime
created. Initial default whitespace check rejected immutable CRLF fixture bytes;
actual source patch checks retain normal whitespace rules plus cr-at-eol.
Temporary indexes removed in finally; real indexes unchanged until named docs
staging. Compact exact Core commit object metadata makes the draft reproducible
on another laptop without an unreferenced-object dependency. No runtime candidate
ref published or deployment/setting/native request. Qualification-tool closure,
scoped cloud build and actual credentialed proof remain open. Windows credential
manager target/environment-name checks also found no reader; no values printed.

TRON qualification dependency closure: moved the single typed Docker absence
contract into owned_docker_resources.py. Shared Mongo and package/startup
checkers import that one implementation; TRON no longer imports the unrelated
OpenVINO startup harness merely for resource checks. Full-package workflow
retains/restores all three checker dependency files around exact-source checkout,
including old built sources without the new module. Existing3startup/absence
and4Mongo ownership controls pass locally; syntax/diff checks pass, cloud controls
pending. This is qualification tooling only, no runtime/API behavior, image rebuild,
source replay, private settings, local SDK/database/model download or new worktree.
Next: incorporate only the small qualification closure into the scoped draft and
verify the isolated source in cloud; native reader location remains pending once.
