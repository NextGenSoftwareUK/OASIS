# Dapr CQL MVP

Current7November: Linux restricted-read/write-denial/WEB4 native proof passed.
Rebuilt upstream native IO now loads and passes the synchronous AOF write probe
in42416939381; its full durable restart also passed. Stronger shared
configuration/default-writer-disabled recovery also passed in42417629733; no hosted Dapr source,
startup flag, visitor enrollment or public card is enabled. Coverage119/91/28.


The read-only profile uses the existing Dapr .NET SDK to request strongly
consistent reads of `oasis/holon/{id:N}` from one named state component. It
uses an explicit IPv4 loopback sidecar endpoint, authenticates with a sidecar
token and caps gRPC replies at 1 MiB. The shared query profile checks public
visibility and canonical identity before paging. The query backend rejects
listing, writes, deletes and non-Holon reads.
[Dapr .NET client](https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-client/dotnet-daprclient-usage/).

Six focused tests passed with zero skips at Core
`79405fdb497834f275ef10edeb7d4b3b2c47ee38`. The run includes four CQL and
endpoint controls and two storage index regressions. TRX SHA-256 is
`2adfe17177d0204e3bf1e512993854692f0cf03351158570a8eaf98a3360edd4`.

Native verification passed on a pinned portable Dapr 1.18.9 sidecar and a
dedicated in-memory SQLite state component. The synthetic fixture returned
3/2/0 and its values and ETags were identical before and after the reads.
The native log SHA-256 is
`eeb88d1f7b32f6b76564d8ffafc8bde3993a10b0d2a8332ddfcb1230c904269f`.
The sidecar stopped, ports 54861â€“54863 were clear and no database file remained.
Hosted WEB4 enrollment
requires a co-located sidecar and a reviewed state component and remains open.
LocalFile keeps the first live Development rollout slot.
[Dapr SQLite component](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-sqlite/).

## Development WEB4 candidate

Opt-in settings are `CQL_DAPR_QUERY_STORE`, `CQL_DAPR_QUERY_GRPC_ENDPOINT`,
`CQL_DAPR_QUERY_PROBE_ID` and the protected `CQL_DAPR_QUERY_SIDECAR_TOKEN`.
The catalogue binds the named component, canonical IPv4 loopback endpoint
and first public ID. All 22 protected-settings tests passed, including
component and port substitution checks. Seven Dapr WEB4 admission checks and
seven Fastly regressions passed without skips at Core
`7ee05c39f2415d3da7ed41acb6514ff6dc2db87d`. Admission TRX SHA-256 is
`f1d76919f67201c6d056a3775231274fe5ead475b5e20c9d109fefee53de2c19`.
Candidate preparation also checks that the protected baseline has a valid
sidecar token. The token stays outside the reviewed catalogue and settings diff.

The sidecar must run in WEB4's network namespace and bind its application
API to loopback. Dapr's state API allowlist covers the state API group,
which includes mutation methods. Verify a dedicated backing store and its
write-denial policy before exposing this profile. Verify the actual sidecar
and native reads on the reviewed WEB4 instance before enabling visitor
settings. [Dapr API allowlists](https://docs.dapr.io/operations/configuration/api-allowlist/).

The Windows Dapr 1.18.9 SQLite component failed to start on a synthetic
database reopened with `mode=ro`. The second run reproduced SQLite disk I/O
error 3850 during the component ping after the writer had shut down cleanly.
It never reached the read or write-denial assertions. The failure log SHA-256
is `76daa1c4052e44efc38b91b1937bf97bf9ce985227d3f64703e5dd4ebed7dcdf`.
Keep hosted write denial open until a reviewed backing component passes it.

The local `state.redis` alternative passed with the existing Garnet 2.2.5
runtime. A dedicated writer sidecar seeded a synthetic record and shut down.
A second sidecar used a restricted Redis ACL principal and read the record
with its ETag. The attempted update returned Redis `NOPERM` through the
sidecar, and the value and ETag remained identical. The result SHA-256 is
`78db3be203f637125ae4cf0c37ad3e19ccad926c03c1d135870f58932a811e04`.
The follow-up run passed native gRPC CQL 3/2/0 through that same restricted
sidecar at Core `7ee05c39f2415d3da7ed41acb6514ff6dc2db87d`. Values and ETags
were unchanged. The native CQL log SHA-256 is
`b0e5e321d3df08f010ffa7fd2a874b13aa1d24fb25deed43c906fd09c0ba1489`.
The actual Development backing store, sidecar and visitor route still require
verification.
[Dapr Redis component](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-redis/).

The loopback HTTP acceptance passed. It runs the shipping CQL
endpoint with the real Dapr registration, profile registry, protected public
catalogue and prepared read execution. The fixture supplies controlled bearer
authentication and usage authorization. The test checks exact public IDs and
provider receipts for both the three-record and two-record requests. The
initial run and the repository fixture runner each passed one test without
skips. The repository runner TRX SHA-256 is
`e93ac939be51b939ba9768498799fe229a51f5f6805eeeecfae10019eb7c8471`.
Hosted authentication, browser verification and federation remain open.

The repository runner is
`Scripts/TestHosts/Run-CosmicDaprWeb4Native.ps1 -Daprd <exe> -Garnet <exe>`.
It validates the pinned Dapr 1.18.9 and Garnet 2.2.5 binaries, builds under
the shared lease, and verifies that the native HTTP test ran and passed
without skipping. It uses synthetic loopback data and leaves compact logs
in this checkout's qualification directory.

## Linux hosting qualification â€” 7 November 2026

The six-reader batch is accepted/published; Dapr is the next independent native
hosting gate. Existing Windows runner pins Windows executable hashes and uses
Windows process creation, so its native result cannot certify Linux deployment.
Keep that original result intact. No native Dapr fixture has been replayed here.

[Linux runtime pins](../../../Scripts/TestHosts/dapr-linux-runtime-pins.json)
record the official Dapr1.18.9 Linux amd64 archive (64024904B) and Garnet2.2.5
Linux x64 framework-dependent archive (5694440B), including release digests.
These were read from official GitHub release metadata, without downloading
archives or creating a local runtime environment. Native Linux execution remains
unverified. Next: one pinned cloud-runner fixture proving restricted reads,
write denial, unchanged values/ETags, WEB4 native3/2 and clean owned shutdown;
then durable hosted source/restart and Mongo federation before enrollment.

[Dapr release](https://github.com/dapr/dapr/releases/tag/v1.18.9),
[Garnet release](https://github.com/microsoft/garnet/releases/tag/v2.2.5).

The native fixture is now shared as `run-cosmic-dapr-web4-native.py`; the
existing PowerShell entry point selects it and retains both exact Windows
binary hashes. Linux requires the pinned archives and exact extracted executable
bytes/locations. Installer extraction rejects links, traversal, duplicate
executables and expanded-size overflow. Five offline controls pass; no native
Linux result is claimed by those tests.

`qualify-dapr-linux.yml` runs the pinned installer only on an ephemeral Linux
runner, builds the actual HTTP host under one provider build lease, then runs
the same native writer-shutdown/restricted-reader/write-denial/unchanged-ETag
and WEB4 assertions. It retains only compact TRX/result evidence for30days,
not archives, ACL credentials or runtime logs. No Development rollout is wired
to this qualification job. Terminal native proof remains required.

## Linux native qualification passed

Exact task source02b15cc56 passed the [Linux qualification](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42412751967),
job128524212961. Actual pinned Dapr1.18.9 and Garnet2.2.5 archives installed
and verified. Build succeeded with0errors (3201existing warnings). Native reader
ACL allowed reads, denied a write with NOPERM, and retained identical value/ETag.
The fixture required its single actual WEB4 HTTP test to pass1/1without skips.
It exercised registered Dapr native3/2 through the shipping endpoint with
controlled test authentication/metering. Owned processes stopped and ports cleared.

Remote compact artifact13590146953:1698B, SHA256
`f006ad2632e1b1605c6780638bd044e582a2d4a5118686e86209d1c62ceb1f41`,
expires7December2026. No artifact/runtime archive downloaded to this laptop.
Durable restart, live visitor authentication, Mongo federation and actual hosted
sidecar enrollment remain open. Counts119local/91live HTTP/28browser unchanged.
Next: extend the dedicated native fixture to persist acknowledged records and
verify a backing-store/sidecar restart without another seed operation.

The next qualification extends the same owned fixture with synchronous AOF
commit acknowledgement and bounded log pages/segments. After the one writer
stage shuts down, the restricted reader checks every original value/ETag and
WEB4 queries. Both backing store and sidecar then restart in recovery mode;
no writer or seed operation exists in that phase. All four original records
(including the tombstone), write denial, and a second1/1WEB4 test must pass.
Both TRX reports remain separate; the earlier passing artifact is preserved.
Recovery flags were checked against [pinned Garnet2.2.5 options](https://github.com/microsoft/garnet/blob/v2.2.5/libs/host/Configuration/Options.cs).
This stronger restart proof is pending execution, not inferred from syntax.

Hosted image packaging is prepared in the task Dockerfile using the same
`dapr-linux-runtime-pins.json` and integrity installer as qualification. Only
the Dapr binary and Garnet net10 runtime directory enter the final image;
Python and downloaded archives stay in the separate build stage. Version
commands are required at image build. This stage has not been image-qualified
or deployed. No startup flag, source seed or visitor enrollment is enabled.
Hosted source creation must be an explicit journalled provisioning operation;
normal startup must only recover existing acknowledged records. Preserve the
live six-reader release and source generations while implementing that contract.

Restart qualification42413413865/j128526185779 FAILED at Garnet initial startup,
after a successful host build. Trace reached `backing_store('initial')` before
the writer sidecar or seed call; no native record mutation or HTTP proof occurred.
The prior passing nonpersistent Linux proof remains valid in its narrower scope.
No TRX/result artifact existed for the failed restart attempt. Do not call this
restart certified or silently remove its failure.

The fixture now retains a bounded native diagnostic with all generated credentials
redacted before tail truncation. Six offline integrity/diagnostic controls pass.
A startup-only preflight runs before the expensive host build and performs no
seed, sidecar call or WEB4 query. This next run diagnoses the actual server error;
no persistence flags are guessed or removed to bypass the failed invariant.

Startup diagnostic42414093417/job128528313952 identified the root cause:
`LogDir specified without enabling tiered storage (UseStorage)`. Garnet exited0
without opening its listener. The preflight stopped before host build or seed.
The fixture incorrectly supplied the main-store `LogDir` while using an in-memory
main store with durable AOF. The correction removes only that invalid setting;
explicit `CheckpointDir` still owns AOF persistence. Synchronous commit waiting,
strict recovery and original-value/ETag comparisons remain required. No source
is recreated in recovery. The redacted diagnostic artifact is retained remotely.

Corrected run42414245421/job128528770620 passed startup preflight and host
build, then FAILED with TimeoutError during the initial Dapr state POST.
No HTTP acceptance or restart stage ran. Original seed outcome remains unknown;
there is no automatic resend and the failed attempt is not promoted to a pass.
Its startup-only artifact13590938195 is254B, SHA256
`1ddb496022a851fb2bd0bbced1ea6fc0465c7c46debe48da64c625b879851273`.
The timed-out fixture was confined to the ephemeral CI runner; live sources and
accepted cohorts were not involved.

Failure capture now covers every owned native process, with bounded redacted
logs and seed intent/ACK state. Every subsequent explicitly isolated fixture has
a unique canonical GUID prefix; the actual C# native test requires that exact
prefix from its runner. Recovery within one fixture retains the same identities.
This prevents another diagnostic fixture from reusing the failed attempt's
record identities. Timeout and persistence flags remain unchanged pending
native server evidence; no success is inferred or retry hidden.

Diagnostic42415058067/job128531230929 FAILED after startup/build passed.
Redacted native logs show the object-store HSET path entering AOF commit and
failing in `AllocatorBase.device.WriteAsync`; the Dapr caller timed out.
The leading device exception was omitted by tail-only truncation. Its original
seed intent is unacknowledged; preserve the uncertain outcome, never resend.
The connector's job snapshot was stale; direct GitHub job timestamps established
terminal failure. Use fresh REST state for this job's subsequent observations.

Diagnostics now retain leading error lines as well as the bounded tail, after
credential redaction. Seven offline integrity/diagnostic controls pass. A direct,
journalled single-key HSET probe on its own unique diagnostic source now runs
before the host build. It exercises the same native AOF write path without Dapr,
so a storage failure can be diagnosed without rebuilding WEB4 or resending the
uncertain Dapr request. Only that probe's own intent/ACK and redacted logs are
retained. Original live sources and accepted cohorts remain untouched.

Native AOF probe42415866369/job128533697756 identified the concrete root cause:
DllNotFoundException for mandatory `runtimes/linux-x64/native/libnative_device.so`.
The probe failed before the host build or Dapr seed; its own native control write
was unacknowledged and is not resent. The standalone archive did not supply the
library at the loader's expected location. This was a packaging defect, not proof
that synchronous commits should be disabled or the timeout enlarged.

The manifest now pins the required native library from the same official Garnet
v2.2.5 commit `8d007138208ce030eb10a50c80e636b1678b83e4`:2210897B, Git blob
`4eaa89279f0a83be63a5829e26faba693420d2c1`. The installer always places it in
its mandatory runtime location, verifies size and complete Git-object identity,
and requires a successful native load before any source write. Required OS
libraries `libaio1t64`/`liburing2` are declared in CI and the runtime image.
No alternate storage device, optional symbol or silent recovery is added.
Eight offline integrity/diagnostic controls pass. Corrected native proof and
image qualification remain required. No native library was downloaded locally.
[Exact upstream native source tree](https://github.com/microsoft/garnet/tree/8d007138208ce030eb10a50c80e636b1678b83e4/libs/storage/Tsavorite/cs/src/core/Device/runtimes/linux-x64/native).

Mandatory-load qualification42416231682/job128534813817 FAILED before any write:
upstream prebuilt native library requires `libaio.so.1`, while the target OS
provides the t64 ABI. The fail-fast load check exposed this instead of allowing
another uncertain source mutation. No compatibility symlink is introduced.

Packaging now builds the mandatory native library from the exact clean upstream
Garnet commit8d007138208ce030eb10a50c80e636b1678b83e4 using its own CMake
`native_device` target, Release mode and USE_URING=ON. A sparse public-source
checkout and two compiler workers stay on the ephemeral runner/build stage.
The resulting library is linked against the target OS's actual development
libraries, placed at the SDK's mandatory path and SHA256-bound to a source/build
receipt. Native loading remains mandatory before source writes. CI and Docker
use this same installer/recipe; compiler/source/archive directories do not enter
the final runtime image. Eight offline integrity/diagnostic controls pass.
The rebuilt native AOF probe and full restart proof remain pending execution.
[Upstream CMake contract](https://github.com/microsoft/garnet/blob/8d007138208ce030eb10a50c80e636b1678b83e4/libs/storage/Tsavorite/cc/CMakeLists.txt).

Source-build qualification42416692352/job128536212384 reached the upstream
native_device compilation, then FAILED on missing `uuid/uuid.h`; no probe or
Dapr save ran. Declared `uuid-dev` in both CI and the isolated Docker build stage.
The native build's source/SHA256 receipt is now included in compact qualification
artifacts, alongside the separate native results. This does not include source
checkouts, compiler outputs, runtime archives or credentials. Native and image
qualification remain open until the corrected build and tests pass.

## Shared hosted lifecycle configuration

`Docker/DaprRuntime/contract.json` owns native ports and AOF parameters.
`configuration.cjs` generates the actual Garnet arguments, Dapr arguments,
component YAML and separate provisioning/reader ACLs. Six configuration controls
pass: strict durable recovery, explicit lifecycle selection, reader-only permissions,
credential-injection rejection, loopback endpoints and distinct writer credentials.
The native fixture now consumes these same generators through Node stdin/stdout;
credentials stay out of command arguments and console output. During recovery
it disables the default writer and authenticates readiness as the reader.
This stronger reader-only recovery is pending cloud execution. The final image
copies the shared configuration, but no startup path invokes Dapr yet.

## Durable Linux restart proof passed

[Run42416939381](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42416939381),
job128536974684, sourcef90c23b74 SUCCESS. Compiled native IO loads and its AOF
probe is acknowledged. The actual Dapr source is seeded once; all four original
records and ETags survive backing-store/sidecar restart. Restricted reads,
write denial and WEB4 native queries pass before and after restart.

Artifact13591404960 inspected in memory:4920B, SHA256
`afded8becb041ab9d3fe5810aaa2557baed056703a42f3bf9a3c7f2ffb3536b1`,
expires7December2026. Initial/restart TRX each contain1passed/1total. The single
four-record seed intent/ACK match. Build receipt matches pinned source/recipe,
compiled native library SHA256
`4ec8298481a7ce929f2245e6a67d3b733a2a4f550c59c96ccecc9a7cd046422d`.
No ZIP or extracted copy saved locally; compact qualified receipt retained.

This baseline used the provisioning backing-store ACL with Dapr's restricted
reader. Stronger recovery with default writer disabled and shared configuration
is running separately in42417629733/source0c5800169. Hosted first-generation
provisioning, image qualification/enrollment, Mongo federation and visitor/browser
acceptance remain open. Coverage119local/91live HTTP/28browser unchanged.

## Reader-only recovery and hosted helper

Shared-configuration qualification42417629733/job128539040599 SUCCESS,
source0c5800169. Native AOF writes, actual Dapr restricted reads/write denial,
original values/ETags and both WEB4 query stages passed with the recovery ACL's
default writer disabled. Seed ran once. Remote artifact13592083986:4915B,
SHA256`5f836937bc7ed8b0fad1bcf5e3599cb04052ac1bafde2fff02998e1bfbc851e6`,
expires7December2026. Earlier baseline and failed attempts remain retained.

`host-sandbox-dapr.cjs` now implements recovery-only service configuration.
It requires original acknowledged source/catalogue/owner/token binding, exact
private ACL/component files and an existing bounded native store. It never
creates a missing source. Reader-authenticated Garnet readiness precedes Dapr
health and exact original values/ETags. Source queries have bounded responses,
explicit cancellation and no retries. Thirteen controlled configuration/runtime
checks pass; those checks do not establish hosted or native SDK acceptance.

The next real Linux fixture tests this shipping helper against the running
recovered source before its second WEB4 query test. It also explicitly rejects
the original provisioning writer's authentication. Source credential files stay
in the owned fixture with restrictive modes and never enter artifacts; only
runtime-source-proof.json is retained. The image copies the helper, but the
supervisor/entrypoint does not enable Dapr. Journalled hosted provisioning,
image/runtime qualification, settings enrollment, federation and visitors remain
open. No live Development settings or existing source/cohort was changed.


### 7 November: hosted recovery fixture filename correction

Run42418683499/job128542212746 failed before the hosted-helper/restart gate: the fixture wrote users.acl while the shipping contract hashes and opens reader.acl. Initial native source save was acknowledged and initial WEB4 query passed; this is not a completed recovery proof. Corrected the fixture to use reader.acl throughout. A new run uses a unique source prefix and does not replay the prior source. The helper now shares bounded observation/startup functions for subsequent explicit provisioning;13 controlled configuration/runtime checks pass. Native qualification remains required.

Failure artifact13592882298:4903 bytes, SHA256 8d2c88f4a309703eedd2623b6b85744ae1864df7f0448afcfd53432caf915fe1.


### Explicit creation journal shared with native qualification

Docker/DaprRuntime/seed.cjs now owns the single Dapr bulk-save path used by the native fixture: validated four-record authority, exclusive intent, payload hash, file sync (plus directory sync on Linux) before one bounded POST, exclusive acknowledgement only on204. An existing intent prevents resend after both success and unknown outcome. Three focused controls pass; these are lifecycle controls, not real SDK proof. Sourceea3d78835 is currently qualifying the preceding ACL correction in run42419782648. The shared seed path requires its own exact-source native qualification. Full hosted provisioning/process lifecycle/image/enrollment remain open.


### Recovery startup wiring (not deployed)

The entrypoint now validates OASIS_SANDBOX_DAPR as an explicit boolean and routes it to the existing process supervisor. Its two services are ordered Garnet recovery, then Dapr exact-source verification, before WEB4 starts. Default remains disabled; no source is created or seeded by startup. Nine supervisor controls pass (fresh Windows IPFS bootstrap deliberately excluded because its retained directory exists);16 Dapr controls pass on cached Node24.22.1. Shell syntax passes. Initial broader test invocation failed safely on the retained IPFS bootstrap directory before mutation; directory preserved. Image and native wiring acceptance remain open.


### Hosted recovery helper native gate passed

Run42419782648/job128545514411 (ea3d7883510fd01090646475527560be54853fa6) isSUCCESS by fresh REST. Terminal job logs confirm one seed, denied writes and WEB4 query success before/after native restart with original values/ETags unchanged. This fixture invokes the actual hosted helper against the recovered source and rejects original admin authentication. Artifact13593286107 is5188bytes, SHA256651593b1d39a829bfab7e90865f0a916ee1baaa7edf8ba49f54afd6a96c87f35; archive inspection remains pending. The newer shared-seed gate42419910853 is still active. No Dapr live activation.


### Shared seed native gate passed; explicit provisioner implementation

Run42419910853/job128545899007 isaee489ed3 SUCCESS. Actual native seed journal, recovered source, denied writes and both WEB4 phases pass. Artifact13593106956:5140bytes SHA256a43d5b598cac7843e33a4a7ab2ae136cafbabe80e184d23e3bb9f8de0562fdd7 (terminal logs inspected; archive pending). Earlier helper artifact13593286107 was inspected in memory: exact ZIP hash, both TRX1/1/0skips and runtime-source-proof four originals verified.

Docker/provision-sandbox-dapr.cjs implements exclusive Linux source creation, durable original intent, one shared native save, original-value/ETag capture, stopped writer process cleanup, reader-only recovery and explicit denied-write/unchanged-record verification before final source acknowledgement. It is not invoked by normal startup. Three boundary controls pass on cached Node24; syntax passes. No full native provisioner invocation has run yet, and it is not wired into a remote publishing command or deployed image. Next: qualify this actual provisioner and its owned-process lifecycle using pinned native engines, then image/controlled source provisioning/enrollment.


### Actual provisioner native qualification connected

The Linux workflow now invokes the shipping provisioner through the --provisioner fixture mode. Runtime paths are explicit, prevalidated pinned executables; shipping defaults remain /opt/garnet and /opt/dapr. One new source is created, recovered within the provisioner, then opened through two further reader-only WEB4 phases with original values/ETags retained. Creation is not replayed. The isolated creator/native process group is stopped on an observation deadline; no unrelated process is targeted.19local Node24 controls and Python AST syntax pass. Native outcome is pending the next exact-source CI run; no image/live qualification inferred.


### Controlled creation command packaged (not deployed)

The image now includes provision-sandbox-dapr.cjs. Its standalone command consumes a bounded plan from stdin (credentials never command arguments), requires exact reviewed deployment and replica IDs, authoritative sandbox DNA enabled and Dapr readers disabled. Public receipt binds the exact input hash and active instance. Four provisioning boundary controls pass on cached Node24. Full native gate42420695067 is still active at checkout; it covers the prior provisioner implementation. The command/packaging revision requires exact-source qualification before rollout. No creation command has been sent to Railway.


### Protected laptop creation journal prepared

Provision-HostedDaprSource.py prepares a DPAPI-protected plan and performs only one explicitly selected create operation. It requires reviewed release/mount/instance/settings, records an exclusive local intent, checks six deployed implementation hashes against canonical reviewed Git objects, transmits the plan over SSH stdin and protects raw outcomes. Exact native receipt is required; unknown outcomes cannot be resent. Three receipt controls pass. A read-only probe on current deployment8783d8eb/instance5e85e983 verified Railway stdin forwarding with a harmless64byte hash receipt (no source writes).

One protected plan is now retained under LocalState/COSMIC/HostedDaprSourceCurrent/source-plan.dpapi for source owner ccafac97-66c2-411e-ba20-29cc6bf6faa0. Do not regenerate it or print its credentials. No create intent exists and no remote creation has run. Provisioner gates42420695067 and42420810266 remain active in native WEB4 qualification at last observation.


### Scoped candidate preparation

Fresh remote inspection confirms parent Development and reused candidate at88b896815, Core candidate944d380. Dapr needs provider/read registration/compiler/admission/project reference plus native package; do not copy the broad task compiler/Startup wholesale. A private release-scope record tracks exact gates. Native qualification now accepts an explicit dispatch ref so the eventual scoped candidate can be tested directly. Companion regression jobs42420810315 (PouchDB) and42420810261 (all3teaching jobs) areSUCCESS. Provisioner jobs42420695067 and42420810266 remain active. No candidate publication/deployment yet.


### Provisioner qualification: TCP port probe correction

Runs42420695067/job128548278396 and42420810266/job128548630883 failed at the first external recovery port probe, after the actual provisioner returned its source acknowledgement. Node successfully rebound all ports before returning; Python then rejected a bare bind with EADDRINUSE. The probe must test native listener ownership using the same Linux SO_REUSEADDR semantics, not reject closed-connection TIME_WAIT. DaprNativePorts.py now owns that check; active listeners still fail. Two native TCP controls were added: active-listener rejection passes on Windows; Linux closed-connection reuse is explicitly pending CI, not claimed from Windows. No accepted fixture or failed source is replayed.

Failure artifacts:13593447685/2081bytes/SHA256a0f23bc1310fb0c0805ff71e83431f3b478160edbb2cc3db7153a80530aa206b;13594085872/2080bytes/SHA25687c92bbfdb8543d4f2157a51a1ea4444cda1b8bf3ccd00cb39b8755522cb1734. Logs inspected in memory; archives not downloaded.

A scoped candidate was assembled locally as5aff049b3e2a3b4590b924ceca2ae2f262efa6a0/Coreec7d440ce5212da7683cb7579f0b9139bbadd5a1 with matching gitlink/manifest validation. It is unpublished and superseded for qualification by this pending probe correction. Preserve its assembly receipt; do not deploy or publish it as qualified. No checkout/worktree switch or local build occurred.


### Corrected scoped candidate published for qualification only

Parenteba69988852439d3b1c60f7727eea3eceefc0d5e/Coreec7d440ce5212da7683cb7579f0b9139bbadd5a1 are published by normal fast-forward pushes to the existing work/cosmic-teaching-release-candidate refs. Development remains88b896815/Core944d380. Candidate contains Dapr-only API/admission additions and native package/tools; unrelated dirty checkout paths excluded. Exact manifest/gitlink and diff checks pass.

One exact-candidate native dispatch and one teaching-enabled image dispatch were each acknowledged204. Exclusive dispatch intents/ACKs are under RecoveryLaptopResume20261107/dapr-candidate-{native,image}-dispatch.*. Qualification is pending; publication is not deployment or acceptance. Task-native port correction run42421434378/job128550494850 was observed live in boundary controls. No new remote source created and original protected plan retained.


### Enrollment tooling and current qualification handles

Publish-HostedDaprConfiguration.py requires the original protected source plan and exact native creation acknowledgement; it never creates a source. Preparation derives the original three live IDs/tombstone/missing ID and reviewed loopback component. Exactly8settings may change; existing providers/settings are preserved. Apply uses original protected snapshots and per-key acknowledgements without triggering deployment. Verify-settings checks full settings, exact active deployment/instance and mounted original values/ETags through read-only native requests. Three focused controls pass (scope preservation, existing-reader rejection, foreign-catalogue rejection). No protected enrollment candidate can be prepared until actual source creation is acknowledged. No prepare/apply command has run.

Fresh observations: task42421434378/job128550494850 and scoped candidate42421572256/job128550926747 passed boundary controls (including Linux TCP semantics) and remain in native WEB4 qualification; exact image42421574240/job128550932896 is building. Candidate iseba699888/Coreec7d440, not deployed.


### Exact candidate compiled; fixture private-file creation corrected

Candidate native42421572256/job128550926747 built with0errors but failed in second recovery helper verification: fixture sidecar removed its component after first recovery and recreated it with default0644 permissions. Shipping provisioner creates0600; helper correctly refuses broad permissions. The fixture now creates component files using os.open(...,0600), preserving the shipping invariant rather than weakening validation. Prior source generation is not replayed. Failure artifact13593453745:4950bytes SHA25609411d5683657a65e9f2c575de566ac8cc2b877a64cb7b9e16a9416ff175be84. No live changes; image42421574240 remains building.


### Private-component candidate correction published

Candidatec9f6ac65df440986fab0ad99c270801e84265828 advanceseba699888 by the fixture private-file creation change only; Core remains ec7d440ce5212da7683cb7579f0b9139bbadd5a1. Manifest/gitlink and diff checks pass. Published by normal fast-forward to the existing candidate branch; one exact native qualification dispatch acknowledged204. No second image build was dispatched yet: await native result before paying for a new exact-image qualification. Development/live remain unchanged. Exclusive candidate/native dispatch receipts are under RecoveryLaptopResume20261107/dapr-candidate-private-component-*.


### Exact scoped provisioner/native WEB4 gate passed

Candidatec9f6ac65df440986fab0ad99c270801e84265828/Coreec7d440ce5212da7683cb7579f0b9139bbadd5a1 passes42422065811/job128552421093. Actual shipping provisioner creates4records once, verifies denied writes and durable recovery, then both external WEB4 recovery stages pass1/1/0skips with unchanged original values/ETags. Actual hosted helper verifies all4 originals. Boundary controls, including Linux listener/TIME_WAIT semantics, pass. Task-source run42422012395 is alsoSUCCESS; no broader provider claim.

Artifact13593759142:5209bytes SHA25627630b488f15f69aa709eb77f2f78a3cecc2762c6fb9f332b422d18ec8f7e735. Inspected entirely in memory: exact ZIP hash, two passing TRX, one4record seed intent/ACK with matching original prefix/no replay, one actual helper proof. No downloaded archive/extraction retained. Compact evidence dapr-exact-candidate-native-qualified.json.

One exactc9f6ac65d teaching-enabled image request acknowledged204 after native acceptance. Earlier image42421574240 covers earlier candidateeba699888 and cannot certify the corrected commit. Exact current image remains pending. Development/live and original protected source plan unchanged; no Railway create operation yet.


### Exact image passed; scoped Development code deployment queued

Exact image42422745498/job128554513809 isSUCCESS for c9f6ac65df440986fab0ad99c270801e84265828 with teaching_sidecars=true. Published image digestsha256:76c37a16693618b98856213054ad403a5ee78daeab6970f5712daf5034dd4d90. Exact source/provenance and pushed manifest verified in logs. All7private pins have advertised exact refs; matching gitlinks/manifest and current Development ancestry checked before a normal fast-forward push.

Development now points toc9f6ac65d/Coreec7d440. Railway code deployment00eace0e-8080-4932-91b3-a7658f33d1b1 was observedQUEUED for that exact commit. Do not request another deployment while this one exists. Active instance remains prior release untilSUCCESS is independently verified. Dapr settings remain absent/disabled and provider not admitted. No remote source creation or enrollment has run.

A protected pre-code snapshot preserves245settings (SHA25633e2d85c05d0c7416a3fdbf3bbf1af95c9dd4af198a1024b8252824b4fd39540); compare before proceeding. Railway actual rebuilt image digest must be observed independently; the GitHub image digest does not establish it. Next: observe this exact deployment, inspect its actual package/mount/settings, then use the original protected Dapr source plan once.


### Live package verified; first source creation unacknowledged under host AIO restriction

Code00eace0e-8080-4932-91b3-a7658f33d1b1 isSUCCESS on instance5693a2e5-7156-4e7d-969b-9924797684f8, actual image sha256:2777ec8ed780529cfb5c3937e9cdad50bedd54694e21910d49bdc572c50e5d2d. Verified6helper Git hashes,193critical active settings and complete245setting baseline unchanged,4SDK/provider DLL hashes, Dapr1.18.9/Garnet2.2.5, pinned native build receipt/hash, source directory absent before creation. Native library SHA2566b9085aa289f0af047fdd41217a295b2823cf43d18749ac08ba9effc2da48159.

Exactly one create command was sent with the original protected plan/source ownerccafac97-66c2-411e-ba20-29cc6bf6faa0. It failed with native save timeout; protected raw outcome retained. Read-only reconciliation finds creation.intent.json and seed.intent.json, no seed/source acknowledgement, and0owned processes remaining. Garnet logs explicitly report io_getevents/libaio completion drainer errno1 Operation not permitted on the Railway host. CI native success does not prove host syscall permissions. Original source directory/AOF/configuration and unacknowledged intent must remain intact; never repeat creation or seed, replace identity, delete source, or activate Dapr.

Exact redacted inspection is RecoveryLaptopResume20261107/dapr-original-creation-failure-inspection.json; original raw outcome is HostedDaprSourceCurrent/create.outcome.dpapi. Investigate officially supported native storage/host policy contracts before any change. Existing demo settings remain unchanged and Dapr disabled; counts unchanged. No account action requested.


### Host native backend contract: explicit io_uring qualification

Read-only host inspection: io_uring_disabled=0, Seccomp=2, aio-nr28706/aio-max65536; original source has one2048byte AOF file. Pinned upstream Options.cs documents --device-type Native and --device-io-backend Uring; default Linux backend is libaio. Source: https://github.com/microsoft/garnet/blob/8d007138208ce030eb10a50c80e636b1678b83e4/libs/host/Configuration/Options.cs .

One explicit Native/Uring startup diagnostic ran on5693a2e5 in an empty owned scratch root /tmp/cosmic-dapr-uring-5t8CfL, port54874, no business records/SDK save. It started without native completion errors and the owned process was stopped. This is startup capability evidence only, not durable-write or original-source recovery proof. It did not access the original source. Exclusive local intent/receipt: dapr-native-uring-preflight.* under RecoveryLaptopResume20261107.

Linux configuration now selects exactly Native/Uring in the shared contract; Windows keeps its supported native default. No runtime error-driven backend selection, storage-device replacement or AOF-format change.21local configuration/runtime/journal/provisioner controls pass on cached Node24. Full exact-source Linux write/restart/WEB4 qualification is required before candidate/image/live changes. Original source creation remains unacknowledged and may only be reconciled, never resent/recreated. The2048byte AOF is not proof of complete data.


### Original pending-source read-only inspector implemented (not executed)

The provisioner now exposes inspectPendingSource: validates original creation/seed journal, exact operation/key list/payload hash and absence of acknowledgements; refuses reseeding or already accepted state. It uses reader-only credentials in separate private inspection configuration, opens the original AOF through strict recovery twice, verifies all4original values/ETags, requires original native file bytes/hashes unchanged, and records inspection evidence only. There is no POST/save in this operation and it does not create an acknowledgement for the original lost save response. It does not overwrite original writer configuration. Inspection-directory exclusivity preserves interrupted outcomes.

22local controls pass, including original-intent tampering rejection. This inspector has not been exercised against native engines or the live original source; native failure-injection coverage is the next gate. Explicit Native/Uring full qualification42425594492/job128563068634 remains active in native WEB4 checks. No original source recovery/seed or new image/deployment performed in this step.


### Native lost-response reconciliation fixture connected

Scripts/TestHosts/qualify-dapr-pending-recovery.cjs now exercises the actual provisioner and pending inspector with real pinned Garnet/Dapr engines. One native save must receive204at the service boundary, then its response delivery to the journal owner is deliberately lost. No save/source ACK is fabricated or deleted. The native inspector must recover all4original records twice, preserve original AOF hashes/ETags and issue zero mutations; original save ACK must remain absent afterwards. This is controlled response-loss injection with real storage, not evidence that the Railway original save completed.

The Linux workflow now runs that fixture under the existing lease after native WEB4 qualification and retains only its compact pending-recovery-native-proof.json. Syntax and6Python command/enrollment controls pass locally; no local native runtime/build/install. Native outcome pending the next exact-source run. Original Railway source/intent remains untouched and Dapr disabled.


### Recovered-outcome metadata transition implemented (not executed)

acknowledgeRecoveredSource requires original private creation/seed evidence, a matching two-recovery inspection, all4exact values/ETags and unchanged current AOF hashes. It validates original writer ACL/component bytes, requires free native ports, journals the transition, installs reader-only configuration, and records seed.reconciled.json outcome=applied plus source metadata. It leaves the original save-response acknowledgement absent and preserves original writer/seed evidence. No SDK save or business mutation is made. Interrupted metadata transitions remain journalled and cannot be repeated automatically.

23local controls pass, including rejection of changed AOF/recovery/record evidence. The native loss fixture now requires this actual transition, shipping reader-source validation and explicit refusal of a second metadata transition. It still requires one save total and no fabricated save-response ACK. Full native result remains pending; no inspector/metadata transition has been sent to the live preserved source.


### Protected two-operation reconciliation transport prepared

Reconcile-HostedDaprSource.py provides inspect and acknowledge as separate explicit lifecycle operations. Both require the immutable original plan/create intent, exact reviewed release/image/mount/instance, disabled readers, installed helper hashes and unchanged settings. Exclusive local intents and DPAPI outcomes prevent replay after uncertainty. Acknowledge requires the exact preceding inspection operation/AOF hash and records recovered outcome context separately from the original creation deployment/instance. It never calls provision or seed.

The final local creation-outcome acknowledgement, if earned, preserves original creation intent hash/context and explicit originalSaveResponseAcknowledged=false. Enrollment now validates that provenance and exact inspection hashes.9Python receipt/enrollment controls pass;23Node controls were previously passed for runtime/reconciliation metadata. No live reconciliation command has run. Native lost-response runs42425997393/job128564274035 and42426250057/job128565052145 remain active; Native/Uring baseline42425594492 isSUCCESS. Inspector-source baseline42425853589 isSUCCESS but did not execute the new loss fixture.


### Lost-response fixture reader-handshake failure; redacted wire diagnostic

Run42425997393/job128564274035 passed normal provisioner/native WEB4 recovery, then failed the new pending-source fixture with "Dapr native reader response differs." No protocol bytes were in the old error, so cause is not yet established. Artifact13594559919:5633bytes SHA2565e12907163db77e7c0a77bc311d8c9ef3356c92827f7eb9495abd4a344338b25 (terminal logs inspected in memory; archive not downloaded). Do not weaken the canonical AUTH/PING response contract or infer recovery acceptance.

One separate read-only native RESP diagnostic reused /tmp/cosmic-dapr-uring-5t8CfL, port54874, Native/Uring, reader-only ACL,0business writes; actual host reply was +OK\r\n+PONG\r\n and owned process stopped. Original source untouched. Local exclusive diagnostic intent/receipt retained. The helper now includes bounded received-frame text only on the unexpected-frame error, redacting the exact reader credential first;8runtime controls pass, including credential-redaction/socket-stop coverage. Next native run will capture the actual unexpected frame rather than guess a different format.

A local unpublished candidate da1c775c2f2f62db968eaf1f8ef15d66b55286f6 was assembled from current Developmentc9f6ac65d with11Dapr-only runtime/reconciliation/tool changes, Core unchanged. Pins/diff pass; it is not published/deployed and is superseded for qualification by this pending diagnostic. Preserve assembly receipt; no image request yet.


### Native recovered-outcome proof passed; scoped correction candidate published

Run42426250057/job128565052145 (c15b650af32b558931eac8f4b9f4820ed11c10ad) isSUCCESS. Artifact13595766967:6067bytes SHA256e204ef2c7bd534bfc98a2281144ad06cd19c8f51d10b691f276b361e9c7ca5e3, verified entirely in memory: normal two WEB4 TRX1/1/0skips and actual lost-response native proof. The latter shows one save request, injected lost response,4original records recovered twice,0inspection mutations, unchanged AOF, recovered metadata accepted, no fabricated save-response ACK and native processes stopped. This proves the controlled fixture, not the original Railway outcome. Earlier unexplained handshake failure remains retained; redacted-frame diagnostics are included in the next candidate.

Candidate81e4cd04360ee6ed37dfed6da1737229e70f1d3c is published by fast-forward on the existing candidate branch, based on livec9f6ac65d. It contains13Dapr-only storage/recovery/tool/control files; Coreec7d440 unchanged. Matching pins/gitlinks and diff checks pass. One exact native dispatch and one teaching-enabled exact image dispatch acknowledged204; retain original intents under dapr-uring-candidate-{native,image}-dispatch.*. No new Development deployment or original live-source operation occurred. Next qualify this exact candidate, observe a new code deployment, then inspect/reconcile the same source using the protected two-operation transport.
